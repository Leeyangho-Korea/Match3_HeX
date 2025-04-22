using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int[] columnHeights = { 3, 4, 5, 6, 5, 4, 3 };
    public float tileWidth = 1f;   // 타일 가로 간격
    public float tileHeight = 0.866f; // 세로 간격 (√3 / 2 ≈ 0.866)

 
    [ContextMenu("Generate Grid")]
    public void GenerateGridContext() => GenerateGrid();

    private Dictionary<Vector2Int, Tile> grid = new Dictionary<Vector2Int, Tile>();
    public Dictionary<Vector2Int, Tile> Grid => grid;

    public void GenerateGrid()
    {
        ClearGrid();

        for (int x = 0; x < columnHeights.Length; x++)
        {
            int height = columnHeights[x];
           
            for (int y = 0; y < height; y++)
            {
                float yCenterOffset = (height % 2 == 0) ? (height / 2f - 0.5f) : (height / 2f);
                float logicalY = y - yCenterOffset;
                Vector2 worldPos = GetTileWorldPosition(x, logicalY);

                Vector2Int gridPos = new Vector2Int(x, y); //  y 인덱스를 그대로 사용
                Tile tile = TilePool.Instance.GetTile(gridPos);
                tile.transform.position = worldPos;
                tile.transform.SetParent(transform);

                grid.Add(gridPos, tile); // 중복 없음
            }
        }
    }

    public Vector2 GetTileWorldPosition(int x, float y)
    {
        float xOffset = tileWidth * 0.75f;
        float yOffset = tileHeight;

        int columnCount = columnHeights.Length;
        float xCenterOffset = (columnCount - 1) / 2f;

        float xPos = (x - xCenterOffset) * xOffset;
        float yPos = y * yOffset;

        if (x % 2 == 1)
            yPos -= yOffset / 2f;

        return new Vector2(xPos, yPos);
    }

    public void ClearGrid()
    {
        foreach (var tile in grid.Values)
        {
            TilePool.Instance.ReturnTile(tile);
        }
        grid.Clear();
    }

    // Scene View에서 그리드 미리보기
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        if (columnHeights == null || columnHeights.Length == 0)
            return;

        for (int x = 0; x < columnHeights.Length; x++)
        {
            int height = columnHeights[x];
            float yCenterOffset = (height % 2 == 0)
                ? (height / 2f - 0.5f)
                : (height / 2f);

            for (int y = 0; y < height; y++)
            {
                float logicalY = y - yCenterOffset;
                Vector2 pos = GetTileWorldPosition(x, logicalY);
                Gizmos.DrawWireSphere(pos, 0.08f);
            }
        }

    }
    [ContextMenu("Log Grid Types")]
    public void LogGridTypes()
    {
        // 칼럼 순, Y 내림차순으로 정렬
        var entries = grid.OrderBy(kv => kv.Key.x)
                          .ThenByDescending(kv => kv.Key.y);

        foreach (var kv in entries)
        {
            Vector2Int pos = kv.Key;
            TileType type = kv.Value.Type;
            Debug.Log($"[Grid] ({pos.x},{pos.y}) → {type}");
        }
    }
}
