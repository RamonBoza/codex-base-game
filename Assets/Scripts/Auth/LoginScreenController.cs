using UnityEngine;

[DisallowMultipleComponent]
public sealed class LoginScreenController : MonoBehaviour
{
    private const string GoogleClientIdPrefsKey = "cozy.googleOAuthClientId";

    [SerializeField] private CozyWorldBootstrap worldBootstrap;
    [SerializeField] private bool restoreSessionOnStart = true;
    [SerializeField] private string googleOAuthClientId = string.Empty;
    [SerializeField] private int googleOAuthTimeoutSeconds = 120;

    private LocalIdentityService identityService;
    private GoogleOAuthService googleOAuthService;
    private AuthSession currentSession;
    private bool showRegisterForm;
    private bool showGoogleSettings;
    private bool isAuthenticating;
    private string email = string.Empty;
    private string password = string.Empty;
    private string displayName = string.Empty;
    private string googleClientIdInput = string.Empty;
    private string statusMessage = string.Empty;
    private Vector2 scrollPosition;

    private GUIStyle logoPrimaryStyle;
    private GUIStyle logoSecondaryStyle;
    private GUIStyle fieldLabelStyle;
    private GUIStyle inputStyle;
    private GUIStyle primaryButtonStyle;
    private GUIStyle googleButtonStyle;
    private GUIStyle googleIconStyle;
    private GUIStyle googleTextStyle;
    private GUIStyle linkStyle;
    private GUIStyle statusStyle;
    private Texture2D backgroundTexture;
    private Texture2D fieldFillTexture;
    private Texture2D fieldBorderTexture;
    private Texture2D primaryButtonTexture;
    private Texture2D primaryButtonHoverTexture;
    private Texture2D googleButtonTexture;
    private Texture2D googleButtonHoverTexture;

    private bool IsAuthenticated => currentSession != null && currentSession.IsValid;

    private void Awake()
    {
        identityService = new LocalIdentityService();
        googleOAuthService = new GoogleOAuthService();
        googleClientIdInput = PlayerPrefs.GetString(GoogleClientIdPrefsKey, googleOAuthClientId);

        if (worldBootstrap == null)
        {
            worldBootstrap = GetComponent<CozyWorldBootstrap>();
        }
    }

    private void Start()
    {
        if (restoreSessionOnStart && identityService.TryLoadSession(out AuthSession restoredSession))
        {
            EnterWorld(restoredSession, "Sesion restaurada.");
        }
    }

    private void OnGUI()
    {
        if (IsAuthenticated)
        {
            DrawSessionHud();
            return;
        }

        DrawLoginScreen();
    }

    private void DrawLoginScreen()
    {
        EnsureStyles();

        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), backgroundTexture);

        float formWidth = Mathf.Min(430f, Screen.width - 48f);
        float contentHeight = CalculateLoginHeight();
        Rect viewportRect = new Rect(0f, 0f, Screen.width, Screen.height);
        Rect contentRect = new Rect(0f, 0f, Screen.width, Mathf.Max(Screen.height, contentHeight + 48f));
        scrollPosition = GUI.BeginScrollView(viewportRect, scrollPosition, contentRect, false, false);

        float x = (Screen.width - formWidth) * 0.5f;
        float y = Mathf.Max(24f, (Screen.height - contentHeight) * 0.5f);

        DrawLogo(x, y, formWidth);
        y += 126f;

        email = DrawTextInput(x, ref y, formWidth, "Username", email, false);
        password = DrawTextInput(x, ref y, formWidth, "Password", password, true);

        if (showRegisterForm)
        {
            displayName = DrawTextInput(x, ref y, formWidth, "Display name", displayName, false);
        }

        y += 8f;

        GUI.enabled = !isAuthenticating;
        string primaryButtonLabel = showRegisterForm ? "<b>Create account</b>" : "<b>Sign in</b>";
        Rect primaryButtonRect = new Rect(x + (formWidth - 122f) * 0.5f, y, 122f, 58f);

        if (GUI.Button(primaryButtonRect, primaryButtonLabel, primaryButtonStyle))
        {
            SubmitEmailForm();
        }

        y += 84f;

        Rect googleButtonRect = new Rect(x + (formWidth - 270f) * 0.5f, y, 270f, 58f);

        if (GUI.Button(googleButtonRect, GUIContent.none, googleButtonStyle))
        {
            StartGoogleSignIn();
        }

        DrawGoogleButtonContent(googleButtonRect);
        GUI.enabled = true;

        y += 76f;

