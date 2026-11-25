using System.Runtime.CompilerServices;
using Files;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Windows;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianMenuLayout
    {
        private sealed class Original
        {
            public float Size, Preferred, Minimum;
            public bool AutoSize, Wrap;
            public Vector2 LabelSize;
        }

        private sealed class ExplorerMenuSizing
        {
            public MenuButtonController[] Buttons;
            public TMP_Text[] Titles;
            public TMP_FontAsset[] Fonts;
            public string[] Texts;
            public float[] FontSizes;
            public float[] PreferredWidths;
            public float WindowWidth = -1f;
            public float ButtonPadding = 72f;
        }

        private static readonly AccessTools.FieldRef<MenuButtonController, TMP_Text> Title =
            AccessTools.FieldRefAccess<MenuButtonController, TMP_Text>("title");
        private static readonly ConditionalWeakTable<TMP_Text, Original> originals = new ConditionalWeakTable<TMP_Text, Original>();
        private static readonly ConditionalWeakTable<FileExplorerController, ExplorerMenuSizing> explorerSizing =
            new ConditionalWeakTable<FileExplorerController, ExplorerMenuSizing>();

        internal static bool IsTitle(MenuButtonController menu, TMP_Text text) => Title(menu) == text;

        internal static void Apply(MenuButtonController menu, TMP_Text text)
        {
            var element = menu.GetComponent<LayoutElement>();
            if (!element) return;
            if (!RussianHardcodedText.IsRussian)
            {
                if (!originals.TryGetValue(text, out var saved)) return;
                text.enableAutoSizing = saved.AutoSize;
                text.enableWordWrapping = saved.Wrap;
                text.fontSize = saved.Size;
                text.rectTransform.sizeDelta = saved.LabelSize;
                element.preferredWidth = saved.Preferred;
                element.minWidth = saved.Minimum;
                originals.Remove(text);
                return;
            }
            if (!originals.TryGetValue(text, out var original))
            {
                original = new Original { Size = text.fontSize, AutoSize = text.enableAutoSizing,
                    Wrap = text.enableWordWrapping, LabelSize = text.rectTransform.sizeDelta,
                    Preferred = element.preferredWidth, Minimum = element.minWidth };
                originals.Add(text, original);
            }
            text.enableAutoSizing = false;
            text.enableWordWrapping = false;
            text.fontSize = original.Size;
            // Preserve a clear, even inset around Russian menu captions. The
            // vanilla allowance left pixel-font labels such as "Переход"
            // almost flush with the button edge.
            float width = Mathf.Ceil(text.GetPreferredValues(text.text, float.PositiveInfinity, float.PositiveInfinity).x);
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width + 16f);
            float buttonPadding = GetButtonPadding(menu, text, width);
            element.minWidth = element.preferredWidth = width + buttonPadding;
            foreach (var resize in menu.GetComponentInParent<WindowController>()?.GetComponentsInChildren<WindowResizeHandler>(true)
                ?? System.Array.Empty<WindowResizeHandler>())
                RussianMenuMinimum.Update(resize);
        }

        private static float GetButtonPadding(MenuButtonController menu, TMP_Text currentTitle, float currentWidth)
        {
            var explorer = menu.GetComponentInParent<FileExplorerController>();
            if (!explorer || !(explorer.transform is RectTransform windowRect))
                return 24f;

            var sizing = explorerSizing.GetValue(explorer, value =>
            {
                var buttons = value.GetComponentsInChildren<MenuButtonController>(true);
                return new ExplorerMenuSizing
                {
                    Buttons = buttons,
                    Titles = new TMP_Text[buttons.Length],
                    Fonts = new TMP_FontAsset[buttons.Length],
                    Texts = new string[buttons.Length],
                    FontSizes = new float[buttons.Length],
                    PreferredWidths = new float[buttons.Length]
                };
            });

            float windowWidth = windowRect.rect.width;
            bool needsRecalculation = !Mathf.Approximately(windowWidth, sizing.WindowWidth);
            float totalTextWidth = 0f;
            int buttonCount = 0;
            for (int index = 0; index < sizing.Buttons.Length; index++)
            {
                var button = sizing.Buttons[index];
                var title = button ? Title(button) : null;
                if (!title || !button.GetComponent<LayoutElement>()) continue;

                string titleText = title.text ?? string.Empty;
                bool entryChanged = sizing.Titles[index] != title || sizing.Fonts[index] != title.font ||
                    sizing.Texts[index] != titleText || !Mathf.Approximately(sizing.FontSizes[index], title.fontSize);
                float preferredWidth = title == currentTitle
                    ? currentWidth
                    : entryChanged
                        ? Mathf.Ceil(title.GetPreferredValues(titleText, float.PositiveInfinity, float.PositiveInfinity).x)
                        : sizing.PreferredWidths[index];
                if (entryChanged || !Mathf.Approximately(sizing.PreferredWidths[index], preferredWidth))
                {
                    sizing.Titles[index] = title;
                    sizing.Fonts[index] = title.font;
                    sizing.Texts[index] = titleText;
                    sizing.FontSizes[index] = title.fontSize;
                    sizing.PreferredWidths[index] = preferredWidth;
                    needsRecalculation = true;
                }

                totalTextWidth += preferredWidth;
                buttonCount++;
            }

            if (buttonCount == 0) return 24f;
            if (needsRecalculation)
            {
                var layout = menu.transform.parent ? menu.transform.parent.GetComponent<HorizontalLayoutGroup>() : null;
                float groupSpacing = layout ? layout.spacing * (buttonCount - 1) : 0f;
                float groupPadding = layout ? layout.padding.horizontal : 0f;
                // File explorer windows can be as narrow as 455 points. Keep a
                // 16 point outer margin while reducing only the empty space
                // around each caption; the font size and label width stay fixed.
                float availableWidth = Mathf.Max(0f, windowWidth - 32f - groupSpacing - groupPadding);
                sizing.ButtonPadding = Mathf.Clamp((availableWidth - totalTextWidth) / buttonCount, 16f, 24f);
                sizing.WindowWidth = windowWidth;
            }

            return sizing.ButtonPadding;
        }
    }

    [HarmonyPatch(typeof(WindowResizeHandler), "OnDrag")]
    internal static class RussianMenuMinimum
    {
        private sealed class OriginalMinimum { public float Width; }
        private static readonly ConditionalWeakTable<WindowResizeHandler, OriginalMinimum> saved =
            new ConditionalWeakTable<WindowResizeHandler, OriginalMinimum>();
        private static void Prefix(WindowResizeHandler __instance) => Update(__instance);

        internal static void Update(WindowResizeHandler resize)
        {
            if (!resize) return;
            var original = saved.GetValue(resize, value => new OriginalMinimum { Width = value.minWindowWidth });
            resize.minWindowWidth = original.Width;
            if (!RussianHardcodedText.IsRussian) return;
            var rect = AccessTools.Field(typeof(WindowResizeHandler), "windowRectTransform").GetValue(resize) as RectTransform;
            if (!rect) return;
            foreach (var group in rect.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            {
                float width = group.padding.horizontal;
                int count = 0;
                foreach (Transform child in group.transform)
                {
                    if (!child.gameObject.activeSelf || !child.GetComponent<MenuButtonController>()) continue;
                    var element = child.GetComponent<LayoutElement>();
                    if (!element || element.ignoreLayout) continue;
                    width += Mathf.Max(element.minWidth, element.preferredWidth);
                    count++;
                }
                if (count == 0) continue;
                width += group.spacing * (count - 1);
                // A fixed outer inset avoids feeding a previous window expansion
                // back into the next minimum calculation.
                resize.minWindowWidth = Mathf.Max(resize.minWindowWidth, Mathf.Ceil(width + 16f));
            }
            // Only the native drag handler changes window geometry. Updating a
            // caption must never resize a partially initialized window prefab.
        }
    }
}
