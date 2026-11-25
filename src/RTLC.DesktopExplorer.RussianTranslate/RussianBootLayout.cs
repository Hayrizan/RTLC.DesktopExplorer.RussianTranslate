using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Login;
using TMPro;
using UnityEngine;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    // The stock device fields overlap by design: their text is short in English.
    // Reserve the full Russian label width and move the model column as one group.
    internal sealed class RussianBootLayout : MonoBehaviour
    {
        private sealed class Field
        {
            public TMP_Text Text;
            public Vector2 Position;
            public float Width;
        }

        private static readonly ConditionalWeakTable<TMP_Text, RussianBootLayout> owners =
            new ConditionalWeakTable<TMP_Text, RussianBootLayout>();
        private Field[] fields;
        private bool applied;
        private string signature;

        internal static bool Owns(TMP_Text text) => owners.TryGetValue(text, out _);

        internal void Initialize(MonoBehaviour controller)
        {
            fields = Enumerable.Range(1, 8).Select(index =>
            {
                var go = (GameObject)AccessTools.Field(controller.GetType(), "screen2Label" + index)?.GetValue(controller);
                var text = go ? go.GetComponent<TMP_Text>() : null;
                return text ? new Field { Text = text, Position = text.rectTransform.anchoredPosition,
                    Width = text.rectTransform.rect.width } : null;
            }).ToArray();
            if (fields.Any(field => field == null)) { fields = null; return; }
            foreach (var field in fields)
            {
                owners.Remove(field.Text);
                owners.Add(field.Text, this);
            }
        }

        private void LateUpdate()
        {
            if (fields == null) return;
            if (!RussianHardcodedText.IsRussian)
            {
                Restore();
                return;
            }
            string current = string.Join("|", fields.Select(field =>
                field.Text.text + ":" + field.Text.font?.GetInstanceID() + ":" + field.Text.fontSize));
            if (signature == current) return;
            signature = current;

            float labelWidth = 0, valueWidth = 0, left = 0;
            for (int index = 0; index < fields.Length; index++)
            {
                var field = fields[index];
                float width = field.Text.GetPreferredValues(field.Text.text).x;
                if (index % 2 == 0)
                {
                    labelWidth = Mathf.Max(labelWidth, width);
                    left = Mathf.Max(left, field.Position.x);
                }
                else valueWidth = Mathf.Max(valueWidth, width);
            }
            labelWidth = Mathf.Ceil(labelWidth) + 2;
            valueWidth = Mathf.Ceil(valueWidth) + 2;
            float gap = Mathf.Ceil(fields[0].Text.fontSize * 2f / 3f);
            for (int index = 0; index < fields.Length; index++)
            {
                var field = fields[index];
                var rect = field.Text.rectTransform;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, index % 2 == 0 ? labelWidth : valueWidth);
                rect.anchoredPosition = index % 2 == 0 ? field.Position :
                    new Vector2(left + labelWidth + gap, field.Position.y);
            }
            applied = true;
        }

        private void OnDisable() => Restore();

        private void OnDestroy()
        {
            if (fields == null) return;
            foreach (var field in fields)
                if (field.Text) owners.Remove(field.Text);
        }

        private void Restore()
        {
            signature = null;
            if (!applied || fields == null) return;
            foreach (var field in fields)
            {
                if (!field.Text) continue;
                var rect = field.Text.rectTransform;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, field.Width);
                rect.anchoredPosition = field.Position;
            }
            applied = false;
        }
    }

    [HarmonyPatch]
    internal static class RussianBootLayoutPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(BootUpController), "Start");
            yield return AccessTools.Method(typeof(TitleCardsAndBootUpController), "Start");
        }

        private static void Postfix(MonoBehaviour __instance)
        {
            if (__instance.GetComponent<RussianBootLayout>() == null)
                __instance.gameObject.AddComponent<RussianBootLayout>().Initialize(__instance);
        }
    }
}