#if UNITY_EDITOR
        if (DrawLink(x, ref y, formWidth, "Play as test user"))
        {
            SubmitEditorTestUser();
        }
#else
        y += 12f;
#endif

        if (!showRegisterForm)
        {
            if (DrawLink(x, ref y, formWidth, "Forgot username?"))
            {
                statusMessage = "La recuperacion de usuario se conectara al backend mas adelante.";
            }

            if (DrawLink(x, ref y, formWidth, "Forgot password?"))
            {
                statusMessage = "La recuperacion de contrasena se conectara al backend mas adelante.";
            }

            if (DrawLink(x, ref y, formWidth, "Create account"))
            {
                showRegisterForm = true;
                statusMessage = string.Empty;
            }
        }
        else if (DrawLink(x, ref y, formWidth, "Already have an account?"))
        {
            showRegisterForm = false;
            statusMessage = string.Empty;
        }

        if (DrawLink(x, ref y, formWidth, showGoogleSettings ? "Hide Google settings" : "Google settings"))
        {
            showGoogleSettings = !showGoogleSettings;
            statusMessage = string.Empty;
        }

        if (showGoogleSettings)
        {
            y += 4f;
            googleClientIdInput = DrawTextInput(x, ref y, formWidth, "OAuth Client ID", googleClientIdInput, false);

            GUI.enabled = !isAuthenticating;
            Rect saveButtonRect = new Rect(x + (formWidth - 160f) * 0.5f, y - 8f, 160f, 42f);

            if (GUI.Button(saveButtonRect, "Save Client ID", primaryButtonStyle))
            {
                SaveGoogleClientId();
            }

            GUI.enabled = true;
            y += 58f;
        }

        DrawStatus(x, y, formWidth);

        GUI.EndScrollView();
    }

    private void DrawSessionHud()
    {
        const float width = 320f;
        Rect hudRect = new Rect(16f, 16f, width, 104f);
        GUI.Box(hudRect, string.Empty);

        GUILayout.BeginArea(new Rect(hudRect.x + 12f, hudRect.y + 10f, hudRect.width - 24f, hudRect.height - 20f));
        GUILayout.Label($"Jugador: {currentSession.displayName}");
        GUILayout.Label($"Proveedor: {currentSession.provider}");

        if (GUILayout.Button("Cerrar sesion"))
        {
            identityService.ClearSession();
            currentSession = null;
            statusMessage = "Sesion cerrada.";

            if (worldBootstrap != null)
            {
                worldBootstrap.ClearWorld();
            }
        }

        GUILayout.EndArea();
    }

    private void SubmitEmailForm()
    {
        if (isAuthenticating)
        {
            return;
        }

        bool success = showRegisterForm
            ? identityService.RegisterWithEmail(email, password, displayName, out AuthSession session, out string error)
            : identityService.LoginWithEmail(email, password, out session, out error);

        if (success)
        {
            EnterWorld(session, showRegisterForm ? "Cuenta creada." : "Sesion iniciada.");
            return;
        }

        statusMessage = error;
    }

#if UNITY_EDITOR
    private void SubmitEditorTestUser()
    {
        if (isAuthenticating)
        {
            return;
        }

        if (identityService.LoginWithEditorTestUser(out AuthSession session, out string error))
        {
            EnterWorld(session, "Sesion de test iniciada.");
            return;
        }

        statusMessage = error;
    }
