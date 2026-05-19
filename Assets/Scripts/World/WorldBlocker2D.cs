using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class WorldBlocker2D : MonoBehaviour
{
    [SerializeField] private bool blocksMovement = true;
    [SerializeField] private string blockerId = "blocker";

    public bool BlocksMovement => blocksMovement;
    public string BlockerId => blockerId;

    private void Awake()
    {
        ApplyColliderMode(true);
    }

    private void OnEnable()
    {
        ApplyColliderMode(true);
    }

    private void Reset()
    {
        ApplyColliderMode(true);
    }

    private void OnValidate()
    {
        ApplyColliderMode(false);
    }

    private void ApplyColliderMode(bool addMissingCollider)
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();

        if (boxCollider == null && addMissingCollider)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        if (boxCollider == null)
        {
            return;
        }

        boxCollider.isTrigger = !blocksMovement;
    }
}
