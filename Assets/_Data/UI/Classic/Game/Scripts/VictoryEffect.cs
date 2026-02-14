using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class VictoryEffect
{
    private const int GRID_SIZE = 9;
    private const float WAVE_DELAY_PER_CELL = 0.06f;
    private const float CELEBRATION_HOLD_DURATION = 8.0f;
    private const int CONFETTI_COUNT = 120;
    private const int FIREWORK_BURST_COUNT = 14;
    private const int SPARKS_PER_BURST = 24;
    private const int TRAIL_SPARKS_PER_BURST = 8;

    private const string CLASS_VICTORY_CELL = "sudoku-cell--victory";
    private const string CLASS_VICTORY_OVERLAY = "victory-overlay";
    private const string CLASS_VICTORY_OVERLAY_VISIBLE = "victory-overlay--visible";
    private const string CLASS_VICTORY_BANNER = "victory-banner";
    private const string CLASS_VICTORY_TITLE = "victory-title";
    private const string CLASS_VICTORY_SUBTITLE = "victory-subtitle";

    private static readonly Color[] CONFETTI_COLORS = new Color[]
    {
        new Color(1f, 0.84f, 0f),       // Gold
        new Color(1f, 0.27f, 0.27f),     // Red
        new Color(0.27f, 0.8f, 1f),      // Cyan
        new Color(0.6f, 1f, 0.4f),       // Lime
        new Color(1f, 0.55f, 0.85f),     // Pink
        new Color(0.65f, 0.45f, 1f),     // Purple
        new Color(1f, 0.65f, 0.15f),     // Orange
        new Color(1f, 1f, 0.4f),         // Yellow
    };

    private static readonly Color[] FIREWORK_COLORS = new Color[]
    {
        new Color(1f, 0.84f, 0f),
        new Color(1f, 0.4f, 0.4f),
        new Color(0.4f, 0.85f, 1f),
        new Color(0.5f, 1f, 0.5f),
        new Color(1f, 0.55f, 0.85f),
    };

    private SudokuCell[,] cells;
    private VisualElement root;
    private VisualElement victoryOverlay;
    private VisualElement particleLayer;
    private List<VisualElement> activeParticles;
    private bool isPlaying;

    public bool IsPlaying => this.isPlaying;

    public VictoryEffect(SudokuCell[,] cells, VisualElement root)
    {
        this.cells = cells;
        this.root = root;
        this.isPlaying = false;
        this.activeParticles = new List<VisualElement>();
        this.BuildOverlay();
    }

    private void BuildOverlay()
    {
        // Victory overlay (full screen, initially hidden)
        this.victoryOverlay = new VisualElement();
        this.victoryOverlay.AddToClassList(CLASS_VICTORY_OVERLAY);
        this.victoryOverlay.pickingMode = PickingMode.Ignore;

        // Particle layer for fireworks and confetti (behind banner)
        this.particleLayer = new VisualElement();
        this.particleLayer.AddToClassList("victory-particle-layer");
        this.particleLayer.pickingMode = PickingMode.Ignore;
        this.victoryOverlay.Add(this.particleLayer);

        // Banner container (pickingMode Ignore to prevent blocking grid clicks when invisible)
        VisualElement banner = new VisualElement();
        banner.AddToClassList(CLASS_VICTORY_BANNER);
        banner.pickingMode = PickingMode.Ignore;

        // Title
        Label title = new Label("\u2605 VICTORY \u2605");
        title.AddToClassList(CLASS_VICTORY_TITLE);
        title.pickingMode = PickingMode.Ignore;
        banner.Add(title);

        this.victoryOverlay.Add(banner);
        this.root.Add(this.victoryOverlay);

        // Dismiss on click
        this.victoryOverlay.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            this.StopEffect();
        });
    }

    /// <summary>
    /// Start the victory celebration animation via coroutine
    /// </summary>
    public IEnumerator PlayAnimation()
    {
        if (this.isPlaying) yield break;
        this.isPlaying = true;

        // Allow overlay to receive clicks for dismissal
        this.victoryOverlay.pickingMode = PickingMode.Position;

        // Phase 1: Wave animation across cells (diagonal sweep)
        float maxDelay = 0f;

        for (int diag = 0; diag < (GRID_SIZE * 2 - 1); diag++)
        {
            for (int row = 0; row < GRID_SIZE; row++)
            {
                int col = diag - row;
                if (col < 0 || col >= GRID_SIZE) continue;
                if (this.cells[row, col] == null) continue;

                float delay = diag * WAVE_DELAY_PER_CELL;
                if (delay > maxDelay) maxDelay = delay;

                this.ScheduleVictoryClass(row, col, delay);
            }
        }

        // Wait for wave to reach last cell
        yield return new WaitForSeconds(maxDelay + 0.1f);

        // Phase 2: Show victory overlay
        this.victoryOverlay.AddToClassList(CLASS_VICTORY_OVERLAY_VISIBLE);

        // Phase 3: Launch fireworks (staggered bursts across full duration)
        for (int i = 0; i < FIREWORK_BURST_COUNT; i++)
        {
            // Rapid fire at start, then steady bursts
            float burstDelay;
            if (i < 4)
                burstDelay = i * 0.25f; // Fast initial volley
            else
                burstDelay = 1.0f + (i - 4) * 0.55f; // Steady continuation

            this.ScheduleFireworkBurst(burstDelay);
        }

        // Phase 4: Spawn confetti rain (multiple waves)
        this.SpawnConfetti();

        // Second wave of confetti after 3 seconds
        this.particleLayer.schedule.Execute(() =>
        {
            if (!this.isPlaying) return;
            this.SpawnConfetti();
        }).StartingIn(3000);

        // Phase 5: Hold celebration
        yield return new WaitForSeconds(CELEBRATION_HOLD_DURATION);

        // Phase 6: Auto cleanup
        this.StopEffect();
    }

    private void ScheduleVictoryClass(int row, int col, float delay)
    {
        SudokuCell cell = this.cells[row, col];
        if (cell == null || cell.Element == null) return;

        cell.Element.schedule.Execute(() =>
        {
            cell.Element.AddToClassList(CLASS_VICTORY_CELL);
        }).StartingIn((long)(delay * 1000));
    }

    #region Fireworks

    private void ScheduleFireworkBurst(float delay)
    {
        this.particleLayer.schedule.Execute(() =>
        {
            if (!this.isPlaying) return;
            this.CreateFireworkBurst();
        }).StartingIn((long)(delay * 1000));
    }

    private void CreateFireworkBurst()
    {
        float rootWidth = this.root.resolvedStyle.width;
        float rootHeight = this.root.resolvedStyle.height;

        if (rootWidth <= 0 || rootHeight <= 0) return;

        // Random burst origin within the center ~80% of the screen
        float centerX = rootWidth * (0.1f + Random.value * 0.8f);
        float centerY = rootHeight * (0.1f + Random.value * 0.55f);

        Color burstColor = FIREWORK_COLORS[Random.Range(0, FIREWORK_COLORS.Length)];

        // Create main sparks radiating outward
        for (int i = 0; i < SPARKS_PER_BURST; i++)
        {
            float angle = (360f / SPARKS_PER_BURST) * i + Random.Range(-10f, 10f);
            float distance = Random.Range(90f, 220f);
            float rad = angle * Mathf.Deg2Rad;

            float endX = centerX + Mathf.Cos(rad) * distance;
            float endY = centerY + Mathf.Sin(rad) * distance;

            float sparkSize = Random.Range(6f, 14f);

            VisualElement spark = new VisualElement();
            spark.AddToClassList("firework-spark");
            spark.pickingMode = PickingMode.Ignore;

            // Slight color variation per spark
            Color sparkColor = Color.Lerp(burstColor, Color.white, Random.Range(0f, 0.35f));
            spark.style.backgroundColor = sparkColor;
            spark.style.width = sparkSize;
            spark.style.height = sparkSize;
            spark.style.borderTopLeftRadius = sparkSize;
            spark.style.borderTopRightRadius = sparkSize;
            spark.style.borderBottomLeftRadius = sparkSize;
            spark.style.borderBottomRightRadius = sparkSize;

            // Start at center
            spark.style.left = centerX - sparkSize / 2f;
            spark.style.top = centerY - sparkSize / 2f;

            this.particleLayer.Add(spark);
            this.activeParticles.Add(spark);

            // Animate outward with translate + fade
            float duration = Random.Range(0.6f, 1.2f);
            float translateX = endX - centerX;
            float translateY = endY - centerY;

            // Phase 1: expand outward
            spark.schedule.Execute(() =>
            {
                spark.style.translate = new Translate(translateX, translateY);
                spark.style.opacity = 0f;
                spark.style.scale = new Scale(new Vector3(0.2f, 0.2f, 1f));
            }).StartingIn(20);

            // Phase 2: remove after animation
            spark.schedule.Execute(() =>
            {
                this.RemoveParticle(spark);
            }).StartingIn((long)(duration * 1000) + 100);
        }

        // Create trailing sparks (secondary smaller sparks that linger)
        for (int i = 0; i < TRAIL_SPARKS_PER_BURST; i++)
        {
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(30f, 100f);
            float rad = angle * Mathf.Deg2Rad;

            float trailSize = Random.Range(3f, 6f);
            Color trailColor = Color.Lerp(burstColor, Color.yellow, Random.Range(0.2f, 0.6f));

            VisualElement trail = new VisualElement();
            trail.AddToClassList("firework-spark");
            trail.pickingMode = PickingMode.Ignore;
            trail.style.backgroundColor = trailColor;
            trail.style.width = trailSize;
            trail.style.height = trailSize;
            trail.style.borderTopLeftRadius = trailSize;
            trail.style.borderTopRightRadius = trailSize;
            trail.style.borderBottomLeftRadius = trailSize;
            trail.style.borderBottomRightRadius = trailSize;
            trail.style.left = centerX - trailSize / 2f;
            trail.style.top = centerY - trailSize / 2f;

            this.particleLayer.Add(trail);
            this.activeParticles.Add(trail);

            float trailEndX = Mathf.Cos(rad) * distance;
            // Trail sparks fall downward a bit (gravity feel)
            float trailEndY = Mathf.Sin(rad) * distance + Random.Range(30f, 80f);
            float trailDuration = Random.Range(0.8f, 1.6f);

            // Delayed start for trailing effect
            long trailDelay = (long)(Random.Range(0.1f, 0.4f) * 1000);
            trail.schedule.Execute(() =>
            {
                trail.style.translate = new Translate(trailEndX, trailEndY);
                trail.style.opacity = 0f;
                trail.style.scale = new Scale(new Vector3(0.1f, 0.1f, 1f));
            }).StartingIn(trailDelay);

            trail.schedule.Execute(() =>
            {
                this.RemoveParticle(trail);
            }).StartingIn((long)(trailDuration * 1000) + trailDelay + 100);
        }

        // Flash at burst center (bigger flash)
        VisualElement flash = new VisualElement();
        flash.AddToClassList("firework-flash");
        flash.pickingMode = PickingMode.Ignore;
        flash.style.backgroundColor = Color.Lerp(burstColor, Color.white, 0.6f);
        flash.style.left = centerX - 20f;
        flash.style.top = centerY - 20f;
        flash.style.width = 40f;
        flash.style.height = 40f;
        flash.style.borderTopLeftRadius = 20f;
        flash.style.borderTopRightRadius = 20f;
        flash.style.borderBottomLeftRadius = 20f;
        flash.style.borderBottomRightRadius = 20f;

        this.particleLayer.Add(flash);
        this.activeParticles.Add(flash);

        // Fade flash
        flash.schedule.Execute(() =>
        {
            flash.AddToClassList("firework-flash--fade");
        }).StartingIn(30);

        flash.schedule.Execute(() =>
        {
            this.RemoveParticle(flash);
        }).StartingIn(600);
    }

    #endregion

    #region Confetti

    private void SpawnConfetti()
    {
        float rootWidth = this.root.resolvedStyle.width;
        float rootHeight = this.root.resolvedStyle.height;

        if (rootWidth <= 0 || rootHeight <= 0) return;

        for (int i = 0; i < CONFETTI_COUNT; i++)
        {
            float delay = Random.Range(0f, 3.5f);

            this.particleLayer.schedule.Execute(() =>
            {
                if (!this.isPlaying) return;
                this.CreateConfettiPiece(rootWidth, rootHeight);
            }).StartingIn((long)(delay * 1000));
        }
    }

    private void CreateConfettiPiece(float rootWidth, float rootHeight)
    {
        VisualElement confetti = new VisualElement();
        confetti.AddToClassList("confetti-piece");
        confetti.pickingMode = PickingMode.Ignore;

        Color color = CONFETTI_COLORS[Random.Range(0, CONFETTI_COLORS.Length)];
        confetti.style.backgroundColor = color;

        // Random size (rectangular confetti pieces)
        float width = Random.Range(6f, 14f);
        float height = Random.Range(4f, 10f);
        confetti.style.width = width;
        confetti.style.height = height;
        confetti.style.borderTopLeftRadius = Random.Range(0f, 3f);
        confetti.style.borderTopRightRadius = Random.Range(0f, 3f);
        confetti.style.borderBottomLeftRadius = Random.Range(0f, 3f);
        confetti.style.borderBottomRightRadius = Random.Range(0f, 3f);

        // Start from top, random horizontal position
        float startX = Random.Range(-20f, rootWidth + 20f);
        float startY = Random.Range(-40f, -10f);

        confetti.style.left = startX;
        confetti.style.top = startY;

        // Random initial rotation
        float initialRotation = Random.Range(0f, 360f);
        confetti.style.rotate = new Rotate(initialRotation);

        this.particleLayer.Add(confetti);
        this.activeParticles.Add(confetti);

        // Animate falling with horizontal drift
        float fallDistance = rootHeight + 60f;
        float driftX = Random.Range(-80f, 80f);
        float endRotation = initialRotation + Random.Range(-360f, 360f);
        float duration = Random.Range(2.0f, 3.5f);

        confetti.schedule.Execute(() =>
        {
            confetti.style.translate = new Translate(driftX, fallDistance);
            confetti.style.rotate = new Rotate(endRotation);
            confetti.style.opacity = 0.3f;

            // Set transition dynamically for varied fall speed
            confetti.style.transitionProperty = new List<StylePropertyName>
            {
                new StylePropertyName("translate"),
                new StylePropertyName("rotate"),
                new StylePropertyName("opacity")
            };
            confetti.style.transitionDuration = new List<TimeValue>
            {
                new TimeValue(duration, TimeUnit.Second),
                new TimeValue(duration, TimeUnit.Second),
                new TimeValue(duration * 0.8f, TimeUnit.Second)
            };
            confetti.style.transitionTimingFunction = new List<EasingFunction>
            {
                new EasingFunction(EasingMode.EaseIn),
                new EasingFunction(EasingMode.Linear),
                new EasingFunction(EasingMode.EaseIn)
            };
        }).StartingIn(20);

        // Remove after animation
        confetti.schedule.Execute(() =>
        {
            this.RemoveParticle(confetti);
        }).StartingIn((long)(duration * 1000) + 200);
    }

    #endregion

    #region Cleanup

    private void RemoveParticle(VisualElement particle)
    {
        if (particle != null && particle.parent != null)
        {
            particle.parent.Remove(particle);
        }
        this.activeParticles.Remove(particle);
    }

    /// <summary>
    /// Stop and clean up the victory effect
    /// </summary>
    public void StopEffect()
    {
        if (!this.isPlaying) return;

        // Remove victory class from all cells
        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                if (this.cells[row, col] != null)
                {
                    this.cells[row, col].Element.RemoveFromClassList(CLASS_VICTORY_CELL);
                }
            }
        }

        // Remove all active particles
        for (int i = this.activeParticles.Count - 1; i >= 0; i--)
        {
            VisualElement particle = this.activeParticles[i];
            if (particle != null && particle.parent != null)
            {
                particle.parent.Remove(particle);
            }
        }
        this.activeParticles.Clear();

        // Hide overlay
        this.victoryOverlay.RemoveFromClassList(CLASS_VICTORY_OVERLAY_VISIBLE);
        this.victoryOverlay.pickingMode = PickingMode.Ignore;

        this.isPlaying = false;
    }

    /// <summary>
    /// Clean up overlay from DOM
    /// </summary>
    public void Dispose()
    {
        this.StopEffect();

        if (this.victoryOverlay != null && this.victoryOverlay.parent != null)
        {
            this.victoryOverlay.parent.Remove(this.victoryOverlay);
        }
    }

    #endregion
}
