# Changelog

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
