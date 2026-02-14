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
        int total = this.CalculateTotalMaxStars();

        this.textLabel.text = $"{collected} / {total}";
    }

    // Calculate the max possible stars across all 23 levels
    private int CalculateTotalMaxStars()
    {
        int total = 0;

        for (int level = 1; level <= 23; level++)
        {
            int difficulty = this.GetDifficultyFromLevel(level);
            total += GameProgress.GetStarsForDifficulty(difficulty);
        }

        return total;
    }

    // Mirror GameProgress difficulty mapping
    private int GetDifficultyFromLevel(int levelNumber)
    {
        if (levelNumber >= 1 && levelNumber <= 21)
        {
            return (levelNumber - 1) / 3;
        }
        else if (levelNumber == 22)
        {
            return 7;
        }
        else if (levelNumber == 23)
        {
            return 8;
        }

        return 0;
    }
}
