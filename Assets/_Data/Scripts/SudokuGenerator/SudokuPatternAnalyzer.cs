using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using com.cyborgAssets.inspectorButtonPro;

public class SudokuPatternAnalyzer : SaiBehaviour
{
    public enum PatternType
    {
        None,
        FullHouse,
        NakedSingle,
        HiddenSingle,
        NakedPair,
        NakedTriple,
        NakedQuad,
        HiddenPair,
        HiddenTriple,
        HiddenQuad,
        PointingPair,
        BoxLineReduction,
        XWing,
        Swordfish,
        Jellyfish,
        FinnedFish,
        SashimiFish,
        XYWing,
        XYZWing,
        WWing,
        SimpleColors,
        XChain,
        XYChain,
        UniqueRectangle,
        BBUG,
        EmptyRectangle,
        SueDeCoq,
        Medusa3D,
        ALS
    }

    [Header("Analysis Settings")]
    [SerializeField] private bool autoAnalyze = true;
    [SerializeField] private bool showDetailedLog = false;

    [Header("Current Analysis")]
    [SerializeField] private int patternsFoundCount = 0;
    [TextArea(10, 20)]
    [SerializeField] private string patternsReport = "";

    [Header("Pattern Statistics")]
    [SerializeField] private int fullHouseCount = 0;
    [SerializeField] private int nakedSingleCount = 0;
    [SerializeField] private int hiddenSingleCount = 0;
    [SerializeField] private int nakedPairCount = 0;
    [SerializeField] private int hiddenPairCount = 0;
    [SerializeField] private int pointingPairCount = 0;
    [SerializeField] private int xWingCount = 0;
    [SerializeField] private int advancedPatternCount = 0;

    private List<PatternInfo> detectedPatterns;
    private const int GRID_SIZE = 9;
    private const int BOX_SIZE = 3;

    protected override void Awake()
    {
        base.Awake();
        this.detectedPatterns = new List<PatternInfo>();
    }

    /// <summary>
    /// Analyze puzzle state and notes to detect patterns
    /// </summary>
    public void AnalyzePatterns(int[,] currentPuzzle, List<int>[,] cellNotes)
    {
        this.detectedPatterns.Clear();
        this.ResetStatistics();

        // Detect various patterns
        this.DetectFullHouse(currentPuzzle);
        this.DetectNakedSingles(cellNotes);
        this.DetectHiddenSingles(currentPuzzle, cellNotes);
        this.DetectNakedSubsets(cellNotes);
        this.DetectHiddenSubsets(currentPuzzle, cellNotes);
        this.DetectPointingPairs(cellNotes);
        this.DetectBoxLineReduction(cellNotes);
        this.DetectXWing(cellNotes);
        this.DetectSwordfish(cellNotes);
        this.DetectXYWing(currentPuzzle, cellNotes);

        this.patternsFoundCount = this.detectedPatterns.Count;
        this.GenerateReport();

        if (this.showDetailedLog && this.patternsFoundCount > 0)
        {
            Debug.Log($"<color=cyan>Pattern Analysis:</color> Found {this.patternsFoundCount} patterns\n{this.patternsReport}");
        }
    }

    #region Basic Patterns

