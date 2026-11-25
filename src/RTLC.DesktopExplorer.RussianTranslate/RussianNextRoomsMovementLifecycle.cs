using System;
using Cinemachine;
using HarmonyLib;
using Program.NextRooms.Player;
using UnityEngine;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(PlayerKeyMovementController), "HandleMovement")]
    internal static class NextRoomsMovementCameraLifecycle
    {
        private static bool hasLoggedUnavailableCamera;

        private static bool Prefix(ICinemachineCamera ___activeCamera,
            PlayerKeyMovementController __instance, ICinemachineCamera ___currentMovementCamera, Vector2 ___moveInput,
            Vector2 ___currentMovement, bool ___changeCameraMovementUntilInputChanges,
            bool ___isMoving)
        {
            if (__instance && __instance.gameObject.scene.name == "AdminInterlude_FSS")
                RussianAdminInterludeCollisionScope.Refresh();

            ICinemachineCamera movementCamera = ___activeCamera;
            if (___changeCameraMovementUntilInputChanges && ___isMoving &&
                ___currentMovementCamera != null && ___moveInput == ___currentMovement)
            {
                movementCamera = ___currentMovementCamera;
            }

            if (movementCamera == null)
                return true;

            GameObject virtualCameraObject;
            try
            {
                virtualCameraObject = movementCamera.VirtualCameraGameObject;
            }
            catch (MissingReferenceException)
            {
                return SkipForUnavailableCamera();
            }
            catch (NullReferenceException)
            {
                // A camera can be torn down between its zone-change event and this
                // Update. Contain only the camera lookup; leave movement failures visible.
                return SkipForUnavailableCamera();
            }

            return virtualCameraObject || SkipForUnavailableCamera();
        }

        private static bool SkipForUnavailableCamera()
        {
            if (!hasLoggedUnavailableCamera)
            {
                hasLoggedUnavailableCamera = true;
                Plugin.LogInfo("Skipped NextRooms movement while its Cinemachine camera was being destroyed.");
            }

            return false;
        }
    }
}
