using HarmonyLib;
using Progress;
using Progress.Puzzles;
using Progress.SaveData;
using TMPro;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianNextRoomsPeopleCount
    {
        internal static int CurrentPeopleCount()
        {
            var nextRooms = NextRoomsPersistenceRepository.Instance;
            var saveData = SaveDataManager.Instance;
            if (nextRooms == null || saveData == null) return 1;

            bool hasSeenTimeMaze = nextRooms.IsTimeMazeUnlocked() &&
                nextRooms.HasSeenTimeMazeDoorCutscene();
            bool corkboardCleared = saveData.IsPuzzleCleared(DEPuzzleId.M_MaxCorkBoard);
            return !hasSeenTimeMaze || corkboardCleared ? 1 : 2;
        }
    }

    [HarmonyPatch(typeof(TMP_Text), "set_text")]
    internal static class RussianNextRoomsPeopleCountTextRepair
    {
        private const string MissingRussianCount = "Людей: /10";
        private const string MissingEnglishCount = "People: /10";

        private static void Prefix(TMP_Text __instance, ref string value)
        {
            if (!__instance || __instance.name != "PeopleLabel") return;
            if (value != MissingRussianCount && value != MissingEnglishCount) return;

            int count = RussianNextRoomsPeopleCount.CurrentPeopleCount();
            if (value == MissingRussianCount) value = "Людей: " + count + "/10";
            else if (value == MissingEnglishCount) value = "People: " + count + "/10";
        }
    }
}
