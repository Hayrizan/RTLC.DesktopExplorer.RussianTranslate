using System;
using System.Collections;
using HarmonyLib;
using Program.Tower.Cutscene;
using Program.Tower.Navigation;
using Progress.Actions.Tower.Cutscene;
using UnityEngine;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    // The shipped OpenOutsideDoor timeline uses Hold extrapolation. The game's
    // DEStartTowerCutsceneAction waits for a Stopped state, so it never resumes
    // puzzle completion when that timeline reaches its final frame.
    [HarmonyPatch(typeof(DEStartTowerCutsceneAction), nameof(DEStartTowerCutsceneAction.Execute))]
    internal static class RussianOutsideDoorCutsceneCompletion
    {
        private static readonly System.Reflection.FieldInfo CutsceneField =
            AccessTools.Field(typeof(DEStartTowerCutsceneAction), "towerCutscene");

        private static readonly System.Reflection.FieldInfo PlayableDirectorField =
            AccessTools.Field(typeof(TowerCutsceneController), "playableDirector");

        private static bool Prefix(DEStartTowerCutsceneAction __instance, ref IEnumerator __result)
        {
            object cutscene = CutsceneField?.GetValue(__instance);
            if (!string.Equals(cutscene?.ToString(), nameof(DETowerCutscene.OpenOutsideDoor), StringComparison.Ordinal))
                return true;

            __result = PlayOpenOutsideDoorToEnd();
            return false;
        }

        private static IEnumerator PlayOpenOutsideDoorToEnd()
        {
            TowerCutsceneController controller = TowerSceneNavigationController.Instance
                ? TowerSceneNavigationController.Instance.GetActiveTowerCutsceneController()
                : null;
            if (!controller)
            {
                Plugin.LogWarning("OpenOutsideDoor could not start because no active Tower cutscene controller exists.");
                yield break;
            }

            controller.PlayCutscene(DETowerCutscene.OpenOutsideDoor);
            object director = PlayableDirectorField?.GetValue(controller);
            var directorUnityObject = director as UnityEngine.Object;
            var timeProperty = director?.GetType().GetProperty("time");
            var durationProperty = director?.GetType().GetProperty("duration");
            if (director == null || (directorUnityObject && !directorUnityObject) ||
                timeProperty == null || durationProperty == null)
            {
                Plugin.LogWarning("OpenOutsideDoor has no readable Timeline; stopping it to release puzzle progress.");
                controller.StopCutscene();
                yield break;
            }

            double endTime = Convert.ToDouble(durationProperty.GetValue(director));
            if (endTime <= 0d || double.IsNaN(endTime))
            {
                Plugin.LogWarning("OpenOutsideDoor has no finite Timeline duration; stopping it to release puzzle progress.");
                controller.StopCutscene();
                yield break;
            }

            while (controller && controller.IsCutscenePlaying())
            {
                if (directorUnityObject && !directorUnityObject)
                    break;

                double currentTime = Convert.ToDouble(timeProperty.GetValue(director));
                if (currentTime >= endTime - 0.03d)
                {
                    controller.StopCutscene();
                    break;
                }

                yield return null;
            }

            Plugin.LogInfo("OpenOutsideDoor reached its final frame; Admin's outside puzzle can continue.");
        }
    }
}
