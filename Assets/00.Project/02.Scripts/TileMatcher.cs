using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileMatcher : MonoBehaviour
{
    private GridManager gridManager => GameManager.Instance.gridManager;

    public static Vector2Int[] GetOffsetNeighbors(int col)
    {
        int mid = GameManager.Instance.gridManager.columnHeights.Length / 2;
        if (col < mid)
        {
            return new[] {
                new Vector2Int( 0, 1),   // 상
                new Vector2Int( 0, -1),  // 하
                new Vector2Int( 1, 1),  //우상
                new Vector2Int(-1, -1), //좌하
                new Vector2Int(-1, 0), // 좌상
                new Vector2Int( 1, 0) // 우하
            };
        }
        else if (col > mid)
        {
            return new[] {
                new Vector2Int( 0, 1),
                new Vector2Int( 0, -1),
                new Vector2Int( 1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(-1, 1),
                new Vector2Int( 1, -1)
            };
        }
        else
        {
            return new[] {
                new Vector2Int( 0, 1),
                new Vector2Int( 0, -1), 
                new Vector2Int( 1, 0),
                new Vector2Int(-1, -1),
                new Vector2Int(-1, 0),
                new Vector2Int( 1, -1)
            };
        }
    }

    private bool IsMatchableTile(Tile tile, TileType type)
    {
        return tile.Type == type && tile.Type != TileType.Egg;
    }

    public List<Tile> FindMatches(Dictionary<Vector2Int, Tile> grid)
    {
        var matched = new HashSet<Tile>();
        var dirPairs = new[] { (0, 1), (2, 3), (4, 5) };

        foreach (var kv in grid)
        {
            var centerPos = kv.Key;
            var centerTile = kv.Value;
            var centerType = centerTile.Type;
            if (centerType == TileType.Egg) continue;

            var centerOffsets = GetOffsetNeighbors(centerPos.x);

            foreach (var (i, j) in dirPairs)
            {
                var linePos = new List<Vector2Int> { centerPos };

                // i 방향
                var cur = centerPos + centerOffsets[i];
                while (grid.TryGetValue(cur, out var t1) && IsMatchableTile(t1, centerType))
                {
                    linePos.Add(cur);
                    cur += GetOffsetNeighbors(cur.x)[i];
                }

                // j 방향
                cur = centerPos + centerOffsets[j];
                while (grid.TryGetValue(cur, out var t2) && IsMatchableTile(t2, centerType))
                {
                    linePos.Add(cur);
                    cur += GetOffsetNeighbors(cur.x)[j];
                }

                if (linePos.Count >= 3)
                {
                    foreach (var pos in linePos)
                        matched.Add(grid[pos]);
                }
            }
        }

        matched.UnionWith(FindDiamondMatches(grid));
        return matched.ToList();
    }

    private HashSet<Tile> FindDiamondMatches(Dictionary<Vector2Int, Tile> grid)
    {
        var matched = new HashSet<Tile>();

        foreach (var kv in grid)
        {
            var center = kv.Key;
            var centerTile = kv.Value;
            var centerType = centerTile.Type;
            if (centerType == TileType.Egg) continue;

            var centerOffsets = GetOffsetNeighbors(center.x);

            var combos = new[] {
                (0, 2, 5), (0, 4, 3),
                (1, 3, 4), (1, 5, 2),
                (1, 3, 5), (0, 2, 4)
            };

            foreach (var (i1, i2, i3) in combos)
            {
                var p1 = center + centerOffsets[i1];
                var p2 = center + centerOffsets[i2];
                var p3 = center + centerOffsets[i3];

                if (
                    grid.TryGetValue(p1, out var t1) && IsMatchableTile(t1, centerType) &&
                    grid.TryGetValue(p2, out var t2) && IsMatchableTile(t2, centerType) &&
                    grid.TryGetValue(p3, out var t3) && IsMatchableTile(t3, centerType)
                )
                {
                    matched.Add(centerTile);
                    matched.Add(t1);
                    matched.Add(t2);
                    matched.Add(t3);

                    foreach (var basePos in new[] { center, p1, p2, p3 })
                    {
                        var offs = GetOffsetNeighbors(basePos.x);
                        foreach (var off in offs)
                        {
                            var adj = basePos + off;
                            if (grid.TryGetValue(adj, out var extra) && IsMatchableTile(extra, centerType))
                                matched.Add(extra);
                        }
                    }

                    break;
                }
            }
        }

        return matched;
    }


    public IEnumerator ClearMatches(List<Tile> matchedTiles)
    {
        GameManager.Instance.BlockInput(true);

        var grid = gridManager.Grid;
        float duration = 0.35f;

        // 장애물에 알림
        var hitObstacles = GameManager.Instance.obstacleManager.NotifyNearbyMatches(new HashSet<Tile>(matchedTiles));

        // 일반 타일 애니메이션
        foreach (var tile in matchedTiles)
        {
            GameManager.Instance.StartCoroutine(tile.PlayDestroyAnimation(duration));
        }

        // 장애물 애니메이션
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

        // UI 갱신
        int normalMatchCount = matchedTiles.Count(t => t.Type != TileType.Egg);
        GameManager.Instance.AddTile(normalMatchCount);

        int removedHearts = hitObstacles.Count(t =>
            t.Type == TileType.Egg &&
            t.GetComponent<IObstacle>()?.IsRemovable == true);

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
            if (tA.Type == TileType.Egg) continue;

            var neighbors = GetOffsetNeighbors(pos.x);
            foreach (var dir in neighbors)
            {
                Vector2Int nPos = pos + dir;
                if (!grid.TryGetValue(nPos, out var tB)) continue;
                if (tB.Type == TileType.Egg) continue;

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
