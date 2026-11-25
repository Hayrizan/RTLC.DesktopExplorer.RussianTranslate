using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Audio;
using DG.Tweening;
using Cutscene;
using Cutscene.File;
using Files;
using HarmonyLib;
using Login;
using OS;
using OS.FullScreenSequence.MaxPostMall;
using OS.MusicManagement;
using OS.ScreenSaver;
using Program;
using Program.Tower.Interactables.ChessRoom;
using Progress;
using Program.NextRooms.Scene;
using Program.NextRooms.CameraZones;
using Program.RenderTextureSoftware;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using Windows;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch]
    internal static class ChessboardDissolveDestroyedRendererGuard
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(ObjectDissolver),
            "DissolveObject", new[] { typeof(float), typeof(float) });

        private static void Postfix(ObjectDissolver __instance, ref IEnumerator __result)
        {
            if (!__instance || !__instance.GetComponentInParent<ChessboardInteractableController>() ||
                __result == null)
                return;

            __result = new ChessboardDissolveEnumerator(__result);
        }

        private sealed class ChessboardDissolveEnumerator : IEnumerator
        {
            private readonly IEnumerator inner;
            private bool reported;

            internal ChessboardDissolveEnumerator(IEnumerator inner) => this.inner = inner;

            public object Current => inner.Current;

            public bool MoveNext()
            {
                try
                {
                    return inner.MoveNext();
                }
                catch (NullReferenceException exception)
                {
                    if (!reported)
                    {
                        reported = true;
                        Plugin.LogWarning("Skipped the remaining GAdmin chessboard dissolve animation after it referenced a destroyed renderer; the pawn transfer continues. " + exception.Message);
                    }
                    return false;
                }
            }

            public void Reset() => inner.Reset();
        }
    }

    [HarmonyPatch(typeof(WindowController), nameof(WindowController.SetSubmenuActionState))]
    internal static class MissingSubmenuActionStateGuard
    {
        private static bool Prefix(WindowController __instance, string submenuActionID)
        {
            var actions = AccessTools.Field(typeof(WindowController), "submenuActionDictionary")
                ?.GetValue(__instance) as IDictionary;
            // Some SafeMode NotePage recipes omit save-source commands from their
            // menu, while the native NoteReader still tries to disable those IDs.
            // A missing command has no UI to update, so skip the unsafe dictionary lookup.
            return actions != null && actions.Contains(submenuActionID);
        }
    }

    [HarmonyPatch(typeof(WindowController), nameof(WindowController.SetSubmenuActionToggle))]
    internal static class MissingSubmenuActionToggleGuard
    {
        private static bool Prefix(WindowController __instance, string submenuActionID)
        {
            var actions = AccessTools.Field(typeof(WindowController), "submenuActionDictionary")
                ?.GetValue(__instance) as IDictionary;
            return actions != null && actions.Contains(submenuActionID);
        }
    }

    [HarmonyPatch(typeof(MusicSoftwareController), nameof(MusicSoftwareController.DidSetInFocus))]
    internal static class MusicFocusDuringTeardown
    {
        // A room speaker can request background music while its window and audio
        // manager are being destroyed. There is no track to resume at that point.
        private static bool Prefix(WindowController ___WindowController) =>
            ___WindowController && MusicManager.Instance;
    }

    [HarmonyPatch]
    internal static class MusicManagerStaleTrackNotificationGuard
    {
        private static MethodBase TargetMethod()
        {
            var method = AccessTools.Method(typeof(MusicManager), "SetPlayingTrack");
            var stateMachine = method?.GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>()
                ?.StateMachineType;
            return stateMachine == null ? null : AccessTools.Method(stateMachine, "MoveNext");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var code = instructions.ToList();
            var method = AccessTools.Method(typeof(MusicManager), "SetPlayingTrack");
            var stateMachine = method?.GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>()
                ?.StateMachineType;
            var inputTrackField = stateMachine == null ? null : AccessTools.Field(stateMachine, "track");
            var playingTrackField = AccessTools.Field(typeof(MusicManager), "playingTrack");
            if (inputTrackField == null || playingTrackField == null)
                return code;

            int eventIndex = code.FindIndex(instruction => instruction.operand is MethodInfo target &&
                target.Name == "SendPlayTrackEvent");
            if (eventIndex < 0)
                return code;

            int trackLoadIndex = code.FindIndex(0, eventIndex, instruction =>
                instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, playingTrackField));
            int exitIndex = trackLoadIndex < 0 ? -1 : code.FindIndex(eventIndex + 1,
                instruction => instruction.opcode == OpCodes.Leave || instruction.opcode == OpCodes.Leave_S);
            if (trackLoadIndex < 1 || exitIndex < 0)
                return code;

            var defineLabel = generator.GetType().GetMethod("DefineLabel", Type.EmptyTypes);
            var continueLabel = defineLabel?.Invoke(generator, null);
            var staleTrackLabel = defineLabel?.Invoke(generator, null);
            var nullTrackLabel = defineLabel?.Invoke(generator, null);
            var labelsField = AccessTools.Field(typeof(CodeInstruction), "labels");
            if (continueLabel == null || staleTrackLabel == null || nullTrackLabel == null || labelsField == null)
                return code;

            ((IList)labelsField.GetValue(code[exitIndex])).Add(staleTrackLabel);

            var originalLoad = code[trackLoadIndex];
            var guard = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldfld, playingTrackField),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Brfalse, nullTrackLabel),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, inputTrackField),
                new CodeInstruction(OpCodes.Beq, continueLabel),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Br, staleTrackLabel),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Br, staleTrackLabel)
            };
            ((IList)labelsField.GetValue(guard[9])).Add(nullTrackLabel);
            var originalLabels = (IList)labelsField.GetValue(originalLoad);
            var guardStartLabels = (IList)labelsField.GetValue(guard[0]);
            foreach (var label in originalLabels) guardStartLabels.Add(label);
            guard[0].blocks.AddRange(originalLoad.blocks);
            originalLabels.Clear();
            originalLoad.blocks.Clear();
            originalLabels.Add(continueLabel);
            code.InsertRange(trackLoadIndex, guard);
            return code;
        }
    }

    [HarmonyPatch(typeof(DEWindowManager), nameof(DEWindowManager.RemoveFocusable))]
    internal static class DestroyedScreenObjectFocusRemovalGuard
    {
        private static bool Prefix(DEScreenObject obj, List<DEScreenObject> ___deScreenObjects)
        {
            if (obj) return true;

            // Cutscene fade callbacks can outlive the blocking-screen window.
            // Remove the stale focus entry without dereferencing its destroyed GameObject.
            ___deScreenObjects?.Remove(obj);
            if (CutsceneController.CutsceneBlockingScreen == obj)
                CutsceneController.CutsceneBlockingScreen = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(BlockingScreenController), nameof(BlockingScreenController.UpdateBlockingScreenAlpha))]
    internal static class BlockingScreenFadeLifetime
    {
        private static bool Prefix(float newAlpha, float animTime, Action onComplete,
            Image ___blockingScreenImage)
        {
            if (!___blockingScreenImage) return false;

            var color = ___blockingScreenImage.color;
            if (animTime > 0f)
            {
                ___blockingScreenImage.DOColor(
                        new Color(color.r, color.g, color.b, newAlpha), animTime)
                    .SetLink(___blockingScreenImage.gameObject, LinkBehaviour.KillOnDestroy)
                    .OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                color.a = newAlpha;
                ___blockingScreenImage.color = color;
                onComplete?.Invoke();
            }

            // Match the native fade while linking its tween to the transient screen.
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WindowAnimationLifetime
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(SelectableFile), "PlayOpenAnimation");
            yield return AccessTools.Method(typeof(WindowController), "PlayCloseWindowAnimation");
            yield return AccessTools.Method(typeof(WindowController), "PlaySpinCloseAnimation");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var create = AccessTools.Method(typeof(DOTween), nameof(DOTween.Sequence), Type.EmptyTypes);
            var link = AccessTools.Method(typeof(WindowAnimationLifetime), nameof(LinkSequence));
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.Calls(create))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, link);
                }
            }
        }

        // Folder navigation and scene changes can destroy the animated owner first.
        private static Sequence LinkSequence(Sequence sequence, Component owner) =>
            sequence.SetLink(owner.gameObject, LinkBehaviour.KillOnDestroy);
    }

    [HarmonyPatch(typeof(PizarroController), "StartDialogue")]
    internal static class PizarroReplacedBubbleAnimation
    {
        private static void Prefix(GameObject ___currentSpeechBubble)
        {
            if (___currentSpeechBubble) ___currentSpeechBubble.transform.DOKill();
        }
    }

    [HarmonyPatch(typeof(PizarroController), "OnDisable")]
    internal static class PizarroUninitializedTypewriterCleanup
    {
        private static bool Prefix(UnityEngine.Object ___typewriterByCharacter)
        {
            // The listener is attached only after a speech bubble is created. Scene
            // teardown can disable an idle Pizarro before that happens, leaving no
            // typewriter listener to detach.
            return ___typewriterByCharacter;
        }
    }

    [HarmonyPatch(typeof(DEImageTransitionAction), "CloneTexture")]
    internal static class ImageTransitionRenderTargetLifetime
    {
        private static bool Prefix(Texture2D source, ref Texture2D __result)
        {
            // Blit can bind its destination. Save the caller's target before the blit.
            var previous = RenderTexture.active;
            var temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Texture2D copy = null;
            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point
                };
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                copy.Apply();
                __result = copy;
                return false;
            }
            catch
            {
                if (copy) UnityEngine.Object.Destroy(copy);
                throw;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }
    }

    [HarmonyPatch(typeof(TitleCardsController), "OnDestroy")]
    internal static class TitleCardsEarlyDestroyCleanup
    {
        private static readonly MethodInfo BootstrapCallback = AccessTools.Method(
            typeof(TitleCardsController), "OnBootstrapComplete");

        private static bool Prefix(TitleCardsController __instance, object ___controls)
        {
            if (___controls != null) return true;

            // Awake subscribes before the asynchronous bootstrap creates PlayerControls.
            // Closing during that interval still has to remove the bootstrap callback,
            // but there is no input subscription to remove yet. Keep native cleanup
            // unchanged for every controller that completed its initialization.
            SceneBootstrapper.OnBootstrapComplete -= (Action)Delegate.CreateDelegate(
                typeof(Action), __instance, BootstrapCallback);
            return false;
        }
    }

    [HarmonyPatch(typeof(TMP_SubMeshUI), "UpdateMaterial")]
    internal static class DisabledTmpSubMeshMaterialUpdate
    {
        // Graphic.UpdateMaterial checks IsActive; this game's TMP override omits it.
        // MaskableGraphic.OnDisable clears the renderer, then calls SetMaterialDirty.
        // Reassigning a material there both undoes that cleanup and dereferences a
        // parent material which Addressables may already have released at shutdown.
        // Active render paths retain their native behavior and error reporting.
        private static bool Prefix(TMP_SubMeshUI __instance) => __instance.IsActive();
    }

    [HarmonyPatch(typeof(ScreenSaverTransitioner), "ShowDesktop")]
    internal static class MissingScreenSaverTransitionAnimator
    {
        private static bool hasLoggedMissingAnimator;

        private static bool Prefix(Animator ___animator)
        {
            if (___animator) return true;

            // Some desktop cutscene exits call the transitioner after its Animator
            // has already been destroyed. Skipping only the visual trigger lets the
            // caller continue its normal screen-saver cleanup instead of throwing.
            if (!hasLoggedMissingAnimator)
            {
                hasLoggedMissingAnimator = true;
                Plugin.LogWarning("Skipping a screen-saver transition trigger because its Animator is unavailable.");
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(SafeModeFSSScreenSaverController), "Start")]
    internal static class SafeModeFullscreenSequenceStartsOnActivation
    {
        private static bool hasLoggedSkip;

        private static bool Prefix()
        {
            // This controller starts its sequence both from Start and from
            // ActivateScreenSaver. The duplicate run completes and unloads the
            // same additive scene twice, racing its fire-and-forget Timeline bindings.
            if (!hasLoggedSkip)
            {
                hasLoggedSkip = true;
                Plugin.LogInfo("SafeMode FSS sequence will start from ActivateScreenSaver only.");
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(MimiAnimationController), nameof(MimiAnimationController.RotateTowardsQuaternionSmooth))]
    internal static class MimiRotationDestroyedObjectGuard
    {
        private static bool Prefix(MimiAnimationController __instance, UnityEngine.Quaternion rotation,
            float speed, float epsilon, ref Cysharp.Threading.Tasks.UniTask __result)
        {
            __result = RotateUntilDestroyed(__instance, rotation, speed, epsilon);
            return false;
        }

        private static async Cysharp.Threading.Tasks.UniTask RotateUntilDestroyed(
            MimiAnimationController controller, UnityEngine.Quaternion rotation, float speed, float epsilon)
        {
            while (controller && UnityEngine.Quaternion.Angle(controller.transform.rotation, rotation) > epsilon)
            {
                controller.transform.rotation = UnityEngine.Quaternion.Slerp(
                    controller.transform.rotation, rotation, UnityEngine.Time.deltaTime * speed);
                await Cysharp.Threading.Tasks.UniTask.Yield();
            }
            if (controller)
                controller.transform.rotation = rotation;
        }
    }

    [HarmonyPatch(typeof(ControlPanelController), "ClearScreenSaverOnDesktop")]
    internal static class DestroyedScreenSaverControllerCleanup
    {
        private static readonly MethodInfo SetFullScreenSequenceActive = AccessTools.PropertySetter(
            typeof(ControlPanelController), "IsFullScreenSequenceActive");
        private static bool hasLoggedMissingController;

        private static bool Prefix(ControlPanelController __instance,
            ScreenSaverController ___currentScreenSaverController,
            Camera ___mainDesktopCamera,
            ScreenSaverTransitioner ___screenSaverTransitioner)
        {
            if (__instance && ___currentScreenSaverController) return true;
            if (!__instance) return false;

            // ResetTimeElapsedSinceInteraction can reach this method after a cutscene
            // has unloaded its additive screen-saver scene. Preserve the normal desktop
            // restoration, but do not call DeactivateScreenSaver on the destroyed owner.
            if (AudioController.Instance)
                AudioController.Instance.StopScreensaver3DAmbience();
            __instance.isScreenSaverShowing = false;
            SetFullScreenSequenceActive?.Invoke(__instance, new object[] { false });
            if (___mainDesktopCamera)
                ___mainDesktopCamera.gameObject.SetActive(true);
            if (___screenSaverTransitioner)
                ___screenSaverTransitioner.ShowDesktop();

            if (!hasLoggedMissingController)
            {
                hasLoggedMissingController = true;
                Plugin.LogWarning("Restored the desktop after its screen-saver controller was destroyed.");
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(NextRoomVolumeIndicator), "StartBounceAnimation")]
    internal static class NextRoomIndicatorTweenLifetime
    {
        // Native indicators create an infinite local-position tween without tying
        // it to the target's lifetime. Scene unload otherwise leaves DOTween reading
        // a destroyed Transform until its safe mode catches and kills the tween.
        private static void Postfix(Tween ___bounceTween, UnityEngine.GameObject ___indicatorPrefab) =>
            ___bounceTween.SetLink(___indicatorPrefab, LinkBehaviour.KillOnDestroy);
    }

    [HarmonyPatch(typeof(global::TaskbarNotificationController), "ShowAndFadeOut")]
    internal static class TaskbarNotificationTweenLifetime
    {
        // The delayed second fade must not start after leaving the desktop or
        // quitting. Native code only kills the sequence when showing another notice.
        private static void Postfix(global::TaskbarNotificationController __instance, Sequence ___fadeSequence) =>
            ___fadeSequence.SetLink(__instance.gameObject, LinkBehaviour.KillOnDestroy);
    }

    [HarmonyPatch(typeof(RenderTextureBootstrapper), nameof(RenderTextureBootstrapper.ReleaseRenderTexture))]
    internal static class NextRoomActiveRenderTextureRelease
    {
        private static void Prefix(RenderTextureBootstrapper __instance)
        {
            // A render target must be unbound before the owning window releases it.
            // Leave unrelated active targets and the native release itself untouched.
            if (__instance.RenderTexture && UnityEngine.RenderTexture.active == __instance.RenderTexture)
                UnityEngine.RenderTexture.active = null;
        }
    }

    [HarmonyPatch(typeof(NextRoomColorLensBlender), "OnDestroy")]
    internal static class NextRoomColorLensRenderTextureRelease
    {
        private static void Prefix(UnityEngine.RenderTexture ___mainCameraRT,
            UnityEngine.RenderTexture ___lensCameraRT, UnityEngine.RenderTexture ___outputRT)
        {
            // LateUpdate's Graphics.Blit leaves outputRT active. Native OnDestroy
            // releases all three targets without first unbinding that active target.
            var active = UnityEngine.RenderTexture.active;
            if (active && (active == ___mainCameraRT || active == ___lensCameraRT || active == ___outputRT))
                UnityEngine.RenderTexture.active = null;
        }
    }

    [HarmonyPatch(typeof(TransformLastWindowBehaviour), "OnBehaviourPlay")]
    internal static class CutsceneWindowTransformLifetime
    {
        private static bool Prefix(Playable playable, FrameData info)
        {
            var window = CutsceneController.LastSpawnedCutsceneGameObject;
            if (!window) return false;

            var target = window.GetComponent<RectTransform>();
            var parent = target ? target.parent as RectTransform : null;
            if (!target || !parent) return false;

            Vector2 parentSize = parent.rect.size;
            Vector2 finalPosition = new Vector2(parentSize.x / 2f, -parentSize.y / 2f) -
                new Vector2(target.rect.width / 2f, -target.rect.height / 2f);
            DECoroutineRunner.Run(MoveCutsceneWindowSafely(target, finalPosition, 3.5f, 20f,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)));
            return false;
        }

        private static IEnumerator MoveCutsceneWindowSafely(RectTransform target, Vector2 finalPosition,
            float duration, float simulatedFps, AnimationCurve easeCurve)
        {
            if (!target) yield break;
            Vector2 startPosition = target.anchoredPosition;
            int steps = Mathf.Max(1, Mathf.RoundToInt(simulatedFps * duration));
            float stepTime = 1f / simulatedFps;
            for (int i = 1; i <= steps; i++)
            {
                if (!target) yield break;
                float t = easeCurve.Evaluate((float)i / steps);
                target.anchoredPosition = Vector2.LerpUnclamped(startPosition, finalPosition, t);
                yield return new WaitForSecondsRealtime(stepTime);
            }
            if (target) target.anchoredPosition = finalPosition;
        }
    }
}
