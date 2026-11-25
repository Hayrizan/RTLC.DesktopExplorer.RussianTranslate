using System.Runtime.CompilerServices;
using HarmonyLib;
using Progress.Puzzles.Guppy.GHalibut;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianNextSocialLayout
    {
        private sealed class OriginalLayout
        {
            internal float ButtonWidth;
            internal float LabelWidth;
        }

        private static readonly ConditionalWeakTable<Button, OriginalLayout> OriginalLayouts =
            new ConditionalWeakTable<Button, OriginalLayout>();
        private static readonly System.Reflection.FieldInfo SendButtonField = AccessTools.Field(
            typeof(NextSocialController), "sendButton");

        internal static void Apply(NextSocialController controller)
        {
            if (!controller || !(SendButtonField?.GetValue(controller) is Button button) || !button)
                return;

            var buttonRect = button.transform as RectTransform;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            var labelRect = label ? label.rectTransform : null;
            if (!buttonRect || !label || !labelRect)
                return;

            if (!OriginalLayouts.TryGetValue(button, out var original))
            {
                original = new OriginalLayout
                {
                    ButtonWidth = buttonRect.rect.width,
                    LabelWidth = labelRect.rect.width
                };
                OriginalLayouts.Add(button, original);
            }

            if (LocalizationSettings.SelectedLocale?.Identifier.Code != Plugin.RussianCode)
            {
                buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, original.ButtonWidth);
                if (labelRect != buttonRect)
                    labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, original.LabelWidth);
                return;
            }

            label.ForceMeshUpdate();
            float requiredLabelWidth = label.GetPreferredValues(label.text, float.PositiveInfinity,
                float.PositiveInfinity).x + label.margin.x + label.margin.z;
            const float horizontalPadding = 16f;
            float buttonWidth = Mathf.Max(original.ButtonWidth, requiredLabelWidth + horizontalPadding);
            buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, buttonWidth);
            if (labelRect != buttonRect)
                labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    Mathf.Max(original.LabelWidth, buttonWidth - horizontalPadding));
        }
    }

    [HarmonyPatch(typeof(NextSocialController), "OnLocalizedTextChanged")]
    internal static class RussianNextSocialLayoutPatch
    {
        private static void Postfix(NextSocialController __instance) => RussianNextSocialLayout.Apply(__instance);
    }
}
