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
    public void LoadClassicHome()
    {
        SceneManager.LoadScene(this.classicHomeSceneName);
    }

    /// <summary>
    /// Load Classic Game Scene with level and difficulty parameters
    /// </summary>
    public void LoadClassicGame(int level, int difficulty, string levelName = "")
    {
        GameData.SelectedLevel = level;
        GameData.SelectedDifficulty = difficulty;
        GameData.SelectedLevelName = string.IsNullOrEmpty(levelName) ? $"level-{this.CalculateLevelNumber(level, difficulty)}" : levelName;
        SceneManager.LoadScene(this.classicGameSceneName);
    }

    /// <summary>
    /// Calculate level number (1-23) from difficulty and sub-level
    /// </summary>
    private int CalculateLevelNumber(int subLevel, int difficulty)
    {
        if (difficulty >= 0 && difficulty <= 6)
        {
            return difficulty * 3 + subLevel;
        }
        else if (difficulty == 7)
        {
            return 22;
        }
        else if (difficulty == 8)
        {
            return 23;
        }
        return 1;
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

    #region Version
    /// <summary>
    /// Returns the application version from Project Settings
    /// </summary>
    public string GetVersion()
    {
        return $"v{Application.version}";
    }
    #endregion

    #region Game Control
    /// <summary>
    /// Quit Game
    /// </summary>

    [ProButton]
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    #endregion
}
