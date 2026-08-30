using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using CommandFramework.Patches;

namespace CommandFramework.Commands
{
    /// <summary>
    /// Manages multi-waypoint queues (Shift + Right Click) and continuous Patrol Loops for units.
    /// Handles sequential waypoint progression, pause/resume route stashing, and synchronized map rendering.
    /// </summary>
    public static class WaypointQueueManager
    {
        private static readonly Dictionary<Unit, List<GlobalPosition>> _queues = new Dictionary<Unit, List<GlobalPosition>>();
        private static readonly Dictionary<Unit, List<GlobalPosition>> _originalWaypoints = new Dictionary<Unit, List<GlobalPosition>>();
        private static readonly Dictionary<Unit, List<GlobalPosition>> _pausedQueues = new Dictionary<Unit, List<GlobalPosition>>();
        private static readonly HashSet<Unit> _loopUnits = new HashSet<Unit>();

        public static void ClearQueue(Unit unit)
        {
            if (unit == null) return;
            _queues.Remove(unit);
            _originalWaypoints.Remove(unit);
            _pausedQueues.Remove(unit);
            _loopUnits.Remove(unit);
        }

        public static List<GlobalPosition> GetQueue(Unit unit)
        {
            if (unit != null && _queues.TryGetValue(unit, out var list))
            {
                return list;
            }
            return null;
        }

        public static bool HasPausedQueue(Unit unit)
        {
            return unit != null && _pausedQueues.ContainsKey(unit) && _pausedQueues[unit].Count > 0;
        }

        public static bool IsLoopMode(Unit unit)
        {
            return unit != null && _loopUnits.Contains(unit);
        }

        public static bool ToggleLoopMode(Unit unit)
        {
            if (unit == null) return false;
            bool newState = !IsLoopMode(unit);
            SetLoopMode(unit, newState);
            return newState;
        }

        public static void SetLoopMode(Unit unit, bool isLoop)
        {
            if (unit == null) return;

            if (isLoop)
            {
                _loopUnits.Add(unit);
                if (_queues.TryGetValue(unit, out var list) && list != null && list.Count > 0)
                {
                    _originalWaypoints[unit] = new List<GlobalPosition>(list);
                }
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Unit '{unit.NetworkunitName}' enabled PATROL LOOP mode.");
            }
            else
            {
                _loopUnits.Remove(unit);
                _originalWaypoints.Remove(unit);
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Unit '{unit.NetworkunitName}' disabled PATROL LOOP mode.");
            }

            if (DynamicMap.i != null && DynamicMap.mapMaximized)
            {
                TacticalWaypointPatches.RefreshMapWaypoints(DynamicMap.i);
            }
        }

        public static void SetSingleWaypoint(Unit unit, GlobalPosition dest)
        {
            if (unit == null) return;
            _loopUnits.Remove(unit);
            _originalWaypoints.Remove(unit);
            _pausedQueues.Remove(unit);
            _queues[unit] = new List<GlobalPosition> { dest };
            IssueDestinationToUnit(unit, dest);
        }

        public static void AppendWaypoint(Unit unit, GlobalPosition dest)
        {
            if (unit == null) return;

            _pausedQueues.Remove(unit);

            if (!_queues.TryGetValue(unit, out var list) || list == null || list.Count == 0)
            {
                _queues[unit] = new List<GlobalPosition> { dest };
                if (IsLoopMode(unit))
                {
                    _originalWaypoints[unit] = new List<GlobalPosition> { dest };
                }
                IssueDestinationToUnit(unit, dest);
            }
            else
            {
                list.Add(dest);
                if (IsLoopMode(unit))
                {
                    if (!_originalWaypoints.TryGetValue(unit, out var orig))
                    {
                        orig = new List<GlobalPosition>();
                        _originalWaypoints[unit] = orig;
                    }
                    orig.Add(dest);
                }
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Appended waypoint {list.Count} for '{unit.NetworkunitName}'.");
            }
        }

        /// <summary>
        /// Pauses and stashes the active waypoint queue when the unit is stopped.
        /// </summary>
        public static void PauseQueue(Unit unit)
        {
            if (unit == null) return;

            if (_queues.TryGetValue(unit, out var list) && list != null && list.Count > 0)
            {
                _pausedQueues[unit] = new List<GlobalPosition>(list);
                _queues.Remove(unit);
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Paused & stashed {list.Count} waypoints for '{unit.NetworkunitName}'.");
            }
        }

        /// <summary>
        /// Restores and resumes the stashed waypoint queue when the unit is resumed.
        /// </summary>
        public static void ResumeQueue(Unit unit)
        {
            if (unit == null) return;

            if (_pausedQueues.TryGetValue(unit, out var list) && list != null && list.Count > 0)
            {
                _queues[unit] = new List<GlobalPosition>(list);
                _pausedQueues.Remove(unit);
                GlobalPosition target = list[0];
                IssueDestinationToUnit(unit, target);
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Resumed {list.Count} waypoints for '{unit.NetworkunitName}'.");

                if (DynamicMap.i != null && DynamicMap.mapMaximized)
                {
                    TacticalWaypointPatches.RefreshMapWaypoints(DynamicMap.i);
                }
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

                // Check distance to current active waypoint (list[0])
                GlobalPosition currentTarget = list[0];
                GlobalPosition currentUnitPos = GlobalPositionExtensions.GlobalPosition(unit.transform);
                Vector3 diff = (Vector3)(currentUnitPos - currentTarget);
                float distSq = diff.sqrMagnitude;

                float arrivalThreshold = (unit is Ship) ? (200f * 200f) : (45f * 45f);

                if (distSq <= arrivalThreshold)
                {
                    list.RemoveAt(0);

                    // If route finished
                    if (list.Count == 0)
                    {
                        // Check if patrol loop mode is active
                        if (IsLoopMode(unit) && _originalWaypoints.TryGetValue(unit, out var orig) && orig != null && orig.Count > 0)
                        {
                            list.AddRange(orig);
                            GlobalPosition nextTarget = list[0];
                            CommandFrameworkPlugin.LogInfo($"[CommandFramework] Unit '{unit.NetworkunitName}' completed loop cycle. Restarting route ({list.Count} points).");
                            IssueDestinationToUnit(unit, nextTarget);
                        }
                        else
                        {
                            CommandFrameworkPlugin.LogInfo($"[CommandFramework] Unit '{unit.NetworkunitName}' arrived at final destination.");
                            if (toRemove == null) toRemove = new List<Unit>();
                            toRemove.Add(unit);
                        }
                    }
                    else
                    {
                        GlobalPosition nextTarget = list[0];
                        CommandFrameworkPlugin.LogInfo($"[CommandFramework] Unit '{unit.NetworkunitName}' reached intermediate waypoint. Advancing to next ({list.Count} remaining).");
                        IssueDestinationToUnit(unit, nextTarget);
                    }

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

        public static void OnUnitDestroyed(Unit unit)
        {
            if (unit != null)
            {
                ClearQueue(unit);
            }
        }
    }
}
