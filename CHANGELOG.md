# Changelog

## [0.3.1] - 2026-08-30

### Added
- Tactical Context Menu: Right-click on any unit (in 3D view or on the tactical map) to open a minimal, neon-green outlined command popup.
- Stop / Hold Position: Orders ground vehicles and ships to halt movement while keeping weapons, turrets, and radar active.
- Autonomous Resume & AI Nudge: Resuming a stopped unit re-activates autonomous pathfinding, saved mission waypoints, or road network patrols.
- Selection Isolation: Suppresses accidental yellow waypoint/move orders to selected units when right-clicking on other units.
- Empty Space Deselection: Left-clicking on empty map or terrain space deselects targets and units.
- Developer API: Complete `IUnitCommandAction`, `UnitOrderState`, and `CommandFrameworkAPI` for third-party modders to add custom unit commands and order interceptors.
- Full NOMM (Nuclear Option Mod Manager) and Thunderstore package compatibility.
