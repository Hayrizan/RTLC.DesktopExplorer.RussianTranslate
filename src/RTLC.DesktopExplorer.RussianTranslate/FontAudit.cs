using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class FontAudit
    {
        private const string RussianLetters = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя";
        private static readonly Dictionary<string, object> fonts = new Dictionary<string, object>();

        public static void Write(string path, ManualLogSource log)
        {
            try
            {
                foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                {
                    string missingAtlas = new string(RussianLetters.Where(c => !font.HasCharacter(c, false, false)).ToArray());
                    Font source = font.sourceFontFile;
                    string[] objects = Resources.FindObjectsOfTypeAll<TMP_Text>().Where(t => t.font == font)
                        .Select(t => t.gameObject.name).Distinct().OrderBy(n => n).ToArray();
                    fonts[font.name] = new
                    {
                        font = font.name, family = font.faceInfo.familyName,
                        style = font.faceInfo.styleName, atlasMode = font.atlasPopulationMode.ToString(),
                        source = source?.name,
                        missingInOwnAtlas = missingAtlas, textObjects = objects
                    };
                }
                File.WriteAllText(path, JsonConvert.SerializeObject(fonts.Values, Formatting.Indented));
            }
            catch (Exception ex)
            {
                log.LogWarning("Could not write font audit: " + ex.Message);
            }
        }
    }
}
