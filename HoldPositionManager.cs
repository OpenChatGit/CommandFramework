using System;
using System.Collections.Generic;
using CommandFramework.API;
using HarmonyLib;
using UnityEngine;

namespace CommandFramework
{
    /// <summary>
    /// Manages the 'Hold Position' (isHoldPosition) state of units.
    /// When a unit is in Hold Position, movement/orders are stopped and blocked,
    /// while combat, aiming, and firing systems remain fully operational.
    /// When resumed, an automatic AI Nudge wakes up the pathfinder/AI to resume action.
    /// </summary>
    public static class HoldPositionManager
    {
        private static readonly HashSet<Unit> _heldUnits = new HashSet<Unit>();

        public static event Action<Unit, bool> OnHoldPositionChanged;

        /// <summary>
        /// Checks whether the unit is currently ordered to hold position.
        /// </summary>
        public static bool IsHoldingPosition(Unit unit)
        {
            if (unit == null) return false;
            PruneDeadUnits();
            return _heldUnits.Contains(unit);
        }

        /// <summary>
        /// Toggles the hold position state of the given unit.
        /// </summary>
        public static bool ToggleHoldPosition(Unit unit)
        {
            if (unit == null) return false;
            bool newState = !IsHoldingPosition(unit);
            SetHoldPosition(unit, newState);
            return newState;
        }

        /// <summary>
        /// Sets the hold position state for the given unit.
        /// </summary>
        public static void SetHoldPosition(Unit unit, bool hold)
        {
            if (unit == null) return;

            var state = UnitStateManager.GetOrCreateState(unit);
            state.IsHoldPosition = hold;

            if (hold)
            {
                _heldUnits.Add(unit);
                if (string.IsNullOrEmpty(state.CustomStateKey))
                {
                    state.CustomStateKey = "HoldPosition";
                    state.CustomStateLabel = "HOLD POSITION";
                    state.CustomStateColor = new Color(1.0f, 0.65f, 0.15f);
                }
                ApplyHold(unit);
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Unit '{unit.NetworkunitName}' set to HOLD POSITION (Stop).");
            }
            else
            {
                _heldUnits.Remove(unit);
                if (state.CustomStateKey == "HoldPosition")
                {
                    state.CustomStateKey = null;
                    state.CustomStateLabel = null;
                    state.CustomStateColor = null;
                }
                ApplyResume(unit);
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Unit '{unit.NetworkunitName}' RESUMED and NUDGED back to active AI operations.");
            }

            OnHoldPositionChanged?.Invoke(unit, hold);
        }

        private static void ApplyHold(Unit unit)
        {
            if (unit == null) return;

            // Ground vehicle hold
            if (unit is GroundVehicle gv)
            {
                gv.SetHoldPosition(true);
                gv.StopImmediately();
            }
            // Ship hold
            else if (unit is Ship ship)
            {
                ship.SetHoldPosition(true);
                var shipAI = ship.GetComponent<ShipAI>();
                if (shipAI != null)
                {
                    AccessTools.Method(typeof(ShipAI), "StartHoldPosition")?.Invoke(shipAI, null);
                }
            }
        }

        private static void ApplyResume(Unit unit)
        {
            if (unit == null) return;
            NudgeAI(unit);
        }