#endif

    private void SaveGoogleClientId()
    {
        googleClientIdInput = googleClientIdInput.Trim();
        googleOAuthClientId = googleClientIdInput;
        PlayerPrefs.SetString(GoogleClientIdPrefsKey, googleClientIdInput);
        PlayerPrefs.Save();
        statusMessage = string.IsNullOrWhiteSpace(googleClientIdInput)
            ? "Google Client ID borrado."
            : "Google Client ID guardado localmente.";
    }

    private void StartGoogleSignIn()
    {
        if (string.IsNullOrWhiteSpace(googleClientIdInput))
        {
            showGoogleSettings = true;
            statusMessage = "Configura primero el Google OAuth Client ID.";
            return;
        }

        StartCoroutine(GoogleSignInRoutine());
    }

    private System.Collections.IEnumerator GoogleSignInRoutine()
    {
        if (isAuthenticating)
        {
            yield break;
        }

        SaveGoogleClientId();
        isAuthenticating = true;
        statusMessage = "Abriendo Google en el navegador...";

        GoogleOAuthResult result = null;
        yield return googleOAuthService.SignIn(googleClientIdInput, googleOAuthTimeoutSeconds, oauthResult => result = oauthResult);
        isAuthenticating = false;

        if (result == null)
        {
            statusMessage = "Google SSO no devolvio resultado.";
            yield break;
        }

        if (!result.IsSuccess)
        {
            statusMessage = result.Error;
            yield break;
        }

        if (identityService.LoginWithGoogle(result.Profile, result.IdToken, out AuthSession session, out string error))
        {
            EnterWorld(session, "Sesion iniciada con Google.");
            yield break;
        }

        statusMessage = error;
    }

    private void EnterWorld(AuthSession session, string message)
    {
        currentSession = session;
        statusMessage = message;

        if (worldBootstrap != null)
        {
            worldBootstrap.SetAuthenticatedPlayer(session.displayName);
            worldBootstrap.RebuildWorld();
        }
    }

    private float CalculateLoginHeight()
    {
        float height = showRegisterForm ? 670f : 600f;

#if UNITY_EDITOR
        height += 42f;
#endif

        if (showGoogleSettings)
        {
            height += 140f;
        }

        return height;
    }

    private void DrawLogo(float x, float y, float width)
    {
        GUIContent cozyContent = new GUIContent("cozy");
        GUIContent socialContent = new GUIContent("social");
        Vector2 cozySize = logoPrimaryStyle.CalcSize(cozyContent);
        Vector2 socialSize = logoSecondaryStyle.CalcSize(socialContent);
        float logoX = x + (width - cozySize.x - socialSize.x - 2f) * 0.5f;

        GUI.Label(new Rect(logoX, y, cozySize.x, 66f), cozyContent, logoPrimaryStyle);
        GUI.Label(new Rect(logoX + cozySize.x + 2f, y, socialSize.x, 66f), socialContent, logoSecondaryStyle);
    }

    private string DrawTextInput(float x, ref float y, float width, string label, string value, bool passwordField)
    {
        GUI.Label(new Rect(x, y, width, 30f), $"<b>{label}</b>", fieldLabelStyle);
        y += 38f;

        Rect fieldRect = new Rect(x, y, width, 56f);
        DrawFieldBackground(fieldRect);

        value = passwordField
            ? GUI.PasswordField(fieldRect, value, '*', inputStyle)
            : GUI.TextField(fieldRect, value, inputStyle);

        y += 92f;
        return value;
    }

    private bool DrawLink(float x, ref float y, float width, string text)
    {
        Rect linkRect = new Rect(x, y, width, 34f);
        bool clicked = GUI.Button(linkRect, text, linkStyle);
        y += 54f;
        return clicked;
    }

    private void DrawStatus(float x, float y, float width)
    {
        if (string.IsNullOrWhiteSpace(statusMessage))
        {
            return;
        }

        GUI.Label(new Rect(x, y, width, 54f), statusMessage, statusStyle);
    }

    private void DrawGoogleButtonContent(Rect rect)
    {
        string label = isAuthenticating ? "Waiting for Google..." : "Sign in with Google";
        GUI.Label(new Rect(rect.x + 20f, rect.y + 12f, 30f, 34f), "<b>G</b>", googleIconStyle);
        GUI.Label(new Rect(rect.x + 70f, rect.y + 15f, rect.width - 84f, 30f), label, googleTextStyle);
    }

    private void DrawFieldBackground(Rect rect)
    {
        GUI.DrawTexture(rect, fieldBorderTexture);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), fieldFillTexture);
    }

    private void EnsureStyles()
    {
        if (backgroundTexture != null)
        {
            return;
        }

        backgroundTexture = CreateTexture(new Color(0.98f, 0.98f, 0.97f));
        fieldFillTexture = CreateTexture(Color.white);
        fieldBorderTexture = CreateTexture(new Color(0.77f, 0.78f, 0.8f));
        primaryButtonTexture = CreateTexture(new Color(0.19f, 0.49f, 0.82f));
        primaryButtonHoverTexture = CreateTexture(new Color(0.16f, 0.42f, 0.72f));
        googleButtonTexture = CreateTexture(Color.white);
        googleButtonHoverTexture = CreateTexture(new Color(0.96f, 0.97f, 0.98f));

        logoPrimaryStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 48,
            richText = true,
            normal = { textColor = new Color(0.87f, 0.28f, 0.24f) }
        };

        logoSecondaryStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 48,
            richText = true,
            normal = { textColor = new Color(0.18f, 0.49f, 0.82f) }
        };

        fieldLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            richText = true,
            normal = { textColor = new Color(0.13f, 0.14f, 0.18f) }
        };

        inputStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 20,
            padding = new RectOffset(14, 14, 15, 8),
            border = new RectOffset(0, 0, 0, 0)
        };
        inputStyle.normal.background = null;
        inputStyle.focused.background = null;
        inputStyle.hover.background = null;
        inputStyle.active.background = null;

        primaryButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22,
            richText = true,
            normal =
            {
                background = primaryButtonTexture,
                textColor = Color.white
            },
            hover =
            {
                background = primaryButtonHoverTexture,
                textColor = Color.white
            },
            active =
            {
                background = primaryButtonHoverTexture,
                textColor = Color.white
            },
            padding = new RectOffset(8, 8, 8, 8)
        };

        googleButtonStyle = new GUIStyle(GUI.skin.button)
        {
            normal =
            {
                background = googleButtonTexture,
                textColor = new Color(0.34f, 0.36f, 0.4f)
            },
            hover =
            {
                background = googleButtonHoverTexture,
                textColor = new Color(0.22f, 0.24f, 0.28f)
            },
            active =
            {
                background = googleButtonHoverTexture,
                textColor = new Color(0.22f, 0.24f, 0.28f)
            }
        };

        googleIconStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            richText = true,
            normal = { textColor = new Color(0.26f, 0.52f, 0.96f) }
        };

        googleTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            richText = true,
            normal = { textColor = new Color(0.42f, 0.43f, 0.46f) }
        };

        linkStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22,
            normal =
            {
                background = null,
                textColor = new Color(0.12f, 0.45f, 0.85f)
            },
            hover =
            {
                background = null,
                textColor = new Color(0.08f, 0.33f, 0.68f)
            },
            active =
            {
                background = null,
                textColor = new Color(0.08f, 0.33f, 0.68f)
            },
            padding = new RectOffset(0, 0, 0, 0)
        };

        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            wordWrap = true,
            normal = { textColor = new Color(0.47f, 0.22f, 0.17f) }
        };
    }

    private static Texture2D CreateTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
