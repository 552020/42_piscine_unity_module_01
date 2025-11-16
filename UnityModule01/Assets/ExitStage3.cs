using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class ExitStage3 : MonoBehaviour
{
    // Track which players are currently inside the trigger
    private HashSet<PlayerController> playersInside = new HashSet<PlayerController>();
    private const int totalPlayers = 3; // Hardcoded: Claire, John, Thomas

    void Start()
    {
        // Ensure the collider is set as a trigger
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            if (!collider.isTrigger)
            {
                Debug.LogWarning($"[ExitStage3] '{gameObject.name}': BoxCollider is not set as trigger! Setting it now.");
                collider.isTrigger = true;
            }
            Debug.Log($"[ExitStage3] '{gameObject.name}' initialized. Collider size: {collider.size}, isTrigger: {collider.isTrigger}. Waiting for {totalPlayers} players to enter.");
        }
        else
        {
            Debug.LogError($"[ExitStage3] '{gameObject.name}': No BoxCollider found! Trigger detection will not work!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ExitStage3] OnTriggerEnter called - Object: '{other.gameObject.name}'");

        // Check if the collider belongs to a Player
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            playersInside.Add(player);
            Debug.Log($"[ExitStage3] '{player.name}' entered the final platform. Players inside: {playersInside.Count}/{totalPlayers}");

            // Log all players currently inside
            string playerNames = "";
            foreach (PlayerController p in playersInside)
            {
                playerNames += (playerNames == "" ? "" : ", ") + p.name;
            }
            Debug.Log($"[ExitStage3] Current players inside: [{playerNames}]");
        }
        else
        {
            Debug.Log($"[ExitStage3] '{other.gameObject.name}' entered but does not have Player component. Ignoring.");
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Check if all 3 players are inside
        if (playersInside.Count == totalPlayers)
        {
            Debug.Log($"[ExitStage3] ✓ ALL {totalPlayers} PLAYERS ARE ON THE FINAL PLATFORM!");
            Debug.Log($"[ExitStage3] Loading next scene...");
            GameUtils.LoadNextScene();
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[ExitStage3] OnTriggerExit called - Object: '{other.gameObject.name}'");

        // Check if the collider belongs to a Player
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            playersInside.Remove(player);
            Debug.Log($"[ExitStage3] '{player.name}' left the final platform. Players inside: {playersInside.Count}/{totalPlayers}");
        }
        else
        {
            Debug.Log($"[ExitStage3] '{other.gameObject.name}' exited but does not have Player component. Ignoring.");
        }
    }
}
