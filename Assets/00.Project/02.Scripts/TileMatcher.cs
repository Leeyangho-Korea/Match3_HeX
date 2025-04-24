using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;

public class TileMatcher : MonoBehaviour
{
    private GridManager gridManager => GameManager.Instance.gridManager;

    public static Vector2Int[] GetOffsetNeighbors(int col)
    {
        if (col < GameManager.Instance.gridManager.columnHeights.Length / 2)
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
        else if (col > GameManager.Instance.gridManager.columnHeights.Length / 2)
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
            if(centerType == TileType.Egg) continue; // 팽이는 매칭 제외

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

                if (linePos.Count >= 3)
                {
                    foreach (var pos in linePos)
                        matched.Add(grid[pos]);

                  //  Debug.Log($"[매치 성공] {linePos.Count}개: {string.Join(", ", linePos)}");
                }
            }
        }

        // 추가: 다이아몬드 + 주변까지 포함
        matched.UnionWith(FindDiamondMatches(grid));

        return matched.ToList();
    }

    private HashSet<Tile> FindDiamondMatches(Dictionary<Vector2Int, Tile> grid)
    {
        var matched = new HashSet<Tile>();

        foreach (var kv in grid)
        {
            var center = kv.Key;
            var centerType = kv.Value.Type;
            var centerOffsets = GetOffsetNeighbors(center.x);
            if (centerType == TileType.Egg) continue; // 팽이는 매칭 제외
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

    public IEnumerator ClearMatches(List<Tile> matchedTiles)
    {
        GameManager.Instance.BlockInput(true);

        var grid = gridManager.Grid;
        float duration = 0.3f;

        // 장애물에게 hit 전달, 그리고 hit된 애들 목록 받음
        var hitObstacles = GameManager.Instance.obstacleManager.NotifyNearbyMatches(new HashSet<Tile>(matchedTiles));

        // 일반 타일 애니메이션
        foreach (var tile in matchedTiles)
        {
            GameManager.Instance.StartCoroutine(tile.PlayDestroyAnimation(duration));
        }

        // 장애물 애니메이션도 동시에 처리
        foreach (var obsTile in hitObstacles)
        {
            if (obsTile.GetComponent<IObstacle>()?.IsRemovable == true)
            {
                GameManager.Instance.StartCoroutine(obsTile.GetComponent<IObstacle>().PlayDestroyEffect());
            }
        }

        yield return new WaitForSeconds(duration);

        // 일반 타일 제거
        foreach (var tile in matchedTiles)
        {
            if (tile.Type != TileType.Egg)
            {
                grid.Remove(tile.GridPosition);
                tile.gameObject.SetActive(false);
                TilePool.Instance.ReturnTile(tile);
            }
        }

        // 장애물 제거
        foreach (var tile in hitObstacles)
        {
            if (tile.GetComponent<IObstacle>()?.IsRemovable == true)
            {
                grid.Remove(tile.GridPosition);
                tile.gameObject.SetActive(false);
                TilePool.Instance.ReturnTile(tile);
            }
        }

        //UI표현
        int normalMatchCount = matchedTiles.Count(t => t.Type != TileType.Egg);
        GameManager.Instance.AddTile(normalMatchCount);
  
        int removedHearts = hitObstacles.Count(t => t.Type == TileType.Egg
        && t.GetComponent<IObstacle>()?.IsRemovable == true);
        GameManager.Instance.AddHeart(removedHearts);

        GameManager.Instance.BlockInput(false);
    }

    public bool TryFindFirstValidSwap(Dictionary<Vector2Int, Tile> grid, out Tile tileA, out Tile tileB)
    {
        tileA = null;
        tileB = null;

        var positions = grid.Keys.ToList();

        foreach (var pos in positions)
        {
            if (!grid.TryGetValue(pos, out var tA)) continue;

            var neighbors = GetOffsetNeighbors(pos.x);
            foreach (var dir in neighbors)
            {
                Vector2Int nPos = pos + dir;
                if (!grid.TryGetValue(nPos, out var tB)) continue;

                // 스왑 시뮬레이션
                grid[pos] = tB;
                grid[nPos] = tA;
                (tA.GridPosition, tB.GridPosition) = (nPos, pos);

                var matches = FindMatches(grid);

                // 복원
                grid[pos] = tA;
                grid[nPos] = tB;
                (tA.GridPosition, tB.GridPosition) = (pos, nPos);

                if (matches.Count >= 3)
                {
                    tileA = tA;
                    tileB = tB;
                    return true;
                }
            }
        }

        return false;
    }


}
