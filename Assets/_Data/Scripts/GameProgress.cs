using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.cyborgAssets.inspectorButtonPro;
using SaiGame.Services;

/// <summary>
/// Manages game progress including level completion and stars earned
/// </summary>
public class GameProgress : SaiSingleton<GameProgress>
{
    private const string AUTO_NOTE_UNLOCKED_KEY = "AutoNote_Unlocked";
    private const string CLEAR_NOTES_UNLOCKED_KEY = "ClearNotes_Unlocked";
    private const string HINT_PANEL_UNLOCKED_KEY = "HintPanel_Unlocked";
    private const string PATTERN_DISPLAY_UNLOCKED_KEY = "PatternDisplay_Unlocked";
    
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
    [SerializeField] private int autoNoteUsageCost = 2;
    [SerializeField] private bool hintPanelUnlocked = false;
    [SerializeField] private int hintPanelUnlockCost = 50;
    [SerializeField] private int hintUsageCost = 7;
    [SerializeField] private bool patternDisplayUnlocked = false;
    [SerializeField] private int patternDisplayUnlockCost = 50;
    
    [Header("Completed Levels")]
    [SerializeField] private List<LevelCompletionData> completedLevelsList = new List<LevelCompletionData>();
    
    // Internal dictionary for fast lookup
    private Dictionary<string, int> completedLevels;
    
    protected override void Awake()
    {
        base.Awake();
        
        // If this is not the singleton instance (will be destroyed), skip initialization
        if (Instance != this)
        {
            Debug.Log("[GameProgress] Duplicate instance detected, skipping initialization");
            return;
        }
        
        this.completedLevels = new Dictionary<string, int>();
        this.Load();
        this.autoNoteUnlocked = this.IsAutoNoteUnlocked();
        this.clearNotesUnlocked = this.IsClearNotesUnlocked();
        this.hintPanelUnlocked = this.IsHintPanelUnlocked();
        this.patternDisplayUnlocked = this.IsPatternDisplayUnlocked();
        Debug.Log("[GameProgress] Initialized and loaded progress from SaiService");
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
        this.patternDisplayUnlocked = this.IsPatternDisplayUnlocked();
        
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
        
        // Reset unlock states
        this.SetUnlockState(AUTO_NOTE_UNLOCKED_KEY, false);
        this.SetUnlockState(CLEAR_NOTES_UNLOCKED_KEY, false);
        this.SetUnlockState(HINT_PANEL_UNLOCKED_KEY, false);
        this.SetUnlockState(PATTERN_DISPLAY_UNLOCKED_KEY, false);
        
        this.autoNoteUnlocked = false;
        this.clearNotesUnlocked = false;
        this.hintPanelUnlocked = false;
        this.patternDisplayUnlocked = false;
        
        this.UpdateInspectorData();
        this.Save();
        
        Debug.Log($"[GameProgress] Progress reset! Cleared {previousCount} levels and {previousStars} stars");
    }
    
    /// <summary>
    /// Save progress to SaiService
    /// </summary>
    public void Save()
    {
        this.SaveToSaiService();
    }
    
