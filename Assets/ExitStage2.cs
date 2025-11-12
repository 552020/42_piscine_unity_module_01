using UnityEngine;
using System.Collections.Generic;

public class ExitStage2 : MonoBehaviour
{
    // Track which players are currently inside the trigger
    private HashSet<PlayerScene2> playersInside = new HashSet<PlayerScene2>();
    private const int totalPlayers = 3; // Hardcoded: Claire, John, Thomas

    void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to a PlayerScene2
        PlayerScene2 player = other.GetComponent<PlayerScene2>();
        if (player != null)
        {
            playersInside.Add(player);
            Debug.Log($"{player.name} entered the final platform");
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Check if all 3 players are inside
        if (playersInside.Count == totalPlayers)
        {
            Debug.Log("All players are on the final platform!");
            GameUtils.LoadNextScene();
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if the collider belongs to a PlayerScene2
        PlayerScene2 player = other.GetComponent<PlayerScene2>();
        if (player != null)
        {
            playersInside.Remove(player);
            Debug.Log($"{player.name} left the final platform");
        }
    }
}
