using HarmonyLib;
using Program.Mall.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(NextRoomOptionController), nameof(NextRoomOptionController.SetOptionWidth))]
    internal static class RussianNextRoomsOptionWidth
    {
        private static void Prefix(ref float width, TMP_Text ___optionLabel, TMP_Text ___goLabel,
            bool ___shouldShowGoLabel, HorizontalLayoutGroup ___horizontalLayoutGroup)
        {
            if (!RussianHardcodedText.IsRussian || !___shouldShowGoLabel || !___optionLabel || !___goLabel) return;
            float goWidth = ___goLabel.GetPreferredValues(___goLabel.text).x + 16f;
            float labelWidth = ___optionLabel.GetPreferredValues(___optionLabel.text).x;
            float left = ___horizontalLayoutGroup ? ___horizontalLayoutGroup.padding.left : 0f;
            width = Mathf.Max(width, left + labelWidth + goWidth + 64f);
            if (___horizontalLayoutGroup)
            {
                var padding = ___horizontalLayoutGroup.padding;
                ___horizontalLayoutGroup.padding = new RectOffset(padding.left,
                    Mathf.Max(padding.right, Mathf.CeilToInt(goWidth + 48f)), padding.top, padding.bottom);
            }
        }
    }
}
