using HarmonyLib;
using OS.UI;
using Program.Playtest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(PlaytestFeedbackController), "OnEnable")]
    internal static class RussianFeedbackFormLayoutPatch
    {
        private static void Postfix(PlaytestFeedbackController __instance)
        {
            var layout = __instance.GetComponent<RussianFeedbackFormLayout>();
            if (layout == null) layout = __instance.gameObject.AddComponent<RussianFeedbackFormLayout>();
            layout.Initialize(__instance);
        }
    }

    internal sealed class RussianFeedbackFormLayout : MonoBehaviour
    {
        private sealed class RectState
        {
            internal RectTransform Rect;
            internal Vector2 Size;
            internal Vector2 Position;

            internal void Capture(RectTransform rect)
            {
                Rect = rect;
                if (!rect) return;
                Size = rect.sizeDelta;
                Position = rect.anchoredPosition;
            }

            internal void Restore()
            {
                if (!Rect) return;
                Rect.sizeDelta = Size;
                Rect.anchoredPosition = Position;
            }
        }

        private static readonly System.Reflection.FieldInfo CategoryDropdown =
            AccessTools.Field(typeof(PlaytestFeedbackController), "feedbackCategoryDropdown");

        private PlaytestFeedbackController controller;
        private TMP_Text titleLabel, titlePlaceholder, categoryLabel, sendLabel, summaryPlaceholder;
        private RectState titleLabelRect, titleViewportRect, categoryLabelRect, buttonRect, buttonLabelRect,
            summaryViewportRect;
        private Button sendButton;
        private string signature;
        private bool applied;

        internal void Initialize(PlaytestFeedbackController value)
        {
            if (controller == value && titleLabelRect != null) return;
            controller = value;
            Transform form = value.transform.Find("WindowBody/FeedbackContainer");
            if (!form) return;

            titleLabel = form.Find("TitleLabel (1)")?.GetComponent<TMP_Text>();
            categoryLabel = CategoryDropdown?.GetValue(value) is DEDropdownController dropdown
                ? AccessTools.Field(typeof(DEDropdownController), "selectedLabel").GetValue(dropdown) as TMP_Text
                : null;
            sendButton = form.Find("SendFeedbackButton")?.GetComponent<Button>();
            sendLabel = sendButton ? sendButton.GetComponentInChildren<TMP_Text>(true) : null;
            var titleInput = form.Find("TitleInputField");
            var summaryInput = form.Find("SummaryInputField");
            var titleViewport = titleInput ? titleInput.Find("Text Area") as RectTransform : null;
            var summaryViewport = summaryInput ? summaryInput.Find("Text Area") as RectTransform : null;
            titlePlaceholder = titleViewport ? titleViewport.Find("Placeholder")?.GetComponent<TMP_Text>() : null;
            summaryPlaceholder = summaryViewport ? summaryViewport.Find("Placeholder")?.GetComponent<TMP_Text>() : null;

            titleLabelRect = Capture(titleLabel ? titleLabel.rectTransform : null);
            titleViewportRect = Capture(titleViewport);
            categoryLabelRect = Capture(categoryLabel ? categoryLabel.rectTransform : null);
            buttonRect = Capture(sendButton ? sendButton.transform as RectTransform : null);
            buttonLabelRect = Capture(sendLabel ? sendLabel.rectTransform : null);
            summaryViewportRect = Capture(summaryViewport);
            signature = null;
        }

        private void LateUpdate()
        {
            if (!controller) return;
            if (!RussianHardcodedText.IsRussian)
            {
                if (applied) Restore();
                signature = null;
                return;
            }

            string current = CurrentSignature();
            if (applied && current == signature && !NeedsRepair()) return;
            Apply(current);
        }

        private string CurrentSignature() => string.Join("|",
            titleLabel ? titleLabel.text : "",
            titlePlaceholder ? titlePlaceholder.text : "",
            categoryLabel ? categoryLabel.text : "",
            sendLabel ? sendLabel.text : "",
            summaryPlaceholder ? summaryPlaceholder.text : "");

        private bool NeedsRepair() =>
            NeedsHeight(titleLabel, titleLabelRect, 4f) ||
            NeedsHeight(titlePlaceholder, titleViewportRect, 4f) ||
            NeedsHeight(categoryLabel, categoryLabelRect, 4f) ||
            NeedsHeight(summaryPlaceholder, summaryViewportRect, 4f) ||
            (sendLabel && sendButton && (sendLabel.enableAutoSizing ||
                sendLabel.rectTransform.rect.width < PreferredWidth(sendLabel) + 12f ||
                ((RectTransform)sendButton.transform).rect.width < sendLabel.rectTransform.rect.width + 16f));

        private void Apply(string current)
        {
            FitHeight(titleLabel, titleLabelRect, 4f, preserveBottom: true);
            FitHeight(titlePlaceholder, titleViewportRect, 4f);
            FitHeight(categoryLabel, categoryLabelRect, 4f);
            FitHeight(summaryPlaceholder, summaryViewportRect, 4f);

            if (sendLabel && sendButton)
            {
                sendLabel.enableAutoSizing = false;
                float labelWidth = Mathf.Ceil(Mathf.Max(buttonLabelRect.Size.x,
                    PreferredWidth(sendLabel) + 12f));
                float buttonWidth = Mathf.Ceil(Mathf.Max(buttonRect.Size.x, labelWidth + 16f));
                sendLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, labelWidth);
                ((RectTransform)sendButton.transform).SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, buttonWidth);
            }

            LayoutRebuilder.MarkLayoutForRebuild(controller.transform as RectTransform);
            signature = current;
            applied = true;
        }

        private static RectState Capture(RectTransform rect)
        {
            var state = new RectState();
            state.Capture(rect);
            return state;
        }

        private static float PreferredWidth(TMP_Text text) =>
            Mathf.Ceil(text.GetPreferredValues(text.text, float.PositiveInfinity, float.PositiveInfinity).x);

        private static float PreferredHeight(TMP_Text text) =>
            Mathf.Ceil(text.GetPreferredValues(text.text, float.PositiveInfinity, float.PositiveInfinity).y);

        private static bool NeedsHeight(TMP_Text text, RectState state, float padding) =>
            text && state?.Rect && state.Rect.rect.height < PreferredHeight(text) + padding - 0.5f;

        private static void FitHeight(TMP_Text text, RectState state, float padding, bool preserveBottom = false)
        {
            if (!text || !state?.Rect) return;
            float targetHeight = Mathf.Max(state.Size.y, PreferredHeight(text) + padding);
            float delta = targetHeight - state.Size.y;
            state.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            if (preserveBottom)
            {
                state.Rect.anchoredPosition = state.Position + Vector2.up * (state.Rect.pivot.y * delta);
            }
        }

        private void Restore()
        {
            titleLabelRect?.Restore();
            titleViewportRect?.Restore();
            categoryLabelRect?.Restore();
            buttonRect?.Restore();
            buttonLabelRect?.Restore();
            summaryViewportRect?.Restore();
            applied = false;
            signature = null;
            LayoutRebuilder.MarkLayoutForRebuild(controller.transform as RectTransform);
        }
    }
}
