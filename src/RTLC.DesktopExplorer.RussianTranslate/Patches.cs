using Cysharp.Threading.Tasks;
using HarmonyLib;
using OS;
using Settings;
using StartBIOS;
using StartBIOS.BIOSSettings;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using System;
using System.Collections.Generic;
using System.Reflection;
using Utils;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(DESettingsProvider), nameof(DESettingsProvider.LoadSettings))]
    internal static class LoadSettingsPatch
    {
        private static void Postfix(ref UniTask __result) =>
            __result = Plugin.Instance.AfterLoadSettings(__result);
    }

    [HarmonyPatch(typeof(OSBootstrapper), nameof(OSBootstrapper.Initialize))]
    internal static class BootstrapPatch
    {
        private static void Postfix(ref UniTask __result) =>
            __result = Plugin.Instance.AfterBootstrap(__result);
    }

    [HarmonyPatch(typeof(DesktopExplorerUtils), nameof(DesktopExplorerUtils.SortedLocales))]
    internal static class LocaleOrderPatch
    {
        private static void Prefix() => Plugin.AddRussianFirst();
    }

    [HarmonyPatch(typeof(LanguagePickerController), "LoadLanguageSetting")]
    internal static class InitialLanguagePickerPatch
    {
        private static void Postfix(ref string ___selectedLocale, SettingSelectorController ___languagePicker)
        {
            // This is the one-time welcome picker. Unity starts it at en-US even when
            // the game profile is new, and the plugin config may already exist from an
            // earlier test or another profile. Set the displayed default directly here.
            var russian = LocalizationSettings.AvailableLocales.GetLocale(Plugin.RussianCode);
            var locales = DesktopExplorerUtils.SortedLocales();
            int russianIndex = locales.FindIndex(locale => locale.Identifier.Code == Plugin.RussianCode);
            if (russian != null && russianIndex >= 0)
            {
                ___languagePicker?.SetCurrentIndex(russianIndex);
                LocalizationSettings.SelectedLocale = russian;
            }
            ___selectedLocale = Plugin.RussianCode;
        }
    }

    [HarmonyPatch]
    internal static class RussianPreloadPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var operation = typeof(LocalizationSettings).Assembly.GetType("UnityEngine.Localization.Operations.PreloadLocaleOperation`2", true);
            yield return AccessTools.Method(operation.MakeGenericType(typeof(AssetTable), typeof(AssetTableEntry)), "BeginPreloading");
            yield return AccessTools.Method(operation.MakeGenericType(typeof(StringTable), typeof(StringTableEntry)), "BeginPreloading");
        }

        private static bool Prefix(object __instance, Locale ___m_Locale)
        {
            if (___m_Locale?.Identifier.Code != Plugin.RussianCode) return true;
            // Native label-based preloading bypasses ITableProvider and registers the
            // game's empty ru-RU tables in the database cache. Load Russian tables on
            // demand through our provider instead; leave all original locales alone.
            AccessTools.Method(__instance.GetType(), "CompleteAndRelease").Invoke(__instance, new object[] { true, null });
            return false;
        }
    }

    [HarmonyPatch(typeof(LocalizationUtils), "GetLocalizedStringNoArgs")]
    internal static class LocaleLookupRecoveryPatch
    {
        private static bool Prefix(string tableReference, string id, ref UniTask<string> __result)
        {
            __result = GetLocalizedString(tableReference, id);
            return false;
        }

        private static async UniTask<string> GetLocalizedString(string tableReference, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                UnityEngine.Debug.LogWarning($"Cannot Find: {id} in table {tableReference}.");
                return "BAD_LOCALIZATION_KEY";
            }

            if (LocalizationSettings.HasSettings)
            {
                var initialization = LocalizationSettings.InitializationOperation;
                if (initialization.IsValid() && !initialization.IsDone)
                    await initialization;
            }

            Plugin.Instance?.EnsureSelectedLocale($"localized string lookup ({tableReference})");
            var locale = LocalizationSettings.HasSettings ? LocalizationSettings.SelectedLocale : null;
            if (locale == null && LocalizationSettings.HasSettings)
            {
                string savedLocaleCode = DESettingsProvider.SELECTED_LANGUAGE?.GetValue();
                if (string.IsNullOrWhiteSpace(savedLocaleCode))
                    savedLocaleCode = Plugin.RussianCode;
                var availableLocales = LocalizationSettings.AvailableLocales;
                locale = availableLocales?.GetLocale(savedLocaleCode) ??
                    availableLocales?.GetLocale(Plugin.RussianCode) ??
                    availableLocales?.GetLocale(Plugin.EnglishCode);
            }

            if (locale == null)
            {
                Plugin.LogError($"Could not resolve a locale while loading table '{tableReference}' entry '{id}'.");
                return id;
            }

            var tableOperation = LocalizationSettings.StringDatabase.GetTableAsync(tableReference, locale);
            while (tableOperation.IsValid() && !tableOperation.IsDone)
                await Cysharp.Threading.Tasks.UniTask.Yield();
            if (!tableOperation.IsValid() ||
                !string.Equals(tableOperation.Status.ToString(), "Succeeded", StringComparison.Ordinal))
            {
                Plugin.LogError($"[Localization] Table '{tableReference}' failed to load for {locale.Identifier.Code}.");
                return id;
            }

            var stringTable = tableOperation.Result;
            if (!stringTable)
            {
                UnityEngine.Debug.LogError($"[Localization] Table '{tableReference}' not found.");
                return id;
            }

            var entry = stringTable.GetEntry(id);
            if (entry == null)
            {
                UnityEngine.Debug.LogWarning($"[Localization] Entry '{id}' not found in table '{tableReference}'.");
                return id;
            }

            return string.IsNullOrWhiteSpace(entry.Value) ? id : entry.Value;
        }
    }
}
