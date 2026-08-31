using System;
using System.Collections.Generic;
using UnityEngine;

namespace CommandFramework.API
{
    /// <summary>
    /// Contract for implementing custom unit command actions in Command Framework.
    /// </summary>
    public interface IUnitCommandAction
    {
        /// <summary>
        /// Unique identifier for the command action (e.g. "core.hold_position", "com.author.mod.guard").
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Display priority in the Context Menu. Lower values appear higher up in the menu.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Returns the dynamic text label to display on the action button.
        /// </summary>
        string GetDisplayName(Unit unit);

        /// <summary>
        /// Determines whether this action should appear in the context menu for the given unit.
        /// </summary>
        bool IsVisible(Unit unit);

        /// <summary>
        /// Determines whether the action button is interactable/clickable for the given unit.
        /// </summary>
        bool IsEnabled(Unit unit);

        /// <summary>
        /// Optional custom button background / text tint color override. Return null for default theme.
        /// </summary>
        Color? GetButtonColor(Unit unit);

        /// <summary>
        /// Optional high-definition icon texture (64x64 or 32x32) to display on the button. Return null for text-only.
        /// </summary>
        Texture2D GetIcon(Unit unit);

        /// <summary>
        /// Invoked when the player clicks the command action button.
        /// </summary>
        void Execute(Unit unit);
    }

    /// <summary>
    /// Delegate signature for filtering destination and AI waypoint changes.
    /// Return true to allow the order, or false to block it.
    /// </summary>
    public delegate bool OrderFilterDelegate(Unit unit, Vector3 targetPosition);

    /// <summary>
    /// Holds custom order states, tags, and arbitrary metadata attached to a specific Unit.
    /// </summary>
    public class UnitOrderState
    {
        public static readonly UnitOrderState Empty = new UnitOrderState(null);

        public Unit Unit { get; }
        public string CustomStateKey { get; set; }
        public string DisplayBadge { get; set; }
        public Color? BadgeColor { get; set; }

        private readonly Dictionary<string, object> _customData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public UnitOrderState(Unit unit)
        {
            Unit = unit;
        }

        public void SetData<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _customData[key] = value;
        }

        public T GetData<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;
            if (_customData.TryGetValue(key, out var obj) && obj is T typed)
            {
                return typed;
            }
            return defaultValue;
        }

        public bool HasData(string key)
        {
            return !string.IsNullOrEmpty(key) && _customData.ContainsKey(key);
        }

        public void RemoveData(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _customData.Remove(key);
            }
        }

        public void Clear()
        {
            CustomStateKey = null;
            DisplayBadge = null;
            BadgeColor = null;
            _customData.Clear();
        }
    }
}
