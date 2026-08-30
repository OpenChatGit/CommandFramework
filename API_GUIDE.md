# Command Framework API Guide

The Command Framework provides an API for mod developers to register custom commands (`IUnitCommandAction`), custom unit order states (`UnitOrderState` / `customOrderState`), and destination filters (`OrderFilters`) in Nuclear Option.

---

## 1. Creating and Registering a Custom Command

Implement `IUnitCommandAction` and register the action via `CommandFrameworkAPI.RegisterCommand`:

```csharp
using CommandFramework.API;
using UnityEngine;

public class GuardAreaCommandAction : IUnitCommandAction
{
    public string Id => "com.example.guard_area";
    public int Priority => 50; // Order priority in context menu

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
            : new Color(0.2f, 0.6f, 0.9f);
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
            state.SetData("GuardRadius", 1500f);
        }
    }
}

// In your plugin Awake():
CommandFrameworkAPI.RegisterCommand(new GuardAreaCommandAction());
```

---

## 2. Using UnitOrderState and Custom Data

Every unit has a `UnitOrderState` instance:

```csharp
// Retrieve order state for a unit
var state = CommandFrameworkAPI.GetOrderState(unit);

// Store and retrieve custom typed data
state.SetData("TargetZone", "Airbase_Alpha");
string zone = state.GetData<string>("TargetZone");

// Set custom state
CommandFrameworkAPI.SetCustomOrderState(unit, "PatrolRoute", "ON PATROL", Color.green);

// Clear custom state
CommandFrameworkAPI.ClearCustomOrderState(unit);
```

---

## 3. Registering Order and Mission Filters

Prevent the AI from assigning conflicting mission waypoints or destination changes while your custom order is active:

```csharp
CommandFrameworkAPI.RegisterOrderFilter((unit, targetPos) =>
{
    var state = CommandFrameworkAPI.GetOrderState(unit);
    if (state.CustomStateKey == "Guarding")
    {
        // Block automated AI waypoint overrides
        return false;
    }
    return true;
});
```

---

## 4. Triggering AI Nudge

```csharp
// Recalculates pathfinding and immediately resumes active missions or road network patrols
CommandFrameworkAPI.NudgeAI(unit);
```

---

## 5. Subscribing to Events

```csharp
CommandFrameworkAPI.OnCommandExecuted += (unit, action) =>
{
    Debug.Log($"Command {action.Id} executed on unit {unit.NetworkunitName}!");
};

CommandFrameworkAPI.OnHoldPositionChanged += (unit, isHolding) =>
{
    Debug.Log($"Unit {unit.NetworkunitName} HoldPosition: {isHolding}");
};

CommandFrameworkAPI.OnCustomOrderStateChanged += (unit, stateKey) =>
{
    Debug.Log($"Unit {unit.NetworkunitName} new state: {stateKey}");
};
```
