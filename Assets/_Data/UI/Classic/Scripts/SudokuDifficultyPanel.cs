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
    private DropdownField difficultyDropdown;
    private Button generateButton;

    private Dictionary<string, SudokuGenerator.DifficultyLevel> difficultyMap;

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
    }

    private void InitializeUI()
    {
        if (this.uiDocument == null) return;

        this.root = this.uiDocument.rootVisualElement;
        this.difficultyDropdown = this.root.Q<DropdownField>("difficulty-dropdown");
        this.generateButton = this.root.Q<Button>("generate-button");

        if (this.difficultyDropdown != null)
        {
            this.SetupDifficultyDropdown();
        }

        if (this.generateButton != null)
        {
            this.generateButton.clicked += this.OnGenerateButtonClicked;
        }
    }

    private void SetupDifficultyDropdown()
    {
        List<string> difficultyChoices = new List<string>(this.difficultyMap.Keys);
        this.difficultyDropdown.choices = difficultyChoices;
        
        string currentDifficulty = this.GetCurrentDifficultyString();
        this.difficultyDropdown.value = currentDifficulty;

        this.difficultyDropdown.RegisterValueChangedCallback(evt =>
        {
            this.OnDifficultyChanged(evt.newValue);
        });
    }

    private string GetCurrentDifficultyString()
    {
        if (this.sudokuGenerator == null)
            return "Medium";

        SudokuGenerator.DifficultyLevel currentLevel = this.sudokuGenerator.GetDifficulty();
        
        foreach (var kvp in this.difficultyMap)
        {
            if (kvp.Value == currentLevel)
                return kvp.Key;
        }

        return "Medium";
    }

    private void OnDifficultyChanged(string newDifficulty)
    {
        if (this.sudokuGenerator == null) return;

        if (this.difficultyMap.TryGetValue(newDifficulty, out SudokuGenerator.DifficultyLevel level))
        {
            this.sudokuGenerator.SetDifficulty(level);
            Debug.Log($"Difficulty changed to: {newDifficulty}");
        }
    }

    private void OnGenerateButtonClicked()
    {
        if (this.sudokuGenerator == null) return;

        string selectedDifficulty = this.difficultyDropdown.value;
        
        if (this.difficultyMap.TryGetValue(selectedDifficulty, out SudokuGenerator.DifficultyLevel level))
        {
            this.sudokuGenerator.GeneratePuzzle(level);
            Debug.Log($"Generated new puzzle with difficulty: {selectedDifficulty}");

            if (this.gridView != null)
            {
                this.gridView.InitializeGrid();
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
