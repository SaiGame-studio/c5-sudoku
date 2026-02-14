using UnityEngine;
using UnityEngine.UIElements;

// Reusable back button for scene navigation
public class BackButton : VisualElement
{
    public new class UxmlFactory : UxmlFactory<BackButton, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        private UxmlStringAttributeDescription targetAttr = new UxmlStringAttributeDescription
        {
            name = "target",
            defaultValue = "main-menu"
        };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            ((BackButton)ve).target = this.targetAttr.GetValueFromBag(bag, cc);
        }
    }

    private string target;

    public string Target
    {
        get => this.target;
        set => this.target = value;
    }

    public BackButton()
    {
        this.name = "back-button";

        // Load internal markup from UXML template
        var template = Resources.Load<VisualTreeAsset>("BackButton");
        if (template != null) template.CloneTree(this);

        var btn = this.Q<Button>("back-button-btn");
        if (btn != null)
        {
            btn.clicked += this.OnClicked;
        }
    }

    private void OnClicked()
    {
        if (!Application.isPlaying) return;
        if (GameManager.Instance == null) return;

        switch (this.target)
        {
            case "main-menu":
                GameManager.Instance.LoadMainMenu();
                break;
            case "classic-home":
                GameManager.Instance.LoadClassicHome();
                break;
        }
    }
}
