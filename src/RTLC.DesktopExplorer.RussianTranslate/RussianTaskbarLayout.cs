using Cysharp.Threading.Tasks;
using HarmonyLib;
using Program.Terminal;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Windows;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(global::TaskbarNotificationController), "ShowAndFadeOut")]
    internal static class RussianTaskbarNotificationLayout
    {
        private static void Postfix(global::TaskbarNotificationController __instance, string text,
            TMP_Text ___notificationText)
        {
            if (LocalizationSettings.SelectedLocale?.Identifier.Code != Plugin.RussianCode ||
                !__instance || !___notificationText || string.IsNullOrEmpty(text))
                return;

            var bubble = __instance.transform as RectTransform;
            var labelRect = ___notificationText.rectTransform;
            if (!bubble || !labelRect)
                return;

            Canvas.ForceUpdateCanvases();
            float currentLabelWidth = labelRect.rect.width;
            float currentBubbleWidth = bubble.rect.width;
            if (currentLabelWidth <= 0f || currentBubbleWidth <= 0f)
                return;

            float preferredWidth = ___notificationText.GetPreferredValues(text,
                float.PositiveInfinity, float.PositiveInfinity).x;
            if (preferredWidth <= currentLabelWidth)
                return;

            // Keep the authored padding and grow the bubble toward its anchored
            // side so long Russian track names stay on one line at the original
            // font size instead of running outside the notification.
            float padding = Mathf.Max(0f, currentBubbleWidth - currentLabelWidth);
            float expandedLabelWidth = Mathf.Ceil(preferredWidth + 4f);
            labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, expandedLabelWidth);
            bubble.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, expandedLabelWidth + padding);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubble);
            if (bubble.parent is RectTransform parent)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            Canvas.ForceUpdateCanvases();
        }
    }

    [HarmonyPatch(typeof(TerminalController), nameof(TerminalController.PrintOutput))]
    internal static class ClosedTerminalOutputGuard
    {
        private static bool Prefix(TerminalController __instance,
            ref UniTask<TerminalOutputController> __result)
        {
            var lineLayout = AccessTools.Field(typeof(TerminalController), "currentLineLayout")
                ?.GetValue(__instance) as VerticalLayoutGroup;
            var outputPrefab = AccessTools.Field(typeof(TerminalController), "terminalOutput")
                ?.GetValue(__instance) as GameObject;
            if (__instance && lineLayout && outputPrefab)
                return true;

            // Async command-output callbacks can outlive a Tower window during a
            // desktop transition. Dropping output after the owning layout is gone
            // avoids dereferencing its destroyed transform during teardown.
            __result = UniTask.FromResult<TerminalOutputController>(null);
            return false;
        }
    }
}
