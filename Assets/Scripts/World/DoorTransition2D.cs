using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class DoorTransition2D : MonoBehaviour
{
    [SerializeField] private PlayerSpawnPoint2D destination;
    [SerializeField] private HouseInterior2D houseInterior = null;
    [SerializeField] private bool exitsHouse = false;
    [SerializeField] private bool requireInteraction = false;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool isUnlocked = true;
    [SerializeField] private string lockedMessage = "Door is locked.";
    [SerializeField] private Vector2 triggerSize = new Vector2(0.8f, 0.5f);
    [SerializeField] private Vector2 triggerOffset = Vector2.zero;
    [SerializeField] private float reentryCooldownSeconds = 0.35f;

    private PlayerMovement2D playerInRange;
    private PlayerMovement2D cachedPlayer;
    private bool wasPlayerDetected;
    private float lastTransitionTime = -999f;
    private static float nextAllowedTransitionTime;

    public PlayerSpawnPoint2D Destination
    {
        get => destination;
        set => destination = value;
    }

    public bool IsUnlocked
    {
        get => isUnlocked;
        set => isUnlocked = value;
    }

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
            wasPlayerDetected = TryTransition(player);
            return;
        }

        if (!requireInteraction && (!wasPlayerDetected || Time.time >= nextAllowedTransitionTime))
        {
            wasPlayerDetected = TryTransition(player);
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
            TryTransition(player);
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

    private bool TryTransition(PlayerMovement2D player)
    {
        if (Time.time - lastTransitionTime < reentryCooldownSeconds)
        {
            return false;
        }

        if (Time.time < nextAllowedTransitionTime)
        {
            return false;
        }

        if (!isUnlocked)
        {
            Debug.Log(lockedMessage);
            nextAllowedTransitionTime = Time.time + reentryCooldownSeconds;
            return false;
        }

        if (houseInterior != null)
        {
            if (exitsHouse)
            {
                houseInterior.Exit(player);
            }
            else
            {
                houseInterior.Enter(player);
            }

            lastTransitionTime = Time.time;
            nextAllowedTransitionTime = Time.time + reentryCooldownSeconds;
            playerInRange = null;
            return true;
        }

        if (destination == null)
        {
            Debug.LogWarning($"Door '{name}' has no destination spawn point.", this);
            return false;
        }

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector2 targetPosition = destination.transform.position;

        if (body != null)
        {
            body.position = targetPosition;
            body.linearVelocity = Vector2.zero;
        }
        else
        {
            player.transform.position = targetPosition;
        }

        lastTransitionTime = Time.time;
        nextAllowedTransitionTime = Time.time + reentryCooldownSeconds;
        playerInRange = null;
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
