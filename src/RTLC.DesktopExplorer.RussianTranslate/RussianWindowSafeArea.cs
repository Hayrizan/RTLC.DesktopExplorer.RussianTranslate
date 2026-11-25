using System.Collections;
using HarmonyLib;
using UnityEngine;
using Windows;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianWindowSafeArea
    {
        private const float TaskbarClearance = 48f;
        private const float EdgeInset = 4f;

        internal static void KeepWindowInsideDesktop(RectTransform rectTransform, Vector3[] reusableCorners = null)
        {
            if (!RussianHardcodedText.IsRussian || !rectTransform ||
                !(rectTransform.parent is RectTransform desktopRect) ||
                !rectTransform.gameObject.CompareTag("SoftwareWindow"))
                return;

            Vector3[] corners = reusableCorners ?? new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            foreach (var worldCorner in corners)
            {
                Vector3 point = desktopRect.InverseTransformPoint(worldCorner);
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }

            Rect bounds = desktopRect.rect;
            float deltaX = GetCorrection(minX, maxX,
                bounds.xMin + EdgeInset, bounds.xMax - EdgeInset);
            float deltaY = GetCorrection(minY, maxY,
                bounds.yMin + TaskbarClearance + EdgeInset, bounds.yMax - EdgeInset);
            if (Mathf.Abs(deltaX) > 0.01f || Mathf.Abs(deltaY) > 0.01f)
                rectTransform.anchoredPosition += new Vector2(deltaX, deltaY);
        }

        internal static IEnumerator KeepWindowInsideAfterLayout(RectTransform rectTransform)
        {
            yield return new WaitForEndOfFrame();
            if (!rectTransform) yield break;
            Canvas.ForceUpdateCanvases();
            KeepWindowInsideDesktop(rectTransform);

            // File Explorer's layout can settle one additional frame after its window is shown.
            yield return new WaitForSecondsRealtime(0.2f);
            if (!rectTransform) yield break;
            Canvas.ForceUpdateCanvases();
            KeepWindowInsideDesktop(rectTransform);
        }

        private static float GetCorrection(float min, float max, float safeMin, float safeMax)
        {
            float size = max - min;
            float available = safeMax - safeMin;
            if (size > available)
                return (safeMin + safeMax - min - max) * 0.5f;
            if (min < safeMin) return safeMin - min;
            if (max > safeMax) return safeMax - max;
            return 0f;
        }
    }

    [HarmonyPatch(typeof(WindowSpawnerController), nameof(WindowSpawnerController.SetWindowPosition))]
    internal static class RussianWindowSafeAreaPositionPatch
    {
        private static void Postfix(RectTransform rectTransform) =>
            RussianWindowSafeArea.KeepWindowInsideDesktop(rectTransform);
    }

    [HarmonyPatch(typeof(WindowController), nameof(WindowController.FinalizeFirstTimeSpawning))]
    internal static class RussianWindowSafeAreaFinalizedPatch
    {
        private static void Postfix(WindowController __instance)
        {
            if (!RussianHardcodedText.IsRussian || !__instance || !Plugin.Instance)
                return;

            var rectTransform = __instance.GetComponent<RectTransform>();
            if (rectTransform)
                Plugin.Instance.StartCoroutine(RussianWindowSafeArea.KeepWindowInsideAfterLayout(rectTransform));
        }
    }

    [HarmonyPatch(typeof(WindowController), "Awake")]
    internal static class RussianWindowSafeAreaEnforcerPatch
    {
        private static void Postfix(WindowController __instance)
        {
            if (__instance && !__instance.GetComponent<RussianWindowSafeAreaEnforcer>())
                __instance.gameObject.AddComponent<RussianWindowSafeAreaEnforcer>();
        }
    }

    [DisallowMultipleComponent]
    internal sealed class RussianWindowSafeAreaEnforcer : MonoBehaviour
    {
        private RectTransform windowRect;
        private RectTransform lastParent;
        private Rect lastWindowBounds;
        private Rect lastParentBounds;
        private Matrix4x4 lastParentMatrix;
        private Vector2 lastAnchoredPosition;
        private readonly Vector3[] corners = new Vector3[4];
        private bool dirty = true;

        private void Awake() => windowRect = GetComponent<RectTransform>();

        private void OnEnable()
        {
            dirty = true;
            Canvas.willRenderCanvases += EnforceSafeBounds;
        }

        private void OnDisable() => Canvas.willRenderCanvases -= EnforceSafeBounds;

        private void OnRectTransformDimensionsChange() => dirty = true;

        private void LateUpdate() => EnforceSafeBounds();

        private void EnforceSafeBounds()
        {
            if (!RussianHardcodedText.IsRussian || !windowRect ||
                !(windowRect.parent is RectTransform parent) ||
                !windowRect.gameObject.CompareTag("SoftwareWindow"))
                return;

            Rect windowBounds = windowRect.rect;
            Rect parentBounds = parent.rect;
            Matrix4x4 parentMatrix = parent.localToWorldMatrix;
            Vector2 anchoredPosition = windowRect.anchoredPosition;
            if (!dirty && lastParent == parent && lastWindowBounds == windowBounds &&
                lastParentBounds == parentBounds && lastParentMatrix == parentMatrix &&
                lastAnchoredPosition == anchoredPosition)
                return;

            dirty = false;
            lastParent = parent;
            lastWindowBounds = windowBounds;
            lastParentBounds = parentBounds;
            lastParentMatrix = parentMatrix;
            RussianWindowSafeArea.KeepWindowInsideDesktop(windowRect, corners);
            lastAnchoredPosition = windowRect.anchoredPosition;
        }
    }
}
