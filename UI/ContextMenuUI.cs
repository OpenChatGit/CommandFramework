using System;
using System.Collections.Generic;
using CommandFramework.API;
using UnityEngine;

namespace CommandFramework.UI
{
    /// <summary>
    /// Tactical Context Menu Controller.
    /// Intercepts mouse input and delegates context menu rendering to CommandFrameworkAPI.MenuRenderer.
    /// </summary>
    public class ContextMenuUI : MonoBehaviour
    {
        public static ContextMenuUI Instance { get; private set; }

        private bool _isOpen = false;
        private Unit _targetUnit;
        private Vector2 _screenPosition;
        private Rect _menuRect;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                TryWorldRightClick();
            }

            if (_isOpen && (_targetUnit == null || _targetUnit.disabled))
            {
                Close();
            }
        }

        public void OpenForUnit(Unit unit, Vector2 screenPos)
        {
            if (unit == null || unit.disabled) return;
            if (!CommandFrameworkSettings.EnableDefaultContextMenu) return;

            _targetUnit = unit;
            _screenPosition = screenPos;
            _isOpen = true;

            // Recalculate dimensions via active renderer
            var renderer = CommandFrameworkAPI.MenuRenderer;
            var actions = CommandFrameworkAPI.GetVisibleCommandsForUnit(unit);
            Vector2 dim = renderer != null ? renderer.CalculateMenuDimensions(unit, actions) : new Vector2(230f, 150f);

            float x = Mathf.Clamp(_screenPosition.x, 10f, Screen.width - dim.x - 10f);
            float y = Mathf.Clamp(Screen.height - _screenPosition.y, 10f, Screen.height - dim.y - 10f);
            _menuRect = new Rect(x, y, dim.x, dim.y);

            CommandFrameworkPlugin.Log?.LogInfo($"[ContextMenuUI] Context menu opened for unit '{unit.gameObject.name}' at ({x:F0}, {y:F0})");
        }

        public void Close()
        {
            _isOpen = false;
            _targetUnit = null;
        }

        private void TryWorldRightClick()
        {
            if (DynamicMap.mapMaximized) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray, 10000f);

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                var unit = hit.collider.GetComponentInParent<Unit>();
                if (unit != null && !unit.disabled && (unit is GroundVehicle || unit is Ship))
                {
                    OpenForUnit(unit, Input.mousePosition);
                    return;
                }
            }
        }

        private void OnGUI()
        {
            if (!_isOpen || _targetUnit == null || _targetUnit.disabled) return;
            if (!CommandFrameworkSettings.EnableDefaultContextMenu) return;

            GUI.depth = -99999;

            var renderer = CommandFrameworkAPI.MenuRenderer;
            if (renderer == null) return;

            var visibleActions = CommandFrameworkAPI.GetVisibleCommandsForUnit(_targetUnit);
            renderer.RenderMenu(_menuRect, _targetUnit, visibleActions, Close);

            // Close on left-click outside menu
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Vector2 mousePos = Event.current.mousePosition;
                if (!_menuRect.Contains(mousePos))
                {
                    Close();
                }
            }
        }
    }
}
