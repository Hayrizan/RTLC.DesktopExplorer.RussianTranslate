using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine.TextCore.LowLevel;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch]
    internal static class RussianNotepageGlyphs
    {
        private static readonly AccessTools.FieldRef<TMP_FontAsset, GlyphRenderMode> RenderMode =
            AccessTools.FieldRefAccess<TMP_FontAsset, GlyphRenderMode>("m_AtlasRenderMode");

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(TMP_FontAsset), "TryAddCharacterInternal");
            yield return AccessTools.Method(typeof(TMP_FontAsset), "TryAddCharacters",
                new[] { typeof(string), typeof(string).MakeByRefType(), typeof(bool) });
            yield return AccessTools.Method(typeof(TMP_FontAsset), "TryAddCharacters",
                new[] { typeof(uint[]), typeof(uint[]).MakeByRefType(), typeof(bool) });
        }

        private static void Prefix(TMP_FontAsset __instance, out bool __state)
        {
            // TMP can retain a managed reference after Unity has destroyed the
            // native font asset. Canvas/layout rebuilds may still ask that stale
            // reference for a glyph while leaving a scene; Unity's `name` getter
            // throws in that state, so keep the Harmony prefix side-effect free.
            if (__instance == null)
            {
                __state = false;
                return;
            }

            __state = __instance.name == "FSEX302 SDF" && RenderMode(__instance) == GlyphRenderMode.SDF32 &&
                RussianHardcodedText.IsRussian;
            // The shipped atlas contains Latin only. SDF32 generates each newly
            // encountered Cyrillic glyph for ~1 second, blocking TMP_InputField.
            // SDFAA uses the same embedded font, point size, padding and metrics.
            // Existing atlas glyphs and the material are left intact.
            if (__state) RenderMode(__instance) = GlyphRenderMode.SDFAA;
        }

        private static void Finalizer(TMP_FontAsset __instance, bool __state)
        {
            if (__state) RenderMode(__instance) = GlyphRenderMode.SDF32;
        }
    }
}
