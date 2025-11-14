using UnityEngine;

/// <summary>
/// Script attached to the ColPlatform GameObject.
/// Handles layer and color switching when button is pressed.
/// Platform starts invisible and non-colliding (Layer_None, transparent).
/// </summary>
public class ColPlatform : MonoBehaviour
{
    void Start()
    {
        // Debug: Print all components attached to this GameObject
        Debug.Log($"[ColPlatform] ===== DEBUGGING {gameObject.name} =====");
        Component[] allComponents = GetComponents<Component>();
        Debug.Log($"[ColPlatform] Total components attached: {allComponents.Length}");
        foreach (Component comp in allComponents)
        {
            Debug.Log($"[ColPlatform] Component: {comp.GetType().Name} - {comp.name}");
        }
        
        // Set layer to Layer_None (invisible, no collisions)
        int layerNone = LayerMask.NameToLayer("Layer_None");
        if (layerNone == -1)
        {
            Debug.LogError("[ColPlatform] Layer_None not found! Make sure it exists in Unity's Layer settings.");
        }
        else
        {
            gameObject.layer = layerNone;
            Debug.Log($"[ColPlatform] Set layer to Layer_None (layer index: {layerNone})");
        }
        
        // Disable MeshRenderer to make platform invisible
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
            Debug.Log("[ColPlatform] Disabled MeshRenderer. Platform is now invisible.");
        }
        else
        {
            Debug.LogError("[ColPlatform] MeshRenderer not found! Cannot disable renderer.");
        }
        
        Debug.Log($"[ColPlatform] ===== END DEBUGGING =====");
    }
    
    /// <summary>
    /// Changes the platform's layer and color based on the player name.
    /// Called by ButtonCollider when button sinking completes.
    /// </summary>
    /// <param name="playerName">Name of the player (e.g., "John", "Claire", "Thomas")</param>
    public void ChangeLayer(string playerName)
    {
        Debug.Log($"[ColPlatform] ChangeLayer called for player: {playerName}");
        
        // Change layer to Layer_{playerName}
        string layerName = $"Layer_{playerName}";
        int layer = LayerMask.NameToLayer(layerName);
        
        if (layer == -1)
        {
            Debug.LogError($"[ColPlatform] Layer '{layerName}' not found! Make sure it exists in Unity's Layer settings.");
            return;
        }
        
        gameObject.layer = layer;
        Debug.Log($"[ColPlatform] Changed layer to {layerName} (layer index: {layer})");
        
        // Find Player by name to get their color
        GameObject playerObject = GameObject.Find(playerName);
        if (playerObject == null)
        {
            Debug.LogError($"[ColPlatform] Could not find Player GameObject named '{playerName}'!");
            return;
        }
        
        MeshRenderer playerRenderer = playerObject.GetComponent<MeshRenderer>();
        if (playerRenderer == null || playerRenderer.material == null)
        {
            Debug.LogError($"[ColPlatform] Player '{playerName}' has no MeshRenderer or material! Cannot get color.");
            return;
        }
        
        // Get player's color
        Color playerColor = playerRenderer.material.color;
        Debug.Log($"[ColPlatform] Found player color: RGBA({playerColor.r}, {playerColor.g}, {playerColor.b}, {playerColor.a})");
        
        // Apply color to platform and enable renderer
        MeshRenderer platformRenderer = GetComponent<MeshRenderer>();
        if (platformRenderer != null && platformRenderer.material != null)
        {
            platformRenderer.material.color = playerColor;
            platformRenderer.enabled = true; // Make platform visible
            Debug.Log($"[ColPlatform] Changed platform color to match player '{playerName}' and enabled renderer.");
        }
        else
        {
            Debug.LogError("[ColPlatform] Platform has no MeshRenderer or material! Cannot set color.");
        }
    }
}

