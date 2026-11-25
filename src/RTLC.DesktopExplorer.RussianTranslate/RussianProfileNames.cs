using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using System.Reflection;
using Login;
using Progress;
using StartMenu;
using TMPro;
using UnityEngine;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianProfileNames
    {
        private static readonly System.Reflection.FieldInfo UsernameLabel =
            AccessTools.Field(typeof(LoginUserController), "usernameLabel");
        private static readonly FieldInfo UserControllers =
            AccessTools.Field(typeof(LoginController), "userControllers");
        private static readonly ConditionalWeakTable<TMP_Text, LoginUserController> BoundControllers =
            new ConditionalWeakTable<TMP_Text, LoginUserController>();
        private static readonly HashSet<string> KnownProfileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Halibut", "Maximus", "M4ximus", "M4X1MUS", "Admin", "Guppy"
        };
        private static readonly HashSet<string> MaximusNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Maximus", "M4ximus", "M4X1MUS"
        };

        internal static string Translate(TMP_Text owner, string source)
        {
            if (owner == null || !LooksLikeProfileName(source)) return source;
            var controller = FindController(owner);
            return controller == null ? source : Translate(controller.GetUser(), source, Plugin.RussianCode);
        }

        internal static string Translate(DEUser user, string source, string localeCode)
        {
            if (localeCode != Plugin.RussianCode || source == null) return source;
            string name = TrimLineBreaks(source);
            string suffix = source.Substring(name.Length);
            string translated = user switch
            {
                DEUser.Halibut or DEUser.GHalibut when string.Equals(name, "Halibut", StringComparison.OrdinalIgnoreCase) => "Палтус",
                DEUser.Maximus or DEUser.GMaximus when MaximusNames.Contains(name) => "М4кс1мус",
                DEUser.Admin or DEUser.GAdmin when string.Equals(name, "Admin", StringComparison.OrdinalIgnoreCase) => "Админ",
                DEUser.Guppy when string.Equals(name, "Guppy", StringComparison.OrdinalIgnoreCase) => "Гуппи",
                _ => name
            };
            return translated == name ? source : translated + suffix;
        }

        private static bool LooksLikeProfileName(string source) =>
            source != null && KnownProfileNames.Contains(TrimLineBreaks(source));

        private static string TrimLineBreaks(string source) => source.TrimEnd('\r', '\n');

        private static LoginUserController FindController(TMP_Text owner)
        {
            if (BoundControllers.TryGetValue(owner, out var bound) && bound != null)
                return bound;

            var parent = owner.GetComponentInParent<LoginUserController>(true);
            if (OwnsLabel(parent, owner))
            {
                Bind(owner, parent);
                return parent;
            }

            // The login cards serialize their usernameLabel reference on the card
            // controller, but the label itself is a sibling in the scene hierarchy.
            // Match that serialized reference so profile localization stays scoped.
            foreach (var controller in Resources.FindObjectsOfTypeAll<LoginUserController>())
                if (controller && OwnsLabel(controller, owner))
                {
                    Bind(owner, controller);
                    return controller;
                }
            return null;
        }

        private static void Bind(TMP_Text label, LoginUserController controller)
        {
            if (label == null || controller == null) return;
            BoundControllers.Remove(label);
            BoundControllers.Add(label, controller);
        }

        private static bool OwnsLabel(LoginUserController controller, TMP_Text owner) =>
            controller != null && UsernameLabel != null && ReferenceEquals(UsernameLabel.GetValue(controller), owner);

        internal static bool IsProfileName(TMP_Text owner, string source)
        {
            if (owner == null || !LooksLikeProfileName(source)) return false;
            var controller = FindController(owner);
            if (controller == null) return false;
            return Translate(controller.GetUser(), source, Plugin.RussianCode) != source;
        }

        internal static void Refresh(LoginUserController controller)
        {
            if (controller == null) return;
            var label = (TMP_Text)UsernameLabel?.GetValue(controller);
            Bind(label, controller);
            RussianHardcodedText.Apply(label);
        }

        internal static void RefreshAll(LoginController controller)
        {
            if (controller == null) return;
            if (UserControllers?.GetValue(controller) is IEnumerable<LoginUserController> configuredControllers)
                foreach (var userController in configuredControllers)
                    Refresh(userController);

            foreach (var userController in Resources.FindObjectsOfTypeAll<LoginUserController>())
                if (userController && userController.gameObject.scene == controller.gameObject.scene)
                    Refresh(userController);
        }
    }

    [HarmonyPatch(typeof(LoginUserController), "Awake")]
    internal static class RussianProfileNameCardAwakePatch
    {
        private static void Postfix(LoginUserController __instance) => RussianProfileNames.Refresh(__instance);
    }

    [HarmonyPatch(typeof(LoginController), "Start")]
    internal static class RussianProfileNameInitialScreenPatch
    {
        private static void Postfix(LoginController __instance) => RussianProfileNames.RefreshAll(__instance);
    }

    [HarmonyPatch(typeof(LoginUserController), "SelectUser")]
    internal static class RussianProfileNameSelectPatch
    {
        private static void Postfix(LoginUserController __instance) => RussianProfileNames.Refresh(__instance);
    }

    [HarmonyPatch(typeof(LoginUserController), "DeselectUser")]
    internal static class RussianProfileNameDeselectPatch
    {
        private static void Postfix(LoginUserController __instance) => RussianProfileNames.Refresh(__instance);
    }

    [HarmonyPatch(typeof(StartMenuController), "PreConfigUserText")]
    internal static class RussianStartMenuProfileName
    {
        private static void Prefix(ref string userText)
        {
            if (!RussianHardcodedText.IsRussian || string.IsNullOrEmpty(userText) ||
                Progress.SaveDataManager.Instance == null)
                return;

            string source = Progress.SaveDataManager.Instance.GetCurrentUserString();
            if (string.IsNullOrEmpty(source) || !userText.EndsWith(source, StringComparison.Ordinal))
                return;

            string translated = RussianProfileNames.Translate(
                Progress.SaveDataManager.Instance.GetCurrentUser(), source, Plugin.RussianCode);
            if (translated != source)
                userText = userText.Substring(0, userText.Length - source.Length) + translated;
        }
    }
}
