using System;
using System.Collections.Generic;
using CommandFramework.API;
using CommandFramework.Commands;
using HarmonyLib;
using UnityEngine;

namespace CommandFramework.UI
{
    /// <summary>
    /// Minimalist tactical in-game IMGUI context menu for commanding units.
    /// Rendered with a crisp green outline border and compact action buttons.
    /// </summary>
    public class ContextMenuUI : MonoBehaviour
    {
        public static ContextMenuUI Instance { get; private set; }

        private bool _isOpen;
        private Unit _targetUnit;
        private Rect _menuRect;

        // Visual Styles
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _buttonMinimalStopStyle;
        private GUIStyle _buttonMinimalResumeStyle;
        private GUIStyle _buttonCustomStyle;

        private Texture2D _bgBorderTexture;
        private Texture2D _btnStopTexture;
        private Texture2D _btnResumeTexture;
        private Texture2D _btnHoverTexture;
        private bool _stylesInitialized;

        public static bool IsOpen => Instance != null && Instance._isOpen;
        public static Unit CurrentUnit => Instance?._targetUnit;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // Close on Escape
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            // Left-Click on Empty Space Deselection
            if (Input.GetMouseButtonDown(0))
            {
                TryHandleLeftClick();
            }

            // 3D World Right Click
            if (Input.GetMouseButtonDown(1))
            {
                TryWorldRightClick();
            }

            // Close if unit is gone
            if (_isOpen && (_targetUnit == null || _targetUnit.disabled))
            {
                Close();
            }
        }

        private void TryHandleLeftClick()
        {
            // If clicking inside our open menu, let IMGUI process it
            if (_isOpen)
            {
                Vector2 mousePosImgui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                if (_menuRect.Contains(mousePosImgui)) return;
                
                // Clicking outside an open menu in 3D view simply closes the menu
                if (!DynamicMap.mapMaximized)
                {
                    Close();
                    return;
                }
            }

            // Deselection ONLY applies when the tactical map is maximized/open
            if (!DynamicMap.mapMaximized)
            {
                return;
            }

            // If clicking on any unit icon on the map, don't deselect (user is selecting/interacting)
            if (Patches.RightClickSuppressor.TryGetUnitUnderCursor(out _)) return;

            // Otherwise, it is an empty space click on the tactical map: deselect map icons
            TryDeselectMapEmptySpace();
        }

        private void TryDeselectMapEmptySpace()
        {
            if (_isOpen)
            {
                Close();
            }

            try
            {
                // Deselect only on the Tactical Map
                if (DynamicMap.i != null)
                {
                    DynamicMap.i.UnselectAll();
                    DynamicMap.i.DeselectAllIcons();
                }

                // Clear Mission Editor map selection if active
                if (NuclearOption.MissionEditorScripts.UnitSelection.i != null)
                {
                    NuclearOption.MissionEditorScripts.UnitSelection.i.ClearSelection();
                }

                CommandFrameworkPlugin.LogInfo("[CommandFramework] Deselected map units cleanly via empty map space left-click.");
            }
            catch (Exception ex)
            {
                CommandFrameworkPlugin.LogError($"[CommandFramework] Error during map deselection: {ex}");
            }
        }

        private void TryWorldRightClick()
        {
            if (_isOpen)
            {
                Vector2 mousePosImgui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                if (_menuRect.Contains(mousePosImgui)) return;
            }

            if (Patches.RightClickSuppressor.TryGetUnitUnderCursor(out Unit unit))
            {
                Patches.RightClickSuppressor.SuppressThisFrame();
                OpenForUnit(unit, Input.mousePosition);
            }
            else if (_isOpen)
            {
                Close();
            }
        }

        /// <summary>
        /// Opens the minimal context menu at the cursor for the target unit.
        /// </summary>
        public static void OpenForUnit(Unit unit, Vector2 screenPos)
        {
            if (unit == null || unit.disabled || !Patches.TacticalWaypointPatches.IsUnitFriendlyToPlayer(unit)) return;

            if (Instance == null)
            {
                var go = new GameObject("CommandFramework_UI");
                go.AddComponent<ContextMenuUI>();
            }

            Instance._targetUnit = unit;
            Instance._isOpen = true;

            var visibleActions = CommandFrameworkAPI.GetVisibleCommandsForUnit(unit);
            float totalHeight = 44f + (visibleActions.Count * 28f);

            float imguiX = Mathf.Clamp(screenPos.x, 10f, Screen.width - 190f);
            float imguiY = Mathf.Clamp(Screen.height - screenPos.y, 10f, Screen.height - totalHeight - 20f);
            Instance._menuRect = new Rect(imguiX, imguiY, 180, totalHeight);

            CommandFrameworkPlugin.LogInfo($"[CommandFramework] Opened Minimal Menu for '{unit.NetworkunitName}'.");
        }

