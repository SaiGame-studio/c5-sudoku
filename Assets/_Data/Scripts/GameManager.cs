using com.cyborgAssets.inspectorButtonPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SaiSingleton<GameManager>
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "main_menu";
    [SerializeField] private string classicSceneName = "classic_home";
    [SerializeField] private string adventureSceneName = "adventure_home";

    #region Scene Management
    [ProButton]
    /// <summary>
    /// Load Classic Scene
    /// </summary>
    public void LoadClassicScene()
    {
        Debug.Log("Loading Classic Scene...");
        SceneManager.LoadScene(classicSceneName);
    }

    [ProButton]
    /// <summary>
    /// Load Adventure Scene
    /// </summary>
    public void LoadAdventureScene()
    {
        Debug.Log("Loading Adventure Scene...");
        SceneManager.LoadScene(adventureSceneName);
    }

    [ProButton]
    /// <summary>
    /// Return to Main Menu
    /// </summary>
    public void LoadMainMenu()
    {
        Debug.Log("Loading Main Menu...");
        SceneManager.LoadScene(mainMenuSceneName);
    }
    #endregion

    #region Game Control
    /// <summary>
    /// Quit Game
    /// </summary>
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
