using CommandFramework.API;
using UnityEngine;

namespace CommandFramework.Commands
{
    /// <summary>
    /// Built-in command action for toggling Rules of Engagement (RoE):
    /// Hold Fire (weapons safe, no automatic engagement) vs Engage at Will.
    /// </summary>
    public class HoldFireCommandAction : IUnitCommandAction
    {
        public string Id => "core.hold_fire";

        public int Priority => 20;

        public string GetDisplayName(Unit unit)
        {
            bool isHoldFire = HoldFireManager.IsHoldFire(unit);
            return isHoldFire ? "🔥 ENGAGE AT WILL" : "🎯 HOLD FIRE";
        }

        public bool IsVisible(Unit unit)
        {
            return unit != null && !unit.disabled;
        }

        public bool IsEnabled(Unit unit)
        {
            return unit != null && !unit.disabled;
        }

        public Color? GetButtonColor(Unit unit)
        {
            bool isHoldFire = HoldFireManager.IsHoldFire(unit);
            if (isHoldFire)
            {
                return new Color(0.06f, 0.88f, 0.50f); // Green Engage
            }
            return new Color(1.0f, 0.35f, 0.35f); // Red Hold Fire
        }

        public void Execute(Unit unit)
        {
            if (unit == null || unit.disabled) return;
            HoldFireManager.ToggleHoldFire(unit);
        }
    }
}
