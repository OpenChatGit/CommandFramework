using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CommandFramework.Patches
{
    /// <summary>
    /// Harmony patches that:
    /// 1. Recolor all map waypoints and trajectory vector lines to tactical neon Green (#0FE078).
    /// 2. Automatically restore and display a unit's active waypoint when selecting or re-selecting it.
    /// 3. Dynamically track the moving unit icon so the trajectory line stays connected in real-time.
    /// </summary>
    [HarmonyPatch]
    public static class TacticalWaypointPatches
    {
        public static readonly Color TacticalGreen = new Color(0.06f, 0.88f, 0.50f, 1.0f);
        public static readonly Color TacticalGreenVector = new Color(0.06f, 0.88f, 0.50f, 0.75f);

        /// <summary>
        /// Postfix on MapWaypoint.PlaceMarker to apply tactical green coloring to the marker and vector.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapWaypoint), nameof(MapWaypoint.PlaceMarker))]
        public static void MapWaypoint_PlaceMarker_Postfix(MapWaypoint __instance)
        {
            ApplyTacticalGreen(__instance);
        }

        /// <summary>
        /// Postfix on MapWaypoint.UpdateMarker to preserve tactical green coloring when scaled or updated.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapWaypoint), nameof(MapWaypoint.UpdateMarker))]
        public static void MapWaypoint_UpdateMarker_Postfix(MapWaypoint __instance)
        {
            ApplyTacticalGreen(__instance);
        }

        /// <summary>
        /// Postfix on DynamicMap.Awake to tint the base waypoint prefabs.
        /// </summary>
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
        /// Postfix on DynamicMap.MapControls to ensure live dragging waypoint vector preview is tinted green.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DynamicMap), "MapControls")]
        public static void DynamicMap_MapControls_Postfix(DynamicMap __instance)
        {
            if (__instance == null) return;

            if (__instance.waypoints != null && __instance.waypoints.Count > 0)
            {
                for (int i = 0; i < __instance.waypoints.Count; i++)
                {
                    ApplyTacticalGreen(__instance.waypoints[i]);
                }
            }
        }

        /// <summary>
        /// Postfix on DynamicMap.SelectIcon(Unit) to restore the unit's active waypoint if reselected.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DynamicMap), nameof(DynamicMap.SelectIcon), new Type[] { typeof(Unit) })]
        public static void DynamicMap_SelectIcon_Postfix(DynamicMap __instance, Unit unit)
        {
            if (__instance == null || unit == null) return;
            TryRestoreUnitWaypoint(__instance, unit);
        }

        /// <summary>
        /// Postfix on DynamicMap.UpdateMap to dynamically update the waypoint trajectory line
        /// as the selected unit moves across the map.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DynamicMap), "UpdateMap")]
        public static void DynamicMap_UpdateMap_Postfix(DynamicMap __instance)
        {
            if (__instance == null || __instance.waypoints == null || __instance.waypoints.Count != 1) return;
            if (__instance.selectedIcons == null || __instance.selectedIcons.Count != 1) return;

            var icon = __instance.selectedIcons[0] as UnitMapIcon;
            if (icon == null || icon.iconImage == null) return;

            var wp = __instance.waypoints[0];
            if (wp == null || wp.marker == null || wp.vector == null) return;

            Vector3 currentUnitMapPos = icon.iconImage.transform.localPosition;
            wp.previousWaypoint = currentUnitMapPos;
            wp.PlaceMarker();
            ApplyTacticalGreen(wp);
        }

        /// <summary>
        /// Checks if the unit has an active commanded destination and spawns the green waypoint visual.
        /// </summary>
        public static void TryRestoreUnitWaypoint(DynamicMap map, Unit unit)
        {
            if (map == null || unit == null || map.waypoints == null) return;
            if (map.waypoints.Count > 0) return;

            if (!TryGetUnitCommandedDestination(unit, out GlobalPosition dest)) return;

            GlobalPosition currentPos = GlobalPositionExtensions.GlobalPosition(unit.transform);
            Vector3 diff = (Vector3)(currentPos - dest);
            if (diff.sqrMagnitude < 50f * 50f) return;

            if (!DynamicMap.TryGetMapIcon(unit, out UnitMapIcon unitIcon) || unitIcon == null) return;

            Vector3 unitMapPos = unitIcon.iconImage != null ? unitIcon.iconImage.transform.localPosition : unitIcon.transform.localPosition;

            Vector3 destV3 = dest.AsVector3() * map.mapDisplayFactor;
            Vector3 mapDestPos = new Vector3(destV3.x, destV3.z, 0f);

            if (map.mapWaypoint == null || map.mapWaypointVector == null || map.iconLayer == null) return;

            GameObject marker = UnityEngine.Object.Instantiate(map.mapWaypoint, map.iconLayer.transform);
            GameObject vector = UnityEngine.Object.Instantiate(map.mapWaypointVector, map.iconLayer.transform);

            MapWaypoint wp = new MapWaypoint(mapDestPos, unitMapPos, marker, vector);
            ApplyTacticalGreen(wp);

            map.waypoints.Add(wp);
            if (map.constructWaypoints != null)
            {
                map.constructWaypoints.Clear();
                map.constructWaypoints.Add(dest);
            }
        }

        private static bool TryGetUnitCommandedDestination(Unit unit, out GlobalPosition destination)
        {
            destination = default;
            if (unit == null) return false;

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
