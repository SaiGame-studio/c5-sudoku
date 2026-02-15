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
    private const string AUTO_NOTE_UNLOCKED_KEY = "AutoNote_Unlocked";
    private const string CLEAR_NOTES_UNLOCKED_KEY = "ClearNotes_Unlocked";
    private const string HINT_PANEL_UNLOCKED_KEY = "HintPanel_Unlocked";
    
    // Stars earned for each difficulty level (0-8)
    // Maps directly to star display: difficulty 0 = 1 star, difficulty 1 = 2 stars, etc.
    private static readonly int[] STARS_PER_DIFFICULTY = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    
    [Header("Progress Data")]
    [SerializeField] private int totalStars = 0; // Cumulative stars earned (cap at 99)
    [SerializeField] private int completedLevelCount = 0;
    
    private const int MAX_STARS = 99; // Maximum total stars
    
    [Header("Unlock")]
    [SerializeField] private bool clearNotesUnlocked = false;
    [SerializeField] private int clearNotesUnlockCost = 2;
    [SerializeField] private bool autoNoteUnlocked = false;
    [SerializeField] private int autoNoteUnlockCost = 16;
    [SerializeField] private bool hintPanelUnlocked = false;
    [SerializeField] private int hintPanelUnlockCost = 50;
    
    [Header("Completed Levels")]
    [SerializeField] private List<LevelCompletionData> completedLevelsList = new List<LevelCompletionData>();
    
    // Internal dictionary for fast lookup
    private Dictionary<string, int> completedLevels;
    
    protected override void Awake()
    {
        base.Awake();
        this.completedLevels = new Dictionary<string, int>();
        this.Load();
        this.autoNoteUnlocked = this.IsAutoNoteUnlocked();
        this.clearNotesUnlocked = this.IsClearNotesUnlocked();
        this.hintPanelUnlocked = this.IsHintPanelUnlocked();
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
            }
        }
        
        this.completedLevelCount = this.completedLevels.Count;
        this.autoNoteUnlocked = this.IsAutoNoteUnlocked();
        this.clearNotesUnlocked = this.IsClearNotesUnlocked();
        this.hintPanelUnlocked = this.IsHintPanelUnlocked();
        
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
    /// Stars are awarded EVERY time level is won, even if already completed
    /// </summary>
    /// <param name="levelNumber">Global level number (1-23)</param>
    /// <param name="difficulty">Actual difficulty played (0-8)</param>
    public void CompleteLevel(int levelNumber, int difficulty)
    {
        if (levelNumber < 1 || levelNumber > 23)
        {
            Debug.LogError($"[GameProgress] Invalid level number: {levelNumber}. Must be between 1 and 23.");
            return;
        }
        
        if (difficulty < 0 || difficulty > 8)
        {
            Debug.LogError($"[GameProgress] Invalid difficulty: {difficulty}. Must be between 0 and 8.");
            return;
        }
        
        string key = this.GetLevelKey(levelNumber);
        int starsToAward = GetStarsForDifficulty(difficulty);
        
        bool isNewCompletion = !this.completedLevels.ContainsKey(key);
        
        // Mark level as completed (store stars for reference)
        if (isNewCompletion)
        {
            this.completedLevels[key] = starsToAward;
        }
        else
        {
            // Keep the max stars for this specific level (for reference)
            int oldStars = this.completedLevels[key];
            this.completedLevels[key] = Mathf.Max(oldStars, starsToAward);
        }
        
        // Calculate how many stars can be awarded (don't increment yet - let animation do it)
        int previousTotal = this.totalStars;
        int targetTotal = Mathf.Min(this.totalStars + starsToAward, MAX_STARS);
        int actualStarsAwarded = targetTotal - previousTotal;
        
        if (actualStarsAwarded > 0)
        {
            Debug.Log($"[GameProgress] Level {levelNumber} won! Difficulty: {GameData.DIFFICULTY_NAMES[difficulty]}, Will award: +{actualStarsAwarded} stars (from {previousTotal} to {targetTotal}/{MAX_STARS})");
            
            // Trigger flying stars animation - it will increment stars as each one explodes
            FlyingStarAnimation.PlayAnimation(actualStarsAwarded, previousTotal, targetTotal, MAX_STARS);
        }
        else
        {
            Debug.Log($"[GameProgress] Level {levelNumber} won! Max stars ({MAX_STARS}) already reached.");
            this.UpdateInspectorData();
            this.Save();
        }
    }
    
    /// <summary>
    /// Increment total stars by specified amount (called by animation system)
    /// </summary>
    public void IncrementTotalStars(int amount)
    {
        int oldValue = this.totalStars;
        this.totalStars = Mathf.Min(this.totalStars + amount, MAX_STARS);
        
        if (this.totalStars != oldValue)
        {
            this.UpdateInspectorData();
            this.Save();
        }
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
    /// Get total cumulative stars earned (capped at 99)
    /// </summary>
    public int GetTotalStars()
    {
        return this.totalStars;
    }
    
    /// <summary>
    /// Get maximum possible stars
    /// </summary>
    public int GetMaxStars()
    {
        return MAX_STARS;
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
        this.totalStars = 0;
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
                totalStars = this.totalStars,
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
                
                // Load total stars (cumulative)
                this.totalStars = saveData.totalStars;
                
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
                this.totalStars = 0;
                this.UpdateInspectorData();
                Debug.Log("[GameProgress] No saved data found, starting fresh");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameProgress] Failed to load game progress: {e.Message}");
            this.completedLevels.Clear();
            this.totalStars = 0;
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
    
    #region Auto Note Unlock
    
    /// <summary>
    /// Check if AutoNote feature has been permanently unlocked
    /// </summary>
    public bool IsAutoNoteUnlocked()
    {
        return PlayerPrefs.GetInt(AUTO_NOTE_UNLOCKED_KEY, 0) == 1;
    }
    
    /// <summary>
    /// Get the star cost to unlock AutoNote
    /// </summary>
    public int GetAutoNoteUnlockCost()
    {
        return this.autoNoteUnlockCost;
    }
    
    /// <summary>
    /// Attempt to unlock AutoNote by spending stars. Returns true if successful.
    /// </summary>
    public bool TryUnlockAutoNote()
    {
        if (this.IsAutoNoteUnlocked()) return true;
        
        if (this.totalStars < this.autoNoteUnlockCost) return false;
        
        this.totalStars -= this.autoNoteUnlockCost;
        PlayerPrefs.SetInt(AUTO_NOTE_UNLOCKED_KEY, 1);
        this.autoNoteUnlocked = true;
        
        this.UpdateInspectorData();
        this.Save();
        
        return true;
    }
    
    /// <summary>
    /// Check if player can afford to unlock AutoNote
    /// </summary>
    public bool CanAffordAutoNoteUnlock()
    {
        return this.totalStars >= this.autoNoteUnlockCost;
    }
    
    #endregion
    
    #region Clear Notes Unlock
    
    /// <summary>
    /// Check if Clear Notes feature has been permanently unlocked
    /// </summary>
    public bool IsClearNotesUnlocked()
    {
        return PlayerPrefs.GetInt(CLEAR_NOTES_UNLOCKED_KEY, 0) == 1;
    }
    
    /// <summary>
    /// Get the star cost to unlock Clear Notes
    /// </summary>
    public int GetClearNotesUnlockCost()
    {
        return this.clearNotesUnlockCost;
    }
    
    /// <summary>
    /// Attempt to unlock Clear Notes by spending stars. Returns true if successful.
    /// </summary>
    public bool TryUnlockClearNotes()
    {
        if (this.IsClearNotesUnlocked()) return true;
        
        if (this.totalStars < this.clearNotesUnlockCost) return false;
        
        this.totalStars -= this.clearNotesUnlockCost;
        PlayerPrefs.SetInt(CLEAR_NOTES_UNLOCKED_KEY, 1);
        this.clearNotesUnlocked = true;
        
        this.UpdateInspectorData();
        this.Save();
        
        return true;
    }
    
    /// <summary>
    /// Check if player can afford to unlock Clear Notes
    /// </summary>
    public bool CanAffordClearNotesUnlock()
    {
        return this.totalStars >= this.clearNotesUnlockCost;
    }
    
    #endregion
    
    #region Hint Panel Unlock
    
    /// <summary>
    /// Check if Hint Panel feature has been permanently unlocked
    /// </summary>
    public bool IsHintPanelUnlocked()
    {
        return PlayerPrefs.GetInt(HINT_PANEL_UNLOCKED_KEY, 0) == 1;
    }
    
    /// <summary>
    /// Get the star cost to unlock Hint Panel
    /// </summary>
    public int GetHintPanelUnlockCost()
    {
        return this.hintPanelUnlockCost;
    }
    
    /// <summary>
    /// Attempt to unlock Hint Panel by spending stars. Returns true if successful.
    /// </summary>
    public bool TryUnlockHintPanel()
    {
        if (this.IsHintPanelUnlocked()) return true;
        
        if (this.totalStars < this.hintPanelUnlockCost) return false;
        
        this.totalStars -= this.hintPanelUnlockCost;
        PlayerPrefs.SetInt(HINT_PANEL_UNLOCKED_KEY, 1);
        this.hintPanelUnlocked = true;
        
        this.UpdateInspectorData();
        this.Save();
        
        return true;
    }
    
    /// <summary>
    /// Check if player can afford to unlock Hint Panel
    /// </summary>
    public bool CanAffordHintPanelUnlock()
    {
        return this.totalStars >= this.hintPanelUnlockCost;
    }
    
    #endregion
    
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
        // Sync unlock Inspector values back to PlayerPrefs
        PlayerPrefs.SetInt(AUTO_NOTE_UNLOCKED_KEY, this.autoNoteUnlocked ? 1 : 0);
        PlayerPrefs.SetInt(CLEAR_NOTES_UNLOCKED_KEY, this.clearNotesUnlocked ? 1 : 0);
        PlayerPrefs.SetInt(HINT_PANEL_UNLOCKED_KEY, this.hintPanelUnlocked ? 1 : 0);
        
        Debug.Log($"[GameProgress] Force saving progress... AutoNote unlocked: {this.autoNoteUnlocked}, ClearNotes unlocked: {this.clearNotesUnlocked}, HintPanel unlocked: {this.hintPanelUnlocked}");
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
        public int totalStars; // Cumulative stars earned
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
