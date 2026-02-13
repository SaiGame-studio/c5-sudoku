using System;
using System.Collections.Generic;
using UnityEngine;
using com.cyborgAssets.inspectorButtonPro;

/// <summary>
/// Manages game progress including level completion and stars earned
/// </summary>
public class GameProgress : SaiSingleton<GameProgress>
{
    private const string SAVE_KEY = "GameProgress_Save";
    
    // Stars earned for each difficulty level (0-8)
    private static readonly int[] STARS_PER_DIFFICULTY = { 1, 2, 3, 4, 3, 3, 4, 8, 9 };
    
    [Header("Progress Data")]
    [SerializeField] private int totalStars = 0;
    [SerializeField] private int completedLevelCount = 0;
    
    [Header("Completed Levels")]
    [SerializeField] private List<LevelCompletionData> completedLevelsList = new List<LevelCompletionData>();
    
    // Internal dictionary for fast lookup
    private Dictionary<string, int> completedLevels;
    
    protected override void Awake()
    {
        base.Awake();
        this.completedLevels = new Dictionary<string, int>();
        this.Load();
        Debug.Log("[GameProgress] Initialized and loaded progress from PlayerPrefs");
    }
    
    /// <summary>
    /// Auto save when application is paused (important for mobile)
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            this.Save();
            Debug.Log("[GameProgress] Auto-saved on application pause");
        }
    }
    
    /// <summary>
    /// Auto save when application is about to quit
    /// </summary>
    private void OnApplicationQuit()
    {
        this.Save();
        Debug.Log("[GameProgress] Auto-saved on application quit");
    }
    
    /// <summary>
    /// Ensure save before GameObject is destroyed
    /// </summary>
    private void OnDestroy()
    {
        if (this == Instance)
        {
            this.Save();
            Debug.Log("[GameProgress] Auto-saved on destroy");
        }
    }
    
    /// <summary>
    /// Update Inspector visible data from internal dictionary
    /// </summary>
    private void UpdateInspectorData()
    {
        this.completedLevelsList.Clear();
        this.totalStars = 0;
        
        foreach (var kvp in this.completedLevels)
        {
            // Extract level number from key (e.g., "level_15" -> 15)
            string[] parts = kvp.Key.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[1], out int levelNumber))
            {
                int difficulty = this.GetDifficultyFromLevelNumber(levelNumber);
                this.completedLevelsList.Add(new LevelCompletionData
                {
                    level = levelNumber,
                    difficulty = difficulty,
                    stars = kvp.Value,
                    difficultyName = GameData.DIFFICULTY_NAMES[difficulty]
                });
                this.totalStars += kvp.Value;
            }
        }
        
        this.completedLevelCount = this.completedLevels.Count;
        
        // Sort by level number for better Inspector view
        this.completedLevelsList.Sort((a, b) => a.level.CompareTo(b.level));
    }
    
    /// <summary>
    /// Get stars awarded for completing a specific difficulty
    /// </summary>
    public static int GetStarsForDifficulty(int difficulty)
    {
        if (difficulty < 0 || difficulty >= STARS_PER_DIFFICULTY.Length)
        {
            return 0;
        }
        return STARS_PER_DIFFICULTY[difficulty];
    }
    
    /// <summary>
    /// Mark a level as completed and award stars
    /// </summary>
    /// <param name="levelNumber">Global level number (1-23)</param>
    public void CompleteLevel(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > 23)
        {
            Debug.LogError($"[GameProgress] Invalid level number: {levelNumber}. Must be between 1 and 23.");
            return;
        }
        
        string key = this.GetLevelKey(levelNumber);
        int difficulty = this.GetDifficultyFromLevelNumber(levelNumber);
        int stars = GetStarsForDifficulty(difficulty);
        
        bool isNewCompletion = !this.completedLevels.ContainsKey(key);
        
        // Store the stars earned for this level
        if (isNewCompletion)
        {
            this.completedLevels[key] = stars;
            Debug.Log($"[GameProgress] Level {levelNumber} completed! Difficulty: {GameData.DIFFICULTY_NAMES[difficulty]}, Stars: {stars}");
        }
        else
        {
            // Keep the existing stars (don't override)
            int oldStars = this.completedLevels[key];
            this.completedLevels[key] = Mathf.Max(oldStars, stars);
            if (this.completedLevels[key] > oldStars)
            {
                Debug.Log($"[GameProgress] Level {levelNumber} improved! Stars: {oldStars} -> {this.completedLevels[key]}");
            }
        }
        
        this.UpdateInspectorData();
        this.Save();
    }
    
    /// <summary>
    /// Check if a level has been completed
    /// </summary>
    /// <param name="levelNumber">Global level number (1-23)</param>
    public bool IsLevelCompleted(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > 23) return false;
        
        string key = this.GetLevelKey(levelNumber);
        return this.completedLevels.ContainsKey(key);
    }
    
    /// <summary>
    /// Get stars earned for a specific level
    /// </summary>
    /// <param name="levelNumber">Global level number (1-23)</param>
    public int GetLevelStars(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > 23) return 0;
        
        string key = this.GetLevelKey(levelNumber);
        return this.completedLevels.ContainsKey(key) ? this.completedLevels[key] : 0;
    }
    
    /// <summary>
    /// Get total stars earned across all levels
    /// </summary>
    public int GetTotalStars()
    {
        int total = 0;
        foreach (var stars in this.completedLevels.Values)
        {
            total += stars;
        }
        return total;
    }
    
    /// <summary>
    /// Get total number of completed levels
    /// </summary>
    public int GetCompletedLevelCount()
    {
        return this.completedLevels.Count;
    }
    
    /// <summary>
    /// Check if a level is unlocked (for sequential unlocking)
    /// First level is always unlocked, others unlock after completing previous level
    /// </summary>
    /// <param name="levelNumber">Global level number (1-23)</param>
    public bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > 23) return false;
        
        // First level is always unlocked
        if (levelNumber == 1)
        {
            return true;
        }
        
        // Check if previous level is completed
        return this.IsLevelCompleted(levelNumber - 1);
    }
    
    /// <summary>
    /// Reset all progress
    /// </summary>
    [ProButton]
    public void ResetProgress()
    {
        int previousCount = this.completedLevels.Count;
        int previousStars = this.totalStars;
        
        this.completedLevels.Clear();
        this.UpdateInspectorData();
        this.Save();
        
        Debug.Log($"[GameProgress] Progress reset! Cleared {previousCount} levels and {previousStars} stars");
    }
    
    /// <summary>
    /// Save progress to PlayerPrefs
    /// </summary>
    public void Save()
    {
        try
        {
            SaveData saveData = new SaveData
            {
                completedLevels = new List<LevelData>()
            };
            
            foreach (var kvp in this.completedLevels)
            {
                // Extract level number from key (e.g., "level_15" -> 15)
                string[] parts = kvp.Key.Split('_');
                if (parts.Length == 2 && int.TryParse(parts[1], out int levelNumber))
                {
                    saveData.completedLevels.Add(new LevelData
                    {
                        level = levelNumber,
                        stars = kvp.Value
                    });
                }
            }
            
            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            
            Debug.Log($"[GameProgress] Saved to PlayerPrefs: {this.completedLevelCount} levels, {this.totalStars} total stars");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameProgress] Failed to save game progress: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load progress from PlayerPrefs
    /// </summary>
    public void Load()
    {
        try
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);
                
                this.completedLevels.Clear();
                foreach (var levelData in saveData.completedLevels)
                {
                    // New format: level number directly stored
                    if (levelData.level > 0)
                    {
                        string key = this.GetLevelKey(levelData.level);
                        this.completedLevels[key] = levelData.stars;
                    }
                    // Backward compatibility: convert old (difficulty, level) format
                    else if (levelData.difficulty >= 0)
                    {
                        // Calculate global level number from old format
                        int globalLevel;
                        if (levelData.difficulty <= 6)
                        {
                            globalLevel = levelData.difficulty * 3 + levelData.level;
                        }
                        else if (levelData.difficulty == 7)
                        {
                            globalLevel = 22; // Extreme
                        }
                        else
                        {
                            globalLevel = 23; // Legendary
                        }
                        
                        string key = this.GetLevelKey(globalLevel);
                        this.completedLevels[key] = levelData.stars;
                        Debug.Log($"[GameProgress] Converted old format: Difficulty {levelData.difficulty}, SubLevel {levelData.level} -> Level {globalLevel}");
                    }
                }
                
                this.UpdateInspectorData();
                Debug.Log($"[GameProgress] Loaded from PlayerPrefs: {this.completedLevelCount} levels, {this.totalStars} total stars");
            }
            else
            {
                this.UpdateInspectorData();
                Debug.Log("[GameProgress] No saved data found, starting fresh");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameProgress] Failed to load game progress: {e.Message}");
            this.completedLevels.Clear();
            this.UpdateInspectorData();
        }
    }
    
    /// <summary>
    /// Generate unique key for level number
    /// </summary>
    /// <param name="levelNumber">Global level number (1-23)</param>
    private string GetLevelKey(int levelNumber)
    {
        return $"level_{levelNumber}";
    }
    
    /// <summary>
    /// Get difficulty index from global level number
    /// </summary>
    /// <param name="levelNumber">Global level number (1-23)</param>
    /// <returns>Difficulty index (0-8)</returns>
    private int GetDifficultyFromLevelNumber(int levelNumber)
    {
        // Levels 1-21: 3 levels per difficulty (0-6)
        // Level 22: Extreme (difficulty 7)
        // Level 23: Legendary (difficulty 8)
        if (levelNumber >= 1 && levelNumber <= 21)
        {
            return (levelNumber - 1) / 3;
        }
        else if (levelNumber == 22)
        {
            return 7; // Extreme
        }
        else if (levelNumber == 23)
        {
            return 8; // Legendary
        }
        
        return 0; // Default to Very Easy
    }
    
    /// <summary>
    /// Extract level number from level name (e.g., "level-15" -> 15)
    /// </summary>
    public static int ParseLevelName(string levelName)
    {
        if (string.IsNullOrEmpty(levelName)) return 1;
        
        string[] parts = levelName.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[1], out int levelNumber))
        {
            return Mathf.Clamp(levelNumber, 1, 23);
        }
        
        return 1; // Default to level 1
    }
    
    #region Debug & Utility Methods
    /// <summary>
    /// Delete all saved progress from PlayerPrefs (for debugging/testing)
    /// </summary>
    [ProButton]
    public void DeleteSaveData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("[GameProgress] Deleted save data from PlayerPrefs");
        }
        else
        {
            Debug.Log("[GameProgress] No save data found to delete");
        }
    }
    
    /// <summary>
    /// Check if save data exists in PlayerPrefs
    /// </summary>
    public bool HasSaveData()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }
    
    /// <summary>
    /// Get save data as JSON string (for debugging)
    /// </summary>
    [ProButton]
    public string GetSaveDataJson()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            Debug.Log($"[GameProgress] Save data JSON: {json}");
            return json;
        }
        Debug.Log("[GameProgress] No save data found");
        return "No save data found";
    }
    
    /// <summary>
    /// Force reload progress from PlayerPrefs
    /// </summary>
    [ProButton]
    public void ReloadProgress()
    {
        Debug.Log("[GameProgress] Force reloading progress...");
        this.Load();
    }
    
    /// <summary>
    /// Force save current progress to PlayerPrefs
    /// </summary>
    [ProButton]
    public void ForceSave()
    {
        Debug.Log("[GameProgress] Force saving progress...");
        this.Save();
    }
    #endregion
    
    #region Serialization Classes
    /// <summary>
    /// Data class for displaying level completion in Inspector
    /// </summary>
    [Serializable]
    public class LevelCompletionData
    {
        public int level;           // Global level number (1-23)
        public int difficulty;      // Difficulty index (0-8) - calculated from level
        public string difficultyName;
        public int stars;
    }
    
    [Serializable]
    private class SaveData
    {
        public List<LevelData> completedLevels;
    }
    
    [Serializable]
    private class LevelData
    {
        public int level;       // Global level number (1-23)
        public int difficulty;  // Kept for backward compatibility (not used in new format)
        public int stars;
    }
    #endregion
}
