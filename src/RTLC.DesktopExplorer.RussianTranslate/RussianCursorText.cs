using System;
using System.Collections.Generic;
using Settings;
using TMPro;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianCursorText
    {
        // Display strings only. The game still receives its original cursor enum values.
        private static readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Default"] = "Стандартный",
            ["Loading"] = "Загрузка",
            ["Boat"] = "Лодка",
            ["Pen"] = "Перо",
            ["Eraser"] = "Ластик",
            ["Steps"] = "Следы",
            ["Selection"] = "Выбор",
            ["CutsceneDialog"] = "Диалог",
            ["TowerDefault"] = "Башня",
            ["TowerLook"] = "Осмотр",
            ["TowerUse"] = "Действие",
            ["TowerGo"] = "Движение",
            ["AirBrush"] = "Аэрограф",
            ["Fill"] = "Заливка",
            ["BIOS"] = "BIOS",
            ["SquareShape"] = "Квадрат",
            ["TriangleShape"] = "Треугольник",
            ["TrapezoidShape"] = "Трапеция",
            ["StarShape"] = "Звезда",
            ["Disabled"] = "Недоступен",
            ["Resize"] = "Размер",
            ["StepsIdle"] = "Следы (покой)",
            ["NextOS Default Cursor\n"] = "Стандартный курсор NextOS\n",
            ["This is a Boat"] = "Это лодка",
            ["You gotta wait a little more"] = "Придётся ещё немного подождать",
            ["Point & Click"] = "Наведи и щёлкни",
            ["A Cursor from another realm"] = "Курсор из другого мира",
            ["A cursor to customize your environment to your liking"] = "Курсор для настройки окружения на свой вкус",
            ["Still need asset"] = "Изображение ещё не готово",
            ["Need Fill Asset"] = "Изображение заливки ещё не готово"
        };

        internal static string Translate(TMP_Text owner, string value)
        {
            if (value != null && Values.TryGetValue(value, out string translated) &&
                owner.GetComponentInParent<CursorSettingsController>(true) != null)
                return translated;
            return value;
        }
    }
}
