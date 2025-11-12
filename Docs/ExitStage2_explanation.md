# ExitStage2 Script Explanation

## Overview
`ExitStage2` is a MonoBehaviour script that detects when all players reach the final platform in Stage 2 and automatically loads the next scene.

## How It Works

### 1. HashSet for Tracking Players

```csharp
private HashSet<PlayerScene2> playersInside = new HashSet<PlayerScene2>();
```

**What is a HashSet?**
- A collection that stores unique items (no duplicates)
- Fast to add, remove, and check if something exists
- We use it to track which players are currently inside the trigger

**Why use HashSet instead of a List?**
- If a player somehow enters twice, it only counts once (no duplicates)
- Easy to check "are all players inside?" by comparing the count
- More efficient for checking membership

### 2. OnTriggerEnter - When a Player Enters

```csharp
void OnTriggerEnter(Collider other)
{
    PlayerScene2 player = other.GetComponent<PlayerScene2>();
    if (player != null)
    {
        playersInside.Add(player);
        Debug.Log($"{player.name} entered the final platform");
    }
}
```

**What happens step by step:**
1. Unity automatically calls this method when a collider enters the trigger zone
2. `other` is the collider that entered (could be a player, could be something else)
3. `other.GetComponent<PlayerScene2>()` checks if that collider belongs to a PlayerScene2 component
4. If it's a PlayerScene2, we add it to our `playersInside` HashSet
5. Log a message for debugging

**Important:** This is a Unity API method - Unity calls it automatically, you don't call it yourself.

### 3. OnTriggerStay - While a Player Is Inside

```csharp
void OnTriggerStay(Collider other)
{
    // Keep checking - player is still inside
    // We'll add completion check here later
}
```

**What happens:**
- Unity calls this method **every frame** while a collider is inside the trigger
- This is where we'll add the logic to check if all players are present
- Currently empty, but this is where the "stage complete" check will go

**Why check every frame?**
- Players might enter/leave at different times
- We need to continuously verify all players are still inside
- When all are inside, trigger the scene transition

### 4. OnTriggerExit - When a Player Leaves

```csharp
void OnTriggerExit(Collider other)
{
    PlayerScene2 player = other.GetComponent<PlayerScene2>();
    if (player != null)
    {
        playersInside.Remove(player);
        Debug.Log($"{player.name} left the final platform");
    }
}
```

**What happens step by step:**
1. Unity automatically calls this when a collider leaves the trigger zone
2. Check if it's a PlayerScene2 component
3. If yes, remove it from the `playersInside` HashSet
4. Log a message for debugging

**Why remove them?**
- If a player leaves, they're no longer on the platform
- We need to know exactly who is inside at any moment
- The stage can only complete when ALL players are inside

## The Complete Flow

1. **Player enters platform** → `OnTriggerEnter` called → Player added to HashSet → Logged
2. **Player stays on platform** → `OnTriggerStay` called every frame → (Will check if all players are here)
3. **Player leaves platform** → `OnTriggerExit` called → Player removed from HashSet → Logged

## Setup Requirements

1. **Script attached** to the final platform GameObject
2. **Box Collider** component (or other collider) with `Is Trigger = true`
3. **Collider sized** to cover the platform area where players need to stand
4. **Players must have** a `PlayerScene2` component attached

## Next Steps

The script currently tracks players entering/leaving. The next piece will be:
- Finding all PlayerScene2 instances in the scene
- Comparing `playersInside.Count` with total players
- When all are inside, call the scene loading function

