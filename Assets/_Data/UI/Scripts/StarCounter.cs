using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// Reusable star counter component that displays collected / total stars
/// </summary>
[UxmlElement]
public partial class StarCounter : VisualElement
{
    private static List<StarCounter> instances = new List<StarCounter>();
    
    private Label iconLabel;
    private Label textLabel;
    private int currentDisplayValue = 0;
    private IVisualElementScheduledItem animationSchedule;
    private IVisualElementScheduledItem bounceSchedule;
    private IVisualElementScheduledItem glowSchedule;

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
        this.iconLabel = this.Q<Label>("star-counter-icon");
        this.textLabel = this.Q<Label>("star-counter-text");

        this.RegisterCallback<AttachToPanelEvent>(this.OnAttachToPanel);
        this.RegisterCallback<DetachFromPanelEvent>(this.OnDetachFromPanel);
        
        instances.Add(this);
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        // Defer to ensure singletons have initialized via Awake()
        this.schedule.Execute(this.UpdateStarCount);
    }
    
    private void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        instances.Remove(this);
        this.animationSchedule?.Pause();
        this.bounceSchedule?.Pause();
        this.glowSchedule?.Pause();
    }

    private void UpdateStarCount()
    {
        if (!Application.isPlaying) return;
        if (GameProgress.Instance == null) return;

        int collected = GameProgress.Instance.GetTotalStars();
        int total = GameProgress.Instance.GetMaxStars();
        
        this.currentDisplayValue = collected;
        this.textLabel.text = $"{collected} / {total}";
    }
    
    /// <summary>
    /// Simple increment for flying star impact - just updates value with quick pulse
    /// </summary>
    public void IncrementWithPulse(int newValue, int maxStars)
    {
        if (this.textLabel == null) return;
        
        this.currentDisplayValue = newValue;
        this.textLabel.text = $"{newValue} / {maxStars}";
        
        // Quick pulse effect
        this.AddToClassList("star-counter--pulse");
        this.AddToClassList("star-counter--glow");
        
        // Bounce scale
        float bounceProgress = 0f;
        this.bounceSchedule?.Pause();
        this.bounceSchedule = this.schedule.Execute(() =>
        {
            bounceProgress += 0.15f;
            float bounce = Mathf.Sin(bounceProgress * Mathf.PI) * 0.2f;
            this.style.scale = new Scale(new Vector3(1f + bounce, 1f + bounce, 1f));
            
            if (bounceProgress >= 1f)
            {
                this.style.scale = new Scale(Vector3.one);
                this.bounceSchedule.Pause();
            }
        }).Every(16);
        
        // Remove effect classes
        this.schedule.Execute(() =>
        {
            this.RemoveFromClassList("star-counter--pulse");
            this.RemoveFromClassList("star-counter--glow");
        }).StartingIn(200);
    }
    
    /// <summary>
    /// Animate star counter from old value to new value with enhanced multi-layer effects
    /// </summary>
    public void AnimateStarIncrease(int fromValue, int toValue, int maxStars)
    {
        if (this.textLabel == null || toValue <= fromValue) return;
        
        // Cancel any ongoing animations
        this.animationSchedule?.Pause();
        this.bounceSchedule?.Pause();
        this.glowSchedule?.Pause();
        
        int stepsToAnimate = toValue - fromValue;
        int currentStep = 0;
        float delayPerStep = Mathf.Clamp(1.2f / stepsToAnimate, 0.08f, 0.2f);
        
        this.currentDisplayValue = fromValue;
        
        // Initial pop animation
        this.PlayInitialPopAnimation();
        
        // Main counting animation with multiple effects per step
        this.animationSchedule = this.schedule.Execute(() =>
        {
            currentStep++;
            this.currentDisplayValue = fromValue + currentStep;
            this.textLabel.text = $"{this.currentDisplayValue} / {maxStars}";
            
            // Multi-layer effects on each increment
            this.PlayIncrementEffects();
            
            // Final celebration when done
            if (currentStep >= stepsToAnimate)
            {
                this.animationSchedule.Pause();
                this.PlayFinalCelebration();
            }
        }).StartingIn((long)(300)).Every((long)(delayPerStep * 1000));
    }
    
    /// <summary>
    /// Initial pop animation when counting starts
    /// </summary>
    private void PlayInitialPopAnimation()
    {
        // Scale bounce: 1.0 → 1.2 → 1.0
        this.AddToClassList("star-counter--pop");
        this.schedule.Execute(() => this.RemoveFromClassList("star-counter--pop")).StartingIn(250);
    }
    
    /// <summary>
    /// Multi-layer effects on each number increment
    /// </summary>
    private void PlayIncrementEffects()
    {
        // Layer 1: Pulse scale effect
        this.AddToClassList("star-counter--pulse");
        
        // Layer 2: Glow effect
        this.AddToClassList("star-counter--glow");
        
        // Layer 3: Bounce animation (layered scale transforms)
        float bounceProgress = 0f;
        this.bounceSchedule?.Pause();
        this.bounceSchedule = this.schedule.Execute(() =>
        {
            bounceProgress += 0.1f;
            float bounce = Mathf.Sin(bounceProgress * Mathf.PI) * 0.15f; // 0 → 0.15 → 0
            this.style.scale = new Scale(new Vector3(1f + bounce, 1f + bounce, 1f));
            
            if (bounceProgress >= 1f)
            {
                this.style.scale = new Scale(Vector3.one);
                this.bounceSchedule.Pause();
            }
        }).Every(16); // ~60 FPS
        
        // Remove classes after animation
        this.schedule.Execute(() =>
        {
            this.RemoveFromClassList("star-counter--pulse");
            this.RemoveFromClassList("star-counter--glow");
        }).StartingIn(180);
    }
    
    /// <summary>
    /// Final celebration effect when counting completes
    /// </summary>
    private void PlayFinalCelebration()
    {
        // Big bounce celebration
        this.AddToClassList("star-counter--celebrate");
        
        // Elastic bounce animation
        float celebrateProgress = 0f;
        this.bounceSchedule?.Pause();
        this.bounceSchedule = this.schedule.Execute(() =>
        {
            celebrateProgress += 0.05f;
            
            // Elastic bounce: big overshoot then settle
            float elastic = Mathf.Sin(celebrateProgress * Mathf.PI * 2f) * 
                           Mathf.Exp(-celebrateProgress * 4f) * 0.3f;
            this.style.scale = new Scale(new Vector3(1f + elastic, 1f + elastic, 1f));
            
            if (celebrateProgress >= 1f)
            {
                this.style.scale = new Scale(Vector3.one);
                this.bounceSchedule.Pause();
            }
        }).Every(16); // ~60 FPS
        
        // Pulsing glow effect
        int glowPulses = 0;
        this.glowSchedule?.Pause();
        this.glowSchedule = this.schedule.Execute(() =>
        {
            glowPulses++;
            this.AddToClassList("star-counter--glow");
            this.schedule.Execute(() => this.RemoveFromClassList("star-counter--glow")).StartingIn(120);
            
            if (glowPulses >= 3)
            {
                this.glowSchedule.Pause();
            }
        }).Every(200);
        
        // Remove celebrate class
        this.schedule.Execute(() => this.RemoveFromClassList("star-counter--celebrate")).StartingIn(1000);
    }
    
    /// <summary>
    /// Trigger animation on all active StarCounter instances
    /// </summary>
    public static void AnimateAllInstances(int fromValue, int toValue, int maxStars)
    {
        foreach (var instance in instances)
        {
            if (instance != null && instance.panel != null)
            {
                instance.AnimateStarIncrease(fromValue, toValue, maxStars);
            }
        }
    }
    
    /// <summary>
    /// Increment all active StarCounter instances with quick pulse effect
    /// Used for flying star impacts
    /// </summary>
    public static void IncrementAllInstances(int newValue, int maxStars)
    {
        foreach (var instance in instances)
        {
            if (instance != null && instance.panel != null)
            {
                instance.IncrementWithPulse(newValue, maxStars);
            }
        }
    }
}
