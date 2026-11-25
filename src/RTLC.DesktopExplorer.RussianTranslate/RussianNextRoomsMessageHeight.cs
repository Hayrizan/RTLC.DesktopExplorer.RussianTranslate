using HarmonyLib;
using Program.Mall.UI;
using TMPro;
using UnityEngine;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(NextRoomsUILabelController), nameof(NextRoomsUILabelController.SetupNextRoomsLabel))]
    internal static class RussianNextRoomsMessageHeight
    {
        private static void Postfix(TMP_Text ___labelText, RectTransform ___rectTransform)
        {
            if (!RussianHardcodedText.IsRussian || !___labelText || !___rectTransform) return;
            float width = ___labelText.rectTransform.rect.width;
            if (width <= 0) width = ___rectTransform.rect.width;
            float required = ___labelText.GetPreferredValues(___labelText.text, width, Mathf.Infinity).y + 32f;
            ___rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(___rectTransform.rect.height, required));
        }
    }
}
