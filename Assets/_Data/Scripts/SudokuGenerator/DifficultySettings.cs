using UnityEngine;

[System.Serializable]
public class DifficultySettings
{
    public SudokuGenerator.DifficultyLevel level;
    public int minClues;
    public int maxClues;

    public DifficultySettings(SudokuGenerator.DifficultyLevel level, int minClues, int maxClues)
    {
        this.level = level;
        this.minClues = minClues;
        this.maxClues = maxClues;
    }
}
