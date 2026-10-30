using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LayoutSwitchFocusFix
{
    /// <summary>
    /// The bug: switching the keyboard layout makes the Input System report a keyboard ConfigurationChanged. ZInput passes
    /// every such change on as OnInputLayoutChanged (meant for keyboard ↔ gamepad switches), and the build menu answers it
    /// with EventSystem.SetSelectedGameObject(null): whatever text field is being typed in, anywhere in the game, loses
    /// focus. The other listeners only refresh hints and cursors.
    /// The fix: while a text field has keyboard focus and the keyboard is the active input, the build menu does
    /// everything it did on that event except the deselect.
    /// </summary>
    [HarmonyPatch(typeof(BuildUi), nameof(BuildUi.OnLayoutChanged))]
    internal static class KeepFocus
    {
        public static int Kept { get; private set; }

        private static bool Prefix(BuildUi __instance)
        {
            if (!Plugin.Enabled.Value || ZInput.IsGamepadActive())
                return true;
            EventSystem es = EventSystem.current;
            GameObject selected = es != null ? es.currentSelectedGameObject : null;
            TMP_InputField field = selected != null ? selected.GetComponent<TMP_InputField>() : null;
            if (field == null || !field.isFocused)
                return true;

            if (__instance.m_favoritesDropdown != null)
                __instance.m_favoritesDropdown.Close();
            Kept++;
            if (Plugin.DebugLog.Value)
                Plugin.Log.LogInfo($"kept focus on {Diagnostics.Path(field)} through an input layout change, frame {Time.frameCount}");
            return false;
        }
    }
}
