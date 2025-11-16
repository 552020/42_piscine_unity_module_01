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
    public Transform startPosition;
    public float startPositionYOffset = 1.0f;

    private static Player activePlayer = null;
    private static Player[] players;
    private static bool playersInitialized = false;

    public static Player GetActivePlayer() => activePlayer;

    private Rigidbody rb;
    private bool isGrounded = false;
    private bool wantJump = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Color originalColor;
    private static bool isGameOver = false;

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
    }

    void Start()
    {
        if (players == null)
            players = FindObjectsByType<Player>(FindObjectsSortMode.None);

        foreach (var p in players)
        {
            if (p.name == "Thomas")
            {
                p.playerNumber = 1;
                p.moveSpeed = 5f;
                p.jumpForce = 7f;
            }
            if (p.name == "Claire")
            {
                p.playerNumber = 2;
                p.moveSpeed = 4f;
                p.jumpForce = 4f;
            }
            if (p.name == "John")
            {
                p.playerNumber = 3;
                p.moveSpeed = 6f;
                p.jumpForce = 8f;
            }
        }

        // Always exclude Layer_None from groundMask if it exists
        // This prevents players from detecting platforms on Layer_None as ground
        // We need LayerNone only in the Scene 3
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