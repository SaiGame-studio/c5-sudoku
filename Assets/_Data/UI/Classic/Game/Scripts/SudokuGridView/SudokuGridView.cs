using com.cyborgAssets.inspectorButtonPro;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
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
    [SerializeField] private SudokuAutoPlayer autoPlayer;

    [Header("Popup Settings")]
    [SerializeField] private Vector2 popupOffset = new Vector2(0f, -10f);

    [Header("Live Analysis")]
    [SerializeField] private bool autoAnalyze = true;
    [SerializeField] private bool autoAnalyzePatterns = true;
    
    [Header("Debug/Testing")]
    [SerializeField] private bool showDebugButtons = false;

    [Header("Scale Setting")]
    [SerializeField] private float addLandscapeScale = 0f;
    [SerializeField] private float addPortraitScale = 1.2f;

    [Header("Visual Elements (Runtime Only)")]
    [SerializeField, HideInInspector] private bool showVisualElements = false;
    [SerializeField] private VisualElement root;
    [SerializeField] private VisualElement gridContainer;
    [SerializeField] private VisualElement popupOverlay;
    [SerializeField] private VisualElement popupContainer;
    [SerializeField] private VisualElement difficultyStarsContainer;
    [SerializeField] private Label levelNameLabel;
    [SerializeField] private Button hintButton;
    [SerializeField] private Button autoNotesButton;
    [SerializeField] private Button clearNotesButton;
    [SerializeField] private VisualElement debugButtonsContainer;
    [SerializeField] private Button autoPlayButton;
    [SerializeField] private Label patternNameLabel;
    [SerializeField] private Label patternNameLabel2;
    [SerializeField] private SudokuCell[,] cells;
    [SerializeField] private SudokuCell selectedCell;
    [SerializeField] private int[,] cachedSolution;
    [SerializeField] private VisualElement mainContainer;
    [SerializeField] private VictoryEffect victoryEffect;
    private ScrollView scrollView;

    private SudokuGridViewScaleManager scaleManager;
    private SudokuGridViewPopupHandler popupHandler;
    private SudokuGridViewHighlightManager highlightManager;
    private SudokuGridViewAnalysisManager analysisManager;
    private SudokuGridViewHintManager hintManager;

    private void OnRootGeometryChanged(GeometryChangedEvent evt)
    {
        if (this.scaleManager != null)
        {
            this.scaleManager.ApplyResponsiveScale();
        }
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadUIDocument();
        this.LoadUIElements();
        this.LoadSudokuGenerator();
        this.LoadResultAnalyzer();
        this.LoadPatternAnalyzer();
        this.LoadHintSystem();
        this.LoadAutoNotes();
        this.LoadAutoPlayer();
    }

    private void LoadUIDocument()
    {
        if (this.uiDocument != null) return;
        this.uiDocument = GetComponent<UIDocument>();
        Debug.Log(transform.name + ": LoadUIDocument", gameObject);
    }

    [ProButton]
    private void LoadUIElements()
    {
        if (this.uiDocument == null)
        {
            Debug.LogWarning("UIDocument is null, cannot load UI elements");
            return;
        }

        this.root = this.uiDocument.rootVisualElement;
        if (this.root == null)
        {
            Debug.LogWarning("UIDocument rootVisualElement is null, UI may not be ready yet");
            return;
        }

        // Load all UI element references
        this.mainContainer = this.root.Q<VisualElement>("sudoku-main-container");
        this.gridContainer = this.root.Q<VisualElement>("sudoku-grid");
        this.popupOverlay = this.root.Q<VisualElement>("popup-overlay");
        this.popupContainer = this.root.Q<VisualElement>("popup-container");
        this.difficultyStarsContainer = this.root.Q<VisualElement>("difficulty-stars");
        this.levelNameLabel = this.root.Q<Label>("level-name-label");
        this.hintButton = this.root.Q<Button>("hint-button");
        this.autoNotesButton = this.root.Q<Button>("auto-notes-button");
        this.clearNotesButton = this.root.Q<Button>("clear-notes-button");
        this.debugButtonsContainer = this.root.Q<VisualElement>("debug-buttons");
        this.autoPlayButton = this.root.Q<Button>("auto-play-button");
        this.patternNameLabel = this.root.Q<Label>("pattern-name-label");
        this.patternNameLabel2 = this.root.Q<Label>("pattern-name-label-2");

        // Fix ScrollView content container styles (USS #unity-content-container may not apply reliably)
        this.scrollView = this.root.Q<ScrollView>("sudoku-scroll-view");
        if (this.scrollView != null)
        {
            this.scrollView.contentContainer.style.flexGrow = 0;
            this.scrollView.contentContainer.style.flexShrink = 0;
            this.scrollView.contentContainer.style.justifyContent = Justify.FlexStart;
            this.scrollView.contentContainer.style.alignItems = Align.Center;
        }

        Debug.Log(transform.name + ": LoadUIElements - MainContainer=" + (this.mainContainer != null ? "OK" : "NULL"), gameObject);
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
    
    private void LoadAutoPlayer()
    {
        if (this.autoPlayer != null) return;
        this.autoPlayer = FindFirstObjectByType<SudokuAutoPlayer>();
        Debug.Log(transform.name + ": LoadAutoPlayer", gameObject);
    }

    protected override void Start()
    {
        base.Start();
        this.InitializeGrid();
    }

    private void ResetScrollPosition()
    {
        if (this.scrollView == null) return;
        StartCoroutine(this.ResetScrollCoroutine());
    }

    private IEnumerator ResetScrollCoroutine()
    {
        // Wait 2 frames to ensure all layout passes and scale changes are finalized
        yield return null;
        yield return null;
        if (this.scrollView != null)
        {
            this.scrollView.scrollOffset = Vector2.zero;
        }
    }

    [ProButton]
    protected void ApplyResponsiveScale()
    {
        if (this.mainContainer == null)
        {
            this.LoadUIElements();
        }

        if (this.mainContainer == null)
        {
            Debug.LogWarning("MainContainer is still null after LoadUIElements, cannot apply responsive scale");
            return;
        }

        if (this.scaleManager == null)
        {
            this.scaleManager = new SudokuGridViewScaleManager(
                this.uiDocument, 
                this.mainContainer, 
                this.root, 
                this.addLandscapeScale, 
                this.addPortraitScale
            );
        }

        this.scaleManager.ApplyResponsiveScale();
    }

    [ProButton]
    public void InitializeGrid()
    {
        if (this.root == null || this.mainContainer == null)
        {
            this.LoadUIElements();
        }

        if (this.root == null)
        {
            Debug.LogError("UIDocument rootVisualElement is still null after LoadUIElements");
            return;
        }

        this.cells = new SudokuCell[GRID_SIZE, GRID_SIZE];

        this.root.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            this.root.UnregisterCallback<GeometryChangedEvent>(this.OnRootGeometryChanged);
            this.ApplyResponsiveScale();
            this.ResetScrollPosition();
        });

        if (this.hintButton != null)
        {
            this.hintButton.clicked += this.OnHintButtonClicked;
        }

        if (this.autoNotesButton != null)
        {
            this.autoNotesButton.clicked += this.OnAutoNotesButtonClicked;
        }

        if (this.clearNotesButton != null)
        {
            this.clearNotesButton.clicked += this.OnClearNotesButtonClicked;
        }
        
        if (this.autoPlayButton != null)
        {
            this.autoPlayButton.clicked += this.OnAutoPlayButtonClicked;
            this.UpdateAutoPlayButtonVisibility();
        }

        if (this.popupOverlay != null)
        {
            this.popupOverlay.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == this.popupOverlay)
                    this.HidePopup();
            });
        }

        this.root.RegisterCallback<KeyDownEvent>(this.OnKeyDown);
        this.root.focusable = true;
        this.root.Focus();

        this.BuildGrid();
        this.InitializeManagers();
        this.ClearAllNotes();
        this.LoadPuzzle();
        this.RefreshDifficultyStars();
        this.UpdateLevelNameDisplay();

        this.victoryEffect = new VictoryEffect(this.cells, this.root);
    }

    private void InitializeManagers()
    {
        this.scaleManager = new SudokuGridViewScaleManager(
            this.uiDocument, 
            this.mainContainer, 
            this.root, 
            this.addLandscapeScale, 
            this.addPortraitScale
        );

        this.popupHandler = new SudokuGridViewPopupHandler(
            this.root, 
            this.popupOverlay, 
            this.popupContainer, 
            this.popupOffset
        );

        this.highlightManager = new SudokuGridViewHighlightManager(this.cells);

        this.analysisManager = new SudokuGridViewAnalysisManager(
            this.resultAnalyzer, 
            this.patternAnalyzer, 
            this.cells
        );

        this.hintManager = new SudokuGridViewHintManager(
            this.hintSystem, 
            this.cells, 
            this.patternNameLabel,
            this.patternNameLabel2
        );
    }

    private void BuildGrid()
    {
        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                VisualElement existingElement = this.root.Q<VisualElement>("cell-" + row + "-" + col);
                if (existingElement == null) 
                {
                    Debug.LogWarning($"Cell element cell-{row}-{col} not found in UXML!");
                    continue;
                }

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

        this.sudokuGenerator.SetDifficulty(GameData.GetDifficultyLevel());

        this.sudokuGenerator.GeneratePuzzle();
        this.RefreshGridFromCurrentPuzzle();
    }
    
    /// <summary>
    /// Refresh grid display from current puzzle state without generating new puzzle
    /// </summary>
    public void RefreshGridFromCurrentPuzzle()
    {
        if (this.sudokuGenerator == null) return;
        
        this.StopVictoryEffect();
        this.selectedCell = null;
        this.HidePopup();
        this.ClearAllHighlights();
        this.ClearAllNotes();
        
        int[,] puzzle = this.sudokuGenerator.GetPuzzle();
        this.cachedSolution = this.sudokuGenerator.GetSolution();

        if (this.analysisManager != null)
        {
            this.analysisManager.SetCachedSolution(this.cachedSolution);
            this.analysisManager.ResetStats();
        }

        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                int value = puzzle[row, col];
                bool isClue = value != 0;
                this.cells[row, col].SetValue(value, isClue);
            }
        }

        this.RefreshDifficultyStars();
        
        if (this.autoAnalyze && this.analysisManager != null)
        {
            this.analysisManager.AnalyzeCurrentState(this.PlayVictoryEffect);
        }

        if (this.autoAnalyzePatterns && this.analysisManager != null)
        {
            this.analysisManager.AnalyzePatterns();
        }
    }

    private void OnCellClicked(int row, int col)
    {
        if (this.highlightManager != null)
        {
            this.highlightManager.ClearAllHighlights();
        }
        
        this.ClearHint();

        SudokuCell cell = this.cells[row, col];
        this.selectedCell = cell;
        cell.SetSelected(true);

        if (this.highlightManager != null)
        {
            this.highlightManager.HighlightRelatedCells(row, col);
            this.highlightManager.HighlightSameNumber(cell.Value);
        }

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
        if (this.popupHandler != null)
        {
            this.popupHandler.ShowPopup(cell, this.OnFillNumber, this.OnToggleNote, this.OnErase);
        }
    }

    private void HidePopup()
    {
        if (this.popupHandler != null)
        {
            this.popupHandler.HidePopup();
        }
    }

    private void OnFillNumber(int number)
    {
        if (this.selectedCell == null) return;

        this.selectedCell.SetPlayerValue(number);

        bool isCorrect = this.cachedSolution != null &&
                         this.cachedSolution[this.selectedCell.Row, this.selectedCell.Col] == number;
        this.selectedCell.SetError(!isCorrect);

        this.HidePopup();
        this.RefreshHighlights();

        if (this.autoAnalyze && this.analysisManager != null)
        {
            this.analysisManager.AnalyzeCurrentState(this.PlayVictoryEffect);
        }
    }

    private void OnToggleNote(int number, VisualElement button)
    {
        if (this.selectedCell == null) return;

        this.selectedCell.ToggleNote(number);
        this.selectedCell.SetError(false);

        if (this.selectedCell.HasNote(number))
            button.AddToClassList("popup-note-button--active");
        else
            button.RemoveFromClassList("popup-note-button--active");

        if (this.autoAnalyzePatterns && this.analysisManager != null)
        {
            this.analysisManager.AnalyzePatterns();
        }
    }

    private void OnErase()
    {
        if (this.selectedCell == null) return;

        this.selectedCell.SetPlayerValue(0);
        this.selectedCell.SetError(false);
        this.HidePopup();
        this.RefreshHighlights();

        if (this.autoAnalyze && this.analysisManager != null)
        {
            this.analysisManager.AnalyzeCurrentState(this.PlayVictoryEffect);
        }
    }
    #endregion

    #region Highlights
    private void RefreshHighlights()
    {
        if (this.highlightManager != null)
        {
            this.highlightManager.RefreshHighlights(this.selectedCell);
        }
    }

    private void ClearAllHighlights()
    {
        if (this.highlightManager != null)
        {
            this.highlightManager.ClearAllHighlights();
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Reload puzzle with a new generated board
    /// </summary>
    public void NewGame()
    {
        this.StopVictoryEffect();
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

    private const int TOTAL_STARS = 9;

    private void RefreshDifficultyStars()
    {
        if (this.sudokuGenerator == null || this.difficultyStarsContainer == null) return;

        int activeCount = (int)this.sudokuGenerator.GetDifficulty() + 1;

        for (int i = 1; i <= TOTAL_STARS; i++)
        {
            Label star = this.difficultyStarsContainer.Q<Label>("star-" + i);
            if (star == null) continue;

            // Reset opacity to ensure stars are visible after flying animation
            star.style.opacity = 1f;

            if (i <= activeCount)
                star.AddToClassList("difficulty-star--active");
            else
                star.RemoveFromClassList("difficulty-star--active");
        }
    }

    /// <summary>
    /// Update level name display from GameData
    /// </summary>
    private void UpdateLevelNameDisplay()
    {
        if (this.levelNameLabel == null) return;

        string levelName = GameData.SelectedLevelName;
        
        // Format the display: "Level 1" instead of "level-1"
        if (levelName.StartsWith("level-"))
        {
            string levelNumber = levelName.Replace("level-", "");
            this.levelNameLabel.text = "Level " + levelNumber;
        }
        else
        {
            this.levelNameLabel.text = levelName;
        }
    }

    /// <summary>
    /// Play the victory celebration effect (can be triggered at any time)
    /// </summary>
    [ProButton]
    public void PlayVictoryEffect()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("PlayVictoryEffect() can only be called during Play Mode");
            return;
        }

        if (this.victoryEffect == null || this.victoryEffect.IsPlaying) return;

        this.ClearAllHighlights();
        this.HidePopup();
        this.selectedCell = null;

        StartCoroutine(this.victoryEffect.PlayAnimation());
    }

    /// <summary>
    /// Stop the victory effect if currently playing
    /// </summary>
    public void StopVictoryEffect()
    {
        if (this.victoryEffect != null)
        {
            this.victoryEffect.StopEffect();
        }
    }
    #endregion

    #region Result Analysis
    /// <summary>
    /// Get current game result
    /// </summary>
    public SudokuResultAnalyzer.GameResult GetCurrentResult()
    {
        return this.analysisManager != null ? this.analysisManager.CurrentResult : SudokuResultAnalyzer.GameResult.NotCompleted;
    }

    /// <summary>
    /// Get current completion percentage
    /// </summary>
    public float GetCompletionPercentage()
    {
        return this.analysisManager != null ? this.analysisManager.CompletionPercentage : 0f;
    }

    /// <summary>
    /// Get all cell notes (public for external access)
    /// </summary>
    public List<int>[,] GetCellNotes()
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
    /// Get cell data for external access (e.g., AutoPlayer)
    /// </summary>
    public CellData GetCellData(int row, int col)
    {
        if (row < 0 || row >= GRID_SIZE || col < 0 || col >= GRID_SIZE)
        {
            return new CellData { value = 0, isClue = false };
        }

        SudokuCell cell = this.cells[row, col];
        if (cell == null)
        {
            Debug.LogWarning($"Cell at [{row},{col}] is null!");
            return new CellData { value = 0, isClue = false };
        }

        CellData data = new CellData
        {
            value = cell.Value,
            isClue = cell.IsClue
        };

        if (row == 0 && col == 0)
        {
            Debug.Log($"GetCellData[{row},{col}]: value={data.value}, isClue={data.isClue}");
        }

        return data;
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

        this.ClearAllHighlights();

        this.selectedCell = cell;
        cell.SetSelected(true);
        
        if (this.highlightManager != null)
        {
            this.highlightManager.HighlightRelatedCells(row, col);
        }

        cell.SetPlayerValue(number);

        bool isCorrect = this.cachedSolution != null &&
                         this.cachedSolution[row, col] == number;
        cell.SetError(!isCorrect);

        if (this.highlightManager != null)
        {
            this.highlightManager.HighlightSameNumber(number);
        }

        if (this.autoAnalyze && this.analysisManager != null)
        {
            this.analysisManager.AnalyzeCurrentState(this.PlayVictoryEffect);
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
    #endregion

    #region Hint System

    /// <summary>
    /// Handle hint button click
    /// </summary>
    private void OnHintButtonClicked()
    {
        if (this.hintManager == null)
        {
            Debug.LogWarning("Hint Manager is not available");
            return;
        }

        int[,] currentPuzzle = this.GetCurrentUserPuzzle();
        List<int>[,] cellNotes = this.GetCellNotes();

        this.hintManager.RequestHint(currentPuzzle, cellNotes);
    }

    private void OnAutoNotesButtonClicked()
    {
        if (this.autoNotes == null)
        {
            Debug.LogWarning("Auto Notes System is not available");
            return;
        }

        this.ClearAllNotes();
        
        this.autoNotes.StartAutoNotes();
    }

    private void OnClearNotesButtonClicked()
    {
        this.ClearAllNotes();
    }
    
    /// <summary>
    /// Handle auto play button click - uses SudokuAutoPlayer to solve puzzle
    /// </summary>
    private void OnAutoPlayButtonClicked()
    {
        if (this.autoPlayer == null)
        {
            Debug.LogWarning("[SudokuGridView] AutoPlayer is not available");
            return;
        }
        
        Debug.Log("[SudokuGridView] Starting auto-play via SudokuAutoPlayer...");
        
        // Use the auto player to solve the puzzle
        this.autoPlayer.StartAutoPlayOnGridView(this);
    }
    
    /// <summary>
    /// Update debug buttons visibility based on inspector setting
    /// </summary>
    private void UpdateAutoPlayButtonVisibility()
    {
        // If debug-buttons container exists (Landscape), hide/show the entire container
        if (this.debugButtonsContainer != null)
        {
            this.debugButtonsContainer.style.display = this.showDebugButtons ? DisplayStyle.Flex : DisplayStyle.None;
        }
        // Otherwise, control individual auto-play button (Portrait)
        else if (this.autoPlayButton != null)
        {
            this.autoPlayButton.style.display = this.showDebugButtons ? DisplayStyle.Flex : DisplayStyle.None;
        }
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
    /// Clear hint highlighting
    /// </summary>
    public void ClearHint()
    {
        if (this.hintManager != null)
        {
            this.hintManager.ClearHint();
        }
    }

    #endregion
}
