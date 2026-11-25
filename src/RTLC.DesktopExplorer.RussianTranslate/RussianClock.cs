using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Program;
using TMPro;
using UnityEngine;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianCalendarLabels
    {
        private static readonly Dictionary<string, string> MonthNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["January"] = "янв.", ["February"] = "февр.", ["March"] = "мар.", ["April"] = "апр.",
            ["May"] = "май", ["June"] = "июнь", ["July"] = "июль", ["August"] = "авг.",
            ["September"] = "сент.", ["October"] = "окт.", ["November"] = "нояб.", ["December"] = "дек."
        };
        private static readonly Dictionary<string, string> WeekdayNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Sunday"] = "воскресенье", ["Monday"] = "понедельник", ["Tuesday"] = "вторник",
            ["Wednesday"] = "среда", ["Thursday"] = "четверг", ["Friday"] = "пятница", ["Saturday"] = "суббота"
        };

        internal static string Translate(TMP_Text owner, string value)
        {
            if (owner == null || string.IsNullOrEmpty(value)) return value;
            if (owner.gameObject.name == "MonthLabel" && MonthNames.TryGetValue(value, out string month))
                return month;
            if (owner.gameObject.name == "DayLabel" && WeekdayNames.TryGetValue(value, out string day))
                return day;
            return value;
        }
    }

    [HarmonyPatch(typeof(ClockDisplay), "FormatClockTime")]
    internal static class RussianClockFormat
    {
        internal static void Refresh()
        {
            foreach (var clock in UnityEngine.Object.FindObjectsOfType<ClockDisplay>())
            {
                if (clock.IsAnimating) continue;
                var value = clock.IsTimeOverriden ? clock.OverridenDateTime : Progress.SaveDataManager.Instance.GetCurrentDateTime();
                var text = (TMPro.TMP_Text)AccessTools.Field(typeof(ClockDisplay), "clockText").GetValue(clock);
                text.text = value.ToString(RussianHardcodedText.IsRussian ? "HH:mm" : "hh:mm tt", CultureInfo.InvariantCulture);
                AccessTools.Method(typeof(ClockDisplay), "ResizeToFitText").Invoke(clock, null);
            }
        }

        private static void Postfix(DateTime dt, ref string __result)
        {
            if (RussianHardcodedText.IsRussian)
                __result = dt.ToString("HH:mm", CultureInfo.InvariantCulture);
        }
    }

    [HarmonyPatch(typeof(TimeSettingsController), "UpdateTime")]
    internal static class RussianTimeSettings
    {
        private sealed class Layout
        {
            public Vector2 ClockPosition, DividerPosition;
        }
        private static readonly ConditionalWeakTable<TimeSettingsController, Layout> layouts =
            new ConditionalWeakTable<TimeSettingsController, Layout>();

        private static void Postfix(TimeSettingsController __instance, bool ___isTimeOverriden,
            DateTime ___overrideDateTime, DateTime ___localDateTime)
        {
            if (RussianHardcodedText.IsRussian)
            {
                DateTime displayed = ___isTimeOverriden ? ___overrideDateTime : ___localDateTime;
                __instance.hourDigitLabel.text = displayed.Hour.ToString("D2", CultureInfo.InvariantCulture);
            }
            ApplyLayout(__instance, RussianHardcodedText.IsRussian);
        }

        internal static void ApplyLayout(TimeSettingsController instance, bool russian)
        {
            RussianClockApplyButton.Refresh(instance, russian);
            // HourStepperChange already wraps 0..23 and publishes the original
            // DateTime. Story events, frozen clocks and analog hands stay native.
            var stepper = instance.amPmLabel.transform.parent.parent.gameObject;
            stepper.SetActive(!russian);
            var row = (RectTransform)stepper.transform.parent;
            var section = (RectTransform)row.parent;
            var divider = section.Find("HourMinuteDivider") as RectTransform;
            if (!divider) return;
            if (section.rect.width <= 0f || row.rect.width <= 0f)
            {
                Canvas.ForceUpdateCanvases();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(section);
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(row);
            }
            if (!layouts.TryGetValue(instance, out var saved))
            {
                saved = new Layout { ClockPosition = row.anchoredPosition, DividerPosition = divider.anchoredPosition };
                layouts.Add(instance, saved);
            }
            var horizontalLayout = row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (horizontalLayout != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(row);
            // Center the remaining controls; the colon lives outside the layout.
            var hour = (RectTransform)instance.hourDigitLabel.transform.parent.parent;
            var minute = (RectTransform)instance.minuteDigitLabel.transform.parent.parent;
            var group = horizontalLayout;
            if (group == null) return;
            float width = hour.rect.width + minute.rect.width + group.spacing;
            float shift = russian ? Mathf.Max(0, (section.rect.width - width) * 0.5f) : 0f;
            row.anchoredPosition = saved.ClockPosition + new Vector2(shift, 0);
            divider.anchoredPosition = saved.DividerPosition + new Vector2(shift, 0);
        }
    }

    internal static class RussianClockApplyButton
    {
        private sealed class Original
        {
            public Vector2 ButtonSize, LabelSize;
            public float MinWidth, PreferredWidth;
        }
        private static readonly ConditionalWeakTable<UnityEngine.UI.Button, Original> originals =
            new ConditionalWeakTable<UnityEngine.UI.Button, Original>();
        internal static void Refresh(TimeSettingsController instance, bool russian)
        {
            var button = AccessTools.Field(typeof(TimeSettingsController), "applyBtn").GetValue(instance) as UnityEngine.UI.Button;
            if (!button) return;
            var rect = button.transform as RectTransform;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (!rect || !label) return;
            var element = button.GetComponent<UnityEngine.UI.LayoutElement>();
            var original = originals.GetValue(button, value => new Original {
                ButtonSize = rect.sizeDelta, LabelSize = label.rectTransform.sizeDelta,
                MinWidth = element ? element.minWidth : -1f,
                PreferredWidth = element ? element.preferredWidth : -1f });
            if (!russian)
            {
                rect.sizeDelta = original.ButtonSize;
                label.rectTransform.sizeDelta = original.LabelSize;
                if (element) { element.minWidth = original.MinWidth; element.preferredWidth = original.PreferredWidth; }
                return;
            }
            float textWidth = Mathf.Ceil(label.GetPreferredValues(label.text, float.PositiveInfinity, float.PositiveInfinity).x);
            float width = Mathf.Max(rect.rect.width, textWidth + 24f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 16f);
            if (element) { element.minWidth = width; element.preferredWidth = width; }
        }
    }

    [HarmonyPatch(typeof(TimeSettingsController), "SetupTimeSettings")]
    internal static class RussianTimeSettingsFirstFrame
    {
        private static void Postfix(TimeSettingsController __instance) =>
            RussianTimeSettings.ApplyLayout(__instance, RussianHardcodedText.IsRussian);
    }
}
