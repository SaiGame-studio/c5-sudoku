using UnityEngine;
using UnityEngine.UIElements;

// Reusable version label component that displays app version
public class VersionLabel : VisualElement
{
    public new class UxmlFactory : UxmlFactory<VersionLabel, UxmlTraits> { }
    public new class UxmlTraits : VisualElement.UxmlTraits { }

    private Label textLabel;

    public VersionLabel()
    {
        this.name = "version-label";
        this.AddToClassList("version-label");

        // Load internal markup from UXML template
        var template = Resources.Load<VisualTreeAsset>("VersionLabel");
        if (template != null) template.CloneTree(this);

        this.textLabel = this.Q<Label>("version-label-text");

        this.RegisterCallback<AttachToPanelEvent>(this.OnAttachToPanel);
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        // Defer to ensure singletons have initialized via Awake()
        this.schedule.Execute(this.UpdateVersion);
    }

    private void UpdateVersion()
    {
        if (!Application.isPlaying) return;
        if (GameManager.Instance == null) return;

        this.textLabel.text = GameManager.Instance.GetVersion();
    }
}
