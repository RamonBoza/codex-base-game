using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSpawnPoint2D : MonoBehaviour
{
    [SerializeField] private string spawnId = "default";

    public string SpawnId => spawnId;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.65f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position + Vector3.down * 0.55f, transform.position + Vector3.up * 0.55f);
        Gizmos.DrawLine(transform.position + Vector3.left * 0.55f, transform.position + Vector3.right * 0.55f);
    }
}
