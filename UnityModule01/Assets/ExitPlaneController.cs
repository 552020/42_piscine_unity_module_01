using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Exit plane that detects when all 3 players are standing on it.
/// When all players are inside, loads the next scene.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ExitPlane : MonoBehaviour
{
    private HashSet<PlayerController> playersInside = new HashSet<PlayerController>();
    private const int totalPlayers = 3;

    void Start()
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null && !collider.isTrigger)
        {
            Debug.LogWarning($"[ExitPlane] '{gameObject.name}': BoxCollider is not set as trigger! Setting it now.");
            collider.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            playersInside.Add(player);
            Debug.Log($"{player.name} entered the exit plane. Players inside: {playersInside.Count}/{totalPlayers}");
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (playersInside.Count == totalPlayers)
        {
            Debug.Log("All players are on the exit plane! Loading next scene...");
            GameUtils.LoadNextScene();
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            playersInside.Remove(player);
            Debug.Log($"{player.name} left the exit plane. Players inside: {playersInside.Count}/{totalPlayers}");
        }
    }
}
