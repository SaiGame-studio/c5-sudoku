using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using com.cyborgAssets.inspectorButtonPro;

public class SudokuGenerator : SaiBehaviour
{
    public enum DifficultyLevel
    {
        VeryEasy,
        Easy,
        Medium,
        Hard,
        VeryHard,
        Expert,
        Master,
        Extreme,
        Legendary
    }

    [Header("Difficulty Settings")]
    [SerializeField] private DifficultyLevel currentDifficulty = DifficultyLevel.Medium;
    
    [Header("Debug Info")]
    [SerializeField] private bool isPuzzleGenerated = false;
    [SerializeField] private int totalClues = 0;
    [SerializeField] private int emptyCells = 0;
    [SerializeField] private string summary = "";
    [TextArea(11, 11)]
    [SerializeField] private string puzzlePreview = "";
    
    private Dictionary<DifficultyLevel, DifficultySettings> difficultyMap;
    private int[,] solution;
    private int[,] puzzle;
    private const int GRID_SIZE = 9;
    private const int BOX_SIZE = 3;

    protected override void Awake()
    {
        base.Awake();
        this.InitializeDifficultySettings();
    }

    private void InitializeDifficultySettings()
    {
        this.difficultyMap = new Dictionary<DifficultyLevel, DifficultySettings>
        {
            { DifficultyLevel.VeryEasy, new DifficultySettings(DifficultyLevel.VeryEasy, 50, 55) },
            { DifficultyLevel.Easy, new DifficultySettings(DifficultyLevel.Easy, 45, 50) },
            { DifficultyLevel.Medium, new DifficultySettings(DifficultyLevel.Medium, 40, 45) },
            { DifficultyLevel.Hard, new DifficultySettings(DifficultyLevel.Hard, 35, 40) },
            { DifficultyLevel.VeryHard, new DifficultySettings(DifficultyLevel.VeryHard, 30, 35) },
            { DifficultyLevel.Expert, new DifficultySettings(DifficultyLevel.Expert, 25, 30) },
            { DifficultyLevel.Master, new DifficultySettings(DifficultyLevel.Master, 20, 25) },
            { DifficultyLevel.Extreme, new DifficultySettings(DifficultyLevel.Extreme, 18, 20) },
            { DifficultyLevel.Legendary, new DifficultySettings(DifficultyLevel.Legendary, 17, 18) }
        };
    }

    #region Public Methods
    [ProButton]
    /// <summary>
    /// Generate a new Sudoku puzzle with current difficulty
    /// </summary>
    public void GeneratePuzzle()
    {
        this.GeneratePuzzle(this.currentDifficulty);
    }

    /// <summary>
    /// Generate a new Sudoku puzzle with specified difficulty
    /// </summary>
    public void GeneratePuzzle(DifficultyLevel difficulty)
    {
        this.currentDifficulty = difficulty;
        // Ensure difficulty settings are initialized
        if (this.difficultyMap == null)
        {
            this.InitializeDifficultySettings();
        }

        this.currentDifficulty = difficulty;
        this.solution = new int[GRID_SIZE, GRID_SIZE];
        this.puzzle = new int[GRID_SIZE, GRID_SIZE];

        this.GenerateCompleteSolution();
        this.CreatePuzzleFromSolution(difficulty);
        this.UpdateDebugInfo();
    }

    /// <summary>
    /// Get the current puzzle (0 represents empty cells)
    /// </summary>
    public int[,] GetPuzzle()
    {
        return (int[,])this.puzzle.Clone();
    }

    /// <summary>
    /// Get the solution
    /// </summary>
    public int[,] GetSolution()
    {
        return (int[,])this.solution.Clone();
    }

    /// <summary>
    /// Set difficulty level
    /// </summary>
    public void SetDifficulty(DifficultyLevel difficulty)
    {
        this.currentDifficulty = difficulty;
    }

    /// <summary>
    /// Get current difficulty
    /// </summary>
    public DifficultyLevel GetDifficulty()
    {
        return this.currentDifficulty;
    }

    /// <summary>
    /// Check if a number is valid at given position in current puzzle
    /// </summary>
    public bool IsValidMove(int row, int col, int num)
    {
        return this.IsValid(this.puzzle, row, col, num);
    }

    [ProButton]
    /// <summary>
    /// Print puzzle to console (for debugging)
    /// </summary>
    public void PrintPuzzle()
    {
        this.PrintGrid(this.puzzle, "Puzzle");
    }

    [ProButton]
    /// <summary>
    /// Print solution to console (for debugging)
    /// </summary>
    public void PrintSolution()
    {
        this.PrintGrid(this.solution, "Solution");
    }

    [ProButton]
    /// <summary>
    /// Clear current puzzle
    /// </summary>
    public void ClearPuzzle()
    {
        this.solution = null;
        this.puzzle = null;
        this.isPuzzleGenerated = false;
        this.totalClues = 0;
        this.emptyCells = 0;
        this.summary = "";
        this.puzzlePreview = "";
    }
    #endregion

