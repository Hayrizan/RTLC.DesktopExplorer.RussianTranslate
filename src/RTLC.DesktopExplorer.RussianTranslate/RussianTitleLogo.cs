using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianTitleLogo
    {
        private static readonly Dictionary<Transform, Vector3> Positions = new Dictionary<Transform, Vector3>();
        private static readonly Dictionary<Transform, GameObject> Added = new Dictionary<Transform, GameObject>();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<RectTransform, DesktopLogoSize>
            DesktopLogoSizes = new System.Runtime.CompilerServices.ConditionalWeakTable<RectTransform, DesktopLogoSize>();

        private sealed class DesktopLogoSize
        {
            public Vector2 Original;
        }

        private static Texture2D texture;
        private static Sprite sprite;

        private static string TextureName(GameObject item)
        {
            var image = item.GetComponent<Image>();
            if (image && image.sprite) return image.sprite.texture.name;
            var raw = item.GetComponent<RawImage>();
            if (raw && raw.texture) return raw.texture.name;
            var renderer = item.GetComponent<MeshRenderer>();
            if (renderer && renderer.sharedMaterial && renderer.sharedMaterial.mainTexture)
                return renderer.sharedMaterial.mainTexture.name;
            return null;
        }

        private static bool IsLetter(string name, int index) =>
            name == "logo_explorer_" + index || name == "RTLC logo_explorer_" + index + " ru-RU";

        internal static void ResizeDesktopLogo(RectTransform rect, Texture current)
        {
            if (!rect || !current) return;
            if (current.name == "logo_desktop")
            {
                if (DesktopLogoSizes.TryGetValue(rect, out var size))
                {
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.Original.x);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.Original.y);
                    DesktopLogoSizes.Remove(rect);
                }
                return;
            }
            if (current.name != "RTLC logo_desktop ru-RU") return;

            var original = DesktopLogoSizes.GetValue(rect, key => new DesktopLogoSize { Original = key.rect.size });
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, original.Original.x * current.width / 120f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, original.Original.y * current.height / 22f);
        }

        internal static void Refresh(LocalizationBundle payload)
        {
            foreach (var item in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (!item || !item.gameObject.scene.IsValid()) continue;
                string textureName = TextureName(item.gameObject);
                if (!IsLetter(textureName, 7)) continue;
                Transform previous = null;
                foreach (Transform sibling in item.parent)
                    if (IsLetter(TextureName(sibling.gameObject), 6)) { previous = sibling; break; }
                if (!previous) continue;
                if (!Positions.ContainsKey(item)) Positions[item] = item.localPosition;
                if (!Positions.ContainsKey(previous)) Positions[previous] = previous.localPosition;
                Vector3 step = Positions[item] - Positions[previous];
                step.y = 0f; // The native wave animates Y independently of horizontal spacing.
                if (!Added.TryGetValue(item, out var added) || !added)
                {
                    if (!RussianHardcodedText.IsRussian) continue;
                    if (!texture)
                    {
                        var asset = payload.FindText("web-assets/logo_explorer_8.png");
                        if (!asset) continue;
                        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (!ImageConversion.LoadImage(texture, asset.bytes)) { Object.Destroy(texture); texture = null; continue; }
                        texture.name = "RTLC ninth title letter";
                        texture.filterMode = FilterMode.Point;
                        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f,.5f), 100);
                    }
                    added = new GameObject("RTLC logo_explorer_8");
                    added.transform.SetParent(item.parent, false);
                    added.transform.localRotation = item.localRotation;
                    added.transform.localScale = item.localScale;
                    var sourceImage = item.GetComponent<Image>();
                    var sourceRaw = item.GetComponent<RawImage>();
                    if (sourceImage || sourceRaw)
                    {
                        var rect = added.AddComponent<RectTransform>();
                        var original = (RectTransform)item;
                        rect.anchorMin = original.anchorMin; rect.anchorMax = original.anchorMax;
                        rect.pivot = original.pivot; rect.sizeDelta = original.sizeDelta;
                        if (sourceImage)
                        {
                            var image = added.AddComponent<Image>(); image.sprite = sprite;
                            image.material = sourceImage.material; image.color = sourceImage.color;
                            image.raycastTarget = false;
                        }
                        else
                        {
                            var image = added.AddComponent<RawImage>(); image.texture = texture;
                            image.material = sourceRaw.material; image.color = sourceRaw.color; image.raycastTarget = false;
                        }
                    }
                    else
                    {
                        var filter = item.GetComponent<MeshFilter>();
                        var renderer = item.GetComponent<MeshRenderer>();
                        if (!filter || !renderer) { Object.Destroy(added); continue; }
                        added.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                        var material = new Material(renderer.sharedMaterial); material.mainTexture = texture;
                        added.AddComponent<MeshRenderer>().sharedMaterial = material;
                    }
                    Added[item] = added;
                    added.transform.localPosition = Positions[item] + step * .5f;
                }
                bool russian = RussianHardcodedText.IsRussian;
                added.SetActive(russian);
                // Expand the native eight-cell row symmetrically by half a cell on each side.
                foreach (Transform sibling in item.parent)
                {
                    bool native = false;
                    for (int i = 0; i < 8; i++) if (IsLetter(TextureName(sibling.gameObject), i)) { native = true; break; }
                    if (!native) continue;
                    if (!Positions.ContainsKey(sibling)) Positions[sibling] = sibling.localPosition;
                    Vector3 desired = Positions[sibling] - (russian ? step * .5f : Vector3.zero);
                    desired.y = sibling.localPosition.y;
                    sibling.localPosition = desired;
                }
                Vector3 addedPosition = Positions[item] + step * .5f;
                addedPosition.y = added.transform.localPosition.y;
                added.transform.localPosition = addedPosition;
                var addedRect = added.transform as RectTransform;
                if (addedRect)
                    foreach (var animator in Resources.FindObjectsOfTypeAll<StartBIOS.ExplorerWaveAnimator>())
                    {
                        if (!animator || animator.letters == null || !animator.letters.Contains(item as RectTransform)) continue;
                        if (russian && !animator.letters.Contains(addedRect))
                            animator.letters.Add(addedRect);
                        else if (!russian)
                            animator.letters.Remove(addedRect);
                    }
            }
        }
    }
}
