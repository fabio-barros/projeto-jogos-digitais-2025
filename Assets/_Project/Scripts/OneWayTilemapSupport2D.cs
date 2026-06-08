using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class OneWayTilemapSupport2D : MonoBehaviour
{
    public float colliderHeight = 0.18f;
    public float surfaceArc = 175f;
    public string supportRootName = "Generated_OneWaySupportColliders";

    private Tilemap tilemap;

    private void Awake()
    {
        RebuildSupportColliders();
    }

    public void RebuildSupportColliders()
    {
        tilemap = GetComponent<Tilemap>();
        if (tilemap == null)
            return;

        DisableTilemapPhysicsShape();
        RemoveExistingSupportRoot();

        GameObject root = new GameObject(supportRootName);
        root.transform.SetParent(transform, false);

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
            root.layer = groundLayer;

        Dictionary<int, List<int>> rows = CollectOccupiedRows();
        foreach (KeyValuePair<int, List<int>> row in rows)
            CreateSupportSpans(root.transform, row.Key, row.Value, groundLayer);
    }

    private Dictionary<int, List<int>> CollectOccupiedRows()
    {
        Dictionary<int, List<int>> rows = new Dictionary<int, List<int>>();
        BoundsInt bounds = tilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(cell))
                continue;

            if (!rows.TryGetValue(cell.y, out List<int> cells))
            {
                cells = new List<int>();
                rows.Add(cell.y, cells);
            }

            cells.Add(cell.x);
        }

        foreach (List<int> cells in rows.Values)
            cells.Sort();

        return rows;
    }

    private void CreateSupportSpans(Transform root, int y, List<int> cells, int groundLayer)
    {
        if (cells == null || cells.Count == 0)
            return;

        int start = cells[0];
        int previous = cells[0];

        for (int i = 1; i <= cells.Count; i++)
        {
            bool endSpan = i == cells.Count || cells[i] != previous + 1;
            if (endSpan)
            {
                CreateSupportCollider(root, start, previous, y, groundLayer);
                if (i < cells.Count)
                    start = cells[i];
            }

            if (i < cells.Count)
                previous = cells[i];
        }
    }

    private void CreateSupportCollider(Transform root, int startX, int endX, int y, int groundLayer)
    {
        Vector3 startCenter = tilemap.GetCellCenterLocal(new Vector3Int(startX, y, 0));
        Vector3 endCenter = tilemap.GetCellCenterLocal(new Vector3Int(endX, y, 0));
        Vector3 cellSize = tilemap.layoutGrid != null ? tilemap.layoutGrid.cellSize : Vector3.one;

        float cellWidth = Mathf.Abs(cellSize.x) > 0f ? Mathf.Abs(cellSize.x) : 1f;
        float cellHeight = Mathf.Abs(cellSize.y) > 0f ? Mathf.Abs(cellSize.y) : 1f;
        float width = ((endX - startX) + 1) * cellWidth;
        float centerX = (startCenter.x + endCenter.x) * 0.5f;
        float topY = startCenter.y + cellHeight * 0.5f;
        float centerY = topY - colliderHeight * 0.5f;

        GameObject support = new GameObject("OneWaySupport_" + y + "_" + startX + "_" + endX);
        support.transform.SetParent(root, false);
        support.transform.localPosition = new Vector3(centerX, centerY, 0f);
        if (groundLayer >= 0)
            support.layer = groundLayer;

        Rigidbody2D rb = support.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        BoxCollider2D box = support.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, colliderHeight);
        box.usedByEffector = true;

        PlatformEffector2D effector = support.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = surfaceArc;
        effector.useSideFriction = false;
        effector.useSideBounce = false;
    }

    private void DisableTilemapPhysicsShape()
    {
        TilemapCollider2D tilemapCollider = GetComponent<TilemapCollider2D>();
        if (tilemapCollider != null)
            tilemapCollider.enabled = false;

        CompositeCollider2D composite = GetComponent<CompositeCollider2D>();
        if (composite != null)
            composite.enabled = false;

        PlatformEffector2D effector = GetComponent<PlatformEffector2D>();
        if (effector != null)
            effector.enabled = false;
    }

    private void RemoveExistingSupportRoot()
    {
        Transform existing = transform.Find(supportRootName);
        if (existing != null)
            Destroy(existing.gameObject);
    }
}
