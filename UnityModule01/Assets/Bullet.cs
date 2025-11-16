using UnityEngine;

/// <summary>
/// Script attached to bullet GameObjects.
/// Handles bullet movement, collision detection, and game over logic.
/// Bullets only hit players of the same color as the bullet.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    /// <summary>
    /// Speed of the bullet in units per second.
    /// </summary>
    public float speed = 10f;

    /// <summary>
    /// Maximum lifetime of the bullet in seconds.
    /// Bullet will be destroyed after this time if it doesn't hit anything.
    /// </summary>
    public float lifetime = 5f;

    /// <summary>
    /// Color of this bullet. Set by the Turret when spawning.
    /// Bullets can only hit players of the same color.
    /// </summary>
    public Color bulletColor = Color.white;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"[Bullet] {name}: No Rigidbody component found!");
            return;
        }

        // Configure Rigidbody for straight-line movement
        rb.useGravity = false; // No gravity - bullets travel in straight lines
        rb.linearVelocity = transform.forward * speed; // Set initial velocity

        // Auto-destroy after lifetime (safety measure)
        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// Called when bullet collides with a trigger collider.
    /// Checks if the hit object is a player of matching color.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // Check if we hit a player
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            // Not a player, ignore (bullet passes through)
            return;
        }

        // Get player's color from their material
        Renderer playerRenderer = player.GetComponent<Renderer>();
        if (playerRenderer == null || playerRenderer.material == null)
        {
            Debug.LogWarning($"[Bullet] Player '{player.name}' has no Renderer or material. Cannot check color.");
            return;
        }

        Color playerColor = playerRenderer.material.color;

        // Compare colors (with small tolerance for floating point comparison)
        if (ColorsMatch(bulletColor, playerColor))
        {
            Debug.Log($"[Bullet] Hit! {player.name} (color: {playerColor}) was hit by bullet (color: {bulletColor})");

            // Visual feedback: change player color to black
            playerRenderer.material.color = Color.black;

            // Trigger game over
            PlayerController.TriggerGameOver();

            // Destroy bullet
            Destroy(gameObject);
        }
        else
        {
            // Colors don't match - bullet passes through this player
            Debug.Log($"[Bullet] Bullet (color: {bulletColor}) passed through {player.name} (color: {playerColor}) - colors don't match");
        }
    }

    /// <summary>
    /// Compares two colors with a small tolerance for floating point precision.
    /// </summary>
    private bool ColorsMatch(Color color1, Color color2)
    {
        float tolerance = 0.01f; // Small tolerance for color comparison
        return Mathf.Abs(color1.r - color2.r) < tolerance &&
               Mathf.Abs(color1.g - color2.g) < tolerance &&
               Mathf.Abs(color1.b - color2.b) < tolerance;
    }
}



