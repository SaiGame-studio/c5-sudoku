using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class SudokuGridViewHintManager
{
    private const int GRID_SIZE = 9;

    private SudokuHintSystem hintSystem;
    private SudokuCell[,] cells;
    private Label patternNameLabel;
    private PatternInfo currentHintPattern;

    public SudokuGridViewHintManager(SudokuHintSystem hintSystem, SudokuCell[,] cells, Label patternNameLabel)
    {
        this.hintSystem = hintSystem;
        this.cells = cells;
        this.patternNameLabel = patternNameLabel;
    }

    public void RequestHint(int[,] currentPuzzle, List<int>[,] cellNotes)
    {
        if (this.hintSystem == null)
        {
            Debug.LogWarning("Hint System is not available");
            return;
        }

        HintResult result = this.hintSystem.GetHint(currentPuzzle, cellNotes);

        if (result.success)
        {
            this.ShowHint(result);
        }
        else
        {
            this.ShowNoHintMessage(result.message);
        }
    }

    private void ShowHint(HintResult result)
    {
        if (result.patternInfo == null) return;

        this.currentHintPattern = result.patternInfo;

        if (this.patternNameLabel != null)
        {
            this.patternNameLabel.text = $"{result.patternInfo.type.ToString()}";
        }

        if (result.patternInfo.affectedCells != null && result.patternInfo.affectedCells.Count > 0)
        {
            foreach (var cellPos in result.patternInfo.affectedCells)
            {
                if (cellPos.row >= 0 && cellPos.row < GRID_SIZE && cellPos.col >= 0 && cellPos.col < GRID_SIZE)
                {
                    this.cells[cellPos.row, cellPos.col].SetHint(true);
                }
            }
        }

        Debug.Log($"<color=cyan>Hint:</color> {result.message}");
    }

    private void ShowNoHintMessage(string message)
    {
        if (this.patternNameLabel != null)
        {
            this.patternNameLabel.text = "No hint available";
        }

        Debug.Log($"<color=yellow>No Hint Available:</color> {message}");
    }

    public void ClearHint()
    {
        if (this.patternNameLabel != null)
        {
            this.patternNameLabel.text = "";
        }

        if (this.currentHintPattern != null && this.currentHintPattern.affectedCells != null)
        {
            foreach (var cellPos in this.currentHintPattern.affectedCells)
            {
                if (cellPos.row >= 0 && cellPos.row < GRID_SIZE && cellPos.col >= 0 && cellPos.col < GRID_SIZE)
                {
                    this.cells[cellPos.row, cellPos.col].SetHint(false);
                }
            }
        }

        this.currentHintPattern = null;
    }
}
