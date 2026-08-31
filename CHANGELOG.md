# Changelog

All notable changes to Command Framework are documented in this file.

---

## [0.7.0] - 2026-08-31

### Added
- **Tactical Map & Overlays API (`CommandFrameworkAPI.Map` & `TacticalMapAPI`):** Real-time GPU-accelerated drawing engine, coordinate transforms (`WorldToScreen`, `ScreenToWorld`), world-anchored threat circles/SAM rings, and freehand drawing mode with Undo/Redo/Clear support.
- **Mathematical Viewport Clipping:** Real-time Liang-Barsky parametric line segment clipping against the tactical map viewport.
- **Vector Icons:** Added `IconPen` (`core.pen`, `core.draw`, `core.edit`) to `CommandIconLibrary`.
- **Anti-Aliased Procedural Rounded Rectangles:** SDF rasterization in `UIBuilder.GetRoundedBoxTexture` supporting smooth border radiuses and translucent glass styling.

### Fixed
- **Multi-Pass Flexbox Row Engine (`UINode.cs`):** Fixed element overflow across container boundaries in `space-between` and `row` layouts.
- **GPU Batch Rendering:** Replaced per-segment IMGUI draw calls with hardware-accelerated `GL.QUADS` batching, maintaining solid 144+ FPS during drawing.
- **Drawing Mode Map Controls:** Isolated left-click for drawing and mapped right-click dragging to map panning without triggering base game move orders.

## [0.6.0] - 2026-08-31

### Added
- **HTML & CSS UI Engine (`UIDocument` & `UICompiler`):** Support for designing UI layouts in `.ui.html` and `.ui.css` templates with automated transpilation into C# classes during build.
- **HiDPI Scaling & Pixel Snapping (`UIScaleManager`):** Resolution scaling relative to 1080p and integer pixel snapping for borders, rectangles, and fonts.
- **Game Utilities (`GameAPI`):** Helper functions for unit movement, hold states, map coordinates, and faction checks.
- **Startup Init Banner:** Configurable startup notification banner positioned at the bottom center of the screen.
- **Fluent Command Builder:** `CommandFrameworkAPI.CreateCommand(id)` for registering unit actions.
- **Icon Library:** 64x64 vector icons and PNG file loader.
- **Theming:** `UIThemeManager` with runtime theme switching (`TacticalGreen`, `CyberCyan`, `AmberAlert`).

### Changed
- Streamlined framework architecture into a pure UI engine and modding SDK.
- Decoupled context menu rendering via `IMenuRenderer`.

### Removed
- Removed hardcoded default gameplay commands, formation update loops, and marquee box selection to keep the framework un-opinionated.
