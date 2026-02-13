using UnityEngine;
using System;
using System.Collections.Generic;

public class SudokuResultAnalyzer : SaiBehaviour
{
    public enum GameResult
    {
        NotCompleted,
        Victory,
        Defeat
    }

    [Header("Current Game State")]
    [SerializeField] private GameResult currentResult = GameResult.NotCompleted;
    [SerializeField] private bool isAnalyzed = false;
    [SerializeField] private int totalMoves = 0;
    [SerializeField] private int correctCells = 0;
    [SerializeField] private int incorrectCells = 0;
    [SerializeField] private float completionPercentage = 0f;
    
    [Header("Game Statistics")]
    [SerializeField] private float timeTaken = 0f;
    [SerializeField] private int hintsUsed = 0;
    
    [Space(10)]
    [Header("Analysis Details")]
    [TextArea(5, 10)]
    [SerializeField] private string analysisReport = "";

    private int[,] userSolution;
    private int[,] correctSolution;
    private List<CellError> errors;

    protected override void Awake()
    {
        base.Awake();
        this.errors = new List<CellError>();
    }

    /// <summary>
    /// Submit user's completed puzzle for analysis
    /// </summary>
    public GameResult SubmitSolution(int[,] userPuzzle, int[,] solution, float gameTime = 0f, int hints = 0)
    {
        if (!Application.isPlaying || userPuzzle == null || solution == null)
        {
            return GameResult.NotCompleted;
        }

        this.userSolution = (int[,])userPuzzle.Clone();
        this.correctSolution = (int[,])solution.Clone();
        this.timeTaken = gameTime;
        this.hintsUsed = hints;
        
        this.AnalyzeSolution();
        this.GenerateReport();
        
        return this.currentResult;
    }

    /// <summary>
    /// Analyze the user's solution and determine result
    /// </summary>
    private void AnalyzeSolution()
    {
        if (!Application.isPlaying || this.correctSolution == null || this.userSolution == null)
        {
            return;
        }

        if (this.errors == null)
        {
            this.errors = new List<CellError>();
        }

        this.errors.Clear();
        this.correctCells = 0;
        this.incorrectCells = 0;
        this.totalMoves = 0;

        int gridSize = this.correctSolution.GetLength(0);

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                int userValue = this.userSolution[row, col];
                int correctValue = this.correctSolution[row, col];

                if (userValue != 0)
                {
                    this.totalMoves++;

                    if (userValue == correctValue)
                    {
                        this.correctCells++;
                    }
                    else
                    {
                        this.incorrectCells++;
                        this.errors.Add(new CellError(row, col, userValue, correctValue));
                    }
                }
            }
        }

        int totalCells = gridSize * gridSize;
        this.completionPercentage = (float)this.totalMoves / totalCells * 100f;

        // Determine game result
        if (this.totalMoves == totalCells && this.incorrectCells == 0)
        {
            this.currentResult = GameResult.Victory;
        }
        else if (this.incorrectCells > 0)
        {
            this.currentResult = GameResult.Defeat;
        }
        else
        {
            this.currentResult = GameResult.NotCompleted;
        }

        this.isAnalyzed = true;
    }

    /// <summary>
    /// Generate detailed analysis report
    /// </summary>
    private void GenerateReport()
    {
        System.Text.StringBuilder report = new System.Text.StringBuilder();

        report.AppendLine("=== SUDOKU RESULT ANALYSIS ===\n");
        report.AppendLine($"Final Result: {this.GetResultText()}");
        report.AppendLine($"Completion: {this.completionPercentage:F1}%");
        report.AppendLine($"Time Taken: {this.FormatTime(this.timeTaken)}");
        report.AppendLine($"Hints Used: {this.hintsUsed}\n");

        report.AppendLine("--- Cell Statistics ---");
        report.AppendLine($"Total Moves: {this.totalMoves}");
        report.AppendLine($"Correct Cells: {this.correctCells}");
        report.AppendLine($"Incorrect Cells: {this.incorrectCells}\n");

        if (this.errors.Count > 0)
        {
            report.AppendLine("--- Errors Found ---");
            foreach (CellError error in this.errors)
            {
                report.AppendLine($"Position [{error.row}, {error.col}]: " +
                                $"You entered {error.userValue}, correct is {error.correctValue}");
            }
        }
        else if (this.currentResult == GameResult.Victory)
        {
            report.AppendLine("Perfect! All cells are correct!");
        }

        this.analysisReport = report.ToString();
    }

    /// <summary>
    /// Get current game result
    /// </summary>
    public GameResult GetCurrentResult()
    {
        return this.currentResult;
    }

    /// <summary>
    /// Check if puzzle is completed correctly
    /// </summary>
    public bool IsVictory()
    {
        return this.currentResult == GameResult.Victory;
    }

    /// <summary>
    /// Get list of errors in user's solution
    /// </summary>
    public List<CellError> GetErrors()
    {
        return new List<CellError>(this.errors);
    }

    /// <summary>
    /// Get completion percentage
    /// </summary>
    public float GetCompletionPercentage()
    {
        return this.completionPercentage;
    }

    /// <summary>
    /// Get analysis report
    /// </summary>
    public string GetAnalysisReport()
    {
        return this.analysisReport;
    }

    /// <summary>
    /// Reset analyzer state
    /// </summary>
    public void Reset()
    {
        this.currentResult = GameResult.NotCompleted;
        this.isAnalyzed = false;
        this.totalMoves = 0;
        this.correctCells = 0;
        this.incorrectCells = 0;
        this.completionPercentage = 0f;
        this.timeTaken = 0f;
        this.hintsUsed = 0;
        this.analysisReport = "";
        this.errors.Clear();
    }

    /// <summary>
    /// Quick validation without storing full analysis
    /// </summary>
    public bool ValidateSolution(int[,] userPuzzle, int[,] solution)
    {
        int gridSize = solution.GetLength(0);

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                if (userPuzzle[row, col] != solution[row, col])
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Check if a specific cell is correct
    /// </summary>
    public bool IsCellCorrect(int row, int col, int userValue, int[,] solution)
    {
        return userValue == solution[row, col];
    }

    private string GetResultText()
    {
        switch (this.currentResult)
        {
            case GameResult.Victory:
                return "VICTORY! Puzzle solved correctly!";
            case GameResult.Defeat:
                return "DEFEAT! There are errors in your solution.";
            case GameResult.NotCompleted:
                return "Puzzle not completed yet.";
            default:
                return "Unknown";
        }
    }

    private string FormatTime(float seconds)
    {
        if (seconds <= 0) return "N/A";

        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:D2}:{secs:D2}";
    }
}

/// <summary>
/// Represents an error in a specific cell
/// </summary>
[System.Serializable]
public class CellError
{
    public int row;
    public int col;
    public int userValue;
    public int correctValue;

    public CellError(int row, int col, int userValue, int correctValue)
    {
        this.row = row;
        this.col = col;
        this.userValue = userValue;
        this.correctValue = correctValue;
    }

    public override string ToString()
    {
        return $"Error at [{this.row},{this.col}]: entered {this.userValue}, expected {this.correctValue}";
    }
}
