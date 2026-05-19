using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class DoorTransition2D : MonoBehaviour
{
    [SerializeField] private PlayerSpawnPoint2D destination;
    [SerializeField] private bool requireInteraction = true;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool isUnlocked = true;
    [SerializeField] private string lockedMessage = "Door is locked.";
    [SerializeField] private Vector2 triggerSize = new Vector2(0.8f, 0.5f);
    [SerializeField] private Vector2 triggerOffset = Vector2.zero;
    [SerializeField] private float reentryCooldownSeconds = 0.35f;

    private PlayerMovement2D playerInRange;
    private float lastTransitionTime = -999f;

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

        if (requireInteraction && playerInRange != null && Input.GetKeyDown(interactionKey))
        {
            TryTransition(playerInRange);
        }
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
        }
    }

    private void TryTransition(PlayerMovement2D player)
    {
        if (Time.time - lastTransitionTime < reentryCooldownSeconds)
        {
            return;
        }

        if (!isUnlocked)
        {
            Debug.Log(lockedMessage);
            return;
        }

        if (destination == null)
        {
            Debug.LogWarning($"Door '{name}' has no destination spawn point.", this);
            return;
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
        playerInRange = null;
    }
}
