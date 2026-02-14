using UnityEngine;
using UnityEngine.UIElements;

// Reusable star counter that displays collected / total stars
public class StarCounter : VisualElement
{
    public new class UxmlFactory : UxmlFactory<StarCounter, UxmlTraits> { }
    public new class UxmlTraits : VisualElement.UxmlTraits { }

    private Label textLabel;

    public StarCounter()
    {
        this.name = "star-counter";
        this.AddToClassList("star-counter");

        // Load internal markup from UXML template
        var template = Resources.Load<VisualTreeAsset>("StarCounter");
        if (template != null) template.CloneTree(this);

        this.textLabel = this.Q<Label>("star-counter-text");

        this.RegisterCallback<AttachToPanelEvent>(this.OnAttachToPanel);
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        // Defer to ensure singletons have initialized via Awake()
        this.schedule.Execute(this.UpdateStarCount);
    }

    private void UpdateStarCount()
    {
        if (!Application.isPlaying) return;
        if (GameProgress.Instance == null) return;

        int collected = GameProgress.Instance.GetTotalStars();
        int total = GameProgress.Instance.GetMaxStars();

        this.textLabel.text = $"{collected} / {total}";
    }
}
