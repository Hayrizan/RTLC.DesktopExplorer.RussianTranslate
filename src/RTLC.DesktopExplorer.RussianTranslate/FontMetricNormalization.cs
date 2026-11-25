using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch]
    internal static class FontMetricNormalization
    {
        private sealed class Metrics
        {
            public float X, Y;
            public float Cell;
            public readonly HashSet<Glyph> Done = new HashSet<Glyph>();
            public readonly HashSet<TMP_GlyphPairAdjustmentRecord> Pairs = new HashSet<TMP_GlyphPairAdjustmentRecord>();
        }

        private static readonly ConditionalWeakTable<TMP_FontAsset, Metrics> fonts = new ConditionalWeakTable<TMP_FontAsset, Metrics>();

        internal static Vector2 Register(TMP_FontAsset font, TMP_FontAsset reference, bool monospace = false)
        {
            float sourceWidth = 0, referenceWidth = 0;
            // Lowercase is the body of both English and Russian UI/prose. Measure
            // the same alphabet in each asset, rather than scaling to a label's box.
            foreach (char c in "abcdefghijklmnopqrstuvwxyz")
            {
                if (!font.characterLookupTable.TryGetValue(c, out var source) ||
                    !reference.characterLookupTable.TryGetValue(c, out var target)) continue;
                sourceWidth += source.glyph.metrics.horizontalAdvance;
                referenceWidth += target.glyph.metrics.horizontalAdvance;
            }
            if (sourceWidth <= 0 || referenceWidth <= 0 || font.faceInfo.capLine <= 0 || reference.faceInfo.capLine <= 0)
                throw new InvalidOperationException("Cannot calibrate font metrics against " + reference.name);
            var metrics = new Metrics
            {
                X = Mathf.Clamp(referenceWidth / sourceWidth, 0.5f, 1.25f),
                Y = reference.faceInfo.capLine / font.faceInfo.capLine
            };
            if (monospace) metrics.Cell = reference.characterLookupTable['a'].glyph.metrics.horizontalAdvance;
            fonts.Add(font, metrics);
            // Retain standard point-size / face scale math, and keep the game's
            // line, baseline and decoration positions. Only glyph geometry changes;
            // atlas rectangles and texture coordinates must remain untouched.
            var face = font.faceInfo;
            var original = reference.faceInfo;
            face.lineHeight = original.lineHeight;
            face.ascentLine = original.ascentLine;
            face.capLine = original.capLine;
            face.meanLine = original.meanLine;
            face.baseline = original.baseline;
            face.descentLine = original.descentLine;
            face.superscriptOffset = original.superscriptOffset;
            face.superscriptSize = original.superscriptSize;
            face.subscriptOffset = original.subscriptOffset;
            face.subscriptSize = original.subscriptSize;
            face.tabWidth = original.tabWidth;
            face.underlineOffset = original.underlineOffset;
            face.underlineThickness = original.underlineThickness;
            face.strikethroughOffset = original.strikethroughOffset;
            face.strikethroughThickness = original.strikethroughThickness;
            font.faceInfo = face;
            Apply(font);
            return new Vector2(metrics.X, metrics.Y);
        }

        private static void Apply(TMP_FontAsset font)
        {
            if (!fonts.TryGetValue(font, out var scale)) return;
            foreach (var glyph in font.glyphTable)
            {
                // The same glyph may be shared by Latin/Cyrillic characters. Scale
                // each object once, including glyphs added to the dynamic atlas later.
                if (!scale.Done.Add(glyph)) continue;
                var m = glyph.metrics;
                bool cell = scale.Cell > 0 && m.horizontalAdvance > 0;
                float x = cell ? Mathf.Min(scale.X, scale.Cell / m.horizontalAdvance) : scale.X;
                float offset = cell ? (scale.Cell - m.horizontalAdvance * x) * 0.5f : 0;
                glyph.metrics = new GlyphMetrics(m.width * x, m.height * scale.Y,
                    m.horizontalBearingX * x + offset, m.horizontalBearingY * scale.Y,
                    cell ? scale.Cell : m.horizontalAdvance * x);
            }
            if (font.fontFeatureTable == null) return;
            foreach (var pair in font.fontFeatureTable.glyphPairAdjustmentRecords)
            {
                if (!scale.Pairs.Add(pair)) continue;
                pair.firstAdjustmentRecord = Adjust(pair.firstAdjustmentRecord, scale);
                pair.secondAdjustmentRecord = Adjust(pair.secondAdjustmentRecord, scale);
            }
        }

        private static TMP_GlyphAdjustmentRecord Adjust(TMP_GlyphAdjustmentRecord record, Metrics scale)
        {
            var value = record.glyphValueRecord;
            // Proportional kerning follows the same geometric scale. A terminal
            // grid must not move columns through the source font's GPOS kerning.
            record.glyphValueRecord = scale.Cell > 0 ? new TMP_GlyphValueRecord(0, 0, 0, 0) :
                new TMP_GlyphValueRecord(value.xPlacement * scale.X, value.yPlacement * scale.Y,
                    value.xAdvance * scale.X, value.yAdvance * scale.Y);
            return record;
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(TMP_FontAsset), "TryAddCharacterInternal");
            yield return AccessTools.Method(typeof(TMP_FontAsset), "TryAddCharacters", new[] { typeof(string), typeof(string).MakeByRefType(), typeof(bool) });
            yield return AccessTools.Method(typeof(TMP_FontAsset), "TryAddCharacters", new[] { typeof(uint[]), typeof(uint[]).MakeByRefType(), typeof(bool) });
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(TMP_FontAsset)))
                if (method.Name == "UpdateGlyphAdjustmentRecords") yield return method;
        }

        private static void Postfix(TMP_FontAsset __instance)
        {
            // TMP's dynamic-font callbacks can finish after Unity destroyed the
            // corresponding native asset during a locale/scene transition.
            if (__instance != null) Apply(__instance);
        }
    }
}
