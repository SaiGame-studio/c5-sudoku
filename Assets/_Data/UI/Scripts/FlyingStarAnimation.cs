using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// Animated stars that fly from difficulty display to star counter
/// </summary>
public class FlyingStarAnimation
{
    private VisualElement rootElement;
    private List<Label> flyingStars = new List<Label>();
    private int starsToAnimate;
    private int currentStarIndex;
    private int fromValue;
    private int toValue;
    private int maxStars;
    
    /// <summary>
    /// Start flying star animation from difficulty stars to star counter
    /// Automatically finds UI elements in active game scene
    /// </summary>
    public static void PlayAnimation(int starsEarned, int fromValue, int toValue, int maxStars)
    {
        if (starsEarned <= 0)
        {
            Debug.LogWarning("[FlyingStarAnimation] No stars to animate");
            return;
        }
        
        // Find active UIDocument in scene
        var uiDocument = Object.FindFirstObjectByType<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogWarning("[FlyingStarAnimation] No UIDocument found in scene");
            return;
        }
        
        var root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogWarning("[FlyingStarAnimation] UIDocument root is null");
            return;
        }
        
        // Find difficulty stars container
        var sourceStarsContainer = root.Q<VisualElement>("difficulty-stars");
        if (sourceStarsContainer == null)
        {
            Debug.LogWarning("[FlyingStarAnimation] Difficulty stars container not found");
            return;
        }
        
        var animation = new FlyingStarAnimation();
        animation.rootElement = root;
        animation.starsToAnimate = starsEarned;
        animation.fromValue = fromValue;
        animation.toValue = toValue;
        animation.maxStars = maxStars;
        animation.currentStarIndex = 0;
        
        Debug.Log($"[FlyingStarAnimation] Starting animation: {starsEarned} stars, from {fromValue} to {toValue}");
        
        // Delay start to allow victory effect to play first
        root.schedule.Execute(() =>
        {
            animation.StartAnimation(sourceStarsContainer);
        }).StartingIn(800); // Start after 800ms delay
    }
    
    private void StartAnimation(VisualElement sourceStarsContainer)
    {
        // Get source star positions
        var sourceStars = sourceStarsContainer.Query<Label>(className: "difficulty-star").ToList();
        
        if (sourceStars.Count < this.starsToAnimate)
        {
            Debug.LogWarning($"[FlyingStarAnimation] Not enough source stars: {sourceStars.Count} < {this.starsToAnimate}");
            return;
        }
        
        // Find target StarCounter position
        var starCounter = this.rootElement.Q<StarCounter>();
        if (starCounter == null)
        {
            Debug.LogWarning("[FlyingStarAnimation] StarCounter not found in UI");
            return;
        }
        
        // Get world bounds
        var targetBounds = starCounter.worldBound;
        Vector2 targetCenter = new Vector2(
            targetBounds.x + targetBounds.width * 0.5f,
            targetBounds.y + targetBounds.height * 0.5f
        );
        
        // Create flying stars with delay between each
        this.rootElement.schedule.Execute(() =>
        {
            if (this.currentStarIndex < this.starsToAnimate)
            {
                int starIndex = this.currentStarIndex; // Capture index for this specific star
                Debug.Log($"[FlyingStarAnimation] Launching star {starIndex} of {this.starsToAnimate}");
                this.LaunchStar(sourceStars[starIndex], targetCenter, starCounter, starIndex);
                this.currentStarIndex++;
            }
        }).Every(200).Until(() => this.currentStarIndex >= this.starsToAnimate); // Stop when all stars launched
    }
    
    private void LaunchStar(Label sourceStar, Vector2 targetPosition, StarCounter targetCounter, int starIndex)
    {
        // Get source position
        var sourceBounds = sourceStar.worldBound;
        Vector2 startPosition = new Vector2(
            sourceBounds.x + sourceBounds.width * 0.5f,
            sourceBounds.y + sourceBounds.height * 0.5f
        );
        
        // Create flying star particle
        var flyingStar = new Label("★");
        flyingStar.AddToClassList("flying-star");
        flyingStar.style.position = Position.Absolute;
        flyingStar.style.left = startPosition.x;
        flyingStar.style.top = startPosition.y;
        flyingStar.style.fontSize = 28;
        flyingStar.style.color = new Color(1f, 0.84f, 0f, 1f); // Gold
        flyingStar.style.unityTextAlign = TextAnchor.MiddleCenter;
        
        this.rootElement.Add(flyingStar);
        this.flyingStars.Add(flyingStar);
        
        // Animate star flying to target
        this.AnimateStar(flyingStar, startPosition, targetPosition, targetCounter, starIndex);
    }
    
    private void AnimateStar(Label flyingStar, Vector2 startPos, Vector2 targetPos, StarCounter targetCounter, int starIndex)
    {
        float duration = 0.8f; // Animation duration in seconds
        float elapsed = 0f;
        float rotationSpeed = 720f; // Degrees per second
        float currentRotation = 0f;
        bool hasReachedTarget = false;
        
        var animSchedule = flyingStar.schedule.Execute(() =>
        {
            elapsed += 0.016f; // ~60 FPS
            float t = elapsed / duration;
            
            if (t >= 1f && !hasReachedTarget)
            {
                hasReachedTarget = true;
                // Star reached target - trigger explosion and increment
                this.OnStarReachedTarget(flyingStar, targetCounter, starIndex);
                return;
            }
            
            // Easing curve: ease-in-out
            float easedT = t < 0.5f 
                ? 2f * t * t 
                : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
            
            // Bezier curve for natural arc path
            Vector2 midPoint = (startPos + targetPos) * 0.5f;
            midPoint.y -= 100f; // Arc upward
            
            Vector2 currentPos = this.QuadraticBezier(startPos, midPoint, targetPos, easedT);
            
            flyingStar.style.left = currentPos.x;
            flyingStar.style.top = currentPos.y;
            
            // Rotate during flight
            currentRotation += rotationSpeed * 0.016f;
            flyingStar.style.rotate = new Rotate(currentRotation);
            
            // Scale pulse during flight
            float scale = 1f + Mathf.Sin(t * Mathf.PI * 3f) * 0.3f;
            flyingStar.style.scale = new Scale(new Vector3(scale, scale, 1f));
            
        }).Every(16).Until(() => hasReachedTarget); // Stop schedule when target reached
    }
    
    private Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        
        return uu * p0 + 2f * u * t * p1 + tt * p2;
    }
    
    private void OnStarReachedTarget(Label flyingStar, StarCounter targetCounter, int starIndex)
    {
        // Play explosion effect
        this.PlayExplosion(flyingStar);
        
        // Increment GameProgress by 1 star (this will also trigger Save)
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.IncrementTotalStars(1);
            Debug.Log($"[FlyingStarAnimation] Star {starIndex} reached target. Incremented by 1. Current total: {GameProgress.Instance.GetTotalStars()}");
        }
        
        // Update UI counters with new value (fromValue + starIndex + 1)
        int newValue = this.fromValue + starIndex + 1;
        if (newValue <= this.toValue)
        {
            StarCounter.IncrementAllInstances(newValue, this.maxStars);
        }
        
        // Remove flying star after explosion
        flyingStar.schedule.Execute(() =>
        {
            this.rootElement.Remove(flyingStar);
            this.flyingStars.Remove(flyingStar);
        }).StartingIn(300);
    }
    
    private void PlayExplosion(Label flyingStar)
    {
        // Create explosion particles
        int particleCount = 8;
        float explosionRadius = 50f;
        
        for (int i = 0; i < particleCount; i++)
        {
            float angle = (360f / particleCount) * i * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            
            this.CreateExplosionParticle(flyingStar, direction, explosionRadius);
        }
        
        // Flash the flying star
        flyingStar.style.scale = new Scale(new Vector3(2f, 2f, 1f));
        flyingStar.style.opacity = 1f;
        
        // Fade out flash
        float elapsed = 0f;
        flyingStar.schedule.Execute(() =>
        {
            elapsed += 0.016f;
            float t = elapsed / 0.3f;
            
            flyingStar.style.scale = new Scale(Vector3.one * (2f - t));
            flyingStar.style.opacity = 1f - t;
            
        }).Every(16).Until(() => elapsed >= 0.3f);
    }
    
    private void CreateExplosionParticle(Label source, Vector2 direction, float maxDistance)
    {
        var particle = new Label("•");
        particle.style.position = Position.Absolute;
        particle.style.left = source.resolvedStyle.left;
        particle.style.top = source.resolvedStyle.top;
        particle.style.fontSize = 20;
        particle.style.color = new Color(1f, 0.92f, 0.5f, 1f);
        
        this.rootElement.Add(particle);
        
        // Animate particle outward
        float elapsed = 0f;
        float duration = 0.3f;
        Vector2 startPos = new Vector2(particle.resolvedStyle.left, particle.resolvedStyle.top);
        
        particle.schedule.Execute(() =>
        {
            elapsed += 0.016f;
            float t = elapsed / duration;
            
            if (t >= 1f)
            {
                this.rootElement.Remove(particle);
                return;
            }
            
            float distance = maxDistance * t;
            Vector2 pos = startPos + direction * distance;
            
            particle.style.left = pos.x;
            particle.style.top = pos.y;
            particle.style.opacity = 1f - t;
            particle.style.scale = new Scale(Vector3.one * (1f - t * 0.5f));
            
        }).Every(16);
    }
}
