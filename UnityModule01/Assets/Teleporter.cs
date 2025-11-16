using UnityEngine;
using TMPro;

/// <summary>
/// Teleporter script that creates a teleportation system between two linked teleporter frames.
/// When a player enters the entrance teleporter, they are instantly teleported to the exit teleporter.
/// 
/// The teleporter uses a LineRenderer to draw a visible frame and a BoxCollider trigger to detect player entry.
/// Unlike ExitFrame, this uses a fixed size (set in the Inspector) rather than auto-sizing to match players.
/// 
/// ORIENTATION: The teleporter frame faces the player (like ExitFrame). For a side-scrolling game where players
/// move horizontally (X-axis), the frame opening is horizontal (X-axis direction). Players align themselves
/// horizontally with the frame to "enter" it - they don't pass through it like a door, but position themselves
/// within the frame's trigger area.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(LineRenderer))]
public class Teleporter : MonoBehaviour
{
    [Header("Teleporter Settings")]
    /// <summary>
    /// Size of the Teleporter frame (width, height).
    /// - width (x): Horizontal opening size (X-axis) - how wide the frame is
    /// - height (y): Vertical size (Y-axis) - how tall the frame is
    /// This is editable in the Unity Inspector and is used to:
    /// - Set the BoxCollider trigger volume size
    /// - Draw the LineRenderer frame at the correct dimensions
    /// - Position the collider center correctly
    /// Unlike ExitFrame, this is a fixed size and doesn't auto-adjust to player dimensions.
    /// The frame faces the player, so players align horizontally (X-axis) to enter it.
    /// </summary>
    public Vector2 teleporterSize = new Vector2(2f, 3f);  // width (X-axis), height (Y-axis) - fixed size

    /// <summary>
    /// Width/thickness of the LineRenderer lines that draw the teleporter frame outline.
    /// This controls how thick the frame border appears. Default is 0.05 (thin lines).
    /// Increase for thicker, more visible frame lines.
    /// </summary>
    public float lineWidth = 0.05f;

    /// <summary>
    /// Depth (Z-axis) of the BoxCollider trigger volume.
    /// For side-scrolling games with horizontal movement (X-axis), players might be slightly offset in Z.
    /// Default is 50 (large enough to catch players even if they're not exactly at the teleporter's Z position).
    /// This ensures reliable triggering even if player and teleporter Z positions don't match exactly.
    /// Can be set even larger (100, 1000) if needed, or decreased for more precise triggering.
    /// </summary>
    public float triggerDepth = 50f;

    /// <summary>
    /// Color of the teleporter frame outline drawn by the LineRenderer.
    /// This is a fixed color (unlike ExitFrame which matches player colors).
    /// Can be set to a distinctive color like cyan, blue, or purple to make teleporters easily recognizable.
    /// </summary>
    public Color color = Color.cyan;

    [Header("Teleporter Configuration")]
    public bool isEntrance = true;  // true = entrance, false = exit
    public Teleporter linkedTeleporter;  // The paired teleporter (entrance links to exit, exit links to entrance)

    [Header("Signal Text Settings")]
    /// <summary>
    /// Text to display on the signal (child GameObject with TextMeshPro component).
    /// If left empty, will auto-set based on isEntrance: "ENTRY" for entrance, "EXIT" for exit.
    /// Can be manually set to custom text like "TELEPORT", "↑", etc.
    /// </summary>
    public string signalText = "";  // Empty = auto-set based on isEntrance

    /// <summary>
    /// Reference to the TextMeshPro component on a child GameObject (the signal/marker).
    /// If not assigned, will automatically find it in children on Start().
    /// </summary>
    [SerializeField] private TextMeshPro signalTextMesh;

    [Header("Optional Settings")]
    public bool autoAlignWithGround = true;

    // Component references
    private BoxCollider box;
    private LineRenderer lr;

    void OnEnable()
    {
        // Setup when component is enabled (in editor or at runtime)
        // Note: Alignment handled by OnValidate() in editor, Start() at runtime
        Apply();  // Configure BoxCollider and LineRenderer
    }

    void OnValidate()
    {
        // Updates live in the editor when values change in Inspector
        if (!Application.isPlaying && autoAlignWithGround)
        {
            AlignWithGround();
        }
        Apply();  // Update BoxCollider and LineRenderer when settings change
        ConfigureSignalText();  // Update signal text when settings change
    }

