using System;
using UnityEngine;

namespace CommandFramework.API
{
    /// <summary>
    /// Universal, un-opinionated helper utilities for interacting with Nuclear Option game types.
    /// Eliminates the need for modders to write complex reflection or Harmony hooks for common unit operations.
    /// </summary>
    public static class GameAPI
    {
        /// <summary>
        /// Issues a clean movement / waypoint destination order to any ground vehicle or ship.
        /// </summary>
        public static bool SetUnitDestination(Unit unit, Vector3 worldPosition)
        {
            if (unit == null || unit.disabled) return false;

            var cmd = unit.GetComponent<UnitCommand>();
            if (cmd != null)
            {
                cmd.SetDestination(new GlobalPosition(worldPosition), true);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Halts or resumes movement for a ground vehicle or ship.
        /// </summary>
        public static void SetUnitHoldPosition(Unit unit, bool hold)
        {
            if (unit == null || unit.disabled) return;

            if (unit is GroundVehicle gv)
            {
                gv.SetHoldPosition(hold);
            }
            else if (unit is Ship ship)
            {
                ship.SetHoldPosition(hold);
            }
        }

        /// <summary>
        /// Converts the player's current cursor position on the DynamicMap into 3D world space coordinates.
        /// </summary>
        public static Vector3 GetMapCursorWorldPosition(DynamicMap map)
        {
            if (map == null) return Vector3.zero;
            var coord = map.GetCursorCoordinates();
            return new Vector3((float)coord.x, (float)coord.y, (float)coord.z);
        }

        /// <summary>
        /// Finds the closest unit within screen radius under the mouse on the tactical map.
        /// </summary>
        public static Unit FindUnitUnderMouse(DynamicMap map, Vector2 mousePos, float thresholdRadius = 32f)
        {
            if (map == null) return null;

            Unit closest = null;
            float closestDist = thresholdRadius;

            // Search Ground Vehicles
            foreach (var gv in UnityEngine.Object.FindObjectsOfType<GroundVehicle>())
            {
                if (gv == null || gv.disabled) continue;
                if (DynamicMap.TryGetMapIcon(gv, out var icon) && icon != null && icon.gameObject.activeInHierarchy)
                {
                    Vector2 iconScreenPos = new Vector2(icon.transform.position.x, icon.transform.position.y);
                    float dist = Vector2.Distance(mousePos, iconScreenPos);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = gv;
                    }
                }
            }

            // Search Ships
            foreach (var ship in UnityEngine.Object.FindObjectsOfType<Ship>())
            {
                if (ship == null || ship.disabled) continue;
                if (DynamicMap.TryGetMapIcon(ship, out var icon) && icon != null && icon.gameObject.activeInHierarchy)
                {
                    Vector2 iconScreenPos = new Vector2(icon.transform.position.x, icon.transform.position.y);
                    float dist = Vector2.Distance(mousePos, iconScreenPos);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = ship;
                    }
                }
            }

            return closest;
        }

        /// <summary>
        /// Checks whether two units belong to the same faction / HQ.
        /// </summary>
        public static bool IsFriendly(Unit a, Unit b)
        {
            if (a == null || b == null) return false;
            return a.MapHQ == b.MapHQ;
        }
    }
}
