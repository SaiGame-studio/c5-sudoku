using UnityEngine;

public class SudokuGridViewHighlightManager
{
    private const int GRID_SIZE = 9;
    private const int BOX_SIZE = 3;

    private SudokuCell[,] cells;

    public SudokuGridViewHighlightManager(SudokuCell[,] cells)
    {
        this.cells = cells;
    }

    public void RefreshHighlights(SudokuCell selectedCell)
    {
        this.ClearAllHighlights();
        if (selectedCell != null)
        {
            selectedCell.SetSelected(true);
            this.HighlightRelatedCells(selectedCell.Row, selectedCell.Col);
            this.HighlightSameNumber(selectedCell.Value);
        }
    }

    public void HighlightRelatedCells(int row, int col)
    {
        for (int i = 0; i < GRID_SIZE; i++)
        {
            if (i != col) this.cells[row, i].SetHighlighted(true);
            if (i != row) this.cells[i, col].SetHighlighted(true);
        }

        int startRow = (row / BOX_SIZE) * BOX_SIZE;
        int startCol = (col / BOX_SIZE) * BOX_SIZE;

        for (int r = startRow; r < startRow + BOX_SIZE; r++)
        {
            for (int c = startCol; c < startCol + BOX_SIZE; c++)
            {
                if (r != row || c != col)
                    this.cells[r, c].SetHighlighted(true);
            }
        }
    }

    public void HighlightSameNumber(int number, SudokuCell excludeCell = null)
    {
        if (number == 0) return;

        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                if (this.cells[row, col].Value == number && this.cells[row, col] != excludeCell)
                {
                    this.cells[row, col].SetSameNumber(true);
                }
            }
        }
    }

    public void ClearAllHighlights()
    {
        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                this.cells[row, col].ClearHighlights();
            }
        }
    }
}
