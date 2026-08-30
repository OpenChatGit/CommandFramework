using System;
using System.Collections.Generic;
using UnityEngine;

namespace CommandFramework.API
{
    /// <summary>
    /// Public API for registering custom unit commands, attaching custom order states to units,
    /// triggering AI nudges, and filtering destination/order changes from other systems.
    /// </summary>
    public static class CommandFrameworkAPI
    {
        private static readonly List<IUnitCommandAction> _registeredCommands = new List<IUnitCommandAction>();
        private static readonly List<Func<Unit, GlobalPosition, bool>> _orderFilters = new List<Func<Unit, GlobalPosition, bool>>();

        public static event Action<Unit, IUnitCommandAction> OnCommandExecuted;
        public static event Action<Unit, bool> OnHoldPositionChanged;
        public static event Action<Unit, string> OnCustomOrderStateChanged;

        static CommandFrameworkAPI()
        {
            HoldPositionManager.OnHoldPositionChanged += (unit, isHolding) =>
            {
                OnHoldPositionChanged?.Invoke(unit, isHolding);
            };

            UnitStateManager.OnCustomStateChanged += (unit, stateKey) =>
            {
                OnCustomOrderStateChanged?.Invoke(unit, stateKey);
            };
        }

        /// <summary>
        /// Registers a custom unit command action.
        /// </summary>
        public static void RegisterCommand(IUnitCommandAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (_registeredCommands.Exists(c => c.Id == action.Id))
            {
                CommandFrameworkPlugin.LogWarning($"[CommandFrameworkAPI] Command with ID '{action.Id}' is already registered. Overwriting.");
                _registeredCommands.RemoveAll(c => c.Id == action.Id);
            }

            _registeredCommands.Add(action);
            _registeredCommands.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            CommandFrameworkPlugin.LogInfo($"[CommandFrameworkAPI] Registered command '{action.Id}' (Priority {action.Priority}).");
        }

        /// <summary>
        /// Unregisters a command action by ID.
        /// </summary>
        public static bool UnregisterCommand(string commandId)
        {
            int removed = _registeredCommands.RemoveAll(c => c.Id == commandId);
            return removed > 0;
        }

        /// <summary>
        /// Returns all registered command actions that are visible for the given unit.
        /// </summary>
        public static List<IUnitCommandAction> GetVisibleCommandsForUnit(Unit unit)
        {
            var visible = new List<IUnitCommandAction>();
            if (unit == null) return visible;

            foreach (var cmd in _registeredCommands)
            {
                try
                {
                    if (cmd.IsVisible(unit))
                    {
                        visible.Add(cmd);
                    }
                }
                catch (Exception ex)
                {
                    CommandFrameworkPlugin.LogError($"[CommandFrameworkAPI] Error checking visibility for command '{cmd.Id}': {ex}");
                }
            }

            return visible;
        }

        /// <summary>
        /// Executes a command action on the specified unit and fires events.
        /// </summary>
        public static void ExecuteCommand(IUnitCommandAction action, Unit unit)
        {
            if (action == null || unit == null) return;

            try
            {
                action.Execute(unit);
                OnCommandExecuted?.Invoke(unit, action);
                CommandFrameworkPlugin.LogInfo($"[CommandFrameworkAPI] Executed command '{action.Id}' on '{unit.NetworkunitName}'.");
            }
            catch (Exception ex)
            {
                CommandFrameworkPlugin.LogError($"[CommandFrameworkAPI] Error executing command '{action.Id}': {ex}");
            }
        }

        /// <summary>
        /// Retrieves the UnitOrderState for a given unit.
        /// </summary>
        public static UnitOrderState GetOrderState(Unit unit)
        {
            return UnitStateManager.GetOrCreateState(unit);
        }

        /// <summary>
        /// Sets a custom order state and visual label on the unit.
        /// </summary>
        public static void SetCustomOrderState(Unit unit, string stateKey, string stateLabel = null, Color? stateColor = null)
        {
            UnitStateManager.SetCustomState(unit, stateKey, stateLabel, stateColor);
        }

        /// <summary>
        /// Clears any custom order state on the unit.
        /// </summary>
        public static void ClearCustomOrderState(Unit unit)
        {
            UnitStateManager.ClearCustomState(unit);
        }

        /// <summary>
        /// Sets the unit's Hold Position state.
        /// </summary>
        public static void SetHoldPosition(Unit unit, bool hold)
        {
            HoldPositionManager.SetHoldPosition(unit, hold);
        }

        /// <summary>
        /// Checks if the unit is currently in Hold Position.
        /// </summary>
        public static bool IsHoldingPosition(Unit unit)
        {
            return HoldPositionManager.IsHoldingPosition(unit);
        }

        /// <summary>
        /// Wakes up / nudges the unit's AI to resume pathfinding and autonomous action.
        /// </summary>
        public static void NudgeAI(Unit unit)
        {
            HoldPositionManager.NudgeAI(unit);
        }

        /// <summary>
        /// Registers a destination filter delegate. Return false from the delegate to block the destination change.
        /// </summary>
        public static void RegisterOrderFilter(Func<Unit, GlobalPosition, bool> filter)
        {
            if (filter != null && !_orderFilters.Contains(filter))
            {
                _orderFilters.Add(filter);
            }
        }

        /// <summary>
        /// Evaluates all registered order filters and hold position state to check if a destination change is permitted.
        /// </summary>
        public static bool IsDestinationAllowed(Unit unit, GlobalPosition targetPosition)
        {
            if (unit == null) return true;

            if (HoldPositionManager.IsHoldingPosition(unit))
            {
                return false;
            }

            foreach (var filter in _orderFilters)
            {
                try
                {
                    if (!filter(unit, targetPosition))
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    CommandFrameworkPlugin.LogError($"[CommandFrameworkAPI] Error in OrderFilter: {ex}");
                }
            }

            return true;
        }
    }
}
