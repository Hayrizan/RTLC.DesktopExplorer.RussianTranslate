using HarmonyLib;
using Progress;
using Progress.SaveData.Tower;
using System.Reflection;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    // The current game build's DESetBoolPersistenceValueAction switch omits
    // HasTakenPawnFromTower, although the G-Admin chess puzzle still serializes
    // that key in its native action list. Preserve the intended native action.
    [HarmonyPatch(typeof(DESetBoolPersistenceValueAction), nameof(DESetBoolPersistenceValueAction.Execute))]
    internal static class GuppyPawnPersistenceCompatibilityPatch
    {
        private static readonly FieldInfo RepositoryField =
            AccessTools.Field(typeof(DESetBoolPersistenceValueAction), "repository");
        private static readonly FieldInfo GuppyKeyField =
            AccessTools.Field(typeof(DESetBoolPersistenceValueAction), "guppyPersistenceKey");
        private static readonly FieldInfo ValueField =
            AccessTools.Field(typeof(DESetBoolPersistenceValueAction), "value");

        private static void Prefix(DESetBoolPersistenceValueAction __instance)
        {
            if (__instance == null || RepositoryField == null || GuppyKeyField == null || ValueField == null)
                return;

            string repository = RepositoryField.GetValue(__instance)?.ToString();
            string key = GuppyKeyField.GetValue(__instance)?.ToString();
            if (repository != "Guppy" || key != "HasTakenPawnFromTower")
                return;

            bool value = (bool)ValueField.GetValue(__instance);
            GuppyPersistenceRepository.Instance.SetHasTakenPawnFromTower(value);
            Plugin.LogInfo($"Applied native persistence action for HasTakenPawnFromTower={value}.");
        }
    }
}
