using UnityEngine;
using UnityEngine.InputSystem.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Detects screen orientation (Landscape/Portrait) and toggles the corresponding Canvas
public class OrientationCanvasManager : SaiBehaviour
{
    [Header("Canvas References")]
    [SerializeField] private GameObject landscapeCanvas;
    [SerializeField] private GameObject portraitCanvas;

    private bool isLandscape;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadLandscapeCanvas();
        this.LoadPortraitCanvas();
        this.EvaluateOrientation();

    }

    private void LoadLandscapeCanvas()
    {
        if (this.landscapeCanvas != null) return;
        Transform found = this.transform.Find("LandscapeCanvas");
        if (found != null) this.landscapeCanvas = found.gameObject;
        Debug.Log(transform.name + ": LoadLandscapeCanvas", gameObject);
    }

    private void LoadPortraitCanvas()
    {
        if (this.portraitCanvas != null) return;
        Transform found = this.transform.Find("PortraitCanvas");
        if (found != null) this.portraitCanvas = found.gameObject;
        Debug.Log(transform.name + ": LoadPortraitCanvas", gameObject);
    }

    private void EvaluateOrientation()
    {
        this.landscapeCanvas.SetActive(true);
        this.portraitCanvas.SetActive(true);

        bool currentIsLandscape = this.GetIsLandscape();

        if (currentIsLandscape == this.isLandscape
            && this.landscapeCanvas.activeSelf == currentIsLandscape)
            return;

        this.isLandscape = currentIsLandscape;
        Debug.Log("Screen: " + this.GetScreenWidth() + "x" + this.GetScreenHeight() + " => isLandscape: " + this.isLandscape);
        this.ApplyOrientation();
    }

    private void ApplyOrientation()
    {
        this.landscapeCanvas.SetActive(this.isLandscape);
        this.portraitCanvas.SetActive(!this.isLandscape);
    }

    private bool GetIsLandscape()
    {
        return this.GetScreenWidth() >= this.GetScreenHeight();
    }

    private float GetScreenWidth()
    {
#if UNITY_EDITOR
        return Handles.GetMainGameViewSize().x;
#else
        return Screen.width;
#endif
    }

    private float GetScreenHeight()
    {
#if UNITY_EDITOR
        return Handles.GetMainGameViewSize().y;
#else
        return Screen.height;
#endif
    }
}
