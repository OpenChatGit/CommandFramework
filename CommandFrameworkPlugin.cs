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
        public const string PluginVersion = "0.4.7";

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
            HandleMapEmptySpaceLeftClick();
        }

        private void HandleMapEmptySpaceLeftClick()
        {
            // Deselection applies only when the tactical map is maximized
            if (!DynamicMap.mapMaximized || DynamicMap.i == null) return;

            if (!Input.GetMouseButtonDown(0)) return;

            // If context menu is open
            if (UI.ContextMenuUI.IsOpen)
            {
                Vector2 mousePosImgui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                if (UI.ContextMenuUI.MenuRect.Contains(mousePosImgui)) return;

                UI.ContextMenuUI.CloseMenu();
            }

            // If clicking on any unit icon on the map, don't deselect (user is selecting/interacting)
            if (Patches.RightClickSuppressor.TryGetUnitUnderCursor(out _)) return;

            // Empty space click on the tactical map: deselect map icons & waypoints cleanly
            try
            {
                DynamicMap.i.UnselectAll();
                DynamicMap.i.DeselectAllIcons();
                DynamicMap.i.ClearWaypoints();

                if (NuclearOption.MissionEditorScripts.UnitSelection.i != null)
                {
                    NuclearOption.MissionEditorScripts.UnitSelection.i.ClearSelection();
                }

                LogInfo("[CommandFramework] Deselected map units cleanly via empty map space left-click.");
            }
            catch (System.Exception ex)
            {
                LogError($"[CommandFramework] Error during map deselection: {ex}");
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
