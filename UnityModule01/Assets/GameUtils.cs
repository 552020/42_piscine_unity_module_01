using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameUtils
{
    public static void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        string currentSceneName = SceneManager.GetActiveScene().name;
        int nextIndex = (currentIndex + 1) % SceneManager.sceneCountInBuildSettings;
        
        // If nextIndex is 0, we're wrapping to the first scene (last scene completed)
        if (nextIndex == 0)
        {
            Debug.Log($"[GameUtils] Last scene completed! Wrapping to first scene (index 0).");
        }
        else
        {
            Debug.Log($"[GameUtils] Loading next scene: index {nextIndex} (from current scene '{currentSceneName}' index {currentIndex})");
        }
        
        SceneManager.LoadScene(nextIndex);
    }
}

