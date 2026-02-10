using UnityEngine;
using UnityEngine.UIElements;

public class SudokuGridView : SaiBehaviour
{
    private const int GRID_SIZE = 9;
    private const int BOX_SIZE = 3;

    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Dependencies")]
    [SerializeField] private SudokuGenerator sudokuGenerator;

    private VisualElement gridContainer;
    private VisualElement numberPanel;
    private SudokuCell[,] cells;
    private SudokuCell selectedCell;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        if (this.uiDocument == null)
            this.uiDocument = GetComponent<UIDocument>();
        if (this.sudokuGenerator == null)
            this.sudokuGenerator = GetComponent<SudokuGenerator>();
    }

    protected override void Start()
    {
        base.Start();
        this.InitializeGrid();
    }

    private void InitializeGrid()
    {
        VisualElement root = this.uiDocument.rootVisualElement;
        this.gridContainer = root.Q<VisualElement>("sudoku-grid");
        this.numberPanel = root.Q<VisualElement>("number-panel");

        this.cells = new SudokuCell[GRID_SIZE, GRID_SIZE];

        this.BuildGrid();
        this.BuildNumberPanel();
        this.LoadPuzzle();
    }

    private void BuildGrid()
    {
        this.gridContainer.Clear();

        // Build 3 bands (rows of boxes)
        for (int bandIndex = 0; bandIndex < BOX_SIZE; bandIndex++)
        {
            VisualElement band = new VisualElement();
            band.AddToClassList("sudoku-band");

            // Build 3 boxes per band
            for (int boxCol = 0; boxCol < BOX_SIZE; boxCol++)
            {
                VisualElement box = new VisualElement();
                box.AddToClassList("sudoku-box");

                // Build 3 rows per box
                for (int localRow = 0; localRow < BOX_SIZE; localRow++)
                {
                    VisualElement boxRow = new VisualElement();
                    boxRow.AddToClassList("sudoku-box-row");

                    // Build 3 cells per row
                    for (int localCol = 0; localCol < BOX_SIZE; localCol++)
                    {
                        int row = bandIndex * BOX_SIZE + localRow;
                        int col = boxCol * BOX_SIZE + localCol;

                        SudokuCell cell = new SudokuCell(row, col);
                        this.cells[row, col] = cell;

                        // Register click
                        int capturedRow = row;
                        int capturedCol = col;
                        cell.Element.RegisterCallback<ClickEvent>(evt =>
                            this.OnCellClicked(capturedRow, capturedCol));

                        boxRow.Add(cell.Element);
                    }

                    box.Add(boxRow);
                }

                band.Add(box);
            }

            this.gridContainer.Add(band);
        }
    }

    private void BuildNumberPanel()
    {
        this.numberPanel.Clear();

        // Buttons 1-9
        for (int i = 1; i <= GRID_SIZE; i++)
        {
            int number = i;
            VisualElement button = new VisualElement();
            button.AddToClassList("number-button");

            Label label = new Label(number.ToString());
            label.AddToClassList("number-button-label");
            button.Add(label);

            button.RegisterCallback<ClickEvent>(evt => this.OnNumberSelected(number));
            this.numberPanel.Add(button);
        }

        // Erase button
        VisualElement eraseButton = new VisualElement();
        eraseButton.AddToClassList("erase-button");

        Label eraseLabel = new Label("X");
        eraseLabel.AddToClassList("erase-button-label");
        eraseButton.Add(eraseLabel);

        eraseButton.RegisterCallback<ClickEvent>(evt => this.OnNumberSelected(0));
        this.numberPanel.Add(eraseButton);
    }

    private void LoadPuzzle()
    {
        if (this.sudokuGenerator == null) return;

        this.sudokuGenerator.GeneratePuzzle();
        int[,] puzzle = this.sudokuGenerator.GetPuzzle();
        int[,] solution = this.sudokuGenerator.GetSolution();

        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                int value = puzzle[row, col];
                bool isClue = value != 0;
                this.cells[row, col].SetValue(value, isClue);
            }
        }
    }

    private void OnCellClicked(int row, int col)
    {
        this.ClearAllHighlights();

        SudokuCell cell = this.cells[row, col];
        this.selectedCell = cell;
        cell.SetSelected(true);

        this.HighlightRelatedCells(row, col);
        this.HighlightSameNumber(cell.Value);
    }

    private void OnNumberSelected(int number)
    {
        if (this.selectedCell == null) return;
        if (this.selectedCell.IsClue) return;

        this.selectedCell.SetPlayerValue(number);

        // Validate move
        if (number > 0)
        {
            int[,] solution = this.sudokuGenerator.GetSolution();
            bool isCorrect = solution[this.selectedCell.Row, this.selectedCell.Col] == number;
            this.selectedCell.SetError(!isCorrect);
        }
        else
        {
            this.selectedCell.SetError(false);
        }

        // Refresh highlights for new number
        this.ClearAllHighlights();
        this.selectedCell.SetSelected(true);
        this.HighlightRelatedCells(this.selectedCell.Row, this.selectedCell.Col);
        this.HighlightSameNumber(this.selectedCell.Value);
    }

    private void HighlightRelatedCells(int row, int col)
    {
        // Highlight same row and column
        for (int i = 0; i < GRID_SIZE; i++)
        {
            if (i != col) this.cells[row, i].SetHighlighted(true);
            if (i != row) this.cells[i, col].SetHighlighted(true);
        }

        // Highlight same 3x3 box
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

    private void HighlightSameNumber(int number)
    {
        if (number == 0) return;

        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                if (this.cells[row, col].Value == number && this.cells[row, col] != this.selectedCell)
                {
                    this.cells[row, col].SetSameNumber(true);
                }
            }
        }
    }

    private void ClearAllHighlights()
    {
        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                this.cells[row, col].ClearHighlights();
            }
        }
    }

    /// <summary>
    /// Reload puzzle with a new generated board
    /// </summary>
    public void NewGame()
    {
        this.selectedCell = null;
        this.ClearAllHighlights();
        this.LoadPuzzle();
    }

    /// <summary>
    /// Reload puzzle with specified difficulty
    /// </summary>
    public void NewGame(SudokuGenerator.DifficultyLevel difficulty)
    {
        this.sudokuGenerator.SetDifficulty(difficulty);
        this.NewGame();
    }
}
