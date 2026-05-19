using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldMapRegion2D : MonoBehaviour
{
    [SerializeField] private string regionId = "region";
    [SerializeField] private string displayName = "Region";
    [SerializeField] private Vector2 regionSize = new Vector2(10f, 8f);
    [SerializeField] private Color gizmoColor = new Color(0.35f, 0.8f, 0.55f, 0.22f);
    [SerializeField] private string[] connectedRegionIds = System.Array.Empty<string>();

    public string RegionId => regionId;
    public string DisplayName => displayName;
    public Vector2 RegionSize => regionSize;
    public string[] ConnectedRegionIds => connectedRegionIds;

    private void OnDrawGizmos()
    {
        Vector3 size = new Vector3(Mathf.Max(0.1f, regionSize.x), Mathf.Max(0.1f, regionSize.y), 0.1f);
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position, size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.95f);
        Gizmos.DrawWireCube(transform.position, size);
    }
}
