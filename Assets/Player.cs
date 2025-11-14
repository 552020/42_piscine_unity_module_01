using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    public int playerNumber = 1;      // 1, 2, or 3 in the Inspector
    public float moveSpeed = 5f;      // Movement speed
    public float jumpForce = 7f;      // Jump force
    public LayerMask groundMask = -1; // Layers that count as ground (-1 = everything)
    public float groundCheckDistance = 0.6f; // Distance to check for ground
    
    [Header("Start Position")]
    /// <summary>
    /// Optional start position GameObject. If assigned, the player will spawn at this GameObject's position,
    /// slightly above it in the Y axis (by startPositionYOffset). Drag a GameObject from the Hierarchy here.
    /// If not assigned, player uses its current position in the scene.
    /// </summary>
    public Transform startPosition;
    
    /// <summary>
    /// Y offset above the start position GameObject. Player will spawn at startPosition's Y + this value.
    /// Default is 1.0 (spawns 1 unit above the start position).
    /// </summary>
    public float startPositionYOffset = 1.0f;

    private static Player activePlayer = null;
    private static Player[] players;
    private static bool playersInitialized = false;

    // Public getter for camera to access active player
    public static Player GetActivePlayer() => activePlayer;

    private Rigidbody rb;
    private bool isGrounded = false;
    private bool wantJump = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"{name}: No Rigidbody component found!");
            return;
        }
        rb.freezeRotation = true; // Prevent tipping over
        rb.isKinematic = false; // Ensure Rigidbody responds to physics forces
        
        // Debug Rigidbody settings
        Debug.Log($"{name} Rigidbody: isKinematic={rb.isKinematic}, drag={rb.linearDamping}, mass={rb.mass}, constraints={rb.constraints}");
    }

    void Start()
    {
        if (players == null)
            players = FindObjectsByType<Player>(FindObjectsSortMode.None);

        // Auto-assign player numbers 1, 2, 3
        if (!playersInitialized && players != null)
        {
            for (int i = 0; i < players.Length && i < 3; i++)
            {
                players[i].playerNumber = i + 1;
            }
            playersInitialized = true;
        }

        Debug.Log($"Start() called for {name} (Player {playerNumber})");
        
        // Always exclude Layer_None from groundMask if it exists
        // This prevents players from detecting platforms on Layer_None as ground
        int layerNone = LayerMask.NameToLayer("Layer_None");
        if (layerNone != -1)
        {
            // Remove Layer_None from groundMask by using bitwise AND with inverted bit
            groundMask = groundMask & ~(1 << layerNone);
            Debug.Log($"[Player] {name}: Excluded Layer_None from groundMask. Layer_None index: {layerNone}");
        }
        
        // If start position is assigned, move player there (slightly above in Y)
        if (startPosition != null)
        {
            Vector3 startPos = startPosition.position;
            startPos.y += startPositionYOffset;
            transform.position = startPos;
            // Reset rotation to identity (upright, no rotation) when using start position
            transform.rotation = Quaternion.identity;
            Debug.Log($"{name} positioned at start position: {startPos} (above '{startPosition.name}')");
        }
        
        // Store initial position and rotation for reset
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        activePlayer = null;          // start with no active player
    }

    void Update()
    {
        // Handle player switching
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Switching to player 1...");
            SetActivePlayer(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Switching to player 2...");
            SetActivePlayer(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Switching to player 3...");
            SetActivePlayer(3);
        }

        // Handle jump input (only for active player)
        if (this == activePlayer && Input.GetKeyDown(KeyCode.Space))
        {
            wantJump = true;
        }

        // Handle scene reset (R or Backspace) - only check once per frame
        if (players != null && players.Length > 0 && this == players[0] && (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Backspace)))
        {
            Debug.Log("Reset key pressed!");
            ResetAllPlayers();
        }
    }

    void FixedUpdate()
    {
        // Ground check (for all players) - accounts for cube size
        int mask = groundMask.value == 0 ? ~0 : groundMask.value;
        
        // Get collider to determine cube size
        Collider col = GetComponent<Collider>();
        float checkDistance = groundCheckDistance;
        
        if (col != null)
        {
            // Calculate distance from center to bottom of cube
            float bottomOffset = col.bounds.extents.y;
            // Cast from just above the bottom of the cube
            Vector3 rayOrigin = transform.position - Vector3.up * (bottomOffset - 0.1f);
            checkDistance = groundCheckDistance + 0.1f;
            isGrounded = Physics.Raycast(rayOrigin, Vector3.down, checkDistance, mask);
        }
        else
        {
            // Fallback to simple raycast if no collider
            isGrounded = Physics.Raycast(transform.position, Vector3.down, checkDistance, mask);
        }

        // Only handle movement/jump if this is the active player
        if (activePlayer != this) return;

        // Handle jump (only if grounded)
        if (wantJump && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        wantJump = false;

        // Handle horizontal movement (A/D, arrows, or gamepad)
        float horizontal = Input.GetAxisRaw("Horizontal");
        
        // ============================================================================
        // IMPORTANT: Direct Velocity Control for Platformer Movement
        // ============================================================================
        // We use direct velocity control (setting rb.linearVelocity directly) instead
        // of AddForce() because:
        // 
        // 1. IMMEDIATE RESPONSIVENESS: Platformers require instant, precise control.
        //    Players expect the character to move immediately when pressing keys and
        //    stop immediately when releasing them. AddForce() creates acceleration
        //    which feels sluggish and unresponsive.
        //
        // 2. PREVENTS SLIDING: When using AddForce(), if we only apply force when
        //    there's input, the player continues sliding due to inertia when input
        //    stops. By always setting velocity (including zero when no input), we
        //    ensure the player stops immediately when keys are released.
        //
        // 3. CONSISTENT BEHAVIOR: Direct velocity control gives predictable, frame-
        //    independent movement speed that matches the moveSpeed value exactly,
        //    regardless of physics timestep or frame rate.
        //
        // 4. STANDARD PRACTICE: This is the most common approach in platformer games
        //    (Mario, Celeste, Hollow Knight, etc.) because it provides the tight,
        //    responsive controls players expect.
        //
        // We preserve the Y velocity (for jumping/falling) and Z velocity (if any),
        // but always control X velocity directly based on input.
        // ============================================================================
        
        if (rb == null)
        {
            Debug.LogError($"{name}: Rigidbody is null!");
            return;
        }
        
        // Always set horizontal velocity directly (even when input is zero to stop sliding)
        rb.linearVelocity = new Vector3(horizontal * moveSpeed, rb.linearVelocity.y, rb.linearVelocity.z);
    }

    private static void SetActivePlayer(int number)
    {
        foreach (var p in players)
        {
            if (p.playerNumber == number)
            {
                activePlayer = p;
                Debug.Log($"Switched! Active player is now: {p.name} (Player {number})");
                return;
            }
        }
        Debug.LogWarning($"Player {number} not found! Make sure playerNumber is set correctly in Inspector.");
    }

    public static void ResetAllPlayers()
    {
        if (players == null)
        {
            Debug.LogWarning("ResetAllPlayers: players array is null!");
            return;
        }

        Debug.Log($"ResetAllPlayers: Resetting {players.Length} players");
        
        foreach (var p in players)
        {
            if (p != null && p.rb != null)
            {
                // Reset position and rotation
                p.transform.position = p.initialPosition;
                p.transform.rotation = p.initialRotation;
                
                // Stop all movement
                p.rb.linearVelocity = Vector3.zero;
                p.rb.angularVelocity = Vector3.zero;
                
                // Reset jump flag
                p.wantJump = false;
                
                Debug.Log($"Reset {p.name} to position {p.initialPosition}");
            }
            else
            {
                Debug.LogWarning($"ResetAllPlayers: Player {p?.name} is null or has no Rigidbody!");
            }
        }
        
        // Reset active player selection
        activePlayer = null;
        
        // Reset exit completion flags
        ExitFrame.ResetCompletionFlag();
        
        Debug.Log("Scene reset: All players returned to initial positions");
    }
}
