using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class TileMatcher : MonoBehaviour
{
    private GridManager gridManager => GameManager.Instance.gridManager;

    private Vector2Int[] GetOffsetNeighbors(int col)
    {
        if (col < gridManager.columnHeights.Length / 2)
        {
            return new[] {
                new Vector2Int( 0, 1),     // 0 ↑
                new Vector2Int( 0, -1),    // 1 ↓
                new Vector2Int( 1, 1),     // 2 ↗
                new Vector2Int(-1, -1),    // 3 ↙
                new Vector2Int(-1, 0),     // 4 ↖
                new Vector2Int( 1, 0)      // 5 ↘
            };
        }
        else if (col > gridManager.columnHeights.Length / 2)
        {
            return new[] {
                new Vector2Int( 0, 1),     // 0 ↑
                new Vector2Int( 0, -1),    // 1 ↓
                new Vector2Int( 1, 0),     // 2 ↗
                new Vector2Int(-1, 0),     // 3 ↙
                new Vector2Int(-1, 1),     // 4 ↖
                new Vector2Int( 1, -1)     // 5 ↘
            };
        }
        else
        {
            return new[] {
                new Vector2Int( 0, 1),     // 0 ↑
                new Vector2Int( 0, 1),     // 1 ↓ (중앙은 상하 동일)
                new Vector2Int( 1, 0),     // 2 ↗
                new Vector2Int(-1, -1),    // 3 ↙
                new Vector2Int(-1, 0),     // 4 ↖
                new Vector2Int( 1, -1)     // 5 ↘
            };
        }
    }

    public List<Tile> FindMatches(Dictionary<Vector2Int, Tile> grid)
    {
        var matched = new HashSet<Tile>();

        var dirPairs = new[] {
            (0, 1),  // ↑ ↓
            (2, 3),  // ↗ ↙
            (4, 5)   // ↖ ↘
        };

        foreach (var kv in grid)
        {
            var centerPos = kv.Key;
            var centerType = kv.Value.Type;
            var centerOffsets = GetOffsetNeighbors(centerPos.x);

            foreach (var (i, j) in dirPairs)
            {
                var linePos = new List<Vector2Int> { centerPos };

                // i 방향
                var cur = centerPos + centerOffsets[i];
                while (grid.TryGetValue(cur, out var t1) && t1.Type == centerType)
                {
                    if (!linePos.Contains(cur)) linePos.Add(cur);
                    cur += GetOffsetNeighbors(cur.x)[i];
                }

                // j 방향
                cur = centerPos + centerOffsets[j];
                while (grid.TryGetValue(cur, out var t2) && t2.Type == centerType)
                {
                    if (!linePos.Contains(cur)) linePos.Add(cur);
                    cur += GetOffsetNeighbors(cur.x)[j];
                }

                if (linePos.Count >= 3 && IsLinear(centerPos, linePos))
                {
                    foreach (var pos in linePos)
                        matched.Add(grid[pos]);

                    Debug.Log($"[매치 성공] {linePos.Count}개: {string.Join(", ", linePos)}");
                }
            }
        }

        // 추가: 다이아몬드 + 주변까지 포함
        matched.UnionWith(FindDiamondMatches(grid));

        return matched.ToList();
    }

    private bool IsLinear(Vector2Int center, List<Vector2Int> line)
    {
        return true;
        if (line.Count < 3) return false;

        var others = line.Where(p => p != center).ToList();
        var baseVec = others[0] - center;

        for (int i = 1; i < others.Count; i++)
        {
            var delta = others[i] - center;
            int cross = baseVec.x * delta.y - baseVec.y * delta.x;
            if (cross != 0)
                return false;
        }

        return true;
    }

    private HashSet<Tile> FindDiamondMatches(Dictionary<Vector2Int, Tile> grid)
    {
        var matched = new HashSet<Tile>();

        foreach (var kv in grid)
        {
            var center = kv.Key;
            var centerType = kv.Value.Type;
            var centerOffsets = GetOffsetNeighbors(center.x);

            // 네 방향 조합 조건
            var combos = new[] {
                (0, 2, 5),
                (0, 4, 3),
                (1, 3, 4),
                (1, 5, 2),
                (1, 3, 5),
                (0, 2, 4)
            };

            foreach (var (i1, i2, i3) in combos)
            {
                var p1 = center + centerOffsets[i1];
                var p2 = center + centerOffsets[i2];
                var p3 = center + centerOffsets[i3];

                if (grid.TryGetValue(p1, out var t1) && t1.Type == centerType &&
                    grid.TryGetValue(p2, out var t2) && t2.Type == centerType &&
                    grid.TryGetValue(p3, out var t3) && t3.Type == centerType)
                {
                    matched.Add(grid[center]);
                    matched.Add(t1);
                    matched.Add(t2);
                    matched.Add(t3);

                    // 주변까지 확장
                    foreach (var basePos in new[] { center, p1, p2, p3 })
                    {
                        var offs = GetOffsetNeighbors(basePos.x);
                        foreach (var off in offs)
                        {
                            var adj = basePos + off;
                            if (grid.TryGetValue(adj, out var extra) && extra.Type == centerType)
                                matched.Add(extra);
                        }
                    }

                    break; // 한 구조 매칭되면 반복 종료
                }
            }
        }

        return matched;
    }

    public void ClearMatches(List<Tile> matchedTiles)
    {
        var grid = gridManager.Grid;

        foreach (var tile in matchedTiles)
        {
            Debug.Log($"{tile.GridPosition} 제거");
            grid.Remove(tile.GridPosition);
            tile.gameObject.SetActive(false);
            TilePool.Instance.ReturnTile(tile);
        }
    }
}
