using com.cyborgAssets.inspectorButtonPro;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuController : SaiBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Scale Setting")]
    [SerializeField] private float addLandscapeScale = 1f;
    [SerializeField] private float addPortraitScale = 2f;

    [Header("Visual Elements")]
    [SerializeField] private VisualElement root;
    [SerializeField] private VisualElement menuContainer;

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

        this.menuContainer = this.root.Q<VisualElement>(className: "menu-container");
    }

    protected override void Start()
    {
        base.Start();
        this.InitializeUI();
    }

    private void InitializeUI()
    {
        if (this.root == null || this.menuContainer == null)
        {
            this.LoadUIElements();
        }

        if (this.root == null) return;

        this.root.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            this.root.UnregisterCallback<GeometryChangedEvent>(this.OnRootGeometryChanged);
            this.ApplyResponsiveScale();
        });

        this.RegisterButtonCallbacks();
    }

    private void OnRootGeometryChanged(GeometryChangedEvent evt)
    {
        this.ApplyResponsiveScale();
    }

    [ProButton]
    private void ApplyResponsiveScale()
    {
        if (this.menuContainer == null)
        {
            this.LoadUIElements();
        }

        if (this.menuContainer == null) return;

        float canvasWidth = 0f;
        float canvasHeight = 0f;
        string resolutionSource = "Unknown";

        var panelSettings = this.uiDocument.panelSettings;

        if (panelSettings != null)
        {
            // Always use Reference Resolution if it exists
            Vector2 refRes = panelSettings.referenceResolution;
            if (refRes.x > 0 && refRes.y > 0)
            {
                canvasWidth = refRes.x;
                canvasHeight = refRes.y;
                resolutionSource = "Reference Resolution";
            }
            // Fallback to RenderTexture
            else if (panelSettings.targetTexture != null)
            {
                canvasWidth = panelSettings.targetTexture.width;
                canvasHeight = panelSettings.targetTexture.height;
                resolutionSource = "RenderTexture";
            }
        }

        // Fallback: Use Screen size (unreliable in Edit Mode)
        if (canvasWidth <= 0 || float.IsNaN(canvasWidth))
        {
            canvasWidth = Screen.width;
            canvasHeight = Screen.height;
            resolutionSource = "Screen Size (UNRELIABLE)";
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
        // Base resolution: 1920x1080 (landscape)
        float baseWidth = 1920f;
        float baseHeight = 1080f;

        // Calculate scale factor based on canvas size
        float scaleX = canvasWidth / baseWidth;
        float scaleY = canvasHeight / baseHeight;
        float scale = Mathf.Min(scaleX, scaleY);

        // Apply minimum scale to avoid too small UI
        scale = Mathf.Max(scale, 0.5f);
        scale += this.addLandscapeScale;

        // Set transform-origin to top-center for proper scaling
        this.menuContainer.style.transformOrigin = new StyleTransformOrigin(
            new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(0, LengthUnit.Percent))
        );

        // Apply scale to menu container
        this.menuContainer.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1)));
    }

    private void ApplyResponsiveScalePortrait(float canvasWidth, float canvasHeight)
    {
        // Base resolution: 1080x1920 (portrait)
        float baseWidth = 1080f;
        float baseHeight = 1920f;

        // Calculate scale factor based on canvas size
        float scaleX = canvasWidth / baseWidth;
        float scaleY = canvasHeight / baseHeight;
        float scale = Mathf.Min(scaleX, scaleY);

        // Apply minimum scale to avoid too small UI
        scale = Mathf.Max(scale, 0.4f);
        scale += this.addPortraitScale;

        // Set transform-origin to top-center for proper scaling
        this.menuContainer.style.transformOrigin = new StyleTransformOrigin(
            new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(0, LengthUnit.Percent))
        );

        // Apply scale to menu container
        this.menuContainer.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1)));
    }

    private void RegisterButtonCallbacks()
    {
        Button classicButton = this.root.Q<Button>("classic-button");
        if (classicButton != null)
        {
            classicButton.clicked += this.OnClassicButtonClicked;
        }

        Button storyButton = this.root.Q<Button>("story-button");
        if (storyButton != null)
        {
            storyButton.clicked += this.OnStoryButtonClicked;
        }

        Button quitButton = this.root.Q<Button>("quit-button");
        if (quitButton != null)
        {
            quitButton.clicked += this.OnQuitButtonClicked;
        }
    }

    private void OnClassicButtonClicked()
    {
        GameManager.Instance.LoadClassicHome();
    }

    private void OnStoryButtonClicked()
    {
        GameManager.Instance.LoadStoryScene();
    }

    private void OnQuitButtonClicked()
    {
        GameManager.Instance.QuitGame();
    }
}
