using com.cyborgAssets.inspectorButtonPro;
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

    [Header("Popup Settings")]
    [SerializeField] private Vector2 popupOffset = new Vector2(0f, -10f);

    private VisualElement root;
    private VisualElement gridContainer;
    private VisualElement popupOverlay;
    private VisualElement popupContainer;
    private VisualElement themeToggle;
    private Label themeToggleLabel;
    private VisualElement difficultyStarsContainer;
    private SudokuCell[,] cells;
    private SudokuCell selectedCell;
    private bool isLightMode;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadUIDocument();
        this.LoadSudokuGenerator();
    }

    private void LoadUIDocument()
    {
        if (this.uiDocument != null) return;
        this.uiDocument = GetComponent<UIDocument>();
        Debug.Log(transform.name + ": LoadUIDocument", gameObject);
    }

    private void LoadSudokuGenerator()
    {
        if (this.sudokuGenerator != null) return;
        this.sudokuGenerator = FindFirstObjectByType<SudokuGenerator>();
        Debug.Log(transform.name + ": LoadSudokuGenerator", gameObject);
    }

    protected override void Start()
    {
        base.Start();
        this.InitializeGrid();
    }

    [ProButton]
    public void InitializeGrid()
    {
        this.root = this.uiDocument.rootVisualElement;
        this.gridContainer = this.root.Q<VisualElement>("sudoku-grid");
        this.popupOverlay = this.root.Q<VisualElement>("popup-overlay");
        this.popupContainer = this.root.Q<VisualElement>("popup-container");
        this.themeToggle = this.root.Q<VisualElement>("theme-toggle");
        this.themeToggleLabel = this.root.Q<Label>("theme-toggle-label");
        this.difficultyStarsContainer = this.root.Q<VisualElement>("difficulty-stars");

        this.cells = new SudokuCell[GRID_SIZE, GRID_SIZE];

        // Theme toggle click
        this.themeToggle.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            this.ToggleTheme();
        });

        // Click overlay background to close popup
        this.popupOverlay.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == this.popupOverlay)
                this.HidePopup();
        });

        // Keyboard input for fill and notes
        this.root.RegisterCallback<KeyDownEvent>(this.OnKeyDown);
        this.root.focusable = true;
        this.root.Focus();

        this.BuildGrid();
        this.LoadPuzzle();
        this.RefreshDifficultyStars();
    }

    private void BuildGrid()
    {
        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                VisualElement existingElement = this.root.Q<VisualElement>("cell-" + row + "-" + col);
                if (existingElement == null) continue;

                SudokuCell cell = new SudokuCell(row, col, existingElement);
                this.cells[row, col] = cell;

                int capturedRow = row;
                int capturedCol = col;
                cell.Element.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    this.OnCellClicked(capturedRow, capturedCol);
                });
            }
        }
    }

    private void LoadPuzzle()
    {
        if (this.sudokuGenerator == null) return;

        this.sudokuGenerator.GeneratePuzzle();
        int[,] puzzle = this.sudokuGenerator.GetPuzzle();

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

        // Show popup for non-clue cells
        if (!cell.IsClue)
        {
            this.ShowPopup(cell);
        }
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (this.selectedCell == null) return;
        if (this.selectedCell.IsClue) return;

        if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
        {
            this.OnErase();
            evt.StopPropagation();
            evt.PreventDefault();
            return;
        }

        int number = this.KeyCodeToNumber(evt.keyCode);
        if (number < 1 || number > GRID_SIZE) return;

        if (evt.shiftKey)
        {
            this.selectedCell.ToggleNote(number);
            this.selectedCell.SetError(false);
        }
        else
        {
            this.OnFillNumber(number);
        }

        evt.StopPropagation();
        evt.PreventDefault();
    }

    private int KeyCodeToNumber(KeyCode keyCode)
    {
        if (keyCode >= KeyCode.Alpha1 && keyCode <= KeyCode.Alpha9)
            return keyCode - KeyCode.Alpha0;
        if (keyCode >= KeyCode.Keypad1 && keyCode <= KeyCode.Keypad9)
            return keyCode - KeyCode.Keypad0;
        return -1;
    }

    #region Popup
    private void ShowPopup(SudokuCell cell)
    {
        this.popupContainer.Clear();
        this.popupOverlay.RemoveFromClassList("popup-overlay--hidden");

        // Position popup near the clicked cell
        this.popupContainer.RegisterCallbackOnce<GeometryChangedEvent>(evt =>
            this.PositionPopup(cell));

        // === Fill row (large numbers) ===
        VisualElement fillRow = new VisualElement();
        fillRow.AddToClassList("popup-row");

        for (int i = 1; i <= GRID_SIZE; i++)
        {
            int number = i;
            VisualElement button = new VisualElement();
            button.AddToClassList("popup-fill-button");

            Label label = new Label(number.ToString());
            label.AddToClassList("popup-fill-label");
            button.Add(label);

            button.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                this.OnFillNumber(number);
            });

            fillRow.Add(button);
        }

        this.popupContainer.Add(fillRow);

        // === Separator ===
        VisualElement separator = new VisualElement();
        separator.AddToClassList("popup-separator");
        this.popupContainer.Add(separator);

        // === Note row (small numbers) ===
        VisualElement noteRow = new VisualElement();
        noteRow.AddToClassList("popup-row");

        for (int i = 1; i <= GRID_SIZE; i++)
        {
            int number = i;
            VisualElement button = new VisualElement();
            button.AddToClassList("popup-note-button");

            // Mark active if note already exists
            if (cell.HasNote(number))
                button.AddToClassList("popup-note-button--active");

            Label label = new Label(number.ToString());
            label.AddToClassList("popup-note-label");
            button.Add(label);

            button.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                this.OnToggleNote(number, button);
            });

            noteRow.Add(button);
        }

        this.popupContainer.Add(noteRow);

        // === Erase button ===
        VisualElement eraseButton = new VisualElement();
        eraseButton.AddToClassList("popup-erase-button");

        Label eraseLabel = new Label("\uf12d");
        eraseLabel.AddToClassList("popup-erase-label");
        eraseButton.Add(eraseLabel);

        eraseButton.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            this.OnErase();
        });

        this.popupContainer.Add(eraseButton);
    }

    private void PositionPopup(SudokuCell cell)
    {
        Rect cellBound = cell.Element.worldBound;
        float popupWidth = this.popupContainer.resolvedStyle.width;
        float popupHeight = this.popupContainer.resolvedStyle.height;
        float rootWidth = this.root.resolvedStyle.width;
        float rootHeight = this.root.resolvedStyle.height;

        // Center horizontally on cell, apply X offset
        float x = cellBound.center.x - popupWidth / 2f + this.popupOffset.x;

        // Place popup above the cell top edge with Y offset as gap
        float y = cellBound.yMin - popupHeight + this.popupOffset.y;

        // If popup goes above screen, flip to below cell
        if (y < 0)
            y = cellBound.yMax - this.popupOffset.y;

        // Clamp horizontal
        if (x < 4f) x = 4f;
        if (x + popupWidth > rootWidth - 4f) x = rootWidth - popupWidth - 4f;

        // Clamp vertical
        if (y + popupHeight > rootHeight - 4f) y = rootHeight - popupHeight - 4f;

        this.popupContainer.style.left = x;
        this.popupContainer.style.top = y;
    }

    private void HidePopup()
    {
        this.popupOverlay.AddToClassList("popup-overlay--hidden");
        this.popupContainer.Clear();
    }

    private void OnFillNumber(int number)
    {
        if (this.selectedCell == null) return;

        this.selectedCell.SetPlayerValue(number);

        // Validate
        int[,] solution = this.sudokuGenerator.GetSolution();
        bool isCorrect = solution[this.selectedCell.Row, this.selectedCell.Col] == number;
        this.selectedCell.SetError(!isCorrect);

        this.HidePopup();
        this.RefreshHighlights();
    }

    private void OnToggleNote(int number, VisualElement button)
    {
        if (this.selectedCell == null) return;

        this.selectedCell.ToggleNote(number);
        this.selectedCell.SetError(false);

        // Toggle active style on the button
        if (this.selectedCell.HasNote(number))
            button.AddToClassList("popup-note-button--active");
        else
            button.RemoveFromClassList("popup-note-button--active");
    }

    private void OnErase()
    {
        if (this.selectedCell == null) return;

        this.selectedCell.SetPlayerValue(0);
        this.selectedCell.SetError(false);
        this.HidePopup();
        this.RefreshHighlights();
    }
    #endregion

    #region Highlights
    private void RefreshHighlights()
    {
        this.ClearAllHighlights();
        if (this.selectedCell != null)
        {
            this.selectedCell.SetSelected(true);
            this.HighlightRelatedCells(this.selectedCell.Row, this.selectedCell.Col);
            this.HighlightSameNumber(this.selectedCell.Value);
        }
    }

    private void HighlightRelatedCells(int row, int col)
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
    #endregion

    #region Public Methods
    /// <summary>
    /// Reload puzzle with a new generated board
    /// </summary>
    public void NewGame()
    {
        this.selectedCell = null;
        this.HidePopup();
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
        this.RefreshDifficultyStars();
    }

    private const int TOTAL_STARS = 7;

    private void RefreshDifficultyStars()
    {
        if (this.sudokuGenerator == null || this.difficultyStarsContainer == null) return;

        int activeCount = (int)this.sudokuGenerator.GetDifficulty() + 1;

        for (int i = 1; i <= TOTAL_STARS; i++)
        {
            Label star = this.difficultyStarsContainer.Q<Label>("star-" + i);
            if (star == null) continue;

            if (i <= activeCount)
                star.AddToClassList("difficulty-star--active");
            else
                star.RemoveFromClassList("difficulty-star--active");
        }
    }

    public void ToggleTheme()
    {
        this.isLightMode = !this.isLightMode;

        if (this.isLightMode)
        {
            this.root.AddToClassList("light-mode");
            this.themeToggleLabel.text = "\u2600"; // Sun
        }
        else
        {
            this.root.RemoveFromClassList("light-mode");
            this.themeToggleLabel.text = "\u263E"; // Moon
        }
    }
    #endregion
}
