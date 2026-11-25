using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal sealed class TranslationCatalog
    {
        private readonly Dictionary<string, Dictionary<string, string>> tables =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        public int Count { get; private set; }

        public static TranslationCatalog Load(LocalizationBundle payload)
        {
            var result = new TranslationCatalog();
            foreach (string file in payload.Names)
            {
                if (!file.StartsWith("translations/", StringComparison.Ordinal) ||
                    !file.EndsWith(".json", StringComparison.Ordinal)) continue;
                var asset = payload.Require<UnityEngine.TextAsset>(file);
                var table = JsonConvert.DeserializeObject<Dictionary<string, string>>(asset.text.TrimStart('\uFEFF'));
                if (table == null)
                    throw new InvalidDataException("Translation file is empty: " + file);
                result.tables.Add(Path.GetFileNameWithoutExtension(asset.name), table);
                result.Count += table.Count;
            }
            if (result.Count == 0)
                throw new InvalidDataException("No Russian translations were found in rtlc.bundle");
            return result;
        }

        public bool TryGet(string tableName, string key, out string value)
        {
            value = null;
            return key != null && tables.TryGetValue(tableName, out var table) && table.TryGetValue(key, out value);
        }

        public IEnumerable<KeyValuePair<string, string>> GetEntries(string tableName)
        {
            return tables.TryGetValue(tableName, out var table)
                ? table
                : Array.Empty<KeyValuePair<string, string>>();
        }
    }
}