    /// <summary>
    /// Save progress to SaiService (cloud save)
    /// </summary>
    private void SaveToSaiService()
    {
        GamerProgress gamerProgress = SaiService.Instance?.GamerProgress;
        
        if (gamerProgress == null)
        {
            Debug.LogWarning("[GameProgress] Cannot save to SaiService: GamerProgress not found");
            return;
        }
        
        if (!gamerProgress.HasProgress)
        {
            Debug.LogWarning("[GameProgress] Cannot save to SaiService: No progress data exists. Create progress first.");
            return;
        }
        
        try
        {
            SaveData saveData = new SaveData
            {
                totalStars = this.totalStars,
                completedLevels = new List<LevelData>(),
                autoNoteUnlocked = this.GetUnlockState(AUTO_NOTE_UNLOCKED_KEY),
                clearNotesUnlocked = this.GetUnlockState(CLEAR_NOTES_UNLOCKED_KEY),
                hintPanelUnlocked = this.GetUnlockState(HINT_PANEL_UNLOCKED_KEY),
                patternDisplayUnlocked = this.GetUnlockState(PATTERN_DISPLAY_UNLOCKED_KEY)
            };
            
            foreach (var kvp in this.completedLevels)
            {
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
            
            string gameDataJson = JsonUtility.ToJson(saveData);
            
            // Update progress with game_data
            gamerProgress.UpdateProgress(
                0, // experienceDelta
                0, // goldDelta
                gameDataJson,
                progress =>
                {
                    Debug.Log($"[GameProgress] Saved to SaiService: {this.completedLevelCount} levels, {this.totalStars} total stars");
                },
                error =>
                {
                    Debug.LogError($"[GameProgress] Failed to save to SaiService: {error}");
                }
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameProgress] Failed to save game progress to SaiService: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load progress from SaiService
    /// </summary>
    public void Load()
    {
        this.LoadFromSaiService();
    }
    
    /// <summary>
    /// Load progress from SaiService (cloud save)
    /// </summary>
    private void LoadFromSaiService(System.Action onSuccess = null, System.Action<string> onError = null)
    {
        GamerProgress gamerProgress = SaiService.Instance?.GamerProgress;
        
        if (gamerProgress == null)
        {
            Debug.LogWarning("[GameProgress] Cannot load from SaiService: GamerProgress not found. Starting with empty data.");
            this.totalStars = 0;
            this.completedLevels.Clear();
            this.UpdateInspectorData();
            onError?.Invoke("GamerProgress not found");
            return;
        }
        
        gamerProgress.GetProgress(
            progress =>
            {
                try
                {
                    if (string.IsNullOrEmpty(progress.game_data) || progress.game_data == "{}")
                    {
                        Debug.Log("[GameProgress] No game data in SaiService, starting fresh");
                        this.totalStars = 0;
                        this.completedLevels.Clear();
                        this.SetUnlockState(AUTO_NOTE_UNLOCKED_KEY, false);
                        this.SetUnlockState(CLEAR_NOTES_UNLOCKED_KEY, false);
                        this.SetUnlockState(HINT_PANEL_UNLOCKED_KEY, false);
                        this.SetUnlockState(PATTERN_DISPLAY_UNLOCKED_KEY, false);
                        this.UpdateInspectorData();
                        StarCounter.RefreshAll();
                        onSuccess?.Invoke();
                        return;
                    }
                    
                    SaveData saveData = JsonUtility.FromJson<SaveData>(progress.game_data);
                    
                    // Load total stars (cumulative)
                    this.totalStars = saveData.totalStars;
                    
                    this.completedLevels.Clear();
                    foreach (var levelData in saveData.completedLevels)
                    {
                        if (levelData.level > 0)
                        {
                            string key = this.GetLevelKey(levelData.level);
                            this.completedLevels[key] = levelData.stars;
                        }
                    }
                    
                    // Load unlock states
                    this.SetUnlockState(AUTO_NOTE_UNLOCKED_KEY, saveData.autoNoteUnlocked);
                    this.SetUnlockState(CLEAR_NOTES_UNLOCKED_KEY, saveData.clearNotesUnlocked);
                    this.SetUnlockState(HINT_PANEL_UNLOCKED_KEY, saveData.hintPanelUnlocked);
                    this.SetUnlockState(PATTERN_DISPLAY_UNLOCKED_KEY, saveData.patternDisplayUnlocked);
                    
                    this.UpdateInspectorData();
                    Debug.Log($"[GameProgress] Loaded from SaiService: {this.completedLevelCount} levels, {this.totalStars} total stars");
                    StarCounter.RefreshAll();
                    onSuccess?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameProgress] Failed to parse game data from SaiService: {e.Message}");
                    this.totalStars = 0;
                    this.completedLevels.Clear();
                    this.SetUnlockState(AUTO_NOTE_UNLOCKED_KEY, false);
                    this.SetUnlockState(CLEAR_NOTES_UNLOCKED_KEY, false);
                    this.SetUnlockState(HINT_PANEL_UNLOCKED_KEY, false);
                    this.SetUnlockState(PATTERN_DISPLAY_UNLOCKED_KEY, false);
                    this.UpdateInspectorData();
                    StarCounter.RefreshAll();
                    onError?.Invoke(e.Message);
                }
            },
            error =>
            {
                Debug.LogError($"[GameProgress] Failed to load from SaiService: {error}. Starting with empty data.");
                this.totalStars = 0;
                this.completedLevels.Clear();
                this.SetUnlockState(AUTO_NOTE_UNLOCKED_KEY, false);
                this.SetUnlockState(CLEAR_NOTES_UNLOCKED_KEY, false);
                this.SetUnlockState(HINT_PANEL_UNLOCKED_KEY, false);
                this.SetUnlockState(PATTERN_DISPLAY_UNLOCKED_KEY, false);
                this.UpdateInspectorData();
                StarCounter.RefreshAll();
                onError?.Invoke(error);
            }
        );
    }

    /// <summary>
    /// Refresh in-memory game progress from SaiService and notify when complete.
    /// </summary>
    public void RefreshFromSaiService(System.Action onSuccess = null, System.Action<string> onError = null)
    {
        this.LoadFromSaiService(onSuccess, onError);
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
    /// Get unlock state from in-memory data
    /// </summary>
    private bool GetUnlockState(string key)
    {
        if (key == AUTO_NOTE_UNLOCKED_KEY) return this.autoNoteUnlocked;
        if (key == CLEAR_NOTES_UNLOCKED_KEY) return this.clearNotesUnlocked;
        if (key == HINT_PANEL_UNLOCKED_KEY) return this.hintPanelUnlocked;
        if (key == PATTERN_DISPLAY_UNLOCKED_KEY) return this.patternDisplayUnlocked;
        return false;
    }
    
    /// <summary>
    /// Set unlock state to in-memory data
    /// </summary>
    private void SetUnlockState(string key, bool value)
    {
        if (key == AUTO_NOTE_UNLOCKED_KEY) this.autoNoteUnlocked = value;
        else if (key == CLEAR_NOTES_UNLOCKED_KEY) this.clearNotesUnlocked = value;
        else if (key == HINT_PANEL_UNLOCKED_KEY) this.hintPanelUnlocked = value;
        else if (key == PATTERN_DISPLAY_UNLOCKED_KEY) this.patternDisplayUnlocked = value;
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
        return this.GetUnlockState(AUTO_NOTE_UNLOCKED_KEY);
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
        this.SetUnlockState(AUTO_NOTE_UNLOCKED_KEY, true);
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
    
    /// <summary>
    /// Get the star cost to use Auto Note feature (per use)
    /// </summary>
    public int GetAutoNoteUsageCost()
    {
        return this.autoNoteUsageCost;
    }
    
    /// <summary>
    /// Check if player can afford to use Auto Note
    /// </summary>
    public bool CanAffordAutoNoteUsage()
    {
        return this.totalStars >= this.autoNoteUsageCost;
    }
    
    /// <summary>
    /// Deduct stars for using Auto Note. Returns true if successful.
    /// </summary>
    public bool TryUseAutoNote()
    {
        if (this.totalStars < this.autoNoteUsageCost) return false;
        
        this.totalStars -= this.autoNoteUsageCost;
        this.UpdateInspectorData();
        this.Save();
        StarCounter.RefreshAll();
        
        Debug.Log($"[GameProgress] Auto Note used. Cost: {this.autoNoteUsageCost} stars. Remaining: {this.totalStars}");
        return true;
    }
    
    #endregion
    
    #region Clear Notes Unlock
    
    /// <summary>
    /// Check if Clear Notes feature has been permanently unlocked
    /// </summary>
    public bool IsClearNotesUnlocked()
    {
        return this.GetUnlockState(CLEAR_NOTES_UNLOCKED_KEY);
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
        this.SetUnlockState(CLEAR_NOTES_UNLOCKED_KEY, true);
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
        return this.GetUnlockState(HINT_PANEL_UNLOCKED_KEY);
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
        this.SetUnlockState(HINT_PANEL_UNLOCKED_KEY, true);
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
    
    /// <summary>
    /// Get the star cost to use Hint feature (per use)
    /// </summary>
    public int GetHintUsageCost()
    {
        return this.hintUsageCost;
    }
    
    /// <summary>
    /// Check if player can afford to use Hint
    /// </summary>
    public bool CanAffordHintUsage()
    {
        return this.totalStars >= this.hintUsageCost;
    }
    
    /// <summary>
    /// Deduct stars for using Hint. Returns true if successful.
    /// </summary>
    public bool TryUseHint()
    {
        if (this.totalStars < this.hintUsageCost) return false;
        
        this.totalStars -= this.hintUsageCost;
        this.UpdateInspectorData();
        this.Save();
        StarCounter.RefreshAll();
        
        Debug.Log($"[GameProgress] Hint used. Cost: {this.hintUsageCost} stars. Remaining: {this.totalStars}");
        return true;
    }
    
    #endregion
    
    #region Pattern Display Unlock
    
    /// <summary>
    /// Check if Pattern Display feature has been permanently unlocked
    /// </summary>
    public bool IsPatternDisplayUnlocked()
    {
        return this.GetUnlockState(PATTERN_DISPLAY_UNLOCKED_KEY);
    }
    
    /// <summary>
    /// Get the star cost to unlock Pattern Display
    /// </summary>
    public int GetPatternDisplayUnlockCost()
    {
        return this.patternDisplayUnlockCost;
    }
    
    /// <summary>
    /// Attempt to unlock Pattern Display by spending stars. Returns true if successful.
    /// Requires Hint Panel to be unlocked first.
    /// </summary>
    public bool TryUnlockPatternDisplay()
    {
        if (this.IsPatternDisplayUnlocked()) return true;
        
        // Pattern Display requires Hint Panel to be unlocked first
        if (!this.IsHintPanelUnlocked()) return false;
        
        if (this.totalStars < this.patternDisplayUnlockCost) return false;
        
        this.totalStars -= this.patternDisplayUnlockCost;
        this.SetUnlockState(PATTERN_DISPLAY_UNLOCKED_KEY, true);
        this.patternDisplayUnlocked = true;
        
        this.UpdateInspectorData();
        this.Save();
        
        return true;
    }
    
    /// <summary>
    /// Check if player can afford to unlock Pattern Display
    /// Requires Hint Panel to be unlocked first.
    /// </summary>
    public bool CanAffordPatternDisplayUnlock()
    {
        return this.IsHintPanelUnlocked() && this.totalStars >= this.patternDisplayUnlockCost;
    }
    
    #endregion
    
    #region Debug & Utility Methods
    /// <summary>
    /// Delete all in-memory progress data and push empty state to SaiService.
    /// </summary>
    [ProButton]
    public void DeleteSaveData()
    {
        this.completedLevels.Clear();
        this.totalStars = 0;
        this.SetUnlockState(AUTO_NOTE_UNLOCKED_KEY, false);
        this.SetUnlockState(CLEAR_NOTES_UNLOCKED_KEY, false);
        this.SetUnlockState(HINT_PANEL_UNLOCKED_KEY, false);
        this.SetUnlockState(PATTERN_DISPLAY_UNLOCKED_KEY, false);
        this.UpdateInspectorData();
        this.SaveToSaiService();
        Debug.Log("[GameProgress] Cleared local progress and synced empty state to SaiService");
    }
    
    /// <summary>
    /// Check if any progress data exists in memory
    /// </summary>
    public bool HasSaveData()
    {
        return this.completedLevels.Count > 0 || this.totalStars > 0;
    }
    
    /// <summary>
    /// Get current in-memory progress data as JSON string (for debugging)
    /// </summary>
    [ProButton]
    public string GetSaveDataJson()
    {
        SaveData saveData = new SaveData
        {
            totalStars = this.totalStars,
            completedLevels = new List<LevelData>(),
            autoNoteUnlocked = this.GetUnlockState(AUTO_NOTE_UNLOCKED_KEY),
            clearNotesUnlocked = this.GetUnlockState(CLEAR_NOTES_UNLOCKED_KEY),
            hintPanelUnlocked = this.GetUnlockState(HINT_PANEL_UNLOCKED_KEY),
            patternDisplayUnlocked = this.GetUnlockState(PATTERN_DISPLAY_UNLOCKED_KEY)
        };

        foreach (var kvp in this.completedLevels)
        {
            string[] parts = kvp.Key.Split('_');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int levelNumber)) continue;

            saveData.completedLevels.Add(new LevelData
            {
                level = levelNumber,
                stars = kvp.Value
            });
        }

        string json = JsonUtility.ToJson(saveData);
        Debug.Log($"[GameProgress] Current data JSON: {json}");
        return json;
    }
    
    /// <summary>
    /// Force reload progress from SaiService
    /// </summary>
    [ProButton]
    public void ReloadProgress()
    {
        Debug.Log("[GameProgress] Force reloading progress...");
        this.Load();
    }
    
    /// <summary>
    /// Force save current progress to SaiService
    /// </summary>
    [ProButton]
    public void ForceSave()
    {
        Debug.Log($"[GameProgress] Force saving progress... AutoNote unlocked: {this.autoNoteUnlocked}, ClearNotes unlocked: {this.clearNotesUnlocked}, HintPanel unlocked: {this.hintPanelUnlocked}, PatternDisplay unlocked: {this.patternDisplayUnlocked}");
        this.Save();
    }
    
    /// <summary>
    /// Sync local in-memory data to SaiService (upload to cloud)
    /// </summary>
    [ProButton]
    public void SyncToSaiService()
    {
        Debug.Log("[GameProgress] Syncing in-memory data to SaiService...");
        this.SaveToSaiService();
    }
    
    /// <summary>
    /// Pull data from SaiService to local (download from cloud)
    /// </summary>
    [ProButton]
    public void PullFromSaiService()
    {
        Debug.Log("[GameProgress] Pulling data from SaiService...");
        this.RefreshFromSaiService();
    }
    
    /// <summary>
    /// Create new progress in SaiService (if not exists)
    /// </summary>
    [ProButton]
    public void CreateSaiServiceProgress()
    {
        GamerProgress gamerProgress = SaiService.Instance?.GamerProgress;
        
        if (gamerProgress == null)
        {
            Debug.LogError("[GameProgress] Cannot create progress: GamerProgress not found");
            return;
        }
        
        if (gamerProgress.HasProgress)
        {
            Debug.LogWarning("[GameProgress] Progress already exists in SaiService. Use SyncToSaiService to update it.");
            return;
        }
        
        gamerProgress.CreateProgress(
            progress =>
            {
                Debug.Log($"[GameProgress] Created new progress in SaiService: ID={progress.id}");
                // After creating, sync current data to it
                this.SaveToSaiService();
            },
            error =>
            {
                Debug.LogError($"[GameProgress] Failed to create progress in SaiService: {error}");
            }
        );
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
        public bool autoNoteUnlocked;
        public bool clearNotesUnlocked;
        public bool hintPanelUnlocked;
        public bool patternDisplayUnlocked;
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