        public static void Close()
        {
            if (Instance != null)
            {
                Instance._isOpen = false;
                Instance._targetUnit = null;
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            Color greenAccent = new Color(0.06f, 0.88f, 0.50f, 1.0f);
            Color darkBg = new Color(0.02f, 0.04f, 0.06f, 0.96f);

            // Textures
            _bgBorderTexture = MakeBorderedTexture(64, 64, darkBg, greenAccent, 2);
            _btnStopTexture = MakeBorderedTexture(32, 32, new Color(0.06f, 0.88f, 0.50f, 0.08f), new Color(0.06f, 0.88f, 0.50f, 0.40f), 1);
            _btnResumeTexture = MakeBorderedTexture(32, 32, new Color(1.0f, 0.65f, 0.15f, 0.12f), new Color(1.0f, 0.65f, 0.15f, 0.50f), 1);
            _btnHoverTexture = MakeBorderedTexture(32, 32, new Color(0.06f, 0.88f, 0.50f, 0.25f), greenAccent, 1);

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _bgBorderTexture },
                padding = new RectOffset(8, 8, 6, 6)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = greenAccent },
                alignment = TextAnchor.MiddleCenter
            };

            _buttonMinimalStopStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { background = _btnStopTexture, textColor = new Color(0.9f, 0.98f, 0.94f) },
                hover = { background = _btnHoverTexture, textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _buttonMinimalResumeStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { background = _btnResumeTexture, textColor = new Color(1.0f, 0.75f, 0.25f) },
                hover = { background = MakeBorderedTexture(32, 32, new Color(1.0f, 0.65f, 0.15f, 0.3f), new Color(1.0f, 0.75f, 0.25f), 1), textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _buttonCustomStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { background = _btnStopTexture, textColor = new Color(0.8f, 0.95f, 1.0f) },
                hover = { background = _btnHoverTexture, textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!_isOpen || _targetUnit == null) return;

            InitStyles();

            // Click outside closes
            if (Event.current.type == EventType.MouseDown && !_menuRect.Contains(Event.current.mousePosition))
            {
                Close();
                return;
            }

            GUI.Window(98234, _menuRect, DrawMenuContent, "", _boxStyle);
        }

        private void DrawMenuContent(int windowID)
        {
            if (_targetUnit == null) return;

            string unitName = !string.IsNullOrEmpty(_targetUnit.NetworkunitName) ? _targetUnit.NetworkunitName : _targetUnit.name;
            bool isHolding = HoldPositionManager.IsHoldingPosition(_targetUnit);

            GUILayout.BeginVertical();

            // Minimal Title
            GUILayout.Label(unitName.ToUpper(), _headerStyle);
            GUILayout.Space(4);

            // Dynamic Action Buttons
            var visibleActions = CommandFrameworkAPI.GetVisibleCommandsForUnit(_targetUnit);
            foreach (var action in visibleActions)
            {
                GUIStyle style = _buttonCustomStyle;
                if (action.Id == "core.hold_position")
                {
                    style = isHolding ? _buttonMinimalResumeStyle : _buttonMinimalStopStyle;
                }
                
                string label = action.Id == "core.hold_position" 
                    ? (isHolding ? "▶ RESUME" : "⏹ STOP") 
                    : action.GetDisplayName(_targetUnit);

                GUI.enabled = action.IsEnabled(_targetUnit);
                if (GUILayout.Button(label, style, GUILayout.Height(24)))
                {
                    CommandFrameworkAPI.ExecuteCommand(action, _targetUnit);
                }
                GUI.enabled = true;

                GUILayout.Space(3);
            }

            GUILayout.EndVertical();
        }

        private static Texture2D MakeBorderedTexture(int width, int height, Color fill, Color border, int borderWidth)
        {
            Color[] pix = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = x < borderWidth || x >= width - borderWidth || y < borderWidth || y >= height - borderWidth;
                    pix[y * width + x] = isBorder ? border : fill;
                }
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
