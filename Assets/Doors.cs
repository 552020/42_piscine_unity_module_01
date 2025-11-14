using UnityEngine;

/// <summary>
/// Script attached to the "Doors" GameObject that manages all 3 doors.
/// Auto-finds doors in children (Doors->DoorJohn->Door, etc.) and opens the matching door
/// based on player name.
/// </summary>
public class Doors : MonoBehaviour
{
    
    /// <summary>
    /// Helper function to find a door by player name.
    /// Looks for Doors -> Door{playerName} -> Door (with Door.cs script)
    /// </summary>
    /// <param name="playerName">Name of the player (e.g., "John", "Claire", "Thomas")</param>
    /// <returns>Door component if found, null otherwise</returns>
    private Door FindDoor(string playerName)
    {
        string doorParentName = $"Door{playerName}";
        Transform doorParentTransform = transform.Find(doorParentName);
        
        if (doorParentTransform == null)
        {
            Debug.LogError($"[Doors] Could not find {doorParentName} child!");
            return null;
        }
        
        Transform doorTransform = doorParentTransform.Find("Door");
        if (doorTransform == null)
        {
            Debug.LogError($"[Doors] {doorParentName} found but has no 'Door' child!");
            return null;
        }
        
        Door door = doorTransform.GetComponent<Door>();
        if (door == null)
        {
            Debug.LogError($"[Doors] {doorParentName}->Door GameObject found but has no Door.cs script!");
            return null;
        }
        
        Debug.Log($"[Doors] Found {doorParentName}");
        return door;
    }
    
    /// <summary>
    /// Opens the door that matches the given player name.
    /// Called by ButtonCollider when button sinking completes.
    /// </summary>
    /// <param name="playerName">Name of the player (e.g., "John", "Claire", "Thomas")</param>
    public void OpenDoor(string playerName)
    {
        Debug.Log($"[Doors] OpenDoor called for player: {playerName}");
        
        // Find door by player name
        Door matchingDoor = FindDoor(playerName);
        
        if (matchingDoor != null)
        {
            Debug.Log($"[Doors] Opening door for {playerName}");
            matchingDoor.Open();
        }
        else
        {
            Debug.LogWarning($"[Doors] Could not find door for player: {playerName}");
        }
    }
}

