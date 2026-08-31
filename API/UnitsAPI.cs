using System;
using System.Collections.Generic;
using NuclearOption.Networking;
using UnityEngine;

namespace CommandFramework.API
{
    /// <summary>
    /// Dynamic, unconstrained Units & Combat API for Nuclear Option.
    /// Provides high-performance querying, spatial lookups, player-unit resolution,
    /// dynamic health/fuel/ammo manipulation, invulnerability hooks, and dynamic metadata attachment.
    /// </summary>
    public static class UnitsAPI
    {
        private static readonly HashSet<Unit> _invulnerableUnits = new HashSet<Unit>();
        private static readonly HashSet<Unit> _infiniteAmmoUnits = new HashSet<Unit>();
        private static readonly Dictionary<Unit, Dictionary<string, object>> _unitCustomData = new Dictionary<Unit, Dictionary<string, object>>();

        // --- 1. Dynamic Unit Retrieval & Lookups ---

        /// <summary>
        /// Dynamically retrieves the current local player unit (Aircraft, Vehicle, or Pilot).
        /// </summary>
        public static Unit GetPlayerUnit()
        {
            if (GameManager.GetLocalAircraft(out var localAircraft) && localAircraft != null)
            {
                return localAircraft;
            }

            if (GameManager.GetLocalPlayer(out Player localPlayer) && localPlayer != null)
            {
                if (localPlayer.Aircraft != null) return localPlayer.Aircraft;
            }

            return null;
        }

        /// <summary>
        /// Retrieves the local player's Aircraft, if any.
        /// </summary>
        public static Aircraft GetPlayerAircraft()
        {
            if (GameManager.GetLocalAircraft(out var localAircraft))
            {
                return localAircraft;
            }
            return null;
        }

        /// <summary>
        /// Retrieves the local player's FactionHQ.
        /// </summary>
        public static FactionHQ GetPlayerHQ()
        {
            if (GameManager.GetLocalHQ(out var localHq))
            {
                return localHq;
            }
            return null;
        }

        /// <summary>
        /// Returns all currently active units matching an optional predicate.
        /// </summary>
        public static List<Unit> GetAll(Func<Unit, bool> filter = null)
        {
            var results = new List<Unit>();
            var allUnits = UnityEngine.Object.FindObjectsOfType<Unit>();
            for (int i = 0; i < allUnits.Length; i++)
            {
                var u = allUnits[i];
                if (u != null && (filter == null || filter(u)))
                {
                    results.Add(u);
                }
            }
            return results;
        }

        /// <summary>
        /// Returns all currently active units of a specific component type (e.g. Aircraft, GroundVehicle, Ship).
        /// </summary>
        public static List<T> GetAll<T>(Func<T, bool> filter = null) where T : Component
        {
            var results = new List<T>();
            var allComponents = UnityEngine.Object.FindObjectsOfType<T>();
            for (int i = 0; i < allComponents.Length; i++)
            {
                var c = allComponents[i];
                if (c != null && (filter == null || filter(c)))
                {
                    results.Add(c);
                }
            }
            return results;
        }