        /// <summary>
        /// Automatically nudges/re-activates the unit's AI, recalculates pathfinding,
        /// and resumes active missions or road network navigation.
        /// </summary>
        public static void NudgeAI(Unit unit)
        {
            if (unit == null || unit.disabled) return;

            try
            {
                // 1. Ground Vehicles
                if (unit is GroundVehicle gv)
                {
                    gv.SetHoldPosition(false);
                    AccessTools.Field(typeof(GroundVehicle), "anchored")?.SetValue(gv, false);
                    AccessTools.Field(typeof(GroundVehicle), "resetStationary")?.SetValue(gv, true);
                    AccessTools.Field(typeof(GroundVehicle), "commandedDestination")?.SetValue(gv, false);

                    // Check if MobileArtilleryAI
                    var arty = gv.GetComponent<MobileArtilleryAI>();
                    if (arty != null)
                    {
                        var method = AccessTools.Method(typeof(MobileArtilleryAI), "TargetSearch");
                        method?.Invoke(arty, null);
                        CommandFrameworkPlugin.LogInfo($"[CommandFramework] Nudged MobileArtilleryAI for '{gv.NetworkunitName}'.");
                        return;
                    }

                    // Check if RearmVehicleAI
                    var rearm = gv.GetComponent<RearmVehicleAI>();
                    if (rearm != null)
                    {
                        var method = AccessTools.Method(typeof(RearmVehicleAI), "LeaveDepot");
                        method?.Invoke(rearm, null);
                        CommandFrameworkPlugin.LogInfo($"[CommandFramework] Nudged RearmVehicleAI for '{gv.NetworkunitName}'.");
                        return;
                    }

                    // Check if saved mission waypoints exist
                    var savedWpField = AccessTools.Field(typeof(GroundVehicle), "savedWaypoints");
                    var waypoints = savedWpField?.GetValue(gv) as System.Collections.IList;
                    if (waypoints != null && waypoints.Count > 0)
                    {
                        var firstWp = waypoints[0];
                        var posField = AccessTools.Field(firstWp.GetType(), "position");
                        if (posField != null)
                        {
                            GlobalPosition targetPos = (GlobalPosition)posField.GetValue(firstWp);
                            gv.GetComponent<UnitCommand>()?.SetDestination(targetPos, false);
                            CommandFrameworkPlugin.LogInfo($"[CommandFramework] Nudged '{gv.NetworkunitName}' towards saved waypoint.");
                            return;
                        }
                    }

                    // Issue forward movement order and return to road network
                    GlobalPosition forwardTarget = GlobalPositionExtensions.GlobalPosition(gv.transform) + gv.transform.forward * 800f;
                    gv.GetComponent<UnitCommand>()?.SetDestination(forwardTarget, false);
                    gv.ReturnToRoad(null);
                    CommandFrameworkPlugin.LogInfo($"[CommandFramework] Nudged GroundVehicle '{gv.NetworkunitName}' forward on road network.");
                }
                // 2. Ships
                else if (unit is Ship ship)
                {
                    ship.SetHoldPosition(false);
                    var shipAI = ship.GetComponent<ShipAI>();
                    if (shipAI != null)
                    {
                        var stateField = AccessTools.Field(typeof(ShipAI), "state");
                        if (stateField != null)
                        {
                            stateField.SetValue(shipAI, System.Enum.ToObject(stateField.FieldType, 0));
                        }
                        AccessTools.Field(typeof(ShipAI), "commandedDestination")?.SetValue(shipAI, false);
                        AccessTools.Method(typeof(ShipAI), "ChooseTarget")?.Invoke(shipAI, null);
                        var inputs = AccessTools.Field(typeof(ShipAI), "inputs")?.GetValue(shipAI) as ShipInputs;
                        if (inputs != null) inputs.throttle = 1f;
                        CommandFrameworkPlugin.LogInfo($"[CommandFramework] Nudged ShipAI for '{ship.NetworkunitName}' and restored state to Moving.");
                    }
                }
            }
            catch (Exception ex)
            {
                CommandFrameworkPlugin.LogError($"[CommandFramework] Error during AI Nudge for '{unit.NetworkunitName}': {ex}");
            }
        }

        /// <summary>
        /// Cleans up any destroyed or disabled units from tracking.
        /// </summary>
        public static void PruneDeadUnits()
        {
            if (_heldUnits.Count == 0) return;
            _heldUnits.RemoveWhere(u => u == null || u.disabled);
        }

        /// <summary>
        /// Clears all tracking (e.g. on match/mission change).
        /// </summary>
        public static void ClearAll()
        {
            _heldUnits.Clear();
        }
    }
}
