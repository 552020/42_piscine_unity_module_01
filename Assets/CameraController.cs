using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 5f, -10f);
    
    private Player lastActivePlayer = null;

    void Start()
    {
        Debug.Log("CameraController: Start() called");
    }

    void LateUpdate()
    {
        Player activePlayer = Player.GetActivePlayer();
        if (activePlayer == null)
        {
            // Only log once when player becomes null
            if (lastActivePlayer != null)
            {
                Debug.Log("CameraController: No active player found");
                lastActivePlayer = null;
            }
            return;
        }

        // Only log when active player changes
        if (activePlayer != lastActivePlayer)
        {
            Debug.Log($"CameraController: Now following {activePlayer.name}");
            lastActivePlayer = activePlayer;
        }

        Transform t = activePlayer.transform;
        Vector3 targetPosition = t.position + offset;
        transform.position = targetPosition;
    }
}
