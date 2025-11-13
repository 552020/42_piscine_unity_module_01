using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ButtonCollider : MonoBehaviour
{
    // Button state: colored or not colored
    private bool isColored = false;
    
    // Reference to the Trigger GameObject (the top part that changes color)
    private GameObject triggerObject;
    private MeshRenderer triggerRenderer;
    
    // ============================================================================
    // STEP 2: Base Cylinder Reference
    // ============================================================================
    // Reference to the Base cylinder GameObject. We need this to calculate where
    // the Trigger should sink to (Trigger's top should align with Base's top).
    // Base is a sibling of Trigger, both children of Button.
    // ============================================================================
    private Transform baseTransform;
    private Renderer baseRenderer;
    
    // Original color (white)
    private Color originalColor = Color.white;
    
    // Coroutine reference for reset timer
    private Coroutine resetCoroutine;
    
    // ============================================================================
    // STEP 9: Store Original Trigger Position
    // ============================================================================
    // Store the original Y position of the Trigger cylinder. This is needed so we
    // know where to rise back to after the button has sunk. We store it in Start()
    // before any sinking occurs, so we always have the initial position.
    // ============================================================================
    private float originalTriggerY = 0f;
    
    // ============================================================================
    // STEP 1: Player Tracking
    // ============================================================================
    // Track which players are currently on the button using a HashSet.
    // This is more reliable than a counter because it tracks actual Player objects,
    // preventing sync issues when the button sinks and ButtonCollider moves.
    // ============================================================================
    private HashSet<Player> playersOnButton = new HashSet<Player>();
    
    // ============================================================================
    // STEP 4: Sinking State Variables
    // ============================================================================
    // Track the state of sinking/rising animations and manage coroutines.
    //
    // Why we need coroutine references:
    // - When StartCoroutine() is called, it returns a Coroutine object
    // - We need to store this reference so we can call StopCoroutine() later
    // - This allows us to cancel animations mid-way (e.g., if player leaves
    //   while sinking, or if another player joins while rising)
    // - Without the reference, we cannot stop a running coroutine
    //
    // Why we need boolean flags:
    // - isSinking/isRising tell us the current animation state
    // - Useful for preventing duplicate animations (don't start sinking if already sinking)
    // - Helps with edge case handling (e.g., player joins mid-rise)
    // ============================================================================
    private bool isSinking = false;
    private bool isRising = false;
    private Coroutine sinkCoroutine = null;  // Reference to stop sinking animation if needed
    private Coroutine riseCoroutine = null;   // Reference to stop rising animation if needed
    private float lastSinkCompleteTime = -1f; // Track when sinking last completed (for grace period)
    private const float SINK_GRACE_PERIOD = 0.2f; // Ignore OnTriggerExit for 0.2s after sinking completes
    
    /// <summary>
    /// Speed at which the Trigger cylinder sinks/rises (units per second).
    /// Higher values = faster animation. Default is 2 units per second.
    /// </summary>
    [SerializeField] private float sinkSpeed = 2f;

    void Start()
    {
        // Find the Trigger GameObject (parent's sibling)
        // ButtonCollider is child of Trigger, so: transform.parent = Trigger
        // Trigger's parent = Button, so: transform.parent.parent = Button
        // Then find child named "Trigger"
        Transform buttonTransform = transform.parent.parent;
        if (buttonTransform != null)
        {
            // Find Trigger GameObject (for color changing)
            Transform triggerTransform = buttonTransform.Find("Trigger");
            if (triggerTransform != null)
            {
                triggerObject = triggerTransform.gameObject;
                triggerRenderer = triggerObject.GetComponent<MeshRenderer>();
                
                // Store the original color
                if (triggerRenderer != null && triggerRenderer.material != null)
                {
                    originalColor = triggerRenderer.material.color;
                }
                
                // STEP 9: Store the original Y position of the Trigger
                originalTriggerY = triggerTransform.position.y;
                Debug.Log($"[ButtonCollider] STEP 9: Stored original Trigger Y position: {originalTriggerY}");
            }
            else
            {
                Debug.LogError($"ButtonCollider: Could not find 'Trigger' GameObject under '{buttonTransform.name}'");
            }
            
            // STEP 2: Find Base GameObject (sibling of Trigger, child of Button)
            Transform baseTransformFound = buttonTransform.Find("Base");
            if (baseTransformFound != null)
            {
                baseTransform = baseTransformFound;
                baseRenderer = baseTransform.GetComponent<Renderer>();
                Debug.Log($"[ButtonCollider] STEP 2: Base cylinder found: {baseTransform.name} at position {baseTransform.position}");
            }
            else
            {
                Debug.LogError($"ButtonCollider: STEP 2: Could not find 'Base' GameObject under '{buttonTransform.name}'");
            }
        }
        else
        {
            Debug.LogError("ButtonCollider: Could not find parent Button GameObject");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ButtonCollider] OnTriggerEnter triggered! Triggered by: {other.gameObject.name}");
        
        // Check if trigger is with a Player
        Player player = other.gameObject.GetComponent<Player>();
        if (player == null)
        {
            Debug.Log($"[ButtonCollider] Not a Player component. Ignoring trigger with {other.gameObject.name}");
            return; // Not a player, ignore
        }
        
        Debug.Log($"[ButtonCollider] Player detected! Name: {player.name}, PlayerNumber: {player.playerNumber}, isColored: {isColored}");
        
        // STEP 1: Add player to set (HashSet prevents duplicates)
        playersOnButton.Add(player);
        Debug.Log($"[ButtonCollider] Players on button: {playersOnButton.Count} (added {player.name})");
        
        // Cancel any pending reset if player lands again
        if (resetCoroutine != null)
        {
            Debug.Log("[ButtonCollider] Cancelling pending reset coroutine");
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }
        
        // ============================================================================
        // STEP 7: Handle Sinking When Button is Already Colored
        // ============================================================================
        // If button is already colored and this is the first player (or first after
        // all players left), start sinking. Stop any rising animation first.
        // ============================================================================
        if (isColored)
        {
            Debug.Log("[ButtonCollider] Button already colored. Checking if sinking should start...");
            
            // Stop any rising animation or pending rise if player joins
            // Check both isRising flag and riseCoroutine (in case it's in delay phase)
            if (riseCoroutine != null)
            {
                Debug.Log("[ButtonCollider] STEP 7: Player joined. Stopping any pending/active rise animation.");
                StopCoroutine(riseCoroutine);
                riseCoroutine = null;
                isRising = false;
            }
            
            // Start sinking if this is the first player on the button and not already sinking
            if (playersOnButton.Count == 1 && !isSinking)
            {
                Debug.Log("[ButtonCollider] STEP 7: Starting sink animation (button colored, first player on button)");
                if (sinkCoroutine != null)
                {
                    StopCoroutine(sinkCoroutine);
                }
                sinkCoroutine = StartCoroutine(SinkTrigger());
            }
            else if (isSinking)
            {
                Debug.Log("[ButtonCollider] STEP 7: Already sinking, no action needed.");
            }
            
            return; // Don't color the button again
        }
        
        // Check if player is landing from above
        // For trigger, check if player's position is above the button
        float playerY = player.transform.position.y;
        float buttonY = transform.position.y;
        float heightDifference = playerY - buttonY;
        
        Debug.Log($"[ButtonCollider] Player Y: {playerY}, Button Y: {buttonY}, Height difference: {heightDifference}");
        
        // Player should be above the button (at least slightly)
        if (heightDifference < 0.1f)
        {
            Debug.Log($"[ButtonCollider] Player not above button (height diff: {heightDifference} < 0.1). Ignoring.");
            return; // Not landing from above
        }
        
        Debug.Log($"[ButtonCollider] Player landing from above confirmed! Proceeding to color button.");
        
        // ============================================================================
        // COLOR READING APPROACH: Read color directly from player's material
        // ============================================================================
        // Instead of using playerNumber to determine color, we read the actual color
        // from the player's MeshRenderer material. This approach is more reliable because:
        // 1. It matches the visual color the player actually has (what you see on screen)
        // 2. It doesn't depend on playerNumber being set correctly
        // 3. It's more flexible if player colors change in the future
        // 4. It automatically handles any color variations or custom colors
        // ============================================================================
        
        // Get the player's MeshRenderer to read their material color
        MeshRenderer playerRenderer = player.GetComponent<MeshRenderer>();
        Color playerColor = Color.white; // Default fallback color
        
        if (playerRenderer != null && playerRenderer.material != null)
        {
            // Read the color directly from the player's material
            // Using .material (not .sharedMaterial) to get the instance color at runtime
            playerColor = playerRenderer.material.color;
            Debug.Log($"[ButtonCollider] Read player color directly from material: {playerColor} (RGB: {playerColor.r}, {playerColor.g}, {playerColor.b})");
        }
        else
        {
            Debug.LogWarning($"[ButtonCollider] Player '{player.name}' has no MeshRenderer or material! Using default white color.");
        }
        
        // Change Trigger's material color to match the player's color
        if (triggerRenderer != null)
        {
            Material mat = triggerRenderer.material;
            mat.color = playerColor;
            Debug.Log($"[ButtonCollider] Button colored with player '{player.name}'s color: {playerColor}");
        }
        else
        {
            Debug.LogError("[ButtonCollider] triggerRenderer is null! Cannot change color.");
        }
        
        // Set state to colored
        isColored = true;
        Debug.Log("[ButtonCollider] Button state set to colored = true");
    }
    
    void OnTriggerExit(Collider other)
    {
        Debug.LogError($"[ButtonCollider] ===== OnTriggerExit FIRED (ERROR LOG TO ENSURE VISIBILITY) ===== Exited by: {other.gameObject.name}");
        Debug.Log($"[ButtonCollider] ===== OnTriggerExit FIRED ===== Exited by: {other.gameObject.name}");
        Debug.Log($"[ButtonCollider] OnTriggerExit: Current state - Players on button: {playersOnButton.Count}, isColored: {isColored}, isSinking: {isSinking}, isRising: {isRising}");
        
        // Check if trigger exit is with a Player
        Player player = other.gameObject.GetComponent<Player>();
        if (player == null)
        {
            Debug.Log($"[ButtonCollider] Not a Player component. Ignoring trigger exit with {other.gameObject.name}");
            return; // Not a player, ignore
        }
        
        Debug.Log($"[ButtonCollider] Player exited! Name: {player.name}, isColored: {isColored}, playersOnButton before removal: {playersOnButton.Count}");
        
        // CRITICAL FIX: If button is sinking OR just finished sinking, the OnTriggerExit is likely due to the button moving,
        // not the player actually leaving. Don't remove the player in this case.
        bool withinGracePeriod = (Time.time - lastSinkCompleteTime) < SINK_GRACE_PERIOD;
        if (isSinking || withinGracePeriod)
        {
            Debug.Log($"[ButtonCollider] Button is sinking or within grace period - ignoring OnTriggerExit for {player.name} (isSinking: {isSinking}, withinGracePeriod: {withinGracePeriod}, timeSinceSinkComplete: {Time.time - lastSinkCompleteTime})");
            return; // Don't process the exit - player is still on the button
        }
        
        // STEP 1: Remove player from set
        bool removed = playersOnButton.Remove(player);
        if (!removed)
        {
            Debug.LogWarning($"[ButtonCollider] WARNING: Tried to remove {player.name} but they weren't in the set! Counter may be out of sync.");
        }
        Debug.Log($"[ButtonCollider] Players on button: {playersOnButton.Count} (after removal)");
        
        // ============================================================================
        // STEP 8: Handle Rising When All Players Leave
        // ============================================================================
        // If all players have left (playersOnButton.Count == 0) AND button is colored,
        // start rising animation. Stop any sinking animation first.
        // ============================================================================
        Debug.Log($"[ButtonCollider] STEP 8: Checking if should rise. playersOnButton.Count: {playersOnButton.Count}, isColored: {isColored}, isSinking: {isSinking}, isRising: {isRising}");
        
        if (playersOnButton.Count == 0 && isColored)
        {
            Debug.Log($"[ButtonCollider] ===== STEP 8: STARTING RISE ===== Players on button: {playersOnButton.Count}, isColored: {isColored}, isSinking: {isSinking}, isRising: {isRising}");
            
            // Stop any sinking animation if player leaves while sinking
            if (isSinking && sinkCoroutine != null)
            {
                Debug.Log("[ButtonCollider] STEP 8: Player left while sinking. Stopping sink animation.");
                StopCoroutine(sinkCoroutine);
                sinkCoroutine = null;
                isSinking = false;
            }
            
            // Start rising animation if not already rising
            if (!isRising)
            {
                Debug.Log("[ButtonCollider] STEP 8: Starting rise animation.");
                if (riseCoroutine != null)
                {
                    StopCoroutine(riseCoroutine);
                }
                riseCoroutine = StartCoroutine(RiseTrigger());
            }
            else
            {
                Debug.Log("[ButtonCollider] STEP 8: Already rising, no action needed.");
            }
        }
        else if (playersOnButton.Count > 0)
        {
            Debug.Log($"[ButtonCollider] STEP 8: {playersOnButton.Count} player(s) still on button. Not rising.");
        }
        else if (!isColored)
        {
            Debug.Log("[ButtonCollider] STEP 8: Button not colored, no rising needed.");
        }
        
        // Only reset color if button is colored
        if (!isColored)
        {
            return;
        }
        
        // Start 2-second timer to reset button color (this happens regardless of rising)
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }
        Debug.Log("[ButtonCollider] Starting 2-second reset timer");
        resetCoroutine = StartCoroutine(ResetButtonAfterDelay(2f));
    }
    
    private IEnumerator ResetButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Reset button color to original
        if (triggerRenderer != null)
        {
            Material mat = triggerRenderer.material;
            mat.color = originalColor;
            Debug.Log("Button reset to original color");
        }
        
        // Reset state
        isColored = false;
        
        // Clear playersOnButton when button resets (only if no players are actually on it)
        // This ensures clean state. If players are still on, they'll re-trigger OnTriggerEnter
        if (playersOnButton.Count == 0)
        {
            playersOnButton.Clear();
            Debug.Log("[ButtonCollider] ResetButtonAfterDelay: Cleared playersOnButton HashSet (was empty)");
        }
        else
        {
            Debug.LogWarning($"[ButtonCollider] ResetButtonAfterDelay: playersOnButton still has {playersOnButton.Count} player(s)! Not clearing.");
        }
        
        resetCoroutine = null;
    }
    
    // ============================================================================
    // STEP 3: Calculate Target Positions
    // ============================================================================
    // Get the top Y position of objects using their mesh renderer bounds.
    // We use bounds.max.y to get the highest Y value of the rendered mesh.
    // This is what we care about for the visual sinking effect.
    // ============================================================================
    
    /// <summary>
    /// Gets the top Y position of a GameObject using its mesh renderer bounds.
    /// Returns the highest Y value of the mesh (bounds.max.y).
    /// </summary>
    /// <param name="renderer">The Renderer component to get bounds from</param>
    /// <param name="objectName">Name of the object (for debug logging)</param>
    private float GetTopY(Renderer renderer, string objectName)
    {
        if (renderer == null)
        {
            Debug.LogError($"[ButtonCollider] STEP 3: {objectName} has no Renderer component! Cannot get bounds.");
            return 0f;
        }
        
        float topY = renderer.bounds.max.y;
        Debug.Log($"[ButtonCollider] STEP 3: {objectName} top Y = {topY} (from bounds.max.y)");
        return topY;
    }
    
    /// <summary>
    /// Gets the top Y position of the Base cylinder.
    /// </summary>
    private float GetBaseTopY()
    {
        return GetTopY(baseRenderer, "Base");
    }
    
    /// <summary>
    /// Gets the top Y position of the Trigger cylinder.
    /// </summary>
    private float GetTriggerTopY()
    {
        return GetTopY(triggerRenderer, "Trigger");
    }
    
    // ============================================================================
    // STEP 5 & 6: Unified Trigger Movement
    // ============================================================================
    // Unified function to animate the Trigger cylinder to any target Y position.
    // Both sinking and rising use the same animation logic, just different targets.
    // ============================================================================
    
    /// <summary>
    /// Unified coroutine that smoothly moves the Trigger cylinder to a target Y position.
    /// Used for both sinking (to Base top) and rising (to original position).
    /// </summary>
    /// <param name="targetY">Target Y position to move the Trigger to</param>
    /// <param name="animationName">Name of animation for debug logging (e.g., "sink" or "rise")</param>
    private IEnumerator MoveTrigger(float targetY, string animationName)
    {
        Debug.LogError($"[ButtonCollider] ===== MoveTrigger CALLED (ERROR LOG) ===== Animation: {animationName}, Target Y: {targetY}");
        Debug.Log($"[ButtonCollider] ===== MoveTrigger CALLED ===== Animation: {animationName}, Target Y: {targetY}");
        
        if (triggerObject == null)
        {
            Debug.LogError($"[ButtonCollider] MoveTrigger: triggerObject is null! Cannot {animationName}.");
            yield break;
        }
        
        Transform triggerTransform = triggerObject.transform;
        float startY = triggerTransform.position.y;
        
        Debug.Log($"[ButtonCollider] MoveTrigger: Starting {animationName}. Start Y: {startY}, Target Y: {targetY}");
        
        // Animate using lerp
        float elapsedTime = 0f;
        float distance = Mathf.Abs(targetY - startY);
        float duration = distance / sinkSpeed; // Time needed based on speed
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // Smooth lerp from start to target
            float newY = Mathf.Lerp(startY, targetY, t);
            triggerTransform.position = new Vector3(
                triggerTransform.position.x,
                newY,
                triggerTransform.position.z
            );
            
            yield return null; // Wait one frame
        }
        
        // Ensure we end exactly at target position
        triggerTransform.position = new Vector3(
            triggerTransform.position.x,
            targetY,
            triggerTransform.position.z
        );
        
        Debug.Log($"[ButtonCollider] MoveTrigger: {animationName} complete! Trigger at Y: {targetY}");
    }
    
    /// <summary>
    /// Coroutine wrapper that sinks the Trigger cylinder down until its top
    /// aligns with the Base cylinder's top.
    /// </summary>
    private IEnumerator SinkTrigger()
    {
        if (triggerObject == null)
        {
            Debug.LogError("[ButtonCollider] SinkTrigger: triggerObject is null!");
            isSinking = false;
            yield break;
        }
        
        // Calculate target Y position
        // We want Trigger's top to align with Base's top
        float currentTriggerTopY = GetTriggerTopY();
        float baseTopY = GetBaseTopY();
        float topDifference = currentTriggerTopY - baseTopY;
        float startY = triggerObject.transform.position.y;
        float targetY = startY - topDifference;
        
        isSinking = true;
        yield return StartCoroutine(MoveTrigger(targetY, "sink"));
        
        // Log state just before exiting SinkTrigger
        Debug.Log($"[ButtonCollider] SinkTrigger: About to exit. Players on button: {playersOnButton.Count}, isSinking: {isSinking}, isRising: {isRising}");
        
        isSinking = false;
        lastSinkCompleteTime = Time.time; // Mark when sinking completed (for grace period)
        sinkCoroutine = null;
        
        // Log state just after exiting SinkTrigger
        Debug.Log($"[ButtonCollider] SinkTrigger: Exited. Players on button: {playersOnButton.Count}, isSinking: {isSinking}, isRising: {isRising}, grace period started");
    }
    
    /// <summary>
    /// Coroutine wrapper that rises the Trigger cylinder back up to its original position.
    /// </summary>
    private IEnumerator RiseTrigger()
    {
        Debug.Log($"[ButtonCollider] ===== RiseTrigger STARTED ===== Players on button: {playersOnButton.Count}, isColored: {isColored}, isSinking: {isSinking}, isRising: {isRising}");
        
        isRising = true;
        yield return StartCoroutine(MoveTrigger(originalTriggerY, "rise"));
        isRising = false;
        riseCoroutine = null;
    }
}
