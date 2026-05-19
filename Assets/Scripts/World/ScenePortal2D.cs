using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class ScenePortal2D : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "ForestPassage";
    [SerializeField] private string targetSpawnId = "default";
    [SerializeField] private bool requireInteraction = false;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool isUnlocked = true;
    [SerializeField] private string lockedMessage = "The path is blocked.";
    [SerializeField] private Vector2 triggerSize = new Vector2(1.6f, 2.4f);
    [SerializeField] private Vector2 triggerOffset = Vector2.zero;
    [SerializeField] private float reentryCooldownSeconds = 0.45f;

    private PlayerMovement2D playerInRange;
    private PlayerMovement2D cachedPlayer;
    private bool wasPlayerDetected;
    private float lastTransitionTime = -999f;
    private static float nextAllowedTransitionTime;

    private void Awake()
    {
        EnsureTriggerCollider(true);
    }

    private void OnEnable()
    {
        EnsureTriggerCollider(true);
    }

    private void Reset()
    {
        EnsureTriggerCollider(true);
    }

    private void OnValidate()
    {
        EnsureTriggerCollider(false);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        PlayerMovement2D player = ResolvePlayerInRange();

        if (player == null)
        {
            wasPlayerDetected = false;
            return;
        }

        bool playerDetected = IsPlayerInDetectionArea(player);
        playerInRange = playerDetected ? player : null;

        if (!playerDetected)
        {
            wasPlayerDetected = false;
            return;
        }

        if (requireInteraction && Input.GetKeyDown(interactionKey))
        {
            wasPlayerDetected = TryTransition();
            return;
        }

        if (!requireInteraction && (!wasPlayerDetected || Time.time >= nextAllowedTransitionTime))
        {
            wasPlayerDetected = TryTransition();
            return;
        }

        wasPlayerDetected = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement2D player = other.GetComponentInParent<PlayerMovement2D>();

        if (player == null)
        {
            return;
        }

        playerInRange = player;

        if (!requireInteraction)
        {
            TryTransition();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerMovement2D player = other.GetComponentInParent<PlayerMovement2D>();

        if (player != null && player == playerInRange)
        {
            playerInRange = null;
            wasPlayerDetected = false;
        }
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
        boxCollider.size = triggerSize;
        boxCollider.offset = triggerOffset;
        return boxCollider;
    }

    private bool TryTransition()
    {
        if (Time.time - lastTransitionTime < reentryCooldownSeconds || Time.time < nextAllowedTransitionTime)
        {
            return false;
        }

        if (!isUnlocked)
        {
            Debug.Log(lockedMessage);
            nextAllowedTransitionTime = Time.time + reentryCooldownSeconds;
            return false;
        }

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning($"Scene portal '{name}' has no target scene.", this);
            return false;
        }

        CozyWorldBootstrap bootstrap = FindAnyObjectByType<CozyWorldBootstrap>(FindObjectsInactive.Include);

        if (bootstrap == null)
        {
            Debug.LogWarning($"Scene portal '{name}' could not find a CozyWorldBootstrap.", this);
            return false;
        }

        lastTransitionTime = Time.time;
        nextAllowedTransitionTime = Time.time + reentryCooldownSeconds;
        playerInRange = null;
        bootstrap.LoadWorldScene(targetSceneName, targetSpawnId);
        return true;
    }

    private PlayerMovement2D ResolvePlayerInRange()
    {
        if (playerInRange != null)
        {
            return playerInRange;
        }

        if (cachedPlayer == null || !cachedPlayer.isActiveAndEnabled)
        {
            cachedPlayer = FindAnyObjectByType<PlayerMovement2D>(FindObjectsInactive.Exclude);
        }

        return cachedPlayer;
    }

    private bool IsPlayerInDetectionArea(PlayerMovement2D player)
    {
        Vector2 center = transform.TransformPoint(triggerOffset);
        Vector2 size = Vector2.Scale(triggerSize, Abs(transform.lossyScale));
        Bounds detectionBounds = new Bounds(center, new Vector3(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y), 10f));
        Collider2D playerCollider = player.GetComponent<Collider2D>();

        if (playerCollider != null)
        {
            return detectionBounds.Intersects(playerCollider.bounds);
        }

        return detectionBounds.Contains(player.transform.position);
    }

    private static Vector2 Abs(Vector3 value)
    {
        return new Vector2(Mathf.Abs(value.x), Mathf.Abs(value.y));
    }
}
