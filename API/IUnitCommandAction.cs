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
        /// Invoked when the player clicks the command action button.
        /// </summary>
        void Execute(Unit unit);
    }
}
