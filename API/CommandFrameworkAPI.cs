using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommandFramework.UI;
using CommandFramework.UI.Declarative;
using UnityEngine;

namespace CommandFramework.API
{
    /// <summary>
    /// Central entry point for the Command Framework SDK & UI Engine.
    /// Provides access to Command Registration, Fluent Command Builders, Declarative HTML/CSS UI, Theming, and Game Utilities.
    /// </summary>
    public static class CommandFrameworkAPI
    {
        private static readonly List<IUnitCommandAction> _commands = new List<IUnitCommandAction>();
        private static readonly Dictionary<Unit, UnitOrderState> _orderStates = new Dictionary<Unit, UnitOrderState>();
        private static readonly List<OrderFilterDelegate> _orderFilters = new List<OrderFilterDelegate>();

        public static IReadOnlyList<IUnitCommandAction> RegisteredCommands => _commands.AsReadOnly();

        /// <summary>
        /// Active tactical context menu renderer. Can be replaced or disabled by any third-party mod.
        /// </summary>
        public static IMenuRenderer MenuRenderer { get; set; } = new DefaultMenuRenderer();

        /// <summary>
        /// Declarative HTML & CSS UI Engine facade.
        /// </summary>
        public static class UI
        {
            public static UIDocument Parse(string html, string css = null) => UIDocument.Parse(html, css);
            public static UIDocument LoadFromFile(string htmlPath, string cssPath = null) => UIDocument.LoadFromFile(htmlPath, cssPath);
            public static void SetTheme(string themeName) => UIThemeManager.SetTheme(themeName);
        }

        /// <summary>
        /// Universal Nuclear Option game helpers.
        /// </summary>
        public static class Game
        {
            public static bool SetDestination(Unit unit, Vector3 pos) => GameAPI.SetUnitDestination(unit, pos);
            public static void SetHoldPosition(Unit unit, bool hold) => GameAPI.SetUnitHoldPosition(unit, hold);
            public static Vector3 GetMapCursorWorldPosition(DynamicMap map) => GameAPI.GetMapCursorWorldPosition(map);
            public static Unit FindUnitUnderMouse(DynamicMap map, Vector2 mousePos, float radius = 24f) => GameAPI.FindUnitUnderMouse(map, mousePos, radius);
            public static bool IsFriendly(Unit a, Unit b) => GameAPI.IsFriendly(a, b);
        }

        /// <summary>
        /// Tactical Map, Overlays, and Freehand Drawing API.
        /// </summary>
        public static class Map
        {
            public static bool IsOpen => TacticalMapAPI.IsMapOpen();
            public static bool IsDrawingMode { get => TacticalMapAPI.IsDrawingMode; set => TacticalMapAPI.IsDrawingMode = value; }
            public static Color DrawColor { get => TacticalMapAPI.ActiveDrawColor; set => TacticalMapAPI.ActiveDrawColor = value; }
            public static float DrawWidth { get => TacticalMapAPI.ActiveDrawWidth; set => TacticalMapAPI.ActiveDrawWidth = value; }

            public static Vector2 WorldToScreen(Vector3 worldPos) => TacticalMapAPI.WorldToScreen(worldPos);
            public static Vector3 ScreenToWorld(Vector2 screenPos) => TacticalMapAPI.ScreenToWorld(screenPos);
            public static Vector3 CursorWorldPosition => TacticalMapAPI.GetCursorWorldPosition();

            public static void AddStroke(TacticalStroke stroke) => TacticalMapAPI.AddStroke(stroke);
            public static void Undo() => TacticalMapAPI.UndoLastStroke();
            public static void Redo() => TacticalMapAPI.RedoLastStroke();
            public static void Clear() => TacticalMapAPI.ClearAll();
            public static void AddThreatZone(Vector3 center, float radiusMeters, Color borderColor, Color? fillColor = null, float borderWidth = 2f, string label = null) =>
                TacticalMapAPI.AddThreatZone(center, radiusMeters, borderColor, fillColor, borderWidth, label);
        }

        // --- Command Builder & Registration ---

        /// <summary>
        /// Creates a fluent CommandBuilder to declare and register custom commands in 3 lines of code.
        /// </summary>
        public static CommandBuilder CreateCommand(string id)
        {
            return new CommandBuilder(id);
        }

        public static void RegisterCommand(IUnitCommandAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (_commands.Exists(c => c.Id == action.Id))
            {
                CommandFrameworkPlugin.Log?.LogWarning($"Command '{action.Id}' already registered. Replacing.");
                _commands.RemoveAll(c => c.Id == action.Id);
            }
            _commands.Add(action);
            _commands.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public static void UnregisterCommand(string id)
        {
            _commands.RemoveAll(c => c.Id == id);
        }

        public static List<IUnitCommandAction> GetVisibleCommandsForUnit(Unit unit)
        {
            var list = new List<IUnitCommandAction>();
            if (unit == null || unit.disabled) return list;

            for (int i = 0; i < _commands.Count; i++)
            {
                var cmd = _commands[i];
                if (cmd != null && cmd.IsVisible(unit) && cmd.IsEnabled(unit))
                {
                    list.Add(cmd);
                }
            }
            return list;
        }

        public static bool ExecuteCommand(IUnitCommandAction cmd, Unit unit)
        {
            if (cmd != null && cmd.IsEnabled(unit))
            {
                cmd.Execute(unit);
                return true;
            }
            return false;
        }

        public static bool ExecuteCommand(string id, Unit unit)
        {
            var cmd = _commands.Find(c => c.Id == id);
            if (cmd != null && cmd.IsEnabled(unit))
            {
                cmd.Execute(unit);
                return true;
            }
            return false;
        }

        // --- Order State Tracking ---

        public static UnitOrderState GetOrderState(Unit unit)
        {
            if (unit == null) return UnitOrderState.Empty;
            if (_orderStates.TryGetValue(unit, out var state))
            {
                return state;
            }
            return UnitOrderState.Empty;
        }

        public static void SetOrderState(Unit unit, UnitOrderState state)
        {
            if (unit == null) return;
            _orderStates[unit] = state;
        }

        public static void ClearOrderState(Unit unit)
        {
            if (unit == null) return;
            _orderStates.Remove(unit);
        }
    }
}
