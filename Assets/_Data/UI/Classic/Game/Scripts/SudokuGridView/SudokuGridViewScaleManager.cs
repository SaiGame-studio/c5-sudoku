using UnityEngine;
using UnityEngine.UIElements;

public class SudokuGridViewScaleManager
{
    private UIDocument uiDocument;
    private VisualElement mainContainer;
    private VisualElement root;
    private float addLandscapeScale;
    private float addPortraitScale;

    public SudokuGridViewScaleManager(UIDocument uiDocument, VisualElement mainContainer, VisualElement root, float addLandscapeScale, float addPortraitScale)
    {
        this.uiDocument = uiDocument;
        this.mainContainer = mainContainer;
        this.root = root;
        this.addLandscapeScale = addLandscapeScale;
        this.addPortraitScale = addPortraitScale;
    }

    public void ApplyResponsiveScale()
    {
        if (this.mainContainer == null)
        {
            Debug.LogWarning("MainContainer is null, cannot apply responsive scale");
            return;
        }

        float canvasWidth = 0f;
        float canvasHeight = 0f;
        string resolutionSource = "Unknown";

        var panelSettings = this.uiDocument.panelSettings;
        
        if (panelSettings != null)
        {
            Debug.Log($"<color=yellow>[Debug PanelSettings]</color> Found! RefRes=({panelSettings.referenceResolution.x}, {panelSettings.referenceResolution.y}), ScaleMode={panelSettings.scaleMode}, TargetTexture={panelSettings.targetTexture != null}");
            
            Vector2 refRes = panelSettings.referenceResolution;
            if (refRes.x > 0 && refRes.y > 0)
            {
                canvasWidth = refRes.x;
                canvasHeight = refRes.y;
                resolutionSource = $"Reference Resolution";
            }
            else if (panelSettings.targetTexture != null)
            {
                canvasWidth = panelSettings.targetTexture.width;
                canvasHeight = panelSettings.targetTexture.height;
                resolutionSource = "RenderTexture";
            }
            else
            {
                Debug.LogWarning($"<color=red>[PanelSettings Problem]</color> Reference Resolution is zero or negative: {refRes}");
            }
        }
        else
        {
            Debug.LogError("<color=red>[ApplyScale ERROR]</color> PanelSettings is NULL! Check UIDocument component.");
        }

        if (canvasWidth <= 0 || float.IsNaN(canvasWidth))
        {
            canvasWidth = Screen.width;
            canvasHeight = Screen.height;
            resolutionSource = "Screen Size (UNRELIABLE)";
            Debug.LogWarning($"<color=orange>[Fallback Warning]</color> Using Screen size because Reference Resolution failed!");
        }

        bool isLandscape = canvasWidth > canvasHeight;

        Debug.Log($"<color=cyan>[ApplyResponsiveScale FINAL]</color> Source: <b>{resolutionSource}</b>, Resolution: <b>{canvasWidth:F0}x{canvasHeight:F0}</b>, Orientation: <b>{(isLandscape ? "Landscape" : "Portrait")}</b>");

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

        Debug.Log($"Applying landscape scale: {scale:F2} (Canvas: {canvasWidth}x{canvasHeight})");

        this.mainContainer.style.transformOrigin = new StyleTransformOrigin(
            new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(50, LengthUnit.Percent))
        );

        this.mainContainer.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1)));
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

        Debug.Log($"Applying portrait scale: {scale:F2} (Canvas: {canvasWidth}x{canvasHeight})");

        // Use top-center origin so scaled content stays aligned to the top of the ScrollView
        this.mainContainer.style.transformOrigin = new StyleTransformOrigin(
            new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(0, LengthUnit.Percent))
        );

        this.mainContainer.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1)));
    }
}