        /// <summary>
        /// Finds all units within a specified radius (meters) of a world position.
        /// </summary>
        public static List<Unit> GetUnitsInRange(Vector3 center, float radiusMeters, Func<Unit, bool> filter = null)
        {
            var results = new List<Unit>();
            float sqrRadius = radiusMeters * radiusMeters;
            var allUnits = UnityEngine.Object.FindObjectsOfType<Unit>();

            for (int i = 0; i < allUnits.Length; i++)
            {
                var u = allUnits[i];
                if (u != null)
                {
                    float sqrDist = (u.transform.position - center).sqrMagnitude;
                    if (sqrDist <= sqrRadius && (filter == null || filter(u)))
                    {
                        results.Add(u);
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Finds the closest unit to a world position matching an optional filter.
        /// </summary>
        public static Unit GetNearest(Vector3 position, Func<Unit, bool> filter = null)
        {
            Unit nearest = null;
            float nearestSqrDist = float.MaxValue;
            var allUnits = UnityEngine.Object.FindObjectsOfType<Unit>();

            for (int i = 0; i < allUnits.Length; i++)
            {
                var u = allUnits[i];
                if (u != null && (filter == null || filter(u)))
                {
                    float sqrDist = (u.transform.position - position).sqrMagnitude;
                    if (sqrDist < nearestSqrDist)
                    {
                        nearestSqrDist = sqrDist;
                        nearest = u;
                    }
                }
            }
            return nearest;
        }

        /// <summary>
        /// Checks if two units are on opposing factions.
        /// </summary>
        public static bool IsHostile(Unit a, Unit b)
        {
            if (a == null || b == null) return false;
            return a.MapHQ != null && b.MapHQ != null && a.MapHQ != b.MapHQ;
        }

        /// <summary>
        /// Checks if two units belong to the same faction.
        /// </summary>
        public static bool IsFriendly(Unit a, Unit b)
        {
            if (a == null || b == null) return false;
            return a.MapHQ != null && b.MapHQ != null && a.MapHQ == b.MapHQ;
        }

        // --- 2. Dynamic Combat, Health, Fuel & Ammo ---

        /// <summary>
        /// Repairs a unit and clears disabled state.
        /// </summary>
        public static void Repair(Unit unit, float healthPercent = 1.0f)
        {
            if (unit == null) return;

            unit.disabled = false;

            if (unit is Aircraft ac)
            {
                ac.disabled = false;
            }
        }

        /// <summary>
        /// Refuels a unit to the specified percentage (0.0 to 1.0).
        /// </summary>
        public static void Refuel(Unit unit, float fuelPercent = 1.0f)
        {
            if (unit == null) return;

            var tanks = unit.GetComponentsInChildren<FuelTank>(true);
            float totalCap = 0f;
            float targetRatio = Mathf.Clamp01(fuelPercent);

            if (tanks != null && tanks.Length > 0)
            {
                for (int i = 0; i < tanks.Length; i++)
                {
                    var t = tanks[i];
                    if (t != null)
                    {
                        t.fuelMass = t.FuelCapacity * targetRatio;
                        totalCap += t.FuelCapacity;
                    }
                }
            }

            if (unit is Aircraft ac)
            {
                if (totalCap > 0f)
                {
                    ac.fuelLevel = totalCap * targetRatio;
                }
                else
                {
                    ac.fuelLevel = 1000f * targetRatio;
                }
            }
        }

        /// <summary>
        /// Fully rearms all weapon stations and reloaders on a unit.
        /// </summary>
        public static void Rearm(Unit unit)
        {
            if (unit == null) return;

            var stations = unit.GetComponentsInChildren<WeaponStation>(true);
            for (int i = 0; i < stations.Length; i++)
            {
                var ws = stations[i];
                if (ws != null)
                {
                    ws.Rearm(ws.FullAmmo);
                }
            }

            unit.SearchForRearm();
        }

        /// <summary>
        /// Sets whether a unit is invulnerable to damage.
        /// </summary>
        public static void SetInvulnerable(Unit unit, bool invulnerable)
        {
            if (unit == null) return;
            if (invulnerable)
                _invulnerableUnits.Add(unit);
            else
                _invulnerableUnits.Remove(unit);
        }

        public static bool IsInvulnerable(Unit unit) => unit != null && _invulnerableUnits.Contains(unit);

        /// <summary>
        /// Sets whether a unit has infinite ammo.
        /// </summary>
        public static void SetInfiniteAmmo(Unit unit, bool infinite)
        {
            if (unit == null) return;
            if (infinite)
                _infiniteAmmoUnits.Add(unit);
            else
                _infiniteAmmoUnits.Remove(unit);
        }

        public static bool HasInfiniteAmmo(Unit unit) => unit != null && _infiniteAmmoUnits.Contains(unit);

        // --- 3. Dynamic Custom Metadata & Property Bag ---

        /// <summary>
        /// Attaches dynamic metadata to any unit without subclassing.
        /// </summary>
        public static void SetCustomData<T>(Unit unit, string key, T value)
        {
            if (unit == null || string.IsNullOrEmpty(key)) return;
            if (!_unitCustomData.TryGetValue(unit, out var dict))
            {
                dict = new Dictionary<string, object>();
                _unitCustomData[unit] = dict;
            }
            dict[key] = value;
        }

        /// <summary>
        /// Retrieves dynamic metadata attached to a unit.
        /// </summary>
        public static T GetCustomData<T>(Unit unit, string key, T defaultValue = default)
        {
            if (unit == null || string.IsNullOrEmpty(key)) return defaultValue;
            if (_unitCustomData.TryGetValue(unit, out var dict) && dict.TryGetValue(key, out var obj) && obj is T val)
            {
                return val;
            }
            return defaultValue;
        }

        /// <summary>
        /// Checks if a unit has specific metadata attached.
        /// </summary>
        public static bool HasCustomData(Unit unit, string key)
        {
            if (unit == null || string.IsNullOrEmpty(key)) return false;
            return _unitCustomData.TryGetValue(unit, out var dict) && dict.ContainsKey(key);
        }
    }
}
