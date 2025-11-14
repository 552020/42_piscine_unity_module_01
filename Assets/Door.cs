using UnityEngine;

/// <summary>
/// Script attached to the moving part of each door (e.g., DoorJohn->Door).
/// Handles opening when the matching button is pressed.
/// </summary>
public class Door : MonoBehaviour
{
    private bool isOpen = false;
    private float originalY;
    private float targetY;
    
    void Start()
    {
        // Store original Y position (closed position)
        originalY = transform.position.y;
        
        // Calculate target Y: originalY + (2 * tallest player's height)
        float tallestPlayerHeight = GetTallestPlayerHeight();
        targetY = originalY + (2f * tallestPlayerHeight);
        
        Debug.Log($"[Door] {gameObject.name}: Original Y = {originalY}, Target Y = {targetY} (Tallest player height: {tallestPlayerHeight})");
    }
    
    /// <summary>
    /// Gets the height of the tallest player by finding all Player objects,
    /// comparing their MeshRenderer bounds, and returning the maximum height.
    /// </summary>
    private float GetTallestPlayerHeight()
    {
        // Find all Player objects in the scene
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        
        if (players == null || players.Length == 0)
        {
            Debug.LogError("[Door] Could not find any Player objects! Using default height of 1.5");
            return 1.5f; // Default fallback
        }
        
        float maxHeight = 0f;
        string tallestPlayerName = "";
        
        // Compare heights of all players
        foreach (Player player in players)
        {
            MeshRenderer playerRenderer = player.GetComponent<MeshRenderer>();
            if (playerRenderer == null)
            {
                Debug.LogWarning($"[Door] Player {player.name} has no MeshRenderer! Skipping.");
                continue;
            }
            
            float height = playerRenderer.bounds.size.y;
            if (height > maxHeight)
            {
                maxHeight = height;
                tallestPlayerName = player.name;
            }
        }
        
        if (maxHeight <= 0f)
        {
            Debug.LogError("[Door] Could not determine any player height! Using default height of 1.5");
            return 1.5f; // Default fallback
        }
        
        Debug.Log($"[Door] Found tallest player: {tallestPlayerName} with height: {maxHeight}");
        return maxHeight;
    }
    
    /// <summary>
    /// Opens the door by moving it instantly to the target Y position.
    /// If already open, does nothing.
    /// </summary>
    public void Open()
    {
        if (isOpen)
        {
            Debug.Log($"[Door] {gameObject.name}: Already open, ignoring Open() call");
            return;
        }
        
        // Move door instantly to target position
        transform.position = new Vector3(
            transform.position.x,
            targetY,
            transform.position.z
        );
        
        isOpen = true;
        Debug.Log($"[Door] {gameObject.name}: Door opened! Moved to Y: {targetY}");
    }
}

