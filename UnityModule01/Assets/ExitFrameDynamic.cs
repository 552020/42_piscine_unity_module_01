using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Dynamic version of ExitFrame that auto-configures BoxCollider and LineRenderer
/// based on inspector values and player size. Useful for prototyping and quick setup.
/// For production, use the simpler ExitFrame and configure components manually.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(LineRenderer))]
public class ExitFrameDynamic : MonoBehaviour
{
    public Vector2 size = new Vector2(1f, 1f);   // width, height
    public float padding = 1.5f;                  // padding around player size
    public MonoBehaviour targetPlayer;             // PlayerController to match size with
    public float lineWidth = 0.05f;
    public Color color = Color.white;

    BoxCollider box;
    LineRenderer lr;

    // Static tracking for all exits
    private static ExitFrameDynamic[] allExits;
    private static bool exitsInitialized = false;
    private bool isPlayerInside = false;

    void OnEnable()
    {
        if (targetPlayer != null) CalculateSizeFromPlayer();
        Apply();
    }

    void OnValidate()
    {
        if (targetPlayer != null) CalculateSizeFromPlayer();
        Apply();
    }

    void Start()
    {
        if (targetPlayer != null) CalculateSizeFromPlayer();
        Apply();

        if (!exitsInitialized)
        {
            allExits = FindObjectsByType<ExitFrameDynamic>(FindObjectsSortMode.None);
            exitsInitialized = true;
        }
    }

    void CalculateSizeFromPlayer()
    {
        Renderer r = targetPlayer.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            Vector3 playerSize = r.bounds.size;
            size.x = playerSize.x + padding;
            size.y = playerSize.y + padding;
        }
    }

    void Apply()
    {
        if (!box) box = GetComponent<BoxCollider>();
        if (!lr) lr = GetComponent<LineRenderer>();

        // trigger volume that the player must stand in
        box.isTrigger = true;
        box.size = new Vector3(size.x, size.y, 0.2f);
        box.center = new Vector3(0, size.y * 0.5f, 0);

        // simple rectangular outline
        lr.useWorldSpace = false;
        lr.positionCount = 5;
        lr.startWidth = lr.endWidth = lineWidth;
        lr.loop = false;

        // Handle material
        if (lr.sharedMaterial == null)
        {
            Material newMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lr.sharedMaterial = newMat;
        }

        // Set color
        if (Application.isPlaying)
        {
            lr.material.color = color;
        }
        else
        {
            if (lr.sharedMaterial != null)
            {
                lr.sharedMaterial.color = color;
            }
        }

        float hx = size.x * 0.5f;
        lr.SetPositions(new Vector3[] {
            new(-hx, 0, 0),           // bottom-left
            new(hx, 0, 0),            // bottom-right
            new(hx, size.y, 0),       // top-right
            new(-hx, size.y, 0),      // top-left
            new(-hx, 0, 0)            // back to bottom-left
        });
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == targetPlayer)
            isPlayerInside = true;
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
            isPlayerInside = false;
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
