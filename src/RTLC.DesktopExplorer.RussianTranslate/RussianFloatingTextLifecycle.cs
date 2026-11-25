using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using OS.FullScreenSequence.UI;
using TMPro;
using UnityEngine;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch]
    internal static class DelayedFssTypewriterLifetime
    {
        private static bool hasLoggedDestroyedText;

        private static MethodBase TargetMethod() => AccessTools.Method(
            typeof(FSSFloatingTextController), "StartTypewriterWithDelay");

        private static bool Prefix(FSSFloatingTextController __instance,
            Component ___typewriter, TMP_Text ___floatingText,
            string ___textToTypewrite, float ___typewriterStartDelay,
            ref UniTask __result)
        {
            __result = StartTypewriterSafely(__instance, ___typewriter,
                ___floatingText, ___textToTypewrite, ___typewriterStartDelay);
            return false;
        }

        private static async UniTask StartTypewriterSafely(FSSFloatingTextController owner,
            Component typewriter, TMP_Text floatingText, string text, float delay)
        {
            await UniTask.WaitForSeconds(delay);

            // SetupFloatingText is async void in the game. A screen transition can
            // destroy the floating-text prefab while its header delay is pending,
            // leaving the continuation to call ShowText on a destroyed component.
            if (!owner || !typewriter || !floatingText)
            {
                LogDestroyedTextOnce();
                return;
            }

            Color color = floatingText.color;
            floatingText.color = new Color(color.r, color.g, color.b, 1f);

            MethodInfo showText = AccessTools.Method(typewriter.GetType(), "ShowText",
                new[] { typeof(string) });
            if (showText == null)
                throw new MissingMethodException(typewriter.GetType().FullName, "ShowText");

            try
            {
                ((Action<string>)Delegate.CreateDelegate(typeof(Action<string>), typewriter, showText))(text);
            }
            catch (NullReferenceException) when (!owner || !typewriter || !floatingText)
            {
                // Keep this narrowly scoped to Unity's destroyed-object race; other
                // typewriter failures remain visible in the player log.
                LogDestroyedTextOnce();
            }
            catch (MissingReferenceException)
            {
                LogDestroyedTextOnce();
            }
        }

        private static void LogDestroyedTextOnce()
        {
            if (hasLoggedDestroyedText) return;
            hasLoggedDestroyedText = true;
            Plugin.LogWarning("Skipped a delayed fullscreen typewriter callback after its text object was destroyed.");
        }
    }
}
