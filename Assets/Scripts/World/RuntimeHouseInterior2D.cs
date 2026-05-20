using UnityEngine;

[DisallowMultipleComponent]
public sealed class RuntimeHouseInterior2D : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private Vector2 enterTriggerSize = new Vector2(2.4f, 2.2f);
    [SerializeField] private Vector2 enterTriggerOffset = new Vector2(0f, -1f);
    [SerializeField] private float transitionCooldownSeconds = 0.35f;

    [Header("Interior")]
    [SerializeField] private string interiorName = "House Interior";
    [SerializeField] private Vector2 interiorCenter = new Vector2(0f, -30f);
    [SerializeField] private Vector2 interiorSize = new Vector2(8f, 6f);
    [SerializeField] private Vector2 interiorSpawnOffset = new Vector2(0f, -1.8f);
    [SerializeField] private Vector2 exitTriggerOffset = new Vector2(0f, -2.65f);
    [SerializeField] private Vector2 exitTriggerSize = new Vector2(1.8f, 1f);
    [SerializeField] private bool useDoorPositionForExteriorReturn = true;
    [SerializeField] private Vector2 exteriorReturnOffset = new Vector2(0f, -1.35f);
    [SerializeField] private Vector2 exteriorReturnPosition = Vector2.zero;

    [Header("Camera")]
    [SerializeField] private float exteriorCameraZoom = 7.5f;
    [SerializeField] private float interiorCameraZoom = 4.25f;

    [Header("Palette")]
    [SerializeField] private Color floorColor = new Color32(170, 142, 101, 255);
    [SerializeField] private Color wallColor = new Color32(222, 205, 163, 255);
    [SerializeField] private Color rugColor = new Color32(64, 112, 79, 255);

    private GameObject interiorRoot;
    private PlayerMovement2D cachedPlayer;
    private bool isInside;
    private bool wasEnterDetected;
    private bool wasExitDetected;
    private bool hadCameraBoundsBeforeInterior = true;
    private bool hasStoredCameraBounds;
    private float nextAllowedTransitionTime;

    public bool IsInside => isInside;
    public Vector2 InteriorCenter => interiorCenter;
    public Vector2 InteriorSize => interiorSize;

    private void Awake()
    {
        EnsureTriggerCollider(true);
        EnsureInterior();
    }

    private void OnEnable()
    {
        EnsureTriggerCollider(true);
        EnsureInterior();
    }

    private void Reset()
    {
        exteriorReturnPosition = transform.position;
        EnsureTriggerCollider(true);
    }

    private void OnValidate()
    {
        EnsureTriggerCollider(false);
    }

    private void Start()
    {
        RefreshPlayerState(FindAnyObjectByType<PlayerMovement2D>(FindObjectsInactive.Exclude));
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        PlayerMovement2D player = ResolvePlayer();

        if (player == null)
        {
            wasEnterDetected = false;
            wasExitDetected = false;
            return;
        }

        if (isInside)
        {
            bool exitDetected = IsPlayerInArea(player, InteriorToWorld(exitTriggerOffset), exitTriggerSize);

            if (exitDetected && !wasExitDetected && Time.time >= nextAllowedTransitionTime)
            {
                Exit(player);
                return;
            }

            wasExitDetected = exitDetected;
            ClampPlayer(player);
            return;
        }

        bool enterDetected = IsPlayerInArea(player, (Vector2)transform.TransformPoint(enterTriggerOffset), enterTriggerSize);

        if (enterDetected && !wasEnterDetected && Time.time >= nextAllowedTransitionTime)
        {
            Enter(player);
        }

        wasEnterDetected = enterDetected;
    }

    public void RefreshPlayerState(PlayerMovement2D player)
    {
        EnsureInterior();

        if (player != null && IsPlayerInInteriorBounds(player))
        {
            isInside = true;
            SetInteriorVisible(true);
            ApplyInteriorCameraState(player);
            return;
        }

        isInside = false;
        SetInteriorVisible(false);
        ApplyExteriorCameraState(player);
    }

    private void Enter(PlayerMovement2D player)
    {
        isInside = true;
        wasEnterDetected = false;
        nextAllowedTransitionTime = Time.time + transitionCooldownSeconds;
        EnsureInterior();
        SetInteriorVisible(true);
        MovePlayer(player, InteriorToWorld(interiorSpawnOffset));
        ApplyInteriorCameraState(player);
    }

    private void Exit(PlayerMovement2D player)
    {
        isInside = false;
        wasExitDetected = false;
        nextAllowedTransitionTime = Time.time + transitionCooldownSeconds;
        SetInteriorVisible(false);
        MovePlayer(player, GetExteriorReturnPosition());
        ApplyExteriorCameraState(player);
    }

    private void EnsureInterior()
    {
        if (interiorRoot != null)
        {
            return;
        }

        interiorRoot = new GameObject(string.IsNullOrWhiteSpace(interiorName) ? $"{name} Interior" : interiorName);
        interiorRoot.transform.position = interiorCenter;
        interiorRoot.hideFlags = Application.isPlaying ? HideFlags.None : HideFlags.DontSaveInEditor;

        AddRect("Interior Floor", Vector2.zero, interiorSize, floorColor, -45);
        AddRect("Back Wall", new Vector2(0f, interiorSize.y * 0.42f), new Vector2(interiorSize.x, 0.75f), wallColor, -42);
        AddRect("Interior Rug", new Vector2(0f, -0.1f), new Vector2(interiorSize.x * 0.48f, interiorSize.y * 0.34f), rugColor, -38);
        AddRect("Table Placeholder", new Vector2(0f, 0.35f), new Vector2(1.7f, 0.9f), new Color32(118, 79, 55, 255), -30);
        AddRect("Exit Door Visual", exitTriggerOffset, new Vector2(1.1f, 0.5f), new Color32(116, 80, 55, 255), -28);

        SetInteriorVisible(false);
    }

    private void AddRect(string rectName, Vector2 localPosition, Vector2 size, Color color, int sortingOrder)
    {
        GameObject rect = new GameObject(rectName);
        rect.transform.SetParent(interiorRoot.transform, false);
        rect.transform.localPosition = localPosition;
        SceneRect2D sceneRect = rect.AddComponent<SceneRect2D>();
        sceneRect.Size = size;
        sceneRect.Color = color;
        sceneRect.SortingOrder = sortingOrder;
    }

    private void SetInteriorVisible(bool isVisible)
    {
        if (interiorRoot != null)
        {
            interiorRoot.SetActive(isVisible);
        }
    }

    private PlayerMovement2D ResolvePlayer()
    {
        if (cachedPlayer == null || !cachedPlayer.isActiveAndEnabled)
        {
            cachedPlayer = FindAnyObjectByType<PlayerMovement2D>(FindObjectsInactive.Exclude);
        }

        return cachedPlayer;
    }

    private BoxCollider2D EnsureTriggerCollider(bool addMissingCollider)
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();

        if (boxCollider == null && addMissingCollider)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        if (boxCollider == null)
        {
            return null;
        }

        boxCollider.isTrigger = true;
        boxCollider.size = enterTriggerSize;
        boxCollider.offset = enterTriggerOffset;
        return boxCollider;
    }

    private void ClampPlayer(PlayerMovement2D player)
    {
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector2 currentPosition = body != null ? body.position : (Vector2)player.transform.position;
        Vector2 halfSize = interiorSize * 0.5f;
        Vector2 clampedPosition = new Vector2(
            Mathf.Clamp(currentPosition.x, interiorCenter.x - halfSize.x, interiorCenter.x + halfSize.x),
            Mathf.Clamp(currentPosition.y, interiorCenter.y - halfSize.y, interiorCenter.y + halfSize.y));

        if ((clampedPosition - currentPosition).sqrMagnitude > 0.0001f)
        {
            MovePlayer(player, clampedPosition);
        }
    }

    private bool IsPlayerInInteriorBounds(PlayerMovement2D player)
    {
        return IsPlayerInArea(player, interiorCenter, interiorSize);
    }

    private bool IsPlayerInArea(PlayerMovement2D player, Vector2 center, Vector2 size)
    {
        Bounds bounds = new Bounds(center, new Vector3(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y), 10f));
        Collider2D playerCollider = player.GetComponent<Collider2D>();

        if (playerCollider != null)
        {
            return bounds.Intersects(playerCollider.bounds);
        }

        return bounds.Contains(player.transform.position);
    }

    private Vector2 InteriorToWorld(Vector2 localPoint)
    {
        return interiorCenter + localPoint;
    }

    public Vector2 GetInteriorWorldPoint(Vector2 localPoint)
    {
        return InteriorToWorld(localPoint);
    }

    private Vector2 GetExteriorReturnPosition()
    {
        if (useDoorPositionForExteriorReturn)
        {
            return (Vector2)transform.position + exteriorReturnOffset;
        }

        return exteriorReturnPosition;
    }

    private void ApplyInteriorCameraState(PlayerMovement2D player)
    {
        CameraFollow2D cameraFollow = ResolveCameraFollow();

        if (cameraFollow != null)
        {
            if (!hasStoredCameraBounds)
            {
                hadCameraBoundsBeforeInterior = cameraFollow.UseBounds;
                hasStoredCameraBounds = true;
            }

            cameraFollow.SetBoundsEnabled(false);
        }

        ApplyCameraZoom(interiorCameraZoom);
        SnapCameraToPlayer(player);
    }

    private void ApplyExteriorCameraState(PlayerMovement2D player)
    {
        CameraFollow2D cameraFollow = ResolveCameraFollow();

        if (cameraFollow != null && hasStoredCameraBounds)
        {
            cameraFollow.SetBoundsEnabled(hadCameraBoundsBeforeInterior);
            hasStoredCameraBounds = false;
        }

        ApplyCameraZoom(exteriorCameraZoom);
        SnapCameraToPlayer(player);
    }

    private static void MovePlayer(PlayerMovement2D player, Vector2 targetPosition)
    {
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.position = targetPosition;
            body.linearVelocity = Vector2.zero;
        }
        else
        {
            player.transform.position = targetPosition;
        }
    }

    private static void ApplyCameraZoom(float zoom)
    {
        Camera camera = Camera.main;

        if (camera == null)
        {
            camera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
        }

        if (camera != null)
        {
            camera.orthographicSize = Mathf.Max(1f, zoom);
        }
    }

    private static void SnapCameraToPlayer(PlayerMovement2D player)
    {
        CameraFollow2D cameraFollow = ResolveCameraFollow();

        if (cameraFollow != null && player != null)
        {
            cameraFollow.SetTarget(player.transform);
            cameraFollow.SnapToTarget();
            return;
        }

        Camera camera = Camera.main;

        if (camera == null)
        {
            camera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
        }

        if (camera != null && player != null)
        {
            Vector3 cameraPosition = camera.transform.position;
            camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, cameraPosition.z);
        }
    }

    private static CameraFollow2D ResolveCameraFollow()
    {
        return FindAnyObjectByType<CameraFollow2D>(FindObjectsInactive.Exclude);
    }
}
