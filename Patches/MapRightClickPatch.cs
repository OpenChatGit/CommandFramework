using System;
using CommandFramework.API;
using CommandFramework.UI;
using HarmonyLib;
using UnityEngine;

namespace CommandFramework.Patches
{
    /// <summary>
    /// Harmony patch intercepting tactical map interactions.
    /// Intercepts right-clicking on unit icons and dispatches to the active Command Framework context menu.
    /// </summary>
    [HarmonyPatch(typeof(DynamicMap), "MapControls")]
    public static class MapRightClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(DynamicMap __instance)
        {
            if (__instance == null || !DynamicMap.mapMaximized) return true;

            // Silence base game map controls when in Drawing Mode (left click draws, right click pans)
            if (TacticalMapAPI.IsDrawingMode)
            {
                return false;
            }

            // Intercept Right-Click on Tactical Map
            if (Input.GetMouseButtonDown(1))
            {
                Vector2 mousePos = Input.mousePosition;

                // Check if right-clicking directly on a unit icon
                Unit clickedUnit = GameAPI.FindUnitUnderMouse(__instance, mousePos, 36f);

                if (clickedUnit != null && !clickedUnit.disabled)
                {
                    CommandFrameworkPlugin.Log?.LogInfo($"[MapRightClickPatch] Right-clicked unit: {clickedUnit.gameObject.name} at mouse ({mousePos.x}, {mousePos.y})");

                    // Open Tactical Context Menu if enabled
                    if (CommandFrameworkSettings.EnableDefaultContextMenu && ContextMenuUI.Instance != null)
                    {
                        ContextMenuUI.Instance.OpenForUnit(clickedUnit, mousePos);
                    }
                    // Prevent base game from issuing unintended move order to previously selected unit
                    return false;
                }
            }

            return true;
        }
    }
}
