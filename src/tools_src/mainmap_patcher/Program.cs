// Patches MainMap.uexp to translate the intro string by:
//   1. Loading MainMap.uasset (header only, loadUEXP=false).
//   2. Locating the FunctionExport "ExecuteUbergraph_MainMap".
//   3. Reading just SerialSize bytes from .uexp via FileStream (Int64-aware).
//   4. Parsing only that export's body via UAssetAPI.
//   5. Visiting ScriptBytecode, replacing the matching EX_StringConst with
//      EX_UnicodeStringConst holding the French translation.
//   6. Re-serialising the export and reinjecting it into .uexp + adjusting
//      subsequent SerialOffsets in .uasset.
//
// Usage:
//   MainMapPatcher <input MainMap.umap> <output dir>
//
// The .uexp companion is auto-detected next to the .umap.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

class Program
{
    // The intro string we want to replace. ^ is a literal 0x5E used as line break by the BP.
    const string ORIGINAL_INTRO =
        "Oh no!^The tracks on your map got ruined when you dropped it in the water, you'll just have to redraw them as you go.^"
      + "Just explore the world to fill it back in!^"
      + "Click the map in the bottom right to open it and see where you've been...^"
      + "...and use the stamps to remember landmarks!^"
      + "You can also middle click to place a waypoint.^"
      + "Good luck, engineer!";

    // French translation, vouvoiement, grand public, conventions ABR.
    const string FRENCH_INTRO =
        "Oh non !^Les rails sur votre carte ont été abîmés lorsque vous l'avez fait tomber dans l'eau, vous devrez les redessiner au fur et à mesure.^"
      + "Explorez simplement le monde pour la remplir à nouveau !^"
      + "Cliquez sur la carte en bas à droite pour l'ouvrir et voir où vous êtes passé...^"
      + "...et utilisez les timbres pour marquer les lieux importants !^"
      + "Vous pouvez aussi faire un clic-molette pour placer un repère.^"
      + "Bonne route, mécanicien !";

    const string ORIGINAL_STAFF = "New Staff Member Unlocked!";
    const string FRENCH_STAFF   = "Nouveau personnel débloqué !";

    // Resolved at runtime from --target=intro|staff (default intro).
    static string ORIGINAL = ORIGINAL_INTRO;
    static string TRANSLATION = FRENCH_INTRO;

