using HarmonyLib;
using Models.NextRooms;
using Files;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(FileIO), nameof(FileIO.LoadNextRoomsRoomieDialogueList))]
    internal static class RussianRoomieDialogueSafety
    {
        private static void Postfix(string roomieId, ref DENextRoomsDialogueList __result)
        {
            if (roomieId != "DJSwab" && roomieId != "Strgrm" && roomieId != "Yuggoth")
                return;

            __result ??= new DENextRoomsDialogueList();
            __result.dialogueList ??= new System.Collections.Generic.List<DENextRoomsDialogue>();
            foreach (var dialogue in __result.dialogueList)
            {
                if (dialogue != null && dialogue.dialogueId == "Leave")
                    return;
            }

            __result.dialogueList.Add(new DENextRoomsDialogue
            {
                dialogueId = "Leave",
                dialogueLocKeys = new[] { "Generic_Leave_Option" },
                actionBinding = "Leave"
            });
            Plugin.LogWarning($"Roomie dialogue resource '{roomieId}' omitted the required Leave entry; added a runtime fallback.");
        }
    }
}
