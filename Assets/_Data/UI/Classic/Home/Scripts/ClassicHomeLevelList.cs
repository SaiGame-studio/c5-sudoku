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

    [Header("Scale Setting")]
    [SerializeField] private float addLandscapeScale = 1f;
    [SerializeField] private float addPortraitScale = 1f;

    [Header("Visual Elements")]
    [SerializeField] private VisualElement root;
    [SerializeField] private VisualElement homeContainer;

    private void OnRootGeometryChanged(GeometryChangedEvent evt)
    {
        this.ApplyResponsiveScale();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadUIDocument();
        this.LoadUIElements();
    }

    private void LoadUIDocument()
    {
        if (this.uiDocument != null) return;
        this.uiDocument = GetComponent<UIDocument>();
    }

    [ProButton]
    private void LoadUIElements()
    {
        if (this.uiDocument == null) return;

        this.root = this.uiDocument.rootVisualElement;
        if (this.root == null) return;

        this.homeContainer = this.root.Q<VisualElement>(className: "home-container");
    }

    protected override void Start()
    {
        base.Start();
        this.InitializeUI();
    }

    private void InitializeUI()
    {
        if (this.root == null || this.homeContainer == null)
        {
            this.LoadUIElements();
        }

        if (this.root == null) return;

        this.root.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            this.root.UnregisterCallback<GeometryChangedEvent>(this.OnRootGeometryChanged);
            this.ApplyResponsiveScale();
        });

        this.RegisterCardCallbacks();
        this.UpdateLockedLevels();
        this.UpdateCompletedLevels();
    }

    [ProButton]
    private void ApplyResponsiveScale()
    {
        if (this.homeContainer == null)
        {
            this.LoadUIElements();
        }

        if (this.homeContainer == null) return;

        float canvasWidth = 0f;
        float canvasHeight = 0f;

        var panelSettings = this.uiDocument.panelSettings;

        if (panelSettings != null)
        {
            Vector2 refRes = panelSettings.referenceResolution;
            if (refRes.x > 0 && refRes.y > 0)
            {
                canvasWidth = refRes.x;
                canvasHeight = refRes.y;
            }
            else if (panelSettings.targetTexture != null)
            {
                canvasWidth = panelSettings.targetTexture.width;
                canvasHeight = panelSettings.targetTexture.height;
            }
        }

        if (canvasWidth <= 0 || float.IsNaN(canvasWidth))
        {
            canvasWidth = Screen.width;
            canvasHeight = Screen.height;
        }

        bool isLandscape = canvasWidth > canvasHeight;

        if (isLandscape)
        {
            this.ApplyResponsiveScaleLandscape(canvasWidth, canvasHeight);
        }
        else
        {
            this.ApplyResponsiveScalePortrait(canvasWidth, canvasHeight);
        }
    }

    private void ApplyResponsiveScaleLandscape(float canvasWidth, float canvasHeight)
    {
        float baseWidth = 1920f;
        float baseHeight = 1080f;

        float scaleX = canvasWidth / baseWidth;
        float scaleY = canvasHeight / baseHeight;
        float scale = Mathf.Min(scaleX, scaleY);

        scale = Mathf.Max(scale, 0.5f);
        scale += this.addLandscapeScale;

        this.homeContainer.style.transformOrigin = new StyleTransformOrigin(
            new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(0, LengthUnit.Percent))
        );
        this.homeContainer.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1)));
    }

    private void ApplyResponsiveScalePortrait(float canvasWidth, float canvasHeight)
    {
        float baseWidth = 1080f;
        float baseHeight = 1920f;

        float scaleX = canvasWidth / baseWidth;
        float scaleY = canvasHeight / baseHeight;
        float scale = Mathf.Min(scaleX, scaleY);

        scale = Mathf.Max(scale, 0.4f);
        scale += this.addPortraitScale;

        this.homeContainer.style.transformOrigin = new StyleTransformOrigin(
            new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(0, LengthUnit.Percent))
        );
        this.homeContainer.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1)));
    }

    private void RegisterCardCallbacks()
    {
        // Register level-1 to level-21 (difficulties 0-6, 3 levels each)
        for (int diff = 0; diff < 7; diff++)
        {
            for (int subLevel = 1; subLevel <= GameData.LEVELS_PER_DIFFICULTY; subLevel++)
            {
                int levelNumber = diff * 3 + subLevel;
                string levelName = "level-" + levelNumber;
                this.RegisterCard(levelName, subLevel, diff, levelName);
            }
        }

        // Register level-22 (Extreme - difficulty 7)
        this.RegisterCard("level-22", 1, 7, "level-22");
        
        // Register level-23 (Legendary - difficulty 8)
        this.RegisterCard("level-23", 1, 8, "level-23");
    }

    private void RegisterCard(string cardName, int level, int difficulty, string levelName)
    {
        VisualElement card = this.root.Q<VisualElement>(cardName);
        if (card == null) return;

        int capturedLevel = level;
        int capturedDifficulty = difficulty;
        string capturedLevelName = levelName;

        card.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            
            // Extract global level number from levelName
            int levelNumber = GameProgress.ParseLevelName(capturedLevelName);
            
            // Check if level is unlocked before allowing selection
            if (GameProgress.Instance != null && !GameProgress.Instance.IsLevelUnlocked(levelNumber))
            {
                Debug.Log($"[ClassicHomeLevelList] Level {levelNumber} is locked! Complete previous levels first.");
                return;
            }
            
            this.OnLevelSelected(capturedLevel, capturedDifficulty, capturedLevelName);
        });
    }

    private void OnLevelSelected(int level, int difficulty, string levelName)
    {
        GameManager.Instance.LoadClassicGame(level, difficulty, levelName);
    }

    /// <summary>
    /// Update UI to show which levels are locked/unlocked
    /// </summary>
    private void UpdateLockedLevels()
    {
        if (GameProgress.Instance == null) return;

        // Check all levels 1-23
        for (int levelNumber = 1; levelNumber <= 23; levelNumber++)
        {
            string levelName = $"level-{levelNumber}";
            
            if (!GameProgress.Instance.IsLevelUnlocked(levelNumber))
            {
                this.MarkLevelAsLocked(levelName);
            }
        }
    }
    
    /// <summary>
    /// Update UI to show which levels have been completed
    /// </summary>
    private void UpdateCompletedLevels()
    {
        if (GameProgress.Instance == null) return;

        // Check all levels 1-23 with simplified loop
        for (int levelNumber = 1; levelNumber <= 23; levelNumber++)
        {
            if (GameProgress.Instance.IsLevelCompleted(levelNumber))
            {
                string levelName = $"level-{levelNumber}";
                this.MarkLevelAsCompleted(levelName);
            }
        }
    }

    /// <summary>
    /// Add CSS class to mark a level card as locked
    /// </summary>
    private void MarkLevelAsLocked(string levelName)
    {
        VisualElement card = this.root.Q<VisualElement>(levelName);
        if (card != null && !card.ClassListContains("level-locked"))
        {
            card.AddToClassList("level-locked");
        }
    }
    
    /// <summary>
    /// Add CSS class to mark a level card as completed
    /// </summary>
    private void MarkLevelAsCompleted(string levelName)
    {
        VisualElement card = this.root.Q<VisualElement>(levelName);
        if (card != null && !card.ClassListContains("level-completed"))
        {
            card.AddToClassList("level-completed");
        }
    }
}
