using System;
using System.Collections.Generic;
using CommandFramework.API;
using HarmonyLib;
using UnityEngine;

namespace CommandFramework
{
    /// <summary>
    /// Manages Rules of Engagement (RoE) - 'Hold Fire' vs 'Engage at Will'.
    /// When Hold Fire is active:
    /// - Turrets will not acquire or track targets.
    /// - Missile launchers and fire control will not plan or fire salvos.
    /// - Weapon stations are blocked from automatic firing.
    /// </summary>
    public static class HoldFireManager
    {
        private static readonly HashSet<Unit> _holdFireUnits = new HashSet<Unit>();

        public static event Action<Unit, bool> OnHoldFireChanged;

        public static bool IsHoldFire(Unit unit)
        {
            if (unit == null) return false;
            PruneDeadUnits();
            return _holdFireUnits.Contains(unit);
        }

        public static bool ToggleHoldFire(Unit unit)
        {
            if (unit == null) return false;
            bool newState = !IsHoldFire(unit);
            SetHoldFire(unit, newState);
            return newState;
        }

        public static void SetHoldFire(Unit unit, bool holdFire)
        {
            if (unit == null) return;

            if (holdFire)
            {
                _holdFireUnits.Add(unit);
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Unit '{unit.NetworkunitName}' set to HOLD FIRE.");
            }
            else
            {
                _holdFireUnits.Remove(unit);
                CommandFrameworkPlugin.LogInfo($"[CommandFramework] Unit '{unit.NetworkunitName}' set to ENGAGE AT WILL.");
            }

            OnHoldFireChanged?.Invoke(unit, holdFire);
        }

        public static void PruneDeadUnits()
        {
            if (_holdFireUnits.Count == 0) return;
            _holdFireUnits.RemoveWhere(u => u == null || u.disabled);
        }

        public static void ClearAll()
        {
            _holdFireUnits.Clear();
        }
    }

    /// <summary>
    /// Harmony patches enforcing Hold Fire on Turrets, Fire Control, and Weapons.
    /// </summary>
    [HarmonyPatch]
    public static class HoldFirePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Turret), "ChooseTarget")]
        public static bool Turret_ChooseTarget_Prefix(Turret __instance)
        {
            if (__instance == null) return true;
            var unit = __instance.GetComponentInParent<Unit>();
            if (unit != null && HoldFireManager.IsHoldFire(unit))
            {
                return false; // Prevent turret from acquiring new targets
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FireControl), "PlanSalvo")]
        public static bool FireControl_PlanSalvo_Prefix(FireControl __instance)
        {
            if (__instance == null) return true;
            var unit = __instance.GetComponentInParent<Unit>();
            if (unit != null && HoldFireManager.IsHoldFire(unit))
            {
                return false; // Prevent missile salvos
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Weapon), "Fire")]
        public static bool Weapon_Fire_Prefix(Weapon __instance)
        {
            if (__instance == null) return true;
            var unit = __instance.GetComponentInParent<Unit>();
            if (unit != null && HoldFireManager.IsHoldFire(unit))
            {
                return false; // Block weapon firing
            }
            return true;
        }
    }
}
