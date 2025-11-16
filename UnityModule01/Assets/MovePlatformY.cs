using UnityEngine;
using System.Collections.Generic;

public class MovePlatformY : MonoBehaviour
{
    [SerializeField] private float minY = -1f;  // Absolute world Y coordinate
    [SerializeField] private float maxY = 7f;   // Absolute world Y coordinate
    [SerializeField] private float speed = 2f;

    private Vector3 startPosition;
    private Vector3 previousPosition;
    private HashSet<PlayerController> playersOnPlatform = new HashSet<PlayerController>();

    void Start()
    {
        // Store the starting position
        startPosition = transform.position;
        previousPosition = transform.position;
    }

    void Update()
    {
        // Store previous position before moving
        previousPosition = transform.position;

        // Calculate the ping-pong value between 0 and (maxY - minY)
        float pingPong = Mathf.PingPong(Time.time * speed, maxY - minY);

        // Calculate new Y position as absolute world coordinate
        float newY = minY + pingPong;

        // Update the position (keep X and Z the same)
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // Calculate movement delta
        Vector3 movementDelta = transform.position - previousPosition;

        // Move all players on the platform along with it
        foreach (PlayerController player in playersOnPlatform)
        {
            if (player != null)
            {
                // Move the player by the same delta
                player.transform.position += movementDelta;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if a player landed on the platform
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            playersOnPlatform.Add(player);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // Keep tracking players that are on the platform
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            playersOnPlatform.Add(player);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Remove player when they leave the platform
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            playersOnPlatform.Remove(player);
        }
    }
}
