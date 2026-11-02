using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace LayoutSwitchFocusFix
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "rgeraskin.LayoutSwitchFocusFix";
        public const string Name = "LayoutSwitchFocusFix";
        public const string Version = "0.1.1";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> DebugLog;

        private void Awake()
        {
            Log = Logger;
            Enabled = Config.Bind("General", "Enabled", true,
                "Keep keyboard focus in the text field you're typing in when you switch the keyboard layout.");
            DebugLog = Config.Bind("Debug", "Log", false,
                "Log keyboard layout changes and every focus loss of a text field with what caused it (a short stack trace), for bug reports.");

            var harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(KeepFocus));
            harmony.PatchAll(typeof(Diagnostics));
            Diagnostics.Init();
        }
    }
}
