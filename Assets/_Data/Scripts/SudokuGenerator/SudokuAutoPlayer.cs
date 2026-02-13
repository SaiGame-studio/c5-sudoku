using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using com.cyborgAssets.inspectorButtonPro;

public class SudokuAutoPlayer : SaiBehaviour
{
    [Header("Settings")]
    [SerializeField] private float autoPlayDelay = 0.1f;
    [SerializeField] private bool isAutoPlaying = false;

    [Header("Dependencies")]
    [SerializeField] private SudokuGenerator sudokuGenerator;
    [SerializeField] private SudokuResultAnalyzer resultAnalyzer;

    [Header("Target Grid View")]
    [Tooltip("Leave empty to auto-detect active GridView when playing")]
    [SerializeField] private SudokuGridView targetGridView;

    [Header("Stats")]
    [SerializeField] private int totalMoves = 0;
    [SerializeField] private int currentMove = 0;

    private Coroutine autoPlayCoroutine;
    private const int GRID_SIZE = 9;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadSudokuGenerator();
        this.LoadResultAnalyzer();
        // Don't auto-load targetGridView here, will find active one when playing
    }

    private void LoadSudokuGenerator()
    {
        if (this.sudokuGenerator != null) return;
        this.sudokuGenerator = FindFirstObjectByType<SudokuGenerator>();
        Debug.Log(transform.name + ": LoadSudokuGenerator", gameObject);
    }

    private void LoadResultAnalyzer()
    {
        if (this.resultAnalyzer != null) return;
        this.resultAnalyzer = FindFirstObjectByType<SudokuResultAnalyzer>();
        Debug.Log(transform.name + ": LoadResultAnalyzer", gameObject);
    }

    /// <summary>
    /// Find the active GridView in the scene
    /// </summary>
    private SudokuGridView FindActiveGridView()
    {
        SudokuGridView[] allGridViews = FindObjectsByType<SudokuGridView>(FindObjectsSortMode.None);
        
        foreach (SudokuGridView gridView in allGridViews)
        {
            // Check if GameObject is active
            if (gridView.gameObject.activeInHierarchy && gridView.enabled)
            {
                return gridView;
            }
        }

        // If no active found, return first available
        if (allGridViews.Length > 0)
        {
            return allGridViews[0];
        }

        return null;
    }

    [ProButton]
    /// <summary>
    /// Start auto-playing the puzzle on target grid view
    /// </summary>
    public void StartAutoPlay()
    {
        if (this.isAutoPlaying)
        {
            Debug.LogWarning("Auto play is already running!");
            return;
        }

        if (this.sudokuGenerator == null)
        {
            Debug.LogError("SudokuGenerator not found!");
            return;
        }

        // Auto-detect active GridView if not set or not active
        if (this.targetGridView == null || !this.targetGridView.gameObject.activeInHierarchy)
        {
            this.targetGridView = this.FindActiveGridView();
            if (this.targetGridView != null)
            {
                Debug.Log($"<color=cyan>Auto-detected active GridView:</color> {this.targetGridView.gameObject.name}");
            }
        }

        if (this.targetGridView == null)
        {
            Debug.LogError("No active GridView found!");
            return;
        }

        this.autoPlayCoroutine = StartCoroutine(this.AutoPlayCoroutine());
    }

    /// <summary>
    /// Start auto-play on a specific grid view (use from code, not Inspector)
    /// </summary>
    public void StartAutoPlayOnGridView(SudokuGridView gridView)
    {
        if (gridView == null)
        {
            Debug.LogError("GridView parameter is null!");
            return;
        }

        this.targetGridView = gridView;
        Debug.Log($"<color=cyan>Manually set target GridView:</color> {gridView.gameObject.name}");
        this.StartAutoPlay();
    }

    [ProButton]
    /// <summary>
    /// Stop auto-playing
    /// </summary>
    public void StopAutoPlay()
    {
        if (this.autoPlayCoroutine != null)
        {
            StopCoroutine(this.autoPlayCoroutine);
            this.autoPlayCoroutine = null;
        }
        
        this.isAutoPlaying = false;
        this.currentMove = 0;
        Debug.Log("Auto play stopped.");
    }

    [ProButton]
    /// <summary>
    /// Find and display active GridView (for testing)
    /// </summary>
    public void FindAndShowActiveGridView()
    {
        SudokuGridView activeView = this.FindActiveGridView();
        if (activeView != null)
        {
            Debug.Log($"<color=green>Active GridView found:</color> {activeView.gameObject.name} (Active: {activeView.gameObject.activeInHierarchy})");
            this.targetGridView = activeView;
        }
        else
        {
            Debug.LogWarning("No active GridView found in scene!");
        }
    }

    /// <summary>
    /// Set the delay between each move
    /// </summary>
    public void SetDelay(float delay)
    {
        this.autoPlayDelay = Mathf.Max(0.1f, delay);
    }

    /// <summary>
    /// Manually set target grid view
    /// </summary>
    public void SetTargetGridView(SudokuGridView gridView)
    {
        this.targetGridView = gridView;
        if (gridView != null)
        {
            Debug.Log($"Target GridView set to: {gridView.gameObject.name}");
        }
    }

    /// <summary>
    /// Get current auto play status
    /// </summary>
    public bool IsPlaying()
    {
        return this.isAutoPlaying;
    }

    private IEnumerator AutoPlayCoroutine()
    {
        this.isAutoPlaying = true;
        this.currentMove = 0;
        int[,] solution = this.sudokuGenerator.GetSolution();

        Debug.Log($"<color=cyan>Auto play started on {this.targetGridView.gameObject.name}</color> (delay: {this.autoPlayDelay}s)");

        // Collect all empty cells (non-clues)
        List<(int row, int col)> emptyCells = new List<(int, int)>();
        
        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                CellData cellData = this.targetGridView.GetCellData(row, col);
                if (!cellData.isClue && cellData.value == 0)
                {
                    emptyCells.Add((row, col));
                }
            }
        }

        this.totalMoves = emptyCells.Count;
        Debug.Log($"Found {this.totalMoves} empty cells to fill.");

        // Fill each empty cell with correct answer
        foreach (var (row, col) in emptyCells)
        {
            this.currentMove++;

            // Get correct number from solution
            int correctNumber = solution[row, col];

            // Fill the cell via GridView
            this.targetGridView.FillCell(row, col, correctNumber);

            // Check if victory
            SudokuResultAnalyzer.GameResult currentResult = this.targetGridView.GetCurrentResult();
            if (currentResult == SudokuResultAnalyzer.GameResult.Victory)
            {
                Debug.Log($"<color=green>Auto play completed!</color> Puzzle solved in {this.currentMove} moves.");
                this.isAutoPlaying = false;
                this.currentMove = 0;
                yield break;
            }

            // Wait before next move
            yield return new WaitForSeconds(this.autoPlayDelay);
        }

        this.isAutoPlaying = false;
        this.currentMove = 0;
        Debug.Log("<color=green>Auto play finished!</color>");
    }
}
