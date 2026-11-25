using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Event;
using Files;
using HarmonyLib;
using Models;
using Program.Mall;
using Program.NextRooms.Roomies;
using Program.Notepage;
using Progress;
using StringConstants;
using TMPro;
using Utils;
using Windows;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    [HarmonyPatch]
    internal static class RussianNextRoomChatLogs
    {
        private const string ChatLogs = "chatlogs";

        private static MethodBase TargetMethod()
        {
            var method = AccessTools.Method(typeof(NextRoomWindowController), "CreateConversationChatLog");
            return AccessTools.Method(method.GetCustomAttribute<AsyncStateMachineAttribute>().StateMachineType, "MoveNext");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var name = AccessTools.Method(typeof(LocalizationUtils), nameof(LocalizationUtils.GetLocalizedNextRoomsString));
            var write = AccessTools.Method(typeof(FileIO), nameof(FileIO.WriteFileOnDirectoryByIDAsync),
                new[] { typeof(string), typeof(DEFile), typeof(DEOSFileOperationType), typeof(int), typeof(DEUser), typeof(bool) });
            var resolveFolder = AccessTools.Method(typeof(RussianNextRoomChatLogs), nameof(ResolveChatFolder));
            var writeChatFile = AccessTools.Method(typeof(RussianNextRoomChatLogs), nameof(WriteChatFile));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(name)) instruction.operand = resolveFolder;
                if (instruction.Calls(write)) instruction.operand = writeChatFile;
                yield return instruction;
            }
        }

        private static async UniTask<string> ResolveChatFolder(string key, IList<object> replacements)
        {
            string name = await LocalizationUtils.GetLocalizedNextRoomsString(key, replacements);
            if (!RussianHardcodedText.IsRussian) return name;

            var roomie = OSStringConstants.NextRooms.RoomieNameLocKeys.FirstOrDefault(entry => entry.Value == key);
            if (roomie.Value == null) return name;
            string id = FolderId(roomie.Key);
            if (name == id) return id;

            // The directory already exists under the shipped ID. Repair the shortcut
            // left by earlier versions if it still points to the translated name.
            var root = await FileIO.ReadDirectoryFromIDAsync(ChatLogs);
            if (root != null && !root.GetFile(ChatLogs + "/" + id + ".folder").HasValue)
            {
                int index = root.fileList.FindIndex(file => file.type == DEFileType.folder &&
                    file.fileNameLocKey == key && (file.filename == name ||
                        string.Equals(file.filename, id, StringComparison.OrdinalIgnoreCase)));
                if (index >= 0)
                {
                    var shortcut = root.fileList[index];
                    shortcut.filename = id;
                    root.fileList[index] = shortcut;
                    await FileIO.WriteOnDirectoryAsync(root);
                }
            }
            return id;
        }

        private static UniTask WriteChatFile(string directoryID, DEFile file, DEOSFileOperationType operationType,
            int customFolderPosition, DEUser customUser, bool recentlyAdded)
        {
            if (RussianHardcodedText.IsRussian && file.data is DENotepage note)
            {
                var catalog = Plugin.Instance?.Catalog;
                var roomie = OSStringConstants.NextRooms.RoomieNameLocKeys.FirstOrDefault(entry => FolderId(entry.Key) == directoryID);
                string suffix = "_" + directoryID;
                if (catalog != null && roomie.Value != null && file.filename.EndsWith(suffix, StringComparison.Ordinal) &&
                    catalog.TryGet("nextrooms_dialogues", roomie.Value, out var name))
                    file.filename = file.filename.Substring(0, file.filename.Length - suffix.Length) + "_" + name;
                note.text = note.text.Replace("<color=#EED600>[Комнаты]:</color>", "<color=#00A600>[Комнаты]:</color>");
            }
            return FileIO.WriteFileOnDirectoryByIDAsync(directoryID, file, operationType, customFolderPosition, customUser, recentlyAdded);
        }

        private static string FolderId(DERoomie roomie) => roomie switch
        {
            DERoomie.Femur => "Femur", // The shipped directory uses uppercase, unlike the account name.
            DERoomie.Max => "the_max",
            _ => roomie.GetDisplayName()
        };

        internal static bool IsRussianTranscript(DENotepage note) =>
            RussianHardcodedText.IsRussian && note != null && !note.file.playerCreated &&
            note.file.author == "DesktopExplorer" &&
            OSStringConstants.NextRooms.RoomieNameLocKeys.Keys.Any(roomie => FolderId(roomie) == note.file.parentDirectoryID);
    }

    [HarmonyPatch(typeof(NoteReaderController), nameof(NoteReaderController.PopulateWithData))]
    internal static class RussianChatLogReaderWrap
    {
        private static void Prefix(DENotepage npg, DENotepage ___notepageData, TMP_InputField ___textField,
            out bool __state)
        {
            __state = RussianNextRoomChatLogs.IsRussianTranscript(npg) &&
                (___notepageData == null || ___notepageData.file.GetFilepath() != npg.file.GetFilepath());
            if (__state) ___textField.textComponent.enableWordWrapping = true;
        }

        private static void Postfix(TMP_InputField ___textField, WindowController ___WindowController,
            bool __state, ref UniTask __result)
        {
            if (__state) __result = WrapWhenReady(__result, ___textField, ___WindowController);
        }

        private static async UniTask WrapWhenReady(UniTask loading, TMP_InputField field, WindowController window)
        {
            await loading;
            if (!window) return;
            await window.AreMenusReady;
            // Start applies the shared Notepage setting when the menu becomes ready.
            await UniTask.Yield();
            if (field && window) SetWrap(field, window, true);
        }

        internal static void SetWrap(TMP_InputField field, WindowController window, bool enabled)
        {
            field.textComponent.enableWordWrapping = enabled;
            window.SetSubmenuActionToggle("NP_WordWrap", enabled);
            field.ForceLabelUpdate();
        }
    }

    [HarmonyPatch(typeof(NoteReaderController), nameof(NoteReaderController.ToggleWordWrap))]
    internal static class RussianChatLogWrapToggle
    {
        private static readonly Action<NoteReaderController> Reset = AccessTools.MethodDelegate<Action<NoteReaderController>>(
            AccessTools.Method(typeof(NoteReaderController), "ResetInputField"));

        private static bool Prefix(NoteReaderController __instance, DENotepage ___notepageData,
            TMP_InputField ___textField, WindowController ___WindowController)
        {
            if (!RussianNextRoomChatLogs.IsRussianTranscript(___notepageData)) return true;
            RussianChatLogReaderWrap.SetWrap(___textField, ___WindowController, !___textField.textComponent.enableWordWrapping);
            Reset(__instance);
            return false;
        }
    }
}
