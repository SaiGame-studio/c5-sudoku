using UnityEngine;

// Manages theme state (dark/light mode) across all scenes
public class ThemeManager : SaiSingleton<ThemeManager>
{
    private const string THEME_SAVE_KEY = "ThemeManager_IsLightMode";

    [Header("Theme State")]
    [SerializeField] private bool isLightMode;

    public bool IsLightMode => this.isLightMode;

    protected override void Awake()
    {
        base.Awake();
        
        // If this is not the singleton instance (will be destroyed), skip initialization
        if (Instance != this)
        {
            Debug.Log("[ThemeManager] Duplicate instance detected, skipping initialization");
            return;
        }
        
        this.Load();
    }

    /// <summary>
    /// Toggle between light and dark mode
    /// </summary>
    public void ToggleTheme()
    {
        this.isLightMode = !this.isLightMode;
        this.Save();
    }

    /// <summary>
    /// Returns the icon text for theme toggle button
    /// </summary>
    public string GetThemeIcon()
    {
        return this.isLightMode ? "\u2600" : "\u263E";
    }

    private void Save()
    {
        PlayerPrefs.SetInt(THEME_SAVE_KEY, this.isLightMode ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        this.isLightMode = PlayerPrefs.GetInt(THEME_SAVE_KEY, 0) == 1;
    }
}
