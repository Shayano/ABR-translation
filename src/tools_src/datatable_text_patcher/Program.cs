using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 1) { PrintUsage(); return 2; }

        try
        {
            return args[0] switch
            {
                "--diag"    => RunDiag(args.Skip(1).ToArray()),
                "--extract" => RunExtract(args.Skip(1).ToArray()),
                "--inject"  => RunInject(args.Skip(1).ToArray()),
                "--scan"    => RunScan(args.Skip(1).ToArray()),
                "--scan-all"=> RunScanAll(args.Skip(1).ToArray()),
                "--extract-enum" => RunExtractEnum(args.Skip(1).ToArray()),
                "--inject-enum"  => RunInjectEnum(args.Skip(1).ToArray()),
                "--inject-all"   => RunInjectAll(args.Skip(1).ToArray()),
                "--roundtrip"    => RunRoundtrip(args.Skip(1).ToArray()),
                _ => PrintUsage()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"  Inner: {ex.InnerException?.Message}");
            Console.WriteLine($"  Stack: {ex.StackTrace}");
            return 1;
        }
    }

    static int PrintUsage()
    {
        Console.WriteLine("usage:");
        Console.WriteLine("  datatable_text_patcher --diag    <uasset> <usmap>                                  # dump struct (premieres rows)");
        Console.WriteLine("  datatable_text_patcher --extract <uasset> <usmap> <out.json>                       # FText rows -> JSON");
        Console.WriteLine("  datatable_text_patcher --inject  <uasset_in> <usmap> <trad.json> <uasset_out>      # patch FText");
        Console.WriteLine("  datatable_text_patcher --scan    <dir> <usmap> [out.json]                         # liste DataTables avec TextProperty");
        Console.WriteLine("  datatable_text_patcher --scan-all <dir> <usmap> [out.json]                        # liste TOUTES les TextProperty (pas juste DataTables)");
        Console.WriteLine("  datatable_text_patcher --extract-enum <uasset> <usmap> <out.json>                 # extract recursif TextProperty (Enum/Widget/DataTable)");
        Console.WriteLine("  datatable_text_patcher --inject-enum  <uasset_in> <usmap> <trad.json> <uasset_out># inject par source EN match");
        Console.WriteLine("  datatable_text_patcher --inject-all   <uasset_in> <usmap> <trad.json> <uasset_out># inject TextProperty + bytecode EX_TextConst");
        return 2;
    }

    static int RunRoundtrip(string[] args)
    {
        if (args.Length < 3) return PrintUsage();
        var asset = Load(args[0], args[1]);
        asset.Write(args[2]);
        var inSize  = new FileInfo(args[0]).Length;
        var outSize = new FileInfo(args[2]).Length;
        Console.WriteLine($"  uasset: {inSize} -> {outSize} (delta {outSize-inSize:+#;-#;0})");
        var inExp = args[0].Replace(".uasset", ".uexp");
        var outExp = args[2].Replace(".uasset", ".uexp");
        if (File.Exists(inExp) && File.Exists(outExp))
        {
            var i = new FileInfo(inExp).Length; var o = new FileInfo(outExp).Length;
            Console.WriteLine($"  uexp:   {i} -> {o} (delta {o-i:+#;-#;0})");
        }
        return 0;
    }

    static int RunInjectAll(string[] args)
    {
        if (args.Length < 4) return PrintUsage();
        var asset = Load(args[0], args[1]);
        var json = File.ReadAllText(args[2]);
        var entries = JsonSerializer.Deserialize<List<EnumEntry>>(json) ?? new();
        var dict = new Dictionary<string, string>();
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.Translation) || string.IsNullOrEmpty(e.Source)) continue;
            dict[e.Source] = e.Translation;
        }
        Console.WriteLine($"  Loaded {dict.Count} non-empty translations");

        // 1) TextPropertyData (defaults, datatables, enums)
        var counterProp = new Counter();
        var seen1 = new HashSet<object>();
        foreach (var export in asset.Exports)
        {
            if (export is NormalExport nx)
            {
                foreach (var prop in nx.Data)
                    PatchTextWalk(prop, dict, counterProp, seen1, 0);
            }
        }

        // 2) EX_TextConst dans le bytecode UFunction
        var counterBc = new Counter();
        var seenBc = new HashSet<KismetExpression>();
        int withBytecode = 0, totalExports = 0;
        foreach (var export in asset.Exports)
        {
            totalExports++;
            KismetExpression[] bytecode = null;
            string howFound = null;
            if (export is FunctionExport fe) { bytecode = fe.ScriptBytecode; howFound = "FunctionExport.ScriptBytecode"; }
            else if (export is StructExport se) { bytecode = se.ScriptBytecode; howFound = "StructExport.ScriptBytecode"; }
            if (bytecode == null || bytecode.Length == 0) continue;
            withBytecode++;
            for (int i = 0; i < bytecode.Length; i++)
            {
                int idx = i;
                ExprSetter setter = ne => bytecode[idx] = ne;
                PatchBytecodeWalk(bytecode[idx], setter, dict, counterBc, seenBc);
            }
        }

        Console.WriteLine($"  Patched: {counterProp.Value} TextProperty + {counterBc.Value} EX_TextConst (total {counterProp.Value + counterBc.Value})");

        asset.Write(args[3]);
        var size = new FileInfo(args[3]).Length;
        Console.WriteLine($"  Wrote {args[3]} ({size} bytes)");
        return 0;
    }

    static FString MakeFStringForTrad(string s)
    {
        bool needsUnicode = s.Any(c => c > 127);
        return new FString(s, needsUnicode ? System.Text.Encoding.Unicode : System.Text.Encoding.ASCII);
    }

    static KismetExpression MakeStringExpr(string s)
    {
        if (s.All(c => c <= 127)) return new EX_StringConst { Value = s };
        return new EX_UnicodeStringConst { Value = s };
    }

    static string ExtractStringConst(KismetExpression expr)
    {
        if (expr == null) return null;
        if (expr is EX_StringConst sc) return sc.Value;
        if (expr is EX_UnicodeStringConst usc) return usc.Value;
        return null;
    }

    // Setter pour swap d'expression dans son slot parent. Si null, l'expr ne peut pas etre remplacee
    // (cas des roots de bytecode array, qu'on gere a part).
    delegate void ExprSetter(KismetExpression newExpr);

    static void PatchBytecodeWalk(KismetExpression expr, ExprSetter setter, Dictionary<string, string> dict, Counter counter, HashSet<KismetExpression> seen)
    {
        if (expr == null) return;
        if (!seen.Add(expr)) return;

        if (expr is EX_TextConst tc && tc.Value != null)
        {
            string source = null;
            bool isLocalized = tc.Value.TextLiteralType == EBlueprintTextLiteralType.LocalizedText;
            bool isInvariant = tc.Value.TextLiteralType == EBlueprintTextLiteralType.InvariantText;
            if (isLocalized) source = ExtractStringConst(tc.Value.LocalizedSource);
            else if (isInvariant) source = ExtractStringConst(tc.Value.InvariantLiteralString);
            if (!string.IsNullOrEmpty(source) && dict.TryGetValue(source, out string trad))
            {
                if (isLocalized) tc.Value.LocalizedSource = MakeStringExpr(trad);
                else tc.Value.InvariantLiteralString = MakeStringExpr(trad);
                counter.Value++;
            }
        }
        else if (expr is EX_StringConst sc && !string.IsNullOrEmpty(sc.Value))
        {
            if (dict.TryGetValue(sc.Value, out string trad))
            {
                if (trad.All(c => c <= 127)) sc.Value = trad;
                else if (setter != null) setter(new EX_UnicodeStringConst { Value = trad });
                else sc.Value = trad; // fallback
                counter.Value++;
            }
        }
        else if (expr is EX_UnicodeStringConst usc && !string.IsNullOrEmpty(usc.Value))
        {
            if (dict.TryGetValue(usc.Value, out string trad))
            {
                if (trad.All(c => c <= 127) && setter != null) setter(new EX_StringConst { Value = trad });
                else usc.Value = trad;
                counter.Value++;
            }
        }

        // Recursion via reflection: pour chaque field/prop, fournir un setter swap-capable
        var t = expr.GetType();
        foreach (var f in t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            object v;
            try { v = f.GetValue(expr); } catch { continue; }
            VisitSlot(v, ne => f.SetValue(expr, ne), dict, counter, seen);
        }
        foreach (var p in t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length != 0) continue;
            if (!p.CanWrite) continue;
            object v;
            try { v = p.GetValue(expr); } catch { continue; }
            VisitSlot(v, ne => { try { p.SetValue(expr, ne); } catch { } }, dict, counter, seen);
        }
    }

    static void VisitSlot(object v, Action<KismetExpression> slotSetter, Dictionary<string, string> dict, Counter counter, HashSet<KismetExpression> seen)
    {
        if (v == null) return;
        if (v is string) return;
        if (v.GetType().IsPrimitive) return;
        if (v is KismetExpression kx)
        {
            ExprSetter setter = ne => slotSetter(ne);
            PatchBytecodeWalk(kx, setter, dict, counter, seen);
            return;
        }
        if (v is KismetExpression[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                int idx = i; // capture
                ExprSetter setter = ne => arr[idx] = ne;
                PatchBytecodeWalk(arr[idx], setter, dict, counter, seen);
            }
            return;
        }
        if (v is System.Collections.IEnumerable en)
        {
            foreach (var item in en) if (item is KismetExpression kx2)
            {
                // Pas de setter pour items dans IEnumerable generique (IList aurait pu, mais skip pour simplicite)
                PatchBytecodeWalk(kx2, null, dict, counter, seen);
            }
        }
    }

    class EnumEntry
    {
        [JsonPropertyName("source")] public string Source { get; set; }
        [JsonPropertyName("translation")] public string Translation { get; set; }
    }

    static int RunExtractEnum(string[] args)
    {
        if (args.Length < 3) return PrintUsage();
        var asset = Load(args[0], args[1]);
        var found = new List<Dictionary<string, object>>();
        var seen = new HashSet<object>();
        foreach (var export in asset.Exports)
        {
            if (export is NormalExport nx)
            {
                foreach (var prop in nx.Data)
                    CollectText(prop, export.ObjectName.ToString(), found, seen, 0);
            }
        }
        // Dedupe par source: si plusieurs entries ont la meme source EN, on n'en garde qu'une
        var bySource = new Dictionary<string, EnumEntry>();
        foreach (var f in found)
        {
            string src = (string)f["source"];
            if (!bySource.ContainsKey(src)) bySource[src] = new EnumEntry { Source = src, Translation = "" };
        }
        var entries = bySource.Values.ToList();
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        File.WriteAllText(args[2], json);
        Console.WriteLine($"  Extracted {entries.Count} unique sources (raw count: {found.Count}) -> {args[2]}");
        return 0;
    }

    static int RunInjectEnum(string[] args)
    {
        if (args.Length < 4) return PrintUsage();
        var asset = Load(args[0], args[1]);
        var json = File.ReadAllText(args[2]);
        var entries = JsonSerializer.Deserialize<List<EnumEntry>>(json) ?? new();
        var dict = new Dictionary<string, string>();
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.Translation) || string.IsNullOrEmpty(e.Source)) continue;
            dict[e.Source] = e.Translation;
        }
        Console.WriteLine($"  Loaded {dict.Count} non-empty translations");

        var counter = new Counter();
        var seen = new HashSet<object>();
        foreach (var export in asset.Exports)
        {
            if (export is DataTableExport dx)
            {
                foreach (var row in dx.Table.Data)
                    foreach (var prop in row.Value)
                        PatchTextWalk(prop, dict, counter, seen, 0);
            }
            if (export is NormalExport nx)
            {
                foreach (var prop in nx.Data)
                    PatchTextWalk(prop, dict, counter, seen, 0);
            }
        }
        Console.WriteLine($"  Patched {counter.Value} String/Text values");

        asset.Write(args[3]);
        var size = new FileInfo(args[3]).Length;
        Console.WriteLine($"  Wrote {args[3]} ({size} bytes)");
        return 0;
    }

    class Counter { public int Value; }

    static void PatchTextWalk(object node, Dictionary<string, string> dict, Counter counter, HashSet<object> seen, int depth)
    {
        if (node == null || depth > 10) return;
        if (!seen.Add(node)) return;

        if (node is TextPropertyData tp)
        {
            var src = tp.CultureInvariantString?.Value;
            if (!string.IsNullOrEmpty(src) && dict.TryGetValue(src, out string trad))
            {
                bool needsUnicode = trad.Any(c => c > 127);
                var enc = needsUnicode ? System.Text.Encoding.Unicode : System.Text.Encoding.ASCII;
                tp.CultureInvariantString = new FString(trad, enc);
                counter.Value++;
            }
            return;
        }
        if (node is StrPropertyData strp)
        {
            var src = strp.Value?.Value;
            if (!string.IsNullOrEmpty(src) && dict.TryGetValue(src, out string trad))
            {
                bool needsUnicode = trad.Any(c => c > 127);
                var enc = needsUnicode ? System.Text.Encoding.Unicode : System.Text.Encoding.ASCII;
                strp.Value = new FString(trad, enc);
                counter.Value++;
            }
            return;
        }
        if (node is StructPropertyData sp)
        {
            foreach (var c in sp.Value) PatchTextWalk(c, dict, counter, seen, depth + 1);
            return;
        }
        if (node is ArrayPropertyData ap)
        {
            foreach (var c in ap.Value) PatchTextWalk(c, dict, counter, seen, depth + 1);
            return;
        }
        if (node is MapPropertyData mp)
        {
            foreach (var kv in mp.Value)
            {
                PatchTextWalk(kv.Key, dict, counter, seen, depth + 1);
                PatchTextWalk(kv.Value, dict, counter, seen, depth + 1);
            }
            return;
        }
        if (node is SetPropertyData stp)
        {
            foreach (var c in stp.Value) PatchTextWalk(c, dict, counter, seen, depth + 1);
            return;
        }
    }

    static int RunScanAll(string[] args)
    {
        if (args.Length < 2) return PrintUsage();
        string root = args[0];
        string usmapPath = args[1];
        string outJson = args.Length >= 3 ? args[2] : null;
        var mappings = new Usmap(usmapPath);
        Console.WriteLine($"Mappings: {mappings.Schemas.Count} schemas. Scanning {root}...");

        var files = Directory.EnumerateFiles(root, "*.uasset", SearchOption.AllDirectories).ToList();
        Console.WriteLine($"  {files.Count} .uasset files");

        var hits = new List<Dictionary<string, object>>();
        int scanned = 0, errors = 0, withText = 0;
        foreach (var path in files)
        {
            scanned++;
            try
            {
                var asset = new UAsset(path, EngineVersion.VER_UE5_3, mappings);
                var found = new List<Dictionary<string, object>>();
                var seen = new HashSet<object>();
                foreach (var export in asset.Exports)
                {
                    if (export is NormalExport nx)
                    {
                        foreach (var prop in nx.Data)
                            CollectText(prop, export.ObjectName.ToString(), found, seen, 0);
                    }
                }
                if (found.Count == 0) continue;
                withText++;
                string rel = path.Substring(root.Length).TrimStart('\\', '/');
                hits.Add(new Dictionary<string, object>
                {
                    ["path"] = rel,
                    ["count"] = found.Count,
                    ["entries"] = found
                });
                Console.WriteLine($"  [{found.Count}] {rel}");
            }
            catch (Exception ex)
            {
                errors++;
                if (errors <= 5) Console.WriteLine($"  ERR: {Path.GetFileName(path)}: {ex.GetType().Name}: {ex.Message}");
            }
        }
        Console.WriteLine($"\nSummary: scanned {scanned} | with TextProperty {withText} | errors {errors}");
        if (outJson != null)
        {
            var json = JsonSerializer.Serialize(hits, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(outJson, json);
            Console.WriteLine($"Report: {outJson}");
        }
        return 0;
    }

    static void CollectText(object node, string ownerExport, List<Dictionary<string, object>> found, HashSet<object> seen, int depth)
    {
        if (node == null || depth > 10) return;
        if (!seen.Add(node)) return;

        if (node is TextPropertyData tp)
        {
            var src = tp.CultureInvariantString?.Value;
            if (!string.IsNullOrWhiteSpace(src))
            {
                found.Add(new Dictionary<string, object>
                {
                    ["export"] = ownerExport,
                    ["prop"] = tp.Name?.ToString(),
                    ["source"] = src
                });
            }
            return;
        }

        // Recurse: structs / arrays / maps / sets
        if (node is StructPropertyData sp)
        {
            foreach (var c in sp.Value) CollectText(c, ownerExport, found, seen, depth + 1);
            return;
        }
        if (node is ArrayPropertyData ap)
        {
            foreach (var c in ap.Value) CollectText(c, ownerExport, found, seen, depth + 1);
            return;
        }
        if (node is MapPropertyData mp)
        {
            foreach (var kv in mp.Value)
            {
                CollectText(kv.Key, ownerExport, found, seen, depth + 1);
                CollectText(kv.Value, ownerExport, found, seen, depth + 1);
            }
            return;
        }
        if (node is SetPropertyData stp)
        {
            foreach (var c in stp.Value) CollectText(c, ownerExport, found, seen, depth + 1);
            return;
        }
    }

    static int RunScan(string[] args)
    {
        if (args.Length < 2) return PrintUsage();
        string root = args[0];
        string usmapPath = args[1];
        string outJson = args.Length >= 3 ? args[2] : null;
        var mappings = new Usmap(usmapPath);
        Console.WriteLine($"Mappings: {mappings.Schemas.Count} schemas. Scanning {root}...");

        var files = Directory.EnumerateFiles(root, "*.uasset", SearchOption.AllDirectories).ToList();
        Console.WriteLine($"  {files.Count} .uasset files");

        var hits = new List<Dictionary<string, object>>();
        int scanned = 0, errors = 0, dts = 0, dtsWithText = 0;
        foreach (var path in files)
        {
            scanned++;
            try
            {
                var asset = new UAsset(path, EngineVersion.VER_UE5_3, mappings);
                var dt = asset.Exports.OfType<DataTableExport>().FirstOrDefault();
                if (dt == null) continue;
                dts++;
                int rows = dt.Table.Data.Count;
                int textProps = 0, nonEmpty = 0;
                var sample = new List<string>();
                foreach (var row in dt.Table.Data)
                {
                    foreach (var prop in row.Value.OfType<TextPropertyData>())
                    {
                        textProps++;
                        var s = prop.CultureInvariantString?.Value;
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            nonEmpty++;
                            if (sample.Count < 3) sample.Add(s.Length > 50 ? s.Substring(0, 50) + "..." : s);
                        }
                    }
                }
                if (textProps == 0) continue;
                dtsWithText++;
                string rel = path.Substring(root.Length).TrimStart('\\', '/');
                hits.Add(new Dictionary<string, object>
                {
                    ["path"] = rel,
                    ["rows"] = rows,
                    ["text_props_total"] = textProps,
                    ["text_props_non_empty"] = nonEmpty,
                    ["sample"] = sample
                });
                Console.WriteLine($"  [{nonEmpty}/{textProps}] rows={rows}  {rel}");
                foreach (var s in sample) Console.WriteLine($"      \"{s.Replace("\r", "\\r").Replace("\n", "\\n")}\"");
            }
            catch (Exception ex)
            {
                errors++;
                if (errors <= 5) Console.WriteLine($"  ERR: {Path.GetFileName(path)}: {ex.GetType().Name}: {ex.Message}");
            }
        }
        Console.WriteLine($"\nSummary: scanned {scanned} | DataTables {dts} | with TextProperty {dtsWithText} | errors {errors}");
        if (outJson != null)
        {
            var json = JsonSerializer.Serialize(hits, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(outJson, json);
            Console.WriteLine($"Report: {outJson}");
        }
        return 0;
    }

    static UAsset Load(string assetPath, string usmapPath)
    {
        Console.WriteLine($"Loading: {assetPath}");
        Console.WriteLine($"  Mappings: {usmapPath}");
        var mappings = new Usmap(usmapPath);
        Console.WriteLine($"  Mappings: {mappings.Schemas.Count} schemas, {mappings.EnumMap.Count} enums");
        var asset = new UAsset(assetPath, EngineVersion.VER_UE5_3, mappings);
        Console.WriteLine($"  Exports: {asset.Exports.Count}, Imports: {asset.Imports.Count}, EngineVer: {asset.GetEngineVersion()}");
        int failed = asset.Exports.Count(e => e is RawExport);
        Console.WriteLine($"  Raw (failed) exports: {failed} of {asset.Exports.Count}");
        return asset;
    }

    static DataTableExport FindDataTable(UAsset asset)
    {
        var dt = asset.Exports.OfType<DataTableExport>().FirstOrDefault();
        if (dt == null) throw new Exception("No DataTableExport found in asset");
        return dt;
    }

    // Reduit "Name_2_C4C2D9D54622E452248034A91CDF32DF" -> "Name"
    static string ShortFieldName(string full)
    {
        if (string.IsNullOrEmpty(full)) return full;
        int i = full.IndexOf('_');
        return i > 0 ? full.Substring(0, i) : full;
    }

    static int RunDiag(string[] args)
    {
        if (args.Length < 2) return PrintUsage();
        var asset = Load(args[0], args[1]);
        var dt = FindDataTable(asset);
        Console.WriteLine($"  DataTable: {dt.ObjectName}, Rows: {dt.Table.Data.Count}");
        int rowsShown = 0;
        foreach (var row in dt.Table.Data)
        {
            if (rowsShown++ >= 3) break;
            Console.WriteLine($"  --- Row '{row.Name}' (struct {row.StructType})");
            foreach (var prop in row.Value.OfType<TextPropertyData>())
            {
                Console.WriteLine($"      {prop.Name}: " +
                    $"History={prop.HistoryType}, " +
                    $"Namespace=\"{prop.Namespace?.Value}\", " +
                    $"Key=\"{prop.Value?.Value}\", " +
                    $"Source=\"{prop.CultureInvariantString?.Value?.Replace("\n", "\\n")}\"");
            }
        }
        return 0;
    }

    class Entry
    {
        [JsonPropertyName("row")] public string Row { get; set; }
        [JsonPropertyName("field")] public string Field { get; set; }
        [JsonPropertyName("field_full")] public string FieldFull { get; set; }
        [JsonPropertyName("key")] public string Key { get; set; }
        [JsonPropertyName("source")] public string Source { get; set; }
        [JsonPropertyName("translation")] public string Translation { get; set; }
    }

    static int RunExtract(string[] args)
    {
        if (args.Length < 3) return PrintUsage();
        var asset = Load(args[0], args[1]);
        var dt = FindDataTable(asset);
        var entries = new List<Entry>();
        foreach (var row in dt.Table.Data)
        {
            string rowName = row.Name.ToString();
            foreach (var prop in row.Value.OfType<TextPropertyData>())
            {
                string fullField = prop.Name?.ToString();
                entries.Add(new Entry
                {
                    Row = rowName,
                    Field = ShortFieldName(fullField),
                    FieldFull = fullField,
                    Key = prop.Value?.Value,
                    Source = prop.CultureInvariantString?.Value,
                    Translation = ""
                });
            }
        }
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        File.WriteAllText(args[2], json);
        Console.WriteLine($"  Extracted {entries.Count} TextProperty entries -> {args[2]}");
        return 0;
    }

    static int RunInject(string[] args)
    {
        if (args.Length < 4) return PrintUsage();
        var asset = Load(args[0], args[1]);
        var dt = FindDataTable(asset);
        var json = File.ReadAllText(args[2]);
        var entries = JsonSerializer.Deserialize<List<Entry>>(json) ?? new();
        // Indexer par row|field_full pour eviter toute collision si plusieurs champs partagent le prefixe
        var dict = new Dictionary<string, string>();
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.Translation)) continue;
            string fullField = !string.IsNullOrEmpty(e.FieldFull) ? e.FieldFull : e.Field;
            dict[$"{e.Row}|{fullField}"] = e.Translation;
        }
        Console.WriteLine($"  Loaded {dict.Count} non-empty translations");

        int patched = 0, skipped = 0;
        foreach (var row in dt.Table.Data)
        {
            string rowName = row.Name.ToString();
            foreach (var prop in row.Value.OfType<TextPropertyData>())
            {
                string fullField = prop.Name?.ToString();
                if (dict.TryGetValue($"{rowName}|{fullField}", out string trad))
                {
                    bool needsUnicode = trad.Any(c => c > 127);
                    var enc = needsUnicode ? System.Text.Encoding.Unicode : System.Text.Encoding.ASCII;
                    prop.CultureInvariantString = new FString(trad, enc);
                    patched++;
                }
                else skipped++;
            }
        }
        Console.WriteLine($"  Patched {patched} CultureInvariantString values (skipped {skipped})");

        asset.Write(args[3]);
        var size = new FileInfo(args[3]).Length;
        Console.WriteLine($"  Wrote {args[3]} ({size} bytes)");
        return 0;
    }
}
