using CommandFramework.API;
using UnityEngine;

namespace CommandFramework.Commands
{
    /// <summary>
    /// Built-in command action for toggling continuous Patrol Loop mode on multi-waypoint routes.
    /// When enabled, the unit continuously cycles through its waypoint chain indefinitely.
    /// </summary>
    public class PatrolLoopCommandAction : IUnitCommandAction
    {
        public string Id => "core.patrol_loop";

        public int Priority => 30;

        public string GetDisplayName(Unit unit)
        {
            bool isLoop = WaypointQueueManager.IsLoopMode(unit);
            return isLoop ? "⏹ END LOOP" : "🔄 PATROL LOOP";
        }

        public bool IsVisible(Unit unit)
        {
            return unit != null && !unit.disabled;
        }

        public bool IsEnabled(Unit unit)
        {
            if (unit == null || unit.disabled) return false;
            var queue = WaypointQueueManager.GetQueue(unit);
            return queue != null && queue.Count > 1;
        }

        public Color? GetButtonColor(Unit unit)
        {
            bool isLoop = WaypointQueueManager.IsLoopMode(unit);
            if (isLoop)
            {
                return new Color(1.0f, 0.65f, 0.15f); // Amber End Loop
            }
            return new Color(0.06f, 0.88f, 0.50f); // Green Loop
        }

        public void Execute(Unit unit)
        {
            if (unit == null || unit.disabled) return;
            WaypointQueueManager.ToggleLoopMode(unit);
        }
    }
}
