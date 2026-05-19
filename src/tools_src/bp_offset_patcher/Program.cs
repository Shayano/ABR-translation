// Edit-in-place BP string patcher with shift-map offset recompute.
//
// Approach: replace EX_StringConst values in place; for each replacement,
// record (oldPos, oldSize, newSize). Build a sorted shift table so that
// shift(oldTarget) = oldTarget + sum(delta for inserts strictly before
// oldTarget). Then walk every absolute-jump opcode (EX_Jump, EX_JumpIfNot,
// EX_PushExecutionFlow, EX_SwitchValue.EndGotoOffset + cases.NextOffset)
// and rewrite their offset via shift().
//
// We deliberately do NOT touch EX_Context.Offset and EX_Skip.CodeOffset
// because empirical scans show these are typically small relative values
// (skip sizes) rather than absolute bytecode positions. If a ContextExpression
// changes size, this remains a known limitation - log a warning if so.
//
// Usage:
//   BPOffsetPatcher <input.uasset> <output_dir> <usmap> --export=<name> --strings-json=<path>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

class Program
{
    public class StringEntry
    {
        public string Original { get; set; }
        public string Translation { get; set; }
    }

    static int Main(string[] args)
    {
        if (args.Length < 5) {
            Console.Error.WriteLine("Usage: BPOffsetPatcher <input.uasset> <output_dir> <usmap> --export=<name> --strings-json=<path>");
            return 1;
        }
        string inAsset = args[0];
        string outDir = args[1];
        string usmapPath = args[2];
        string exportName = args.Skip(3).FirstOrDefault(a => a.StartsWith("--export="))?.Substring("--export=".Length);
        string stringsJsonPath = args.Skip(3).FirstOrDefault(a => a.StartsWith("--strings-json="))?.Substring("--strings-json=".Length);

        if (string.IsNullOrEmpty(exportName)) { Console.Error.WriteLine("Missing --export=<name>"); return 1; }
        if (string.IsNullOrEmpty(stringsJsonPath)) { Console.Error.WriteLine("Missing --strings-json=<path>"); return 1; }
        if (!File.Exists(stringsJsonPath)) { Console.Error.WriteLine($"strings JSON not found: {stringsJsonPath}"); return 1; }

        string inUexp = Path.ChangeExtension(inAsset, "uexp");
        string inUbulk = Path.ChangeExtension(inAsset, "ubulk");
        if (!File.Exists(inAsset)) { Console.Error.WriteLine($"Missing {inAsset}"); return 1; }
        if (!File.Exists(inUexp)) { Console.Error.WriteLine($"Missing {inUexp}"); return 1; }

        var entries = JsonSerializer.Deserialize<List<StringEntry>>(
            File.ReadAllText(stringsJsonPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var valid = entries.Where(e => !string.IsNullOrEmpty(e?.Translation)).ToList();
        Console.Error.WriteLine($"Loaded {entries.Count} entries ({valid.Count} translatable).");
        if (valid.Count == 0) return 1;

        // ---- Load asset header only ----
        var asset = new UAsset();
        asset.SetEngineVersion(EngineVersion.VER_UE5_3);
        asset.CustomSerializationFlags = CustomSerializationFlags.SkipParsingBytecode;
        if (File.Exists(usmapPath)) asset.Mappings = new Usmap(usmapPath);
        byte[] uassetBytes = File.ReadAllBytes(inAsset);
        long uassetHeaderSize = uassetBytes.LongLength;
        using (var headerStream = new MemoryStream(uassetBytes, writable: false))
        using (var headerReader = new AssetBinaryReader(headerStream, false, asset))
            asset.Read(headerReader);

        Export targetExport = null;
        int targetIdx = -1;
        for (int i = 0; i < asset.Exports.Count; i++) {
            if (asset.Exports[i].ObjectName?.ToString() == exportName) {
                targetExport = asset.Exports[i];
                targetIdx = i;
                break;
            }
        }
        if (targetExport == null) { Console.Error.WriteLine($"Export '{exportName}' not found."); return 2; }

        long localOffsetInUexp = targetExport.SerialOffset - uassetHeaderSize;
        long oldSize = targetExport.SerialSize;
        byte[] exportBytes = new byte[oldSize];
        using (var fs = File.OpenRead(inUexp)) {
            fs.Seek(localOffsetInUexp, SeekOrigin.Begin);
            int read = 0;
            while (read < exportBytes.Length) {
                int n = fs.Read(exportBytes, read, exportBytes.Length - read);
                if (n <= 0) break;
                read += n;
            }
        }

        asset.CustomSerializationFlags &= ~CustomSerializationFlags.SkipParsingBytecode;
        using (var ms = new MemoryStream(exportBytes, writable: false))
        using (var reader = new AssetBinaryReader(ms, true, asset))
            targetExport.Read(reader, (int)oldSize);

        var struc = (StructExport)targetExport;
        if (struc.ScriptBytecode == null) { Console.Error.WriteLine("ScriptBytecode null."); return 5; }
        Console.Error.WriteLine($"Parsed {struc.ScriptBytecode.Length} top-level exprs, total bytecode size {struc.ScriptBytecodeSize}.");

        // ============ Phase A : preScan ============
        // For every expression, record (objectRef -> oldPosition) AND list jump-like opcodes.
        var oldExprPos = new Dictionary<KismetExpression, uint>(ReferenceEqualityComparer.Instance);
        var jumpables = new List<JumpableRef>();
        uint walkA = 0;
        foreach (var top in struc.ScriptBytecode) {
            top.Visit(asset, ref walkA, (e, off) => {
                if (!oldExprPos.ContainsKey(e)) oldExprPos[e] = off;
                if (e is EX_Jump j) jumpables.Add(new JumpableRef(j, JumpKind.Jump));
                else if (e is EX_JumpIfNot jin) jumpables.Add(new JumpableRef(jin, JumpKind.JumpIfNot));
                else if (e is EX_PushExecutionFlow pef) jumpables.Add(new JumpableRef(pef, JumpKind.PushExecutionFlow));
                else if (e is EX_SwitchValue sv) {
                    jumpables.Add(new JumpableRef(sv, JumpKind.SwitchValueEnd));
                    for (int i = 0; i < sv.Cases.Length; i++) {
                        jumpables.Add(new JumpableRef(sv, JumpKind.SwitchValueCase, i));
                    }
                }
            });
        }
        Console.Error.WriteLine($"PreScan: {oldExprPos.Count} expressions, {jumpables.Count} absolute-jump-like opcodes.");

        // ============ Phase B : replace strings + record (pos, oldSize, newSize) ============
        var mods = new List<(uint pos, int oldSize, int newSize)>();
        int totalPatched = 0;
        int skipped = 0;
        foreach (var entry in valid) {
            var found = FindMatchInBytecode(struc.ScriptBytecode, entry.Original);
            if (found.parent == null) {
                skipped++;
                Console.Error.WriteLine($"  [skip] '{Preview(entry.Original)}'");
                continue;
            }
            // Compute old size of the matched string-expr at its known oldPos.
            if (!oldExprPos.TryGetValue(found.matched, out uint oldStringPos)) {
                skipped++;
                Console.Error.WriteLine($"  [skip] '{Preview(entry.Original)}' (no oldPos)");
                continue;
            }
            int oldSizeBytes = SizeOfStringConst(found.matched);
            var newExpr = MakeStringConst(entry.Translation);
            int newSizeBytes = SizeOfStringConst(newExpr);
            ReplaceSlot(found, newExpr);
            mods.Add((oldStringPos, oldSizeBytes, newSizeBytes));
            totalPatched++;
            Console.Error.WriteLine($"  [OK]   '{Preview(entry.Original)}' @{oldStringPos} ({oldSizeBytes} -> {newSizeBytes})");
        }
        Console.Error.WriteLine($"Strings: {totalPatched} replacements, {skipped} skipped.");
        if (totalPatched == 0) return 6;

        // ============ Phase C : build shift table ============
        var sortedMods = mods.OrderBy(m => m.pos).ToList();
        long Shift(uint oldTarget) {
            long delta = 0;
            foreach (var m in sortedMods) {
                if (m.pos < oldTarget) delta += (m.newSize - m.oldSize);
                else break;
            }
            return (long)oldTarget + delta;
        }
        Console.Error.WriteLine($"Shift table: {sortedMods.Count} entries, total delta {sortedMods.Sum(m => m.newSize - m.oldSize):+#;-#;0} bytes.");

        // ============ Phase D : update absolute jumps ============
        int updated = 0, kept = 0;
        foreach (var jop in jumpables) {
            uint oldT = jop.GetOldTarget();
            long newT = Shift(oldT);
            if (newT < 0 || newT > uint.MaxValue) {
                Console.Error.WriteLine($"  WARN: shift out of range for {jop.Kind} @ old={oldT} -> new={newT}");
                continue;
            }
            if ((uint)newT != oldT) {
                jop.SetNewTarget((uint)newT);
                updated++;
            } else {
                kept++;
            }
        }
        Console.Error.WriteLine($"Offsets: {updated} updated, {kept} unchanged.");

        // ============ Phase D' : patch callers to ExecuteUbergraph_<target> ============
        // For every OTHER export, parse it, find calls to the target Ubergraph, shift the
        // EX_IntConst entry-point, then re-serialize the WHOLE export. We stage (callerIdx,
        // origSerialOffsetInUexp, oldSize, newBytes) so the .uexp splice can substitute each
        // modified export. Size change should be 0 (only an int value changes) but we handle
        // any case defensively.
        var callerPatches = new List<(int callerIdx, long origLocalOffset, long oldSize, byte[] newBytes)>();
        int callerHits = 0;
        for (int ci = 0; ci < asset.Exports.Count; ci++) {
            if (ci == targetIdx) continue;
            var ce = asset.Exports[ci];
            if (ce is not StructExport cse) continue;

            long cLocalOff = ce.SerialOffset - uassetHeaderSize;
            long cOldSize = ce.SerialSize;
            byte[] cBytes = new byte[cOldSize];
            using (var fs = File.OpenRead(inUexp)) {
                fs.Seek(cLocalOff, SeekOrigin.Begin);
                int rd = 0;
                while (rd < cBytes.Length) {
                    int n = fs.Read(cBytes, rd, cBytes.Length - rd);
                    if (n <= 0) break;
                    rd += n;
                }
            }
            try {
                using (var ms = new MemoryStream(cBytes, writable: false))
                using (var rdr = new AssetBinaryReader(ms, true, asset))
                    ce.Read(rdr, (int)cOldSize);
            } catch { continue; }
            if (cse.ScriptBytecode == null) continue;

            // Recursive walk to find and mutate calls.
            bool modified = false;
            void ScanCalls(KismetExpression node) {
                if (node == null) return;
                KismetExpression[] callParams = null;
                UAssetAPI.UnrealTypes.FPackageIndex sn = null;
                if (node is EX_LocalFinalFunction lff2) { sn = lff2.StackNode; callParams = lff2.Parameters; }
                else if (node is EX_FinalFunction ff2)  { sn = ff2.StackNode;  callParams = ff2.Parameters; }
                if (sn != null && callParams != null && callParams.Length > 0
                    && ResolveStackNodeName(asset, sn) == exportName
                    && callParams[0] is EX_IntConst ic2) {
                    long shifted = Shift((uint)ic2.Value);
                    if (shifted != ic2.Value) {
                        ic2.Value = (int)shifted;
                        modified = true;
                        callerHits++;
                    }
                }
                var t = node.GetType();
                var bind = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
                foreach (var f in t.GetFields(bind)) {
                    if (typeof(KismetExpression).IsAssignableFrom(f.FieldType)) {
                        var c = (KismetExpression)f.GetValue(node);
                        if (c != null) ScanCalls(c);
                    } else if (f.FieldType == typeof(KismetExpression[])) {
                        var arr = (KismetExpression[])f.GetValue(node);
                        if (arr != null) foreach (var c in arr) ScanCalls(c);
                    }
                }
            }
            foreach (var top in cse.ScriptBytecode) ScanCalls(top);

            if (!modified) continue;

            // Re-serialize this caller export. Account for the post-bytecode trailer that
            // UAssetAPI doesn't model (same trick as for target).
            byte[] writerOut;
            using (var ms2 = new MemoryStream())
            using (var w = new AssetBinaryWriter(ms2, asset)) {
                ce.Write(w);
                w.Flush();
                writerOut = ms2.ToArray();
            }
            int cTrailer = (int)(cOldSize - writerOut.LongLength);
            if (cTrailer < 0) cTrailer = 0;
            byte[] finalCBytes;
            if (cTrailer > 0) {
                finalCBytes = new byte[writerOut.Length + cTrailer];
                Buffer.BlockCopy(writerOut, 0, finalCBytes, 0, writerOut.Length);
                Buffer.BlockCopy(cBytes, cBytes.Length - cTrailer, finalCBytes, writerOut.Length, cTrailer);
            } else {
                finalCBytes = writerOut;
            }
            callerPatches.Add((ci, cLocalOff, cOldSize, finalCBytes));
        }
        Console.Error.WriteLine($"Callers: scanned {asset.Exports.Count - 1} non-target exports, modified {callerPatches.Count} exports ({callerHits} EX_IntConst shifted).");

        // ============ Phase E : serialize new bytecode ============
        byte[] newExportBytes;
        using (var ms2 = new MemoryStream())
        using (var writer = new AssetBinaryWriter(ms2, asset)) {
            targetExport.Write(writer);
            writer.Flush();
            newExportBytes = ms2.ToArray();
        }

        // Detect post-bytecode trailer (bytes after ScriptBytecode that UAssetAPI doesn't model).
        int trailerLen;
        {
            var fresh = new UAsset();
            fresh.SetEngineVersion(EngineVersion.VER_UE5_3);
            fresh.CustomSerializationFlags = CustomSerializationFlags.SkipParsingBytecode;
            if (File.Exists(usmapPath)) fresh.Mappings = new Usmap(usmapPath);
            using (var hs = new MemoryStream(uassetBytes, writable: false))
            using (var hr = new AssetBinaryReader(hs, false, fresh))
                fresh.Read(hr);
            var freshTarget = (StructExport)fresh.Exports[targetIdx];
            fresh.CustomSerializationFlags &= ~CustomSerializationFlags.SkipParsingBytecode;
            using (var ms3 = new MemoryStream(exportBytes, writable: false))
            using (var rr = new AssetBinaryReader(ms3, true, fresh))
                freshTarget.Read(rr, (int)oldSize);
            byte[] rt;
            using (var ms4 = new MemoryStream())
            using (var ww = new AssetBinaryWriter(ms4, fresh)) {
                freshTarget.Write(ww);
                ww.Flush();
                rt = ms4.ToArray();
            }
            trailerLen = (int)(oldSize - rt.LongLength);
            if (trailerLen < 0) trailerLen = 0;
        }
        if (trailerLen > 0) {
            byte[] trailer = new byte[trailerLen];
            Buffer.BlockCopy(exportBytes, exportBytes.Length - trailerLen, trailer, 0, trailerLen);
            byte[] withT = new byte[newExportBytes.Length + trailerLen];
            Buffer.BlockCopy(newExportBytes, 0, withT, 0, newExportBytes.Length);
            Buffer.BlockCopy(trailer, 0, withT, newExportBytes.Length, trailerLen);
            newExportBytes = withT;
        }
        long newSize = newExportBytes.LongLength;
        long delta = newSize - oldSize;
        Console.Error.WriteLine($"Export size: {oldSize} -> {newSize} (delta {delta:+#;-#;0}, trailer {trailerLen}).");

        Directory.CreateDirectory(outDir);
        string outAsset = Path.Combine(outDir, Path.GetFileName(inAsset));
        string outUexp = Path.Combine(outDir, Path.GetFileName(inUexp));
        string outUbulk = Path.Combine(outDir, Path.GetFileName(inUbulk));

        // Build a sorted list of splice regions (target + every modified caller). Each region
        // says: "replace bytes [origLocalOffset .. origLocalOffset+oldSize) of the input .uexp
        // by newBytes". For callers, newBytes.Length == oldSize (we only mutated an int32).
        // For the target, newBytes.Length == oldSize + delta.
        var splices = new List<(long origStart, long oldSize, byte[] newBytes)>();
        splices.Add((localOffsetInUexp, oldSize, newExportBytes));
        foreach (var (ci, origLocalOff, cOldSize, cNewBytes) in callerPatches) {
            if (cNewBytes.LongLength != cOldSize) {
                Console.Error.WriteLine($"  WARN: caller export idx={ci} re-serialized to {cNewBytes.LongLength} bytes (orig {cOldSize}); size drift will cascade.");
            }
            splices.Add((origLocalOff, cOldSize, cNewBytes));
        }
        splices.Sort((a, b) => a.origStart.CompareTo(b.origStart));

        using (var inFs = File.OpenRead(inUexp))
        using (var outFs = File.Create(outUexp)) {
            long inPos = 0;
            foreach (var (origStart, oldSz, newB) in splices) {
                if (origStart > inPos) {
                    CopyRange(inFs, outFs, inPos, origStart - inPos);
                    inPos = origStart;
                }
                outFs.Write(newB, 0, newB.Length);
                inPos += oldSz;
            }
            if (inPos < inFs.Length) {
                CopyRange(inFs, outFs, inPos, inFs.Length - inPos);
            }
        }
        Console.Error.WriteLine($"Wrote {outUexp} ({new FileInfo(outUexp).Length:N0} bytes) with {splices.Count} splice(s).");

        targetExport.SerialSize = newSize;
        long origTargetSerialOffset = targetExport.SerialOffset; // save before shifting siblings
        for (int i = 0; i < asset.Exports.Count; i++) {
            if (i == targetIdx) continue;
            var e = asset.Exports[i];
            if (e.SerialOffset > origTargetSerialOffset) e.SerialOffset += delta;
        }

        var fld = typeof(UAsset).GetField("ExportOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        long exportOffset = fld != null ? (int)fld.GetValue(asset) : 0;
        if (exportOffset <= 0 || exportOffset >= uassetBytes.Length) {
            Console.Error.WriteLine("Cannot locate export map.");
            return 7;
        }

        var bind2 = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        long targetOrigOffset = targetExport.SerialOffset - delta;
        string[] offsetFieldNames = {
            "BulkDataStartOffset", "PreloadDependencyOffset", "AssetRegistryDataOffset",
            "WorldTileInfoDataOffset", "DependsOffset", "SectionSixOffset"
        };
        var pendingPatches = new List<(string Name, long Old, long New, bool Is64)>();
        foreach (var name in offsetFieldNames) {
            var f2 = typeof(UAsset).GetField(name, bind2);
            if (f2 == null) continue;
            object cur = f2.GetValue(asset);
            if (cur is long lv && lv > targetOrigOffset) {
                pendingPatches.Add((name, lv, lv + delta, true));
                f2.SetValue(asset, lv + delta);
            } else if (cur is int iv && iv > targetOrigOffset) {
                pendingPatches.Add((name, iv, iv + delta, false));
                f2.SetValue(asset, (int)(iv + delta));
            }
        }

        byte[] newUassetBytes = (byte[])uassetBytes.Clone();
        int headerSearchEnd = (int)Math.Min(4096, (long)exportOffset);
        foreach (var (name, oldVal, newVal, is64) in pendingPatches) {
            byte[] ob = is64 ? BitConverter.GetBytes(oldVal) : BitConverter.GetBytes((int)oldVal);
            byte[] nb = is64 ? BitConverter.GetBytes(newVal) : BitConverter.GetBytes((int)newVal);
            for (int i = 0; i <= headerSearchEnd - ob.Length; i++) {
                bool match = true;
                for (int j = 0; j < ob.Length; j++) {
                    if (newUassetBytes[i + j] != ob[j]) { match = false; break; }
                }
                if (match) Buffer.BlockCopy(nb, 0, newUassetBytes, i, nb.Length);
            }
        }

        long entrySize = Export.GetExportMapEntrySize(asset);
        const int SerialSizeOffset = 28;
        const int SerialOffsetOffset = 36;
        for (int i = 0; i < asset.Exports.Count; i++) {
            var e = asset.Exports[i];
            long origSerialOffset = (i == targetIdx) ? e.SerialOffset
                : (e.SerialOffset > targetExport.SerialOffset ? e.SerialOffset - delta : e.SerialOffset);
            bool needs = (i == targetIdx) || (e.SerialOffset != origSerialOffset);
            if (!needs) continue;
            long entryPos = exportOffset + (long)i * entrySize;
            byte[] sb = BitConverter.GetBytes((long)e.SerialSize);
            Buffer.BlockCopy(sb, 0, newUassetBytes, (int)(entryPos + SerialSizeOffset), 8);
            byte[] ob = BitConverter.GetBytes((long)e.SerialOffset);
            Buffer.BlockCopy(ob, 0, newUassetBytes, (int)(entryPos + SerialOffsetOffset), 8);
        }
        File.WriteAllBytes(outAsset, newUassetBytes);
        Console.Error.WriteLine($"Wrote {outAsset} ({newUassetBytes.LongLength:N0} bytes).");
        if (File.Exists(inUbulk)) File.Copy(inUbulk, outUbulk, overwrite: true);
        Console.Error.WriteLine("DONE.");
        return 0;
    }

    // ----------------- Jumpable opcode bookkeeping -----------------

    enum JumpKind { Jump, JumpIfNot, PushExecutionFlow, SwitchValueEnd, SwitchValueCase }

    class JumpableRef
    {
        public KismetExpression Holder;
        public JumpKind Kind;
        public int CaseIndex;
        public JumpableRef(KismetExpression holder, JumpKind kind, int caseIndex = -1)
        {
            Holder = holder; Kind = kind; CaseIndex = caseIndex;
        }
        public uint GetOldTarget() => Kind switch
        {
            JumpKind.Jump => ((EX_Jump)Holder).CodeOffset,
            JumpKind.JumpIfNot => ((EX_JumpIfNot)Holder).CodeOffset,
            JumpKind.PushExecutionFlow => ((EX_PushExecutionFlow)Holder).PushingAddress,
            JumpKind.SwitchValueEnd => ((EX_SwitchValue)Holder).EndGotoOffset,
            JumpKind.SwitchValueCase => ((EX_SwitchValue)Holder).Cases[CaseIndex].NextOffset,
            _ => 0
        };
        public void SetNewTarget(uint v)
        {
            switch (Kind) {
                case JumpKind.Jump: ((EX_Jump)Holder).CodeOffset = v; break;
                case JumpKind.JumpIfNot: ((EX_JumpIfNot)Holder).CodeOffset = v; break;
                case JumpKind.PushExecutionFlow: ((EX_PushExecutionFlow)Holder).PushingAddress = v; break;
                case JumpKind.SwitchValueEnd: ((EX_SwitchValue)Holder).EndGotoOffset = v; break;
                case JumpKind.SwitchValueCase:
                    var sv = (EX_SwitchValue)Holder;
                    sv.Cases[CaseIndex] = new FKismetSwitchCase(
                        sv.Cases[CaseIndex].CaseIndexValueTerm, v, sv.Cases[CaseIndex].CaseTerm);
                    break;
            }
        }
    }

    // ----------------- String replacement helpers -----------------

    struct MatchResult
    {
        public object parent;       // KismetExpression or KismetExpression[]
        public string parentFieldName;
        public int arrayIndex;
        public KismetExpression matched;
    }

    static int SizeOfStringConst(KismetExpression e)
    {
        if (e is EX_StringConst sc) return 1 + (sc.Value?.Length ?? 0) + 1;
        if (e is EX_UnicodeStringConst usc) return 1 + 2 * ((usc.Value?.Length ?? 0) + 1);
        return 0;
    }

    static KismetExpression MakeStringConst(string s)
    {
        if (s.All(c => c <= 0x7E)) return new EX_StringConst { Value = s };
        return new EX_UnicodeStringConst { Value = s };
    }

    static bool IsStringMatch(KismetExpression e, string original)
    {
        if (e is EX_StringConst sc && sc.Value == original) return true;
        if (e is EX_UnicodeStringConst usc && usc.Value == original) return true;
        return false;
    }

    static void ReplaceSlot(MatchResult m, KismetExpression newExpr)
    {
        if (m.parent is KismetExpression[] arr) {
            arr[m.arrayIndex] = newExpr;
            return;
        }
        if (m.parent is KismetExpression parentExpr) {
            var t = parentExpr.GetType();
            var bind = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
            var f = t.GetField(m.parentFieldName, bind);
            if (f != null) { f.SetValue(parentExpr, newExpr); return; }
            var p = t.GetProperty(m.parentFieldName, bind);
            if (p != null && p.CanWrite) { p.SetValue(parentExpr, newExpr); return; }
            // FScriptText subfields
            if (parentExpr is EX_TextConst tc && tc.Value != null) {
                var stT = tc.Value.GetType();
                var sf = stT.GetField(m.parentFieldName, bind);
                if (sf != null) { sf.SetValue(tc.Value, newExpr); return; }
                var sp = stT.GetProperty(m.parentFieldName, bind);
                if (sp != null && sp.CanWrite) { sp.SetValue(tc.Value, newExpr); return; }
            }
            throw new InvalidOperationException($"Cannot replace slot '{m.parentFieldName}' on {t.Name}");
        }
        if (m.parent is FScriptText st) {
            var stT = st.GetType();
            var bind = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
            var sf = stT.GetField(m.parentFieldName, bind);
            if (sf != null) { sf.SetValue(st, newExpr); return; }
            var sp = stT.GetProperty(m.parentFieldName, bind);
            if (sp != null && sp.CanWrite) { sp.SetValue(st, newExpr); return; }
            throw new InvalidOperationException($"Cannot replace slot '{m.parentFieldName}' on FScriptText");
        }
        throw new InvalidOperationException("Unknown parent type in ReplaceSlot");
    }

    static MatchResult FindMatchInBytecode(KismetExpression[] top, string original)
    {
        for (int i = 0; i < top.Length; i++) {
            if (IsStringMatch(top[i], original)) {
                return new MatchResult { parent = top, parentFieldName = "(array)", arrayIndex = i, matched = top[i] };
            }
            var r = FindInExpr(top[i], original);
            if (r.matched != null) return r;
        }
        return new MatchResult();
    }

    static MatchResult FindInExpr(KismetExpression e, string original)
    {
        if (e is EX_TextConst tc && tc.Value != null) {
            var st = tc.Value;
            var fields = new (string name, KismetExpression val, Action<KismetExpression> setter)[] {
                ("LocalizedSource", st.LocalizedSource, v => st.LocalizedSource = v),
                ("LocalizedKey", st.LocalizedKey, v => st.LocalizedKey = v),
                ("LocalizedNamespace", st.LocalizedNamespace, v => st.LocalizedNamespace = v),
                ("InvariantLiteralString", st.InvariantLiteralString, v => st.InvariantLiteralString = v),
                ("LiteralString", st.LiteralString, v => st.LiteralString = v),
                ("StringTableId", st.StringTableId, v => st.StringTableId = v),
                ("StringTableKey", st.StringTableKey, v => st.StringTableKey = v),
            };
            foreach (var (name, val, setter) in fields) {
                if (val == null) continue;
                if (IsStringMatch(val, original)) {
                    return new MatchResult { parent = st, parentFieldName = name, matched = val };
                }
                var r = FindInExpr(val, original);
                if (r.matched != null) return r;
            }
            return new MatchResult();
        }
        var t = e.GetType();
        var bind = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
        foreach (var f in t.GetFields(bind)) {
            if (typeof(KismetExpression).IsAssignableFrom(f.FieldType)) {
                var child = (KismetExpression)f.GetValue(e);
                if (child == null) continue;
                if (IsStringMatch(child, original)) {
                    return new MatchResult { parent = e, parentFieldName = f.Name, matched = child };
                }
                var r = FindInExpr(child, original);
                if (r.matched != null) return r;
            } else if (f.FieldType == typeof(KismetExpression[])) {
                var arr = (KismetExpression[])f.GetValue(e);
                if (arr == null) continue;
                for (int i = 0; i < arr.Length; i++) {
                    if (arr[i] == null) continue;
                    if (IsStringMatch(arr[i], original)) {
                        return new MatchResult { parent = arr, parentFieldName = f.Name, arrayIndex = i, matched = arr[i] };
                    }
                    var r = FindInExpr(arr[i], original);
                    if (r.matched != null) return r;
                }
            }
        }
        return new MatchResult();
    }

    static string ResolveStackNodeName(UAsset asset, UAssetAPI.UnrealTypes.FPackageIndex idx)
    {
        try {
            if (idx == null) return "";
            if (idx.IsImport()) return asset.Imports[-idx.Index - 1].ObjectName.ToString();
            if (idx.IsExport()) return asset.Exports[idx.Index - 1].ObjectName.ToString();
        } catch { }
        return "";
    }

    static string Preview(string s)
    {
        if (s == null) return "<null>";
        return s.Length > 50 ? s.Substring(0, 50) + "..." : s;
    }

    static void CopyRange(Stream src, Stream dst, long offset, long length)
    {
        src.Seek(offset, SeekOrigin.Begin);
        byte[] buf = new byte[64 * 1024];
        long remaining = length;
        while (remaining > 0) {
            int toRead = (int)Math.Min(buf.Length, remaining);
            int n = src.Read(buf, 0, toRead);
            if (n <= 0) throw new IOException();
            dst.Write(buf, 0, n);
            remaining -= n;
        }
    }
}
