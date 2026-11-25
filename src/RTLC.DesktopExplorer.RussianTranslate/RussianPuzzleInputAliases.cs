using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Program;
using Program.Mall;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(NextRoomWindowController), nameof(NextRoomWindowController.LogIn))]
    internal static class RussianNextRoomsPassword
    {
        private static void Prefix(TMP_InputField ___usernameInputField, TMP_InputField ___passwordInputField,
            ref string ___nextRoomsMaxUsername, ref string ___nextRoomsMaxPassword, out string[] __state)
        {
            __state = null;
            if (LocalizationSettings.SelectedLocale?.Identifier.Code != Plugin.RussianCode)
                return;

            __state = new[] { ___nextRoomsMaxUsername, ___nextRoomsMaxPassword };
            // Substitute only the recognized translated credentials for the native comparison.
            // Each field still has to pass the game's own check; persistence stays canonical.
            if (___usernameInputField &&
                string.Equals(___nextRoomsMaxUsername, "The_Max", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(___usernameInputField.text, "Макс", StringComparison.OrdinalIgnoreCase))
                ___nextRoomsMaxUsername = ___usernameInputField.text;
            if (___passwordInputField &&
                string.Equals(___nextRoomsMaxPassword, "sweaters123", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(___passwordInputField.text, "Свитера123", StringComparison.OrdinalIgnoreCase))
                ___nextRoomsMaxPassword = ___passwordInputField.text;
        }

        private static void Finalizer(ref string ___nextRoomsMaxUsername,
            ref string ___nextRoomsMaxPassword, string[] __state)
        {
            if (__state == null) return;
            ___nextRoomsMaxUsername = __state[0];
            ___nextRoomsMaxPassword = __state[1];
        }
    }

    internal static class RussianPuzzleInputAliases
    {
        internal static void Append(string title, List<string> answers, string localeCode)
        {
            if (localeCode != Plugin.RussianCode || answers == null)
                return;

            AddMissing(answers, "bigCatch", "большойУлов");
            AddMissing(answers, "dontForget", "неЗабудь");
            AddMissing(answers, "anglerTrio", "РыбацкоеТрио");
            AddMissing(answers, "cronos", "кронос");
            AddMissing(answers, "Stegosaurus", "стегозавр");
            AddMissing(answers, "Cascadia", "Каскадия");
            // The Russian table localizes duplicate answer slots too. Restore the
            // exact source spellings here so English input remains accepted.
            AddMissing(answers, "стегозавр", "Stegosaurus");
            AddMissing(answers, "Каскадия", "Cascadia");
            AddMissing(answers, "Faiza", "Фаиза", "Файза");
            AddMissing(answers, "Файза", "Faiza", "Фаиза");
            AddMissing(answers, "Фаиза", "Faiza", "Файза");

            if (string.Equals(title, "Киновикторина: вопрос 1", StringComparison.Ordinal))
                AddMissing(answers, "the sweaters", "свитера", "свитеры");
            else if (string.Equals(title, "Киновикторина: вопрос 2", StringComparison.Ordinal))
            {
                AddMissing(answers, "infected", "зараженный", "инфицированный");
                AddMissing(answers, "undead", "оживший мертвец");
            }
            else if (string.Equals(title, "Киновикторина: вопрос 3", StringComparison.Ordinal))
                AddMissing(answers, "red", "красное", "красные");
            else if (string.Equals(title, "Киновикторина: вопрос 4", StringComparison.Ordinal))
                AddMissing(answers, "pineapplesummer", "лето ананаса", "летоананаса", "лето_ананаса", "лето-ананаса");
        }

        internal static void AppendFolderAliases(List<string> passwordKeys, List<string> answers, string localeCode)
        {
            if (localeCode != Plugin.RussianCode || passwordKeys == null || answers == null)
                return;

            bool isHanoiThirdQuestion = false;
            bool isHanoiFourthQuestion = false;
            bool isFatherSecurityQuestion = false;
            foreach (string key in passwordKeys)
            {
                if (string.Equals(key, "pwd_admin_shame", StringComparison.Ordinal))
                    AddMissing(answers, "TAROCCHI", "тарокки");
                else if (key != null && key.StartsWith("SecurityAnswers_P9Hanoi_Q3A", StringComparison.Ordinal))
                    isHanoiThirdQuestion = true;
                else if (key != null && key.StartsWith("SecurityAnswers_P9Hanoi_Q4A", StringComparison.Ordinal))
                    isHanoiFourthQuestion = true;
                else if (key != null && key.StartsWith("SecurityAnswers_P9Hanoi_Achievement", StringComparison.Ordinal))
                    isFatherSecurityQuestion = true;
            }

            if (isFatherSecurityQuestion)
            {
                AddMissing(answers, "dad", "папа");
                foreach (string source in new[] { "mydad", "my dad", "my-dad", "my_dad" })
                    AddMissing(answers, source, "мой папа", "мойпапа", "мой_папа", "мой-папа");
            }

            if (isHanoiThirdQuestion)
                AddMissing(answers, "fire", "огонь");

            if (!isHanoiFourthQuestion)
                return;

            AddMissing(answers, "mybrother", "мойбрат", "мой брат", "мой_брат", "мой-брат");
            AddMissing(answers, "my brother", "мойбрат", "мой брат", "мой_брат", "мой-брат");
            AddMissing(answers, "my_brother", "мойбрат", "мой брат", "мой_брат", "мой-брат");
            AddMissing(answers, "my-brother", "мойбрат", "мой брат", "мой_брат", "мой-брат");
            AddMissing(answers, "bigbrother", "старшийбрат", "старший брат", "старший_брат", "старший-брат");
            AddMissing(answers, "big brother", "старшийбрат", "старший брат", "старший_брат", "старший-брат");
            AddMissing(answers, "big-brother", "старшийбрат", "старший брат", "старший_брат", "старший-брат");
            AddMissing(answers, "big_brother", "старшийбрат", "старший брат", "старший_брат", "старший-брат");
            AddMissing(answers, "brother", "брат");
            AddMissing(answers, "walleye", "судак");
            AddMissing(answers, "walter", "уолтер");
            AddMissing(answers, "walterwaters", "уолтер уотерс", "уолтеруотерс", "уолтер_уотерс", "уолтер-уотерс");
            AddMissing(answers, "walter_waters", "уолтер уотерс", "уолтеруотерс", "уолтер_уотерс", "уолтер-уотерс");
            AddMissing(answers, "walter-waters", "уолтер уотерс", "уолтеруотерс", "уолтер_уотерс", "уолтер-уотерс");
            AddMissing(answers, "walter waters", "уолтер уотерс", "уолтеруотерс", "уолтер_уотерс", "уолтер-уотерс");
            AddMissing(answers, "waltersounder", "уолтер саундер", "уолтерсаундер", "уолтер_саундер", "уолтер-саундер");
            AddMissing(answers, "walter_sounder", "уолтер саундер", "уолтерсаундер", "уолтер_саундер", "уолтер-саундер");
            AddMissing(answers, "walter-sounder", "уолтер саундер", "уолтерсаундер", "уолтер_саундер", "уолтер-саундер");
            AddMissing(answers, "walter sounder", "уолтер саундер", "уолтерсаундер", "уолтер_саундер", "уолтер-саундер");
        }

        internal static void AppendRawFolderPasswordAliases(string filePath, List<string> answers, string localeCode)
        {
            if (localeCode != Plugin.RussianCode || answers == null)
                return;

            // Keep each source answer and add Russian forms only to its matching folder.
            // Keep localized spellings attached to the exact answer folder that owns them.
            switch (filePath)
            {
                case "prologue/puzzle1.folder":
                    AddMissing(answers, "fortuna", "Фортуна");
                    break;
                case "puzzle1/puzzle2.folder":
                    AddMissing(answers, "inanna", "Инанна");
                    AddMissing(answers, "n3kropoli5", "некрополь", "некрополис");
                    break;
                case "puzzle2/puzzle3.folder":
                    AddMissing(answers, "elephas", "Элефас", "слон");
                    AddMissing(answers, "defi4nc3", "непокорность", "неповиновение");
                    break;
                case "puzzleA1/puzzleA_End.folder":
                    AddMissing(answers, "jaws", "челюсти");
                    break;
                case "magicStore/glassBox.folder":
                    AddMissing(answers, "cronos", "Кронос");
                    break;
                case "puzzle3/puzzle4.folder":
                    AddMissing(answers, "salve", "привет");
                    break;
                case "puzzle5/puzzle6.folder":
                    AddMissing(answers, "mmtt", "ММТТ");
                    break;
                case "puzzle6/puzzle7.folder":
                    AddMissing(answers, "kassandra", "Кассандра");
                    break;
                case "puzzle1/puzzle9.folder":
                    AddMissing(answers, "rubicon", "Рубикон");
                    break;
                case "puzzle9/puzzle9.folder":
                    AddMissing(answers, "fito", "Фито");
                    break;
                case "puzzle4/puzzle5.folder":
                    AddMissing(answers, "portonus", "Портонус");
                    break;
                case "puzzleA1/puzzleA2.folder":
                    AddMissing(answers, "ammit", "Аммит");
                    break;
                case "puzzleR1/puzzleR2.folder":
                    AddMissing(answers, "SAHMAT", "САХМАТ");
                    break;
                case "puzzleR2/puzzleR_lockbox.folder":
                    AddMissing(answers, "merneith", "Мернейт", "Меретнейт");
                    break;
                case "podiumChamber/puzzleA1.folder":
                    AddMissing(answers, "Anubis", "Анубис");
                    break;
                case "podiumChamber/puzzleT1.folder":
                    AddMissing(answers, "Thoth", "Тот");
                    break;
                case "podiumChamber/puzzleR1.folder":
                    AddMissing(answers, "Ra", "Ра");
                    break;
                case "templeIntro/podiumChamber.folder":
                    AddMissing(answers, "shai", "Шай");
                    break;
                case "desktopExplorer/diveIn.folder":
                    AddMissing(answers, "theseus", "Тесей");
                    break;
                case "desktop/desktopExplorer.folder":
                case "desktop/openMe.folder":
                    AddMissing(answers, "guppy", "Гуппи");
                    break;
                case "desktop/templeIntro.folder":
                    AddMissing(answers, "demo", "демо");
                    break;
                case "example/nextPuzzle.folder":
                    AddMissing(answers, "password123", "пароль123", "пароль 123");
                    break;
                case "tutorial/tutorial2.folder":
                    AddMissing(answers, "traveler", "путник", "путешественник");
                    break;
                case "tutorial2/puzzle1.folder":
                    AddMissing(answers, "catacomb", "катакомба", "катакомбы");
                    break;
                case "puzzleR2/puzzleR_End.folder":
                    AddMissing(answers, "merneith", "Мернейт", "Меретнейт");
                    break;
            }
        }

        private static void AddMissing(List<string> answers, string sourceAnswer, params string[] aliases)
        {
            bool isTargetQuestion = false;
            foreach (string answer in answers)
                if (string.Equals(answer, sourceAnswer, StringComparison.OrdinalIgnoreCase))
                {
                    isTargetQuestion = true;
                    break;
                }
            if (!isTargetQuestion)
                return;

            foreach (string alias in aliases)
            {
                bool exists = false;
                foreach (string answer in answers)
                    if (string.Equals(answer, alias, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                if (!exists)
                    answers.Add(alias);
            }
        }
    }

    [HarmonyPatch]
    internal static class RussianPhotoBoothInputAliasesPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(
            typeof(InputDialogController),
            nameof(InputDialogController.InitializeAsPasswordDialog),
            new[]
            {
                typeof(string), typeof(List<string>), typeof(Action<string>), typeof(Action<string>),
                typeof(string), typeof(string), typeof(bool), typeof(Sprite)
            });

        private static void Prefix(string title, ref List<string> passwords)
        {
            string localeCode = LocalizationSettings.SelectedLocale?.Identifier.Code;
            if (localeCode != Plugin.RussianCode || passwords == null)
                return;

            // Some callers pass a controller's serialized list (for example the glass box).
            // The dialog owns aliases; its caller must retain the original language-neutral answers.
            var dialogAnswers = new List<string>(passwords);
            RussianPuzzleInputAliases.Append(title, dialogAnswers, localeCode);
            if (dialogAnswers.Count != passwords.Count)
                passwords = dialogAnswers;
        }
    }

    [HarmonyPatch]
    internal static class RussianFolderPasswordAliasesPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(
            typeof(InputDialogController), "PasswordLocKeyReader", new[] { typeof(List<string>) });

        private static void Postfix(List<string> passwordArrayOfLocKeys, ref UniTask<List<string>> __result)
        {
            string localeCode = LocalizationSettings.SelectedLocale?.Identifier.Code;
            if (localeCode == Plugin.RussianCode)
                __result = AppendAliases(__result, passwordArrayOfLocKeys, localeCode);
        }

        private static async UniTask<List<string>> AppendAliases(
            UniTask<List<string>> operation, List<string> passwordKeys, string localeCode)
        {
            List<string> answers = await operation;
            RussianPuzzleInputAliases.AppendFolderAliases(passwordKeys, answers, localeCode);
            return answers;
        }
    }

    [HarmonyPatch]
    internal static class RussianRawFolderPasswordAliasesPatch
    {
        private static readonly FieldInfo FolderToOpen = AccessTools.Field(typeof(InputDialogController), "fileToOpen");
        private static readonly FieldInfo CorrectPasswords = AccessTools.Field(typeof(InputDialogController), "correctPasswords");

        private static MethodBase TargetMethod() => AccessTools.Method(typeof(InputDialogController), "SubmitInput");

        private static void Prefix(InputDialogController __instance)
        {
            if (LocalizationSettings.SelectedLocale?.Identifier.Code != Plugin.RussianCode || __instance == null)
                return;

            if (FolderToOpen?.GetValue(__instance) is Models.DEFolder folder &&
                CorrectPasswords?.GetValue(__instance) is List<string> answers)
            {
                var dialogAnswers = new List<string>(answers);
                RussianPuzzleInputAliases.AppendRawFolderPasswordAliases(
                    folder.file.GetFilepath(), dialogAnswers, Plugin.RussianCode);
                if (dialogAnswers.Count != answers.Count)
                    CorrectPasswords.SetValue(__instance, dialogAnswers);
            }
        }
    }
}
