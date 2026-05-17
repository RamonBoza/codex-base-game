using UnityEngine;

[DisallowMultipleComponent]
public sealed class LoginScreenController : MonoBehaviour
{
    [SerializeField] private CozyWorldBootstrap worldBootstrap;
    [SerializeField] private bool restoreSessionOnStart = true;

    private LocalIdentityService identityService;
    private AuthSession currentSession;
    private bool showRegisterForm = true;
    private string email = "player@cozy.local";
    private string password = "cozy123";
    private string displayName = "Cozy Player";
    private string statusMessage = "Inicia sesion o crea una cuenta para entrar al mundo.";
    private Vector2 scrollPosition;

    private bool IsAuthenticated => currentSession != null && currentSession.IsValid;

    private void Awake()
    {
        identityService = new LocalIdentityService();

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

        DrawLoginPanel();
    }

    private void DrawLoginPanel()
    {
        float panelWidth = Mathf.Min(460f, Screen.width - 32f);
        float panelHeight = Mathf.Min(560f, Screen.height - 32f);
        Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);

        GUI.Box(panelRect, string.Empty);

        GUILayout.BeginArea(new Rect(panelRect.x + 24f, panelRect.y + 20f, panelRect.width - 48f, panelRect.height - 40f));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

        GUILayout.Label("Cozy Social Game");
        GUILayout.Space(8f);
        GUILayout.Label("v0.2.0 - Identity Foundations");
        GUILayout.Space(16f);

        GUILayout.Label(showRegisterForm ? "Modo actual: registro" : "Modo actual: login");

        if (GUILayout.Button(showRegisterForm ? "Ya tengo cuenta" : "Crear una cuenta nueva"))
        {
            showRegisterForm = !showRegisterForm;
        }

        GUILayout.Space(8f);

        GUILayout.Label("Email");
        email = GUILayout.TextField(email, 80);

        GUILayout.Label("Contrasena");
        password = GUILayout.PasswordField(password, '*', 80);

        if (showRegisterForm)
        {
            GUILayout.Label("Nombre visible");
            displayName = GUILayout.TextField(displayName, 40);
        }

        GUILayout.Space(12f);

        if (GUILayout.Button(showRegisterForm ? "Crear cuenta" : "Entrar"))
        {
            SubmitEmailForm();
        }

        GUILayout.Space(10f);
        GUILayout.Label("Proveedores SSO de desarrollo");

        if (GUILayout.Button("Continuar con Google"))
        {
            SubmitDevelopmentProvider("google-dev");
        }

        if (GUILayout.Button("Continuar con Apple"))
        {
            SubmitDevelopmentProvider("apple-dev");
        }

        GUILayout.Space(12f);
        GUILayout.Label(statusMessage);

        GUILayout.Space(16f);
        GUILayout.Label("Nota: Google/Apple usan proveedores locales de desarrollo hasta conectar credenciales reales y backend.");

        GUILayout.EndScrollView();
        GUILayout.EndArea();
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

    private void SubmitDevelopmentProvider(string provider)
    {
        if (identityService.LoginWithDevelopmentProvider(provider, out AuthSession session, out string error))
        {
            EnterWorld(session, "Sesion SSO de desarrollo iniciada.");
            return;
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
}
