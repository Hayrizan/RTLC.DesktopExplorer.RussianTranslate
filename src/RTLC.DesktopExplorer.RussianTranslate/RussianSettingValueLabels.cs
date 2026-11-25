using TMPro;
using UnityEngine;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianSettingValueLabels
    {
        internal static string Translate(TMP_Text owner, string source)
        {
            if (owner == null || source != "ON" || owner.gameObject.name != "SettingValueText")
                return source;

            Transform background = owner.transform.parent;
            Transform picker = background != null ? background.parent : null;
            return picker != null && picker.name == "OptionPicker" ? "ВКЛ." : source;
        }
    }
}
