using System.Collections.Generic;
using HarmonyLib;
using Models.NextRooms;
using Program.NextRooms.Roomies;
using StringConstants;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch(typeof(DENextRoomsDialogue), nameof(DENextRoomsDialogue.GetLocalizedDialogueText))]
    internal static class RussianNextRoomNotifications
    {
        private static void Prefix(DENextRoomsDialogue __instance, int index, ref IList<object> replacements)
        {
            var keys = __instance.dialogueLocKeys;
            if (!RussianHardcodedText.IsRussian || keys == null || (uint)index >= (uint)keys.Length ||
                keys[index] != "Generic_RoomieConversation_Restored" ||
                replacements?.Count != 1 || !(replacements[0] is DERoomie roomie) ||
                !OSStringConstants.NextRooms.RoomieNameLocKeys.TryGetValue(roomie, out var key))
                return;

            var catalog = Plugin.Instance?.Catalog;
            if (catalog != null && catalog.TryGet("nextrooms_dialogues", key, out var name))
                replacements = new object[] { name };
        }
    }
}
