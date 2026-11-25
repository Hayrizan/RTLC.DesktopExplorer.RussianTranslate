using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MTextFont = TinyGiantStudio.Text.Font;
using TinyGiantStudio.Text;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianTextRain
    {
        private const string SpawnerTypeName = "OS.ScreenSaver.Text.TextRainSpawner";
        private static readonly string[] LocalizedWordPool =
        {
            "ПРИВЕТ", "UNITY", "РЕТРО", "ЗАСТАВКА", "DOTWEEN", "ДОЖДЬ", "АТМОСФЕРА"
        };
        private static readonly Dictionary<string, string> WordTranslations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["HELLO"] = "ПРИВЕТ",
            ["RETRO"] = "РЕТРО",
            ["SCREENSAVER"] = "ЗАСТАВКА",
            ["RAIN"] = "ДОЖДЬ",
            ["VIBES"] = "АТМОСФЕРА"
        };
        private static readonly ConditionalWeakTable<object, SpawnerState> SpawnerStates =
            new ConditionalWeakTable<object, SpawnerState>();
        private static readonly Dictionary<MTextFont, MTextFont> LocalizedFonts =
            new Dictionary<MTextFont, MTextFont>();
        private static readonly Dictionary<MTextFont, MTextFont> OriginalFonts =
            new Dictionary<MTextFont, MTextFont>();
        private static readonly Dictionary<MTextFont, bool> NativeCyrillicSupport =
            new Dictionary<MTextFont, bool>();
        private static readonly FieldInfo WordsField = AccessTools.Field(AccessTools.TypeByName(SpawnerTypeName), "words");
        private static readonly FieldInfo TextPrefabField = AccessTools.Field(AccessTools.TypeByName(SpawnerTypeName), "textPrefab");
        private static MTextFont cyrillicFont;
        private static GameObject importerObject;
        private static bool fallbackFailureLogged;

        private sealed class SpawnerState
        {
            internal readonly string[] OriginalWords;
            internal Modular3DText PrefabText;
            internal MTextFont OriginalFont;

            internal SpawnerState(string[] words) => OriginalWords = words;
        }

        internal static void ApplyCurrentLocale()
        {
            Type spawnerType = AccessTools.TypeByName(SpawnerTypeName);
            if (spawnerType == null || WordsField == null || TextPrefabField == null)
                return;

            bool russian = LocalizationSettings.SelectedLocale?.Identifier.Code == Plugin.RussianCode;
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(spawnerType))
            {
                if (candidate is Component component && component.gameObject.scene.IsValid())
                    ApplyToSpawner(candidate, russian);
            }
        }

        internal static void OnSpawnerEnabled(object instance)
        {
            bool russian = LocalizationSettings.SelectedLocale?.Identifier.Code == Plugin.RussianCode;
            ApplyToSpawner(instance, russian);
        }

        private static void ApplyToSpawner(object instance, bool russian)
        {
            var state = SpawnerStates.GetValue(instance, key =>
            {
                var words = WordsField.GetValue(key) as string[];
                return new SpawnerState(words == null ? Array.Empty<string>() : (string[])words.Clone());
            });

            var prefab = TextPrefabField.GetValue(instance) as GameObject;
            var prefabText = prefab != null ? prefab.GetComponent<Modular3DText>() : null;
            string[] localizedWords = russian ? LocalizeWords(state.OriginalWords) : null;
            bool needsCyrillic = localizedWords != null && localizedWords.Any(ContainsCyrillic);
            if (prefabText != null)
            {
                if (state.PrefabText != prefabText)
                {
                    state.PrefabText = prefabText;
                    state.OriginalFont = prefabText.Font;
                }

                if (needsCyrillic)
                {
                    if (!TryApplyCyrillicFont(prefabText))
                    {
                        WordsField.SetValue(instance, (string[])state.OriginalWords.Clone());
                        return;
                    }
                }
                else if (state.OriginalFont != null)
                {
                    prefabText.Font = state.OriginalFont;
                }
            }
            else if (needsCyrillic)
            {
                LogFallbackFailure("The 3D text prefab has no Modular3DText component.");
                WordsField.SetValue(instance, (string[])state.OriginalWords.Clone());
                return;
            }

            WordsField.SetValue(instance, russian
                ? localizedWords
                : (string[])state.OriginalWords.Clone());
        }

        private static string[] LocalizeWords(string[] source)
        {
            var result = (string[])source.Clone();
            for (int index = 0; index < result.Length; index++)
            {
                if (result[index] != null && WordTranslations.TryGetValue(result[index], out string translated))
                    result[index] = translated;
            }
            return result;
        }

        internal static bool TryApplyCyrillicFont(Modular3DText text)
        {
            if (text == null || text.Font == null)
                return false;

            try
            {
                MTextFont original = text.Font;
                if (OriginalFonts.ContainsKey(original))
                    return true;

                if (!NativeCyrillicSupport.TryGetValue(original, out bool supportsCyrillic))
                {
                    supportsCyrillic = SupportsRainCyrillic(original);
                    NativeCyrillicSupport.Add(original, supportsCyrillic);
                }
                if (supportsCyrillic)
                    return true;

                if (!LocalizedFonts.TryGetValue(original, out MTextFont localized))
                {
                    localized = UnityEngine.Object.Instantiate(original);
                    localized.name = original.name + " [Cyrillic fallback]";
                    localized.hideFlags = HideFlags.HideAndDontSave;
                    localized.fallbackFonts = new List<MTextFont>(original.fallbackFonts ?? new List<MTextFont>());
                    MTextFont fallback = GetCyrillicFont();
                    if (!localized.fallbackFonts.Contains(fallback))
                        localized.fallbackFonts.Add(fallback);
                    LocalizedFonts.Add(original, localized);
                    OriginalFonts.Add(localized, original);
                }

                text.Font = localized;
                return true;
            }
            catch (Exception exception)
            {
                LogFallbackFailure("Could not prepare Cyrillic 3D glyphs: " + exception.Message);
                return false;
            }
        }

        private static bool SupportsRainCyrillic(MTextFont font)
        {
            string cyrillic = string.Concat(LocalizedWordPool.Where(ContainsCyrillic).Distinct());
            var typeFace = font.TypeFace;
            if (typeFace == null)
                return false;
            foreach (char character in cyrillic)
            {
                if (!IsCyrillic(character))
                    continue;
                typeFace.SetGlyphData(character);
                if (!typeFace.glyphs.TryGetValue(character, out var glyph) || glyph.GlyphIndex == 0)
                    return false;
            }
            return true;
        }

        private static MTextFont GetCyrillicFont()
        {
            if (cyrillicFont != null)
                return cyrillicFont;

            var fontBytes = Plugin.Payload.Require<TextAsset>("font-bytes/PixeloidSans.ttf");

            importerObject = new GameObject("RTLC Russian 3D font importer")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            UnityEngine.Object.DontDestroyOnLoad(importerObject);
            var importer = importerObject.AddComponent<RuntimeFontImporter>();
            cyrillicFont = importer.CreateFontFromTTFFile(fontBytes.bytes);
            cyrillicFont.name = "Pixeloid Sans [MText Cyrillic fallback]";
            cyrillicFont.hideFlags = HideFlags.HideAndDontSave;
            Plugin.LogInfo("Loaded Pixeloid Sans as a generated-mesh fallback for missing 3D Cyrillic glyphs.");
            return cyrillicFont;
        }

        internal static bool ContainsCyrillic(string text) =>
            text != null && text.Any(IsCyrillic);

        private static bool IsCyrillic(char character) => character >= '\u0400' && character <= '\u052F';

        private static void LogFallbackFailure(string message)
        {
            if (fallbackFailureLogged)
                return;
            fallbackFailureLogged = true;
            Plugin.LogError("3D text localization was left in English. " + message);
        }

        internal static void Dispose()
        {
            Type spawnerType = AccessTools.TypeByName(SpawnerTypeName);
            if (spawnerType != null && WordsField != null && TextPrefabField != null)
            {
                foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(spawnerType))
                {
                    if (!(candidate is Component component) || !component.gameObject.scene.IsValid())
                        continue;
                    if (!SpawnerStates.TryGetValue(candidate, out SpawnerState state))
                        continue;
                    WordsField.SetValue(candidate, (string[])state.OriginalWords.Clone());
                    if (state.PrefabText != null && state.OriginalFont != null)
                        state.PrefabText.Font = state.OriginalFont;
                }
            }

            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(typeof(Modular3DText)))
            {
                if (candidate is Modular3DText text && text.gameObject.scene.IsValid() &&
                    text.Font != null && OriginalFonts.TryGetValue(text.Font, out MTextFont original))
                    text.Font = original;
            }

            foreach (MTextFont font in LocalizedFonts.Values)
                if (font != null) UnityEngine.Object.Destroy(font);
            if (cyrillicFont != null) UnityEngine.Object.Destroy(cyrillicFont);
            if (importerObject != null) UnityEngine.Object.Destroy(importerObject);
            LocalizedFonts.Clear();
            OriginalFonts.Clear();
            NativeCyrillicSupport.Clear();
            cyrillicFont = null;
            importerObject = null;
        }

    }

    [HarmonyPatch]
    internal static class RussianTextRainSpawnerEnablePatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("OS.ScreenSaver.Text.TextRainSpawner"), "OnEnable");

        private static void Prefix(object __instance) => RussianTextRain.OnSpawnerEnabled(__instance);
    }

    [HarmonyPatch]
    internal static class RussianModular3DTextCyrillicPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(Modular3DText), "UpdateText", new[] { typeof(string) });

        private static void Prefix(Modular3DText __instance, string newText)
        {
            if (LocalizationSettings.SelectedLocale?.Identifier.Code == Plugin.RussianCode &&
                RussianTextRain.ContainsCyrillic(newText))
                RussianTextRain.TryApplyCyrillicFont(__instance);
        }
    }
}
