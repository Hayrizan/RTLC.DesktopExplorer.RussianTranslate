using System.Collections.Generic;
using Program.NextRooms.Roomies;
using StringConstants;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianRoomieNameKeys
    {
        internal static void EnsureAllRoomiesCanBeLocalized()
        {
            var keys = OSStringConstants.NextRooms.RoomieNameLocKeys;
            int added = 0;
            AddIfMissing(keys, DERoomie.DJSwab, "RoomieName_DJSwab", ref added);
            AddIfMissing(keys, DERoomie.Strgrm, "RoomieName_Strgrm", ref added);
            AddIfMissing(keys, DERoomie.Yuggoth, "RoomieName_Yuggoth", ref added);
            if (added > 0)
                Plugin.LogInfo($"Added localization keys for {added} roomie names missing from the game's lookup table.");
        }

        private static void AddIfMissing(Dictionary<DERoomie, string> keys, DERoomie roomie, string key, ref int added)
        {
            if (keys.ContainsKey(roomie))
                return;

            keys.Add(roomie, key);
            added++;
        }
    }
}
