using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using com.cyborgAssets.inspectorButtonPro;

public class SudokuAutoNotes : SaiBehaviour
{
    [Header("Settings")]
    [SerializeField] private float noteDelay = 0.1f;
    [SerializeField] private bool isRunning = false;

    [Header("Dependencies")]
    [SerializeField] private SudokuGenerator sudokuGenerator;
    [SerializeField] private SudokuPatternAnalyzer patternAnalyzer;

    [Header("Target Grid View")]
    [Tooltip("Leave empty to auto-detect active GridView")]
    [SerializeField] private SudokuGridView targetGridView;

    [Header("Stats")]
    [SerializeField] private int totalCells = 0;
    [SerializeField] private int cellsProcessed = 0;
    [SerializeField] private int totalNotesAdded = 0;

    private Coroutine autoNoteCoroutine;
    private const int GRID_SIZE = 9;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadSudokuGenerator();
        this.LoadPatternAnalyzer();
    }

    private void LoadSudokuGenerator()
    {
        if (this.sudokuGenerator != null) return;
        this.sudokuGenerator = FindFirstObjectByType<SudokuGenerator>();
        Debug.Log(transform.name + ": LoadSudokuGenerator", gameObject);
    }

    private void LoadPatternAnalyzer()
    {
        if (this.patternAnalyzer != null) return;
        this.patternAnalyzer = FindFirstObjectByType<SudokuPatternAnalyzer>();
        Debug.Log(transform.name + ": LoadPatternAnalyzer", gameObject);
    }

    /// <summary>
    /// Find the active GridView in the scene
    /// </summary>
    private SudokuGridView FindActiveGridView()
    {
        SudokuGridView[] allGridViews = FindObjectsByType<SudokuGridView>(FindObjectsSortMode.None);
        
        foreach (SudokuGridView gridView in allGridViews)
        {
            if (gridView.gameObject.activeInHierarchy && gridView.enabled)
            {
                return gridView;
            }
        }

        if (allGridViews.Length > 0)
        {
            return allGridViews[0];
        }

        return null;
    }

    [ProButton]
    /// <summary>
    /// Start auto-adding candidate notes
    /// </summary>
    public void StartAutoNotes()
    {
        if (this.isRunning)
        {
            Debug.LogWarning("Auto notes is already running!");
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

        this.autoNoteCoroutine = StartCoroutine(this.AutoNoteCoroutine());
    }

    [ProButton]
    /// <summary>
    /// Stop auto-adding notes
    /// </summary>
    public void StopAutoNotes()
    {
        if (this.autoNoteCoroutine != null)
        {
            StopCoroutine(this.autoNoteCoroutine);
            this.autoNoteCoroutine = null;
        }
        
        this.isRunning = false;
        this.cellsProcessed = 0;
        Debug.Log("Auto notes stopped.");
    }

    [ProButton]
    /// <summary>
    /// Clear all notes from the grid
    /// </summary>
    public void ClearAllNotes()
    {
        if (this.targetGridView == null)
        {
            this.targetGridView = this.FindActiveGridView();
        }

        if (this.targetGridView == null)
        {
            Debug.LogError("No GridView found!");
            return;
        }

        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                SudokuGridView.CellData cellData = this.targetGridView.GetCellData(row, col);
                if (!cellData.isClue && cellData.value == 0)
                {
                    // Clear all notes by removing them one by one
                    for (int num = 1; num <= GRID_SIZE; num++)
                    {
                        this.targetGridView.RemoveNoteFromCell(row, col, num);
                    }
                }
            }
        }

        Debug.Log("<color=yellow>All notes cleared.</color>");
    }

    [ProButton]
    /// <summary>
    /// Find and show active GridView
    /// </summary>
    public void FindAndShowActiveGridView()
    {
        SudokuGridView activeView = this.FindActiveGridView();
        if (activeView != null)
        {
            Debug.Log($"<color=green>Active GridView found:</color> {activeView.gameObject.name}");
            this.targetGridView = activeView;
        }
        else
        {
            Debug.LogWarning("No active GridView found!");
        }
    }

    /// <summary>
    /// Set target grid view
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
    /// Set delay between notes
    /// </summary>
    public void SetDelay(float delay)
    {
        this.noteDelay = Mathf.Max(0.01f, delay);
    }

    /// <summary>
    /// Check if auto notes is running
    /// </summary>
    public bool IsRunning()
    {
        return this.isRunning;
    }

    private IEnumerator AutoNoteCoroutine()
    {
        this.isRunning = true;
        this.cellsProcessed = 0;
        this.totalNotesAdded = 0;

        Debug.Log($"<color=cyan>Auto notes started on {this.targetGridView.gameObject.name}</color> (delay: {this.noteDelay}s)");

        // Get current puzzle state
        int[,] currentPuzzle = this.targetGridView.GetCurrentUserPuzzle();

        // Collect all empty cells (non-clues)
        List<(int row, int col)> emptyCells = new List<(int, int)>();
        
        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                SudokuGridView.CellData cellData = this.targetGridView.GetCellData(row, col);
                if (!cellData.isClue && cellData.value == 0)
                {
                    emptyCells.Add((row, col));
                }
            }
        }

        this.totalCells = emptyCells.Count;
        Debug.Log($"Found {this.totalCells} empty cells. Adding candidate notes...");

        // Add notes to each empty cell
        foreach (var (row, col) in emptyCells)
        {
            this.cellsProcessed++;

            // Calculate valid candidates for this cell
            List<int> candidates = this.GetCandidates(currentPuzzle, row, col);

            // Add each candidate as a note
            int notesAdded = 0;
            foreach (int candidate in candidates)
            {
                this.targetGridView.AddNoteToCell(row, col, candidate);
                notesAdded++;
                this.totalNotesAdded++;
                
                // Small delay between each note
                yield return new WaitForSeconds(this.noteDelay);
            }

            if (this.cellsProcessed % 10 == 0)
            {
                Debug.Log($"Progress: {this.cellsProcessed}/{this.totalCells} cells processed, {this.totalNotesAdded} notes added");
            }
        }

        this.isRunning = false;
        Debug.Log($"<color=green>Auto notes completed!</color> Added {this.totalNotesAdded} notes to {this.cellsProcessed} cells.");
        
        // Trigger pattern analysis
        if (this.patternAnalyzer != null)
        {
            Debug.Log("<color=cyan>Running pattern analysis...</color>");
            int[,] finalPuzzle = this.targetGridView.GetCurrentUserPuzzle();
            List<int>[,] allNotes = this.targetGridView.GetCellNotes();
            this.patternAnalyzer.AnalyzePatterns(finalPuzzle, allNotes);
        }
    }

    /// <summary>
    /// Get valid candidates for a cell based on Sudoku rules
    /// </summary>
    private List<int> GetCandidates(int[,] puzzle, int row, int col)
    {
        HashSet<int> used = new HashSet<int>();

        // Check row
        for (int c = 0; c < GRID_SIZE; c++)
        {
            if (puzzle[row, c] != 0)
                used.Add(puzzle[row, c]);
        }

        // Check column
        for (int r = 0; r < GRID_SIZE; r++)
        {
            if (puzzle[r, col] != 0)
                used.Add(puzzle[r, col]);
        }

        // Check 3x3 box
        int boxRow = (row / 3) * 3;
        int boxCol = (col / 3) * 3;
        for (int r = boxRow; r < boxRow + 3; r++)
        {
            for (int c = boxCol; c < boxCol + 3; c++)
            {
                if (puzzle[r, c] != 0)
                    used.Add(puzzle[r, c]);
            }
        }

        // Return all numbers not used
        List<int> candidates = new List<int>();
        for (int num = 1; num <= GRID_SIZE; num++)
        {
            if (!used.Contains(num))
                candidates.Add(num);
        }

        return candidates;
    }
}
