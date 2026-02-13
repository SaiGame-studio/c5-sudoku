using UnityEngine;
using UnityEngine.UIElements;

// Displays the app version in the bottom-right corner of the screen across all scenes
// Attach to same GameObject as UIDocument, finds Label with name "version-label"
public class VersionDisplay : SaiBehaviour
{
    private const string VERSION_LABEL_NAME = "version-label";

    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    private Label versionLabel;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadUIDocument();
    }

    protected override void Start()
    {
        base.Start();
        this.LoadVersionLabel();
        this.UpdateVersionText();
    }

    private void LoadUIDocument()
    {
        if (this.uiDocument != null) return;
        this.uiDocument = this.GetComponent<UIDocument>();
    }

    private void LoadVersionLabel()
    {
        if (this.uiDocument == null) return;
        var root = this.uiDocument.rootVisualElement;
        if (root == null) return;
        this.versionLabel = root.Q<Label>(VERSION_LABEL_NAME);
    }

    private void UpdateVersionText()
    {
        if (this.versionLabel == null) return;
        this.versionLabel.text = GameManager.Instance.GetVersion();
    }
}
