// TileMatcher.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileMatcher : MonoBehaviour
{
    private GridManager _gm => GameManager.Instance.gridManager;

    /// <summary>
    /// odd‑q offset(pointy‑top) 헥사 그리드 6방향
    /// </summary>
    private Vector2Int[] GetOffsetNeighbors(int col)
    {
        if ((col & 1) == 1) // odd column
            return new[] {
                new Vector2Int( 1,  0), new Vector2Int( 0,  1),
                new Vector2Int(-1,  0), new Vector2Int( 0, -1),
                new Vector2Int( 1, -1), new Vector2Int(-1, -1)
            };
        else // even column
            return new[] {
                new Vector2Int( 1,  0), new Vector2Int( 0,  1),
                new Vector2Int(-1,  0), new Vector2Int( 0, -1),
                new Vector2Int( 1,  1), new Vector2Int(-1,  1)
            };
    }

    public List<Tile> FindMatches(Dictionary<Vector2Int, Tile> grid)
    {
        var matched = new HashSet<Tile>();

        // 1) 3연속 직선 매치 (3방향: 0↔2, 1↔3, 4↔5 인덱스 페어)
        foreach (var kv in grid)
        {
            var centerPos = kv.Key;
            var centerType = kv.Value.Type;
            var offs = GetOffsetNeighbors(centerPos.x);

            // 인접 방향 쌍 페어: (0,2), (1,3), (4,5)
            var dirPairs = new[] { (0, 2), (1, 3), (4, 5) };
            foreach (var (i, j) in dirPairs)
            {
                var line = new List<Tile> { grid[centerPos] };

                // + 방향으로 이어붙이기
                var cur = centerPos;
                while (true)
                {
                    var next = cur + offs[i];
                    if (!grid.TryGetValue(next, out var t) || t.Type != centerType) break;
                    line.Add(t);
                    cur = next;
                }

                // – 방향으로 이어붙이기
                cur = centerPos;
                while (true)
                {
                    var next = cur + offs[j];
                    if (!grid.TryGetValue(next, out var t) || t.Type != centerType) break;
                    line.Add(t);
                    cur = next;
                }

                if (line.Count >= 3)
                    foreach (var t in line) matched.Add(t);
            }
        }

        // 2) 다이아몬드 매치: 서로 다른 2방향 조합(offA, offB, offA+offB)
        foreach (var kv in grid)
        {
            var centerPos = kv.Key;
            var centerType = kv.Value.Type;
            var offs = GetOffsetNeighbors(centerPos.x);

            // 가능한 오프셋 조합 (이웃 델타 인덱스 중 2개)
            var combos = new[] {
                (0,1), (0,3), (1,2), (2,5), (3,4), (4,5),
                (1,4), (2,3) // 대각선끼리도 검사
            };

            foreach (var (i, j) in combos)
            {
                var a = offs[i];
                var b = offs[j];
                var pA = centerPos + a;
                var pB = centerPos + b;
                var pAB = centerPos + a + b;

                if (grid.TryGetValue(pA, out var tA) && tA.Type == centerType
                 && grid.TryGetValue(pB, out var tB) && tB.Type == centerType
                 && grid.TryGetValue(pAB, out var tAB) && tAB.Type == centerType)
                {
                    matched.Add(grid[centerPos]);
                    matched.Add(tA);
                    matched.Add(tB);
                    matched.Add(tAB);
                }
            }
        }

        return matched.ToList();
    }

    public void ClearMatches(List<Tile> matchedTiles)
    {
        var grid = _gm.Grid;
        foreach (var tile in matchedTiles)
        {
            grid.Remove(tile.GridPosition);
            tile.gameObject.SetActive(false);
            TilePool.Instance.ReturnTile(tile);
        }
    }
}
