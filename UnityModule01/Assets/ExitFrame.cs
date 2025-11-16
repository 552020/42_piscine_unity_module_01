using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(LineRenderer))]
public class ExitFrame : MonoBehaviour
{
    public MonoBehaviour targetPlayer;

    private static ExitFrame[] allExits;
    private static bool exitsInitialized = false;
    private bool isPlayerInside = false;

    void Start()
    {
        if (!exitsInitialized)
        {
            allExits = FindObjectsByType<ExitFrame>(FindObjectsSortMode.None);
            exitsInitialized = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == targetPlayer)
        {
            isPlayerInside = true;
            Debug.Log($"{targetPlayer.name} entered their exit frame");
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<PlayerController>() == targetPlayer)
        {
            isPlayerInside = true;
            CheckAllExitsComplete();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == targetPlayer)
        {
            isPlayerInside = false;
            Debug.Log($"{targetPlayer.name} left their exit frame");
        }
    }

    private static bool stageCompleteMessageShown = false;

    public static void ResetCompletionFlag()
    {
        stageCompleteMessageShown = false;
        if (allExits != null)
        {
            foreach (var exit in allExits)
            {
                if (exit != null)
                {
                    exit.isPlayerInside = false;
                }
            }
        }
    }

    void CheckAllExitsComplete()
    {
        if (allExits == null) return;

        foreach (var exit in allExits)
        {
            if (exit == null || exit.targetPlayer == null || !exit.isPlayerInside)
            {
                stageCompleteMessageShown = false;
                return;
            }
        }

        if (!stageCompleteMessageShown)
        {
            stageCompleteMessageShown = true;
            Debug.Log("STAGE COMPLETE! All characters are aligned with their exits!");
            GameUtils.LoadNextScene();
        }
    }

}
