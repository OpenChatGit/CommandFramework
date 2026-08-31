using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CommandFramework.API;
using UnityEngine;

namespace CommandFramework.UI.Declarative
{
    /// <summary>
    /// Evaluates dynamic variables, expressions, and event bindings for declarative HTML/CSS UI documents.
    /// </summary>
    public class UIBindingContext
    {
        private static readonly Regex BindingRegex = new Regex(@"\{\{\s*(.*?)\s*\}\}", RegexOptions.Compiled);

        public Unit TargetUnit { get; set; }
        public UnitOrderState OrderState { get; set; }
        public Dictionary<string, object> Variables { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Action> Actions { get; } = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

        public UIBindingContext(Unit unit = null)
        {
            TargetUnit = unit;
            if (unit != null)
            {
                OrderState = CommandFrameworkAPI.GetOrderState(unit);
            }
        }

        public void SetVariable(string key, object value)
        {
            Variables[key] = value;
        }

        public void RegisterAction(string actionKey, Action callback)
        {
            Actions[actionKey] = callback;
        }

        public string ResolveText(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            return BindingRegex.Replace(input, match =>
            {
                string expr = match.Groups[1].Value.Trim();
                return EvaluateExpression(expr);
            });
        }

        public string EvaluateExpression(string expr)
        {
            if (Variables.TryGetValue(expr, out var customVal) && customVal != null)
            {
                return customVal.ToString();
            }

            if (TargetUnit != null)
            {
                if (expr.Equals("unit.name", StringComparison.OrdinalIgnoreCase)) return TargetUnit.NetworkunitName ?? TargetUnit.name;
                if (expr.Equals("unit.type", StringComparison.OrdinalIgnoreCase)) return TargetUnit.GetType().Name;
                if (expr.Equals("unit.speed", StringComparison.OrdinalIgnoreCase)) return $"{TargetUnit.speed:F0} m/s";
                if (expr.Equals("unit.altitude", StringComparison.OrdinalIgnoreCase)) return $"{TargetUnit.radarAlt:F0} m";
            }

            if (OrderState != null)
            {
                if (expr.Equals("order.badge", StringComparison.OrdinalIgnoreCase)) return OrderState.DisplayBadge;
                if (expr.Equals("order.state", StringComparison.OrdinalIgnoreCase)) return OrderState.CustomStateKey;
            }

            return matchFallback(expr);
        }

        private string matchFallback(string expr)
        {
            return $"[{expr}]";
        }

        public void TriggerAction(string actionExpr)
        {
            if (string.IsNullOrEmpty(actionExpr)) return;

            actionExpr = actionExpr.Trim();

            // Custom registered action
            if (Actions.TryGetValue(actionExpr, out var action) && action != null)
            {
                action.Invoke();
                return;
            }

            // Command Framework registered command by ID (e.g. "core.hold_position")
            if (TargetUnit != null)
            {
                foreach (var cmd in CommandFrameworkAPI.RegisteredCommands)
                {
                    if (cmd.Id.Equals(actionExpr, StringComparison.OrdinalIgnoreCase))
                    {
                        CommandFrameworkAPI.ExecuteCommand(cmd.Id, TargetUnit);
                        return;
                    }
                }
            }
        }
    }
}
