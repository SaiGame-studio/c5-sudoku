using UnityEngine;
using UnityEngine.UIElements;

// Reusable pattern display that shows the current hint pattern name
public class PatternDisplay : VisualElement
{
    public new class UxmlFactory : UxmlFactory<PatternDisplay, UxmlTraits> { }
    public new class UxmlTraits : VisualElement.UxmlTraits { }

    private Label patternNameLabel;

    public string PatternName
    {
        get => this.patternNameLabel != null ? this.patternNameLabel.text : "";
        set
        {
            if (this.patternNameLabel != null)
            {
                this.patternNameLabel.text = value;
            }
        }
    }

    public Label PatternNameLabel => this.patternNameLabel;

    public PatternDisplay()
    {
        this.name = "pattern-display-2";
        this.AddToClassList("pattern-display");

        // Load internal markup from UXML template
        var template = Resources.Load<VisualTreeAsset>("PatternDisplay");
        if (template != null) template.CloneTree(this);

        this.patternNameLabel = this.Q<Label>("pattern-name-label-2");
    }
}
