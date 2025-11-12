using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    public int playerNumber = 1;      // 1, 2, or 3 in the Inspector
    public float moveSpeed = 5f;      // Movement speed
    public float jumpForce = 7f;      // Jump force
    public LayerMask groundMask = -1; // Layers that count as ground (-1 = everything)
    public float groundCheckDistance = 0.6f; // Distance to check for ground

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
        Vector3 moveDirection = new Vector3(horizontal, 0f, 0f);

        // Apply movement force
        if (moveDirection != Vector3.zero)
        {
            if (rb == null)
            {
                Debug.LogError($"{name}: Rigidbody is null!");
                return;
            }
            
            Vector3 force = moveDirection * moveSpeed * 10f; // Increase force to overcome friction
            Vector3 posBefore = transform.position;
            Vector3 velBefore = rb.linearVelocity;
            
            // Try direct velocity change instead of force
            // rb.linearVelocity = new Vector3(horizontal * moveSpeed, rb.linearVelocity.y, rb.linearVelocity.z);
            
            // Try AddForce with higher force
            rb.AddForce(force, ForceMode.Acceleration);
            
            Debug.Log($"{name} moving: horizontal={horizontal}, force={force}, posBefore={posBefore}, velBefore={velBefore}, velAfter={rb.linearVelocity}");
        }
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
        
        Debug.Log("Scene reset: All players returned to initial positions");
    }
}
