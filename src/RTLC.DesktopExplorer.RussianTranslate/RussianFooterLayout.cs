using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal sealed class RussianFooterLayout : MonoBehaviour
    {
        private sealed class ButtonState
        {
            public RectTransform Rect;
            public UnityEngine.UI.HorizontalLayoutGroup Layout;
            public float Width;
            public RectOffset Padding;
            public bool Expand;
            public TMP_Text[] Labels;
        }

        private UnityEngine.UI.HorizontalLayoutGroup row;
        private float originalSpacing;
        private ButtonState[] buttons;
        private string signature;
        private bool changed;

        internal static void Attach(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var layout in root.GetComponentsInChildren<UnityEngine.UI.HorizontalLayoutGroup>(true))
                    if (layout.name == "FooterLeft" && layout.transform.parent?.name == "BIOSFooter" &&
                        layout.transform.Find("ApplyButton") != null && layout.GetComponent<RussianFooterLayout>() == null)
                        layout.gameObject.AddComponent<RussianFooterLayout>();

                foreach (var transform in root.GetComponentsInChildren<RectTransform>(true))
                    if (transform.name == "FooterRight" && transform.parent?.name == "BIOSCanvasGroup" &&
                        transform.GetComponent<RussianFooterRightLayout>() == null)
                        transform.gameObject.AddComponent<RussianFooterRightLayout>();
            }
        }

        private void Awake()
        {
            row = GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            originalSpacing = row.spacing;
            var states = new List<ButtonState>();
            foreach (Transform child in transform)
            {
                var layout = child.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                if (layout == null || child.GetComponent<UnityEngine.UI.Button>() == null) continue;
                var rect = (RectTransform)child;
                states.Add(new ButtonState { Rect = rect, Layout = layout, Width = rect.rect.width,
                    Padding = layout.padding, Expand = layout.childForceExpandWidth,
                    Labels = child.GetComponentsInChildren<TMP_Text>(true) });
            }
            buttons = states.ToArray();
        }

        private void LateUpdate()
        {
            bool russian = LocalizationSettings.SelectedLocale?.Identifier.Code == Plugin.RussianCode;
            if (!russian)
            {
                if (changed) Restore();
                signature = null;
                return;
            }
            var active = buttons.Where(b => b.Rect.gameObject.activeInHierarchy).ToArray();
            if (active.Length == 0) return;
            string current = ((RectTransform)transform).rect.width + "|" + string.Join("|", active
                .SelectMany(b => b.Labels).Select(t => t.text + ":" + t.font?.GetInstanceID()));
            if (signature == current) return;
            signature = current;
            float width = 0;
            foreach (var button in active)
            {
                float content = button.Labels.Sum(t => t.GetPreferredValues(t.text, float.PositiveInfinity, float.PositiveInfinity).x);
                float required = content + button.Layout.spacing * (button.Labels.Length - 1) + 32;
                width = Mathf.Max(width, button.Width + 32, required);
            }
            row.spacing = 32;
            float available = (((RectTransform)transform).rect.width - row.padding.horizontal - row.spacing * (active.Length - 1)) / active.Length;
            width = Mathf.Min(Mathf.Ceil(width / 8) * 8, available);
            foreach (var button in buttons)
            {
                button.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                button.Layout.padding = new RectOffset(16, 16, button.Padding.top, button.Padding.bottom);
                button.Layout.childForceExpandWidth = false;
            }
            changed = true;
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }

        private void Restore()
        {
            row.spacing = originalSpacing;
            foreach (var button in buttons)
            {
                button.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, button.Width);
                button.Layout.padding = button.Padding;
                button.Layout.childForceExpandWidth = button.Expand;
            }
            changed = false;
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }
    }

    internal sealed class RussianFooterRightLayout : MonoBehaviour
    {
        private RectTransform rect;
        private UnityEngine.UI.HorizontalLayoutGroup row;
        private RectTransform arrowHint;
        private RectTransform caption;
        private TMP_Text captionText;
        private TMP_Text hintText;
        private string originalHintText;
        private TextAlignmentOptions originalCaptionAlignment;
        private RectOffset originalPadding;
        private float originalWidth, originalSpacing, originalCaptionWidth;
        private float requiredCaptionWidth;
        private Vector2 originalPosition, originalArrowPosition, originalCaptionPosition;
        private string captionSignature;
        private bool changed;

        private void Awake()
        {
            rect = (RectTransform)transform;
            row = GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            arrowHint = transform.Find("Text (TMP)") as RectTransform;
            caption = transform.Find("FooterRightText") as RectTransform;
            captionText = caption ? caption.GetComponent<TMP_Text>() : null;
            hintText = arrowHint ? arrowHint.GetComponent<TMP_Text>() : null;
            originalHintText = hintText ? hintText.text : null;
            originalCaptionAlignment = captionText ? captionText.alignment : TextAlignmentOptions.Left;
            originalWidth = rect.rect.width;
            originalCaptionWidth = caption ? caption.rect.width : 0f;
            originalPosition = rect.anchoredPosition;
            if (arrowHint != null) originalArrowPosition = arrowHint.anchoredPosition;
            if (caption != null) originalCaptionPosition = caption.anchoredPosition;
            Canvas.willRenderCanvases += EnsureCaptionGap;
            if (row != null)
            {
                originalSpacing = row.spacing;
                originalPadding = new RectOffset(row.padding.left, row.padding.right, row.padding.top, row.padding.bottom);
            }
        }

        private void LateUpdate()
        {
            bool russian = LocalizationSettings.SelectedLocale?.Identifier.Code == Plugin.RussianCode;
            if (!russian)
            {
                if (changed) Restore();
                return;
            }
            // Keep the arrow hint and its Russian caption visually distinct at
            // the native font sizes, and leave an inset after the final caption.
            // Widen toward the left so the right edge stays put.
            const float gap = 8f;
            const int rightInset = 176;
            EnsureCaptionWidth();
            if (hintText) hintText.text = "↑ ↓ =";
            if (captionText) captionText.alignment = TextAlignmentOptions.MidlineLeft;
            if (changed)
            {
                // The title-screen controller refreshes its footer layout after
                // scene initialization and can restore the prefab spacing.
                // Reassert only these layout-group values; never add width twice.
                if (row != null)
                {
                    float targetSpacing = gap;
                    int targetRightPadding = originalPadding.right + rightInset;
                    if (Mathf.Abs(row.spacing - targetSpacing) > 0.1f || row.padding.right != targetRightPadding)
                    {
                        row.spacing = targetSpacing;
                        row.padding = new RectOffset(originalPadding.left, targetRightPadding,
                            originalPadding.top, originalPadding.bottom);
                        UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(rect);
                    }
                }
                return;
            }
            float added;
            if (row != null)
            {
                row.spacing = gap;
                row.padding = new RectOffset(originalPadding.left, originalPadding.right + rightInset,
                    originalPadding.top, originalPadding.bottom);
                added = row.spacing - originalSpacing + rightInset;
            }
            else if (arrowHint != null)
            {
                added = gap + rightInset;
                arrowHint.anchoredPosition = originalArrowPosition - new Vector2(added, 0f);
            }
            else return;

            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalWidth + added);
            rect.anchoredPosition = originalPosition + new Vector2((rect.pivot.x - 1f) * added, 0f);
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(rect);
            changed = true;
        }

        private void Restore()
        {
            if (hintText) hintText.text = originalHintText;
            if (captionText) captionText.alignment = originalCaptionAlignment;
            if (row != null)
            {
                row.spacing = originalSpacing;
                row.padding = new RectOffset(originalPadding.left, originalPadding.right,
                    originalPadding.top, originalPadding.bottom);
            }
            if (arrowHint != null) arrowHint.anchoredPosition = originalArrowPosition;
            if (caption != null) caption.anchoredPosition = originalCaptionPosition;
            if (caption != null) caption.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalCaptionWidth);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalWidth);
            rect.anchoredPosition = originalPosition;
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(rect);
            changed = false;
        }

        private void EnsureCaptionGap()
        {
            if (!changed || caption == null || arrowHint == null ||
                LocalizationSettings.SelectedLocale?.Identifier.Code != Plugin.RussianCode ||
                !(caption.parent is RectTransform parent)) return;
            EnsureCaptionWidth();
            var hintCorners = new Vector3[4];
            var captionCorners = new Vector3[4];
            arrowHint.GetWorldCorners(hintCorners);
            caption.GetWorldCorners(captionCorners);
            float hintRight = hintCorners.Max(point => point.x);
            float captionLeft = captionCorners.Min(point => point.x);
            float visibleGap = parent.TransformVector(new Vector3(12f, 0f, 0f)).x;
            if (hintText && captionText)
            {
                hintText.ForceMeshUpdate();
                captionText.ForceMeshUpdate();
                var hintChars = hintText.textInfo.characterInfo.Take(hintText.textInfo.characterCount).Where(c => c.isVisible).ToArray();
                var captionChars = captionText.textInfo.characterInfo.Take(captionText.textInfo.characterCount).Where(c => c.isVisible).ToArray();
                if (hintChars.Length > 0) hintRight = hintChars.Max(c => hintText.transform.TransformPoint(c.topRight).x);
                if (hintChars.Length >= 2)
                {
                    var equals = hintChars[hintChars.Length - 1];
                    var arrow = hintChars[hintChars.Length - 2];
                    visibleGap = Mathf.Max(0f, hintText.transform.TransformPoint(equals.bottomLeft).x -
                        hintText.transform.TransformPoint(arrow.topRight).x);
                }
                if (captionChars.Length > 0) captionLeft = captionChars.Min(c => captionText.transform.TransformPoint(c.bottomLeft).x);
            }
            float correction = hintRight + visibleGap - captionLeft;
            if (Mathf.Abs(correction) > 0.1f)
                caption.anchoredPosition += new Vector2(
                    parent.InverseTransformVector(new Vector3(correction, 0f, 0f)).x, 0f);
        }

        private void EnsureCaptionWidth()
        {
            if (!caption || !captionText || LocalizationSettings.SelectedLocale?.Identifier.Code != Plugin.RussianCode)
                return;
            string signature = captionText.text + "|" + (captionText.font ? captionText.font.GetInstanceID() : 0) +
                "|" + captionText.fontSize + "|" + captionText.characterSpacing;
            if (captionSignature != signature)
            {
                captionSignature = signature;
                captionText.ForceMeshUpdate();
                requiredCaptionWidth = Mathf.Ceil(captionText.GetPreferredValues(captionText.text,
                    float.PositiveInfinity, float.PositiveInfinity).x + 2f);
            }
            if (requiredCaptionWidth > caption.rect.width + 0.5f)
                caption.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredCaptionWidth);
        }

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= EnsureCaptionGap;
        }
    }
}
