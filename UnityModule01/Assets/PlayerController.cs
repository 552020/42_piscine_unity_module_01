using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public int playerNumber = 1;      // 1, 2, or 3 in the Inspector
    public float moveSpeed = 5f;      // Movement speed
    public float jumpForce = 7f;      // Jump force
    public LayerMask groundMask = -1; // Layers that count as ground (-1 = everything)
    public float groundCheckDistance = 0.6f; // Distance to check for ground

    [Header("Start Position")]
    public Transform startPosition;
    public float startPositionYOffset = 1.0f;

    private static PlayerController activePlayer = null;
    private static PlayerController[] players;

    public static PlayerController GetActivePlayer() => activePlayer;

    private Rigidbody rb;
    private bool isGrounded = false;
    private bool wantJump = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private static bool isGameOver = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;
        rb.freezeRotation = true; // Prevent tipping over
        rb.isKinematic = false; // Ensure Rigidbody responds to physics forces
    }

    void OnDestroy()
    {
        // Clear static references when this instance is destroyed
        if (activePlayer == this)
            activePlayer = null;
        
        // Clear the players array to force re-scan on next scene load
        players = null;
    }

    void Start()
    {
        if (players == null)
            players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        // Assign player numbers and stats based on GameObject name
        foreach (var p in players)
        {
            if (p != null) // Guard against destroyed objects
                p.AssignNumberSpeedJump();
        }

        // Build per-player ground mask (excludes other players' layers, includes own)
        BuildPerPlayerGroundMask();

        // Position player at start position if assigned
        SetPlayerIntoStartPosition();

        // Store initial position and rotation for reset
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        activePlayer = null;          // start with no active player
        isGameOver = false;           // Reset game over flag
    }

    void Update()
    {
        if (players != null && players.Length > 0 && this == players[0] && (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Backspace)))
        {
            ResetAllPlayers();
            return;
        }

        if (isGameOver) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetActivePlayer(1);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetActivePlayer(2);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            SetActivePlayer(3);

        if (this == activePlayer && Input.GetKeyDown(KeyCode.Space))
            // Capture in Update to avoid missing the one-frame pulse.
            // See Docs/JumpInput_Update_vs_FixedUpdate.md
            wantJump = true;
    }

    void FixedUpdate()
    {
        if (isGameOver || activePlayer != this || rb == null) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector3(horizontal * moveSpeed, rb.linearVelocity.y, rb.linearVelocity.z);

        isGrounded = CheckGrounded();
        if (wantJump && isGrounded)
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        wantJump = false;

    }

    private static void SetActivePlayer(int number)
    {
        if (players == null) return;
        
        foreach (var p in players)
        {
            if (p == null) continue; // Skip destroyed objects
            if (p.playerNumber == number)
            {
                activePlayer = p;
                Debug.Log($"Switched! Active player is now: {p.name} (Player {number})");
                return;
            }
        }
        Debug.LogWarning($"Player {number} not found! Make sure playerNumber is set correctly in Inspector.");
    }

    public static void TriggerGameOver()
    {
        if (isGameOver)
        {
            return; // Already game over, don't trigger multiple times
        }

        isGameOver = true;
        Debug.Log("GAME OVER! Press R or Backspace to reset.");

        // Stop all player movement
        if (players != null)
        {
            foreach (var p in players)
            {
                if (p != null && p.rb != null)
                {
                    p.rb.linearVelocity = Vector3.zero;
                    p.rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    public static void ResetAllPlayers()
    {
        if (players == null)
            return;

        Debug.Log($"ResetAllPlayers: Resetting {players.Length} players");

        isGameOver = false;

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
        activePlayer = null;
        ExitFrame.ResetCompletionFlag();
        Debug.Log("Scene reset: All players returned to initial positions");
    }

    private void AssignNumberSpeedJump()
    {
        if (this == null) return; // Guard against destroyed objects
        
        if (name == "Thomas")
        {
            playerNumber = 1;
            moveSpeed = 5f;
            jumpForce = 7f;
        }
        else if (name == "Claire")
        {
            playerNumber = 2;
            moveSpeed = 4f;
            jumpForce = 4f;
        }
        else if (name == "John")
        {
            playerNumber = 3;
            moveSpeed = 6f;
            jumpForce = 8f;
        }
    }

    private void SetPlayerIntoStartPosition()
    {
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
    }

    private void BuildPerPlayerGroundMask()
    {
        int mask = groundMask.value;

        // Remove Layer_None
        int layerNone = LayerMask.NameToLayer("Layer_None");
        if (layerNone != -1)
            mask &= ~(1 << layerNone);

        // Remove all three player layers
        int LayerClaire = LayerMask.NameToLayer("Layer_Claire");
        int LayerThomas = LayerMask.NameToLayer("Layer_Thomas");
        int LayerJohn = LayerMask.NameToLayer("Layer_John");

        if (LayerClaire != -1) mask &= ~(1 << LayerClaire);
        if (LayerThomas != -1) mask &= ~(1 << LayerThomas);
        if (LayerJohn != -1) mask &= ~(1 << LayerJohn);

        // Add back only this player's layer
        string playerName = name;
        int playerLayer = -1;

        if (playerName == "Claire")
            playerLayer = LayerClaire;
        else if (playerName == "Thomas")
            playerLayer = LayerThomas;
        else if (playerName == "John")
            playerLayer = LayerJohn;

        if (playerLayer != -1)
            mask |= (1 << playerLayer);

        groundMask = mask;
    }

    private bool CheckGrounded()
    {
        int mask = groundMask.value;
        Collider col = GetComponent<Collider>();
        float checkDistance = groundCheckDistance;
        if (col != null)
        {
            float bottomOffset = col.bounds.extents.y;
            Vector3 rayOrigin = transform.position - Vector3.up * (bottomOffset - 0.1f);
            checkDistance = groundCheckDistance + 0.1f;
            return Physics.Raycast(rayOrigin, Vector3.down, checkDistance, mask);
        }
        else
        {
            return Physics.Raycast(transform.position, Vector3.down, checkDistance, mask);
        }
    }
}