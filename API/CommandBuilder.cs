using System;
using UnityEngine;

namespace CommandFramework.API
{
    /// <summary>
    /// Fluent builder for rapidly creating and registering custom unit commands without boilerplate classes.
    /// </summary>
    public class CommandBuilder
    {
        private readonly string _id;
        private int _priority = 50;
        private Func<Unit, string> _nameProvider;
        private Func<Unit, bool> _visibilityCondition = u => true;
        private Func<Unit, bool> _enableCondition = u => u != null && !u.disabled;
        private Func<Unit, Color?> _colorProvider;
        private Func<Unit, Texture2D> _iconProvider;
        private Action<Unit> _executeAction;

        public CommandBuilder(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _id = id;
            _nameProvider = u => id.ToUpper();
        }

        /// <summary>
        /// Sets a static display name for the command button.
        /// </summary>
        public CommandBuilder WithName(string name)
        {
            _nameProvider = u => name;
            return this;
        }

        /// <summary>
        /// Sets a dynamic display name generator for the command button.
        /// </summary>
        public CommandBuilder WithDynamicName(Func<Unit, string> nameProvider)
        {
            _nameProvider = nameProvider ?? (u => _id.ToUpper());
            return this;
        }

        /// <summary>
        /// Sets the display priority (lower values appear higher up in the menu).
        /// </summary>
        public CommandBuilder WithPriority(int priority)
        {
            _priority = priority;
            return this;
        }

        /// <summary>
        /// Sets a static HD icon texture for the command button.
        /// </summary>
        public CommandBuilder WithIcon(Texture2D icon)
        {
            _iconProvider = u => icon;
            return this;
        }

        /// <summary>
        /// Sets a dynamic icon provider for the command button.
        /// </summary>
        public CommandBuilder WithDynamicIcon(Func<Unit, Texture2D> iconProvider)
        {
            _iconProvider = iconProvider;
            return this;
        }

        /// <summary>
        /// Sets a custom button tint color override.
        /// </summary>
        public CommandBuilder WithColor(Color color)
        {
            _colorProvider = u => color;
            return this;
        }

        /// <summary>
        /// Sets a dynamic color provider for the command button.
        /// </summary>
        public CommandBuilder WithDynamicColor(Func<Unit, Color?> colorProvider)
        {
            _colorProvider = colorProvider;
            return this;
        }

        /// <summary>
        /// Restricts visibility of the command to units that satisfy the predicate.
        /// </summary>
        public CommandBuilder ForUnits(Func<Unit, bool> predicate)
        {
            _visibilityCondition = predicate ?? (u => true);
            return this;
        }

        /// <summary>
        /// Defines the condition required for the command button to be interactable/clickable.
        /// </summary>
        public CommandBuilder When(Func<Unit, bool> enableCondition)
        {
            _enableCondition = enableCondition ?? (u => u != null && !u.disabled);
            return this;
        }

        /// <summary>
        /// Defines the action to execute when the button is clicked.
        /// </summary>
        public CommandBuilder OnExecute(Action<Unit> executeAction)
        {
            _executeAction = executeAction;
            return this;
        }

        /// <summary>
        /// Builds the command into an IUnitCommandAction instance.
        /// </summary>
        public IUnitCommandAction Build()
        {
            if (_executeAction == null) throw new InvalidOperationException($"Command '{_id}' must define an OnExecute action.");
            return new BuiltCommandAction(_id, _priority, _nameProvider, _visibilityCondition, _enableCondition, _colorProvider, _iconProvider, _executeAction);
        }

        /// <summary>
        /// Builds and registers the command directly with CommandFrameworkAPI.
        /// </summary>
        public IUnitCommandAction Register()
        {
            var action = Build();
            CommandFrameworkAPI.RegisterCommand(action);
            return action;
        }

        private class BuiltCommandAction : IUnitCommandAction
        {
            public string Id { get; }
            public int Priority { get; }

            private readonly Func<Unit, string> _nameProvider;
            private readonly Func<Unit, bool> _visibilityCondition;
            private readonly Func<Unit, bool> _enableCondition;
            private readonly Func<Unit, Color?> _colorProvider;
            private readonly Func<Unit, Texture2D> _iconProvider;
            private readonly Action<Unit> _executeAction;

            public BuiltCommandAction(
                string id, 
                int priority, 
                Func<Unit, string> nameProvider, 
                Func<Unit, bool> visibilityCondition, 
                Func<Unit, bool> enableCondition, 
                Func<Unit, Color?> colorProvider, 
                Func<Unit, Texture2D> iconProvider, 
                Action<Unit> executeAction)
            {
                Id = id;
                Priority = priority;
                _nameProvider = nameProvider;
                _visibilityCondition = visibilityCondition;
                _enableCondition = enableCondition;
                _colorProvider = colorProvider;
                _iconProvider = iconProvider;
                _executeAction = executeAction;
            }

            public string GetDisplayName(Unit unit) => _nameProvider?.Invoke(unit) ?? Id;
            public bool IsVisible(Unit unit) => _visibilityCondition?.Invoke(unit) ?? true;
            public bool IsEnabled(Unit unit) => _enableCondition?.Invoke(unit) ?? true;
            public Color? GetButtonColor(Unit unit) => _colorProvider?.Invoke(unit);
            public Texture2D GetIcon(Unit unit) => _iconProvider?.Invoke(unit);
            public void Execute(Unit unit) => _executeAction?.Invoke(unit);
        }
    }
}
