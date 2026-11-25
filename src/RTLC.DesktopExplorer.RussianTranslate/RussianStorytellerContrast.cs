using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Program.StoryTellerPlus;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch]
    internal static class RussianStorytellerContrast
    {
        private sealed class MaterialOverride
        {
            internal readonly TMP_Text Text;
            internal readonly TMP_FontAsset Font;
            internal readonly Material Material;
            internal readonly Color FaceColor;
            internal readonly Color OutlineColor;
            internal readonly float OutlineWidth;

            internal MaterialOverride(TMP_Text text, TMP_FontAsset font, Material material)
            {
                Text = text;
                Font = font;
                Material = material;
                FaceColor = material.GetColor("_FaceColor");
                OutlineColor = material.GetColor("_OutlineColor");
                OutlineWidth = material.GetFloat("_OutlineWidth");
            }
        }

        private static readonly FieldInfo TextField = AccessTools.Field(typeof(StoryReaderController), "textField");
        private static readonly ConditionalWeakTable<StoryReaderController, MaterialOverride> Overrides =
            new ConditionalWeakTable<StoryReaderController, MaterialOverride>();

        private static IEnumerable<MethodBase> TargetMethods()
        {
            var display = AccessTools.Method(typeof(StoryReaderController), "DisplayStorytellerText", new[] { typeof(string) });
            if (display != null)
                yield return display;

            var textShown = AccessTools.Method(typeof(StoryReaderController), "OnTextShowed");
            if (textShown != null)
                yield return textShown;

            var destroy = AccessTools.Method(typeof(StoryReaderController), "OnDestroy");
            if (destroy != null)
                yield return destroy;

            var disable = AccessTools.Method(typeof(StoryReaderController), "OnDisable");
            if (disable != null)
                yield return disable;

            var enable = AccessTools.Method(typeof(StoryReaderController), "OnEnable");
            if (enable != null)
                yield return enable;
        }

        private static void Postfix(MethodBase __originalMethod, StoryReaderController __instance)
        {
            if (__originalMethod == null || ReferenceEquals(__instance, null))
                return;

            if (__originalMethod.Name == "OnDestroy" || __originalMethod.Name == "OnDisable")
            {
                Restore(__instance);
                return;
            }

            if (!__instance || TextField == null)
                return;

            var text = TextField.GetValue(__instance) as TMP_Text;
            if (ReferenceEquals(text, null))
            {
                Restore(__instance);
                return;
            }

            if (LocalizationSettings.SelectedLocale?.Identifier.Code == Plugin.RussianCode)
            {
                Apply(__instance, text);
                Plugin.Instance?.StartCoroutine(ApplyAfterCanvasUpdate(__instance, text));
            }
            else
                Restore(__instance);
        }

        private static IEnumerator ApplyAfterCanvasUpdate(StoryReaderController controller, TMP_Text text)
        {
            yield return new WaitForEndOfFrame();
            if (!controller || !controller.isActiveAndEnabled || !text)
                yield break;

            if (LocalizationSettings.SelectedLocale?.Identifier.Code == Plugin.RussianCode)
                Apply(controller, text);
            else
                Restore(controller);
        }

        private static void Apply(StoryReaderController controller, TMP_Text text)
        {
            if (!text || !text.font)
                return;

            var material = text.fontMaterial;
            if (Overrides.TryGetValue(controller, out var existing) &&
                ReferenceEquals(existing.Text, text) && existing.Font == text.font &&
                existing.Material == material)
            {
                SetContrast(text, material);
                return;
            }

            Restore(controller);
            material = text.fontMaterial;
            if (!material || !material.HasProperty("_FaceColor") ||
                !material.HasProperty("_OutlineColor") || !material.HasProperty("_OutlineWidth"))
                return;

            Overrides.Add(controller, new MaterialOverride(text, text.font, material));
            SetContrast(text, material);
        }

        private static void SetContrast(TMP_Text text, Material material)
        {
            material.SetColor("_FaceColor", new Color(0.15f, 1f, 0.35f, 1f));
            material.SetColor("_OutlineColor", Color.black);
            material.SetFloat("_OutlineWidth", 0f);
            RefreshRendererMaterial(text, material);
            text.SetMaterialDirty();
            text.ForceMeshUpdate(true);
        }

        private static void RefreshRendererMaterial(TMP_Text text, Material material)
        {
            if (!text || !material || !text.canvasRenderer)
                return;

            Material renderMaterial = text.materialForRendering;
            if (!renderMaterial)
                renderMaterial = material;
            text.canvasRenderer.SetMaterial(renderMaterial, 0);
            if (renderMaterial.mainTexture)
                text.canvasRenderer.SetTexture(renderMaterial.mainTexture);
        }

        private static void Restore(StoryReaderController controller)
        {
            if (!Overrides.TryGetValue(controller, out var existing))
                return;

            var text = existing.Text;
            if (text && text.font == existing.Font)
            {
                var material = text.fontMaterial;
                if (material && material.HasProperty("_FaceColor") &&
                    material.HasProperty("_OutlineColor") && material.HasProperty("_OutlineWidth"))
                {
                    material.SetColor("_FaceColor", existing.FaceColor);
                    material.SetColor("_OutlineColor", existing.OutlineColor);
                    material.SetFloat("_OutlineWidth", existing.OutlineWidth);
                    RefreshRendererMaterial(text, material);
                    text.SetMaterialDirty();
                    text.ForceMeshUpdate(true);
                }
            }

            Overrides.Remove(controller);
        }
    }
}
