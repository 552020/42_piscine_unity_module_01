using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Script attached to the "exit" platform GameObject.
/// Detects when all 3 players are on the platform and loads the next scene.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ExitStage4 : MonoBehaviour
{
    // Track which players are currently inside the trigger
    private HashSet<Player> playersInside = new HashSet<Player>();
    private const int totalPlayers = 3; // Hardcoded: Claire, John, Thomas

    void Start()
    {
        // Ensure the collider is set as a trigger
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            if (!collider.isTrigger)
            {
                Debug.LogWarning($"[ExitStage4] '{gameObject.name}': BoxCollider is not set as trigger! Setting it now.");
                collider.isTrigger = true;
            }
            Debug.Log($"[ExitStage4] '{gameObject.name}' initialized. Collider size: {collider.size}, isTrigger: {collider.isTrigger}. Waiting for {totalPlayers} players to enter.");
        }
        else
        {
            Debug.LogError($"[ExitStage4] '{gameObject.name}': No BoxCollider found! Trigger detection will not work!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ExitStage4] OnTriggerEnter called - Object: '{other.gameObject.name}'");
        
        // Check if the collider belongs to a Player
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            playersInside.Add(player);
            Debug.Log($"[ExitStage4] '{player.name}' entered the exit platform. Players inside: {playersInside.Count}/{totalPlayers}");
            
            // Log all players currently inside
            string playerNames = "";
            foreach (Player p in playersInside)
            {
                playerNames += (playerNames == "" ? "" : ", ") + p.name;
            }
            Debug.Log($"[ExitStage4] Current players inside: [{playerNames}]");
        }
        else
        {
            Debug.Log($"[ExitStage4] '{other.gameObject.name}' entered but does not have Player component. Ignoring.");
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Check if all 3 players are inside
        if (playersInside.Count == totalPlayers)
        {
            Debug.Log($"[ExitStage4] ✓ ALL {totalPlayers} PLAYERS ARE ON THE EXIT PLATFORM!");
            Debug.Log($"[ExitStage4] Loading next scene...");
            GameUtils.LoadNextScene();
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[ExitStage4] OnTriggerExit called - Object: '{other.gameObject.name}'");
        
        // Check if the collider belongs to a Player
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            playersInside.Remove(player);
            Debug.Log($"[ExitStage4] '{player.name}' left the exit platform. Players inside: {playersInside.Count}/{totalPlayers}");
        }
        else
        {
            Debug.Log($"[ExitStage4] '{other.gameObject.name}' exited but does not have Player component. Ignoring.");
        }
    }
}

