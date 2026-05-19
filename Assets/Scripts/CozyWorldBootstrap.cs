using UnityEngine;

[DisallowMultipleComponent]
public sealed class CozyWorldBootstrap : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameObject worldRoot;
    [SerializeField] private PlayerMovement2D player;
    [SerializeField] private PlayerSpawnPoint2D defaultSpawn;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private CameraFollow2D cameraFollow;

    [Header("Startup")]
    [SerializeField] private bool hideWorldUntilAuthenticated = true;
    [SerializeField] private bool resetPlayerOnEnterWorld = true;
    [SerializeField] private float playerMoveSpeed = 4.25f;
    [SerializeField] private float cameraZoom = 7.5f;

    private string authenticatedPlayerName = "Player";

    private void Awake()
    {
        ResolveMissingReferences();
        ConfigureSceneReferences();

        if (Application.isPlaying && hideWorldUntilAuthenticated)
        {
            SetWorldActive(false);
        }
    }

    [ContextMenu("Enter World From Scene")]
    public void RebuildWorld()
    {
        ResolveMissingReferences();
        ConfigureSceneReferences();
        SetWorldActive(true);

        if (resetPlayerOnEnterWorld)
        {
            MovePlayerToSpawn(defaultSpawn);
        }
    }

    public void SetAuthenticatedPlayer(string playerName)
    {
        authenticatedPlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();

        if (player != null)
        {
            player.gameObject.name = $"Player - {authenticatedPlayerName}";
        }
    }

    public void ClearWorld()
    {
        if (hideWorldUntilAuthenticated)
        {
            SetWorldActive(false);
        }
    }

    private void ResolveMissingReferences()
    {
        if (worldRoot == null)
        {
            GameObject foundWorldRoot = GameObject.Find("Editable World");
            worldRoot = foundWorldRoot != null ? foundWorldRoot : worldRoot;
        }

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerMovement2D>(FindObjectsInactive.Include);
        }

        if (defaultSpawn == null)
        {
            defaultSpawn = FindAnyObjectByType<PlayerSpawnPoint2D>(FindObjectsInactive.Include);
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (worldCamera == null)
        {
            GameObject cameraObject = GameObject.FindWithTag("MainCamera");

            if (cameraObject == null)
            {
                cameraObject = GameObject.Find("Main Camera");
            }

            if (cameraObject != null)
            {
                worldCamera = GetOrAddComponent<Camera>(cameraObject);
            }
        }

        if (cameraFollow == null && worldCamera != null)
        {
            cameraFollow = GetOrAddComponent<CameraFollow2D>(worldCamera.gameObject);
        }
    }

    private void ConfigureSceneReferences()
    {
        if (player != null)
        {
            player.Configure(playerMoveSpeed);
        }

        if (worldCamera != null)
        {
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = cameraZoom;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = new Color32(128, 176, 197, 255);

            if (worldCamera.GetComponent<AudioListener>() == null)
            {
                worldCamera.gameObject.AddComponent<AudioListener>();
            }
        }

        if (cameraFollow != null && player != null)
        {
            cameraFollow.SetTarget(player.transform);
        }
    }

    private void SetWorldActive(bool isActive)
    {
        if (worldRoot != null)
        {
            worldRoot.SetActive(isActive);
        }

        if (player != null)
        {
            player.gameObject.SetActive(isActive);
        }
    }

    private void MovePlayerToSpawn(PlayerSpawnPoint2D spawnPoint)
    {
        if (player == null || spawnPoint == null)
        {
            return;
        }

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector2 spawnPosition = spawnPoint.transform.position;

        if (body != null)
        {
            body.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
        }
        else
        {
            player.transform.position = spawnPosition;
        }

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(player.transform);
        }
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
