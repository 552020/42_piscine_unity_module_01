using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    [Header("Camera Offsets")]
    /// <summary>
    /// Offset when following a player. More distance (Z) and more space below (Y).
    /// </summary>
    public Vector3 followOffset = new Vector3(0f, 7f, -15f);
    
    private MonoBehaviour lastActivePlayer = null;
    private Vector3 overviewPosition;
    private Quaternion overviewRotation;

    void Start()
    {
        Debug.Log("CameraController: Start() called");
        CalculateOverviewPosition();
    }

    /// <summary>
    /// Calculates the overview camera position to show the whole path along X-axis.
    /// For Stage4: uses Start and Exit GameObjects to determine path bounds.
    /// For other scenes: falls back to player-based calculation.
    /// </summary>
    void CalculateOverviewPosition()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        
        // For Stage4, use Start and Exit GameObjects
        if (sceneName == "Stage4")
        {
            GameObject startObj = GameObject.Find("Start");
            GameObject exitObj = GameObject.Find("Exit");
            
            if (startObj != null && exitObj != null)
            {
                Vector3 startPos = startObj.transform.position;
                Vector3 exitPos = exitObj.transform.position;
                
                // Calculate path length along X-axis
                float pathLength = Mathf.Abs(exitPos.x - startPos.x);
                
                // Use average Y of Start and Exit, elevated
                float pathAvgY = (startPos.y + exitPos.y) / 2f;
                
                // Calculate Y distance needed to see the whole path (top-bottom in view)
                // Use field of view to calculate required distance
                Camera pathCam = GetComponent<Camera>();
                float pathFov = pathCam != null ? pathCam.fieldOfView : 60f;
                float pathHalfFovRad = (pathFov * 0.5f) * Mathf.Deg2Rad;
                // Distance = (pathLength / 2) / tan(halfFov)
                // Add some padding (multiply by 1.2 for extra space)
                float pathYDistance = (pathLength * 0.5f / Mathf.Tan(pathHalfFovRad)) * 1.2f;
                // Ensure minimum distance
                pathYDistance = Mathf.Max(pathYDistance, 10f);
                
                // Position camera behind Start (at Start X or slightly before), elevated to see whole path
                float cameraX = startPos.x - 2f; // Slightly behind Start
                float cameraY = pathAvgY + pathYDistance; // Elevated to see whole path
                float cameraZ = pathAvgY; // Use average Y as Z offset (to the side)
                
                overviewPosition = new Vector3(cameraX, cameraY, cameraZ);
                
                // Camera should look along +X axis toward Exit
                overviewRotation = Quaternion.LookRotation(Vector3.right);
                
                Debug.Log($"CameraController: Overview calculated. Camera at X: {cameraX} (behind Start), Y: {cameraY}, Z: {cameraZ}. Path X: {startPos.x} to {exitPos.x}, Length: {pathLength}, Y Distance: {pathYDistance}");
                return;
            }
            else
            {
                Debug.LogWarning("CameraController: Start or Exit GameObject not found in Stage4! Falling back to player-based calculation.");
            }
        }
        
        // Fallback: Calculate based on player positions (for other scenes or if Start/Exit not found)
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        PlayerScene2[] playersScene2 = FindObjectsByType<PlayerScene2>(FindObjectsSortMode.None);
        
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float playerAvgY = 0f;
        int playerCount = 0;
        
        // Check Player type
        if (players != null && players.Length > 0)
        {
            foreach (Player p in players)
            {
                Vector3 pos = p.transform.position;
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                playerAvgY += pos.y;
                playerCount++;
            }
        }
        
        // Check PlayerScene2 type
        if (playersScene2 != null && playersScene2.Length > 0)
        {
            foreach (PlayerScene2 p in playersScene2)
            {
                Vector3 pos = p.transform.position;
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                playerAvgY += pos.y;
                playerCount++;
            }
        }
        
        if (playerCount == 0)
        {
            Debug.LogWarning("CameraController: No players found! Using default overview position.");
            overviewPosition = new Vector3(0f, 10f, -20f);
            overviewRotation = Quaternion.identity;
            return;
        }
        
        // Calculate center of path along X-axis
        float playerCenterX = (minX + maxX) / 2f;
        playerAvgY /= playerCount;
        
        // Calculate path length
        float playerPathLength = maxX - minX;
        
        // Calculate Z distance based on path length
        Camera cam = GetComponent<Camera>();
        float fov = cam != null ? cam.fieldOfView : 60f;
        float halfFovRad = (fov * 0.5f) * Mathf.Deg2Rad;
        float zDistance = (playerPathLength * 0.5f / Mathf.Tan(halfFovRad)) * 1.2f;
        zDistance = Mathf.Max(zDistance, 20f);
        
        // Position camera to see the whole path
        overviewPosition = new Vector3(playerCenterX, playerAvgY + 10f, -zDistance);
        
        // Calculate rotation to look at path center
        Vector3 playerLookAtTarget = new Vector3(playerCenterX, playerAvgY, 0f);
        Vector3 playerLookDirection = (playerLookAtTarget - overviewPosition).normalized;
        overviewRotation = Quaternion.LookRotation(playerLookDirection);
        
        Debug.Log($"CameraController: Overview calculated from players. Path X range: {minX} to {maxX}, Length: {playerPathLength}, Z Distance: {zDistance}");
    }

    void LateUpdate()
    {
        // Try PlayerScene2 first, then fall back to Player
        MonoBehaviour activePlayer = PlayerScene2.GetActivePlayer();
        if (activePlayer == null)
        {
            activePlayer = Player.GetActivePlayer();
        }
        
        if (activePlayer == null)
        {
            // No active player: show overview of the whole path
            if (lastActivePlayer != null)
            {
                Debug.Log("CameraController: No active player found, switching to overview");
                lastActivePlayer = null;
            }
            transform.position = overviewPosition;
            transform.rotation = overviewRotation;
            return;
        }

        // Only log when active player changes
        if (activePlayer != lastActivePlayer)
        {
            Debug.Log($"CameraController: Now following {activePlayer.name}");
            lastActivePlayer = activePlayer;
        }

        // Follow active player with adjusted offset (more distance, more space below)
        Transform t = activePlayer.transform;
        Vector3 targetPosition = t.position + followOffset;
        transform.position = targetPosition;
    }
}
