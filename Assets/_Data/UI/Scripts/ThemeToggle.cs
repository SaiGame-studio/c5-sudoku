using UnityEngine;
using UnityEngine.UIElements;

// Reusable theme toggle button for dark/light mode switching
public class ThemeToggle : VisualElement
{
    public new class UxmlFactory : UxmlFactory<ThemeToggle, UxmlTraits> { }
    public new class UxmlTraits : VisualElement.UxmlTraits { }

    private const string LIGHT_MODE_CLASS = "light-mode";

    private Label iconLabel;

    public ThemeToggle()
    {
        this.name = "theme-toggle";
        this.AddToClassList("theme-toggle");

        // Load internal markup from UXML template
        var template = Resources.Load<VisualTreeAsset>("ThemeToggle");
        if (template != null) template.CloneTree(this);

        this.iconLabel = this.Q<Label>("theme-toggle-label");

        this.RegisterCallback<AttachToPanelEvent>(this.OnAttachToPanel);
        this.RegisterCallback<ClickEvent>(this.OnClick);
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        // Defer to ensure singletons have initialized via Awake()
        this.schedule.Execute(this.ApplyTheme);
    }

    private void OnClick(ClickEvent evt)
    {
        evt.StopPropagation();
        if (!Application.isPlaying) return;
        if (ThemeManager.Instance == null) return;

        ThemeManager.Instance.ToggleTheme();
        this.ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (!Application.isPlaying) return;
        if (ThemeManager.Instance == null) return;

        var root = this.panel?.visualTree;
        if (root == null) return;

        if (ThemeManager.Instance.IsLightMode)
        {
            root.AddToClassList(LIGHT_MODE_CLASS);
        }
        else
        {
            root.RemoveFromClassList(LIGHT_MODE_CLASS);
        }

        this.iconLabel.text = ThemeManager.Instance.GetThemeIcon();
    }
}
