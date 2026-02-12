using UnityEngine;
using UnityEngine.UIElements;
using com.cyborgAssets.inspectorButtonPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ClassicHomeLevelList : SaiBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float editorScaleFactor = 1.0f;

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

        // Apply scale factor for consistency between editor and runtime
        if (this.editorScaleFactor != 1.0f)
        {
            this.root.transform.scale = new Vector3(this.editorScaleFactor, this.editorScaleFactor, 1f);
        }

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
        if (backButton == null)
        {
            Debug.LogWarning("Back button not found in " + gameObject.name);
            return;
        }

        backButton.clicked += this.OnBackButtonClicked;
        Debug.Log("Back button registered in " + gameObject.name);
    }

    private void OnLevelSelected(int level, int difficulty)
    {
        GameManager.Instance.LoadClassicGame(level, difficulty);
    }

    private void OnBackButtonClicked()
    {
        Debug.Log("Back button clicked in " + gameObject.name);
        GameManager.Instance.LoadMainMenu();
    }

#if UNITY_EDITOR
    [ProButton]
    private void ScaleUI()
    {
        this.ApplyScale(this.editorScaleFactor);
    }

    [ProButton]
    private void ResetScale()
    {
        this.editorScaleFactor = 1.0f;
        this.ApplyScale(1.0f);
    }

    private void ApplyScale(float scale)
    {
        this.LoadUIDocument();
        if (this.uiDocument == null) return;

        this.root = this.uiDocument.rootVisualElement;
        if (this.root == null) return;

        this.root.transform.scale = new Vector3(scale, scale, 1f);
        EditorUtility.SetDirty(this);
    }
#endif
}
