using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Models;
using Program.Notepage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Windows;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(NoteReaderController), nameof(NoteReaderController.PopulateWithData))]
    internal static class RussianLocalizedNotepageWrap
    {
        private static readonly Action<NoteReaderController> Reset = AccessTools.MethodDelegate<Action<NoteReaderController>>(
            AccessTools.Method(typeof(NoteReaderController), "ResetInputField"));

        private static void Prefix(DENotepage npg, TMP_InputField ___textField, out bool __state)
        {
            __state = IsRussianLocalizedNote(npg, ___textField);
            if (__state && ___textField && ___textField.textComponent)
                ___textField.textComponent.enableWordWrapping = true;
        }

        internal static bool IsRussianLocalizedNote(DENotepage note, TMP_InputField field)
        {
            if (!RussianHardcodedText.IsRussian || note == null || note.file.playerCreated)
                return false;

            string translated = null;
            if (!string.IsNullOrWhiteSpace(note.textLocKey) &&
                Plugin.Instance?.Catalog.TryGet("npg_contents", note.textLocKey, out translated) == true &&
                ContainsRussian(translated))
                return true;

            string filenameKey = note.file.fileNameLocKey;
            if (!string.IsNullOrWhiteSpace(filenameKey) &&
                Plugin.Instance?.Catalog.TryGet("file_names", filenameKey, out translated) == true &&
                ContainsRussian(translated))
                return true;

            return ContainsRussian(note.text) ||
                (field && field.textComponent && ContainsRussian(field.textComponent.text));
        }

        private static bool ContainsRussian(string value) => !string.IsNullOrEmpty(value) &&
            value.Any(character => (character >= 'А' && character <= 'я') || character == 'Ё' || character == 'ё');

        [HarmonyPatch(typeof(NoteReaderController), nameof(NoteReaderController.ToggleWordWrap))]
        private static class RussianLocalizedNotepageWrapToggle
        {
            private static bool Prefix(NoteReaderController __instance, DENotepage ___notepageData,
                TMP_InputField ___textField, WindowController ___WindowController)
            {
                if (!IsRussianLocalizedNote(___notepageData, ___textField)) return true;
                RussianChatLogReaderWrap.SetWrap(___textField, ___WindowController,
                    !___textField.textComponent.enableWordWrapping);
                Reset(__instance);
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(NoteReaderController), "Start")]
    internal static class RussianLocalizedNotepageWrapAfterStart
    {
        private static readonly System.Reflection.FieldInfo NoteField =
            AccessTools.Field(typeof(NoteReaderController), "notepageData");
        private static readonly System.Reflection.FieldInfo InputField =
            AccessTools.Field(typeof(NoteReaderController), "textField");
        private static readonly System.Reflection.FieldInfo WrapSettingField =
            AccessTools.Field(typeof(NoteReaderController), "wordWrapSetting");

        private static void Postfix(NoteReaderController __instance) => ApplyAfterStart(__instance).Forget();

        private static async UniTask ApplyAfterStart(NoteReaderController instance)
        {
            if (!instance) return;
            var window = instance.GetComponent<WindowController>();
            if (window) await window.AreMenusReady;

            DENotepage note = null;
            TMP_InputField field = null;
            for (int frame = 0; frame < 120 && instance; frame++)
            {
                note = NoteField?.GetValue(instance) as DENotepage;
                field = InputField?.GetValue(instance) as TMP_InputField;
                if (note != null && field && WrapSettingField?.GetValue(instance) != null)
                    break;
                await UniTask.Yield();
            }

            // NoteReaderController.Start applies the profile's saved setting after its async menu wait.
            await UniTask.Yield();
            if (!instance || !RussianLocalizedNotepageWrap.IsRussianLocalizedNote(note, field) || !field.textComponent)
                return;

            if (window)
                RussianNotepageMenuWidth.EnsureFits(window);

            field.textComponent.enableWordWrapping = true;
            field.ForceLabelUpdate();
            if (window) RussianChatLogReaderWrap.SetWrap(field, window, true);
        }
    }

    internal static class RussianNotepageMenuWidth
    {
        internal static float GetRequiredWidth(MenuBarController menuBar)
        {
            if (!menuBar) return 0f;

            var layout = menuBar.GetComponentInChildren<HorizontalLayoutGroup>(true);
            if (!layout) return 0f;
            var buttonWidths = new List<float>();
            foreach (Transform child in layout.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                var button = child.GetComponent<MenuButtonController>();
                if (!button) continue;
                var element = button.GetComponent<LayoutElement>();
                var rect = button.transform as RectTransform;
                buttonWidths.Add(element && !element.ignoreLayout
                    ? Mathf.Max(element.minWidth, element.preferredWidth)
                    : rect ? rect.rect.width : 0f);
            }

            return CalculateRequiredWidth(buttonWidths, layout.spacing, layout.padding.horizontal);
        }

        internal static float CalculateRequiredWidth(IReadOnlyList<float> buttonWidths,
            float spacing, float horizontalPadding)
        {
            if (buttonWidths == null || buttonWidths.Count == 0) return 0f;
            float width = horizontalPadding + 16f + spacing * (buttonWidths.Count - 1);
            for (int index = 0; index < buttonWidths.Count; index++)
                width += Mathf.Max(0f, buttonWidths[index]);
            return Mathf.Ceil(width);
        }

        internal static void EnsureFits(WindowController window)
        {
            if (!RussianHardcodedText.IsRussian || !window) return;
            var windowRect = window.GetComponent<RectTransform>();
            var menuBar = window.GetComponentInChildren<MenuBarController>(true);
            if (!windowRect || !menuBar) return;

            // Recompute each fixed-size caption before summing the native menu
            // button widths; this runs after the first async menu population.
            foreach (var button in menuBar.GetComponentsInChildren<MenuButtonController>(true))
            {
                foreach (var title in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (!RussianMenuLayout.IsTitle(button, title)) continue;
                    RussianMenuLayout.Apply(button, title);
                    title.ForceMeshUpdate();
                    break;
                }
            }

            Canvas.ForceUpdateCanvases();
            var menuRect = menuBar.transform as RectTransform;
            if (menuRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(menuRect);
            float requiredWidth = GetRequiredWidth(menuBar);
            if (requiredWidth <= windowRect.rect.width + 0.5f) return;

            windowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredWidth);
            Canvas.ForceUpdateCanvases();
            foreach (var resize in window.GetComponentsInChildren<WindowResizeHandler>(true))
                RussianMenuMinimum.Update(resize);
            RussianWindowSafeArea.KeepWindowInsideDesktop(windowRect);
        }
    }
}
