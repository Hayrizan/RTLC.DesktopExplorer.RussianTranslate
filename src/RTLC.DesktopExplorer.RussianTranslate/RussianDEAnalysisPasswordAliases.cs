using System;
using HarmonyLib;
using UnityEngine.Localization.Settings;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianDEAnalysisPasswordAliases
    {
        internal static string MapPromptPrefix(string prefix, string localeCode)
        {
            if (localeCode != Plugin.RussianCode || prefix == null)
                return prefix;

            return prefix == "DEANALYSIS> " ? "АНАЛИЗ> " : prefix;
        }

        internal static string Map(string input, string localeCode)
        {
            if (localeCode != Plugin.RussianCode || input == null)
                return input;

            return input.Trim().ToUpperInvariant() switch
            {
                "ТАРОККИ" => "TAROCCHI",
                "ЗОЛОТОЕ СЕЧЕНИЕ" or "ЗОЛОТОЕСЕЧЕНИЕ" or "ЗОЛОТОЕ_СЕЧЕНИЕ" or "ЗОЛОТОЕ-СЕЧЕНИЕ" or
                "ЗОЛОТОГО СЕЧЕНИЯ" or "ЗОЛОТОГОСЕЧЕНИЯ" or "ЗОЛОТОГО_СЕЧЕНИЯ" or "ЗОЛОТОГО-СЕЧЕНИЯ" => "GOLDENRATIO",
                _ => input,
            };
        }
    }

    [HarmonyPatch(typeof(global::Program.Terminal.CommandListeners.DEAnalysisPwdCmdListener), "HandleInput")]
    internal static class RussianDEAnalysisPasswordAliasPatch
    {
        private static void Prefix(ref string input) =>
            input = RussianDEAnalysisPasswordAliases.Map(input, LocalizationSettings.SelectedLocale?.Identifier.Code);
    }

    [HarmonyPatch(typeof(global::Program.Terminal.CommandListeners.DEAnalysisPwdCmdListener), "GetInputPromptPrefix")]
    internal static class RussianDEAnalysisPasswordPromptPatch
    {
        private static void Postfix(ref string __result) =>
            __result = RussianDEAnalysisPasswordAliases.MapPromptPrefix(
                __result, LocalizationSettings.SelectedLocale?.Identifier.Code);
    }
}
