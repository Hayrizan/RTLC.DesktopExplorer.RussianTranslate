using System;
using HarmonyLib;
using UnityEngine.Localization.Settings;
using Utils;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianStorytellerInputAliases
    {
        internal static string NormalizeMagicWord(string value, string localeCode)
        {
            if (localeCode != Plugin.RussianCode)
                return value;

            if (string.Equals(value, "ИНАННА", StringComparison.OrdinalIgnoreCase))
                return "INANNA";
            if (string.Equals(value, "АБРАКАДАБРА", StringComparison.OrdinalIgnoreCase))
                return "Abracadabra";

            // The temple recognizes this name as a separate story reaction.
            // Keep the serialized matcher Latin while accepting the translated clue.
            if (string.Equals(value, "КАССАНДРА", StringComparison.OrdinalIgnoreCase))
                return "KASSANDRA";

            // The temple has distinct reactions to these names as well; keep them
            // reachable from the translated clues instead of treating them as typos.
            if (string.Equals(value, "ЭЛЕФАС", StringComparison.OrdinalIgnoreCase))
                return "elephas";
            if (string.Equals(value, "ПОРТОНУС", StringComparison.OrdinalIgnoreCase))
                return "portonus";
            if (string.Equals(value, "РУБИКОН", StringComparison.OrdinalIgnoreCase))
                return "rubicon";

            string trimmed = value?.Trim();
            if (string.Equals(trimmed, "СЖЕЧЬ ВСЁ", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "СЖЕЧЬ ВСЕ", StringComparison.OrdinalIgnoreCase))
                return "burn it all";

            return value;
        }
    }

    [HarmonyPatch(typeof(StringExtensions), nameof(StringExtensions.StripSurroundingBraces))]
    internal static class RussianStorytellerMagicWordAliasPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref string __result)
        {
            __result = RussianStorytellerInputAliases.NormalizeMagicWord(
                __result, LocalizationSettings.SelectedLocale?.Identifier.Code);
        }
    }
}