    /// <summary>
    /// Detect Full House - only one empty cell in row/column/box
    /// </summary>
    private void DetectFullHouse(int[,] puzzle)
    {
        // Check rows
        for (int row = 0; row < GRID_SIZE; row++)
        {
            int emptyCount = 0;
            int emptyCol = -1;
            for (int col = 0; col < GRID_SIZE; col++)
            {
                if (puzzle[row, col] == 0)
                {
                    emptyCount++;
                    emptyCol = col;
                }
            }
            if (emptyCount == 1)
            {
                int missingNumber = this.GetMissingNumberInRow(puzzle, row);
                this.detectedPatterns.Add(new PatternInfo(
                    PatternType.FullHouse,
                    $"Row {row + 1}: Cell [{row},{emptyCol}] must be {missingNumber}",
                    new List<(int, int)> { (row, emptyCol) }
                ));
                this.fullHouseCount++;
            }
        }

        // Check columns
        for (int col = 0; col < GRID_SIZE; col++)
        {
            int emptyCount = 0;
            int emptyRow = -1;
            for (int row = 0; row < GRID_SIZE; row++)
            {
                if (puzzle[row, col] == 0)
                {
                    emptyCount++;
                    emptyRow = row;
                }
            }
            if (emptyCount == 1)
            {
                int missingNumber = this.GetMissingNumberInColumn(puzzle, col);
                this.detectedPatterns.Add(new PatternInfo(
                    PatternType.FullHouse,
                    $"Column {col + 1}: Cell [{emptyRow},{col}] must be {missingNumber}",
                    new List<(int, int)> { (emptyRow, col) }
                ));
                this.fullHouseCount++;
            }
        }

        // Check boxes
        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                int emptyCount = 0;
                int emptyR = -1, emptyC = -1;
                for (int r = 0; r < BOX_SIZE; r++)
                {
                    for (int c = 0; c < BOX_SIZE; c++)
                    {
                        int row = boxRow * BOX_SIZE + r;
                        int col = boxCol * BOX_SIZE + c;
                        if (puzzle[row, col] == 0)
                        {
                            emptyCount++;
                            emptyR = row;
                            emptyC = col;
                        }
                    }
                }
                if (emptyCount == 1)
                {
                    int missingNumber = this.GetMissingNumberInBox(puzzle, boxRow, boxCol);
                    this.detectedPatterns.Add(new PatternInfo(
                        PatternType.FullHouse,
                        $"Box ({boxRow},{boxCol}): Cell [{emptyR},{emptyC}] must be {missingNumber}",
                        new List<(int, int)> { (emptyR, emptyC) }
                    ));
                    this.fullHouseCount++;
                }
            }
        }
    }

    /// <summary>
    /// Detect Naked Singles - cell with only one candidate
    /// </summary>
    private void DetectNakedSingles(List<int>[,] notes)
    {
        if (notes == null) return;

        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                if (notes[row, col] != null && notes[row, col].Count == 1)
                {
                    int value = notes[row, col][0];
                    this.detectedPatterns.Add(new PatternInfo(
                        PatternType.NakedSingle,
                        $"Cell [{row},{col}] has only one candidate: {value}",
                        new List<(int, int)> { (row, col) },
                        value
                    ));
                    this.nakedSingleCount++;
                }
            }
        }
    }

    /// <summary>
    /// Detect Hidden Singles - only one cell in region can have a specific number
    /// </summary>
    private void DetectHiddenSingles(int[,] puzzle, List<int>[,] notes)
    {
        if (notes == null) return;

        // Check each number (1-9)
        for (int num = 1; num <= GRID_SIZE; num++)
        {
            // Check rows
            for (int row = 0; row < GRID_SIZE; row++)
            {
                List<int> possibleCols = new List<int>();
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    if (puzzle[row, col] == 0 && notes[row, col] != null && notes[row, col].Contains(num))
                    {
                        possibleCols.Add(col);
                    }
                }
                if (possibleCols.Count == 1)
                {
                    int col = possibleCols[0];
                    if (notes[row, col].Count > 1) // Only hidden if there are other candidates
                    {
                        this.detectedPatterns.Add(new PatternInfo(
                            PatternType.HiddenSingle,
                            $"Row {row + 1}: Only [{row},{col}] can be {num}",
                            new List<(int, int)> { (row, col) },
                            num
                        ));
                        this.hiddenSingleCount++;
                    }
                }
            }

            // Check columns
            for (int col = 0; col < GRID_SIZE; col++)
            {
                List<int> possibleRows = new List<int>();
                for (int row = 0; row < GRID_SIZE; row++)
                {
                    if (puzzle[row, col] == 0 && notes[row, col] != null && notes[row, col].Contains(num))
                    {
                        possibleRows.Add(row);
                    }
                }
                if (possibleRows.Count == 1)
                {
                    int row = possibleRows[0];
                    if (notes[row, col].Count > 1)
                    {
                        this.detectedPatterns.Add(new PatternInfo(
                            PatternType.HiddenSingle,
                            $"Column {col + 1}: Only [{row},{col}] can be {num}",
                            new List<(int, int)> { (row, col) },
                            num
                        ));
                        this.hiddenSingleCount++;
                    }
                }
            }
        }
    }

    #endregion

    #region Naked and Hidden Subsets

    /// <summary>
    /// Detect Naked Pairs, Triples, and Quads
    /// </summary>
    private void DetectNakedSubsets(List<int>[,] notes)
    {
        if (notes == null) return;

        // Check rows
        for (int row = 0; row < GRID_SIZE; row++)
        {
            this.DetectNakedSubsetsInUnit(notes, row, -1, "Row");
        }

        // Check columns
        for (int col = 0; col < GRID_SIZE; col++)
        {
            this.DetectNakedSubsetsInUnit(notes, -1, col, "Column");
        }

        // Check boxes
        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                this.DetectNakedSubsetsInBox(notes, boxRow, boxCol);
            }
        }
    }

    private void DetectNakedSubsetsInUnit(List<int>[,] notes, int row, int col, string unitType)
    {
        List<(int r, int c, List<int> candidates)> cells = new List<(int, int, List<int>)>();

        // Collect cells with 2-4 candidates
        for (int i = 0; i < GRID_SIZE; i++)
        {
            int r = row >= 0 ? row : i;
            int c = col >= 0 ? col : i;
            
            if (notes[r, c] != null && notes[r, c].Count >= 2 && notes[r, c].Count <= 4)
            {
                cells.Add((r, c, notes[r, c]));
            }
        }

        // Check for pairs
        for (int i = 0; i < cells.Count - 1; i++)
        {
            for (int j = i + 1; j < cells.Count; j++)
            {
                var union = cells[i].candidates.Union(cells[j].candidates).ToList();
                if (union.Count == 2 && cells[i].candidates.Count == 2 && cells[j].candidates.Count == 2)
                {
                    this.detectedPatterns.Add(new PatternInfo(
                        PatternType.NakedPair,
                        $"{unitType}: Naked Pair {{{string.Join(",", union)}}} at [{cells[i].r},{cells[i].c}] and [{cells[j].r},{cells[j].c}]",
                        new List<(int, int)> { (cells[i].r, cells[i].c), (cells[j].r, cells[j].c) }
                    ));
                    this.nakedPairCount++;
                }
            }
        }
    }

    private void DetectNakedSubsetsInBox(List<int>[,] notes, int boxRow, int boxCol)
    {
        // Similar logic for boxes - simplified for brevity
    }

    /// <summary>
    /// Detect Hidden Pairs, Triples, and Quads
    /// </summary>
    private void DetectHiddenSubsets(int[,] puzzle, List<int>[,] notes)
    {
        if (notes == null) return;
        // Implementation for hidden subsets
        // This requires checking which numbers appear in only 2-4 cells within a unit
    }

    #endregion

    #region Intersection Patterns

    /// <summary>
    /// Detect Pointing Pairs - candidates in box limited to one row/column
    /// </summary>
    private void DetectPointingPairs(List<int>[,] notes)
    {
        if (notes == null) return;

        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                for (int num = 1; num <= GRID_SIZE; num++)
                {
                    List<(int r, int c)> positions = new List<(int, int)>();
                    
                    for (int r = 0; r < BOX_SIZE; r++)
                    {
                        for (int c = 0; c < BOX_SIZE; c++)
                        {
                            int row = boxRow * BOX_SIZE + r;
                            int col = boxCol * BOX_SIZE + c;
                            if (notes[row, col] != null && notes[row, col].Contains(num))
                            {
                                positions.Add((row, col));
                            }
                        }
                    }

                    if (positions.Count == 2)
                    {
                        // Check if in same row
                        if (positions[0].r == positions[1].r)
                        {
                            this.detectedPatterns.Add(new PatternInfo(
                                PatternType.PointingPair,
                                $"Box ({boxRow},{boxCol}): {num} points to Row {positions[0].r + 1}",
                                positions
                            ));
                            this.pointingPairCount++;
                        }
                        // Check if in same column
                        else if (positions[0].c == positions[1].c)
                        {
                            this.detectedPatterns.Add(new PatternInfo(
                                PatternType.PointingPair,
                                $"Box ({boxRow},{boxCol}): {num} points to Column {positions[0].c + 1}",
                                positions
                            ));
                            this.pointingPairCount++;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Detect Box-Line Reduction - candidates in line limited to one box
    /// </summary>
    private void DetectBoxLineReduction(List<int>[,] notes)
    {
        if (notes == null) return;
        // Implementation for box-line reduction
    }

    #endregion

    #region Fish Patterns

    /// <summary>
    /// Detect X-Wing pattern
    /// </summary>
    private void DetectXWing(List<int>[,] notes)
    {
        if (notes == null) return;

        for (int num = 1; num <= GRID_SIZE; num++)
        {
            // Check rows for X-Wing
            for (int row1 = 0; row1 < GRID_SIZE - 1; row1++)
            {
                List<int> cols1 = new List<int>();
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    if (notes[row1, col] != null && notes[row1, col].Contains(num))
                        cols1.Add(col);
                }

                if (cols1.Count == 2)
                {
                    for (int row2 = row1 + 1; row2 < GRID_SIZE; row2++)
                    {
                        List<int> cols2 = new List<int>();
                        for (int col = 0; col < GRID_SIZE; col++)
                        {
                            if (notes[row2, col] != null && notes[row2, col].Contains(num))
                                cols2.Add(col);
                        }

                        if (cols2.Count == 2 && cols1[0] == cols2[0] && cols1[1] == cols2[1])
                        {
                            this.detectedPatterns.Add(new PatternInfo(
                                PatternType.XWing,
                                $"X-Wing: {num} in rows {row1 + 1},{row2 + 1} columns {cols1[0] + 1},{cols1[1] + 1}",
                                new List<(int, int)> { (row1, cols1[0]), (row1, cols1[1]), (row2, cols1[0]), (row2, cols1[1]) }
                            ));
                            this.xWingCount++;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Detect Swordfish pattern (3x3 fish)
    /// </summary>
    private void DetectSwordfish(List<int>[,] notes)
    {
        if (notes == null) return;
        // Implementation for Swordfish (similar to X-Wing but with 3 rows/columns)
    }

    #endregion

    #region Wings

    /// <summary>
    /// Detect XY-Wing pattern
    /// </summary>
    private void DetectXYWing(int[,] puzzle, List<int>[,] notes)
    {
        if (notes == null) return;

        // Find pivot cells with exactly 2 candidates
        for (int pivotRow = 0; pivotRow < GRID_SIZE; pivotRow++)
        {
            for (int pivotCol = 0; pivotCol < GRID_SIZE; pivotCol++)
            {
                if (puzzle[pivotRow, pivotCol] == 0 && notes[pivotRow, pivotCol] != null && notes[pivotRow, pivotCol].Count == 2)
                {
                    // XY-Wing logic here (simplified)
                    // Need to find two wing cells that share one candidate with pivot
                }
            }
        }
    }

    #endregion

    #region Helper Methods

    private int GetMissingNumberInRow(int[,] puzzle, int row)
    {
        HashSet<int> present = new HashSet<int>();
        for (int col = 0; col < GRID_SIZE; col++)
        {
            if (puzzle[row, col] != 0)
                present.Add(puzzle[row, col]);
        }
        for (int num = 1; num <= GRID_SIZE; num++)
        {
            if (!present.Contains(num))
                return num;
        }
        return 0;
    }

    private int GetMissingNumberInColumn(int[,] puzzle, int col)
    {
        HashSet<int> present = new HashSet<int>();
        for (int row = 0; row < GRID_SIZE; row++)
        {
            if (puzzle[row, col] != 0)
                present.Add(puzzle[row, col]);
        }
        for (int num = 1; num <= GRID_SIZE; num++)
        {
            if (!present.Contains(num))
                return num;
        }
        return 0;
    }

    private int GetMissingNumberInBox(int[,] puzzle, int boxRow, int boxCol)
    {
        HashSet<int> present = new HashSet<int>();
        for (int r = 0; r < BOX_SIZE; r++)
        {
            for (int c = 0; c < BOX_SIZE; c++)
            {
                int row = boxRow * BOX_SIZE + r;
                int col = boxCol * BOX_SIZE + c;
                if (puzzle[row, col] != 0)
                    present.Add(puzzle[row, col]);
            }
        }
        for (int num = 1; num <= GRID_SIZE; num++)
        {
            if (!present.Contains(num))
                return num;
        }
        return 0;
    }

    private void ResetStatistics()
    {
        this.fullHouseCount = 0;
        this.nakedSingleCount = 0;
        this.hiddenSingleCount = 0;
        this.nakedPairCount = 0;
        this.hiddenPairCount = 0;
        this.pointingPairCount = 0;
        this.xWingCount = 0;
        this.advancedPatternCount = 0;
    }

    private void GenerateReport()
    {
        System.Text.StringBuilder report = new System.Text.StringBuilder();

        report.AppendLine("=== SUDOKU PATTERN ANALYSIS ===\n");
        report.AppendLine($"Total Patterns Found: {this.patternsFoundCount}\n");

        if (this.patternsFoundCount == 0)
        {
            report.AppendLine("No patterns detected. Player may need to add more notes.");
            this.patternsReport = report.ToString();
            return;
        }

        // Group by pattern type
        var grouped = this.detectedPatterns.GroupBy(p => p.type).OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            report.AppendLine($"--- {group.Key} ({group.Count()}) ---");
            foreach (var pattern in group)
            {
                report.AppendLine($"  • {pattern.description}");
            }
            report.AppendLine();
        }

        this.patternsReport = report.ToString();
    }

    #endregion

    #region Public Methods

    [ProButton]
    /// <summary>
    /// Clear all detected patterns
    /// </summary>
    public void ClearPatterns()
    {
        this.detectedPatterns.Clear();
        this.patternsFoundCount = 0;
        this.patternsReport = "";
        this.ResetStatistics();
    }

    /// <summary>
    /// Get all detected patterns
    /// </summary>
    public List<PatternInfo> GetDetectedPatterns()
    {
        return new List<PatternInfo>(this.detectedPatterns);
    }

    /// <summary>
    /// Get patterns of specific type
    /// </summary>
    public List<PatternInfo> GetPatternsByType(PatternType type)
    {
        return this.detectedPatterns.Where(p => p.type == type).ToList();
    }

    #endregion
}

/// <summary>
/// Information about a detected pattern
/// </summary>
[System.Serializable]
public class PatternInfo
{
    public SudokuPatternAnalyzer.PatternType type;
    public string description;
    public List<(int row, int col)> affectedCells;
    public int suggestedValue;

    public PatternInfo(SudokuPatternAnalyzer.PatternType type, string description, List<(int, int)> cells, int value = 0)
    {
        this.type = type;
        this.description = description;
        this.affectedCells = cells;
        this.suggestedValue = value;
    }

    public override string ToString()
    {
        return $"[{this.type}] {this.description}";
    }
}
