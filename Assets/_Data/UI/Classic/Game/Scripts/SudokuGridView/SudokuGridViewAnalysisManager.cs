using UnityEngine;
using System.Collections.Generic;

public class SudokuGridViewAnalysisManager
{
    private const int GRID_SIZE = 9;

    private SudokuResultAnalyzer resultAnalyzer;
    private SudokuPatternAnalyzer patternAnalyzer;
    private SudokuCell[,] cells;
    private int[,] cachedSolution;

    public float CompletionPercentage { get; private set; }
    public int CorrectCells { get; private set; }
    public int IncorrectCells { get; private set; }
    public SudokuResultAnalyzer.GameResult CurrentResult { get; private set; }
    public string UserPuzzlePreview { get; private set; }

    public SudokuGridViewAnalysisManager(SudokuResultAnalyzer resultAnalyzer, SudokuPatternAnalyzer patternAnalyzer, SudokuCell[,] cells)
    {
        this.resultAnalyzer = resultAnalyzer;
        this.patternAnalyzer = patternAnalyzer;
        this.cells = cells;
        this.CompletionPercentage = 0f;
        this.CorrectCells = 0;
        this.IncorrectCells = 0;
        this.CurrentResult = SudokuResultAnalyzer.GameResult.NotCompleted;
        this.UserPuzzlePreview = "";
    }

    public void SetCachedSolution(int[,] solution)
    {
        this.cachedSolution = solution;
    }

    public void AnalyzeCurrentState(System.Action onVictory)
    {
        if (this.resultAnalyzer == null || this.cachedSolution == null) return;

        int[,] userPuzzle = this.GetCurrentUserPuzzle();
        int[,] solution = this.cachedSolution;

        SudokuResultAnalyzer.GameResult result = this.resultAnalyzer.SubmitSolution(
            userPuzzle,
            solution,
            gameTime: Time.time,
            hints: 0
        );

        this.CurrentResult = result;
        this.CompletionPercentage = this.resultAnalyzer.GetCompletionPercentage();

        this.CorrectCells = 0;
        this.IncorrectCells = 0;
        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                int userValue = userPuzzle[row, col];
                if (userValue != 0)
                {
                    if (userValue == solution[row, col])
                        this.CorrectCells++;
                    else
                        this.IncorrectCells++;
                }
            }
        }

        if (result == SudokuResultAnalyzer.GameResult.Victory)
        {
            Debug.Log("<color=green>VICTORY!</color> Puzzle solved correctly!");
            Debug.Log(this.resultAnalyzer.GetAnalysisReport());
            onVictory?.Invoke();
        }
        else if (result == SudokuResultAnalyzer.GameResult.Defeat)
        {
            Debug.Log($"<color=yellow>Errors detected:</color> {this.IncorrectCells} incorrect cells");
        }

        this.UserPuzzlePreview = this.GetUserPuzzlePreview(userPuzzle, solution);
    }

    public void AnalyzePatterns()
    {
        if (this.patternAnalyzer == null) return;

        int[,] currentPuzzle = this.GetCurrentUserPuzzle();
        List<int>[,] allNotes = this.GetAllCellNotes();

        this.patternAnalyzer.AnalyzePatterns(currentPuzzle, allNotes);
    }

    public void ResetStats()
    {
        this.CompletionPercentage = 0f;
        this.CorrectCells = 0;
        this.IncorrectCells = 0;
        this.CurrentResult = SudokuResultAnalyzer.GameResult.NotCompleted;
    }

    private int[,] GetCurrentUserPuzzle()
    {
        int[,] puzzle = new int[GRID_SIZE, GRID_SIZE];

        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                puzzle[row, col] = this.cells[row, col].Value;
            }
        }

        return puzzle;
    }

    private List<int>[,] GetAllCellNotes()
    {
        List<int>[,] allNotes = new List<int>[GRID_SIZE, GRID_SIZE];

        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                if (this.cells[row, col].Notes != null && this.cells[row, col].Notes.Count > 0)
                {
                    allNotes[row, col] = new List<int>(this.cells[row, col].Notes);
                }
                else
                {
                    allNotes[row, col] = new List<int>();
                }
            }
        }

        return allNotes;
    }

    private string GetUserPuzzlePreview(int[,] userPuzzle, int[,] solution)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < GRID_SIZE; i++)
        {
            if (i % 3 == 0 && i != 0)
                sb.AppendLine("------+-------+------");

            for (int j = 0; j < GRID_SIZE; j++)
            {
                if (j % 3 == 0 && j != 0)
                    sb.Append("| ");

                int userValue = userPuzzle[i, j];
                if (userValue == 0)
                {
                    sb.Append("· ");
                }
                else if (userValue == solution[i, j])
                {
                    sb.Append(userValue + " ");
                }
                else
                {
                    sb.Append("X ");
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
