using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private bool ensureCameraComponent = true;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float smoothTime = 0.12f;
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minBounds = new Vector2(-18f, -12f);
    [SerializeField] private Vector2 maxBounds = new Vector2(18f, 12f);

    private Vector3 velocity;

    private void OnEnable()
    {
        if (!ensureCameraComponent)
        {
            return;
        }

        Camera camera = GetComponent<Camera>();

        if (camera == null)
        {
            camera = gameObject.AddComponent<Camera>();
        }

        camera.orthographic = true;

        if (GetComponent<AudioListener>() == null)
        {
            gameObject.AddComponent<AudioListener>();
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
        {
            transform.position = GetDesiredPosition();
            velocity = Vector3.zero;
        }
    }

    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = GetDesiredPosition();
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }

    private Vector3 GetDesiredPosition()
    {
        Vector3 desiredPosition = target.position + offset;

        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        }

        return desiredPosition;
    }
}
