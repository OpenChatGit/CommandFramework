using CommandFramework.API;
using UnityEngine;

namespace CommandFramework.Commands
{
    /// <summary>
    /// Core built-in command action for toggling Stop / Hold Position on Ground Vehicles and Ships.
    /// </summary>
    public class HoldPositionCommandAction : IUnitCommandAction
    {
        public string Id => "core.hold_position";

        public int Priority => 10; // Top of menu

        public string GetDisplayName(Unit unit)
        {
            bool isHolding = HoldPositionManager.IsHoldingPosition(unit);
            return isHolding ? "▶ RESUME" : "⏹ STOP";
        }

        public bool IsVisible(Unit unit)
        {
            return unit != null && !unit.disabled && (unit is GroundVehicle || unit is Ship);
        }

        public bool IsEnabled(Unit unit)
        {
            return unit != null && !unit.disabled;
        }

        public Color? GetButtonColor(Unit unit)
        {
            bool isHolding = HoldPositionManager.IsHoldingPosition(unit);
            if (isHolding)
            {
                return new Color(1.0f, 0.65f, 0.15f); // Amber Resume
            }
            return new Color(0.06f, 0.88f, 0.50f); // Green Stop
        }

        public void Execute(Unit unit)
        {
            if (unit == null || unit.disabled) return;
            HoldPositionManager.ToggleHoldPosition(unit);
        }
    }
}
