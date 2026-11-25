using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using OS.FullScreenSequence.Main;
using Program.NextRooms.Player;
using UnityEngine;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianAdminInterludeCollisionScope
    {
        private const string AdminInterludeSequence = "FSS_AdminInterludeSequence";
        private const string AdminInterludeScene = "AdminInterlude_FSS";
        private const string MaxRoomScene = "CH3_NextRooms_MaxRoomScene";
        private static readonly List<CollisionPair> ChangedPairs = new List<CollisionPair>();
        private static float nextRefreshAt;
        private static bool hasLoggedForCurrentSequence;

        internal static void Refresh()
        {
            if (Time.unscaledTime < nextRefreshAt) return;
            nextRefreshAt = Time.unscaledTime + 0.1f;

            bool sequenceActive = Resources.FindObjectsOfTypeAll<FullScreenSequenceManager>()
                .Any(manager => manager && manager.gameObject.scene.IsValid() &&
                    manager.gameObject.scene.isLoaded && manager.GetCurrentSequence() &&
                    manager.GetCurrentSequence().name == AdminInterludeSequence);
            if (!sequenceActive)
            {
                RestoreAll();
                hasLoggedForCurrentSequence = false;
                return;
            }

            var roomColliders = Resources.FindObjectsOfTypeAll<Collider>()
                .Where(collider => collider && collider.enabled && collider.gameObject.activeInHierarchy &&
                    collider.gameObject.scene.IsValid() && collider.gameObject.scene.isLoaded &&
                    collider.gameObject.scene.name == MaxRoomScene && !collider.isTrigger)
                .ToArray();
            int addedPairs = 0;

            foreach (var mover in Resources.FindObjectsOfTypeAll<PlayerKeyMovementController>()
                         .Where(candidate => candidate && candidate.isActiveAndEnabled &&
                             candidate.gameObject.scene.IsValid() && candidate.gameObject.scene.isLoaded &&
                             candidate.gameObject.scene.name == AdminInterludeScene))
            {
                var characterController = AccessTools.Field(typeof(PlayerKeyMovementController), "characterController")
                    ?.GetValue(mover) as CharacterController;
                if (!characterController) continue;

                foreach (var collider in roomColliders)
                {
                    if (!collider || collider == characterController ||
                        collider.transform == characterController.transform ||
                        collider.transform.IsChildOf(characterController.transform) ||
                        HasPair(characterController, collider) ||
                        Physics.GetIgnoreCollision(characterController, collider))
                        continue;

                    Physics.IgnoreCollision(characterController, collider, true);
                    ChangedPairs.Add(new CollisionPair(characterController, collider));
                    addedPairs++;
                }
            }

            if (!hasLoggedForCurrentSequence && addedPairs > 0)
            {
                hasLoggedForCurrentSequence = true;
                Plugin.LogInfo($"Temporarily ignored {ChangedPairs.Count} solid colliders from the additive Max room " +
                    "for the Admin interlude route; collision pairs will be restored when the sequence ends.");
            }
        }

        internal static void RestoreAll()
        {
            foreach (var pair in ChangedPairs)
            {
                if (pair.CharacterController && pair.Collider)
                    Physics.IgnoreCollision(pair.CharacterController, pair.Collider, false);
            }
            ChangedPairs.Clear();
        }

        private static bool HasPair(CharacterController characterController, Collider collider)
        {
            return ChangedPairs.Any(pair => pair.CharacterController == characterController &&
                pair.Collider == collider);
        }

        private sealed class CollisionPair
        {
            internal CollisionPair(CharacterController characterController, Collider collider)
            {
                CharacterController = characterController;
                Collider = collider;
            }

            internal CharacterController CharacterController { get; }
            internal Collider Collider { get; }
        }
    }
}
