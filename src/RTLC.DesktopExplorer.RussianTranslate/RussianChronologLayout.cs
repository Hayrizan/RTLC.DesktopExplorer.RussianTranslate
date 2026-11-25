using System.Runtime.CompilerServices;
using HarmonyLib;
using Program.Chronolog;
using Program.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianChronologLayout
    {
        private sealed class State
        {
            public float OriginalHeight;
        }

        private static readonly ConditionalWeakTable<JournalTabController, State> states =
            new ConditionalWeakTable<JournalTabController, State>();

        internal static void Apply(JournalTabController tab)
        {
            if (!tab) return;
            var label = AccessTools.Field(typeof(JournalTabController), "tabTitle")?.GetValue(tab) as TMP_Text;
            if (!label) return;

            var rect = label.rectTransform;
            var state = states.GetValue(tab, _ => new State { OriginalHeight = rect.rect.height });
            if (!RussianHardcodedText.IsRussian)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, state.OriginalHeight);
                return;
            }

            label.ForceMeshUpdate();
            float preferredHeight = label.GetPreferredValues(label.text, float.PositiveInfinity,
                float.PositiveInfinity).y;
            float requiredHeight = Mathf.Max(32f, preferredHeight + label.margin.y + label.margin.w + 3f);
            if (Mathf.Abs(rect.rect.height - requiredHeight) > 0.5f)
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight);
            if (rect.parent is RectTransform parent)
                LayoutRebuilder.MarkLayoutForRebuild(parent);
        }
    }

    [HarmonyPatch(typeof(JournalTabController), "Start")]
    internal static class RussianChronologTabLayoutPatch
    {
        private static void Postfix(JournalTabController __instance) => RussianChronologLayout.Apply(__instance);
    }

    [HarmonyPatch(typeof(JournalTabController), nameof(JournalTabController.CustomizeTitle))]
    internal static class RussianChronologTabTitlePatch
    {
        private static void Postfix(JournalTabController __instance) => RussianChronologLayout.Apply(__instance);
    }

    [HarmonyPatch(typeof(TabButtonController), "SetSelected")]
    internal static class ChronologDestroyedTabButtonGuard
    {
        private static readonly System.Reflection.FieldInfo TabImage = AccessTools.Field(
            typeof(TabButtonController), "tabImage");
        private static readonly System.Reflection.FieldInfo BottomBorder = AccessTools.Field(
            typeof(TabButtonController), "bottomBorder");

        private static bool Prefix(TabButtonController __instance)
        {
            // The elevator interlude can destroy a tab's UI while the async tab
            // selection is still walking the serialized button list. Skip only a
            // button whose native image/border reference has already been released.
            return __instance && TabImage?.GetValue(__instance) is Image image && image &&
                BottomBorder?.GetValue(__instance) is Behaviour border && border;
        }
    }
}
