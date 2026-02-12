using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

public class SudokuDifficultyPanel : SaiBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Dependencies")]
    [SerializeField] private SudokuGenerator sudokuGenerator;
    [SerializeField] private SudokuGridView gridView;

    private VisualElement root;
    private VisualElement difficultyDropdownContainer;
    private Label[] difficultyStars;
    private Button generateButton;

    private Dictionary<string, SudokuGenerator.DifficultyLevel> difficultyMap;
    private Dictionary<int, SudokuGenerator.DifficultyLevel> starToDifficultyMap;
    private int currentStarLevel = 3;
    private const int TOTAL_DIFFICULTY_STARS = 9;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadUIDocument();
        this.LoadSudokuGenerator();
        this.LoadGridView();
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

    private void LoadGridView()
    {
        if (this.gridView != null) return;
        this.gridView = FindFirstObjectByType<SudokuGridView>();
        Debug.Log(transform.name + ": LoadGridView", gameObject);
    }

    protected override void Start()
    {
        base.Start();
        this.InitializeDifficultyMap();
        this.InitializeUI();
    }

    private void InitializeDifficultyMap()
    {
        this.difficultyMap = new Dictionary<string, SudokuGenerator.DifficultyLevel>
        {
            { "Very Easy", SudokuGenerator.DifficultyLevel.VeryEasy },
            { "Easy", SudokuGenerator.DifficultyLevel.Easy },
            { "Medium", SudokuGenerator.DifficultyLevel.Medium },
            { "Hard", SudokuGenerator.DifficultyLevel.Hard },
            { "Very Hard", SudokuGenerator.DifficultyLevel.VeryHard },
            { "Expert", SudokuGenerator.DifficultyLevel.Expert },
            { "Master", SudokuGenerator.DifficultyLevel.Master },
            { "Extreme", SudokuGenerator.DifficultyLevel.Extreme },
            { "Legendary", SudokuGenerator.DifficultyLevel.Legendary }
        };

        this.starToDifficultyMap = new Dictionary<int, SudokuGenerator.DifficultyLevel>
        {
            { 1, SudokuGenerator.DifficultyLevel.VeryEasy },
            { 2, SudokuGenerator.DifficultyLevel.Easy },
            { 3, SudokuGenerator.DifficultyLevel.Medium },
            { 4, SudokuGenerator.DifficultyLevel.Hard },
            { 5, SudokuGenerator.DifficultyLevel.VeryHard },
            { 6, SudokuGenerator.DifficultyLevel.Expert },
            { 7, SudokuGenerator.DifficultyLevel.Master },
            { 8, SudokuGenerator.DifficultyLevel.Extreme },
            { 9, SudokuGenerator.DifficultyLevel.Legendary }
        };
    }

    private void InitializeUI()
    {
        if (this.uiDocument == null) return;

        this.root = this.uiDocument.rootVisualElement;
        this.difficultyDropdownContainer = this.root.Q<VisualElement>("difficulty-dropdown");
        this.generateButton = this.root.Q<Button>("generate-button");

        if (this.difficultyDropdownContainer != null)
        {
            this.SetupDifficultyStars();
        }

        if (this.generateButton != null)
        {
            this.generateButton.clicked += this.OnGenerateButtonClicked;
        }
    }

    private void SetupDifficultyStars()
    {
        this.difficultyStars = new Label[TOTAL_DIFFICULTY_STARS];
        
        for (int i = 0; i < TOTAL_DIFFICULTY_STARS; i++)
        {
            int starIndex = i + 1;
            this.difficultyStars[i] = this.root.Q<Label>($"diff-star-{starIndex}");
            
            if (this.difficultyStars[i] != null)
            {
                int capturedIndex = starIndex;
                this.difficultyStars[i].RegisterCallback<ClickEvent>(evt => this.OnStarClicked(capturedIndex));
            }
        }

        this.SetCurrentDifficultyFromGenerator();
        this.UpdateStarVisuals();
    }

    private void SetCurrentDifficultyFromGenerator()
    {
        if (this.sudokuGenerator == null)
        {
            this.currentStarLevel = 3;
            return;
        }

        SudokuGenerator.DifficultyLevel currentLevel = this.sudokuGenerator.GetDifficulty();
        
        foreach (var kvp in this.starToDifficultyMap)
        {
            if (kvp.Value == currentLevel)
            {
                this.currentStarLevel = kvp.Key;
                return;
            }
        }

        this.currentStarLevel = 3;
    }

    private void OnStarClicked(int starLevel)
    {
        this.currentStarLevel = starLevel;
        this.UpdateStarVisuals();
        this.OnDifficultyChanged(starLevel);
    }

    private void UpdateStarVisuals()
    {
        for (int i = 0; i < this.difficultyStars.Length; i++)
        {
            if (this.difficultyStars[i] == null) continue;

            if (i < this.currentStarLevel)
            {
                this.difficultyStars[i].AddToClassList("difficulty-dropdown-star--active");
            }
            else
            {
                this.difficultyStars[i].RemoveFromClassList("difficulty-dropdown-star--active");
            }
        }
    }

    private void OnDifficultyChanged(int starLevel)
    {
        if (this.sudokuGenerator == null) return;

        if (this.starToDifficultyMap.TryGetValue(starLevel, out SudokuGenerator.DifficultyLevel level))
        {
            this.sudokuGenerator.SetDifficulty(level);
            Debug.Log($"Difficulty changed to: {level} ({starLevel} stars)");
        }
    }

    private void OnGenerateButtonClicked()
    {
        if (this.sudokuGenerator == null) return;

        if (this.starToDifficultyMap.TryGetValue(this.currentStarLevel, out SudokuGenerator.DifficultyLevel level))
        {
            this.sudokuGenerator.GeneratePuzzle(level);
            Debug.Log($"Generated new puzzle with difficulty: {level} ({this.currentStarLevel} stars)");

            if (this.gridView != null)
            {
                this.gridView.ClearHint();
                this.gridView.RefreshGridFromCurrentPuzzle();
            }
        }
    }

    private void OnDestroy()
    {
        if (this.generateButton != null)
        {
            this.generateButton.clicked -= this.OnGenerateButtonClicked;
        }
    }
}
