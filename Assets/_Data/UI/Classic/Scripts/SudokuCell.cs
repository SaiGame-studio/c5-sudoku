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

    private VisualElement container;
    private Label label;
    private int row;
    private int col;
    private int value;
    private bool isClue;

    public int Row => this.row;
    public int Col => this.col;
    public int Value => this.value;
    public bool IsClue => this.isClue;
    public VisualElement Element => this.container;

    public SudokuCell(int row, int col)
    {
        this.row = row;
        this.col = col;
        this.value = 0;
        this.isClue = false;

        this.container = new VisualElement();
        this.container.AddToClassList(CLASS_CELL);

        this.label = new Label();
        this.label.AddToClassList(CLASS_LABEL);
        this.container.Add(this.label);
    }

    public void SetValue(int number, bool isClue)
    {
        this.value = number;
        this.isClue = isClue;
        this.label.text = number > 0 ? number.ToString() : "";

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
    }

    public void SetPlayerValue(int number)
    {
        if (this.isClue) return;

        this.value = number;
        this.label.text = number > 0 ? number.ToString() : "";
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
