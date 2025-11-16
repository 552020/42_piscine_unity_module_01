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

                // Use average Y and Z of Start and Exit
                float pathAvgY = (startPos.y + exitPos.y) / 2f;
                float pathAvgZ = (startPos.z + exitPos.z) / 2f;

                // Calculate distance needed to see the whole path
                // Since camera looks along X axis, we need to position it at the right distance
                // to frame the path length along X as top-bottom in view
                Camera pathCam = GetComponent<Camera>();
                float pathFov = pathCam != null ? pathCam.fieldOfView : 60f;
                float pathAspect = pathCam != null ? pathCam.aspect : 16f / 9f;

                // Use vertical FOV to calculate distance needed to see path length
                float pathVertFov = pathFov;
                float pathHalfVertFovRad = (pathVertFov * 0.5f) * Mathf.Deg2Rad;
                // Distance = (pathLength / 2) / tan(halfVertFov)
                // Add some padding (multiply by 1.2 for extra space)
                float pathDistance = (pathLength * 0.5f / Mathf.Tan(pathHalfVertFovRad)) * 1.2f;
                // Ensure minimum distance
                pathDistance = Mathf.Max(pathDistance, 15f);

                // Position camera behind Start, elevated and to the side to see whole path
                // Camera should be at a distance that frames the path vertically
                float cameraX = startPos.x - 2f; // Slightly behind Start
                float cameraY = pathAvgY + pathDistance * 0.3f; // Elevated but not too much
                float cameraZ = pathAvgZ - pathDistance * 0.5f; // To the side, at calculated distance

                overviewPosition = new Vector3(cameraX, cameraY, cameraZ);

                // Camera should look at the center of the path
                Vector3 lookAtPoint = new Vector3((startPos.x + exitPos.x) / 2f, pathAvgY, pathAvgZ);
                Vector3 lookDirection = (lookAtPoint - overviewPosition).normalized;
                overviewRotation = Quaternion.LookRotation(lookDirection);

                Debug.Log($"CameraController: Overview calculated. Camera at X: {cameraX} (behind Start), Y: {cameraY}, Z: {cameraZ}. Path X: {startPos.x} to {exitPos.x}, Length: {pathLength}, Distance: {pathDistance}");
                return;
            }
            else
            {
                Debug.LogWarning("CameraController: Start or Exit GameObject not found in Stage4! Falling back to player-based calculation.");
            }
        }

        // Fallback: Calculate based on player positions (for other scenes or if Start/Exit not found)
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float playerAvgY = 0f;
        int playerCount = 0;

        // Check PlayerController type
        if (players != null && players.Length > 0)
        {
            foreach (PlayerController p in players)
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
        // Use PlayerController active player only
        MonoBehaviour activePlayer = PlayerController.GetActivePlayer();

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
        // Camera moves parallel to Z axis (maintains X and Y relative to player, moves along Z)
        Transform t = activePlayer.transform;
        Vector3 targetPosition = new Vector3(
            t.position.x + followOffset.x,
            t.position.y + followOffset.y,
            t.position.z + followOffset.z
        );
        transform.position = targetPosition;

        // Look at the player from the camera position
        Vector3 lookDirection = (t.position - targetPosition).normalized;
        transform.rotation = Quaternion.LookRotation(lookDirection);
    }
}
