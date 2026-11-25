using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal sealed class RussianTableProvider : ITableProvider, IDisposable
    {
        private readonly TranslationCatalog catalog;
        private readonly ITableProvider previous;
        private readonly ManualLogSource log;
        private readonly RussianFonts fonts;
        private readonly Locale englishFallback;
        private readonly List<AsyncOperationHandle> handles = new List<AsyncOperationHandle>();
        private readonly List<LocalizationTable> clones = new List<LocalizationTable>();
        private readonly Dictionary<(Type, string), AsyncOperationHandle> cache = new Dictionary<(Type, string), AsyncOperationHandle>();

        public RussianTableProvider(TranslationCatalog catalog, ITableProvider previous, ManualLogSource log, RussianFonts fonts = null)
        {
            this.catalog = catalog;
            this.previous = previous;
            this.log = log;
            this.fonts = fonts;
            englishFallback = LocalizationSettings.AvailableLocales?.GetLocale(Plugin.EnglishCode);
        }

        public AsyncOperationHandle<TTable> ProvideTableAsync<TTable>(string tableCollectionName, Locale locale)
            where TTable : LocalizationTable
        {
            // A few native listeners request a table during shutdown after the
            // selected locale has been cleared. Passing null to Unity's original
            // provider throws instead of returning the normal English fallback.
            if (locale == null) locale = englishFallback;
            if (locale == null || locale.Identifier.Code != Plugin.RussianCode)
                return previous?.ProvideTableAsync<TTable>(tableCollectionName, locale) ?? default;

            var cacheKey = (typeof(TTable), tableCollectionName);
            if (cache.TryGetValue(cacheKey, out var cached) && cached.IsValid())
            {
                if (cached.Result is AssetTable cachedAssets && tableCollectionName == "FontAssets")
                    fonts?.Apply(cachedAssets);
                return Addressables.ResourceManager.Acquire(cached.Convert<TTable>());
            }

            // Load the English Addressable directly. Calling StringDatabase.GetTableAsync here
            // would depend on LocalizationSettings.InitializationOperation and can deadlock preloading.
            var source = Addressables.LoadAssetAsync<TTable>(tableCollectionName + "_" + Plugin.EnglishCode);
            handles.Add(source);
            var result = Addressables.ResourceManager.CreateChainOperation(source, op =>
            {
                if (op.Status != AsyncOperationStatus.Succeeded || op.Result == null)
                    return Addressables.ResourceManager.CreateCompletedOperation<TTable>(null,
                        $"English source table {tableCollectionName} could not be loaded: {op.OperationException}");

                var clone = UnityEngine.Object.Instantiate(op.Result);
                clone.name = tableCollectionName + "_" + Plugin.RussianCode;
                clone.LocaleIdentifier = new LocaleIdentifier(Plugin.RussianCode);
                clone.hideFlags = HideFlags.DontUnloadUnusedAsset;
                clones.Add(clone);
                int translated = 0;
                if (clone is StringTable strings)
                {
                    foreach (var entry in strings.Values)
                    {
                        string key = strings.SharedData.GetKey(entry.KeyId);
                        // Legacy rows can survive without a SharedTableData name. The extractor
                        // addresses these by their stable ID; keep SharedTableData unchanged.
                        if (string.IsNullOrEmpty(key))
                            key = entry.KeyId.ToString(CultureInfo.InvariantCulture);
                        if (catalog.TryGet(tableCollectionName, key, out string text))
                        {
                            entry.Value = text;
                            translated++;
                        }
                    }

                    var supplementalEntries = new List<KeyValuePair<string, string>>();
                    foreach (var translation in catalog.GetEntries(tableCollectionName))
                    {
                        if (strings.SharedData.GetId(translation.Key, false) == 0)
                            supplementalEntries.Add(translation);
                    }
                    if (supplementalEntries.Count > 0)
                    {
                        // A table clone still references the English shared key data. Clone
                        // that data before AddEntry so supplemental Russian keys stay private.
                        strings.SharedData = UnityEngine.Object.Instantiate(strings.SharedData);
                        foreach (var translation in supplementalEntries)
                        {
                            strings.AddEntry(translation.Key, translation.Value);
                            translated++;
                        }
                    }
                    log.LogInfo($"Table {tableCollectionName}: {translated}/{strings.Count} reviewed Russian entries; remaining values use English.");
                }
                if (clone is AssetTable assets && tableCollectionName == "FontAssets" && fonts != null)
                {
                    var prepared = fonts.Prepare((AssetTable)(LocalizationTable)op.Result);
                    var fontResult = Addressables.ResourceManager.CreateChainOperation(prepared, ready =>
                    {
                        if (ready.Status != AsyncOperationStatus.Succeeded)
                            return Addressables.ResourceManager.CreateCompletedOperation<TTable>(null, "Russian font preparation failed: " + ready.OperationException);
                        fonts.Apply(assets);
                        return Addressables.ResourceManager.CreateCompletedOperation(clone, null);
                    });
                    // CreateChainOperation acquired its dependency.
                    Addressables.Release(prepared);
                    return fontResult;
                }
                // Image assets and all underlying English GUIDs remain unchanged.
                return Addressables.ResourceManager.CreateCompletedOperation(clone, null);
            });
            cache[cacheKey] = result;
            handles.Add(result);
            // Keep one provider-owned reference; the database owns the acquired reference.
            // English assets stay alive for cloned tables even across locale switches.
            return Addressables.ResourceManager.Acquire(result);
        }

        public void Dispose()
        {
            foreach (var clone in clones)
                if (clone != null)
                {
                    if (clone is AssetTable assets) assets.ReleaseAssets();
                    UnityEngine.Object.Destroy(clone);
                }
            clones.Clear();
            foreach (var handle in handles)
                if (handle.IsValid()) Addressables.Release(handle);
            handles.Clear();
            cache.Clear();
        }
    }
}
