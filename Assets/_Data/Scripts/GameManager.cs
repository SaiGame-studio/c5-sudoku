using com.cyborgAssets.inspectorButtonPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SaiSingleton<GameManager>
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "main_menu";
    [SerializeField] private string classicHomeSceneName = "classic_home";
    [SerializeField] private string classicGameSceneName = "classic_game";
    [SerializeField] private string storySceneName = "story_home";

    #region Scene Management
    [ProButton]
    /// <summary>
    /// Load Classic Home Scene
    /// </summary>
    public void LoadClassicScene()
    {
        SceneManager.LoadScene(this.classicHomeSceneName);
    }

    [ProButton]
    /// <summary>
    /// Load Classic Game Scene with level and difficulty parameters
    /// </summary>
    public void LoadClassicGame(int level, int difficulty)
    {
        GameData.SelectedLevel = level;
        GameData.SelectedDifficulty = difficulty;
        SceneManager.LoadScene(this.classicGameSceneName);
    }

    [ProButton]
    /// <summary>
    /// Load Adventure Scene
    /// </summary>
    public void LoadStoryScene()
    {
        SceneManager.LoadScene(this.storySceneName);
    }

    [ProButton]
    /// <summary>
    /// Return to Main Menu
    /// </summary>
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(this.mainMenuSceneName);
    }
    #endregion

    #region Game Control
    /// <summary>
    /// Quit Game
    /// </summary>

    [ProButton]
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    #endregion
}
