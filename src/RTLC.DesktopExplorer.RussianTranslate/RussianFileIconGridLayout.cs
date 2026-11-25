using System.Collections.Generic;
using System.Linq;
using Files;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Windows;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(FileExplorerController), "Awake")]
    internal static class RussianFileIconGridLayoutPatch
    {
        private static readonly System.Reflection.FieldInfo IconLayout =
            AccessTools.Field(typeof(FileExplorerController), "iconLayout");

        private static void Postfix(FileExplorerController __instance)
        {
            var layout = __instance.GetComponent<RussianFileIconGridLayout>();
            if (layout == null) layout = __instance.gameObject.AddComponent<RussianFileIconGridLayout>();
            layout.Initialize(__instance, IconLayout?.GetValue(__instance) as GameObject);
        }
    }

    internal sealed class RussianFileIconGridLayout : MonoBehaviour
    {
        private sealed class RectState
        {
            public RectTransform Rect;
            public Vector2 SizeDelta, Position;

            public RectState(RectTransform rect)
            {
                Rect = rect;
                SizeDelta = rect.sizeDelta;
                Position = rect.anchoredPosition;
            }

            public void Restore()
            {
                if (!Rect) return;
                Rect.sizeDelta = SizeDelta;
                Rect.anchoredPosition = Position;
            }
        }

        private sealed class IconState
        {
            public RectTransform Root;
            public RectState IconLayout, Icon, TitlePanel, Label;
            public TMP_Text Title;
            public bool LabelWordWrapping;
            public TextOverflowModes LabelOverflowMode;

            public IconState(SelectableFile file, TMP_Text title)
            {
                Root = file.transform as RectTransform;
                Title = title;
                var titleRect = title.rectTransform;
                var titlePanel = titleRect.parent as RectTransform;
                var iconLayout = titlePanel ? titlePanel.parent as RectTransform : null;
                var icon = iconLayout ? iconLayout.Find("Icon") as RectTransform : null;
                if (iconLayout) IconLayout = new RectState(iconLayout);
                if (icon) Icon = new RectState(icon);
                if (titlePanel) TitlePanel = new RectState(titlePanel);
                Label = new RectState(titleRect);
                LabelWordWrapping = title.enableWordWrapping;
                LabelOverflowMode = title.overflowMode;
            }

            public void Apply(float cellWidth)
            {
                if (IconLayout?.Rect)
                    IconLayout.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cellWidth);
                if (Icon?.Rect)
                    Icon.Rect.anchoredPosition = new Vector2(cellWidth * 0.5f, Icon.Position.y);
                if (TitlePanel?.Rect)
                {
                    TitlePanel.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(32f, cellWidth - 8f));
                    TitlePanel.Rect.anchoredPosition = new Vector2(cellWidth * 0.5f, TitlePanel.Position.y);
                }
                if (Label?.Rect)
                {
                    Label.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(24f, cellWidth - 16f));
                    Label.Rect.anchoredPosition = new Vector2(cellWidth * 0.5f - 4f, Label.Position.y);
                }
                if (Label?.Rect)
                {
                    SetWrappedMode();
                }
            }

            public void Restore()
            {
                IconLayout?.Restore();
                Icon?.Restore();
                TitlePanel?.Restore();
                Label?.Restore();
                if (Label?.Rect)
                {
                    var title = Label.Rect.GetComponent<TMP_Text>();
                    if (title)
                    {
                        title.enableWordWrapping = LabelWordWrapping;
                        title.overflowMode = LabelOverflowMode;
                    }
                }
            }

            public bool NeedsRepair() => Title && (!Title.enableWordWrapping || Title.enableAutoSizing ||
                Title.overflowMode != TextOverflowModes.Overflow);

            public void SetWrappedMode()
            {
                if (!Title) return;
                Title.enableAutoSizing = false;
                Title.enableWordWrapping = true;
                Title.overflowMode = TextOverflowModes.Overflow;
            }
        }

        private static readonly System.Reflection.FieldInfo IconTitle = AccessTools.Field(typeof(SelectableFile), "iconTitle");
        private FileExplorerController controller;
        private GridLayoutGroup grid;
        private Vector2 originalCellSize, originalSpacing;
        private RectOffset originalPadding;
        private GridLayoutGroup.Constraint originalConstraint;
        private int originalConstraintCount;
        private readonly Dictionary<int, IconState> iconStates = new Dictionary<int, IconState>();
        private List<SelectableFile> observedFiles;
        private int observedFileCount = -1;
        private string observedLocale;
        private string observedTitleSignature;
        private float observedGridWidth, appliedCellWidth, appliedSpacing;
        private bool changed;
        private bool dirty = true;

        internal void Initialize(FileExplorerController value, GameObject content)
        {
            controller = value;
            grid = content ? content.GetComponent<GridLayoutGroup>() : null;
            if (!grid) return;
            originalCellSize = grid.cellSize;
            originalSpacing = grid.spacing;
            originalPadding = ClonePadding(grid.padding);
            originalConstraint = grid.constraint;
            originalConstraintCount = grid.constraintCount;
        }

        private void OnEnable() => Canvas.willRenderCanvases += EnforceSingleLineLabels;

        private void OnDisable() => Canvas.willRenderCanvases -= EnforceSingleLineLabels;

        private void EnforceSingleLineLabels()
        {
            if (LocalizationSettings.SelectedLocale?.Identifier.Code != Plugin.RussianCode || !changed) return;
            foreach (var state in iconStates.Values) state.SetWrappedMode();
        }

        private void LateUpdate()
        {
            if (controller == null) return;
            if (!grid)
            {
                var content = AccessTools.Field(typeof(FileExplorerController), "iconLayout")?.GetValue(controller) as GameObject;
                Initialize(controller, content);
                if (!grid) return;
            }

            string locale = LocalizationSettings.SelectedLocale?.Identifier.Code;
            if (locale != Plugin.RussianCode)
            {
                if (changed) Restore();
                dirty = false;
                observedFiles = controller.SelectableFiles;
                observedFileCount = observedFiles?.Count ?? 0;
                observedLocale = locale;
                observedTitleSignature = BuildTitleSignature(observedFiles);
                observedGridWidth = ((RectTransform)grid.transform).rect.width;
                return;
            }

            var files = controller.SelectableFiles;
            int fileCount = files?.Count ?? 0;
            string titleSignature = BuildTitleSignature(files);
            float gridWidth = ((RectTransform)grid.transform).rect.width;
            bool geometryChanged = changed && (Mathf.Abs(grid.cellSize.x - appliedCellWidth) > 0.1f ||
                Mathf.Abs(grid.spacing.x - appliedSpacing) > 0.1f);
            bool labelLayoutChanged = iconStates.Values.Any(state => state.NeedsRepair());
            bool inputChanged = !ReferenceEquals(files, observedFiles) || fileCount != observedFileCount ||
                locale != observedLocale || titleSignature != observedTitleSignature ||
                Mathf.Abs(gridWidth - observedGridWidth) > 0.1f || geometryChanged ||
                labelLayoutChanged;
            if (!dirty && !inputChanged) return;

            observedFiles = files;
            observedFileCount = fileCount;
            observedLocale = locale;
            observedTitleSignature = titleSignature;
            if (files == null || files.Count == 0)
            {
                dirty = false;
                return;
            }

            var icons = files.Where(file => file).ToArray();
            if (icons.Length == 0)
            {
                dirty = false;
                return;
            }
            var titles = icons.Select(file => (TMP_Text)IconTitle?.GetValue(file))
                .Where(title => title && !string.IsNullOrWhiteSpace(title.text)).ToArray();
            if (titles.Length == 0)
            {
                dirty = false;
                return;
            }

            Canvas.ForceUpdateCanvases();
            float maxPreferredWidth = titles.Max(title =>
            {
                title.ForceMeshUpdate();
                return title.GetPreferredValues(title.text, float.PositiveInfinity, float.PositiveInfinity).x;
            });
            float availableWidth = Mathf.Max(originalCellSize.x,
                ((RectTransform)grid.transform).rect.width - grid.padding.horizontal);
            // Keep each filename in a full-size text box and let the grid use
            // fewer columns when a window is too narrow. Without a fixed
            // column count, Unity can keep the prefab's four columns and place
            // labels beyond the visible area after the cells grow.
            const float minimumGap = 24f;
            int maximumColumns = icons.Length;
            // Bound caption width and wrap long names within their own cell.
            float desiredCellWidth = Mathf.Max(originalCellSize.x, Mathf.Min(320f, maxPreferredWidth + 24f));
            int columns = Mathf.Clamp(Mathf.FloorToInt((availableWidth + minimumGap) /
                (desiredCellWidth + minimumGap)), 1, maximumColumns);
            float cellWidth = Mathf.Max(originalCellSize.x, Mathf.Min(desiredCellWidth,
                (availableWidth - minimumGap * (columns - 1)) / columns));

            var currentIds = new HashSet<int>(icons.Select(file => file.GetInstanceID()));
            foreach (int staleId in iconStates.Keys.Where(id => !currentIds.Contains(id)).ToArray())
            {
                iconStates[staleId].Restore();
                iconStates.Remove(staleId);
            }

            grid.cellSize = new Vector2(cellWidth, originalCellSize.y);
            grid.spacing = new Vector2(minimumGap, originalSpacing.y);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            foreach (var file in icons)
            {
                var title = (TMP_Text)IconTitle?.GetValue(file);
                if (!title) continue;
                int id = file.GetInstanceID();
                if (!iconStates.TryGetValue(id, out var state))
                {
                    state = new IconState(file, title);
                    iconStates.Add(id, state);
                }
                state.Apply(cellWidth);
            }
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)grid.transform);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)grid.transform);
            changed = true;
            dirty = false;
            observedGridWidth = ((RectTransform)grid.transform).rect.width;
            appliedCellWidth = grid.cellSize.x;
            appliedSpacing = grid.spacing.x;
        }

        internal void Invalidate()
        {
            dirty = true;
        }

        private static string BuildTitleSignature(IEnumerable<SelectableFile> files)
        {
            if (files == null) return string.Empty;
            return string.Join("\u001f", files.Where(file => file)
                .Select(file => (TMP_Text)IconTitle?.GetValue(file))
                .Where(title => title)
                .Select(title => title.text ?? string.Empty));
        }

        private void Restore()
        {
            if (grid)
            {
                grid.cellSize = originalCellSize;
                grid.spacing = originalSpacing;
                grid.padding = ClonePadding(originalPadding);
                grid.constraint = originalConstraint;
                grid.constraintCount = originalConstraintCount;
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)grid.transform);
            }
            foreach (var state in iconStates.Values) state.Restore();
            iconStates.Clear();
            changed = false;
            dirty = true;
        }

        private static RectOffset ClonePadding(RectOffset source)
        {
            return source == null ? new RectOffset() :
                new RectOffset(source.left, source.right, source.top, source.bottom);
        }
    }
}
