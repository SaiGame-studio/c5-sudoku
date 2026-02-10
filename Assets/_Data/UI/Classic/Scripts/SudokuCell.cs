using System.Collections.Generic;
using UnityEngine.UIElements;

public class SudokuCell
{
    private const string CLASS_CELL = "sudoku-cell";
    private const string CLASS_LABEL = "sudoku-cell-label";
    private const string CLASS_CLUE = "sudoku-cell--clue";
    private const string CLASS_EMPTY = "sudoku-cell--empty";
    private const string CLASS_SELECTED = "sudoku-cell--selected";
    private const string CLASS_HIGHLIGHTED = "sudoku-cell--highlighted";
    private const string CLASS_SAME_NUMBER = "sudoku-cell--same-number";
    private const string CLASS_ERROR = "sudoku-cell--error";

    private const int GRID_SIZE = 9;
    private const int BOX_SIZE = 3;

    private VisualElement container;
    private Label mainLabel;
    private VisualElement notesGrid;
    private Label[] noteLabels;
    private int row;
    private int col;
    private int value;
    private bool isClue;
    private HashSet<int> notes;

    public int Row => this.row;
    public int Col => this.col;
    public int Value => this.value;
    public bool IsClue => this.isClue;
    public VisualElement Element => this.container;
    public HashSet<int> Notes => this.notes;

    public SudokuCell(int row, int col)
    {
        this.row = row;
        this.col = col;
        this.value = 0;
        this.isClue = false;
        this.notes = new HashSet<int>();

        this.container = new VisualElement();
        this.container.AddToClassList(CLASS_CELL);

        // Main number label
        this.mainLabel = new Label();
        this.mainLabel.AddToClassList(CLASS_LABEL);
        this.container.Add(this.mainLabel);

        // Notes 3x3 grid
        this.notesGrid = new VisualElement();
        this.notesGrid.AddToClassList("sudoku-notes-grid");
        this.notesGrid.style.display = DisplayStyle.None;
        this.noteLabels = new Label[GRID_SIZE + 1]; // index 1-9

        for (int r = 0; r < BOX_SIZE; r++)
        {
            VisualElement noteRow = new VisualElement();
            noteRow.AddToClassList("sudoku-notes-row");

            for (int c = 0; c < BOX_SIZE; c++)
            {
                int num = r * BOX_SIZE + c + 1;
                Label noteLabel = new Label();
                noteLabel.AddToClassList("sudoku-note-label");
                this.noteLabels[num] = noteLabel;
                noteRow.Add(noteLabel);
            }

            this.notesGrid.Add(noteRow);
        }

        this.container.Add(this.notesGrid);
    }

    public void SetValue(int number, bool isClue)
    {
        this.value = number;
        this.isClue = isClue;
        this.notes.Clear();
        this.mainLabel.text = number > 0 ? number.ToString() : "";

        this.container.RemoveFromClassList(CLASS_CLUE);
        this.container.RemoveFromClassList(CLASS_EMPTY);

        if (isClue)
        {
            this.container.AddToClassList(CLASS_CLUE);
        }
        else
        {
            this.container.AddToClassList(CLASS_EMPTY);
        }

        this.RefreshDisplay();
    }

    public void SetPlayerValue(int number)
    {
        if (this.isClue) return;

        this.value = number;
        this.notes.Clear();
        this.mainLabel.text = number > 0 ? number.ToString() : "";
        this.RefreshDisplay();
    }

    public void ToggleNote(int number)
    {
        if (this.isClue) return;
        if (number < 1 || number > GRID_SIZE) return;

        // Clear main value when adding notes
        if (this.value > 0)
        {
            this.value = 0;
            this.mainLabel.text = "";
        }

        if (this.notes.Contains(number))
        {
            this.notes.Remove(number);
        }
        else
        {
            this.notes.Add(number);
        }

        this.RefreshDisplay();
    }

    public bool HasNote(int number)
    {
        return this.notes.Contains(number);
    }

    private void RefreshDisplay()
    {
        bool showNotes = this.value == 0 && this.notes.Count > 0;

        this.mainLabel.style.display = showNotes ? DisplayStyle.None : DisplayStyle.Flex;
        this.notesGrid.style.display = showNotes ? DisplayStyle.Flex : DisplayStyle.None;

        if (showNotes)
        {
            for (int i = 1; i <= GRID_SIZE; i++)
            {
                this.noteLabels[i].text = this.notes.Contains(i) ? i.ToString() : "";
            }
        }
    }

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            this.container.AddToClassList(CLASS_SELECTED);
        }
        else
        {
            this.container.RemoveFromClassList(CLASS_SELECTED);
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlighted)
        {
            this.container.AddToClassList(CLASS_HIGHLIGHTED);
        }
        else
        {
            this.container.RemoveFromClassList(CLASS_HIGHLIGHTED);
        }
    }

    public void SetSameNumber(bool sameNumber)
    {
        if (sameNumber)
        {
            this.container.AddToClassList(CLASS_SAME_NUMBER);
        }
        else
        {
            this.container.RemoveFromClassList(CLASS_SAME_NUMBER);
        }
    }

    public void SetError(bool error)
    {
        if (error)
        {
            this.container.AddToClassList(CLASS_ERROR);
        }
        else
        {
            this.container.RemoveFromClassList(CLASS_ERROR);
        }
    }

    public void ClearHighlights()
    {
        this.container.RemoveFromClassList(CLASS_SELECTED);
        this.container.RemoveFromClassList(CLASS_HIGHLIGHTED);
        this.container.RemoveFromClassList(CLASS_SAME_NUMBER);
        this.container.RemoveFromClassList(CLASS_ERROR);
    }
}
