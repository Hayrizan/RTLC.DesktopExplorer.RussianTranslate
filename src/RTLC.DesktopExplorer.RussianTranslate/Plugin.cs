using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json;
using Settings;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using Utils;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [BepInPlugin(Guid, "Desktop Explorer — Русский перевод", "0.10.297")]
    [BepInProcess("DesktopExplorer.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "rtlc.desktopexplorer.russiantranslate";
        public const string RussianCode = "ru-RU";
        public const string EnglishCode = "en-US";
        internal static Plugin Instance { get; private set; }
        internal static LocalizationBundle Payload { get; private set; }
        internal static string PluginDirectory { get; private set; }
        internal TranslationCatalog Catalog { get; private set; }
        internal RussianSpriteOverrides SpriteOverrides { get; private set; }
        internal ConfigEntry<bool> LanguageInitialized { get; private set; }
        internal static void LogInfo(string message) => Instance?.Logger.LogInfo(message);
        internal static void LogWarning(string message) => Instance?.Logger.LogWarning(message);
        internal static void LogError(string message) => Instance?.Logger.LogError(message);
        internal TMPro.TMP_FontAsset StaticLabelFont(TMPro.TMP_FontAsset original) => fonts?.StaticLabelFont(original);
        private Harmony harmony;
        private ITableProvider originalStringProvider;
        private ITableProvider originalAssetProvider;
        private RussianTableProvider stringProvider;
        private RussianTableProvider assetProvider;
        private RussianFonts fonts;
        private float nextTextureScan;
        private string lastAppliedLocaleCode;

        private void Awake()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);
            Instance = this;
            PluginDirectory = Path.GetDirectoryName(Info.Location);
            Payload = new LocalizationBundle(Path.Combine(PluginDirectory, "rtlc.bundle"));
            RussianSerializedTextVariants.Load(Payload, Logger);
            LanguageInitialized = Config.Bind("Language", "Initialized", false,
                "Русский выбран при первом запуске. Далее язык сохраняется средствами игры. Не сбрасывайте при обновлении.");
            Catalog = TranslationCatalog.Load(Payload);
            RussianRoomieNameKeys.EnsureAllRoomiesCanBeLocalized();

            // The game indexes BOTH lists directly, so their ordering must remain identical.
            AddRussianFirst();
            originalStringProvider = LocalizationSettings.StringDatabase.TableProvider;
            originalAssetProvider = LocalizationSettings.AssetDatabase.TableProvider;
            stringProvider = new RussianTableProvider(Catalog, originalStringProvider, Logger);
            fonts = new RussianFonts(Payload, Logger);
            SpriteOverrides = new RussianSpriteOverrides(Payload, Logger);
            assetProvider = new RussianTableProvider(Catalog, originalAssetProvider, Logger, fonts);
            LocalizationSettings.StringDatabase.TableProvider = stringProvider;
            LocalizationSettings.AssetDatabase.TableProvider = assetProvider;
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Logger.LogInfo($"Russian locale enabled; {Catalog.Count} manually reviewed translation entries loaded.");
        }

        internal static void AddRussianFirst()
        {
            var codes = DesktopExplorerUtils.OrderedSupportedLocaleCodes;
            var names = DesktopExplorerUtils.OrderedSupportedLocaleDisplayNames;
            int existing = codes.IndexOf(RussianCode);
            if (existing == 0 && names[0] == "Русский")
                return;
            if (existing >= 0)
            {
                codes.RemoveAt(existing);
                names.RemoveAt(existing);
            }
            codes.Insert(0, RussianCode);
            names.Insert(0, "Русский");
        }

        internal async UniTask AfterLoadSettings(UniTask operation)
        {
            await operation;
            if (LanguageInitialized.Value)
                return;

            await DESettingsProvider.SELECTED_LANGUAGE.SetNewValue(RussianCode);
            // Mark completion only AFTER the game's own persistence operation succeeded.
            LanguageInitialized.Value = true;
            Config.Save();
            Logger.LogInfo("First plugin launch: Russian selected and saved. Future language choices will be respected.");
        }

        internal async UniTask AfterBootstrap(UniTask operation)
        {
            await operation;
            // OSBootstrapper forces en-US on a fresh save, after loading saved preferences.
            // Apply the actual saved preference once bootstrap has finished, before its caller opens a menu.
            string code = DESettingsProvider.SELECTED_LANGUAGE.GetValue();
            if (!string.IsNullOrEmpty(code))
                DesktopExplorerUtils.SetLanguageFromLocaleCode(code);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSelectedLocale($"scene load ({scene.name})");
            RussianFooterLayout.Attach(scene);
            RussianTextRain.ApplyCurrentLocale();
            StartCoroutine(AuditFontsAfterLoad());
        }

        private void Update()
        {
            RussianAdminInterludeCollisionScope.Refresh();
            RefreshLocalizedState();
            if (Time.unscaledTime < nextTextureScan)
                return;

            nextTextureScan = Time.unscaledTime + 3f;
            RussianStaticLabelFonts.CleanupDestroyed();
            SpriteOverrides?.ScanTextures();
            RussianTitleLogo.Refresh(Payload);
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            if (locale == null)
            {
                EnsureSelectedLocale("locale change");
                return;
            }
            RefreshLocalizedState();
        }

        private void RefreshLocalizedState()
        {
            var selectedLocale = LocalizationSettings.SelectedLocale;
            if (selectedLocale == null)
            {
                EnsureSelectedLocale("localized state refresh");
                selectedLocale = LocalizationSettings.SelectedLocale;
                if (selectedLocale == null)
                    return;
            }

            string localeCode = selectedLocale.Identifier.Code;
            if (string.Equals(localeCode, lastAppliedLocaleCode, StringComparison.Ordinal))
                return;
            lastAppliedLocaleCode = localeCode;
            RussianClockFormat.Refresh();
            RussianHardcodedText.ApplyCurrentLocale();
            RussianTextRain.ApplyCurrentLocale();
            SpriteOverrides?.ApplyCurrentLocale();
        }

        internal void EnsureSelectedLocale(string reason)
        {
            if (!LocalizationSettings.HasSettings || LocalizationSettings.SelectedLocale != null)
                return;

            string localeCode = lastAppliedLocaleCode;
            if (string.IsNullOrWhiteSpace(localeCode))
                localeCode = DESettingsProvider.SELECTED_LANGUAGE?.GetValue();
            if (string.IsNullOrWhiteSpace(localeCode))
                localeCode = LanguageInitialized.Value ? EnglishCode : RussianCode;

            var availableLocales = LocalizationSettings.AvailableLocales;
            var locale = availableLocales?.GetLocale(localeCode);
            if (locale == null)
            {
                string fallbackCode = LanguageInitialized.Value ? EnglishCode : RussianCode;
                locale = availableLocales?.GetLocale(fallbackCode);
            }
            if (locale == null)
                return;

            Logger.LogWarning($"Selected locale was cleared during {reason}; restoring {locale.Identifier.Code}.");
            LocalizationSettings.SelectedLocale = locale;
        }

        private IEnumerator AuditFontsAfterLoad()
        {
            yield return null;
            yield return null;
            SpriteOverrides?.ApplyCurrentLocale();

        }

        private void OnDestroy()
        {
            Logger.LogInfo("Translator unloading.");
            RussianAdminInterludeCollisionScope.RestoreAll();
            RussianTextRain.Dispose();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            harmony?.UnpatchSelf();
            if (LocalizationSettings.HasSettings)
            {
                if (ReferenceEquals(LocalizationSettings.StringDatabase.TableProvider, stringProvider))
                    LocalizationSettings.StringDatabase.TableProvider = originalStringProvider;
                if (ReferenceEquals(LocalizationSettings.AssetDatabase.TableProvider, assetProvider))
                    LocalizationSettings.AssetDatabase.TableProvider = originalAssetProvider;
            }
            stringProvider?.Dispose();
            assetProvider?.Dispose();
            SpriteOverrides?.Dispose();
            RussianStaticLabelFonts.Dispose();
            fonts?.Dispose();
            Payload?.Dispose();
            Payload = null;
        }
    }
}
