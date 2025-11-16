using UnityEngine;
using System.Collections.Generic;

public class MovePlatformX : MonoBehaviour
{
    [SerializeField] private float minX = 6.3f;
    [SerializeField] private float maxX = 17.3f;
    [SerializeField] private float speed = 2f;

    private Vector3 startPosition;
    private Vector3 previousPosition;
    private HashSet<Player> playersOnPlatform = new HashSet<Player>();

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
        
        // Calculate the ping-pong value between 0 and (maxX - minX)
        float pingPong = Mathf.PingPong(Time.time * speed, maxX - minX);
        
        // Add the minimum X value to get the position between minX and maxX
        float newX = minX + pingPong;
        
        // Update the position (keep Y and Z the same)
        transform.position = new Vector3(newX, startPosition.y, startPosition.z);
        
        // Calculate movement delta
        Vector3 movementDelta = transform.position - previousPosition;
        
        // Move all players on the platform along with it
        foreach (Player player in playersOnPlatform)
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
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            playersOnPlatform.Add(player);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // Keep tracking players that are on the platform
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            playersOnPlatform.Add(player);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Remove player when they leave the platform
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            playersOnPlatform.Remove(player);
        }
    }
}
