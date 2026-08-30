using System;
using System.Collections.Generic;
using CommandFramework.Commands;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CommandFramework.Patches
{
    /// <summary>
    /// Harmony patches that:
    /// 1. Render all map waypoints and trajectory vector lines in Tactical Green (#0FE078).
    /// 2. Convert between map terrain coordinates and canvas screen coordinates accurately so waypoints stay fixed on the map.
    /// 3. Support multi-waypoint queues (Shift + Click) chained sequentially from waypoint to waypoint.
    /// 4. Instantly rebuild and display chained waypoints on the exact frame of the click.
    /// 5. Protect against viewing or commanding enemy NPCs in Multiplayer.
    /// </summary>
    [HarmonyPatch]
    public static class TacticalWaypointPatches
    {
        public static readonly Color TacticalGreen = new Color(0.06f, 0.88f, 0.50f, 1.0f);
        public static readonly Color TacticalGreenVector = new Color(0.06f, 0.88f, 0.50f, 0.75f);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapWaypoint), nameof(MapWaypoint.PlaceMarker))]
        public static void MapWaypoint_PlaceMarker_Postfix(MapWaypoint __instance)
        {
            ApplyTacticalGreen(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapWaypoint), nameof(MapWaypoint.UpdateMarker))]
        public static void MapWaypoint_UpdateMarker_Postfix(MapWaypoint __instance)
        {
            ApplyTacticalGreen(__instance);
        }

        /// <summary>
        /// Appends [ FOLLOW NAV ], [ PATROL LOOP ], [ HOLDING ], and [ HOLD FIRE ] to unit map tooltips and selection text.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnitMapIcon), nameof(UnitMapIcon.GetInfoText))]
        public static void UnitMapIcon_GetInfoText_Postfix(UnitMapIcon __instance, ref string __result)
        {
            if (__instance == null || __instance.unit == null) return;

            var unit = __instance.unit;
            if (HoldPositionManager.IsHoldingPosition(unit))
            {
                __result += "\n<color=#FFAA20>[ HOLDING ]</color>";
            }
            else
            {
                var queue = WaypointQueueManager.GetQueue(unit);
                if (queue != null && queue.Count > 0)
                {
                    if (WaypointQueueManager.IsLoopMode(unit))
                    {
                        __result += "\n<color=#0FE078>[ PATROL LOOP ]</color>";
                    }
                    else
                    {
                        __result += "\n<color=#0FE078>[ FOLLOW NAV ]</color>";
                    }
                }
            }

            if (HoldFireManager.IsHoldFire(unit))
            {
                __result += "\n<color=#FF4455>[ HOLD FIRE ]</color>";
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DynamicMap), "Awake")]
        public static void DynamicMap_Awake_Postfix(DynamicMap __instance)
        {
            if (__instance == null) return;

            if (__instance.mapWaypoint != null)
            {
                RecolorGameObjectHierarchy(__instance.mapWaypoint, TacticalGreen);
            }
            if (__instance.mapWaypointVector != null)
            {
                RecolorGameObjectHierarchy(__instance.mapWaypointVector, TacticalGreenVector);
            }
        }

        /// <summary>
        /// Intercepts right-clicks on the tactical map to manage waypoint queues (Shift-Click).
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DynamicMap), "MapControls")]
        public static void DynamicMap_MapControls_Prefix(DynamicMap __instance)
        {
            if (__instance == null || !DynamicMap.mapMaximized) return;

            if (Input.GetMouseButtonDown(1))
            {
                if (RightClickSuppressor.TryGetUnitUnderCursor(out _)) return;
                if (__instance.selectedIcons == null || __instance.selectedIcons.Count == 0) return;

                bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                if (__instance.TryGetCursorCoordinates(out GlobalPosition cursorCoordinates))
                {
                    for (int i = 0; i < __instance.selectedIcons.Count; i++)
                    {
                        var unitIcon = __instance.selectedIcons[i] as UnitMapIcon;
                        if (unitIcon == null) continue;

                        var unit = unitIcon.unit;
                        if (unit == null || unit.disabled || !IsUnitFriendlyToPlayer(unit)) continue;

                        if (isShift)
                        {
                            WaypointQueueManager.AppendWaypoint(unit, cursorCoordinates);
                        }
                        else
                        {
                            WaypointQueueManager.SetSingleWaypoint(unit, cursorCoordinates);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Postfix on DynamicMap.MapControls to immediately clean and rebuild chained green waypoints.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DynamicMap), "MapControls")]
        public static void DynamicMap_MapControls_Postfix(DynamicMap __instance)
        {
            if (__instance == null || !DynamicMap.mapMaximized) return;

            if (Input.GetMouseButtonDown(1))
            {
                if (RightClickSuppressor.TryGetUnitUnderCursor(out _)) return;
                if (__instance.selectedIcons != null && __instance.selectedIcons.Count == 1)
                {
                    var unitIcon = __instance.selectedIcons[0] as UnitMapIcon;
                    if (unitIcon != null && unitIcon.unit != null && IsUnitFriendlyToPlayer(unitIcon.unit))
                    {
                        var queue = WaypointQueueManager.GetQueue(unitIcon.unit);
                        if (queue != null && queue.Count > 0)
                        {
                            RebuildWaypointsForQueue(__instance, unitIcon, queue);
                            return;
                        }
                    }
                }
            }

            if (__instance.waypoints != null && __instance.waypoints.Count > 0)
            {
                for (int i = 0; i < __instance.waypoints.Count; i++)
                {
                    ApplyTacticalGreen(__instance.waypoints[i]);
                }
            }
        }

        /// <summary>
        /// Restores waypoint chain when selecting a unit.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DynamicMap), nameof(DynamicMap.SelectIcon), new Type[] { typeof(Unit) })]
        public static void DynamicMap_SelectIcon_Postfix(DynamicMap __instance, Unit unit)
        {
            if (__instance == null || unit == null) return;
            RefreshMapWaypoints(__instance);
        }

        /// <summary>
        /// Keeps waypoints locked to terrain coordinates during map panning and zooming.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DynamicMap), "UpdateMap")]
        public static void DynamicMap_UpdateMap_Postfix(DynamicMap __instance)
        {
            if (__instance == null || !DynamicMap.mapMaximized) return;

            if (Input.GetMouseButton(0) || Input.GetMouseButton(1)) return;

            if (__instance.selectedIcons != null && __instance.selectedIcons.Count == 1)
            {
                var icon = __instance.selectedIcons[0] as UnitMapIcon;
                if (icon != null && icon.unit != null && IsUnitFriendlyToPlayer(icon.unit))
                {
                    var queue = WaypointQueueManager.GetQueue(icon.unit);
                    if (queue != null && queue.Count > 0 && __instance.waypoints != null && __instance.waypoints.Count == queue.Count)
                    {
                        UpdateExistingWaypoints(__instance, icon, queue);
                    }
                }
            }
        }

        public static void RefreshMapWaypoints(DynamicMap map)
        {
            if (map == null || map.selectedIcons == null || map.selectedIcons.Count != 1) return;

            var icon = map.selectedIcons[0] as UnitMapIcon;
            if (icon == null || icon.unit == null) return;

            // Never show or restore waypoints for enemy units
            if (!IsUnitFriendlyToPlayer(icon.unit))
            {
                map.ClearWaypoints();
                return;
            }

            var queue = WaypointQueueManager.GetQueue(icon.unit);
            if (queue != null && queue.Count > 0)
            {
                RebuildWaypointsForQueue(map, icon, queue);
            }
            else
            {
                if (TryGetUnitCommandedDestination(icon.unit, out GlobalPosition singleDest))
                {
                    WaypointQueueManager.SetSingleWaypoint(icon.unit, singleDest);
                    var singleQueue = WaypointQueueManager.GetQueue(icon.unit);
                    if (singleQueue != null && singleQueue.Count > 0)
                    {
                        RebuildWaypointsForQueue(map, icon, singleQueue);
                    }
                }
            }
        }

        /// <summary>
        /// Validates that a unit belongs to the player's friendly team or spectator mode.
        /// Prevents viewing or issuing waypoints to enemy multiplayer NPCs.
        /// </summary>
        public static bool IsUnitFriendlyToPlayer(Unit unit)
        {
            if (unit == null) return false;

            if (DynamicMap.i != null)
            {
                FactionMode mode = DynamicMap.GetFactionMode(unit.NetworkHQ, false);
                return mode == FactionMode.Friendly || mode == FactionMode.Spectator;
            }

            if (GameManager.GetLocalHQ(out FactionHQ localHQ) && localHQ != null)
            {
                return unit.NetworkHQ == localHQ;
            }

            return true;
        }

        public static void RebuildWaypointsForQueue(DynamicMap map, UnitMapIcon icon, List<GlobalPosition> queue)
        {
            if (map == null || icon == null || queue == null || queue.Count == 0) return;

            map.ClearWaypoints();

            if (map.mapWaypoint == null || map.mapWaypointVector == null || map.iconLayer == null) return;

            Transform iconLayerTransform = map.iconLayer.transform;
            Vector3 prevLocalPos = iconLayerTransform.InverseTransformPoint(icon.transform.position);

            for (int i = 0; i < queue.Count; i++)
            {
                GlobalPosition wpPos = queue[i];
                Vector3 screenPos = GlobalPositionToScreenPoint(map, wpPos);

                GameObject marker = UnityEngine.Object.Instantiate(map.mapWaypoint, iconLayerTransform);
                GameObject vector = UnityEngine.Object.Instantiate(map.mapWaypointVector, iconLayerTransform);

                marker.transform.position = screenPos;
                Vector3 markerLocalPos = marker.transform.localPosition;

                MapWaypoint wp = new MapWaypoint(screenPos, prevLocalPos, marker, vector);
                ApplyTacticalGreen(wp);

                map.waypoints.Add(wp);
                prevLocalPos = markerLocalPos;
            }

            // If patrol loop is enabled, draw closing vector from last waypoint to first waypoint
            if (WaypointQueueManager.IsLoopMode(icon.unit) && map.waypoints.Count > 1)
            {
                var firstWp = map.waypoints[0];
                var lastWp = map.waypoints[map.waypoints.Count - 1];

                if (firstWp.marker != null && lastWp.marker != null)
                {
                    GameObject loopVector = UnityEngine.Object.Instantiate(map.mapWaypointVector, iconLayerTransform);
                    RecolorGameObjectHierarchy(loopVector, TacticalGreenVector);

                    Vector3 startPos = lastWp.marker.transform.localPosition;
                    Vector3 endPos = firstWp.marker.transform.localPosition;
                    Vector3 dir = endPos - startPos;

                    loopVector.transform.localPosition = startPos;
                    loopVector.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
                    loopVector.transform.localScale = new Vector3(1f, dir.magnitude, 1f);
                }
            }
        }

        private static void UpdateExistingWaypoints(DynamicMap map, UnitMapIcon icon, List<GlobalPosition> queue)
        {
            if (map.waypoints == null || map.waypoints.Count != queue.Count) return;

            Transform iconLayerTransform = map.iconLayer.transform;
            Vector3 prevLocalPos = iconLayerTransform.InverseTransformPoint(icon.transform.position);

            for (int i = 0; i < queue.Count; i++)
            {
                var wp = map.waypoints[i];
                if (wp == null || wp.marker == null || wp.vector == null) continue;

                GlobalPosition wpPos = queue[i];
                Vector3 screenPos = GlobalPositionToScreenPoint(map, wpPos);

                wp.waypointPosition = screenPos;
                wp.previousWaypoint = prevLocalPos;
                wp.PlaceMarker();
                ApplyTacticalGreen(wp);

                prevLocalPos = wp.marker.transform.localPosition;
            }
        }

        /// <summary>
        /// Accurately converts a GlobalPosition (game terrain) to Screen Canvas position.
        /// </summary>
        public static Vector3 GlobalPositionToScreenPoint(DynamicMap map, GlobalPosition pos)
        {
            if (map == null || map.mapImage == null) return Vector3.zero;

            float scaleX = map.mapImage.transform.lossyScale.x;
            if (scaleX <= 0.0001f) scaleX = 1f;

            float factor = (900f * scaleX) / map.mapDimension;
            Vector3 offset = new Vector3(pos.x * factor, pos.z * factor, 0f);

            return map.mapImage.transform.position + offset;
        }

        private static bool TryGetUnitCommandedDestination(Unit unit, out GlobalPosition destination)
        {
            destination = default;
            if (unit == null || !IsUnitFriendlyToPlayer(unit)) return false;

            if (unit is GroundVehicle gv)
            {
                var isPlayerCmdField = AccessTools.Field(typeof(GroundVehicle), "commandedDestination");
                bool isPlayerCmd = isPlayerCmdField != null && (bool)isPlayerCmdField.GetValue(gv);
                if (isPlayerCmd)
                {
                    GlobalPosition dest = gv.GetDestination();
                    if (dest.x != 0 || dest.z != 0)
                    {
                        destination = dest;
                        return true;
                    }
                }
            }
            else if (unit is Ship ship)
            {
                var shipAI = ship.GetComponent<ShipAI>();
                if (shipAI != null)
                {
                    var isPlayerCmdField = AccessTools.Field(typeof(ShipAI), "commandedDestination");
                    bool isPlayerCmd = isPlayerCmdField != null && (bool)isPlayerCmdField.GetValue(shipAI);
                    if (isPlayerCmd)
                    {
                        var destField = AccessTools.Field(typeof(ShipAI), "destination");
                        if (destField != null)
                        {
                            GlobalPosition dest = (GlobalPosition)destField.GetValue(shipAI);
                            if (dest.x != 0 || dest.z != 0)
                            {
                                destination = dest;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static void ApplyTacticalGreen(MapWaypoint wp)
        {
            if (wp == null) return;

            if (wp.marker != null)
            {
                RecolorGameObjectHierarchy(wp.marker, TacticalGreen);
            }

            if (wp.vector != null)
            {
                RecolorGameObjectHierarchy(wp.vector, TacticalGreenVector);
            }
        }

        private static void RecolorGameObjectHierarchy(GameObject go, Color color)
        {
            if (go == null) return;

            var graphics = go.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].color = color;
            }

            var spriteRenderers = go.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                spriteRenderers[i].color = color;
            }

            var lineRenderers = go.GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; i < lineRenderers.Length; i++)
            {
                lineRenderers[i].startColor = color;
                lineRenderers[i].endColor = color;
                if (lineRenderers[i].material != null)
                {
                    lineRenderers[i].material.color = color;
                }
            }

            var meshRenderers = go.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (meshRenderers[i].material != null)
                {
                    meshRenderers[i].material.color = color;
                }
            }
        }
    }
}
