using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using SODefinitions.TerminalSO;
using UnityEngine.Localization.Settings;
using Utils;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianTerminalPuzzleAliases
    {
        private static readonly Dictionary<string, string[]> ParameterAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["aleph"] = new[] { "алеф" },
            ["absent"] = new[] { "отсутствует", "пропал" },
            ["hello"] = new[] { "привет" },
            ["body"] = new[] { "тело" },
            ["mind"] = new[] { "разум", "сознание" },
            ["spirit"] = new[] { "дух" }
        };

        internal static bool Matches(LocalizedTerminalParameter parameter, string input, string localeCode)
        {
            if (localeCode != Plugin.RussianCode || parameter == null || string.IsNullOrWhiteSpace(input))
                return false;

            string id = parameter.ParameterId ?? string.Empty;
            string normalizedInput = input.Trim();
            foreach (var entry in ParameterAliases)
            {
                if (id.IndexOf(entry.Key, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                foreach (string alias in entry.Value)
                    if (string.Equals(normalizedInput, alias, StringComparison.OrdinalIgnoreCase))
                        return true;
            }

            return false;
        }

        internal static void AppendCommandAliases(
            TerminalCommandSO command, List<string> aliases, string localizedCommandType, string localeCode)
        {
            if (localeCode != Plugin.RussianCode || command == null || aliases == null)
                return;

            // Keep the original aliases for compatibility and add the localized
            // command label so what players see is also accepted as input.
            if (!string.IsNullOrWhiteSpace(localizedCommandType))
                AddMissing(aliases, localizedCommandType.Trim());

            string commandType = command.commandType.ToString();
            if (string.Equals(commandType, "Yell", StringComparison.OrdinalIgnoreCase))
                AddMissing(aliases, "КРИКНИ");
            else if (string.Equals(commandType, "Chant", StringComparison.OrdinalIgnoreCase))
                AddMissing(aliases, "СПОЙ");
            else if (string.Equals(commandType, "Start", StringComparison.OrdinalIgnoreCase))
                AddMissing(aliases, "СТАРТ");
        }

        internal static string GetAnalysisPlaythroughAlias(
            global::Program.Terminal.CommandListeners.MenuNode.MenuOption option,
            string localeCode)
        {
            if (localeCode != Plugin.RussianCode || option == null)
                return null;

            string id = option.inputKeywordLocId ?? string.Empty;
            return id.StartsWith("CMD_Shell_DEAnalysis_OptPlay1Display_", StringComparison.Ordinal)
                ? "ПРОХОЖДЕНИЕ_1"
                : id.StartsWith("CMD_Shell_DEAnalysis_OptPlay2Display_", StringComparison.Ordinal)
                    ? "ПРОХОЖДЕНИЕ_2"
                    : null;
        }

        internal static void AppendAnalysisPlaythroughAlias(
            global::Program.Terminal.CommandListeners.MenuNode.MenuOption option,
            List<string> aliases,
            string localeCode)
        {
            if (aliases == null) return;
            string alias = GetAnalysisPlaythroughAlias(option, localeCode);
            if (alias != null) AddMissing(aliases, alias);
        }

        internal static string NormalizeMenuInput(string input, string localeCode) =>
            localeCode == Plugin.RussianCode ? input?.Trim() : input;

        internal static string NormalizeChronoRestoreArgument(string input, string localeCode)
        {
            if (localeCode != Plugin.RussianCode || string.IsNullOrWhiteSpace(input))
                return null;

            string[] parts = input.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 &&
                   string.Equals(parts[0], "ВОССТАНОВЛЕНИЕ", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(parts[1], "ХРОНОЛОГА", StringComparison.OrdinalIgnoreCase)
                ? "восстановление хронолога"
                : null;
        }

        private static void AddMissing(List<string> aliases, string alias)
        {
            if (!aliases.Exists(value => string.Equals(value, alias, StringComparison.OrdinalIgnoreCase)))
                aliases.Add(alias);
        }
    }

    [HarmonyPatch(typeof(LocalizedTerminalParameter), nameof(LocalizedTerminalParameter.Matches))]
    internal static class RussianTerminalParameterAliasPatch
    {
        private static void Postfix(LocalizedTerminalParameter __instance, string input, ref bool __result) =>
            __result |= RussianTerminalPuzzleAliases.Matches(
                __instance, input, LocalizationSettings.SelectedLocale?.Identifier.Code);
    }

    [HarmonyPatch(typeof(LocalizationUtils), nameof(LocalizationUtils.GetCommandAliasLocalizedString))]
    internal static class RussianTerminalCommandAliasDisplayPatch
    {
        private static void Postfix(ref UniTask<string> __result)
        {
            if (LocalizationSettings.SelectedLocale?.Identifier.Code == Plugin.RussianCode)
                __result = FilterRussianAliases(__result);
        }

        private static async UniTask<string> FilterRussianAliases(UniTask<string> original)
        {
            string value = await original;
            if (string.IsNullOrWhiteSpace(value))
                return value;

            string[] parts = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var visible = new List<string>();
            foreach (string part in parts)
            {
                bool hasCyrillic = false;
                bool hasLatin = false;
                foreach (char character in part)
                {
                    if ((character >= '\u0400' && character <= '\u052F'))
                        hasCyrillic = true;
                    else if ((character >= 'A' && character <= 'Z') ||
                             (character >= 'a' && character <= 'z'))
                        hasLatin = true;
                }

                if (!hasCyrillic || hasLatin ||
                    visible.Exists(alias => string.Equals(alias, part, StringComparison.OrdinalIgnoreCase)))
                    continue;
                visible.Add(part);
            }

            return visible.Count > 0 ? string.Join(",", visible) : value;
        }
    }

    [HarmonyPatch(typeof(TerminalCommandSO), nameof(TerminalCommandSO.GetCommandAliases))]
    internal static class RussianTerminalCommandAliasPatch
    {
        private static void Postfix(TerminalCommandSO __instance, ref UniTask<List<string>> __result)
        {
            if (LocalizationSettings.SelectedLocale?.Identifier.Code == Plugin.RussianCode)
                __result = AppendRussianAliases(__instance, __result);
        }

        private static async UniTask<List<string>> AppendRussianAliases(
            TerminalCommandSO command, UniTask<List<string>> original)
        {
            List<string> aliases = await original;
            string localizedCommandType = await command.GetLocalizedCommandType();
            RussianTerminalPuzzleAliases.AppendCommandAliases(
                command, aliases, localizedCommandType, LocalizationSettings.SelectedLocale?.Identifier.Code);
            return aliases;
        }
    }

    [HarmonyPatch(typeof(global::Program.Terminal.CommandListeners.MenuNode.MenuOption),
        nameof(global::Program.Terminal.CommandListeners.MenuNode.MenuOption.GetInputKeyword))]
    internal static class RussianDEAnalysisPlaythroughAliasPatch
    {
        private static void Postfix(
            global::Program.Terminal.CommandListeners.MenuNode.MenuOption __instance,
            ref UniTask<List<string>> __result)
        {
            string localeCode = LocalizationSettings.SelectedLocale?.Identifier.Code;
            if (localeCode == Plugin.RussianCode)
                __result = AppendAlias(__instance, __result, localeCode);
        }

        private static async UniTask<List<string>> AppendAlias(
            global::Program.Terminal.CommandListeners.MenuNode.MenuOption option,
            UniTask<List<string>> operation,
            string localeCode)
        {
            List<string> aliases = await operation;
            RussianTerminalPuzzleAliases.AppendAnalysisPlaythroughAlias(option, aliases, localeCode);
            return aliases;
        }
    }

    [HarmonyPatch(typeof(global::Program.Terminal.CommandListeners.MenuNode.MenuOption),
        nameof(global::Program.Terminal.CommandListeners.MenuNode.MenuOption.GetOverwriteKeyword))]
    internal static class RussianDEAnalysisPlaythroughOverwriteAliasPatch
    {
        private static void Postfix(
            global::Program.Terminal.CommandListeners.MenuNode.MenuOption __instance,
            ref UniTask<string> __result)
        {
            string alias = RussianTerminalPuzzleAliases.GetAnalysisPlaythroughAlias(
                __instance, LocalizationSettings.SelectedLocale?.Identifier.Code);
            if (alias != null)
                __result = UniTask.FromResult(alias);
        }
    }

    [HarmonyPatch(typeof(global::Program.Terminal.CommandListeners.GenericShellMenuCmdListener), "HandleMenuInput")]
    internal static class RussianShellMenuInputTrimPatch
    {
        private static void Prefix(ref string input) =>
            input = RussianTerminalPuzzleAliases.NormalizeMenuInput(
                input, LocalizationSettings.SelectedLocale?.Identifier.Code);
    }

    [HarmonyPatch(typeof(global::Program.Terminal.TerminalInterpreter), "GetArgument")]
    internal static class RussianChronoRestoreArgumentPatch
    {
        private static void Postfix(string input, ref string __result)
        {
            string normalized = RussianTerminalPuzzleAliases.NormalizeChronoRestoreArgument(
                input, LocalizationSettings.SelectedLocale?.Identifier.Code);
            if (normalized != null) __result = normalized;
        }
    }
    [HarmonyPatch(typeof(global::Program.Terminal.TerminalController), "ConstructCommand")]
    internal static class RussianStandaloneChronologRestorePatch
    {
        private static void Prefix(ref string input)
        {
            if (LocalizationSettings.SelectedLocale?.Identifier.Code != Plugin.RussianCode || input == null) return;
            string trimmed = input.Trim();
            const string phrase = "восстановление хронолога";
            if (string.Equals(trimmed, phrase, StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith(phrase + " ", StringComparison.OrdinalIgnoreCase))
                input = "CHRONO restore" + trimmed.Substring(phrase.Length);
        }
    }

}
