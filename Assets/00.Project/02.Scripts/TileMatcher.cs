using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 헥사 offset(pointy-top) 3매치 매칭기
/// - 직선 매치(3개 이상), 교차(T/X) 매치, 4개 마름모꼴 매치 지원
/// </summary>
public class TileMatcher : MonoBehaviour
{
    private Vector2Int[] GetOffsetNeighbors(int col)
    {
        if (col % 2 == 0)
            return new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(-1, -1) };
        else
            return new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(-1, 1) };
    }

    public List<Tile> FindMatches(Dictionary<Vector2Int, Tile> grid)
    {
        var matched = new HashSet<Tile>();

        // 헥사 축 3방향(동-서, 남북, 대각)
        int[] axisDir = { 0, 1, 4 };
        int[] axisInv = { 2, 3, 5 };

        // 1) 직선 매치 (line matches)
        foreach (var kv in grid)
        {
            var startPos = kv.Key;
            var startTile = kv.Value;
            var type = startTile.Type;
            var neighbors = GetOffsetNeighbors(startPos.x);

            for (int a = 0; a < 3; a++)
            {
                var dir = neighbors[axisDir[a]];
                var inv = neighbors[axisInv[a]];
                var line = new List<Tile> { startTile };

                // forward
                var pos = startPos;
                while (grid.TryGetValue(pos + dir, out var next) && next.Type == type)
                {
                    line.Add(next);
                    pos += dir;
                }

                // backward
                pos = startPos;
                while (grid.TryGetValue(pos + inv, out var prev) && prev.Type == type)
                {
                    line.Add(prev);
                    pos += inv;
                }

                if (line.Count >= 3)
                    foreach (var t in line)
                        matched.Add(t);
            }
        }

        // 2) 4개 마름모꼴 매치 (diamond match)
        foreach (var kv in grid)
        {
            var pos = kv.Key;
            var center = kv.Value;
            var type = center.Type;
            var neighbors = GetOffsetNeighbors(pos.x);

            // 각 축마다 마름모 확인: dir1, dir2 조합
            for (int a = 0; a < 3; a++)
            {
                var d1 = neighbors[axisDir[a]];
                var d2 = neighbors[axisInv[a]];

                // diamond 네 꼭짓점: center, center+d1, center+d2, center+(d1+d2)
                if (grid.TryGetValue(pos + d1, out var t1) && t1.Type == type
                    && grid.TryGetValue(pos + d2, out var t2) && t2.Type == type
                    && grid.TryGetValue(pos + d1 + d2, out var t3) && t3.Type == type)
                {
                    matched.Add(center);
                    matched.Add(t1);
                    matched.Add(t2);
                    matched.Add(t3);
                }
            }
        }

        return matched.ToList();
    }

    public void ClearMatches(List<Tile> matchedTiles)
    {
        var grid = GameManager.Instance.gridManager.Grid;
        foreach (var tile in matchedTiles)
        {
            grid.Remove(tile.GridPosition);
            tile.gameObject.SetActive(false);
            TilePool.Instance.ReturnTile(tile);
        }
    }
}