    void Start()
    {
        // Initialization when game starts (at runtime)
        if (autoAlignWithGround)
        {
            AlignWithGround();
        }
        Apply();  // Ensure BoxCollider and LineRenderer are configured correctly
        ConfigureSignalText();  // Set up the signal text

        // Optional: Validate that entrance has a linked exit
        if (isEntrance && linkedTeleporter == null)
        {
            Debug.LogWarning($"Teleporter '{gameObject.name}' is set as Entrance but has no linked Teleporter assigned!");
        }
    }

    void AlignWithGround()
    {
        // COMMENTED OUT - positioning logic disabled
        /*
        // Find all GameObjects and look for ones whose names include "Ground", "Plane", or "Floor"
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        GameObject ground = null;
        float highestY = float.NegativeInfinity;
        float teleporterY = transform.position.y;
        
        foreach (GameObject obj in allObjects)
        {
            string objName = obj.name.ToLower();
            
            // Check if name includes "ground", "plane", or "floor"
            if (objName.Contains("ground") || objName.Contains("plane") || objName.Contains("floor"))
            {
                // Get the top surface of this object
                Renderer objRenderer = obj.GetComponent<Renderer>();
                Collider objCollider = obj.GetComponent<Collider>();
                
                float objTop = 0f;
                
                if (objRenderer != null)
                {
                    objTop = objRenderer.bounds.max.y;
                }
                else if (objCollider != null)
                {
                    objTop = objCollider.bounds.max.y;
                }
                else
                {
                    objTop = obj.transform.position.y;
                }
                
                // Only consider objects that are below the teleporter
                // and find the one with the highest Y (closest below)
                if (objTop < teleporterY && objTop > highestY)
                {
                    ground = obj;
                    highestY = objTop;
                }
            }
        }
        
        if (ground != null)
        {
            // Get the top surface of the ground
            Renderer groundRenderer = ground.GetComponent<Renderer>();
            Collider groundCollider = ground.GetComponent<Collider>();
            
            float groundTop = 0f;
            
            if (groundRenderer != null)
            {
                // Use the top of the ground's bounds
                groundTop = groundRenderer.bounds.max.y;
            }
            else if (groundCollider != null)
            {
                // Use collider bounds if no renderer
                groundTop = groundCollider.bounds.max.y;
            }
            else
            {
                // Fallback: use ground's Y position
                groundTop = ground.transform.position.y;
            }
            
            // Set teleporter's Y position to ground top (keep X and Z unchanged)
            Vector3 pos = transform.position;
            pos.y = groundTop;
            transform.position = pos;
        }
        else
        {
            Debug.LogWarning($"Teleporter '{gameObject.name}': Could not find a GameObject below it with 'Ground', 'Plane', or 'Floor' in its name. Place teleporter manually.");
        }
        */
    }

    /// <summary>
    /// Custom method (not a Unity method) that configures the BoxCollider and LineRenderer
    /// based on the teleporter settings (size, color, lineWidth).
    /// This is called from OnEnable(), OnValidate(), and Start() to keep components in sync.
    /// Similar pattern to ExitFrame's Apply() method.
    /// </summary>
    void Apply()
    {
        // Get component references
        if (!box) box = GetComponent<BoxCollider>();
        if (!lr) lr = GetComponent<LineRenderer>();

        // Check if components exist (may be null in prefab mode or before components are added)
        if (box == null || lr == null)
        {
            return; // Components not ready yet, skip configuration
        }

        // Configure BoxCollider: trigger volume that the player must stand in
        box.isTrigger = true;
        box.size = new Vector3(teleporterSize.x, teleporterSize.y, triggerDepth);
        // Center the collider so bottom edge aligns with transform position (ground level)
        box.center = new Vector3(0, teleporterSize.y * 0.5f, 0);

        // Configure LineRenderer: simple rectangular outline
        lr.useWorldSpace = false;
        lr.positionCount = 5;
        lr.startWidth = lr.endWidth = lineWidth;
        lr.loop = false;

        // Handle material - use sharedMaterial for prefabs, material for runtime
        if (lr.sharedMaterial == null)
        {
            Material newMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lr.sharedMaterial = newMat;
        }

        // Set color - use material at runtime, sharedMaterial in editor/prefab mode
        // Always use sharedMaterial in editor to avoid "not allowed to access material on prefab" error
        if (Application.isPlaying)
        {
            // Use .material (creates instance) when playing
            if (lr.material != null)
            {
                lr.material.color = color;
            }
        }
        else
        {
            // In editor/prefab mode, always use sharedMaterial
            if (lr.sharedMaterial != null)
            {
                lr.sharedMaterial.color = color;
            }
        }

        // Calculate frame positions (local space, relative to transform)
        float hx = teleporterSize.x * 0.5f;  // Half-width
        // LineRenderer positions: bottom at Y=0, top at Y=teleporterSize.y (local space)
        // Since useWorldSpace = false, positions are relative to transform
        // Transform should be positioned at ground level (Y=ground top)
        lr.SetPositions(new Vector3[] {
            new(-hx, 0, 0),                    // bottom-left
            new(hx, 0, 0),                     // bottom-right
            new(hx, teleporterSize.y, 0),     // top-right
            new(-hx, teleporterSize.y, 0),     // top-left
            new(-hx, 0, 0)                     // back to bottom-left (close the loop)
        });
    }

