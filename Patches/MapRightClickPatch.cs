using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CommandFramework.Patches
{
    /// <summary>
    /// Global suppressor utility to prevent base game yellow waypoint / move order creation
    /// when the user right-clicks on a unit to open the Command Context Menu.
    /// Only allows waypoint creation when right-clicking on empty / free ground or map space.
    /// </summary>
    public static class RightClickSuppressor
    {
        public static int SuppressFrame = -1;

        public static void SuppressThisFrame()
        {
            SuppressFrame = Time.frameCount;
        }

        public static bool IsSuppressed => SuppressFrame == Time.frameCount;

        /// <summary>
        /// Finds any Unit currently under the mouse cursor (either on the tactical map or in 3D world).
        /// Returns false if cursor is over empty space.
        /// </summary>
        public static bool TryGetUnitUnderCursor(out Unit unit)
        {
            unit = null;

            // 1. On Maximized Tactical Map
            if (DynamicMap.mapMaximized && DynamicMap.i != null && DynamicMap.i.mapIcons != null)
            {
                Vector3 mouse = Input.mousePosition;
                float closestDistSq = 60f * 60f; // 60px radius
                Unit closestUnit = null;

                var icons = DynamicMap.i.mapIcons;
                for (int i = 0; i < icons.Count; i++)
                {
                    var icon = icons[i];
                    if (icon is UnitMapIcon unitIcon && icon.gameObject.activeInHierarchy)
                    {
                        if (unitIcon.unit != null && !unitIcon.unit.disabled)
                        {
                            Vector3 iconPos = icon.transform.position;
                            float distSq = (mouse.x - iconPos.x) * (mouse.x - iconPos.x) + (mouse.y - iconPos.y) * (mouse.y - iconPos.y);
                            if (distSq < closestDistSq)
                            {
                                closestDistSq = distSq;
                                closestUnit = unitIcon.unit;
                            }
                        }
                    }
                }

                if (closestUnit != null)
                {
                    unit = closestUnit;
                    return true;
                }
                return false;
            }

            // 2. In 3D Flight / Cockpit / Chase View (Map Minimized)
            if (!DynamicMap.mapMaximized)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 25000f))
                    {
                        var hitUnit = hit.collider.GetComponentInParent<Unit>();
                        if (hitUnit != null && !hitUnit.disabled)
                        {
                            unit = hitUnit;
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Harmony patch on MapIcon to intercept Right-Click events and open the Context Menu
    /// without issuing a move order to previously selected units.
    /// </summary>
    [HarmonyPatch(typeof(MapIcon), "UnityEngine.EventSystems.IPointerClickHandler.OnPointerClick")]
    public static class MapIcon_RightClick_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(MapIcon __instance, PointerEventData eventData)
        {
            if (__instance == null || eventData == null) return true;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (__instance is UnitMapIcon unitIcon && unitIcon.unit != null)
                {
                    RightClickSuppressor.SuppressThisFrame();
                    UI.ContextMenuUI.OpenForUnit(unitIcon.unit, Input.mousePosition);
                    return false; // Consume right-click event
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Harmony patch on DynamicMap.MapControls to prevent yellow waypoint / move order creation
    /// when right-clicking directly on a unit.
    /// Only allows yellow waypoints when clicking on empty map space with a unit selected.
    /// </summary>
    [HarmonyPatch(typeof(DynamicMap), "MapControls")]
    public static class DynamicMap_MapControls_SuppressMove_Patch
    {
        private static List<MapIcon> _stashedIcons = null;

        [HarmonyPrefix]
        public static void Prefix(DynamicMap __instance)
        {
            if (__instance == null) return;

            if (Input.GetMouseButtonDown(1))
            {
                // Check if right-clicking directly on any unit
                if (RightClickSuppressor.TryGetUnitUnderCursor(out Unit clickedUnit))
                {
                    RightClickSuppressor.SuppressThisFrame();
                    UI.ContextMenuUI.OpenForUnit(clickedUnit, Input.mousePosition);

                    // Stash selectedIcons so MapControls does NOT spawn yellow waypoints or send move orders!
                    if (__instance.selectedIcons != null && __instance.selectedIcons.Count > 0)
                    {
                        _stashedIcons = new List<MapIcon>(__instance.selectedIcons);
                        __instance.selectedIcons.Clear();
                    }
                }
            }
        }

        [HarmonyPostfix]
        public static void Postfix(DynamicMap __instance)
        {
            if (__instance == null) return;

            // Restore selected icons immediately so the player does not lose selection
            if (_stashedIcons != null)
            {
                if (__instance.selectedIcons != null)
                {
                    __instance.selectedIcons.AddRange(_stashedIcons);
                }
                _stashedIcons = null;
            }
        }
    }

    /// <summary>
    /// Harmony patch on UnitCommand.SetDestination to block player-commanded moves if right-clicking a unit.
    /// </summary>
    [HarmonyPatch(typeof(UnitCommand), nameof(UnitCommand.SetDestination))]
    public static class UnitCommand_SetDestination_Suppress_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(UnitCommand __instance, GlobalPosition waypoint, bool playerCommand)
        {
            if (playerCommand && RightClickSuppressor.IsSuppressed)
            {
                CommandFrameworkPlugin.LogInfo("[CommandFramework] Blocked waypoint/destination creation from unit context right-click.");
                return false;
            }
            return true;
        }
    }
}
