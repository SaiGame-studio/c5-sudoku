using UnityEngine;
using UnityEngine.UIElements;

public class ClassicHomeLevelList : SaiBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadUIDocument();
    }

    private void LoadUIDocument()
    {
        if (this.uiDocument != null) return;
        this.uiDocument = GetComponent<UIDocument>();
    }

    protected override void Start()
    {
        base.Start();
        this.InitializeUI();
    }

    private void InitializeUI()
    {
        if (this.uiDocument == null) return;

        this.root = this.uiDocument.rootVisualElement;

        this.RegisterCardCallbacks();
        this.RegisterBackButton();
    }

    private void RegisterCardCallbacks()
    {
        // Rows 1-3: cards named "card-{row}-{difficulty}" for difficulties 0-6
        for (int row = 0; row < GameData.LEVELS_PER_DIFFICULTY; row++)
        {
            for (int diff = 0; diff < 7; diff++)
            {
                this.RegisterCard("card-" + row + "-" + diff, row + 1, diff);
            }
        }

        // Row 4: single cards for Extreme (7) and Legendary (8)
        this.RegisterCard("card-3-7", 1, 7);
        this.RegisterCard("card-3-8", 1, 8);
    }

    private void RegisterCard(string cardName, int level, int difficulty)
    {
        VisualElement card = this.root.Q<VisualElement>(cardName);
        if (card == null) return;

        int capturedLevel = level;
        int capturedDifficulty = difficulty;

        card.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            this.OnLevelSelected(capturedLevel, capturedDifficulty);
        });
    }

    private void RegisterBackButton()
    {
        Button backButton = this.root.Q<Button>("back-button");
        if (backButton == null) return;

        backButton.clicked += this.OnBackButtonClicked;
    }

    private void OnLevelSelected(int level, int difficulty)
    {
        GameManager.Instance.LoadClassicGame(level, difficulty);
    }

    private void OnBackButtonClicked()
    {
        GameManager.Instance.LoadMainMenu();
    }
}
