using System;
using System.Collections.Generic;
using UnityEngine;

namespace CommandFramework.API
{
    /// <summary>
    /// Represents the custom order and mission state of an individual unit.
    /// Supports custom states, arbitrary key-value metadata, and hold position flags.
    /// </summary>
    public class UnitOrderState
    {
        public Unit Unit { get; }

        /// <summary>
        /// Whether the unit is currently commanded to hold its position.
        /// </summary>
        public bool IsHoldPosition { get; set; }

        /// <summary>
        /// An optional custom state key set by third-party mods (e.g. "GuardZone", "PatrolRoute", "Escort").
        /// </summary>
        public string CustomStateKey { get; set; } = null;

        /// <summary>
        /// Formatted label for HUD/Context Menu (e.g. "GUARDING AIRBASE").
        /// </summary>
        public string CustomStateLabel { get; set; } = null;

        /// <summary>
        /// Optional color for custom state badge in the UI.
        /// </summary>
        public Color? CustomStateColor { get; set; } = null;

        /// <summary>
        /// Extensible metadata storage for third-party mod developers to attach arbitrary data to units.
        /// </summary>
        public Dictionary<string, object> CustomData { get; } = new Dictionary<string, object>();

        public UnitOrderState(Unit unit)
        {
            Unit = unit;
        }

        /// <summary>
        /// Retrieves custom data value by key, or returns default if not found.
        /// </summary>
        public T GetData<T>(string key, T defaultValue = default)
        {
            if (CustomData.TryGetValue(key, out object val) && val is T typedVal)
            {
                return typedVal;
            }
            return defaultValue;
        }

        /// <summary>
        /// Sets a custom data value.
        /// </summary>
        public void SetData(string key, object value)
        {
            CustomData[key] = value;
        }

        /// <summary>
        /// Checks if a custom data key exists.
        /// </summary>
        public bool HasData(string key)
        {
            return CustomData.ContainsKey(key);
        }

        /// <summary>
        /// Removes a custom data key.
        /// </summary>
        public bool RemoveData(string key)
        {
            return CustomData.Remove(key);
        }
    }
}
