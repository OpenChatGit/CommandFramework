# Changelog

## [0.4.8] - 2026-08-30

### Added
- "FOLLOW NAV" Tactical Status UI: Units following a waypoint queue now dynamically display `[ FOLLOW NAV ]` in vibrant tactical green on map tooltips, unit selection text, and inside the Command Context Menu (instead of generic navigation text).

## [0.4.7] - 2026-08-30

### Fixed
- Immediate Empty-Space Map Deselection: Moved left-click empty space deselection directly into `CommandFrameworkPlugin.Update()`, ensuring that map unit and waypoint deselection is active immediately upon game start without requiring the context menu to be opened first.

## [0.4.6] - 2026-08-30

### Fixed
- Instant Live Waypoint Chaining: Intercepted MapControls Postfix so that chained multi-waypoints are instantly drawn in perfect green sequential order on the exact frame of the click without needing to deselect and re-select the unit.

## [0.4.5] - 2026-08-30

### Fixed
- Chained Multi-Waypoint Vectors: Fixed Shift-Click waypoint vectors originating from the NPC by computing exact local canvas offsets. Subsequent waypoints connect directly from the preceding waypoint marker ($WP_{i-1} \rightarrow WP_i$).

## [0.4.4] - 2026-08-30

### Security & Multiplayer Privacy
- Faction Isolation: Enforced strict friendly/spectator faction filtering. Hostile/enemy NPC waypoints and trajectory paths are never displayed on the tactical map or accessible via context commands in Multiplayer.

## [0.4.3] - 2026-08-30

### Fixed
- Multi-Waypoint Sequential Progression: Intercepted base game destination overrides so units stay locked onto their first waypoint when queuing additional waypoints with Shift+Click.
- Waypoint Line Chaining: Ensured chained waypoints properly connect each marker sequentially from the preceding waypoint without resetting origin back to the NPC.

## [0.4.2] - 2026-08-30

### Added
- Sequential Multi-Waypoint Queues (Shift + Click): Shift-clicking on the tactical map now creates a true multi-waypoint queue. Units navigate sequentially from waypoint to waypoint until reaching the final destination.
- Accurate Terrain-to-Screen Map Coordinate Projection: Waypoints are precisely projected to map screen coordinates using map dimensions and scale, preventing waypoint drift or markers going out of the map bounds during zooming and panning.

## [0.4.1] - 2026-08-30

### Added
- Persistent Waypoint Display on Reselection: When deselecting and re-selecting a unit with an active commanded destination, its green waypoint marker and trajectory vector line are automatically restored and displayed on the tactical map.
- Real-Time Waypoint Tracking: The waypoint trajectory line dynamically follows moving units across the map in real time.

## [0.4.0] - 2026-08-30

### Added
- Green Tactical Waypoints: All map waypoints and trajectory vector lines are now rendered in vibrant Tactical Green (`#0FE078`) instead of the default yellow.
- Robust AI Waypoint Following: Protected player-commanded destinations for Ground Vehicles and Ships against being dropped or overridden by background mission triggers, artillery search AI, or ambient patrols until arrival.
- Ground Vehicle Un-Stuck & Nav Smoothing: Automatically releases hold position, clears anchoring, resets stationary flags, and smooths pathfinding upon receiving a move order.

## [0.3.2] - 2026-08-30

### Fixed
- Left-click deselection is now strictly restricted to the maximized tactical map. Left-clicking during flight / 3D cockpit view no longer clears CombatHUD aircraft weapon locks or missile targets.

## [0.3.1] - 2026-08-30

### Added
- Tactical Context Menu: Right-click on any unit (in 3D view or on the tactical map) to open a minimal, neon-green outlined command popup.
- Stop / Hold Position: Orders ground vehicles and ships to halt movement while keeping weapons, turrets, and radar active.
- Autonomous Resume & AI Nudge: Resuming a stopped unit re-activates autonomous pathfinding, saved mission waypoints, or road network patrols.
- Selection Isolation: Suppresses accidental yellow waypoint/move orders to selected units when right-clicking on other units.
- Empty Space Deselection: Left-clicking on empty map or terrain space deselects targets and units.
- Developer API: Complete `IUnitCommandAction`, `UnitOrderState`, and `CommandFrameworkAPI` for third-party modders to add custom unit commands and order interceptors.
- Full NOMM (Nuclear Option Mod Manager) and Thunderstore package compatibility.
