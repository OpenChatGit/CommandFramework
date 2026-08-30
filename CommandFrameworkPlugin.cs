using BepInEx;
using BepInEx.Logging;
using CommandFramework.API;
using CommandFramework.Commands;
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
        public const string PluginVersion = "0.4.5";

        public static CommandFrameworkPlugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo($"{PluginName} v{PluginVersion} initializing...");

            try
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll(typeof(CommandFrameworkPlugin).Assembly);
                Log.LogInfo($"{PluginName} successfully applied Harmony patches!");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Failed to apply Harmony patches for {PluginName}: {ex}");
            }

            // Register default built-in commands
            CommandFrameworkAPI.RegisterCommand(new HoldPositionCommandAction());

            // Ensure UI controller is spawned
            var uiGo = new GameObject("CommandFramework_UI");
            uiGo.AddComponent<UI.ContextMenuUI>();
            DontDestroyOnLoad(uiGo);

            Log.LogInfo($"{PluginName} ready! API and Context Menu initialized.");
        }

        private void Update()
        {
            WaypointQueueManager.Update();
        }

        private void OnDestroy()
        {
            try
            {
                _harmony?.UnpatchSelf();
            }
            catch
            {
                // Ignored during shutdown
            }
        }

        public static void LogInfo(string message)
        {
            Log?.LogInfo(message);
        }

        public static void LogWarning(string message)
        {
            Log?.LogWarning(message);
        }

        public static void LogError(string message)
        {
            Log?.LogError(message);
        }
    }
}