    #region Generation Logic
    private void GenerateCompleteSolution()
    {
        this.FillDiagonalBoxes();
        this.FillRemaining(0, BOX_SIZE);
    }

    private void FillDiagonalBoxes()
    {
        for (int box = 0; box < GRID_SIZE; box += BOX_SIZE)
        {
            this.FillBox(box, box);
        }
    }

    private void FillBox(int row, int col)
    {
        List<int> numbers = Enumerable.Range(1, GRID_SIZE).OrderBy(x => Random.value).ToList();
        int index = 0;

        for (int i = 0; i < BOX_SIZE; i++)
        {
            for (int j = 0; j < BOX_SIZE; j++)
            {
                this.solution[row + i, col + j] = numbers[index++];
            }
        }
    }

    private bool FillRemaining(int row, int col)
    {
        if (col >= GRID_SIZE && row < GRID_SIZE - 1)
        {
            row++;
            col = 0;
        }

        if (row >= GRID_SIZE && col >= GRID_SIZE)
            return true;

        if (row < BOX_SIZE)
        {
            if (col < BOX_SIZE)
                col = BOX_SIZE;
        }
        else if (row < GRID_SIZE - BOX_SIZE)
        {
            if (col == (int)(row / BOX_SIZE) * BOX_SIZE)
                col += BOX_SIZE;
        }
        else
        {
            if (col == GRID_SIZE - BOX_SIZE)
            {
                row++;
                col = 0;
                if (row >= GRID_SIZE)
                    return true;
            }
        }

        List<int> numbers = Enumerable.Range(1, GRID_SIZE).OrderBy(x => Random.value).ToList();

        foreach (int num in numbers)
        {
            if (this.IsValid(this.solution, row, col, num))
            {
                this.solution[row, col] = num;
                if (this.FillRemaining(row, col + 1))
                    return true;
                this.solution[row, col] = 0;
            }
        }

        return false;
    }

    private void CreatePuzzleFromSolution(DifficultyLevel difficulty)
    {
        System.Array.Copy(this.solution, this.puzzle, this.solution.Length);

        DifficultySettings settings = this.difficultyMap[difficulty];
        int targetClues = Random.Range(settings.minClues, settings.maxClues + 1);
        int cellsToRemove = (GRID_SIZE * GRID_SIZE) - targetClues;

        List<(int, int)> cells = new List<(int, int)>();
        for (int i = 0; i < GRID_SIZE; i++)
        {
            for (int j = 0; j < GRID_SIZE; j++)
            {
                cells.Add((i, j));
            }
        }

        cells = cells.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < cellsToRemove && i < cells.Count; i++)
        {
            var (row, col) = cells[i];
            this.puzzle[row, col] = 0;
        }
    }
    #endregion

    #region Validation
    private bool IsValid(int[,] grid, int row, int col, int num)
    {
        for (int x = 0; x < GRID_SIZE; x++)
        {
            if (grid[row, x] == num)
                return false;
        }

        for (int x = 0; x < GRID_SIZE; x++)
        {
            if (grid[x, col] == num)
                return false;
        }

        int startRow = row - row % BOX_SIZE;
        int startCol = col - col % BOX_SIZE;

        for (int i = 0; i < BOX_SIZE; i++)
        {
            for (int j = 0; j < BOX_SIZE; j++)
            {
                if (grid[i + startRow, j + startCol] == num)
                    return false;
            }
        }

        return true;
    }
    #endregion

    #region Debug Helpers
    private void UpdateDebugInfo()
    {
        if (this.puzzle == null)
        {
            this.isPuzzleGenerated = false;
            this.totalClues = 0;
            this.emptyCells = 0;
            this.puzzlePreview = "No puzzle generated";
            return;
        }

        this.isPuzzleGenerated = true;
        this.totalClues = 0;
        this.emptyCells = 0;

        for (int i = 0; i < GRID_SIZE; i++)
        {
            for (int j = 0; j < GRID_SIZE; j++)
            {
                if (this.puzzle[i, j] != 0)
                    this.totalClues++;
                else
                    this.emptyCells++;
            }
        }

        this.puzzlePreview = this.GetGridPreview(this.puzzle);
    }

    private string GetGridPreview(int[,] grid)
    {
        if (grid == null) return "No grid available";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        for (int i = 0; i < GRID_SIZE; i++)
        {
            if (i % BOX_SIZE == 0 && i != 0)
                sb.AppendLine("------+-------+------");

            for (int j = 0; j < GRID_SIZE; j++)
            {
                if (j % BOX_SIZE == 0 && j != 0)
                    sb.Append("| ");

                sb.Append(grid[i, j] == 0 ? "x " : grid[i, j] + " ");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void PrintGrid(int[,] grid, string title)
    {
        string gridText = this.GetGridPreview(grid);
        UnityEngine.Debug.Log($"=== {title} ===\n{gridText}");
    }
    #endregion
}
