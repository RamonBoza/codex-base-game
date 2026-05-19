using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4.25f;
    [SerializeField] private bool allowDiagonalMovement = true;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private Vector2 lastFacingDirection = Vector2.down;

    public Vector2 LastFacingDirection => lastFacingDirection;

    public void Configure(float speed)
    {
        moveSpeed = speed;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }

        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (GetComponent<Collider2D>() == null)
        {
            CapsuleCollider2D capsuleCollider = gameObject.AddComponent<CapsuleCollider2D>();
            capsuleCollider.direction = CapsuleDirection2D.Vertical;
            capsuleCollider.size = new Vector2(0.58f, 0.9f);
            capsuleCollider.offset = new Vector2(0f, 0.45f);
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        moveInput = ReadMovementInput();

        if (moveInput.sqrMagnitude > 0.001f)
        {
            lastFacingDirection = moveInput.normalized;

            if (Mathf.Abs(moveInput.x) > 0.01f && spriteRenderer != null)
            {
                spriteRenderer.flipX = moveInput.x < 0f;
            }
        }
    }

    private void FixedUpdate()
    {
        Vector2 nextPosition = body.position + moveInput * moveSpeed * Time.fixedDeltaTime;
        body.MovePosition(nextPosition);
    }

    private Vector2 ReadMovementInput()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            x -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            x += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            y -= 1f;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            y += 1f;
        }

        Vector2 input = new Vector2(x, y);

        if (allowDiagonalMovement && input.sqrMagnitude > 1f)
        {
            return input.normalized;
        }

        if (!allowDiagonalMovement && Mathf.Abs(input.x) > 0.01f)
        {
            input.y = 0f;
        }

        return input;
    }
}
