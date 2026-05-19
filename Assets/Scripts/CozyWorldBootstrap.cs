using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class CozyWorldBootstrap : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameObject worldRoot;
    [SerializeField] private PlayerMovement2D player;
    [SerializeField] private PlayerSpawnPoint2D defaultSpawn;
    [SerializeField] private HouseInterior2D startingHouse;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private CameraFollow2D cameraFollow;

    [Header("Startup")]
    [SerializeField] private bool hideWorldUntilAuthenticated = true;
    [SerializeField] private bool resetPlayerOnEnterWorld = true;
    [SerializeField] private bool startInsideHouse = true;
    [SerializeField] private bool persistAcrossWorldScenes = true;
    [SerializeField] private string initialWorldSceneName = "PlayerHomestead";
    [SerializeField] private string initialWorldSpawnId = "homestead-start";
    [SerializeField] private float playerMoveSpeed = 4.25f;
    [SerializeField] private float cameraZoom = 7.5f;
    [SerializeField] private Vector2 cameraBoundsPadding = new Vector2(4f, 3f);

    private string authenticatedPlayerName = "Player";
    private bool isLoadingWorldScene;

    private void Awake()
    {
        ResolveMissingReferences();
        ConfigureSceneReferences();
        ConfigurePersistentRuntime();

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
            MovePlayerToStart();
        }
    }

    public void LoadWorldScene(string sceneName, string spawnId)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (isLoadingWorldScene)
        {
            return;
        }

        StartCoroutine(LoadWorldSceneRoutine(sceneName, spawnId));
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

        if (startingHouse == null)
        {
            HouseInterior2D[] houses = FindObjectsByType<HouseInterior2D>(FindObjectsInactive.Include);

            foreach (HouseInterior2D house in houses)
            {
                if (house.name.Contains("Player"))
                {
                    startingHouse = house;
                    break;
                }
            }

            if (startingHouse == null && houses.Length > 0)
            {
                startingHouse = houses[0];
            }
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
        ResetHouseInteriors();

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

        ApplySceneCameraBounds();
    }

    private void ConfigurePersistentRuntime()
    {
        if (!Application.isPlaying || !persistAcrossWorldScenes)
        {
            return;
        }

        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);

        if (player != null)
        {
            player.transform.SetParent(null, true);
            DontDestroyOnLoad(player.gameObject);
        }

        if (worldCamera != null)
        {
            worldCamera.transform.SetParent(null, true);
            DontDestroyOnLoad(worldCamera.gameObject);
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

    private void MovePlayerToStart()
    {
        if (player == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(initialWorldSceneName))
        {
            LoadWorldScene(initialWorldSceneName, initialWorldSpawnId);
            return;
        }

        if (startInsideHouse && startingHouse != null)
        {
            startingHouse.Enter(player);
            return;
        }

        MovePlayerToSpawn(defaultSpawn);
    }

    private IEnumerator LoadWorldSceneRoutine(string sceneName, string spawnId)
    {
        isLoadingWorldScene = true;
        SetWorldActive(false);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        if (loadOperation == null)
        {
            Debug.LogWarning($"Could not load world scene '{sceneName}'.");
            isLoadingWorldScene = false;
            SetWorldActive(true);
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        worldRoot = null;
        defaultSpawn = null;
        startingHouse = null;
        ResolveMissingReferences();
        ConfigureSceneReferences();
        SetWorldActive(true);
        MovePlayerToSpawn(FindSpawnPoint(spawnId));
        RefreshRuntimeHouseInteriors();
        isLoadingWorldScene = false;
    }

    private PlayerSpawnPoint2D FindSpawnPoint(string spawnId)
    {
        PlayerSpawnPoint2D[] spawnPoints = FindObjectsByType<PlayerSpawnPoint2D>(FindObjectsInactive.Include);

        if (!string.IsNullOrWhiteSpace(spawnId))
        {
            foreach (PlayerSpawnPoint2D spawnPoint in spawnPoints)
            {
                if (spawnPoint.SpawnId == spawnId)
                {
                    return spawnPoint;
                }
            }
        }

        return spawnPoints.Length > 0 ? spawnPoints[0] : null;
    }

    private void ResetHouseInteriors()
    {
        HouseInterior2D[] houseInteriors = worldRoot != null
            ? worldRoot.GetComponentsInChildren<HouseInterior2D>(true)
            : FindObjectsByType<HouseInterior2D>(FindObjectsInactive.Include);

        foreach (HouseInterior2D houseInterior in houseInteriors)
        {
            houseInterior.ResetToExterior();
        }
    }

    private void ApplySceneCameraBounds()
    {
        if (cameraFollow == null)
        {
            return;
        }

        WorldMapRegion2D[] regions = worldRoot != null
            ? worldRoot.GetComponentsInChildren<WorldMapRegion2D>(true)
            : FindObjectsByType<WorldMapRegion2D>(FindObjectsInactive.Include);

        if (regions.Length == 0)
        {
            cameraFollow.SetBoundsEnabled(false);
            cameraFollow.SnapToTarget();
            return;
        }

        Vector2 min = Vector2.zero;
        Vector2 max = Vector2.zero;
        bool hasBounds = false;

        foreach (WorldMapRegion2D region in regions)
        {
            Vector2 center = region.transform.position;
            Vector2 halfSize = region.RegionSize * 0.5f;
            Vector2 regionMin = center - halfSize;
            Vector2 regionMax = center + halfSize;

            if (!hasBounds)
            {
                min = regionMin;
                max = regionMax;
                hasBounds = true;
                continue;
            }

            min = Vector2.Min(min, regionMin);
            max = Vector2.Max(max, regionMax);
        }

        Vector2 padding = new Vector2(Mathf.Max(0f, cameraBoundsPadding.x), Mathf.Max(0f, cameraBoundsPadding.y));
        cameraFollow.SetBounds(min - padding, max + padding);
        cameraFollow.SnapToTarget();
    }

    private void RefreshRuntimeHouseInteriors()
    {
        RuntimeHouseInterior2D[] runtimeHouseInteriors = FindObjectsByType<RuntimeHouseInterior2D>(FindObjectsInactive.Include);

        foreach (RuntimeHouseInterior2D runtimeHouseInterior in runtimeHouseInteriors)
        {
            runtimeHouseInterior.RefreshPlayerState(player);
        }
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
