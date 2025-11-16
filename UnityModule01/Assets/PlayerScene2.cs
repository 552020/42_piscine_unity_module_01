using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerScene2 : MonoBehaviour
{
    public int playerNumber = 1;      // 1, 2, or 3 in the Inspector
    public float moveSpeed = 5f;      // Movement speed
    public float jumpForce = 7f;      // Jump force
    public float groundCheckDistance = 0.6f; // Distance to check for ground

    private static PlayerScene2 activePlayer = null;
    private static PlayerScene2[] players;
    private static bool playersInitialized = false;

    // Public getter for camera to access active player
    public static PlayerScene2 GetActivePlayer() => activePlayer;

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
            players = FindObjectsByType<PlayerScene2>(FindObjectsSortMode.None);

        // Auto-assign player numbers based on GameObject name
        if (!playersInitialized && players != null)
        {
            foreach (var player in players)
            {
                string playerName = player.name;
                
                if (playerName.Contains("Claire", System.StringComparison.OrdinalIgnoreCase))
                {
                    player.playerNumber = 1;
                }
                else if (playerName.Contains("John", System.StringComparison.OrdinalIgnoreCase))
                {
                    player.playerNumber = 2;
                }
                else if (playerName.Contains("Thomas", System.StringComparison.OrdinalIgnoreCase))
                {
                    player.playerNumber = 3;
                }
                else
                {
                    Debug.LogWarning($"{playerName}: Name doesn't contain Claire, John, or Thomas. Keeping default playerNumber = {player.playerNumber}");
                }
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
        // Each player only detects ground on their specific layer and Layer_All
        int mask = GetGroundLayerMask();
        
        // Get collider to determine cube size
        Collider col = GetComponent<Collider>();
        float checkDistance = groundCheckDistance;
        RaycastHit hit;
        bool hitSomething = false;
        
        if (col != null)
        {
            // Calculate distance from center to bottom of cube
            float bottomOffset = col.bounds.extents.y;
            // Cast from just above the bottom of the cube
            Vector3 rayOrigin = transform.position - Vector3.up * (bottomOffset - 0.1f);
            checkDistance = groundCheckDistance + 0.1f;
            hitSomething = Physics.Raycast(rayOrigin, Vector3.down, out hit, checkDistance, mask);
            isGrounded = hitSomething;
        }
        else
        {
            // Fallback to simple raycast if no collider
            hitSomething = Physics.Raycast(transform.position, Vector3.down, out hit, checkDistance, mask);
            isGrounded = hitSomething;
        }
        
        // Debug ground detection for active player
        if (activePlayer == this && Time.frameCount % 30 == 0) // Log every 30 frames to avoid spam
        {
            if (hitSomething)
            {
                Debug.Log($"{name} (Player {playerNumber}): Grounded! Hit: {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}, mask={mask}");
            }
            else
            {
                Debug.Log($"{name} (Player {playerNumber}): NOT grounded! Mask={mask} (should include Layer_John/Layer_Claire/Layer_Thomas + Layer_All)");
            }
        }

        // Only handle movement/jump if this is the active player
        if (activePlayer != this) return;

        // Handle jump (only if grounded)
        if (wantJump)
        {
            if (isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                Debug.Log($"{name} (Player {playerNumber}): Jumped!");
            }
            else
            {
                Debug.Log($"{name} (Player {playerNumber}): Jump blocked - not grounded! Mask={mask}");
            }
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

    private int GetGroundLayerMask()
    {
        // Player 1 (Claire) -> Layer_Claire + Layer_All
        // Player 2 (John) -> Layer_John + Layer_All
        // Player 3 (Thomas) -> Layer_Thomas + Layer_All
        string playerLayerName = "";
        switch (playerNumber)
        {
            case 1:
                playerLayerName = "Layer_Claire";
                break;
            case 2:
                playerLayerName = "Layer_John";
                break;
            case 3:
                playerLayerName = "Layer_Thomas";
                break;
            default:
                Debug.LogWarning($"{name}: Invalid playerNumber {playerNumber}, defaulting to Layer_All only");
                return LayerMask.GetMask("Layer_All");
        }
        
        // Combine the player's specific layer with Layer_All
        int mask = LayerMask.GetMask(playerLayerName, "Layer_All");
        
        // Debug: Log the mask value once at Start
        if (Time.frameCount < 5) // Only log in first few frames
        {
            Debug.Log($"{name} (Player {playerNumber}): Ground mask = {mask}, looking for layers: {playerLayerName} + Layer_All");
        }
        
        return mask;
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
