using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    public int playerNumber = 1;      // 1, 2, or 3 in the Inspector
    public float moveSpeed = 5f;      // Movement speed

    private static Player activePlayer = null;
    private static Player[] players;
    private static bool playersInitialized = false;

    private Rigidbody rb;

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
    }

    void FixedUpdate()
    {
        // Only move if this is the active player
        if (activePlayer != this) return;

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
            
            // rb.WakeUp(); // Wake up Rigidbody if sleeping - testing if needed
            
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
}
