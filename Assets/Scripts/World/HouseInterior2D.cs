using UnityEngine;

[DisallowMultipleComponent]
public sealed class HouseInterior2D : MonoBehaviour
{
    [Header("Views")]
    [SerializeField] private GameObject exteriorView = null;
    [SerializeField] private GameObject interiorView = null;

    [Header("Spawn Points")]
    [SerializeField] private PlayerSpawnPoint2D interiorSpawn = null;
    [SerializeField] private PlayerSpawnPoint2D exteriorSpawn = null;

    [Header("Camera")]
    [SerializeField] private float exteriorCameraZoom = 7.5f;
    [SerializeField] private float interiorCameraZoom = 4.25f;

    [Header("Interior Bounds")]
    [SerializeField] private bool clampPlayerInside = true;
    [SerializeField] private Vector2 interiorBoundsCenter = Vector2.zero;
    [SerializeField] private Vector2 interiorBoundsSize = new Vector2(7.1f, 5.7f);

    private bool isInside;
    private PlayerMovement2D activePlayer;

    public bool IsInside => isInside;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            ShowInterior(false);
        }
    }

    public void Enter(PlayerMovement2D player)
    {
        if (player == null)
        {
            return;
        }

        isInside = true;
        activePlayer = player;
        ShowInterior(true);
        MovePlayer(player, interiorSpawn);
        ApplyCameraZoom(interiorCameraZoom);
    }

    public void Exit(PlayerMovement2D player)
    {
        if (player == null)
        {
            return;
        }

        isInside = false;
        activePlayer = null;
        ShowInterior(false);
        MovePlayer(player, exteriorSpawn);
        ApplyCameraZoom(exteriorCameraZoom);
    }

    public void ResetToExterior()
    {
        isInside = false;
        activePlayer = null;
        ShowInterior(false);
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || !isInside || !clampPlayerInside || activePlayer == null || interiorView == null)
        {
            return;
        }

        ClampPlayerToInterior(activePlayer);
    }

    private void ShowInterior(bool showInterior)
    {
        if (exteriorView != null)
        {
            exteriorView.SetActive(!showInterior);
        }

        if (interiorView != null)
        {
            interiorView.SetActive(showInterior);
        }
    }

    private static void MovePlayer(PlayerMovement2D player, PlayerSpawnPoint2D spawnPoint)
    {
        if (spawnPoint == null)
        {
            return;
        }

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector2 targetPosition = spawnPoint.transform.position;

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

        if (camera != null)
        {
            camera.orthographicSize = Mathf.Max(1f, zoom);
        }
    }

    private void ClampPlayerToInterior(PlayerMovement2D player)
    {
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector2 currentPosition = body != null ? body.position : (Vector2)player.transform.position;
        Vector2 boundsCenter = interiorView.transform.TransformPoint(interiorBoundsCenter);
        Vector2 worldSize = Vector2.Scale(interiorBoundsSize, Abs(interiorView.transform.lossyScale));
        Vector2 halfSize = worldSize * 0.5f;
        Vector2 clampedPosition = new Vector2(
            Mathf.Clamp(currentPosition.x, boundsCenter.x - halfSize.x, boundsCenter.x + halfSize.x),
            Mathf.Clamp(currentPosition.y, boundsCenter.y - halfSize.y, boundsCenter.y + halfSize.y));

        if ((clampedPosition - currentPosition).sqrMagnitude < 0.0001f)
        {
            return;
        }

        if (body != null)
        {
            body.position = clampedPosition;
            body.linearVelocity = Vector2.zero;
        }
        else
        {
            player.transform.position = clampedPosition;
        }
    }

    private static Vector2 Abs(Vector3 value)
    {
        return new Vector2(Mathf.Abs(value.x), Mathf.Abs(value.y));
    }
}
