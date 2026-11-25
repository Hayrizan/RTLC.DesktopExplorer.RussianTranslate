using System.Reflection;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Models;
using UnityEngine.Localization.Settings;
using Utils;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianChessboardCoordinates
    {
        private const string ChessboardDirectoryId = "chessboard";

        internal static string ToInternalCoordinate(DEFile file, string value, string localeCode)
        {
            if (localeCode != Plugin.RussianCode || !IsChessPiece(file) || !TryGetColumn(value, out char column) ||
                value.Length != 2 || value[1] < '1' || value[1] > '3')
                return value;

            return string.Concat(column, value[1]);
        }

        internal static string ToLocalizedDisplayName(DEFile file, string value, string localeCode)
        {
            if (localeCode != Plugin.RussianCode || !IsChessPiece(file) || string.IsNullOrEmpty(value))
                return value;

            int extensionStart = value.LastIndexOf('.');
            int coordinateLength = extensionStart < 0 ? value.Length : extensionStart;
            string coordinate = value.Substring(0, coordinateLength);
            if (coordinateLength != 2 || coordinate[1] < '1' || coordinate[1] > '3' || !TryGetColumn(coordinate, out char column))
                return value;

            char localizedColumn = column switch
            {
                'A' => 'А',
                'B' => 'Б',
                'C' => 'В',
                _ => coordinate[0]
            };
            string suffix = extensionStart < 0 ? string.Empty : value.Substring(extensionStart);
            return string.Concat(localizedColumn, value[1], suffix);
        }

        private static bool IsChessPiece(DEFile file)
        {
            if (file.parentDirectoryID != ChessboardDirectoryId)
                return false;

            return file.requiredFileId == "gadmin-pawn" ||
                   file.requiredFileId == "gadmin-king" ||
                   file.requiredFileId == "gadmin-knight" ||
                   file.requiredFileId == "gadmin-rook";
        }

        private static bool TryGetColumn(string value, out char column)
        {
            column = '\0';
            if (string.IsNullOrEmpty(value) || value.Length != 2)
                return false;

            column = value[0] switch
            {
                'A' or 'a' or 'А' or 'а' => 'A',
                'B' or 'b' or 'Б' or 'б' => 'B',
                'C' or 'c' or 'В' or 'в' => 'C',
                _ => '\0'
            };
            return column != '\0';
        }
    }

    [HarmonyPatch]
    internal static class RussianChessboardCoordinateInputPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(
            typeof(DEFileData),
            nameof(DEFileData.UpdateFilenameAndExtension),
            new[] { typeof(string), typeof(string) });

        private static void Prefix(DEFileData __instance, ref string newFileName)
        {
            if (__instance == null || LocalizationSettings.SelectedLocale?.Identifier.Code != Plugin.RussianCode)
                return;

            newFileName = RussianChessboardCoordinates.ToInternalCoordinate(__instance.file, newFileName, Plugin.RussianCode);
        }
    }

    [HarmonyPatch]
    internal static class RussianChessboardCoordinateNamePatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(
            typeof(DesktopExplorerUtils),
            nameof(DesktopExplorerUtils.ConstructLocalizedFileName),
            new[] { typeof(DEFile), typeof(bool) });

        private static void Postfix(DEFile file, ref UniTask<string> __result)
        {
            string localeCode = LocalizationSettings.SelectedLocale?.Identifier.Code;
            if (localeCode == Plugin.RussianCode)
                __result = Localize(__result, file, localeCode);
        }

        private static async UniTask<string> Localize(UniTask<string> operation, DEFile file, string localeCode)
        {
            string value = await operation;
            return RussianChessboardCoordinates.ToLocalizedDisplayName(file, value, localeCode);
        }
    }

}
