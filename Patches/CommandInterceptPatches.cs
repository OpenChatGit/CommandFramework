using System;
using CommandFramework.API;
using HarmonyLib;
using UnityEngine;

namespace CommandFramework.Patches
{
    /// <summary>
    /// Harmony patches that:
    /// 1. Enforce Hold Position / Order Filter blocks on units.
    /// 2. Protect player-commanded destinations from being overwritten or dropped
    ///    by ambient mission scripts or AI routines until the unit reaches its destination.
    /// </summary>
    [HarmonyPatch]
    public static class CommandInterceptPatches
    {
        /// <summary>
        /// Intercepts UnitCommand.ServerSetDestination:
        /// - Enforces order filters / hold position checks.
        /// - When a player issues a waypoint, automatically releases hold position and wakes up pathfinding.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UnitCommand), "ServerSetDestination")]
        public static bool UnitCommand_ServerSetDestination_Prefix(UnitCommand __instance, GlobalPosition waypoint, object player)
        {
            if (__instance == null) return true;

            var unit = __instance.GetComponent<Unit>();
            if (unit == null) return true;

            // Check if blocked by custom order filters
            if (!CommandFrameworkAPI.IsDestinationAllowed(unit, waypoint))
            {
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Blocked destination update for '{unit.NetworkunitName}'.");
                return false;
            }

            // If a player commands a move, release hold position
            if (player != null && HoldPositionManager.IsHoldingPosition(unit))
            {
                HoldPositionManager.SetHoldPosition(unit, false);
            }

            // Ensure ground vehicle is unanchored and moving
            if (unit is GroundVehicle gv)
            {
                AccessTools.Field(typeof(GroundVehicle), "anchored")?.SetValue(gv, false);
                AccessTools.Field(typeof(GroundVehicle), "resetStationary")?.SetValue(gv, true);
                AccessTools.Field(typeof(GroundVehicle), "commandedDestination")?.SetValue(gv, true);
            }
            else if (unit is Ship ship)
            {
                var shipAI = ship.GetComponent<ShipAI>();
                if (shipAI != null)
                {
                    AccessTools.Field(typeof(ShipAI), "commandedDestination")?.SetValue(shipAI, true);
                }
            }

            return true;
        }

        /// <summary>
        /// Prevents ambient mission objective triggers from overriding player-assigned waypoints
        /// until the ground vehicle has actually arrived at its destination.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GroundVehicle), "Vehicle_OnObjectiveStarted")]
        public static bool GroundVehicle_Vehicle_OnObjectiveStarted_Prefix(GroundVehicle __instance)
        {
            if (__instance == null) return true;

            // Check if hold position or order filters block it
            if (!CommandFrameworkAPI.IsDestinationAllowed(__instance, default))
            {
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Blocked Objective waypoint for GroundVehicle '{__instance.NetworkunitName}' (Hold Position).");
                return false;
            }

            // Check if currently executing a player-commanded waypoint
            var isPlayerCmdField = AccessTools.Field(typeof(GroundVehicle), "commandedDestination");
            bool isPlayerCmd = isPlayerCmdField != null && (bool)isPlayerCmdField.GetValue(__instance);

            if (isPlayerCmd)
            {
                GlobalPosition dest = __instance.GetDestination();
                GlobalPosition currentPos = GlobalPositionExtensions.GlobalPosition(__instance.transform);
                Vector3 offset = (Vector3)(currentPos - dest);
                float distSq = offset.sqrMagnitude;

                // If more than 80m away, protect the player's waypoint!
                if (distSq > 80f * 80f)
                {
                    CommandFrameworkPlugin.LogInfo($"[CommandFramework] Protected player waypoint for '{__instance.NetworkunitName}' against objective trigger.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Prevents ambient mission objective triggers from overriding player-assigned naval waypoints
        /// until the ship has arrived.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Ship), "Ship_OnObjectiveStarted")]
        public static bool Ship_Ship_OnObjectiveStarted_Prefix(Ship __instance)
        {
            if (__instance == null) return true;

            if (!CommandFrameworkAPI.IsDestinationAllowed(__instance, default))
            {
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Blocked Objective waypoint for Ship '{__instance.NetworkunitName}' (Hold Position).");
                return false;
            }

            var shipAI = __instance.GetComponent<ShipAI>();
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
                        GlobalPosition currentPos = GlobalPositionExtensions.GlobalPosition(__instance.transform);
                        Vector3 offset = (Vector3)(currentPos - dest);
                        float distSq = offset.sqrMagnitude;

                        // If more than 300m away, protect the player's naval route!
                        if (distSq > 300f * 300f)
                        {
                            CommandFrameworkPlugin.LogInfo($"[CommandFramework] Protected player naval waypoint for '{__instance.NetworkunitName}' against objective trigger.");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Prevents Mobile Artillery AI from seeking automatic relocation destinations
        /// while holding position or executing a player waypoint.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MobileArtilleryAI), "DestinationSearch")]
        public static bool MobileArtilleryAI_DestinationSearch_Prefix(MobileArtilleryAI __instance)
        {
            if (__instance != null)
            {
                var unit = __instance.GetComponent<Unit>();
                if (unit != null)
                {
                    if (!CommandFrameworkAPI.IsDestinationAllowed(unit, default))
                    {
                        return false;
                    }

                    var gv = unit as GroundVehicle;
                    if (gv != null)
                    {
                        var isPlayerCmdField = AccessTools.Field(typeof(GroundVehicle), "commandedDestination");
                        bool isPlayerCmd = isPlayerCmdField != null && (bool)isPlayerCmdField.GetValue(gv);
                        if (isPlayerCmd)
                        {
                            // Do not search for new artillery positions while moving to player waypoint
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Prevents ShipAI from setting new random destinations while holding position or on player route.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipAI), "SetDestination")]
        public static bool ShipAI_SetDestination_Prefix(ShipAI __instance, GlobalPosition destination)
        {
            if (__instance != null)
            {
                var unit = __instance.GetComponent<Unit>();
                if (unit != null && !CommandFrameworkAPI.IsDestinationAllowed(unit, destination))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
