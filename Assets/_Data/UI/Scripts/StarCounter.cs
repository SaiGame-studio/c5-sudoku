using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Reusable star counter component that displays collected / total stars
/// </summary>
public class StarCounter : VisualElement
{
    public new class UxmlFactory : UxmlFactory<StarCounter, UxmlTraits> { }
    public new class UxmlTraits : VisualElement.UxmlTraits { }

    private Label iconLabel;
    private Label textLabel;

    public StarCounter()
    {
        this.name = "star-counter";
        this.AddToClassList("star-counter");

        // Apply container inline styles directly
        this.style.flexDirection = FlexDirection.Row;
        this.style.alignItems = Align.Center;
        this.style.justifyContent = Justify.Center;
        this.style.paddingLeft = 14;
        this.style.paddingRight = 14;
        this.style.paddingTop = 6;
        this.style.paddingBottom = 6;
        this.style.borderTopLeftRadius = 16;
        this.style.borderTopRightRadius = 16;
        this.style.borderBottomLeftRadius = 16;
        this.style.borderBottomRightRadius = 16;
        this.style.backgroundColor = new Color(0.22f, 0.23f, 0.27f, 0.85f);
        this.style.borderLeftWidth = 1;
        this.style.borderRightWidth = 1;
        this.style.borderTopWidth = 1;
        this.style.borderBottomWidth = 1;
        this.style.borderLeftColor = new Color(0.39f, 0.39f, 0.47f);
        this.style.borderRightColor = new Color(0.39f, 0.39f, 0.47f);
        this.style.borderTopColor = new Color(0.39f, 0.39f, 0.47f);
        this.style.borderBottomColor = new Color(0.39f, 0.39f, 0.47f);

        // Load UXML template with inline styles for children
        var template = Resources.Load<VisualTreeAsset>("StarCounter");
        if (template != null)
        {
            template.CloneTree(this);
        }

        // Query the labels after template is loaded
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
