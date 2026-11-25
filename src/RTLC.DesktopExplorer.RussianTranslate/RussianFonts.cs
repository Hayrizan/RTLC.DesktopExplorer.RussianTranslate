using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    // Own all generated resources for the plugin's lifetime. Localization releases its
    // table entries on every locale change; that must not destroy an atlas still in use.
    internal sealed class RussianFonts : IDisposable
    {
        internal const string RussianAlphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя";
        private static readonly Dictionary<string, string> Files = new Dictionary<string, string>
        {
            ["MainFont"] = "PixeloidSans.ttf",
            ["MainFont-GlitchVFX"] = "PixeloidSans.ttf",
            ["MonoLine"] = "PixeloidMono.ttf",
            ["MainFontBold"] = "PixeloidSans-Bold.ttf",
            ["MainFontBold-GlitchVFX"] = "PixeloidSans-Bold.ttf",
            ["MainFontBold-TowerUI"] = "MatrixSansVideoSC-Regular.ttf",
            ["MainFontBold-Colorlens"] = "PixeloidSans-Bold.ttf",
            ["TerminalFont"] = "MatrixSans-Regular.ttf"
        };
        private readonly Dictionary<string, TMP_FontAsset> replacements = new Dictionary<string, TMP_FontAsset>();
        private readonly List<AsyncOperationHandle> originals = new List<AsyncOperationHandle>();
        private readonly ManualLogSource log;
        private readonly LocalizationBundle payload;

        internal RussianFonts(LocalizationBundle payload, ManualLogSource log)
        {
            this.payload = payload;
            this.log = log;

        }

        internal AsyncOperationHandle Prepare(AssetTable english)
        {
            var loads = new List<AsyncOperationHandle>();
            var byKey = new Dictionary<string, AsyncOperationHandle<TMP_FontAsset>>();
            // Retain NotepageFont too: it supports Russian already, and its material must
            // remain alive while both the cloned and original table refer to the asset.
            foreach (var entry in english.Values)
            {
                var operation = english.GetAssetAsync<TMP_FontAsset>(entry.KeyId);
                Addressables.ResourceManager.Acquire(operation);
                originals.Add(operation);
                loads.Add(operation);
                string key = english.SharedData.GetKey(entry.KeyId);
                byKey.Add(key, operation);
            }
            var group = Addressables.ResourceManager.CreateGenericGroupOperation(loads, false);
            var prepared = Addressables.ResourceManager.CreateChainOperation(group, ready =>
            {
                try
                {
                    if (ready.Status != AsyncOperationStatus.Succeeded)
                        throw new InvalidOperationException("English font assets failed to load", ready.OperationException);
                    foreach (var item in byKey)
                        if (Files.TryGetValue(item.Key, out string file))
                        {
                            string referenceKey = file == "PixeloidSans-Bold.ttf" ? "MainFontBold" :
                                file == "PixeloidSans.ttf" ? "MainFont" : item.Key;
                            replacements[item.Key] = Create(item.Value.Result, byKey[referenceKey].Result, file, item.Key);
                        }
                    RussianHardcodedText.ApplyCurrentLocale();
                    return Addressables.ResourceManager.CreateCompletedOperation<object>(this, null);
                }
                catch (Exception exception)
                {
                    log.LogError(exception);
                    return Addressables.ResourceManager.CreateCompletedOperation<object>(null, exception.ToString());
                }
            });
            Addressables.Release(group);
            return prepared;
        }

        private TMP_FontAsset Create(TMP_FontAsset original, TMP_FontAsset reference, string file, string key)
        {
            var source = payload.Require<Font>("fonts/" + file);
            var font = TMP_FontAsset.CreateFontAsset(source, (int)original.faceInfo.pointSize,
                original.atlasPadding, original.atlasRenderMode, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            if (font == null) throw new InvalidOperationException("Cannot create TMP font from " + file);
            font.name = source.name + " SDF [ru:" + key + "]";
            font.hideFlags = HideFlags.HideAndDontSave;
            font.normalStyle = original.normalStyle;
            font.boldStyle = original.boldStyle;
            font.normalSpacingOffset = original.normalSpacingOffset;
            font.boldSpacing = original.boldSpacing;
            font.italicStyle = original.italicStyle;
            font.tabSize = original.tabSize;
            // Keep the shader, keywords, colors and every game-specific effect setting.
            // Only atlas-dependent properties belong to the newly generated font.
            var defaultMaterial = font.material;
            var material = new Material(original.material);
            material.name = original.material.name + " [ru:" + key + "]";
            material.hideFlags = HideFlags.HideAndDontSave;
            material.mainTexture = font.atlasTexture;
            foreach (string property in new[] { "_TextureWidth", "_TextureHeight", "_GradientScale" })
                if (material.HasProperty(property) && defaultMaterial.HasProperty(property))
                    material.SetFloat(property, defaultMaterial.GetFloat(property));
            font.material = material;
            UnityEngine.Object.Destroy(defaultMaterial);
            string characters = new string(Enumerable.Range(32, 95).Select(c => (char)c).ToArray()) +
                RussianAlphabet + "№«»„“”‘’—–… ";
            font.TryAddCharacters(characters, out string missing);
            string missingRussian = new string(RussianAlphabet.Where(c => !font.HasCharacter(c, false, false)).ToArray());
            if (missingRussian.Length != 0)
                throw new InvalidOperationException(file + " did not generate Russian glyphs: " + missingRussian);
            var scale = FontMetricNormalization.Register(font, reference, key == "TerminalFont");
            log.LogInfo($"Font metrics {key}: width {scale.x:F3}, height {scale.y:F3}; reference {reference.name}; monospace={key == "TerminalFont"}.");
            log.LogInfo($"Russian font {key}: {original.name} -> {font.name}; all 66 Russian letters baked, shader {material.shader.name}.");
            return font;
        }

        internal void Apply(AssetTable table)
        {
            foreach (var item in replacements)
                table.GetEntry(item.Key)?.SetAssetOverride(item.Value);
        }

        internal TMP_FontAsset StaticLabelFont(TMP_FontAsset original)
        {
            string key = original.name == "Windows Regular SDF" ? "MainFont" :
                original.name == "Windows Bold SDF" ? "MainFontBold" :
                original.name == "Windows Bold SDF_Tower" ? "MainFontBold-TowerUI" : null;
            return key != null && replacements.TryGetValue(key, out var font) ? font : null;
        }

        public void Dispose()
        {
            foreach (var font in replacements.Values)
            {
                foreach (var atlas in font.atlasTextures) UnityEngine.Object.Destroy(atlas);
                UnityEngine.Object.Destroy(font.material);
                UnityEngine.Object.Destroy(font);
            }
            foreach (var handle in originals)
                if (handle.IsValid()) Addressables.Release(handle);
            replacements.Clear();
            originals.Clear();
        }
    }
}
