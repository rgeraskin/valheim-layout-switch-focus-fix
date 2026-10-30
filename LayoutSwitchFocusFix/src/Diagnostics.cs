using System.Diagnostics;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LayoutSwitchFocusFix
{
    /// <summary>With Debug.Log on: who took the focus away from a field being typed in (a short stack trace), for bug reports.</summary>
    [HarmonyPatch]
    internal static class Diagnostics
    {
        public static void Init()
        {
            InputSystem.onDeviceChange += (device, change) =>
            {
                if (Plugin.DebugLog.Value && device is Keyboard keyboard)
                    Plugin.Log.LogInfo($"keyboard {change}: layout '{keyboard.keyboardLayout}', frame {Time.frameCount}");
            };
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.DeactivateInputField))]
        private static void Deactivate(TMP_InputField __instance)
        {
            if (Plugin.DebugLog.Value && __instance.isFocused)
                Plugin.Log.LogInfo($"DeactivateInputField {Path(__instance)}, frame {Time.frameCount}: {Stack()}");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.OnDeselect))]
        private static void Deselect(TMP_InputField __instance)
        {
            if (Plugin.DebugLog.Value && __instance.isFocused)
                Plugin.Log.LogInfo($"OnDeselect {Path(__instance)}, frame {Time.frameCount}: {Stack()}");
        }

        public static string Path(Component c)
        {
            if (c == null)
                return "(gone)";
            Transform t = c.transform;
            string path = t.name;
            for (int i = 0; i < 3 && t.parent != null; i++)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        private static string Stack()
        {
            var sb = new StringBuilder();
            StackFrame[] frames = new StackTrace(2, false).GetFrames();
            int n = 0;
            foreach (StackFrame f in frames ?? new StackFrame[0])
            {
                var m = f.GetMethod();
                if (m == null)
                    continue;
                if (n++ > 0)
                    sb.Append(" < ");
                sb.Append(m.DeclaringType != null ? m.DeclaringType.Name + "." : "").Append(m.Name);
                if (n == 12)
                    break;
            }
            return sb.ToString();
        }
    }
}
