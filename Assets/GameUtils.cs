using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameUtils
{
    public static void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = (currentIndex + 1) % SceneManager.sceneCountInBuildSettings;
        
        Debug.Log($"Loading scene (index {nextIndex})");
        SceneManager.LoadScene(nextIndex);
    }
}

