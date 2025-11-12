using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(LineRenderer))]
public class ExitFrame : MonoBehaviour
{
    public Vector2 size = new Vector2(1f, 1f);   // width, height (used if targetPlayer is null)
    public float padding = 1.5f;                  // padding around player size (larger = easier to fit)
    public Player targetPlayer;                   // Player to match size with
    public float lineWidth = 0.05f;
    public Color color = Color.white;

    BoxCollider box;
    LineRenderer lr;
    
    // Static tracking for all exits
    private static ExitFrame[] allExits;
    private static bool exitsInitialized = false;
    private bool isPlayerInside = false;

    void OnEnable()  
    { 
        if (!Application.isPlaying) AlignWithGround();
        if (targetPlayer != null) CalculateSizeFromPlayer();
        Apply(); 
    }
    
    void OnValidate()
    { 
        if (!Application.isPlaying) AlignWithGround();
        if (targetPlayer != null) CalculateSizeFromPlayer();
        Apply(); 
    }   // updates live in the editor

    void Start()
    {
        // Align bottom with ground
        AlignWithGround();
        
        // Calculate size from target player if set
        if (targetPlayer != null)
        {
            CalculateSizeFromPlayer();
        }
        Apply();
        
        // Initialize exits array
        if (!exitsInitialized)
        {
            allExits = FindObjectsByType<ExitFrame>(FindObjectsSortMode.None);
            exitsInitialized = true;
            Debug.Log($"ExitFrame: Found {allExits.Length} exit frames");
        }
    }

    void AlignWithGround()
    {
        // Find Ground GameObject by name
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            // Try alternative names
            ground = GameObject.Find("Plane") ?? GameObject.Find("Floor");
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
            
            Vector3 pos = transform.position;
            pos.y = groundTop;
            transform.position = pos;
        }
        else
        {
            Debug.LogWarning("ExitFrame: Could not find Ground GameObject. Place exit frames manually.");
        }
    }

    void CalculateSizeFromPlayer()
    {
        Renderer r = targetPlayer.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            Vector3 playerSize = r.bounds.size;
            size.x = playerSize.x + padding;
            size.y = playerSize.y + padding;
        }
    }

    void Apply()
    {
        if (!box) box = GetComponent<BoxCollider>();
        if (!lr)  lr  = GetComponent<LineRenderer>();

        // trigger volume that the player must stand in
        box.isTrigger = true;
        box.size = new Vector3(size.x, size.y, 0.2f);
        // Center the collider so bottom edge aligns with transform position (ground level)
        box.center = new Vector3(0, size.y * 0.5f, 0);

        // simple rectangular outline
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
        
        // Set color - use material at runtime, sharedMaterial in editor
        if (Application.isPlaying)
        {
            lr.material.color = color;
        }
        else
        {
            // In editor/prefab mode, modify sharedMaterial
            if (lr.sharedMaterial != null)
            {
                lr.sharedMaterial.color = color;
            }
        }

        float hx = size.x * 0.5f;
        // LineRenderer positions: bottom at Y=0, top at Y=size.y (local space)
        // Since useWorldSpace = false, positions are relative to transform
        // Transform should be positioned at ground level (Y=0)
        lr.SetPositions(new Vector3[] {
            new(-hx, 0, 0),           // bottom-left
            new(hx, 0, 0),            // bottom-right
            new(hx, size.y, 0),       // top-right
            new(-hx, size.y, 0),      // top-left
            new(-hx, 0, 0)            // back to bottom-left (close the loop)
        });
    }

    void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null && player == targetPlayer)
        {
            isPlayerInside = true;
            Debug.Log($"{targetPlayer.name} entered their exit frame");
        }
    }

    void OnTriggerStay(Collider other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null && player == targetPlayer)
        {
            // Continuously verify player is still inside
            isPlayerInside = true;
            // Check if all exits are complete
            CheckAllExitsComplete();
        }
    }

    void OnTriggerExit(Collider other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null && player == targetPlayer)
        {
            isPlayerInside = false;
            Debug.Log($"{targetPlayer.name} left their exit frame");
        }
    }

    private static bool stageCompleteMessageShown = false;

    public static void ResetCompletionFlag()
    {
        stageCompleteMessageShown = false;
        if (allExits != null)
        {
            foreach (var exit in allExits)
            {
                if (exit != null)
                {
                    exit.isPlayerInside = false;
                }
            }
        }
    }

    void CheckAllExitsComplete()
    {
        if (allExits == null) return;

        bool allComplete = true;
        foreach (var exit in allExits)
        {
            if (exit == null || exit.targetPlayer == null || !exit.isPlayerInside)
            {
                allComplete = false;
                break;
            }
        }

        if (allComplete && !stageCompleteMessageShown)
        {
            stageCompleteMessageShown = true;
            Debug.Log("STAGE COMPLETE! All characters are aligned with their exits!");
        }
        else if (!allComplete && stageCompleteMessageShown)
        {
            // Reset flag if someone leaves
            stageCompleteMessageShown = false;
        }
    }
}
