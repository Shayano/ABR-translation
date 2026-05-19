// Generalized BP string patcher inspired by MainMapPatcher.
//
// Patches one or more EX_StringConst occurrences inside a target export's
// bytecode by inserting placeholder+branch detours (KissE-style) for each
// string. The original statement positions remain intact so external EX_Jumps
// continue to resolve correctly.
//
// Usage:
//   BPStringPatcher <input.uasset_or_umap> <output_dir> <usmap> --export=<name> --strings-json=<path>
//
// strings.json format:
//   [
//     {"Original": "ITEMS MISSING", "Translation": "OBJETS MANQUANTS"},
//     {"Original": "It looks like...", "Translation": "Vous avez..."}
//   ]
//
// Designed for typical BP assets (.uexp < 100 MB). For very large .uexp files
// (e.g. MainMap.uexp ~2 GB), keep using MainMapPatcher which avoids loading
// the full file into memory.
//
// The .uexp companion is auto-detected next to the .uasset/.umap.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            Console.Error.WriteLine("Usage: BPStringPatcher <input.uasset> <output_dir> <usmap> --export=<name> --strings-json=<path>");
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

        var stringEntries = JsonSerializer.Deserialize<List<StringEntry>>(
            File.ReadAllText(stringsJsonPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (stringEntries == null || stringEntries.Count == 0) {
            Console.Error.WriteLine("strings JSON is empty.");
            return 1;
        }
        var validEntries = stringEntries.Where(e => !string.IsNullOrEmpty(e.Translation)).ToList();
        Console.Error.WriteLine($"Loaded {stringEntries.Count} strings from JSON ({validEntries.Count} with non-empty translation).");
        if (validEntries.Count == 0) {
            Console.Error.WriteLine("No translations to apply.");
            return 1;
        }

        Console.Error.WriteLine($"Input asset : {inAsset} ({new FileInfo(inAsset).Length:N0} bytes)");
        Console.Error.WriteLine($"Input .uexp : {inUexp} ({new FileInfo(inUexp).Length:N0} bytes)");
        Console.Error.WriteLine($"Target export: {exportName}");

        var asset = new UAsset();
        asset.SetEngineVersion(EngineVersion.VER_UE5_3);
        asset.CustomSerializationFlags = CustomSerializationFlags.SkipParsingBytecode;
        if (File.Exists(usmapPath)) {
            asset.Mappings = new Usmap(usmapPath);
            Console.Error.WriteLine($"Loaded usmap: {usmapPath}");
        } else {
            Console.Error.WriteLine($"WARNING: usmap not found at {usmapPath}");
        }

        byte[] uassetBytes = File.ReadAllBytes(inAsset);
        long uassetHeaderSize = uassetBytes.LongLength;
        var headerStream = new MemoryStream(uassetBytes, writable: false);
        var headerReader = new AssetBinaryReader(headerStream, false, asset);
        asset.Read(headerReader);
        Console.Error.WriteLine($"Loaded asset: {asset.Exports.Count} exports, header {uassetHeaderSize:N0} bytes");

        Export targetExport = null;
        int targetIdx = -1;
        for (int i = 0; i < asset.Exports.Count; i++) {
            var e = asset.Exports[i];
            if (e.ObjectName?.ToString() == exportName) {
                targetExport = e;
                targetIdx = i;
                break;
            }
        }
        if (targetExport == null) {
            Console.Error.WriteLine($"Export '{exportName}' not found in asset. Available exports with ScriptBytecode-like names:");
            foreach (var e in asset.Exports.Where(x => x.ObjectName?.ToString().StartsWith("ExecuteUbergraph") ?? false)) {
                Console.Error.WriteLine($"  - {e.ObjectName}");
            }
            return 2;
        }
        Console.Error.WriteLine($"Found '{exportName}': SerialOffset=0x{targetExport.SerialOffset:X} SerialSize=0x{targetExport.SerialSize:X}");

        long localOffsetInUexp = targetExport.SerialOffset - uassetHeaderSize;
        long oldSize = targetExport.SerialSize;
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

        asset.CustomSerializationFlags &= ~CustomSerializationFlags.SkipParsingBytecode;
        using (var ms = new MemoryStream(exportBytes, writable: false))
        using (var reader = new AssetBinaryReader(ms, true, asset)) {
            try {
                targetExport.Read(reader, (int)oldSize);
            } catch (Exception ex) {
                Console.Error.WriteLine($"Parse error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 4;
            }
        }
        var struc = (StructExport)targetExport;
        Console.Error.WriteLine($"Parsed StructExport: {struc.ScriptBytecode?.Length ?? -1} top-level expressions, ScriptBytecodeSize={struc.ScriptBytecodeSize}");
        if (struc.ScriptBytecode == null) {
            Console.Error.WriteLine("ScriptBytecode is null (likely failed to parse). ScriptBytecodeRaw length: " + (struc.ScriptBytecodeRaw?.Length ?? -1));
            return 5;
        }

        const uint MAGIC_TES = 200519;
        const uint MAGIC_FES = 60519;
        const int EX_StringConst_overhead = 2;
        const int EX_Jump_size = 5;

        int totalReplacements = 0;
        int skippedCount = 0;
        foreach (var entry in validEntries) {
            string original = entry.Original;
            string translation = entry.Translation;
            string preview = original.Length > 50 ? original.Substring(0, 50) + "..." : original;

            int originalIndex = -1;
            for (int i = 0; i < struc.ScriptBytecode.Length; i++) {
                bool found = false;
                _searchOriginal = original;
                _searchDummy = 0;
                struc.ScriptBytecode[i].Visit(asset, ref _searchDummy, (e, off) => {
                    if (e is EX_StringConst sc && sc.Value == _searchOriginal) found = true;
                });
                if (found) { originalIndex = i; break; }
            }
            if (originalIndex < 0) {
                Console.Error.WriteLine($"  [skip] '{preview}' not found in {exportName} bytecode.");
                skippedCount++;
                continue;
            }
            var topExpr = struc.ScriptBytecode[originalIndex];
            uint topExprSize = topExpr.GetSize(asset);

            int placeholderLen = (int)topExprSize - (EX_StringConst_overhead + EX_Jump_size);
            if (placeholderLen < 0) {
                Console.Error.WriteLine($"  [skip] '{preview}' top-level statement too small ({topExprSize}) for placeholder approach.");
                skippedCount++;
                continue;
            }
            string placeholderStr = (placeholderLen >= 7)
                ? new string('a', 7) + new string('1', placeholderLen - 7)
                : new string('a', placeholderLen);

            int reps = 0;
            _replaceOriginal = original;
            _replaceTranslation = translation;
            struc.ScriptBytecode[originalIndex] = ReplaceInExpr(topExpr, ref reps);
            if (reps == 0) {
                Console.Error.WriteLine($"  [skip] '{preview}' was found by Visit but ReplaceInExpr made 0 replacements.");
                skippedCount++;
                continue;
            }
            var modifiedTopExpr = struc.ScriptBytecode[originalIndex];

            var newCode = new List<KismetExpression>(struc.ScriptBytecode.Length + 3);
            for (int i = 0; i < struc.ScriptBytecode.Length; i++) {
                if (i == originalIndex) {
                    newCode.Add(new EX_Jump { CodeOffset = MAGIC_TES });
                    newCode.Add(new EX_StringConst { Value = placeholderStr });
                } else {
                    newCode.Add(struc.ScriptBytecode[i]);
                }
            }
            int modifiedExprIdx = newCode.Count;
            newCode.Add(modifiedTopExpr);
            int jumpFesIdx = newCode.Count;
            newCode.Add(new EX_Jump { CodeOffset = MAGIC_FES });

            uint walk = 0;
            uint[] positions = new uint[newCode.Count];
            for (int i = 0; i < newCode.Count; i++) {
                positions[i] = walk;
                newCode[i].Visit(asset, ref walk, (_, __) => { });
            }
            ((EX_Jump)newCode[originalIndex]).CodeOffset = positions[modifiedExprIdx];
            ((EX_Jump)newCode[jumpFesIdx]).CodeOffset = positions[originalIndex + 2];

            struc.ScriptBytecode = newCode.ToArray();
            totalReplacements++;
            Console.Error.WriteLine($"  [OK]   '{preview}' patched at top-level idx {originalIndex} (size {topExprSize}, placeholder len {placeholderLen}).");
        }

        Console.Error.WriteLine($"Total : {totalReplacements} patches applied, {skippedCount} skipped.");
        if (totalReplacements == 0) {
            Console.Error.WriteLine("No replacement applied. Aborting.");
            return 6;
        }

        byte[] newExportBytes;
        using (var ms2 = new MemoryStream())
        using (var writer = new AssetBinaryWriter(ms2, asset)) {
            targetExport.Write(writer);
            writer.Flush();
            newExportBytes = ms2.ToArray();
        }
        int trailerLen = 0;
        {
            var freshAsset = new UAsset();
            freshAsset.SetEngineVersion(EngineVersion.VER_UE5_3);
            freshAsset.CustomSerializationFlags = CustomSerializationFlags.SkipParsingBytecode;
            if (File.Exists(usmapPath)) freshAsset.Mappings = new Usmap(usmapPath);
            using (var hs = new MemoryStream(uassetBytes, writable: false))
            using (var hr = new AssetBinaryReader(hs, false, freshAsset)) {
                freshAsset.Read(hr);
            }
            var freshTarget = (StructExport)freshAsset.Exports[targetIdx];
            freshAsset.CustomSerializationFlags &= ~CustomSerializationFlags.SkipParsingBytecode;
            using (var ms3 = new MemoryStream(exportBytes, writable: false))
            using (var rr = new AssetBinaryReader(ms3, true, freshAsset)) {
                freshTarget.Read(rr, (int)oldSize);
            }
            byte[] roundTripBytes;
            using (var ms4 = new MemoryStream())
            using (var ww = new AssetBinaryWriter(ms4, freshAsset)) {
                freshTarget.Write(ww);
                ww.Flush();
                roundTripBytes = ms4.ToArray();
            }
            trailerLen = (int)(oldSize - roundTripBytes.LongLength);
            Console.Error.WriteLine($"Round-trip size: {roundTripBytes.LongLength}, original: {oldSize}, trailer gap: {trailerLen} bytes");
            if (trailerLen < 0) {
                Console.Error.WriteLine("WARN: round-trip produced MORE bytes than original.");
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

        Directory.CreateDirectory(outDir);
        string outAsset = Path.Combine(outDir, Path.GetFileName(inAsset));
        string outUexp = Path.Combine(outDir, Path.GetFileName(inUexp));
        string outUbulk = Path.Combine(outDir, Path.GetFileName(inUbulk));

        using (var inFs = File.OpenRead(inUexp))
        using (var outFs = File.Create(outUexp)) {
            CopyRange(inFs, outFs, 0, localOffsetInUexp);
            outFs.Write(newExportBytes, 0, newExportBytes.Length);
            CopyRange(inFs, outFs, localOffsetInUexp + oldSize, inFs.Length - (localOffsetInUexp + oldSize));
        }
        Console.Error.WriteLine($"Wrote {outUexp} ({new FileInfo(outUexp).Length:N0} bytes)");

        targetExport.SerialSize = newSize;
        for (int i = 0; i < asset.Exports.Count; i++) {
            if (i == targetIdx) continue;
            var e = asset.Exports[i];
            if (e.SerialOffset > targetExport.SerialOffset) {
                e.SerialOffset += delta;
            }
        }

        var fld = typeof(UAsset).GetField("ExportOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        long exportOffset = fld != null ? (int)fld.GetValue(asset) : 0;
        Console.Error.WriteLine($"Asset.ExportOffset = 0x{exportOffset:X}");
        if (exportOffset <= 0 || exportOffset >= uassetBytes.Length) {
            Console.Error.WriteLine("Cannot locate export map for binary patch.");
            return 7;
        }

        var bind2 = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        long targetOrigOffset = targetExport.SerialOffset - delta;
        string[] offsetFieldNames = new[] {
            "BulkDataStartOffset",
            "PreloadDependencyOffset",
            "AssetRegistryDataOffset",
            "WorldTileInfoDataOffset",
            "DependsOffset",
            "SectionSixOffset",
        };
        var pendingPatches = new List<(string Name, long Old, long New, bool Is64)>();
        foreach (var name in offsetFieldNames) {
            var f2 = typeof(UAsset).GetField(name, bind2);
            if (f2 == null) continue;
            object cur = f2.GetValue(asset);
            if (cur is long lv && lv > targetOrigOffset) {
                pendingPatches.Add((name, lv, lv + delta, true));
                f2.SetValue(asset, lv + delta);
                Console.Error.WriteLine($"  shifted {name}: 0x{lv:X} -> 0x{lv + delta:X}");
            } else if (cur is int iv && iv > targetOrigOffset) {
                pendingPatches.Add((name, iv, iv + delta, false));
                f2.SetValue(asset, (int)(iv + delta));
                Console.Error.WriteLine($"  shifted {name}: 0x{iv:X} -> 0x{iv + delta:X}");
            }
        }

        byte[] newUassetBytes = (byte[])uassetBytes.Clone();
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
        }

        long entrySize = Export.GetExportMapEntrySize(asset);
        Console.Error.WriteLine($"Export map entry size: {entrySize} bytes");
        const int SerialSizeOffsetInEntry = 28;
        const int SerialOffsetOffsetInEntry = 36;

        int patchedCount = 0;
        for (int i = 0; i < asset.Exports.Count; i++) {
            var e = asset.Exports[i];
            long origSerialOffset = (i == targetIdx)
                ? e.SerialOffset
                : (e.SerialOffset > targetExport.SerialOffset ? e.SerialOffset - delta : e.SerialOffset);
            bool needsPatch = (i == targetIdx) || (e.SerialOffset != origSerialOffset);
            if (!needsPatch) continue;

            long entryPos = exportOffset + (long)i * entrySize;
            byte[] sizeBytes = BitConverter.GetBytes((long)e.SerialSize);
            Buffer.BlockCopy(sizeBytes, 0, newUassetBytes, (int)(entryPos + SerialSizeOffsetInEntry), 8);
            byte[] offBytes = BitConverter.GetBytes((long)e.SerialOffset);
            Buffer.BlockCopy(offBytes, 0, newUassetBytes, (int)(entryPos + SerialOffsetOffsetInEntry), 8);
            patchedCount++;
        }
        Console.Error.WriteLine($"Surgical export map patches: {patchedCount}");

        File.WriteAllBytes(outAsset, newUassetBytes);
        Console.Error.WriteLine($"Wrote {outAsset} ({newUassetBytes.LongLength:N0} bytes)");

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

    static string _replaceOriginal;
    static string _replaceTranslation;
    static string _searchOriginal;
    static uint _searchDummy;

    static KismetExpression ReplaceInExpr(KismetExpression e, ref int replacements) {
        if (e == null) return null;
        if (e is EX_StringConst sc && sc.Value == _replaceOriginal) {
            replacements++;
            return new EX_UnicodeStringConst { Value = _replaceTranslation };
        }
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
