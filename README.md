# Command Framework

Modding SDK and UI engine for Nuclear Option (BepInEx 5).

Command Framework provides an HTML/CSS UI engine, HiDPI scaling, a command registration pipeline, and game utility functions for Nuclear Option mods.

---

## Features

- **HTML & CSS UI Engine:** Write UI layouts using HTML and CSS with flexbox, margins, padding, colors, and `:hover` states. Build scripts transpile templates into C# classes automatically.
- **HiDPI & Pixel-Perfect Rendering:** Automatic scaling for 1080p, 1440p, and 4K with integer pixel snapping to prevent blurry borders and text.
- **Command API:** Fluent builder to register unit actions in the tactical context menu.
- **Pluggable Menu Renderer:** Default context menu can be customized via themes (`UIThemeManager`) or replaced entirely with a custom `IMenuRenderer`.
- **Game Utilities (`GameAPI`):** Helper functions for unit orders, hold states, map coordinates, and faction checks.
- **HD Icon Library:** Procedural vector icons and external PNG texture loader.

---

## Installation

1. Install BepInEx 5 into Nuclear Option.
2. Copy the `CommandFramework` folder into `Nuclear Option/BepInEx/plugins/`.
3. Start the game.

---

## Quickstart

### 1. UI with HTML & CSS

Create `my_menu.ui.html`:
```html
<div class="menu-box">
    <div class="menu-title">{{unit.name}}</div>
    <button class="menu-btn" icon="core.stop" @click="mymod.stop">STOP</button>
</div>
```

Create `my_menu.ui.css`:
```css
.menu-box {
    width: 200px;
    padding: 8px;
    background: rgba(10, 15, 20, 0.95);
    border: 1px solid #ffffff;
    display: flex;
    flex-direction: column;
    gap: 4px;
}
.menu-title {
    color: #0fe078;
    font-size: 11px;
    font-weight: bold;
    text-align: center;
}
.menu-btn {
    height: 24px;
    color: #ffffff;
    background: rgba(15, 224, 120, 0.15);
    border: 1px solid rgba(15, 224, 120, 0.4);
}
.menu-btn:hover {
    background: rgba(15, 224, 120, 0.35);
}
```

Run `build.ps1` to compile templates into `UI_MyMenu.g.cs`, then render in C#:
```csharp
var doc = UI_MyMenu.GetDocument();
doc.BindingContext = new UIBindingContext(unit);
doc.Render(screenRect, Event.current.mousePosition);
```

---

### 2. Registering a Custom Command

```csharp
using CommandFramework.API;

CommandFrameworkAPI.CreateCommand("mymod.repair")
    .WithName("REPAIR VEHICLE")
    .WithIcon(CommandIconLibrary.IconShield)
    .ForUnits(u => u is GroundVehicle)
    .OnExecute(u => StartRepair(u))
    .Register();
```

---

### 3. Game Utilities (`GameAPI`)

```csharp
using CommandFramework.API;

// Send move order
CommandFrameworkAPI.Game.SetDestination(unit, targetPosition);

// Stop unit
CommandFrameworkAPI.Game.SetHoldPosition(unit, true);

// Map cursor to world position
Vector3 worldPos = CommandFrameworkAPI.Game.GetMapCursorWorldPosition(dynamicMap);

// Find unit under cursor on tactical map
Unit hoveredUnit = CommandFrameworkAPI.Game.FindUnitUnderMouse(dynamicMap, mousePos);
```

---

## License

MIT License.
