using System;
using System.Collections.Generic;
using UnityEngine;

namespace CommandFramework.API
{
    /// <summary>
    /// Global manager that maps Units to their respective UnitOrderState.
    /// Provides thread-safe / lifecycle-safe querying and state mutation.
    /// </summary>
    public static class UnitStateManager
    {
        private static readonly Dictionary<Unit, UnitOrderState> _states = new Dictionary<Unit, UnitOrderState>();

        public static event Action<Unit, UnitOrderState> OnUnitStateCreated;
        public static event Action<Unit, string> OnCustomStateChanged;

        /// <summary>
        /// Gets or creates the UnitOrderState for the specified unit.
        /// </summary>
        public static UnitOrderState GetOrCreateState(Unit unit)
        {
            if (unit == null) return null;

            PruneDeadUnits();

            if (!_states.TryGetValue(unit, out var state))
            {
                state = new UnitOrderState(unit);
                _states[unit] = state;
                OnUnitStateCreated?.Invoke(unit, state);
            }
            return state;
        }

        /// <summary>
        /// Attempts to get the state for a unit if already tracked.
        /// </summary>
        public static bool TryGetState(Unit unit, out UnitOrderState state)
        {
            if (unit == null)
            {
                state = null;
                return false;
            }
            return _states.TryGetValue(unit, out state);
        }

        /// <summary>
        /// Sets a custom order state on a unit.
        /// </summary>
        public static void SetCustomState(Unit unit, string stateKey, string stateLabel = null, Color? stateColor = null)
        {
            if (unit == null) return;
            var state = GetOrCreateState(unit);
            state.CustomStateKey = stateKey;
            state.CustomStateLabel = stateLabel ?? stateKey;
            state.CustomStateColor = stateColor;

            OnCustomStateChanged?.Invoke(unit, stateKey);
            CommandFrameworkPlugin.LogInfo($"[CommandFramework] Custom state '{stateKey}' set on unit '{unit.NetworkunitName}'.");
        }

        /// <summary>
        /// Clears the custom state on a unit.
        /// </summary>
        public static void ClearCustomState(Unit unit)
        {
            if (unit == null) return;
            if (_states.TryGetValue(unit, out var state))
            {
                state.CustomStateKey = null;
                state.CustomStateLabel = null;
                state.CustomStateColor = null;
                OnCustomStateChanged?.Invoke(unit, null);
            }
        }

        /// <summary>
        /// Cleans up destroyed or disabled units from memory.
        /// </summary>
        public static void PruneDeadUnits()
        {
            if (_states.Count == 0) return;

            var toRemove = new List<Unit>();
            foreach (var kvp in _states)
            {
                if (kvp.Key == null || kvp.Key.disabled)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var dead in toRemove)
            {
                _states.Remove(dead);
            }
        }

        /// <summary>
        /// Clears all tracking (e.g. on match/mission change).
        /// </summary>
        public static void ClearAll()
        {
            _states.Clear();
        }
    }
}