    /// <summary>
    /// Configures the signal text on a child GameObject with TextMeshPro component.
    /// Finds the TextMeshPro component if not assigned, and sets the text based on signalText or isEntrance.
    /// </summary>
    void ConfigureSignalText()
    {
        // Find TextMeshPro component if not assigned
        if (signalTextMesh == null)
        {
            signalTextMesh = GetComponentInChildren<TextMeshPro>();
        }

        // If still not found, skip (signal is optional)
        if (signalTextMesh == null)
        {
            return;
        }

        // Determine what text to display
        string textToDisplay = signalText;

        // If signalText is empty, auto-set based on isEntrance
        if (string.IsNullOrEmpty(textToDisplay))
        {
            textToDisplay = isEntrance ? "ENTRY" : "EXIT";
        }

        // Set the text
        signalTextMesh.text = textToDisplay;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Teleporter] OnTriggerEnter called on '{gameObject.name}' - Object: '{other.gameObject.name}'");

        // Only process if this is an entrance teleporter
        if (!isEntrance)
        {
            Debug.Log($"[Teleporter] '{gameObject.name}' is not an entrance, ignoring trigger.");
            return;  // Exit teleporters don't trigger teleportation
        }

        Debug.Log($"[Teleporter] '{gameObject.name}' is an entrance, checking for PlayerController component...");

        // Check for PlayerController component only
        PlayerController player = other.GetComponent<PlayerController>();

        // If no player component found, ignore
        if (player == null)
        {
            Debug.Log($"[Teleporter] '{gameObject.name}' - '{other.gameObject.name}' does not have PlayerController component, ignoring.");
            return;
        }

        Debug.Log($"[Teleporter] '{gameObject.name}' - Player '{player.name}' detected!");

        // Check if we have a linked exit teleporter
        if (linkedTeleporter == null)
        {
            Debug.LogWarning($"[Teleporter] '{gameObject.name}' (Entrance) has no linked Teleporter assigned! Cannot teleport.");
            return;
        }

        Debug.Log($"[Teleporter] '{gameObject.name}' - Linked teleporter found: '{linkedTeleporter.gameObject.name}'");

        // Teleport the player
        TeleportPlayer(player.gameObject);
    }

    /// <summary>
    /// Teleports a player GameObject to the linked exit teleporter's position.
    /// Handles both Rigidbody and Transform-based movement.
    /// </summary>
    void TeleportPlayer(GameObject player)
    {
        if (player == null || linkedTeleporter == null)
        {
            return;
        }

        // Get the exit teleporter's position
        Vector3 exitPosition = linkedTeleporter.transform.position;

        // Check if player has Rigidbody (for physics-based movement)
        Rigidbody rb = player.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Use Rigidbody position for physics objects
            rb.position = exitPosition;
            // Optionally reset velocity to prevent weird physics behavior
            rb.linearVelocity = Vector3.zero;
        }
        else
        {
            // Use Transform position for non-physics objects
            player.transform.position = exitPosition;
        }

        Debug.Log($"{player.name} teleported from '{gameObject.name}' to '{linkedTeleporter.gameObject.name}'");
    }
}
