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
        // Cards are named "card-{row}-{difficulty}" in the UXML
        for (int row = 0; row < GameData.LEVELS_PER_DIFFICULTY; row++)
        {
            for (int diff = 0; diff < GameData.DIFFICULTY_COUNT; diff++)
            {
                string cardName = "card-" + row + "-" + diff;
                VisualElement card = this.root.Q<VisualElement>(cardName);
                if (card == null) continue;

                int capturedLevel = row + 1;
                int capturedDifficulty = diff;

                card.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    this.OnLevelSelected(capturedLevel, capturedDifficulty);
                });
            }
        }
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
