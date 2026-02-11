using com.cyborgAssets.inspectorButtonPro;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class SudokuGridView : SaiBehaviour
{
    private const int GRID_SIZE = 9;
    private const int BOX_SIZE = 3;

    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Dependencies")]
    [SerializeField] private SudokuGenerator sudokuGenerator;
    [SerializeField] private SudokuResultAnalyzer resultAnalyzer;
    [SerializeField] private SudokuPatternAnalyzer patternAnalyzer;
    [SerializeField] private SudokuHintSystem hintSystem;
    [SerializeField] private SudokuAutoNotes autoNotes;

    [Header("Popup Settings")]
    [SerializeField] private Vector2 popupOffset = new Vector2(0f, -10f);

    [Header("Live Analysis")]
    [SerializeField] private bool autoAnalyze = true;
    [SerializeField] private bool autoAnalyzePatterns = true;
    [SerializeField] private float completionPercentage = 0f;
    [SerializeField] private int correctCells = 0;
    [SerializeField] private int incorrectCells = 0;
    [SerializeField] private SudokuResultAnalyzer.GameResult currentResult = SudokuResultAnalyzer.GameResult.NotCompleted;
    [TextArea(11, 11)]
    [SerializeField] private string userPuzzlePreview = "";

    private VisualElement root;
    private VisualElement gridContainer;
    private VisualElement popupOverlay;
    private VisualElement popupContainer;
    private VisualElement themeToggle;
    private Label themeToggleLabel;
    private VisualElement difficultyStarsContainer;
    private Button hintButton;
    private Button autoNotesButton;
    private Label patternNameLabel;
    private SudokuCell[,] cells;
    private SudokuCell selectedCell;
    private bool isLightMode;
    private int[,] cachedSolution;
    private PatternInfo currentHintPattern;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadUIDocument();
        this.LoadSudokuGenerator();
        this.LoadResultAnalyzer();
        this.LoadPatternAnalyzer();
        this.LoadHintSystem();
        this.LoadAutoNotes();
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

    private void LoadResultAnalyzer()
    {
        if (this.resultAnalyzer != null) return;
        this.resultAnalyzer = FindFirstObjectByType<SudokuResultAnalyzer>();
        Debug.Log(transform.name + ": LoadResultAnalyzer", gameObject);
    }

    private void LoadPatternAnalyzer()
    {
        if (this.patternAnalyzer != null) return;
        this.patternAnalyzer = FindFirstObjectByType<SudokuPatternAnalyzer>();
        Debug.Log(transform.name + ": LoadPatternAnalyzer", gameObject);
    }

    private void LoadHintSystem()
    {
        if (this.hintSystem != null) return;
        this.hintSystem = FindFirstObjectByType<SudokuHintSystem>();
        Debug.Log(transform.name + ": LoadHintSystem", gameObject);
    }

    private void LoadAutoNotes()
    {
        if (this.autoNotes != null) return;
        this.autoNotes = FindFirstObjectByType<SudokuAutoNotes>();
        Debug.Log(transform.name + ": LoadAutoNotes", gameObject);
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
        this.hintButton = this.root.Q<Button>("hint-button");
        this.autoNotesButton = this.root.Q<Button>("auto-notes-button");
        this.patternNameLabel = this.root.Q<Label>("pattern-name-label");

        this.cells = new SudokuCell[GRID_SIZE, GRID_SIZE];

        // Theme toggle click
        this.themeToggle.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            this.ToggleTheme();
        });

        // Hint button click
        if (this.hintButton != null)
        {
            this.hintButton.clicked += this.OnHintButtonClicked;
        }

        // Auto Notes button click
        if (this.autoNotesButton != null)
        {
            this.autoNotesButton.clicked += this.OnAutoNotesButtonClicked;
        }

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
        this.ClearAllNotes();
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
        this.cachedSolution = this.sudokuGenerator.GetSolution();

        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                int value = puzzle[row, col];
                bool isClue = value != 0;
                this.cells[row, col].SetValue(value, isClue);
            }
        }

        // Initialize preview
        if (this.autoAnalyze)
        {
            this.AnalyzeCurrentState();
        }

        // Initial pattern analysis
        if (this.autoAnalyzePatterns)
        {
            this.AnalyzePatterns();
        }
    }

    private void OnCellClicked(int row, int col)
    {
        this.ClearAllHighlights();
        this.ClearHint();

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

        // Validate with cached solution
        bool isCorrect = this.cachedSolution != null && 
                         this.cachedSolution[this.selectedCell.Row, this.selectedCell.Col] == number;
        this.selectedCell.SetError(!isCorrect);

        this.HidePopup();
        this.RefreshHighlights();

        // Auto analyze after filling number
        if (this.autoAnalyze)
        {
            this.AnalyzeCurrentState();
        }
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

        // Auto analyze patterns after note change
        if (this.autoAnalyzePatterns)
        {
            this.AnalyzePatterns();
        }
    }

    private void OnErase()
    {
        if (this.selectedCell == null) return;

        this.selectedCell.SetPlayerValue(0);
        this.selectedCell.SetError(false);
        this.HidePopup();
        this.RefreshHighlights();

        // Auto analyze after erasing
        if (this.autoAnalyze)
        {
            this.AnalyzeCurrentState();
        }
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
        
        // Reset analysis stats
        this.completionPercentage = 0f;
        this.correctCells = 0;
        this.incorrectCells = 0;
        this.currentResult = SudokuResultAnalyzer.GameResult.NotCompleted;
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

    private const int TOTAL_STARS = 9;

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

    #region Result Analysis
    /// <summary>
    /// Automatically analyze current puzzle state
    /// </summary>
    private void AnalyzeCurrentState()
    {
        if (this.resultAnalyzer == null || this.cachedSolution == null) return;

        int[,] userPuzzle = this.GetCurrentUserPuzzle();
        int[,] solution = this.cachedSolution;

        // Submit for analysis
        SudokuResultAnalyzer.GameResult result = this.resultAnalyzer.SubmitSolution(
            userPuzzle,
            solution,
            gameTime: Time.time, // Can track actual game time
            hints: 0 // Can track hints used
        );

        // Update stats for inspector visibility
        this.currentResult = result;
        this.completionPercentage = this.resultAnalyzer.GetCompletionPercentage();
        
        // Count correct and incorrect cells
        this.correctCells = 0;
        this.incorrectCells = 0;
        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                int userValue = userPuzzle[row, col];
                if (userValue != 0)
                {
                    if (userValue == solution[row, col])
                        this.correctCells++;
                    else
                        this.incorrectCells++;
                }
            }
        }

        // Log result if victory or defeat
        if (result == SudokuResultAnalyzer.GameResult.Victory)
        {
            Debug.Log("<color=green>VICTORY!</color> Puzzle solved correctly!");
            Debug.Log(this.resultAnalyzer.GetAnalysisReport());
        }
        else if (result == SudokuResultAnalyzer.GameResult.Defeat)
        {
            Debug.Log($"<color=yellow>Errors detected:</color> {this.incorrectCells} incorrect cells");
        }

        // Update preview for inspector
        this.userPuzzlePreview = this.GetUserPuzzlePreview(userPuzzle, solution);
    }

    /// <summary>
    /// Generate visual preview of user's puzzle with error indicators
    /// </summary>
    private string GetUserPuzzlePreview(int[,] userPuzzle, int[,] solution)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        for (int i = 0; i < GRID_SIZE; i++)
        {
            if (i % BOX_SIZE == 0 && i != 0)
                sb.AppendLine("------+-------+------");

            for (int j = 0; j < GRID_SIZE; j++)
            {
                if (j % BOX_SIZE == 0 && j != 0)
                    sb.Append("| ");

                int userValue = userPuzzle[i, j];
                if (userValue == 0)
                {
                    sb.Append("· ");
                }
                else if (userValue == solution[i, j])
                {
                    sb.Append(userValue + " ");
                }
                else
                {
                    sb.Append("X ");  // Mark errors with X
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Get current game result
    /// </summary>
    public SudokuResultAnalyzer.GameResult GetCurrentResult()
    {
        return this.currentResult;
    }

    /// <summary>
    /// Get current completion percentage
    /// </summary>
    public float GetCompletionPercentage()
    {
        return this.completionPercentage;
    }

    /// <summary>
    /// Get all cell notes for pattern analysis
    /// </summary>
    private List<int>[,] GetAllCellNotes()
    {
        List<int>[,] allNotes = new List<int>[GRID_SIZE, GRID_SIZE];

        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                if (this.cells[row, col].Notes != null && this.cells[row, col].Notes.Count > 0)
                {
                    allNotes[row, col] = new List<int>(this.cells[row, col].Notes);
                }
                else
                {
                    allNotes[row, col] = new List<int>();
                }
            }
        }

        return allNotes;
    }

    /// <summary>
    /// Analyze patterns based on current notes
    /// </summary>
    private void AnalyzePatterns()
    {
        if (this.patternAnalyzer == null) return;

        int[,] currentPuzzle = this.GetCurrentUserPuzzle();
        List<int>[,] allNotes = this.GetAllCellNotes();

        this.patternAnalyzer.AnalyzePatterns(currentPuzzle, allNotes);
    }

    /// <summary>
    /// Get all cell notes (public for external access)
    /// </summary>
    public List<int>[,] GetCellNotes()
    {
        return this.GetAllCellNotes();
    }

    /// <summary>
    /// Get cell data for external access (e.g., AutoPlayer)
    /// </summary>
    public CellData GetCellData(int row, int col)
    {
        if (row < 0 || row >= GRID_SIZE || col < 0 || col >= GRID_SIZE)
        {
            return new CellData { value = 0, isClue = false };
        }

        SudokuCell cell = this.cells[row, col];
        return new CellData
        {
            value = cell.Value,
            isClue = cell.IsClue
        };
    }

    /// <summary>
    /// Fill a specific cell with a number (for external control like AutoPlayer)
    /// </summary>
    public void FillCell(int row, int col, int number)
    {
        if (row < 0 || row >= GRID_SIZE || col < 0 || col >= GRID_SIZE)
            return;

        SudokuCell cell = this.cells[row, col];
        if (cell.IsClue) return;

        // Clear current selection and highlights
        this.ClearAllHighlights();

        // Select the cell
        this.selectedCell = cell;
        cell.SetSelected(true);
        this.HighlightRelatedCells(row, col);

        // Fill with number
        cell.SetPlayerValue(number);

        // Validate with cached solution
        bool isCorrect = this.cachedSolution != null && 
                         this.cachedSolution[row, col] == number;
        cell.SetError(!isCorrect);

        // Refresh highlights
        this.HighlightSameNumber(number);

        // Auto analyze if enabled
        if (this.autoAnalyze)
        {
            this.AnalyzeCurrentState();
        }
    }

    /// <summary>
    /// Add a note to a specific cell (for external control like AutoNotes)
    /// </summary>
    public void AddNoteToCell(int row, int col, int number)
    {
        if (row < 0 || row >= GRID_SIZE || col < 0 || col >= GRID_SIZE)
            return;

        SudokuCell cell = this.cells[row, col];
        if (cell.IsClue || cell.Value != 0) return;

        // Add note if not already present
        if (!cell.HasNote(number))
        {
            cell.ToggleNote(number);
        }
    }

    /// <summary>
    /// Remove a note from a specific cell (for external control like AutoNotes)
    /// </summary>
    public void RemoveNoteFromCell(int row, int col, int number)
    {
        if (row < 0 || row >= GRID_SIZE || col < 0 || col >= GRID_SIZE)
            return;

        SudokuCell cell = this.cells[row, col];
        
        // Remove note if present
        if (cell.HasNote(number))
        {
            cell.ToggleNote(number);
        }
    }

    /// <summary>
    /// Get current user puzzle state (for external access like AutoNotes)
    /// </summary>
    public int[,] GetCurrentUserPuzzle()
    {
        int[,] puzzle = new int[GRID_SIZE, GRID_SIZE];
        
        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                puzzle[row, col] = this.cells[row, col].Value;
            }
        }
        
        return puzzle;
    }

    /// <summary>
    /// Cell data structure for external access
    /// </summary>
    public struct CellData
    {
        public int value;
        public bool isClue;
    }
    #endregion

    #region Hint System

    /// <summary>
    /// Handle hint button click
    /// </summary>
    private void OnHintButtonClicked()
    {
        if (this.hintSystem == null)
        {
            Debug.LogWarning("Hint System is not available");
            return;
        }

        int[,] currentPuzzle = this.GetCurrentUserPuzzle();
        List<int>[,] cellNotes = this.GetAllCellNotes();

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

    private void OnAutoNotesButtonClicked()
    {
        if (this.autoNotes == null)
        {
            Debug.LogWarning("Auto Notes System is not available");
            return;
        }

        // Use the existing SudokuAutoNotes component
        this.autoNotes.StartAutoNotes();
    }

    private void ClearAllNotes()
    {
        if (this.cells == null) return;

        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                if (this.cells[row, col] != null)
                {
                    this.cells[row, col].ClearNotes();
                }
            }
        }
    }

    /// <summary>
    /// Show hint with visual feedback
    /// </summary>
    private void ShowHint(HintResult result)
    {
        if (result.patternInfo == null) return;

        this.currentHintPattern = result.patternInfo;

        // Update pattern display label
        if (this.patternNameLabel != null)
        {
            this.patternNameLabel.text = $"{result.patternInfo.type.ToString()}";
        }

        this.ClearAllHighlights();

        if (result.patternInfo.affectedCells != null && result.patternInfo.affectedCells.Count > 0)
        {
            foreach (var cellPos in result.patternInfo.affectedCells)
            {
                if (cellPos.row >= 0 && cellPos.row < GRID_SIZE && cellPos.col >= 0 && cellPos.col < GRID_SIZE)
                {
                    this.cells[cellPos.row, cellPos.col].SetHint(true);
                }
            }

            var firstCell = result.patternInfo.affectedCells[0];
            this.selectedCell = this.cells[firstCell.row, firstCell.col];
            this.selectedCell.SetSelected(true);
        }

        Debug.Log($"<color=cyan>Hint:</color> {result.message}");
    }

    /// <summary>
    /// Show message when no hint is available
    /// </summary>
    private void ShowNoHintMessage(string message)
    {
        // Update pattern display label
        if (this.patternNameLabel != null)
        {
            this.patternNameLabel.text = "No hint available";
        }
        
        Debug.Log($"<color=yellow>No Hint Available:</color> {message}");
    }

    /// <summary>
    /// Clear hint highlighting
    /// </summary>
    public void ClearHint()
    {
        // Clear pattern display label
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

    #endregion
}