    static int Main(string[] args)
    {
        if (args.Length < 3) {
            Console.Error.WriteLine("Usage: MainMapPatcher <MainMap.umap> <output dir> <ABumpyRide.usmap> [--target=intro|staff]");
            return 1;
        }
        string inUmap = args[0];
        string outDir = args[1];
        string usmapPath = args[2];
        string target = args.Skip(3).FirstOrDefault(a => a.StartsWith("--target="))?.Substring("--target=".Length) ?? "intro";
        switch (target) {
            case "intro": ORIGINAL = ORIGINAL_INTRO; TRANSLATION = FRENCH_INTRO; break;
            case "staff": ORIGINAL = ORIGINAL_STAFF; TRANSLATION = FRENCH_STAFF; break;
            default:
                Console.Error.WriteLine($"Unknown --target: {target}. Expected intro|staff.");
                return 1;
        }
        Console.Error.WriteLine($"Target: {target} | Source[{ORIGINAL.Length}]='{ORIGINAL.Substring(0, Math.Min(50, ORIGINAL.Length))}...'");
        string inUexp = Path.ChangeExtension(inUmap, "uexp");
        string inUbulk = Path.ChangeExtension(inUmap, "ubulk");
        if (!File.Exists(inUmap)) { Console.Error.WriteLine($"Missing {inUmap}"); return 1; }
        if (!File.Exists(inUexp)) { Console.Error.WriteLine($"Missing {inUexp}"); return 1; }

        Console.Error.WriteLine($"Input .umap : {inUmap} ({new FileInfo(inUmap).Length:N0} bytes)");
        Console.Error.WriteLine($"Input .uexp : {inUexp} ({new FileInfo(inUexp).Length:N0} bytes)");

        // --- Step 1: load .uasset header only ---
        var asset = new UAsset();
        asset.SetEngineVersion(EngineVersion.VER_UE5_3);
        asset.CustomSerializationFlags = CustomSerializationFlags.SkipParsingBytecode;
        // Load usmap mappings (required for unversioned property parsing in UE5).
        if (File.Exists(usmapPath)) {
            asset.Mappings = new Usmap(usmapPath);
            Console.Error.WriteLine($"Loaded usmap: {usmapPath}");
        } else {
            Console.Error.WriteLine($"WARNING: usmap not found at {usmapPath}");
        }

        byte[] uassetBytes = File.ReadAllBytes(inUmap);
        long uassetHeaderSize = uassetBytes.LongLength;
        var headerStream = new MemoryStream(uassetBytes, writable: false);
        var headerReader = new AssetBinaryReader(headerStream, false, asset);
        asset.Read(headerReader);
        Console.Error.WriteLine($"Loaded asset: {asset.Exports.Count} exports, header {uassetHeaderSize:N0} bytes");

        // --- Step 2: find ExecuteUbergraph_MainMap ---
        Export ubergraph = null;
        int ubergraphIdx = -1;
        for (int i = 0; i < asset.Exports.Count; i++) {
            var e = asset.Exports[i];
            if (e.ObjectName?.ToString() == "ExecuteUbergraph_MainMap") {
                ubergraph = e;
                ubergraphIdx = i;
                break;
            }
        }
        if (ubergraph == null) {
            Console.Error.WriteLine("ExecuteUbergraph_MainMap not found in exports.");
            return 2;
        }
        Console.Error.WriteLine($"Found ExecuteUbergraph_MainMap: SerialOffset=0x{ubergraph.SerialOffset:X} SerialSize=0x{ubergraph.SerialSize:X}");

        // --- Step 3: read just this export's body from .uexp ---
        // SerialOffset is an offset in the combined (.uasset+.uexp) stream.
        // Local offset in .uexp = SerialOffset - uassetHeaderSize.
        long localOffsetInUexp = ubergraph.SerialOffset - uassetHeaderSize;
        long oldSize = ubergraph.SerialSize;
        if (oldSize > int.MaxValue) {
            Console.Error.WriteLine("Export too large for int read.");
            return 3;
        }
        byte[] exportBytes = new byte[oldSize];
        using (var fs = File.OpenRead(inUexp)) {
            fs.Seek(localOffsetInUexp, SeekOrigin.Begin);
            int read = 0;
            while (read < exportBytes.Length) {
                int n = fs.Read(exportBytes, read, exportBytes.Length - read);
                if (n <= 0) break;
                read += n;
            }
            if (read != exportBytes.Length) {
                Console.Error.WriteLine($"Short read: got {read}, expected {exportBytes.Length}");
                return 3;
            }
        }
        Console.Error.WriteLine($"Read {exportBytes.Length:N0} bytes for export from offset 0x{localOffsetInUexp:X} in .uexp.");

        // --- Step 4: parse this export's body (now WITH bytecode parsing) ---
        // We needed SkipParsingBytecode for the header read earlier, but we now
        // want the bytecode tree built. Toggle the flag off.
        asset.CustomSerializationFlags &= ~CustomSerializationFlags.SkipParsingBytecode;

        using (var ms = new MemoryStream(exportBytes, writable: false))
        using (var reader = new AssetBinaryReader(ms, true, asset)) {
            try {
                ubergraph.Read(reader, (int)oldSize);
            } catch (Exception ex) {
                Console.Error.WriteLine($"Parse error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 4;
            }
        }
        var struc = (StructExport)ubergraph;
        Console.Error.WriteLine($"Parsed StructExport: {struc.ScriptBytecode?.Length ?? -1} top-level expressions, ScriptBytecodeSize={struc.ScriptBytecodeSize}");
        if (struc.ScriptBytecode == null) {
            Console.Error.WriteLine("ScriptBytecode is null (likely failed to parse). ScriptBytecodeRaw length: " + (struc.ScriptBytecodeRaw?.Length ?? -1));
            return 5;
        }

        // --- Step 5: KissE-style replacement (placeholder + branch detour) ---
        // The trick: leave the original bytecode positions untouched (so external
        // jumps still resolve correctly). Insert a 0-cost detour that jumps to a
        // copy of the modified statement appended at the end, which jumps back.
        bool dryRun = Environment.GetEnvironmentVariable("MMP_DRY") == "1";
        int replacements = 0;
        if (!dryRun) {
            // Find the top-level statement (entry in ScriptBytecode[]) that
            // contains the EX_StringConst we want to replace.
            int originalIndex = -1;
            for (int i = 0; i < struc.ScriptBytecode.Length; i++) {
                bool found = false;
                uint dummy = 0;
                struc.ScriptBytecode[i].Visit(asset, ref dummy, (e, off) => {
                    if (e is EX_StringConst sc && sc.Value == ORIGINAL) found = true;
                });
                if (found) { originalIndex = i; break; }
            }
            if (originalIndex < 0) {
                Console.Error.WriteLine("Intro string not found in any top-level statement.");
                return 6;
            }
            var topExpr = struc.ScriptBytecode[originalIndex];
            uint topExprSize = topExpr.GetSize(asset);
            Console.Error.WriteLine($"Top-level statement index: {originalIndex} | iCode size: {topExprSize}");

            // Generate placeholder string. Insertion at originalIndex will be:
            //   EX_Jump(MAGIC_TES)  (5 iCode bytes)
            //   EX_StringConst(placeholder)  (1 + len + 1 iCode bytes)
            // Total must equal topExprSize, so placeholder length = topExprSize - 7.
            const int EX_StringConst_overhead = 2;  // 1 op + 1 null
            const int EX_Jump_size = 5;              // 1 op + 4 offset
            int placeholderLen = (int)topExprSize - (EX_StringConst_overhead + EX_Jump_size);
            if (placeholderLen < 0) {
                Console.Error.WriteLine($"Top expr too small ({topExprSize}) for placeholder approach.");
                return 7;
            }
            string placeholderStr = (placeholderLen >= 7)
                ? new string('a', 7) + new string('1', placeholderLen - 7)
                : new string('a', placeholderLen);

            const uint MAGIC_TES = 200519;
            const uint MAGIC_FES = 60519;

            // Modify topExpr in place: replace EX_StringConst with EX_UnicodeStringConst.
            struc.ScriptBytecode[originalIndex] = ReplaceInExpr(topExpr, ref replacements);
            Console.Error.WriteLine($"Made {replacements} replacement(s).");
            if (replacements == 0) {
                Console.Error.WriteLine("Intro string not found in bytecode tree. Aborting.");
                return 6;
            }
            var modifiedTopExpr = struc.ScriptBytecode[originalIndex];

            // Build new ScriptBytecode array.
            var newCode = new System.Collections.Generic.List<KismetExpression>(struc.ScriptBytecode.Length + 3);
            for (int i = 0; i < struc.ScriptBytecode.Length; i++) {
                if (i == originalIndex) {
                    // Insert detour: jumpTES (5 bytes) then placeholder string (rest of original size).
                    newCode.Add(new EX_Jump { CodeOffset = MAGIC_TES });
                    newCode.Add(new EX_StringConst { Value = placeholderStr });
                } else {
                    newCode.Add(struc.ScriptBytecode[i]);
                }
            }
            // Append modified statement + jumpFES at the end.
            int modifiedExprIdx = newCode.Count;
            newCode.Add(modifiedTopExpr);
            int jumpFesIdx = newCode.Count;
            newCode.Add(new EX_Jump { CodeOffset = MAGIC_FES });

            // Compute iCode positions for each entry.
            uint walk = 0;
            uint[] positions = new uint[newCode.Count];
            for (int i = 0; i < newCode.Count; i++) {
                positions[i] = walk;
                newCode[i].Visit(asset, ref walk, (_, __) => { });
            }

            // jumpTES is at originalIndex (we inserted it there).
            // It must jump to the start of modifiedTopExpr (= positions[modifiedExprIdx]).
            ((EX_Jump)newCode[originalIndex]).CodeOffset = positions[modifiedExprIdx];
            // jumpFES is the last entry. It must jump back to right after the
            // original statement's spot, which is positions[originalIndex + 2]
            // (after jumpTES at originalIndex and placeholder at originalIndex+1).
            ((EX_Jump)newCode[jumpFesIdx]).CodeOffset = positions[originalIndex + 2];

            Console.Error.WriteLine($"jumpTES.CodeOffset = 0x{positions[modifiedExprIdx]:X}  (target: modified stmt at end)");
            Console.Error.WriteLine($"jumpFES.CodeOffset = 0x{positions[originalIndex + 2]:X}  (target: post-detour position)");
            Console.Error.WriteLine($"New bytecode total iCode size: {walk}");

            struc.ScriptBytecode = newCode.ToArray();
        } else {
            Console.Error.WriteLine("DRY RUN: skipping replacement.");
        }

        // --- Step 6: re-serialise the export to a new buffer ---
        byte[] newExportBytes;
        using (var ms2 = new MemoryStream())
        using (var writer = new AssetBinaryWriter(ms2, asset)) {
            ubergraph.Write(writer);
            writer.Flush();
            newExportBytes = ms2.ToArray();
        }
        // UAssetAPI's FunctionExport.Write has a "// TODO" — it does not
        // serialise some trailing fields that UE5 still expects. Detect and
        // append the missing trailer from the original export bytes.
        long writtenSize = newExportBytes.LongLength;
        // Determine how many bytes UAssetAPI consumed during Read by writing
        // the export with no modifications applied and comparing.
        // For now: assume the trailing N bytes that we don't write are at the
        // end of the original export. Find this offset via round-trip.
        // Optimisation: on first call we compute the gap by re-parsing+writing
        // an unmodified copy.
        int trailerLen = 0;
        {
            // Re-parse the original (clone) and write it untouched to measure.
            var freshAsset = new UAsset();
            freshAsset.SetEngineVersion(EngineVersion.VER_UE5_3);
            freshAsset.CustomSerializationFlags = CustomSerializationFlags.SkipParsingBytecode;
            if (File.Exists(usmapPath)) freshAsset.Mappings = new Usmap(usmapPath);
            using (var hs = new MemoryStream(uassetBytes, writable: false))
            using (var hr = new AssetBinaryReader(hs, false, freshAsset)) {
                freshAsset.Read(hr);
            }
            var freshUbergraph = (StructExport)freshAsset.Exports[ubergraphIdx];
            freshAsset.CustomSerializationFlags &= ~CustomSerializationFlags.SkipParsingBytecode;
            using (var ms3 = new MemoryStream(exportBytes, writable: false))
            using (var rr = new AssetBinaryReader(ms3, true, freshAsset)) {
                freshUbergraph.Read(rr, (int)oldSize);
            }
            byte[] roundTripBytes;
            using (var ms4 = new MemoryStream())
            using (var ww = new AssetBinaryWriter(ms4, freshAsset)) {
                freshUbergraph.Write(ww);
                ww.Flush();
                roundTripBytes = ms4.ToArray();
            }
            trailerLen = (int)(oldSize - roundTripBytes.LongLength);
            Console.Error.WriteLine($"Round-trip size: {roundTripBytes.LongLength}, original: {oldSize}, trailer gap: {trailerLen} bytes");
            if (trailerLen < 0) {
                Console.Error.WriteLine("WARN: round-trip produced MORE bytes than original — unexpected.");
                trailerLen = 0;
            }
        }
        if (trailerLen > 0) {
            byte[] trailer = new byte[trailerLen];
            Buffer.BlockCopy(exportBytes, exportBytes.Length - trailerLen, trailer, 0, trailerLen);
            byte[] withTrailer = new byte[newExportBytes.Length + trailerLen];
            Buffer.BlockCopy(newExportBytes, 0, withTrailer, 0, newExportBytes.Length);
            Buffer.BlockCopy(trailer, 0, withTrailer, newExportBytes.Length, trailerLen);
            newExportBytes = withTrailer;
            Console.Error.WriteLine($"Appended {trailerLen} trailer bytes from original.");
        }
        long newSize = newExportBytes.LongLength;
        long delta = newSize - oldSize;
        Console.Error.WriteLine($"New export size: {newSize:N0} bytes (delta {delta:+#;-#;0})");

        // --- Step 7: write output .uexp (copy + replace + copy) ---
        Directory.CreateDirectory(outDir);
        string outUmap = Path.Combine(outDir, Path.GetFileName(inUmap));
        string outUexp = Path.Combine(outDir, Path.GetFileName(inUexp));
        string outUbulk = Path.Combine(outDir, Path.GetFileName(inUbulk));

        using (var inFs = File.OpenRead(inUexp))
        using (var outFs = File.Create(outUexp)) {
            // copy [0 .. localOffsetInUexp)
            CopyRange(inFs, outFs, 0, localOffsetInUexp);
            // write new export bytes
            outFs.Write(newExportBytes, 0, newExportBytes.Length);
            // copy [localOffsetInUexp + oldSize .. end)
            CopyRange(inFs, outFs, localOffsetInUexp + oldSize, inFs.Length - (localOffsetInUexp + oldSize));
        }
        Console.Error.WriteLine($"Wrote {outUexp} ({new FileInfo(outUexp).Length:N0} bytes)");

        // --- Step 8: update .uasset header ---
        // 8a. ubergraph SerialSize
        ubergraph.SerialSize = newSize;
        // 8b. shift SerialOffset of all exports after ubergraph
        for (int i = 0; i < asset.Exports.Count; i++) {
            if (i == ubergraphIdx) continue;
            var e = asset.Exports[i];
            if (e.SerialOffset > ubergraph.SerialOffset) {
                e.SerialOffset += delta;
            }
        }
        // 8c. PreloadDependencyOffset, BulkDataStartOffset, etc., must be shifted
        //     since they typically sit after the export data.
        // We'll patch these directly via the WriteData mechanism if possible.

        // Try to write the new .uasset by re-serialising the header.
        try {
            using (var ms3 = new MemoryStream())
            using (var writer3 = new AssetBinaryWriter(ms3, asset)) {
                // Rebuild with current state. WriteData() rebuilds the entire asset
                // including .uexp in memory — too big for our case.
                // Instead, we patch the .uasset in-place by adjusting offset fields.
                // For UE5.3 the canonical approach is to call asset.Write() but it
                // requires WriteData() which builds the full thing. Skip for now and
                // do binary patches.
            }
        } catch { }

        // Binary patch of .uasset: shift fields stored as Int64 / Int32 offsets that
        // point into .uexp. The simplest is to call asset.Write(out _, out _) — but
        // it goes through WriteData() which rebuilds .uexp... not feasible here.
        //
        // Approach: patch SerialOffsets in the .uasset binary directly.
        // The export map is at NameOffset...ExportOffset...etc. We need to find each
        // export entry and rewrite its SerialOffset/SerialSize fields.
        //
        // But we don't have the offsets of each export entry in the .uasset binary
        // saved. Workaround: rewrite the export map header section using
        // AssetBinaryWriter on the existing uassetBytes after seeking to ExportOffset.

        // ExportOffset is internal — access via reflection.
        var fld = typeof(UAsset).GetField("ExportOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        long exportOffset = fld != null ? (int)fld.GetValue(asset) : 0;
        Console.Error.WriteLine($"Asset.ExportOffset = 0x{exportOffset:X}");
        if (exportOffset <= 0 || exportOffset >= uassetBytes.Length) {
            Console.Error.WriteLine("Cannot locate export map for binary patch.");
            return 7;
        }

        // Shift internal header offset fields (BulkDataStartOffset etc.) that
        // point past our export's SerialOffset. Returns list of (fieldName, oldVal, newVal, type).
        var bind2 = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        long ubergraphOrigOffset = ubergraph.SerialOffset - delta; // before mutation
        string[] offsetFieldNames = new[] {
            "BulkDataStartOffset",          // long
            "PreloadDependencyOffset",      // int
            "AssetRegistryDataOffset",      // int
            "WorldTileInfoDataOffset",      // int
            "DependsOffset",                // int
            "SectionSixOffset",             // int
        };
        var pendingPatches = new List<(string Name, long Old, long New, bool Is64)>();
        foreach (var name in offsetFieldNames) {
            var f2 = typeof(UAsset).GetField(name, bind2);
            if (f2 == null) continue;
            object cur = f2.GetValue(asset);
            if (cur is long lv && lv > ubergraphOrigOffset) {
                pendingPatches.Add((name, lv, lv + delta, true));
                f2.SetValue(asset, lv + delta);
                Console.Error.WriteLine($"  shifted {name}: 0x{lv:X} -> 0x{lv + delta:X}");
            } else if (cur is int iv && iv > ubergraphOrigOffset) {
                pendingPatches.Add((name, iv, iv + delta, false));
                f2.SetValue(asset, (int)(iv + delta));
                Console.Error.WriteLine($"  shifted {name}: 0x{iv:X} -> 0x{iv + delta:X}");
            }
        }

        // Build new .uasset binary: clone original and patch in-place.
        // 1. Patch offset fields (header) by binary search+replace.
        // 2. Re-serialize export map at ExportOffset with updated SerialOffsets.
        byte[] newUassetBytes = (byte[])uassetBytes.Clone();

        // Search range for header offset fields: first 4096 bytes (initial summary,
        // before NameMap/ExportMap which start much later). Tune up if needed.
        int headerSearchEnd = (int)Math.Min(4096, (long)exportOffset);
        foreach (var (name, oldVal, newVal, is64) in pendingPatches) {
            byte[] oldBytes = is64 ? BitConverter.GetBytes(oldVal) : BitConverter.GetBytes((int)oldVal);
            byte[] newBytes = is64 ? BitConverter.GetBytes(newVal) : BitConverter.GetBytes((int)newVal);
            int hits = 0;
            int firstHit = -1;
            for (int i = 0; i <= headerSearchEnd - oldBytes.Length; i++) {
                bool match = true;
                for (int j = 0; j < oldBytes.Length; j++) {
                    if (newUassetBytes[i + j] != oldBytes[j]) { match = false; break; }
                }
                if (match) {
                    hits++;
                    if (firstHit < 0) firstHit = i;
                    Buffer.BlockCopy(newBytes, 0, newUassetBytes, i, newBytes.Length);
                }
            }
            Console.Error.WriteLine($"  binary patched {name} at offset(s) 0x{firstHit:X} ({hits} occurrence(s) replaced).");
            if (hits == 0) {
                Console.Error.WriteLine($"  WARNING: {name} old value 0x{oldVal:X} not found in header search range.");
            } else if (hits > 1) {
                Console.Error.WriteLine($"  WARNING: {name} found {hits} times — may have over-patched.");
            }
        }

        // Surgical patch: only update SerialSize+SerialOffset for affected
        // exports. Avoid rewriting entire export entries because some fields
        // (e.g. ClassIndex) may not be populated in memory on assets loaded
        // with loadUEXP=false / SkipParsingBytecode.
        long entrySize = Export.GetExportMapEntrySize(asset);
        Console.Error.WriteLine($"Export map entry size: {entrySize} bytes");
        // Position of SerialSize within entry (UE5.3, 64-bit serial sizes):
        //   ClassIndex(4) + SuperIndex(4) + TemplateIndex(4) + OuterIndex(4)
        //   + ObjectName.NameIndex(4) + ObjectName.Number(4)
        //   + ObjectFlags(4) = 28
        const int SerialSizeOffsetInEntry = 28;   // long (8 bytes)
        const int SerialOffsetOffsetInEntry = 36; // long (8 bytes)

        int patchedCount = 0;
        for (int i = 0; i < asset.Exports.Count; i++) {
            var e = asset.Exports[i];
            // Compute original SerialOffset (before our shift)
            long origSerialOffset = (i == ubergraphIdx)
                ? e.SerialOffset                        // ubergraph: only SerialSize changes
                : (e.SerialOffset > ubergraph.SerialOffset ? e.SerialOffset - delta : e.SerialOffset);
            bool needsPatch = (i == ubergraphIdx) || (e.SerialOffset != origSerialOffset);
            if (!needsPatch) continue;

            long entryPos = exportOffset + (long)i * entrySize;
            // Write SerialSize
            byte[] sizeBytes = BitConverter.GetBytes((long)e.SerialSize);
            Buffer.BlockCopy(sizeBytes, 0, newUassetBytes, (int)(entryPos + SerialSizeOffsetInEntry), 8);
            // Write SerialOffset
            byte[] offBytes = BitConverter.GetBytes((long)e.SerialOffset);
            Buffer.BlockCopy(offBytes, 0, newUassetBytes, (int)(entryPos + SerialOffsetOffsetInEntry), 8);
            patchedCount++;
        }
        Console.Error.WriteLine($"Surgical export map patches: {patchedCount}");

        File.WriteAllBytes(outUmap, newUassetBytes);
        Console.Error.WriteLine($"Wrote {outUmap} ({newUassetBytes.LongLength:N0} bytes)");

        // Copy ubulk if exists
        if (File.Exists(inUbulk)) {
            File.Copy(inUbulk, outUbulk, overwrite: true);
            Console.Error.WriteLine($"Copied {outUbulk}");
        }

        Console.Error.WriteLine("DONE.");
        return 0;
    }

    static void CopyRange(Stream src, Stream dst, long offset, long length) {
        src.Seek(offset, SeekOrigin.Begin);
        byte[] buf = new byte[64 * 1024];
        long remaining = length;
        while (remaining > 0) {
            int toRead = (int)Math.Min(buf.Length, remaining);
            int n = src.Read(buf, 0, toRead);
            if (n <= 0) throw new IOException($"Short read at {offset}, remaining {remaining}");
            dst.Write(buf, 0, n);
            remaining -= n;
        }
    }

    static uint Shift(uint val, uint threshold, int delta, ref int adjusted) {
        if (val >= threshold) { adjusted++; return (uint)((int)val + delta); }
        return val;
    }

    static void AdjustJumps(KismetExpression e, uint threshold, int delta, ref int adjusted) {
        if (e == null) return;
        // Adjust the offset fields for jump-like opcodes.
        if (e is EX_Jump j) {
            j.CodeOffset = Shift(j.CodeOffset, threshold, delta, ref adjusted);
        } else if (e is EX_JumpIfNot jin) {
            jin.CodeOffset = Shift(jin.CodeOffset, threshold, delta, ref adjusted);
        } else if (e is EX_Skip sk) {
            sk.CodeOffset = Shift(sk.CodeOffset, threshold, delta, ref adjusted);
        } else if (e is EX_PushExecutionFlow pef) {
            pef.PushingAddress = Shift(pef.PushingAddress, threshold, delta, ref adjusted);
        } else if (e is EX_Context ctx) {
            // EX_ClassContext and EX_Context_FailSilent inherit from EX_Context.
            ctx.Offset = Shift(ctx.Offset, threshold, delta, ref adjusted);
        } else if (e is EX_SwitchValue sw) {
            sw.EndGotoOffset = Shift(sw.EndGotoOffset, threshold, delta, ref adjusted);
            // Cases is FKismetSwitchCase[] (struct), reflection on KismetExpression
            // won't recurse into them. Handle children + NextOffset explicitly.
            if (sw.Cases != null) {
                for (int i = 0; i < sw.Cases.Length; i++) {
                    var c = sw.Cases[i];
                    AdjustJumps(c.CaseIndexValueTerm, threshold, delta, ref adjusted);
                    c.NextOffset = Shift(c.NextOffset, threshold, delta, ref adjusted);
                    AdjustJumps(c.CaseTerm, threshold, delta, ref adjusted);
                    sw.Cases[i] = c; // struct: must write back
                }
            }
            AdjustJumps(sw.IndexTerm, threshold, delta, ref adjusted);
            AdjustJumps(sw.DefaultTerm, threshold, delta, ref adjusted);
            return; // Already recursed; skip generic reflection below.
        }
        // Recurse into nested expressions.
        var t = e.GetType();
        var bind = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
        foreach (var f in t.GetFields(bind)) {
            if (typeof(KismetExpression).IsAssignableFrom(f.FieldType)) {
                AdjustJumps((KismetExpression)f.GetValue(e), threshold, delta, ref adjusted);
            } else if (f.FieldType == typeof(KismetExpression[])) {
                var arr = (KismetExpression[])f.GetValue(e);
                if (arr != null) foreach (var x in arr) AdjustJumps(x, threshold, delta, ref adjusted);
            }
        }
        foreach (var p in t.GetProperties(bind)) {
            if (!p.CanRead) continue;
            if (typeof(KismetExpression).IsAssignableFrom(p.PropertyType)) {
                AdjustJumps((KismetExpression)p.GetValue(e), threshold, delta, ref adjusted);
            } else if (p.PropertyType == typeof(KismetExpression[])) {
                var arr = (KismetExpression[])p.GetValue(e);
                if (arr != null) foreach (var x in arr) AdjustJumps(x, threshold, delta, ref adjusted);
            }
        }
    }

    static KismetExpression ReplaceInExpr(KismetExpression e, ref int replacements) {
        if (e == null) return null;
        if (e is EX_StringConst sc && sc.Value == ORIGINAL) {
            Console.Error.WriteLine($"  Found nested EX_StringConst matching target. Replacing.");
            replacements++;
            return new EX_UnicodeStringConst { Value = TRANSLATION };
        }
        // Special case: EX_TextConst wraps an FScriptText with nested KismetExpression
        // fields (LocalizedSource/Key/Namespace, InvariantLiteralString, LiteralString,
        // StringTableId/Key). Reflection doesn't recurse into FScriptText (it's not a
        // KismetExpression), so handle each nested expression field explicitly.
        if (e is EX_TextConst tc && tc.Value != null) {
            var st = tc.Value;
            if (st.LocalizedSource != null) {
                var nc = ReplaceInExpr(st.LocalizedSource, ref replacements);
                if (!ReferenceEquals(st.LocalizedSource, nc)) st.LocalizedSource = nc;
            }
            if (st.LocalizedKey != null) {
                var nc = ReplaceInExpr(st.LocalizedKey, ref replacements);
                if (!ReferenceEquals(st.LocalizedKey, nc)) st.LocalizedKey = nc;
            }
            if (st.LocalizedNamespace != null) {
                var nc = ReplaceInExpr(st.LocalizedNamespace, ref replacements);
                if (!ReferenceEquals(st.LocalizedNamespace, nc)) st.LocalizedNamespace = nc;
            }
            if (st.InvariantLiteralString != null) {
                var nc = ReplaceInExpr(st.InvariantLiteralString, ref replacements);
                if (!ReferenceEquals(st.InvariantLiteralString, nc)) st.InvariantLiteralString = nc;
            }
            if (st.LiteralString != null) {
                var nc = ReplaceInExpr(st.LiteralString, ref replacements);
                if (!ReferenceEquals(st.LiteralString, nc)) st.LiteralString = nc;
            }
            if (st.StringTableId != null) {
                var nc = ReplaceInExpr(st.StringTableId, ref replacements);
                if (!ReferenceEquals(st.StringTableId, nc)) st.StringTableId = nc;
            }
            if (st.StringTableKey != null) {
                var nc = ReplaceInExpr(st.StringTableKey, ref replacements);
                if (!ReferenceEquals(st.StringTableKey, nc)) st.StringTableKey = nc;
            }
            return e;
        }
        // Recurse via reflection through all fields/properties holding child
        // KismetExpression(s).
        var t = e.GetType();
        var bind = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
        foreach (var f in t.GetFields(bind)) {
            if (typeof(KismetExpression).IsAssignableFrom(f.FieldType)) {
                var child = (KismetExpression)f.GetValue(e);
                var newChild = ReplaceInExpr(child, ref replacements);
                if (!ReferenceEquals(child, newChild)) f.SetValue(e, newChild);
            } else if (f.FieldType == typeof(KismetExpression[])) {
                var arr = (KismetExpression[])f.GetValue(e);
                if (arr != null) {
                    for (int i = 0; i < arr.Length; i++) {
                        arr[i] = ReplaceInExpr(arr[i], ref replacements);
                    }
                }
            }
        }
        foreach (var p in t.GetProperties(bind)) {
            if (!p.CanRead) continue;
            if (typeof(KismetExpression).IsAssignableFrom(p.PropertyType)) {
                if (!p.CanWrite) continue;
                var child = (KismetExpression)p.GetValue(e);
                var newChild = ReplaceInExpr(child, ref replacements);
                if (!ReferenceEquals(child, newChild)) p.SetValue(e, newChild);
            } else if (p.PropertyType == typeof(KismetExpression[])) {
                var arr = (KismetExpression[])p.GetValue(e);
                if (arr != null) {
                    for (int i = 0; i < arr.Length; i++) {
                        arr[i] = ReplaceInExpr(arr[i], ref replacements);
                    }
                }
            }
        }
        return e;
    }
}
