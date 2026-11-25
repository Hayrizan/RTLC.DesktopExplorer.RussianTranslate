using System;
using HarmonyLib;
using UnityEngine.Localization.Settings;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianShellInputAliases
    {
        internal static string MapExit(string input, string localeCode)
        {
            if (localeCode == Plugin.RussianCode && string.Equals(input, "EXIT", StringComparison.OrdinalIgnoreCase))
                return "ВЫЙТИ";
            return input;
        }

        internal static string MapYesNo(string input, string localeCode)
        {
            if (localeCode != Plugin.RussianCode || input == null)
                return input;

            return input.Trim().ToUpperInvariant() switch
            {
                "YES" => "ДА",
                "Y" => "Д",
                "NO" => "НЕТ",
                "N" => "Н",
                _ => input,
            };
        }

        internal static string MapPassword(string input, string localeCode)
        {
            if (localeCode != Plugin.RussianCode || input == null)
                return input;

            return input.Trim().ToUpperInvariant() switch
            {
                "ТАРОККИ" => "TAROCCHI",
                "НЕВЕРНЫЙ КЛЮЧ" or "НЕВЕРНЫЙКЛЮЧ" or "НЕВЕРНЫЙ_КЛЮЧ" or "НЕВЕРНЫЙ-КЛЮЧ" => "WRONGKEY",
                _ => input,
            };
        }
    }

    [HarmonyPatch(typeof(global::Program.Terminal.CommandListeners.GenericShellMenuCmdListener), "HandleMenuInput")]
    internal static class RussianShellExitAliasPatch
    {
        private static void Prefix(ref string input) =>
            input = RussianShellInputAliases.MapExit(input, LocalizationSettings.SelectedLocale?.Identifier.Code);
    }

    [HarmonyPatch(typeof(global::Program.Terminal.CommandListeners.GenericShellMenuCmdListener), "HandleYesNoInput")]
    internal static class RussianShellYesNoAliasPatch
    {
        private static void Prefix(ref string input) =>
            input = RussianShellInputAliases.MapYesNo(input, LocalizationSettings.SelectedLocale?.Identifier.Code);
    }

    [HarmonyPatch(typeof(global::Program.Terminal.CommandListeners.GenericShellMenuCmdListener), "HandleInput")]
    internal static class RussianShellPasswordAliasPatch
    {
        private static void Prefix(ref string input) =>
            input = RussianShellInputAliases.MapPassword(input, LocalizationSettings.SelectedLocale?.Identifier.Code);
    }
}
