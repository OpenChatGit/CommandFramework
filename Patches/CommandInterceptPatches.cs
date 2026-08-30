using CommandFramework.API;
using HarmonyLib;
using UnityEngine;

namespace CommandFramework.Patches
{
    /// <summary>
    /// Harmony patches that block AI and mission orders for units in Hold Position
    /// or blocked by custom Order Filters registered via CommandFrameworkAPI.
    /// </summary>
    [HarmonyPatch]
    public static class CommandInterceptPatches
    {
        /// <summary>
        /// Intercepts UnitCommand.ServerSetDestination if destination is not allowed.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UnitCommand), "ServerSetDestination")]
        public static bool UnitCommand_ServerSetDestination_Prefix(UnitCommand __instance, GlobalPosition waypoint, object player)
        {
            if (__instance == null) return true;

            var unit = __instance.GetComponent<Unit>();
            if (unit != null && !CommandFrameworkAPI.IsDestinationAllowed(unit, waypoint))
            {
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Blocked destination update for '{unit.NetworkunitName}'.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prevents objective triggers from giving new waypoints to ground vehicles if not allowed.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GroundVehicle), "Vehicle_OnObjectiveStarted")]
        public static bool GroundVehicle_Vehicle_OnObjectiveStarted_Prefix(GroundVehicle __instance)
        {
            if (__instance != null && !CommandFrameworkAPI.IsDestinationAllowed(__instance, default))
            {
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Blocked Objective waypoint for GroundVehicle '{__instance.NetworkunitName}'.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Prevents objective triggers from giving new waypoints to ships if not allowed.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Ship), "Ship_OnObjectiveStarted")]
        public static bool Ship_Ship_OnObjectiveStarted_Prefix(Ship __instance)
        {
            if (__instance != null && !CommandFrameworkAPI.IsDestinationAllowed(__instance, default))
            {
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Blocked Objective waypoint for Ship '{__instance.NetworkunitName}'.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Prevents Mobile Artillery AI from seeking new relocation destinations while holding position.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MobileArtilleryAI), "DestinationSearch")]
        public static bool MobileArtilleryAI_DestinationSearch_Prefix(MobileArtilleryAI __instance)
        {
            if (__instance != null)
            {
                var unit = __instance.GetComponent<Unit>();
                if (unit != null && !CommandFrameworkAPI.IsDestinationAllowed(unit, default))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Prevents ShipAI from setting new destinations while holding position.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipAI), "SetDestination")]
        public static bool ShipAI_SetDestination_Prefix(ShipAI __instance)
        {
            if (__instance != null)
            {
                var unit = __instance.GetComponent<Unit>();
                if (unit != null && !CommandFrameworkAPI.IsDestinationAllowed(unit, default))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
