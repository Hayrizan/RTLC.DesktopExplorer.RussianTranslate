using System.Runtime.CompilerServices;
using Files;
using HarmonyLib;
using Login;
using Program;
using Program.Mall.UI;
using TMPro;
using UnityEngine;
using StartBIOS.BIOSSettings;
using Windows;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(EndSequenceController), "Start")]
    internal static class RussianCaveRestartPromptLayout
    {
        private static void Postfix(GameObject ___pressAnyKeyLayout, TMP_Text ___carat)
        {
            if (!RussianHardcodedText.IsRussian || !___pressAnyKeyLayout || !___carat) return;
            var label = ___pressAnyKeyLayout.transform.Find("PressAnyKeyLabel")?.GetComponent<TMP_Text>();
            var canvas = ___pressAnyKeyLayout.GetComponentInParent<Canvas>();
            if (!label || !canvas || !(canvas.rootCanvas.transform is RectTransform viewport)) return;

            // The native prompt and caret use fixed offsets from the left edge.
            // Fit both in one centered row at the original point size.
            const float promptWidth = 1000f;
            const float gap = 16f;
            var caret = ___carat.rectTransform;
            float center = label.transform.parent.InverseTransformPoint(
                viewport.TransformPoint(viewport.rect.center)).x;
            label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, promptWidth);
            var labelPosition = label.rectTransform.localPosition;
            labelPosition.x = center - (gap + caret.rect.width) * 0.5f;
            label.rectTransform.localPosition = labelPosition;
            var caretPosition = caret.localPosition;
            caretPosition.x = center + (promptWidth + gap) * 0.5f;
            caret.localPosition = caretPosition;
        }
    }

    [HarmonyPatch(typeof(TextMeshProUGUI), "OnPreRenderCanvas")]
    internal static class RussianTextLayout
    {
        private sealed class TowerTopBarState
        {
            public float OriginalHeight;
            public Vector4 OriginalMargin;
            public float AppliedHeight;
            public Vector4 AppliedMargin;
            public string AppliedText;
            public bool IsFitting;
        }

        private sealed class Original
        {
            public float Size, Min, Max;
            public bool AutoSize, Wrap;
            public Vector4 Margin;
        }

        private static readonly ConditionalWeakTable<TMP_Text, Original> originals = new ConditionalWeakTable<TMP_Text, Original>();
        private static readonly ConditionalWeakTable<TMP_Text, TowerTopBarState> towerTopBars =
            new ConditionalWeakTable<TMP_Text, TowerTopBarState>();
        private static readonly System.Reflection.FieldInfo FileIconTitle = AccessTools.Field(typeof(SelectableFile), "iconTitle");
        private static readonly System.Reflection.FieldInfo InputDialogLabel = AccessTools.Field(typeof(InputDialogController), "dialogueText");

        private static void Prefix(TextMeshProUGUI __instance)
        {
            var text = __instance;
            if (text && towerTopBars.TryGetValue(text, out var topBarState) && topBarState.IsFitting)
                return;
            if (RussianBootLayout.Owns(text))
            {
                Restore(text);
                return;
            }
            var menu = text.GetComponentInParent<MenuButtonController>();
            if (menu != null && RussianMenuLayout.IsTitle(menu, text))
            {
                Restore(text);
                RussianMenuLayout.Apply(menu, text);
                return;
            }
            if (IsNextRoomDialogueLabel(text))
            {
                if (RussianHardcodedText.IsRussian)
                {
                    PrepareNextRoomDialogueLabel(text);
                }
                else Restore(text);
                return;
            }
            if (IsTowerTopBarText(text))
            {
                FitTowerTopBarText(text);
                return;
            }
            if (text.GetComponentInParent<WebsiteController>() != null)
            {
                FitBrowserText(text);
                return;
            }
            bool replacement = text.font != null && text.font.name.Contains(" SDF [ru:");
            if (!replacement)
            {
                Restore(text);
                return;
            }
            var inputDialog = text.GetComponentInParent<InputDialogController>();
            if (inputDialog != null && ReferenceEquals(InputDialogLabel?.GetValue(inputDialog), text))
            {
                PrepareInputDialogLabel(text);
                return;
            }
            var selectableFile = text.GetComponentInParent<SelectableFile>();
            if (selectableFile != null && ReferenceEquals(FileIconTitle?.GetValue(selectableFile), text))
            {
                PrepareFileIconTitle(text, true, selectableFile.GetComponentInParent<FileExplorerController>() != null);
                return;
            }
            // Footer labels keep the native point size. Their enclosing buttons
            // are fitted by RussianFooterLayout, with padding and equal widths.
            if (text.transform.parent?.parent?.name == "FooterLeft") return;
            if (originals.TryGetValue(text, out _)) return;
            // Keep short, fixed-height labels at the game's point size. Russian
            // translations must not become smaller just because a label is longer.
            // Paragraphs, scrollable input and large text areas retain native sizing.
            var rect = text.rectTransform.rect;
            if (rect.width <= 0 || rect.height <= 0 ||
                rect.height > text.fontSize * 1.75f || text.text.Length > 120 ||
                text.text.Contains("\n") || text.GetComponentInParent<TMP_InputField>() != null)
                return;
            var original = new Original
            {
                Size = text.fontSize, Min = text.fontSizeMin, Max = text.fontSizeMax,
                AutoSize = text.enableAutoSizing, Wrap = text.enableWordWrapping, Margin = text.margin
            };
            originals.Add(text, original);
            var selector = text.GetComponentInParent<SettingSelectorController>();
            if (selector != null && (TMP_Text)AccessTools.Field(typeof(SettingSelectorController), "settingTitleText").GetValue(selector) == text)
            {
                var value = (TMP_Text)AccessTools.Field(typeof(SettingSelectorController), "volumeText").GetValue(selector);
                if (value != null)
                {
                    // Volume title and percentage intentionally share one rectangle,
                    // aligned left/right. Reserve the numeric value's width explicitly.
                    var margin = text.margin;
                    margin.z = Mathf.Max(margin.z, value.GetPreferredValues("100%").x + 16);
                    text.margin = margin;
                }
            }
            text.enableAutoSizing = false;
            text.fontSize = original.Size;
            text.fontSizeMin = text.fontSize;
            text.fontSizeMax = text.fontSize;
            text.enableWordWrapping = false;
        }

        private static bool IsTowerTopBarText(TMP_Text text) => text && text.name == "TopBarText" &&
            text.transform.parent?.name == "GameUI" &&
            text.GetComponentInParent<Program.Tower.TowerController>() != null;

        private static void FitTowerTopBarText(TMP_Text text)
        {
            var rect = text.rectTransform;
            var state = towerTopBars.GetValue(text, _ => new TowerTopBarState
            {
                OriginalHeight = rect.rect.height,
                OriginalMargin = text.margin
            });
            if (state.IsFitting) return;

            state.IsFitting = true;
            try
            {
                if (!RussianHardcodedText.IsRussian)
                {
                    if (Mathf.Abs(rect.rect.height - state.OriginalHeight) > 0.25f)
                        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, state.OriginalHeight);
                    if (text.margin != state.OriginalMargin) text.margin = state.OriginalMargin;
                    state.AppliedHeight = rect.rect.height;
                    state.AppliedMargin = text.margin;
                    state.AppliedText = text.text;
                    return;
                }

                if (state.AppliedText == text.text &&
                    Mathf.Abs(rect.rect.height - state.AppliedHeight) < 0.25f && text.margin == state.AppliedMargin)
                    return;

                // The Tower's authored 32 px top-bar box clips the tall Cyrillic
                // accent glyphs. Keep the game's point size and grow only this
                // text field until its rendered glyph bounds fit the box.
                if (Mathf.Abs(rect.rect.height - state.OriginalHeight) > 0.25f)
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, state.OriginalHeight);
                text.margin = state.OriginalMargin;
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    text.ForceMeshUpdate();
                    float minY = float.PositiveInfinity;
                    float maxY = float.NegativeInfinity;
                    var characters = text.textInfo.characterInfo;
                    for (int index = 0; index < text.textInfo.characterCount; index++)
                    {
                        var character = characters[index];
                        if (!character.isVisible) continue;
                        minY = Mathf.Min(minY, character.bottomLeft.y);
                        maxY = Mathf.Max(maxY, character.topRight.y);
                    }
                    if (float.IsInfinity(minY) || float.IsInfinity(maxY)) break;

                    float height = rect.rect.height;
                    float overflow = Mathf.Max(maxY - rect.rect.yMax, rect.rect.yMin - minY);
                    if (overflow <= 0.25f) break;
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height + overflow * 2f + 1f);
                }
                text.ForceMeshUpdate();
                state.AppliedHeight = rect.rect.height;
                state.AppliedMargin = text.margin;
                state.AppliedText = text.text;
            }
            finally
            {
                state.IsFitting = false;
            }
        }

        private static void PrepareInputDialogLabel(TMP_Text text)
        {
            if (!RussianHardcodedText.IsRussian)
            {
                Restore(text);
                return;
            }
            if (!originals.TryGetValue(text, out var original))
            {
                original = new Original
                {
                    Size = text.fontSize,
                    Min = text.fontSizeMin,
                    Max = text.fontSizeMax,
                    AutoSize = text.enableAutoSizing,
                    Wrap = text.enableWordWrapping,
                    Margin = text.margin
                };
                originals.Add(text, original);
            }
            // Security prompts can be long in Russian. Wrap them into the dialog's
            // existing content layout so the dialog grows vertically at the native
            // point size instead of letting the text run past its fixed one-line box.
            text.enableAutoSizing = false;
            text.fontSize = original.Size;
            text.fontSizeMin = original.Size;
            text.fontSizeMax = original.Size;
            text.enableWordWrapping = true;
        }

        private static bool IsNextRoomDialogueLabel(TMP_Text text)
        {
            var label = text.transform;
            return label.name == "InteractionText" && label.parent?.name == "ConversationLabel" &&
                label.parent.parent?.name == "InteractionUIDialogueContent";
        }

        internal static void PrepareNextRoomDialogueLabel(TMP_Text text)
        {
            if (!originals.TryGetValue(text, out var original))
            {
                original = new Original
                {
                    Size = text.fontSize,
                    Min = text.fontSizeMin,
                    Max = text.fontSizeMax,
                    AutoSize = text.enableAutoSizing,
                    Wrap = text.enableWordWrapping,
                    Margin = text.margin
                };
                originals.Add(text, original);
            }
            // The NextRooms interaction line has a fixed-height native field.
            // Wrap Russian system messages at the authored point size and leave a
            // small top inset so tall Cyrillic accents stay inside the TMP rect.
            var margin = original.Margin;
            margin.y += 2f;
            text.margin = margin;
            text.enableAutoSizing = false;
            text.fontSize = original.Size;
            text.fontSizeMin = original.Size;
            text.fontSizeMax = original.Size;
            text.enableWordWrapping = true;
        }

        internal static void PrepareFileIconTitle(TMP_Text text, bool russian, bool adaptiveGrid)
        {
            if (text == null) return;
            if (!russian)
            {
                Restore(text);
                return;
            }
            if (!originals.TryGetValue(text, out var original))
            {
                original = new Original { Size = text.fontSize, Min = text.fontSizeMin,
                    Max = text.fontSizeMax, AutoSize = text.enableAutoSizing,
                    Wrap = text.enableWordWrapping, Margin = text.margin };
                originals.Add(text, original);
            }
            text.enableAutoSizing = false;
            text.fontSize = original.Size;
            text.fontSizeMin = original.Size;
            text.fontSizeMax = original.Size;
            // Explorer widens its cells. The desktop has fixed cells at the screen edge.
            text.enableWordWrapping = !adaptiveGrid;
        }

        private static void FitBrowserText(TMP_Text text)
        {
            if (!RussianHardcodedText.IsRussian)
            {
                Restore(text);
                return;
            }

            var rect = text.rectTransform.rect;
            if (rect.width <= 0 || rect.height <= 0 || originals.TryGetValue(text, out _)) return;
            float width = rect.width - text.margin.x - text.margin.z;
            float height = rect.height - text.margin.y - text.margin.w;
            if (width <= 0 || height <= 0) return;
            var preferred = text.GetPreferredValues(text.text, text.enableWordWrapping ? width : 0, 0);
            if (preferred.x <= width + 1f && preferred.y <= height + 1f) return;

            float originalSize = text.fontSize;
            originals.Add(text, new Original { Size = originalSize, Min = text.fontSizeMin,
                Max = text.fontSizeMax, AutoSize = text.enableAutoSizing,
                Wrap = text.enableWordWrapping, Margin = text.margin });
            text.fontSizeMin = Mathf.Min(originalSize, Mathf.Max(8f, originalSize * 0.55f));
            text.fontSizeMax = originalSize;
            text.enableAutoSizing = true;
        }

        internal static void Restore(TMP_Text text)
        {
            if (!originals.TryGetValue(text, out var old)) return;
            text.enableAutoSizing = old.AutoSize;
            text.fontSizeMin = old.Min;
            text.fontSizeMax = old.Max;
            text.enableWordWrapping = old.Wrap;
            text.fontSize = old.Size;
            text.margin = old.Margin;
            originals.Remove(text);
        }
    }

    [HarmonyPatch(typeof(NextRoomsUILabelController), nameof(NextRoomsUILabelController.SetupNextRoomsLabel))]
    internal static class RussianNextRoomsDialogueLayout
    {
        private static readonly System.Reflection.FieldInfo LabelText =
            AccessTools.Field(typeof(NextRoomsUILabelController), "labelText");

        private static void Postfix(NextRoomsUILabelController __instance)
        {
            var label = (TMP_Text)LabelText?.GetValue(__instance);
            if (!label) return;
            if (RussianHardcodedText.IsRussian)
                RussianTextLayout.PrepareNextRoomDialogueLabel(label);
            else RussianTextLayout.Restore(label);
        }
    }

    [HarmonyPatch(typeof(SelectableFile), "SetTruncatedFilename")]
    internal static class RussianSelectableFileIconTitle
    {
        private static readonly System.Reflection.FieldInfo IconTitle = AccessTools.Field(typeof(SelectableFile), "iconTitle");

        private static void Prefix(SelectableFile __instance)
        {
            var text = (TMP_Text)IconTitle?.GetValue(__instance);
            var explorer = __instance.GetComponentInParent<FileExplorerController>();
            explorer?.GetComponent<RussianFileIconGridLayout>()?.Invalidate();
            RussianTextLayout.PrepareFileIconTitle(text, RussianHardcodedText.IsRussian, explorer != null);
        }
    }
}
