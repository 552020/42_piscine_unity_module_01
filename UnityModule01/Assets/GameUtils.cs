using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameUtils
{
    public static void LoadNextScene()
    {
        // Get the index of the currently active scene
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        
        // Calculate the next scene index (increment by 1)
        int nextSceneIndex = currentSceneIndex + 1;
        
        // Get the total number of scenes in the build settings
        int totalScenesInBuild = SceneManager.sceneCountInBuildSettings;
        
        // Use modulo to wrap around: if we exceed the last scene, loop back to scene 0
        int wrappedSceneIndex = nextSceneIndex % totalScenesInBuild;
        
        // Load the calculated scene
        SceneManager.LoadScene(wrappedSceneIndex);
    }
}