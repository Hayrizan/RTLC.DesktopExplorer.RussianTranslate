using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Program.Mall.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianStaticLabelFonts
    {
        private sealed class Original
        {
            public TMP_FontAsset Font, Replacement;
            public Material Material, ReplacementMaterial;
        }

        private static readonly Dictionary<TMP_Text, Original> originals = new Dictionary<TMP_Text, Original>();

        internal static void Apply(TMP_Text text, string source, bool requiresCyrillic = false)
        {
            if (!RussianHardcodedText.IsRussian)
            {
                if (!originals.TryGetValue(text, out var original)) return;
                if (text.font == original.Replacement)
                {
                    bool ownMaterial = text.fontSharedMaterial == original.ReplacementMaterial;
                    text.font = original.Font;
                    if (ownMaterial) text.fontSharedMaterial = original.Material;
                }
                Object.Destroy(original.ReplacementMaterial);
                originals.Remove(text);
                return;
            }
            // These serialized labels have no font localizer in the original prefabs.
            bool isStaticLabel = source == "Zoom" || source == "INSTRUCTIONS" ||
                source?.StartsWith("Game Version: ", System.StringComparison.Ordinal) == true ||
                !System.String.Equals(RussianHardcodedText.Translate(source), source, System.StringComparison.Ordinal);
            bool isProfileName = RussianProfileNames.IsProfileName(text, source);
            if ((!isStaticLabel && !isProfileName && !requiresCyrillic) || text.font == null) return;

            Material sourceMaterial = text.fontSharedMaterial;
            if (originals.TryGetValue(text, out var existing))
            {
                if (text.font == existing.Replacement) return;

                // LoginUserController swaps regular/bold fonts as selection changes.
                // Rebase our atlas/material copy on the newly assigned source font.
                bool reusedReplacementMaterial = text.fontSharedMaterial == existing.ReplacementMaterial;
                sourceMaterial = reusedReplacementMaterial ? text.font.material : text.fontSharedMaterial;
                Object.Destroy(existing.ReplacementMaterial);
                existing.Font = text.font;
                existing.Material = sourceMaterial;
                existing.Replacement = null;
                existing.ReplacementMaterial = null;
            }

            var font = Plugin.Instance?.StaticLabelFont(text.font);
            if (font == null) return;
            var material = new Material(sourceMaterial);
            material.name += " [ru:static label]";
            material.hideFlags = HideFlags.HideAndDontSave;
            material.mainTexture = font.atlasTexture;
            foreach (string property in new[] { "_TextureWidth", "_TextureHeight", "_GradientScale" })
                if (material.HasProperty(property) && font.material.HasProperty(property))
                    material.SetFloat(property, font.material.GetFloat(property));
            if (!originals.TryGetValue(text, out var saved))
            {
                saved = new Original { Font = text.font, Material = text.fontSharedMaterial };
                originals.Add(text, saved);
            }
            saved.Replacement = font;
            saved.ReplacementMaterial = material;
            text.font = font;
            text.fontSharedMaterial = material;

            // Assigning a TMP font can reset custom per-label material properties.
            // Reapply the original styling after the font switch, then restore the
            // replacement atlas settings required by the Cyrillic font asset.
            material.CopyPropertiesFromMaterial(sourceMaterial);
            material.mainTexture = font.atlasTexture;
            foreach (string property in new[] { "_TextureWidth", "_TextureHeight", "_GradientScale" })
                if (material.HasProperty(property) && font.material.HasProperty(property))
                    material.SetFloat(property, font.material.GetFloat(property));
            text.fontSharedMaterial = material;
        }

        internal static void Release(TMP_Text text)
        {
            if (!originals.TryGetValue(text, out var saved)) return;
            Object.Destroy(saved.ReplacementMaterial);
            originals.Remove(text);
        }

        internal static void CleanupDestroyed()
        {
            // Unity does not invoke OnDestroy on a panel that was never active.
            foreach (var text in originals.Keys.Where(text => !text).ToArray()) Release(text);
        }

        internal static void Dispose()
        {
            foreach (var item in originals)
            {
                if (item.Key && item.Key.font == item.Value.Replacement)
                {
                    item.Key.font = item.Value.Font;
                    item.Key.fontSharedMaterial = item.Value.Material;
                }
                Object.Destroy(item.Value.ReplacementMaterial);
            }
            originals.Clear();
        }
    }

    [HarmonyPatch(typeof(NextRoomOptionController), nameof(NextRoomOptionController.SetupNextRoomOption))]
    internal static class RussianNextRoomOptionFont
    {
        private static void Prefix(TMP_Text ___optionLabel, string optionText)
        {
            // RefreshNavigationOptions measures these labels immediately after setup,
            // before the prefab's asynchronous font localizer can finish.
            RussianStaticLabelFonts.Apply(___optionLabel, optionText,
                requiresCyrillic: optionText?.Any(character => character >= '\u0400' && character <= '\u04FF') == true);
        }
    }

    [HarmonyPatch(typeof(NextRoomOptionController), nameof(NextRoomOptionController.SetupNextRoomOption))]
    internal static class RussianNextRoomSubmenuLayout
    {
        private sealed class OriginalWidth
        {
            public float Value;
        }

        private static readonly ConditionalWeakTable<NextRoomOptionController, OriginalWidth> originalWidths =
            new ConditionalWeakTable<NextRoomOptionController, OriginalWidth>();

        private static void Postfix(NextRoomOptionController __instance)
        {
            if (__instance == null) return;
            var subOptions = __instance.GetSubOptions()
                .Select(gameObject => gameObject ? gameObject.GetComponent<NextRoomOptionController>() : null)
                .Where(option => option != null)
                .ToArray();
            if (subOptions.Length == 0) return;

            if (!RussianHardcodedText.IsRussian)
            {
                foreach (var option in subOptions)
                {
                    if (!originalWidths.TryGetValue(option, out var original)) continue;
                    option.SetOptionWidth(original.Value);
                    LayoutRebuilder.MarkLayoutForRebuild((RectTransform)option.transform);
                    originalWidths.Remove(option);
                }
                return;
            }

            // Roomie action buttons are authored at 102 px, which clips Russian
            // captions such as "Убрать из друзей". Give every action the same
            // full-size width based on the widest localized caption plus padding.
            float width = Mathf.Ceil(subOptions.Max(option => option.GetPreferredContentWidth()) + 16f);
            foreach (var option in subOptions)
            {
                var rect = (RectTransform)option.transform;
                if (!originalWidths.TryGetValue(option, out _))
                    originalWidths.Add(option, new OriginalWidth { Value = rect.sizeDelta.x });
                option.SetOptionWidth(width);
                if (option.gameObject.activeInHierarchy)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                else
                    LayoutRebuilder.MarkLayoutForRebuild(rect);
            }
        }
    }

    [HarmonyPatch(typeof(TextMeshProUGUI), "OnDestroy")]
    internal static class RussianStaticUiFontCleanup
    {
        private static void Prefix(TextMeshProUGUI __instance) => RussianStaticLabelFonts.Release(__instance);
    }

    [HarmonyPatch(typeof(TextMeshPro), "OnDestroy")]
    internal static class RussianStaticWorldFontCleanup
    {
        private static void Prefix(TextMeshPro __instance) => RussianStaticLabelFonts.Release(__instance);
    }
}
