public static class GameData
{
    // Level and difficulty parameters passed between scenes
    public static int SelectedLevel { get; set; } = 1;
    public static int SelectedDifficulty { get; set; } = 0;
    public static string SelectedLevelName { get; set; } = "level-1";

    // Map difficulty index (0-6) to SudokuGenerator.DifficultyLevel
    public static SudokuGenerator.DifficultyLevel GetDifficultyLevel()
    {
        switch (SelectedDifficulty)
        {
            case 0: return SudokuGenerator.DifficultyLevel.VeryEasy;
            case 1: return SudokuGenerator.DifficultyLevel.Easy;
            case 2: return SudokuGenerator.DifficultyLevel.Medium;
            case 3: return SudokuGenerator.DifficultyLevel.Hard;
            case 4: return SudokuGenerator.DifficultyLevel.VeryHard;
            case 5: return SudokuGenerator.DifficultyLevel.Expert;
            case 6: return SudokuGenerator.DifficultyLevel.Master;
            case 7: return SudokuGenerator.DifficultyLevel.Extreme;
            case 8: return SudokuGenerator.DifficultyLevel.Legendary;
            default: return SudokuGenerator.DifficultyLevel.Medium;
        }
    }

    public static readonly string[] DIFFICULTY_NAMES =
    {
        "Very Easy",
        "Easy",
        "Medium",
        "Hard",
        "Very Hard",
        "Expert",
        "Master",
        "Extreme",
        "Legendary"
    };

    public const int DIFFICULTY_COUNT = 9;
    public const int LEVELS_PER_DIFFICULTY = 3;
}
