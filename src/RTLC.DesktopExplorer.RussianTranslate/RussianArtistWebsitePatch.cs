using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(WebsiteController), nameof(WebsiteController.Initialize))]
    internal static class RussianArtistWebsitePatch
    {
        private static readonly Dictionary<GameObject, bool> OriginalStates = new Dictionary<GameObject, bool>();

        private static readonly Dictionary<RectTransform, Vector2> OriginalPositions = new Dictionary<RectTransform, Vector2>();

        private static RectTransform FindCard(TMP_Text label, WebsiteController website)
        {
            for (var candidate = label.transform.parent; candidate && candidate != website.transform; candidate = candidate.parent)
            {
                int illustrations = 0;
                foreach (var graphic in candidate.GetComponentsInChildren<Graphic>(true))
                    if (!(graphic is TMP_Text) && graphic.rectTransform.rect.width > 100 &&
                        graphic.rectTransform.rect.height > 100) illustrations++;
                if (illustrations == 1) return candidate as RectTransform;
                if (illustrations > 1) break;
            }
            return null;
        }

        private static RectTransform Illustration(RectTransform card)
        {
            if (!card) return null;
            foreach (var graphic in card.GetComponentsInChildren<Graphic>(true))
                if (!(graphic is TMP_Text) && graphic.rectTransform.rect.width > 100 &&
                    graphic.rectTransform.rect.height > 100) return graphic.rectTransform;
            return null;
        }

        private static Vector3 TopLeft(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners[1];
        }

        private static void AlignIllustration(RectTransform card, RectTransform image, Vector3 target)
        {
            // Align visible image corners, not parent pivots: the original cards
            // use different anchors and pivots in the page's two columns.
            card.position += target - TopLeft(image);
        }

        private static void CompactCards(WebsiteController website)
        {
            RectTransform left = null, right = null, okeeffe = null, edvard = null;
            foreach (var label in website.GetComponentsInChildren<TMP_Text>(true))
            {
                string name = label.text?.Trim();
                if (name == "Арчимбольдо.img" || string.Equals(name, "Rembrandt.img", StringComparison.OrdinalIgnoreCase))
                    left = FindCard(label, website);
                else if (name == "Индиана.img" || string.Equals(name, "Warhol.img", StringComparison.OrdinalIgnoreCase))
                    right = FindCard(label, website);
                else if (name == "Сёра.img" || string.Equals(name, "Okeeffe.img", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "Okeefee.img", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "Okeefe.img", StringComparison.OrdinalIgnoreCase))
                    okeeffe = FindCard(label, website);
                else if (name == "Лихтенштейн.img" || string.Equals(name, "Edvard.img", StringComparison.OrdinalIgnoreCase))
                    edvard = FindCard(label, website);
            }
            if (!left || !right || !okeeffe || !edvard) return;
            foreach (var card in new[] { okeeffe, edvard })
                if (!OriginalPositions.ContainsKey(card)) OriginalPositions.Add(card, card.anchoredPosition);
            if (!RussianHardcodedText.IsRussian)
            {
                okeeffe.anchoredPosition = OriginalPositions[okeeffe];
                edvard.anchoredPosition = OriginalPositions[edvard];
                return;
            }
            var leftImage = Illustration(left);
            var rightImage = Illustration(right);
            var seraImage = Illustration(okeeffe);
            var edvardImage = Illustration(edvard);
            if (!leftImage || !rightImage || !seraImage || !edvardImage) return;
            // Original full-sized illustrations plus caption and inter-row space.
            Vector3 nextRow = leftImage.TransformVector(new Vector3(0, -(leftImage.rect.height + 48f), 0));
            AlignIllustration(okeeffe, seraImage, TopLeft(leftImage) + nextRow);
            AlignIllustration(edvard, edvardImage, TopLeft(rightImage) + nextRow);
        }

        private static void Postfix(WebsiteController __instance)
        {
            Discover(__instance);
            foreach (var label in __instance.GetComponentsInChildren<TMP_Text>(true))
                if (string.Equals(label.text?.Trim(), "Picasso.img", StringComparison.OrdinalIgnoreCase))
                {
                    if (!__instance.GetComponent<RussianArtistCardLayout>())
                        __instance.gameObject.AddComponent<RussianArtistCardLayout>();
                    break;
                }
        }

        internal static void RefreshLayout(WebsiteController website)
        {
            if (website) Discover(website);
        }

        private static void Discover(WebsiteController website)
        {
            foreach (var label in website.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!string.Equals(label.text?.Trim(), "Picasso.img", StringComparison.OrdinalIgnoreCase))
                    continue;
                // The illustration and caption are siblings, not children of the link button.
                // Select the smallest card containing the caption and one full-sized illustration.
                Transform card = null;
                for (var candidate = label.transform.parent; candidate && candidate != website.transform; candidate = candidate.parent)
                {
                    int illustrations = 0;
                    foreach (var graphic in candidate.GetComponentsInChildren<Graphic>(true))
                        if (!(graphic is TMP_Text) && graphic.rectTransform.rect.width > 100 &&
                            graphic.rectTransform.rect.height > 100) illustrations++;
                    if (illustrations == 1) { card = candidate; break; }
                    if (illustrations > 1) break;
                }
                if (card && !OriginalStates.ContainsKey(card.gameObject))
                    OriginalStates.Add(card.gameObject, card.gameObject.activeSelf);
            }
            CompactCards(website);
            UpdateStates();
        }

        private static void UpdateStates()
        {
            var removed = new List<GameObject>();
            foreach (var pair in OriginalStates)
            {
                if (!pair.Key) { removed.Add(pair.Key); continue; }
                pair.Key.SetActive(RussianHardcodedText.IsRussian ? false : pair.Value);
            }
            foreach (var item in removed) OriginalStates.Remove(item);
        }

        internal static void ApplyCurrentLocale()
        {
            foreach (var website in Resources.FindObjectsOfTypeAll<WebsiteController>())
                if (website && website.gameObject.scene.IsValid()) Discover(website);
            UpdateStates();
        }
    }
    // Website initialization precedes TMP localization and Unity's layout pass.
    // Resolve the final captions and positions after the canvas has rebuilt.
    internal sealed class RussianArtistCardLayout : MonoBehaviour
    {
        private WebsiteController website;
        private bool updating;

        private void Awake() => website = GetComponent<WebsiteController>();
        private void OnEnable() => Canvas.willRenderCanvases += Refresh;
        private void OnDisable() => Canvas.willRenderCanvases -= Refresh;

        private void Refresh()
        {
            if (updating || !website) return;
            updating = true;
            try { RussianArtistWebsitePatch.RefreshLayout(website); }
            finally { updating = false; }
        }
    }

}
