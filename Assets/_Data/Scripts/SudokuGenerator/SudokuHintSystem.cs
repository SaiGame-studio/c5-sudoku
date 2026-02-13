using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SudokuHintSystem : SaiBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SudokuPatternAnalyzer patternAnalyzer;

    [Header("Hint Settings")]
    [SerializeField] private bool prioritizeSimplePatterns = true;
    [SerializeField] private bool showHintDescription = true;

    [Header("Current Hint")]
    [SerializeField] private string currentHintMessage = "";
    [SerializeField] private int hintsGivenCount = 0;

    private PatternInfo currentHint;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadPatternAnalyzer();
    }

    private void LoadPatternAnalyzer()
    {
        if (this.patternAnalyzer != null) return;
        this.patternAnalyzer = FindFirstObjectByType<SudokuPatternAnalyzer>();
        Debug.Log(transform.name + ": LoadPatternAnalyzer", gameObject);
    }

    /// <summary>
    /// Get next hint based on detected patterns
    /// </summary>
    public HintResult GetHint(int[,] currentPuzzle, List<int>[,] cellNotes)
    {
        if (this.patternAnalyzer == null)
        {
            return new HintResult
            {
                success = false,
                message = "Pattern analyzer not available",
                patternInfo = null
            };
        }

        this.patternAnalyzer.AnalyzePatterns(currentPuzzle, cellNotes);
        List<PatternInfo> detectedPatterns = this.patternAnalyzer.GetDetectedPatterns();

        if (detectedPatterns == null || detectedPatterns.Count == 0)
        {
            return new HintResult
            {
                success = false,
                message = "No patterns detected. Try adding more notes to the cells to reveal possible patterns.",
                patternInfo = null
            };
        }

        PatternInfo selectedPattern = this.SelectBestPattern(detectedPatterns);
        
        if (selectedPattern == null)
        {
            return new HintResult
            {
                success = false,
                message = "No suitable hint available",
                patternInfo = null
            };
        }

        this.currentHint = selectedPattern;
        this.hintsGivenCount++;
        this.currentHintMessage = this.FormatHintMessage(selectedPattern);

        return new HintResult
        {
            success = true,
            message = this.currentHintMessage,
            patternInfo = selectedPattern
        };
    }

    /// <summary>
    /// Select best pattern for hint based on difficulty and priority
    /// </summary>
    private PatternInfo SelectBestPattern(List<PatternInfo> patterns)
    {
        if (patterns.Count == 0) return null;

        if (!this.prioritizeSimplePatterns)
        {
            return patterns[0];
        }

        var patternPriority = new Dictionary<SudokuPatternAnalyzer.PatternType, int>
        {
            { SudokuPatternAnalyzer.PatternType.FullHouse, 1 },
            { SudokuPatternAnalyzer.PatternType.NakedSingle, 2 },
            { SudokuPatternAnalyzer.PatternType.HiddenSingle, 3 },
            { SudokuPatternAnalyzer.PatternType.NakedPair, 4 },
            { SudokuPatternAnalyzer.PatternType.PointingPair, 5 },
            { SudokuPatternAnalyzer.PatternType.BoxLineReduction, 6 },
            { SudokuPatternAnalyzer.PatternType.NakedTriple, 7 },
            { SudokuPatternAnalyzer.PatternType.HiddenPair, 8 },
            { SudokuPatternAnalyzer.PatternType.XWing, 9 },
            { SudokuPatternAnalyzer.PatternType.XYWing, 10 }
        };

        var sortedPatterns = patterns.OrderBy(p =>
        {
            if (patternPriority.ContainsKey(p.type))
                return patternPriority[p.type];
            return 99;
        }).ToList();

        return sortedPatterns[0];
    }

    /// <summary>
    /// Format hint message with pattern description
    /// </summary>
    private string FormatHintMessage(PatternInfo pattern)
    {
        if (!this.showHintDescription)
        {
            return $"Pattern detected: {pattern.type}";
        }

        string message = $"<b>{pattern.type}</b>\n{pattern.description}";

        if (pattern.suggestedValue > 0 && pattern.affectedCells != null && pattern.affectedCells.Count > 0)
        {
            var cell = pattern.affectedCells[0];
            message += $"\n\n<color=#4CAF50>Suggestion:</color> Cell [{cell.row + 1},{cell.col + 1}] can be {pattern.suggestedValue}";
        }

        return message;
    }

    /// <summary>
    /// Get current hint info
    /// </summary>
    public PatternInfo GetCurrentHint()
    {
        return this.currentHint;
    }

    /// <summary>
    /// Get total hints given
    /// </summary>
    public int GetHintsGivenCount()
    {
        return this.hintsGivenCount;
    }

    /// <summary>
    /// Reset hints counter
    /// </summary>
    public void ResetHintsCounter()
    {
        this.hintsGivenCount = 0;
        this.currentHintMessage = "";
        this.currentHint = null;
    }

    /// <summary>
    /// Get difficulty name for pattern type
    /// </summary>
    public static string GetPatternDifficulty(SudokuPatternAnalyzer.PatternType type)
    {
        switch (type)
        {
            case SudokuPatternAnalyzer.PatternType.FullHouse:
            case SudokuPatternAnalyzer.PatternType.NakedSingle:
                return "Very Easy";
            
            case SudokuPatternAnalyzer.PatternType.HiddenSingle:
                return "Easy";
            
            case SudokuPatternAnalyzer.PatternType.NakedPair:
            case SudokuPatternAnalyzer.PatternType.PointingPair:
                return "Medium";
            
            case SudokuPatternAnalyzer.PatternType.NakedTriple:
            case SudokuPatternAnalyzer.PatternType.HiddenPair:
            case SudokuPatternAnalyzer.PatternType.BoxLineReduction:
                return "Hard";
            
            case SudokuPatternAnalyzer.PatternType.XWing:
            case SudokuPatternAnalyzer.PatternType.XYWing:
                return "Very Hard";
            
            default:
                return "Expert";
        }
    }
}

/// <summary>
/// Result of hint request
/// </summary>
[System.Serializable]
public class HintResult
{
    public bool success;
    public string message;
    public PatternInfo patternInfo;
}
