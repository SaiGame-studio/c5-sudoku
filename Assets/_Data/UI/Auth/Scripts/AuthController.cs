using com.cyborgAssets.inspectorButtonPro;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.UIElements;

public class AuthController : SaiBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Sai Service")]
    [SerializeField] private SaiAuth saiAuth;

    [Header("Scale Setting")]
    [SerializeField] private float addLandscapeScale = 0.5f;
    [SerializeField] private float addPortraitScale = 0f;

    [Header("Visual Elements")]
    [SerializeField] private VisualElement root;
    [SerializeField] private VisualElement authContainer;

    // Login page elements
    private VisualElement loginPage;
    private TextField loginUsernameField;
    private TextField loginPasswordField;
    private Label loginErrorLabel;
    private Button loginButton;
    private Button gotoRegisterButton;
    private Button backToMenuLogin;

    // Register page elements
    private VisualElement registerPage;
    private TextField registerEmailField;
    private TextField registerUsernameField;
    private TextField registerPasswordField;
    private Label registerErrorLabel;
    private Button registerButton;
    private Button gotoLoginButton;
    private Button backToMenuRegister;

    private bool isProcessing;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadUIDocument();
        this.LoadSaiAuth();
        this.LoadUIElements();
    }

    private void LoadUIDocument()
    {
        if (this.uiDocument != null) return;
        this.uiDocument = GetComponent<UIDocument>();
    }

    private void LoadSaiAuth()
    {
        if (this.saiAuth != null) return;
        this.saiAuth = FindFirstObjectByType<SaiAuth>();
    }

    [ProButton]
    private void LoadUIElements()
    {
        if (this.uiDocument == null) return;

        this.root = this.uiDocument.rootVisualElement;
        if (this.root == null) return;

        this.authContainer = this.root.Q<VisualElement>(className: "auth-container");

        // Login page
        this.loginPage = this.root.Q<VisualElement>("login-page");
        this.loginUsernameField = this.root.Q<TextField>("login-username");
        this.loginPasswordField = this.root.Q<TextField>("login-password");
        this.loginErrorLabel = this.root.Q<Label>("login-error");
        this.loginButton = this.root.Q<Button>("login-button");
        this.gotoRegisterButton = this.root.Q<Button>("goto-register-button");
        this.backToMenuLogin = this.root.Q<Button>("back-to-menu-login");

        // Register page
        this.registerPage = this.root.Q<VisualElement>("register-page");
        this.registerEmailField = this.root.Q<TextField>("register-email");
        this.registerUsernameField = this.root.Q<TextField>("register-username");
        this.registerPasswordField = this.root.Q<TextField>("register-password");
        this.registerErrorLabel = this.root.Q<Label>("register-error");
        this.registerButton = this.root.Q<Button>("register-button");
        this.gotoLoginButton = this.root.Q<Button>("goto-login-button");
        this.backToMenuRegister = this.root.Q<Button>("back-to-menu-register");
    }

    protected override void Start()
    {
        base.Start();
        this.InitializeUI();
    }

    private void InitializeUI()
    {
        if (this.root == null || this.authContainer == null)
        {
            this.LoadUIElements();
        }

        if (this.root == null) return;

        this.root.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            this.root.UnregisterCallback<GeometryChangedEvent>(this.OnRootGeometryChanged);
            this.ApplyResponsiveScale();
        });

        this.RegisterButtonCallbacks();
        this.ShowLoginPage();
    }

    private void OnRootGeometryChanged(GeometryChangedEvent evt)
    {
        this.ApplyResponsiveScale();
    }

    [ProButton]
    private void ApplyResponsiveScale()
    {
        if (this.authContainer == null)
        {
            this.LoadUIElements();
        }

        if (this.authContainer == null) return;

        float canvasWidth = 0f;
        float canvasHeight = 0f;

        var panelSettings = this.uiDocument.panelSettings;

        if (panelSettings != null)
        {
            Vector2 refRes = panelSettings.referenceResolution;
            if (refRes.x > 0 && refRes.y > 0)
            {
                canvasWidth = refRes.x;
                canvasHeight = refRes.y;
            }
            else if (panelSettings.targetTexture != null)
            {
                canvasWidth = panelSettings.targetTexture.width;
                canvasHeight = panelSettings.targetTexture.height;
            }
        }

        if (canvasWidth <= 0 || float.IsNaN(canvasWidth))
        {
            canvasWidth = Screen.width;
            canvasHeight = Screen.height;
        }

        bool isLandscape = canvasWidth > canvasHeight;

        if (isLandscape)
        {
            this.ApplyResponsiveScaleLandscape(canvasWidth, canvasHeight);
        }
        else
        {
            this.ApplyResponsiveScalePortrait(canvasWidth, canvasHeight);
        }
    }

    private void ApplyResponsiveScaleLandscape(float canvasWidth, float canvasHeight)
    {
        float baseWidth = 1920f;
        float baseHeight = 1080f;

        float scaleX = canvasWidth / baseWidth;
        float scaleY = canvasHeight / baseHeight;
        float scale = Mathf.Min(scaleX, scaleY);

        scale = Mathf.Max(scale, 0.5f);
        scale += this.addLandscapeScale;

        this.authContainer.style.transformOrigin = new StyleTransformOrigin(
            new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(0, LengthUnit.Percent))
        );

        this.authContainer.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1)));
    }

    private void ApplyResponsiveScalePortrait(float canvasWidth, float canvasHeight)
    {
        float baseWidth = 1080f;
        float baseHeight = 1920f;

        float scaleX = canvasWidth / baseWidth;
        float scaleY = canvasHeight / baseHeight;
        float scale = Mathf.Min(scaleX, scaleY);

        scale = Mathf.Max(scale, 0.4f);
        scale += this.addPortraitScale;

        this.authContainer.style.transformOrigin = new StyleTransformOrigin(
            new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(0, LengthUnit.Percent))
        );

        this.authContainer.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1)));
    }

    private void RegisterButtonCallbacks()
    {
        // Login page buttons
        if (this.loginButton != null)
        {
            this.loginButton.clicked += this.OnLoginButtonClicked;
        }

        if (this.gotoRegisterButton != null)
        {
            this.gotoRegisterButton.clicked += this.ShowRegisterPage;
        }

        if (this.backToMenuLogin != null)
        {
            this.backToMenuLogin.clicked += this.OnBackToMenuClicked;
        }

        // Register page buttons
        if (this.registerButton != null)
        {
            this.registerButton.clicked += this.OnRegisterButtonClicked;
        }

        if (this.gotoLoginButton != null)
        {
            this.gotoLoginButton.clicked += this.ShowLoginPage;
        }

        if (this.backToMenuRegister != null)
        {
            this.backToMenuRegister.clicked += this.OnBackToMenuClicked;
        }
    }

    #region Page Navigation

    [ProButton]
    private void ShowLoginPage()
    {
        if (this.loginPage != null)
        {
            this.loginPage.style.display = DisplayStyle.Flex;
        }

        if (this.registerPage != null)
        {
            this.registerPage.style.display = DisplayStyle.None;
        }

        this.ClearLoginForm();
    }

    [ProButton]
    private void ShowRegisterPage()
    {
        if (this.loginPage != null)
        {
            this.loginPage.style.display = DisplayStyle.None;
        }

        if (this.registerPage != null)
        {
            this.registerPage.style.display = DisplayStyle.Flex;
        }

        this.ClearRegisterForm();
    }

    #endregion

    #region Login

    private void OnLoginButtonClicked()
    {
        if (this.isProcessing) return;

        string username = this.loginUsernameField?.value ?? "";
        string password = this.loginPasswordField?.value ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            this.ShowLoginError("Please fill in all fields");
            return;
        }

        this.SetLoginProcessing(true);
        this.ShowLoginError("");

        this.saiAuth.Login(username, password,
            response =>
            {
                this.SetLoginProcessing(false);
                this.OnLoginSuccess(response);
            },
            error =>
            {
                this.SetLoginProcessing(false);
                this.ShowLoginError(error);
            }
        );
    }

    private void OnLoginSuccess(LoginResponse response)
    {
        // Navigate to menu after successful login
        GameManager.Instance.LoadMainMenu();
    }

    private void ShowLoginError(string message)
    {
        if (this.loginErrorLabel != null)
        {
            this.loginErrorLabel.text = message;
        }
    }

    private void SetLoginProcessing(bool processing)
    {
        this.isProcessing = processing;

        if (this.loginButton != null)
        {
            this.loginButton.SetEnabled(!processing);
            Label label = this.loginButton.Q<Label>(className: "auth-button-label");
            if (label != null)
            {
                label.text = processing ? "Logging in..." : "Login";
            }
        }
    }

    private void ClearLoginForm()
    {
        if (this.loginUsernameField != null) this.loginUsernameField.value = "";
        if (this.loginPasswordField != null) this.loginPasswordField.value = "";
        this.ShowLoginError("");
    }

    #endregion

    #region Register

    private void OnRegisterButtonClicked()
    {
        if (this.isProcessing) return;

        string email = this.registerEmailField?.value ?? "";
        string username = this.registerUsernameField?.value ?? "";
        string password = this.registerPasswordField?.value ?? "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            this.ShowRegisterError("Please fill in all fields");
            return;
        }

        if (!this.IsValidEmail(email))
        {
            this.ShowRegisterError("Please enter a valid email address");
            return;
        }

        this.SetRegisterProcessing(true);
        this.ShowRegisterError("");

        this.saiAuth.Register(email, username, password,
            response =>
            {
                this.SetRegisterProcessing(false);
                this.OnRegisterSuccess(response);
            },
            error =>
            {
                this.SetRegisterProcessing(false);
                this.ShowRegisterError(error);
            }
        );
    }

    private void OnRegisterSuccess(RegisterResponse response)
    {
        // Switch to login page after successful registration
        this.ShowLoginPage();
        this.ShowLoginError("Registration successful! Please login.");

        // Override error color to green for success message
        if (this.loginErrorLabel != null)
        {
            this.loginErrorLabel.style.color = new StyleColor(new Color(0.4f, 0.9f, 0.5f));
        }
    }

    private void ShowRegisterError(string message)
    {
        if (this.registerErrorLabel != null)
        {
            this.registerErrorLabel.text = message;
        }
    }

    private void SetRegisterProcessing(bool processing)
    {
        this.isProcessing = processing;

        if (this.registerButton != null)
        {
            this.registerButton.SetEnabled(!processing);
            Label label = this.registerButton.Q<Label>(className: "auth-button-label");
            if (label != null)
            {
                label.text = processing ? "Registering..." : "Register";
            }
        }
    }

    private void ClearRegisterForm()
    {
        if (this.registerEmailField != null) this.registerEmailField.value = "";
        if (this.registerUsernameField != null) this.registerUsernameField.value = "";
        if (this.registerPasswordField != null) this.registerPasswordField.value = "";
        this.ShowRegisterError("");
    }

    #endregion

    #region Helpers

    private void OnBackToMenuClicked()
    {
        GameManager.Instance.LoadMainMenu();
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        int atIndex = email.IndexOf('@');
        if (atIndex <= 0) return false;
        int dotIndex = email.LastIndexOf('.');
        return dotIndex > atIndex + 1 && dotIndex < email.Length - 1;
    }

    #endregion
}
