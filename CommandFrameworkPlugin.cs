using BepInEx;
using BepInEx.Logging;
using CommandFramework.API;
using CommandFramework.UI;
using HarmonyLib;
using UnityEngine;

namespace CommandFramework
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("NuclearOption.exe")]
    public class CommandFrameworkPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.nuclearoption.commandframework";
        public const string PluginName = "Command Framework";
        public const string PluginVersion = "0.7.0";

        public static CommandFrameworkPlugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo($"{PluginName} v{PluginVersion} (Framework & SDK) initializing...");

            try
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll(typeof(CommandFrameworkPlugin).Assembly);
                Log.LogInfo($"{PluginName} Harmony patches successfully applied!");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Failed to apply Harmony patches: {ex}");
            }

            // Ensure UI Controllers are created on the persistent plugin GameObject
            gameObject.AddComponent<ContextMenuUI>();

            Log.LogInfo($"{PluginName} v{PluginVersion} (Pure API & SDK Platform) ready!");
        }

        private void OnGUI()
        {
            if (TacticalMapAPI.IsMapOpen())
            {
                TacticalMapAPI.HandleDrawingInput();
                TacticalMapAPI.RenderOverlays();
            }
        }

        private void OnDestroy()
        {
            try
            {
                _harmony?.UnpatchSelf();
            }
            catch
            {
                // Ignored
            }
        }
    }
}
