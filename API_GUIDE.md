# Command Framework API Reference

Developer guide for creating mods, custom UIs, and commands using the Command Framework SDK.

---

## 1. HTML & CSS UI Engine (`UIDocument` & `UICompiler`)

Command Framework compiles HTML and CSS templates directly into native C# UI classes.

### Workflow
1. Create `your_template.ui.html` and `your_template.ui.css` in your mod folder.
2. Run `build.ps1` to generate `UI_YourTemplate.g.cs`.
3. In `OnGUI()`, render the document:
```csharp
using CommandFramework.UI.Generated;

void OnGUI()
{
    var doc = UI_TacticalMenu.GetDocument();
    doc.BindingContext = new UIBindingContext(selectedUnit);
    doc.Render(screenRect, Event.current.mousePosition);
}
```

### Supported HTML Elements & Attributes
- `<div class="..." id="...">` - Flexbox container.
- `<button icon="core.stop" @click="core.hold_position">LABEL</button>` - Action button.
- `<header>`, `<span>`, `<badge>`, `<divider>` - Semantic elements.
- `{{unit.name}}`, `{{init.status}}` - Variable bindings.

### Supported CSS Properties
- **Box Model:** `width`, `height`, `min-width`, `max-width`, `padding`, `margin`, `gap`.
- **Flexbox:** `display: flex`, `flex-direction: row | column`, `justify-content`, `align-items`.
- **Styling:** `background`, `background-color`, `border`, `border-color`, `border-width`, `border-radius`.
- **Typography:** `color`, `font-size`, `font-weight: bold`, `text-align: center | left | right`.
- **States:** `:hover`, `:active`, `:disabled`.

---

## 2. Pixel-Perfect Snapping & HiDPI (`UIScaleManager`)

```csharp
// 1. Grid Snapping
float snappedX = UIScaleManager.Snap(rawFloat);
Rect crispRect = UIScaleManager.Snap(new Rect(x, y, w, h));

// 2. Resolution Scaling (relative to 1080p)
float scaledWidth = UIScaleManager.ScaleAndSnap(210f);
int scaledFontSize = UIScaleManager.ScaleFontSize(11);

// 3. Settings
CommandFrameworkSettings.UIScaleMultiplier = 1.0f;
CommandFrameworkSettings.EnableAutoHiDPI = true;
CommandFrameworkSettings.EnablePixelSnapping = true;
```

---

## 3. Command Builder (`CommandBuilder`)

```csharp
using CommandFramework.API;

CommandFrameworkAPI.CreateCommand("mymod.repair")
    .WithName("REPAIR VEHICLE")
    .WithIcon(CommandIconLibrary.IconShield)
    .ForUnits(u => u is GroundVehicle)
    .When(u => !u.disabled)
    .OnExecute(u => StartRepair(u))
    .Register();
```

---

## 4. Icon Library (`CommandIconLibrary`)

```csharp
// Built-in 64x64 vector icons
Texture2D stop = CommandIconLibrary.IconStop;
Texture2D resume = CommandIconLibrary.IconResume;
Texture2D loop = CommandIconLibrary.IconLoop;
Texture2D form = CommandIconLibrary.IconFormation;
Texture2D fire = CommandIconLibrary.IconHoldFire;
Texture2D shield = CommandIconLibrary.IconShield;
Texture2D radar = CommandIconLibrary.IconRadar;

// Load external PNG
Texture2D custom = CommandIconLibrary.LoadFromFile("path/to/icon.png");
```

---

## 5. Game Utilities (`GameAPI`)

```csharp
using CommandFramework.API;

// Send movement order
CommandFrameworkAPI.Game.SetDestination(unit, targetPosition);

// Stop / Hold
CommandFrameworkAPI.Game.SetHoldPosition(unit, true);

// Map cursor to world position
Vector3 worldPos = CommandFrameworkAPI.Game.GetMapCursorWorldPosition(dynamicMap);

// Find unit under cursor
Unit hoveredUnit = CommandFrameworkAPI.Game.FindUnitUnderMouse(dynamicMap, mousePos, thresholdRadius: 24f);

// Check if friendly
bool isFriend = CommandFrameworkAPI.Game.IsFriendly(unitA, unitB);
```

---

## 6. Theming & Custom Renderers (`IMenuRenderer`)

```csharp
// Switch themes
CommandFrameworkAPI.UI.SetTheme("CyberCyan"); // TacticalGreen, CyberCyan, AmberAlert

// Use a custom context menu renderer
CommandFrameworkAPI.MenuRenderer = new MyCustomMenuRenderer();
```
