using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Settings;
using StartMenu;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Windows;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(StartMenuController), "PreConfigUserText")]
    internal static class RussianStartMenuLayoutPatch
    {
        private static void Postfix(StartMenuController __instance)
        {
            var layout = __instance.GetComponent<RussianStartMenuLayout>();
            if (layout == null) layout = __instance.gameObject.AddComponent<RussianStartMenuLayout>();
            layout.Initialize(__instance);
        }
    }

    internal sealed class RussianStartMenuLayout : MonoBehaviour
    {
        private sealed class ButtonState
        {
            public RectTransform Rect;
            public RectTransform IconRect;
            public float Width;
            public Vector3 Scale;
            public LayoutElement Element;
            public float PreferredWidth, MinWidth;
            public TMP_Text Label;
            public RectTransform LabelRect;
            public float LabelWidth;
            public Vector2 LabelPosition;
            public float LabelFontSize, LabelFontSizeMin, LabelFontSizeMax;
            public bool LabelAutoSizing;
        }

        private static readonly string[] ButtonFields =
        {
            "shutDownButton", "logOffButton", "settingsButton", "chronologButton", "documentsButton"
        };
        private static readonly System.Reflection.FieldInfo Background = AccessTools.Field(typeof(StartMenuController), "backgroundImage");
        private static readonly System.Reflection.FieldInfo Programs = AccessTools.Field(typeof(StartMenuController), "defaultProgramRows");
        private StartMenuController controller;
        private RectTransform rootRect, backgroundRect, sidebarRect, rowLayoutRect;
        private float rootWidth, backgroundWidth;
        private float rowLayoutWidth;
        private Vector2 rootPosition, backgroundPosition;
        private Vector2 rowLayoutPosition;
        private ButtonState[] buttonStates;
        private string signature;
        private bool russianApplied;

        internal void Initialize(StartMenuController value)
        {
            if (controller == value && buttonStates != null) return;
            controller = value;
            rootRect = (RectTransform)value.transform;
            rootWidth = rootRect.rect.width;
            rootPosition = rootRect.anchoredPosition;
            sidebarRect = rootRect.Find("StartMenuSidebar") as RectTransform;
            rowLayoutRect = rootRect.Find("StartMenuRowLayout") as RectTransform;
            if (rowLayoutRect != null)
            {
                rowLayoutWidth = rowLayoutRect.rect.width;
                rowLayoutPosition = rowLayoutRect.anchoredPosition;
            }
            var background = (Image)Background?.GetValue(value);
            backgroundRect = background ? background.rectTransform : null;
            if (backgroundRect != null)
            {
                backgroundWidth = backgroundRect.rect.width;
                backgroundPosition = backgroundRect.anchoredPosition;
            }
            buttonStates = GetButtons(value).Distinct().Where(button => button && button.transform is RectTransform)
                .Select(button =>
                {
                    var rect = (RectTransform)button.transform;
                    var element = button.GetComponent<LayoutElement>();
                    var label = button.GetComponentInChildren<TMP_Text>(true);
                    return new ButtonState
                    {
                        Rect = rect,
                        IconRect = rect.Find("IconImage") as RectTransform,
                        Width = rect.rect.width,
                        Scale = rect.localScale,
                        Element = element,
                        PreferredWidth = element ? element.preferredWidth : -1,
                        MinWidth = element ? element.minWidth : -1,
                        Label = label,
                        LabelRect = label ? label.rectTransform : null,
                        LabelWidth = label ? label.rectTransform.rect.width : 0f,
                        LabelPosition = label ? label.rectTransform.anchoredPosition : Vector2.zero,
                        LabelFontSize = label ? label.fontSize : 0f,
                        LabelFontSizeMin = label ? label.fontSizeMin : 0f,
                        LabelFontSizeMax = label ? label.fontSizeMax : 0f,
                        LabelAutoSizing = label && label.enableAutoSizing
                    };
                }).ToArray();
            signature = null;
        }

        private void LateUpdate()
        {
            if (controller == null || rootRect == null || buttonStates == null) return;
            bool russian = LocalizationSettings.SelectedLocale?.Identifier.Code == Plugin.RussianCode;
            if (!russian)
            {
                if (russianApplied) Restore();
                signature = null;
                return;
            }

            string current = string.Join("|", buttonStates.Select(state => state.Label ? state.Label.text : string.Empty));
            if (russianApplied && current == signature && !NeedsLayoutRepair()) return;
            Apply(current);
        }

        private bool NeedsLayoutRepair()
        {
            float requiredWidth = buttonStates.Where(state => state.Label && state.LabelRect)
                .Select(RequiredButtonWidth).DefaultIfEmpty(0f).Max();
            float currentRowWidth = rowLayoutRect ? rowLayoutWidth :
                buttonStates.Select(state => state.Width).DefaultIfEmpty(0f).Max();
            float targetWidth = Mathf.Ceil(Mathf.Max(currentRowWidth, requiredWidth + 48f) / 8f) * 8f;
            float sidebarRight = sidebarRect ? LocalBoundsMaxX(rootRect, sidebarRect) : 0f;
            const float gutter = 8f;
            const float rightMargin = 24f;
            float targetRootWidth = Mathf.Ceil((sidebarRight + gutter + targetWidth + rightMargin) / 8f) * 8f;
            if (Mathf.Abs(rootRect.rect.width - targetRootWidth) > 1f ||
                (rowLayoutRect && (Mathf.Abs(rowLayoutRect.rect.width - targetWidth) > 1f ||
                    Mathf.Abs(LocalBoundsMinX(rootRect, rowLayoutRect) - (sidebarRight + gutter)) > 1f))) return true;

            foreach (var state in buttonStates)
            {
                if (!state.Rect || Mathf.Abs(state.Rect.rect.width - targetWidth) > 1f) return true;
                if ((state.Rect.localScale - state.Scale).sqrMagnitude > 0.0001f) return true;
                if (state.Label && (state.Label.enableAutoSizing ||
                    Mathf.Abs(state.Label.fontSize - state.LabelFontSize) > 0.1f)) return true;
                float preferred = state.Label && state.LabelRect
                    ? state.Label.GetPreferredValues(state.Label.text, float.PositiveInfinity, float.PositiveInfinity).x
                    : 0f;
                float labelWidth = Mathf.Max(state.LabelWidth, preferred + 4f);
                if (state.LabelRect && Mathf.Abs(state.LabelRect.rect.width - labelWidth) > 1f) return true;
                if (state.LabelRect)
                {
                    float labelAnchor = Mathf.Lerp(state.LabelRect.anchorMin.x, state.LabelRect.anchorMax.x,
                        state.LabelRect.pivot.x);
                    float labelX = -labelAnchor * targetWidth + 88f +
                        state.LabelRect.pivot.x * labelWidth;
                    state.Label.alignment = TextAlignmentOptions.MidlineLeft;
                    if (Mathf.Abs(state.LabelRect.anchoredPosition.x - labelX) > 1f) return true;
                }
                if (state.Element != null && (Mathf.Abs(state.Element.minWidth - targetWidth) > 1f ||
                    Mathf.Abs(state.Element.preferredWidth - targetWidth) > 1f)) return true;
            }
            return false;
        }

        private void Apply(string currentSignature)
        {
            // The vanilla panel reserves a very wide empty left side while its
            // right-anchored rows are only 240 units wide. Fit the panel around
            // the sidebar and a row wide enough for the longest full-size label.
            float requiredWidth = buttonStates.Where(state => state.Label && state.LabelRect)
                .Select(RequiredButtonWidth).DefaultIfEmpty(0f).Max();
            float currentRowWidth = rowLayoutRect ? rowLayoutRect.rect.width :
                buttonStates.Select(state => state.Width).DefaultIfEmpty(0f).Max();
            float targetWidth = Mathf.Ceil(Mathf.Max(currentRowWidth, requiredWidth + 48f) / 8f) * 8f;
            float sidebarRight = sidebarRect ? LocalBoundsMaxX(rootRect, sidebarRect) : 0f;
            const float gutter = 8f;
            const float rightMargin = 24f;
            float targetRootWidth = Mathf.Ceil((sidebarRight + gutter + targetWidth + rightMargin) / 8f) * 8f;
            float rootDelta = targetRootWidth - rootWidth;
            rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetRootWidth);
            rootRect.anchoredPosition = rootPosition + new Vector2(rootRect.pivot.x * rootDelta, 0f);
            if (backgroundRect != null && backgroundRect != rootRect)
            {
                backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetRootWidth);
                backgroundRect.anchoredPosition = backgroundPosition;
            }
            if (rowLayoutRect)
            {
                rowLayoutRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                float currentLeft = LocalBoundsMinX(rootRect, rowLayoutRect);
                rowLayoutRect.anchoredPosition += new Vector2(sidebarRight + gutter - currentLeft, 0f);
            }
            foreach (var state in buttonStates)
            {
                state.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                // The stock button Animator scales the entire row on hover.
                // This makes long Russian labels and their icons visibly collapse.
                // Keep the original row scale; the native color highlight remains.
                state.Rect.localScale = state.Scale;
                if (state.Label)
                {
                    state.Label.enableAutoSizing = false;
                    state.Label.fontSize = state.LabelFontSize;
                }
                if (state.Element != null)
                {
                    state.Element.minWidth = targetWidth;
                    state.Element.preferredWidth = targetWidth;
                }
                if (state.LabelRect != null)
                {
                    float preferred = state.Label.GetPreferredValues(state.Label.text,
                        float.PositiveInfinity, float.PositiveInfinity).x;
                    float labelWidth = Mathf.Max(state.LabelWidth, preferred + 4f);
                    state.LabelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, labelWidth);
                    float labelAnchor = Mathf.Lerp(state.LabelRect.anchorMin.x, state.LabelRect.anchorMax.x,
                        state.LabelRect.pivot.x);
                    float labelX = -labelAnchor * targetWidth + 88f +
                        state.LabelRect.pivot.x * labelWidth;
                    state.Label.alignment = TextAlignmentOptions.MidlineLeft;
                    state.LabelRect.anchoredPosition = new Vector2(labelX, state.LabelPosition.y);
                }
            }
            LayoutRebuilder.MarkLayoutForRebuild(rootRect);
            if (rowLayoutRect) LayoutRebuilder.MarkLayoutForRebuild(rowLayoutRect);
            signature = currentSignature;
            russianApplied = true;
        }

        private static float RequiredButtonWidth(ButtonState state)
        {
            var label = state.LabelRect;
            float preferred = state.Label.GetPreferredValues(state.Label.text,
                float.PositiveInfinity, float.PositiveInfinity).x;
            float labelWidth = Mathf.Max(state.LabelWidth, preferred + 4f);
            if (!state.IconRect) return labelWidth + 28f;
            float iconAnchor = Mathf.Lerp(state.IconRect.anchorMin.x, state.IconRect.anchorMax.x,
                state.IconRect.pivot.x);
            float denominator = 1f - iconAnchor;
            if (denominator <= 0.01f) return state.Width;
            float iconRightFromLeftAnchor = state.IconRect.anchoredPosition.x +
                (1f - state.IconRect.pivot.x) * state.IconRect.rect.width;
            // Reserve a clear gap after the icon and a 20-unit inset at the
            // right edge; the row can then stay compact without clipping text.
            return (labelWidth + 20f + 8f + 8f + iconRightFromLeftAnchor) / denominator;
        }

        private static float LocalBoundsMaxX(RectTransform root, RectTransform child)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            return corners.Max(corner => root.InverseTransformPoint(corner).x);
        }

        private static float LocalBoundsMinX(RectTransform root, RectTransform child)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            return corners.Min(corner => root.InverseTransformPoint(corner).x);
        }

        private void Restore()
        {
            rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rootWidth);
            rootRect.anchoredPosition = rootPosition;
            if (backgroundRect != null && backgroundRect != rootRect)
            {
                backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, backgroundWidth);
                backgroundRect.anchoredPosition = backgroundPosition;
            }
            if (rowLayoutRect)
            {
                rowLayoutRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rowLayoutWidth);
                rowLayoutRect.anchoredPosition = rowLayoutPosition;
            }
            foreach (var state in buttonStates)
            {
                if (!state.Rect) continue;
                state.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, state.Width);
                state.Rect.localScale = state.Scale;
                if (state.Element != null)
                {
                    state.Element.minWidth = state.MinWidth;
                    state.Element.preferredWidth = state.PreferredWidth;
                }
                if (state.LabelRect != null)
                {
                    state.LabelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, state.LabelWidth);
                    state.LabelRect.anchoredPosition = state.LabelPosition;
                }
                if (state.Label)
                {
                    state.Label.enableAutoSizing = state.LabelAutoSizing;
                    state.Label.fontSizeMin = state.LabelFontSizeMin;
                    state.Label.fontSizeMax = state.LabelFontSizeMax;
                    state.Label.fontSize = state.LabelFontSize;
                }
            }
            LayoutRebuilder.MarkLayoutForRebuild(rootRect);
            if (rowLayoutRect) LayoutRebuilder.MarkLayoutForRebuild(rowLayoutRect);
            russianApplied = false;
        }

        private static IEnumerable<Button> GetButtons(StartMenuController value)
        {
            var drawer = AccessTools.Field(typeof(StartMenuController), "programsDrawerController")?.GetValue(value)
                as StartMenuRowController;
            if (drawer && AccessTools.Field(typeof(StartMenuRowController), "button")?.GetValue(drawer) is Button drawerButton)
                yield return drawerButton;
            foreach (string fieldName in ButtonFields)
                if (AccessTools.Field(typeof(StartMenuController), fieldName)?.GetValue(value) is Button button)
                    yield return button;
            if (Programs?.GetValue(value) is System.Collections.IEnumerable rows)
                foreach (var row in rows)
                    if (row != null && AccessTools.Field(row.GetType(), "Item1")?.GetValue(row) is Button programButton)
                        yield return programButton;
        }
    }

    [HarmonyPatch(typeof(ToolbarAddressController), "Awake")]
    internal static class RussianAddressSubmitLayoutPatch
    {
        private static readonly System.Reflection.FieldInfo Submit =
            AccessTools.Field(typeof(ToolbarAddressController), "addressBarSubmit");

        private static void Postfix(ToolbarAddressController __instance)
        {
            var layout = __instance.GetComponent<RussianAddressSubmitLayout>();
            if (layout == null) layout = __instance.gameObject.AddComponent<RussianAddressSubmitLayout>();
            layout.Initialize(Submit?.GetValue(__instance) as Button);
        }
    }

    internal sealed class RussianAddressSubmitLayout : MonoBehaviour
    {
        private Button button;
        private RectTransform buttonRect, labelRect;
        private TMP_Text label;
        private LayoutElement element;
        private float originalWidth, originalLabelWidth, originalFontSize, originalFontSizeMin, originalFontSizeMax;
        private float originalPreferredWidth, originalMinWidth;
        private Vector2 originalLabelPosition;
        private bool originalAutoSize, changed;
        private string signature;

        internal void Initialize(Button value)
        {
            button = value;
            if (!button) return;
            buttonRect = button.transform as RectTransform;
            label = button.GetComponentInChildren<TMP_Text>(true);
            if (!buttonRect || !label) return;
            labelRect = label.rectTransform;
            element = button.GetComponent<LayoutElement>();
            originalWidth = buttonRect.rect.width;
            originalLabelWidth = labelRect.rect.width;
            originalLabelPosition = labelRect.anchoredPosition;
            originalFontSize = label.fontSize;
            originalFontSizeMin = label.fontSizeMin;
            originalFontSizeMax = label.fontSizeMax;
            originalAutoSize = label.enableAutoSizing;
            originalPreferredWidth = element ? element.preferredWidth : -1f;
            originalMinWidth = element ? element.minWidth : -1f;
        }

        private void LateUpdate()
        {
            if (!button || !label || !buttonRect || !labelRect) return;
            bool russian = LocalizationSettings.SelectedLocale?.Identifier.Code == Plugin.RussianCode;
            if (!russian)
            {
                if (changed) Restore();
                signature = null;
                return;
            }

            string current = label.text + ":" + label.fontSize + ":" + labelRect.rect.width;
            if (changed && current == signature && Mathf.Abs(buttonRect.rect.width - AppliedButtonWidth()) < 1f &&
                !label.enableAutoSizing && Mathf.Abs(label.fontSize - originalFontSize) < 0.1f) return;

            float preferred = label.GetPreferredValues(label.text, float.PositiveInfinity, float.PositiveInfinity).x;
            float labelWidth = Mathf.Ceil(Mathf.Max(originalLabelWidth, preferred + 48f));
            // Keep substantially more breathing room around the label. The
            // native button is narrow enough that a matching LayoutElement can
            // still truncate it even when TMP reports the text as fitting.
            float buttonWidth = Mathf.Ceil(Mathf.Max(originalWidth, labelWidth + 128f));
            buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, buttonWidth);
            labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, labelWidth);
            // The address bar uses a right-anchored submit button. Keep its right
            // edge in place so the extra Russian width grows into the address field.
            labelRect.anchoredPosition = new Vector2(originalLabelPosition.x, originalLabelPosition.y);
            label.enableAutoSizing = false;
            label.fontSize = originalFontSize;
            if (element)
            {
                element.minWidth = buttonWidth;
                element.preferredWidth = buttonWidth;
            }
            LayoutRebuilder.MarkLayoutForRebuild(buttonRect.parent as RectTransform);
            signature = current;
            changed = true;
        }

        private float AppliedButtonWidth()
        {
            float preferred = label.GetPreferredValues(label.text, float.PositiveInfinity, float.PositiveInfinity).x;
            return Mathf.Ceil(Mathf.Max(originalWidth, Mathf.Max(originalLabelWidth, preferred + 48f) + 128f));
        }

        private void Restore()
        {
            buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalWidth);
            labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalLabelWidth);
            labelRect.anchoredPosition = originalLabelPosition;
            label.enableAutoSizing = originalAutoSize;
            label.fontSizeMin = originalFontSizeMin;
            label.fontSizeMax = originalFontSizeMax;
            label.fontSize = originalFontSize;
            if (element)
            {
                element.minWidth = originalMinWidth;
                element.preferredWidth = originalPreferredWidth;
            }
            LayoutRebuilder.MarkLayoutForRebuild(buttonRect.parent as RectTransform);
            changed = false;
        }
    }

    [HarmonyPatch(typeof(GeneralSettingsController), "Awake")]
    internal static class RussianGeneralSettingsLayoutPatch
    {
        private static void Postfix(GeneralSettingsController __instance)
        {
            var layout = __instance.GetComponent<RussianGeneralSettingsLayout>();
            if (layout == null) layout = __instance.gameObject.AddComponent<RussianGeneralSettingsLayout>();
            layout.Initialize(__instance);
        }
    }

    internal sealed class RussianGeneralSettingsLayout : MonoBehaviour
    {
        private sealed class State
        {
            public RectTransform Rect;
            public Vector2 Position, Size;
        }

        private static readonly System.Reflection.FieldInfo CrtToggle = AccessTools.Field(typeof(GeneralSettingsController), "crtFilterToggle");
        private static readonly System.Reflection.FieldInfo SettingsWindow = AccessTools.Field(typeof(GeneralSettingsController), "settingsWindow");
        private GeneralSettingsController controller;
        private Toggle toggle;
        private TMP_Text label;
        private RectTransform toggleRect, labelRect, settingsRect, checkboxRect, checkmarkRect;
        private State[] saved;
        private string signature;
        private bool changed;

        internal void Initialize(GeneralSettingsController value)
        {
            controller = value;
            toggle = (Toggle)CrtToggle?.GetValue(value);
            var window = (Windows.WindowController)SettingsWindow?.GetValue(value);
            settingsRect = window ? window.transform as RectTransform : null;
            toggleRect = toggle ? toggle.transform as RectTransform : null;
            label = toggle ? toggle.GetComponentInChildren<TMP_Text>(true) : null;
            labelRect = label ? label.rectTransform : null;
            checkboxRect = toggle && toggle.targetGraphic ? toggle.targetGraphic.rectTransform : null;
            checkmarkRect = toggle && toggle.graphic ? toggle.graphic.rectTransform : null;
            if (saved == null)
                saved = new[] { Capture(toggleRect), Capture(labelRect), Capture(settingsRect), Capture(checkboxRect), Capture(checkmarkRect) }
                    .Where(state => state != null).ToArray();
        }

        private void LateUpdate()
        {
            if (controller == null || label == null || labelRect == null) return;
            bool russian = LocalizationSettings.SelectedLocale?.Identifier.Code == Plugin.RussianCode;
            if (!russian)
            {
                if (changed) Restore();
                signature = null;
                return;
            }
            string current = label.text + ":" + label.fontSize + ":" + (label.font ? label.font.GetInstanceID() : 0);
            if (changed && current == signature) return;
            Apply(current);
        }

        private void Apply(string current)
        {
            if (!label || !toggleRect) return;
            Canvas.ForceUpdateCanvases();
            label.ForceMeshUpdate();
            float preferred = label.GetPreferredValues(label.text, float.PositiveInfinity, float.PositiveInfinity).x + 8f;
            float extra = Mathf.Max(0f, preferred - labelRect.rect.width);
            if (extra > 0f)
            {
                labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, labelRect.rect.width + extra);
                labelRect.anchoredPosition += new Vector2(labelRect.pivot.x * extra, 0f);
            }

            float shift = 0f;
            if (checkboxRect)
            {
                var labelCorners = new Vector3[4];
                var checkboxCorners = new Vector3[4];
                labelRect.GetWorldCorners(labelCorners);
                checkboxRect.GetWorldCorners(checkboxCorners);
                shift = Mathf.Max(0f, labelCorners[2].x + 10f - checkboxCorners[0].x);
                if (shift > 0f)
                {
                    MoveRight(checkboxRect, shift);
                    if (checkmarkRect && !checkmarkRect.IsChildOf(checkboxRect)) MoveRight(checkmarkRect, shift);
                }
            }

            float windowExtra = extra + shift;
            if (windowExtra > 0f)
            {
                toggleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, toggleRect.rect.width + windowExtra);
                if (settingsRect)
                {
                    settingsRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, settingsRect.rect.width + windowExtra + 16f);
                    settingsRect.anchoredPosition += new Vector2(settingsRect.pivot.x * (windowExtra + 16f), 0f);
                }
            }
            LayoutRebuilder.MarkLayoutForRebuild(settingsRect ? settingsRect : toggleRect);
            signature = current;
            changed = true;
        }

        private void Restore()
        {
            foreach (var state in saved)
            {
                if (!state.Rect) continue;
                state.Rect.sizeDelta = state.Size;
                state.Rect.anchoredPosition = state.Position;
            }
            if (settingsRect) LayoutRebuilder.MarkLayoutForRebuild(settingsRect);
            changed = false;
        }

        private static State Capture(RectTransform rect) => rect ? new State { Rect = rect, Position = rect.anchoredPosition, Size = rect.sizeDelta } : null;

        private static void MoveRight(RectTransform rect, float pixels)
        {
            if (!rect || !(rect.parent is RectTransform parent)) return;
            rect.anchoredPosition += new Vector2(parent.InverseTransformVector(new Vector3(pixels, 0f, 0f)).x, 0f);
        }
    }
}
