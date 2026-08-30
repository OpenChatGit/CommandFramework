using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CommandFramework.Patches
{
    /// <summary>
    /// Harmony patches that recolor all map waypoints and trajectory vector lines
    /// from the default yellow to tactical neon Green (#0FE078).
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
        /// Postfix on DynamicMap.MapControls to ensure the live dragging waypoint vector preview is tinted green.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DynamicMap), "MapControls")]
        public static void DynamicMap_MapControls_Postfix(DynamicMap __instance)
        {
            if (__instance == null) return;

            // Recolor any spawned waypoint instances in the waypoints list
            if (__instance.waypoints != null && __instance.waypoints.Count > 0)
            {
                for (int i = 0; i < __instance.waypoints.Count; i++)
                {
                    ApplyTacticalGreen(__instance.waypoints[i]);
                }
            }
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

            // 1. UI Graphics / Images
            var graphics = go.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].color = color;
            }

            // 2. SpriteRenderers
            var spriteRenderers = go.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                spriteRenderers[i].color = color;
            }

            // 3. LineRenderers
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

            // 4. MeshRenderers
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
