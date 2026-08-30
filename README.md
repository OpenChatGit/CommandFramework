# Command Framework for Nuclear Option

A modular unit commanding framework and in-game tactical context menu for Nuclear Option (BepInEx 5).

---

## Features

- Tactical Context Menu: Right-click on any unit (on the map or in 3D world view) to open a minimal, green-outlined command popup.
- Stop / Hold Position: Sets `isHoldPosition = true` to halt unit movement while keeping weapon turrets, radar, and combat defense active.
- Resume with Automatic AI Nudge: Resuming a stopped unit (`isHoldPosition = false`) wakes up pathfinding and AI routines to resume autonomous movement, mission waypoints, or road network patrols.
- Selection Protection: Right-clicking on other units opens their menu without issuing move or attack orders to previously selected units.
- Empty Space Left-Click Deselection: Left-clicking on any empty space (terrain, sky, or map) cleanly deselects active target locks or selected units.
- Developer API: Register custom unit commands, attach custom order states and metadata to units, and filter AI destination changes from other mods.

---

## Installation

1. Install BepInEx 5 into your Nuclear Option game directory.
2. Copy the `CommandFramework` folder into `Nuclear Option/BepInEx/plugins/`.
3. Start the game.

---

## Developer API Guide

### 1. Adding a Project Reference

Add a reference to `CommandFramework.dll` in your `.csproj`:

```xml
<ItemGroup>
  <Reference Include="CommandFramework">
    <HintPath>path\to\BepInEx\plugins\CommandFramework\CommandFramework.dll</HintPath>
    <Private>False</Private>
  </Reference>
</ItemGroup>
```

Declare the BepInDependency attribute in your plugin class:

```csharp
[BepInDependency("com.nuclearoption.commandframework")]
public class MyPlugin : BaseUnityPlugin
{
    // ...
}
```

---

### 2. Registering a Custom Command

Implement `IUnitCommandAction` and register it using `CommandFrameworkAPI.RegisterCommand`:

```csharp
using CommandFramework.API;
using UnityEngine;

public class GuardAreaCommand : IUnitCommandAction
{
    public string Id => "com.example.guard_area";
    public int Priority => 30;

    public string GetDisplayName(Unit unit)
    {
        var state = CommandFrameworkAPI.GetOrderState(unit);
        return state.CustomStateKey == "Guarding" ? "STOP GUARDING" : "GUARD AREA";
    }

    public bool IsVisible(Unit unit)
    {
        return unit is GroundVehicle || unit is Ship;
    }

    public bool IsEnabled(Unit unit) => unit != null && !unit.disabled;

    public Color? GetButtonColor(Unit unit)
    {
        var state = CommandFrameworkAPI.GetOrderState(unit);
        return state.CustomStateKey == "Guarding" 
            ? new Color(0.8f, 0.3f, 0.2f) 
            : new Color(0.1f, 0.6f, 0.9f);
    }

    public void Execute(Unit unit)
    {
        var state = CommandFrameworkAPI.GetOrderState(unit);
        if (state.CustomStateKey == "Guarding")
        {
            CommandFrameworkAPI.ClearCustomOrderState(unit);
            CommandFrameworkAPI.NudgeAI(unit);
        }
        else
        {
            CommandFrameworkAPI.SetCustomOrderState(unit, "Guarding", "GUARDING AREA", Color.cyan);
            state.SetData("GuardCenter", unit.transform.position);
            state.SetData("GuardRadius", 2000f);
        }
    }
}

// In your plugin Awake():
CommandFrameworkAPI.RegisterCommand(new GuardAreaCommand());
```

---

### 3. Custom Order States and Metadata Storage

Attach custom states, tags, or metadata to units:

```csharp
// Get unit state
UnitOrderState state = CommandFrameworkAPI.GetOrderState(unit);

// Store arbitrary data on the unit
state.SetData("PatrolRouteIndex", 3);
state.SetData("CustomTargetHQ", "Alpha_Base");

// Retrieve stored data
int routeIndex = state.GetData<int>("PatrolRouteIndex");

// Set a custom state badge
CommandFrameworkAPI.SetCustomOrderState(unit, "Patrolling", "PATROL ROUTE 3", Color.green);

// Clear custom state
CommandFrameworkAPI.ClearCustomOrderState(unit);
```

---

### 4. Order and Destination Interception Filters

Intercept and block automated destination changes when a custom state is active:

```csharp
// Register an order filter
CommandFrameworkAPI.RegisterOrderFilter((unit, targetPosition) =>
{
    var state = CommandFrameworkAPI.GetOrderState(unit);
    
    // Block destination updates if unit is in custom guard mode
    if (state.CustomStateKey == "Guarding")
    {
        return false;
    }
    
    return true;
});
```

---

### 5. Triggering AI Nudge

Force an immediate AI pathfinding recalculation and mission resume:

```csharp
CommandFrameworkAPI.NudgeAI(unit);
```

---

### 6. Events

Subscribe to state and command lifecycle events:

```csharp
CommandFrameworkAPI.OnCommandExecuted += (unit, action) =>
{
    Debug.Log($"Command {action.Id} executed on {unit.NetworkunitName}");
};

CommandFrameworkAPI.OnHoldPositionChanged += (unit, isHolding) =>
{
    Debug.Log($"Unit {unit.NetworkunitName} hold position: {isHolding}");
};

CommandFrameworkAPI.OnCustomOrderStateChanged += (unit, stateKey) =>
{
    Debug.Log($"Unit {unit.NetworkunitName} state changed to: {stateKey}");
};
```

---

## License

MIT License
