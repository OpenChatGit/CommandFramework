using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using CommandFramework.Patches;

namespace CommandFramework.Commands
{
    /// <summary>
    /// Manages multi-waypoint queues (Shift + Right Click) for units.
    /// Handles sequential waypoint progression and synchronized tactical green map rendering.
    /// </summary>
    public static class WaypointQueueManager
    {
        private static readonly Dictionary<Unit, List<GlobalPosition>> _queues = new Dictionary<Unit, List<GlobalPosition>>();

        public static void ClearQueue(Unit unit)
        {
            if (unit == null) return;
            if (_queues.ContainsKey(unit))
            {
                _queues.Remove(unit);
            }
        }

        public static List<GlobalPosition> GetQueue(Unit unit)
        {
            if (unit != null && _queues.TryGetValue(unit, out var list))
            {
                return list;
            }
            return null;
        }

        public static void SetSingleWaypoint(Unit unit, GlobalPosition dest)
        {
            if (unit == null) return;
            _queues[unit] = new List<GlobalPosition> { dest };
            IssueDestinationToUnit(unit, dest);
        }

        public static void AppendWaypoint(Unit unit, GlobalPosition dest)
        {
            if (unit == null) return;

            if (!_queues.TryGetValue(unit, out var list) || list == null || list.Count == 0)
            {
                _queues[unit] = new List<GlobalPosition> { dest };
                IssueDestinationToUnit(unit, dest);
            }
            else
            {
                list.Add(dest);
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Appended waypoint {list.Count} for '{unit.NetworkunitName}'.");
            }
        }

        private static void IssueDestinationToUnit(Unit unit, GlobalPosition dest)
        {
            var uc = unit.GetComponent<UnitCommand>();
            if (uc != null)
            {
                uc.SetDestination(dest, true);
            }
        }

        /// <summary>
        /// Called every frame to monitor units navigating through waypoint queues.
        /// </summary>
        public static void Update()
        {
            if (_queues.Count == 0) return;

            List<Unit> toRemove = null;

            foreach (var kvp in _queues)
            {
                var unit = kvp.Key;
                var list = kvp.Value;

                if (unit == null || unit.disabled || list == null || list.Count == 0)
                {
                    if (toRemove == null) toRemove = new List<Unit>();
                    toRemove.Add(unit);
                    continue;
                }

                // If only 1 waypoint remaining, unit is heading to final destination
                if (list.Count <= 1) continue;

                // Check distance to current active waypoint (list[0])
                GlobalPosition currentTarget = list[0];
                GlobalPosition currentUnitPos = GlobalPositionExtensions.GlobalPosition(unit.transform);
                Vector3 diff = (Vector3)(currentUnitPos - currentTarget);
                float distSq = diff.sqrMagnitude;

                float arrivalThreshold = (unit is Ship) ? (200f * 200f) : (45f * 45f);

                if (distSq <= arrivalThreshold)
                {
                    // Reached intermediate waypoint! Pop and advance to next
                    list.RemoveAt(0);
                    GlobalPosition nextTarget = list[0];
                    CommandFrameworkPlugin.LogInfo($"[CommandFramework] Unit '{unit.NetworkunitName}' reached intermediate waypoint. Advancing to next ({list.Count} remaining).");
                    IssueDestinationToUnit(unit, nextTarget);

                    // If currently selected on map, refresh waypoints
                    if (DynamicMap.i != null && DynamicMap.mapMaximized)
                    {
                        TacticalWaypointPatches.RefreshMapWaypoints(DynamicMap.i);
                    }
                }
            }

            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                {
                    _queues.Remove(toRemove[i]);
                }
            }
        }

        /// <summary>
        /// Cleans up destroyed units.
        /// </summary>
        public static void OnUnitDestroyed(Unit unit)
        {
            if (unit != null)
            {
                _queues.Remove(unit);
            }
        }
    }
}
