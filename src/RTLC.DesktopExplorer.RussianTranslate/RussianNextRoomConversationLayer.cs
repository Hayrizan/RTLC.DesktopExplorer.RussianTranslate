using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using Program.Mall;
using Program.Mall.UI;
using UnityEngine;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianNextRoomConversationLayer
    {
        private static readonly HashSet<int> scheduledLayers = new HashSet<int>();

        internal static void RefreshAfterConversationSetup(TMP_Text interactionText)
        {
            if (!RussianHardcodedText.IsRussian || !interactionText) return;

            var conversationLayer = interactionText.transform.parent?.parent?.parent;
            if (!conversationLayer) return;
            var plugin = Plugin.Instance;
            if (!plugin) return;

            int layerId = conversationLayer.GetInstanceID();
            if (scheduledLayers.Add(layerId))
                plugin.StartCoroutine(ShowConversationAfterCanvasPass(interactionText, conversationLayer, layerId));
        }

        private static IEnumerator ShowConversationAfterCanvasPass(
            TMP_Text interactionText, Transform conversationLayer, int layerId)
        {
            yield return null;
            try
            {
                if (conversationLayer && conversationLayer.parent)
                {
                    var descriptionLayer = conversationLayer.parent.Find("NextRoomDescriptionUI");
                    if (descriptionLayer && conversationLayer.GetSiblingIndex() < descriptionLayer.GetSiblingIndex())
                        conversationLayer.SetSiblingIndex(descriptionLayer.GetSiblingIndex());
                    if (interactionText)
                    {
                        interactionText.SetAllDirty();
                        interactionText.ForceMeshUpdate();
                    }
                    Canvas.ForceUpdateCanvases();
                }
            }
            finally
            {
                scheduledLayers.Remove(layerId);
            }
        }
    }

    [HarmonyPatch(typeof(NextRoomWindowController), "SetupOptionsButtonNavigation")]
    internal static class RussianNextRoomConversationLayerPatch
    {
        private static readonly System.Reflection.FieldInfo LabelText =
            AccessTools.Field(typeof(NextRoomsUILabelController), "labelText");

        private static void Postfix(GameObject ___interactionLabelRef)
        {
            var controller = ___interactionLabelRef
                ? ___interactionLabelRef.GetComponent<NextRoomsUILabelController>()
                : null;
            var label = controller ? (TMP_Text)LabelText?.GetValue(controller) : null;
            RussianNextRoomConversationLayer.RefreshAfterConversationSetup(label);
        }
    }
}
