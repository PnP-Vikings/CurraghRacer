using UnityEngine;

public class LevelButton : MonoBehaviour
{
    public string levelSceneName;
    public TMPro.TMP_Text levelName;
    public void SetLevelSceneName(string sceneName)
    {
        levelSceneName = sceneName;
        if (levelName != null)
        {
            levelName.text = sceneName;
        }
    }
    
    public void LoadLevel()
    {
        if (!string.IsNullOrEmpty(levelSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(levelSceneName);
        }
        else
        {
            Debug.LogError("Level scene name is not set!");
        }
    }
}
