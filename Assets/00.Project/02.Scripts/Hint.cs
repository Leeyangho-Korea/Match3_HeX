using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Hint : MonoBehaviour
{
    private GridManager _gm => GameManager.Instance.gridManager;
    private TileMatcher _matcher => GameManager.Instance.tileMatcher;

    public void ShowHint()
    {
        var grid = _gm.Grid;
        var positions = grid.Keys.ToList();

        foreach (var pos in positions)
        {
            if (!grid.TryGetValue(pos, out var tileA)) continue;

            var neighbors = GetOffsetNeighbors(pos.x);
            foreach (var dir in neighbors)
            {
                var neighborPos = pos + dir;
                if (!grid.ContainsKey(neighborPos)) continue;

                // 정확한 인접 방향 확인 (스왑 유효성 보장)
                Vector2Int actualDelta = neighborPos - pos;
                if (!neighbors.Contains(actualDelta)) continue;

                var tileB = grid[neighborPos];

                // 스왑 시뮬레이션
                grid[pos] = tileB;
                grid[neighborPos] = tileA;
                (tileA.GridPosition, tileB.GridPosition) = (neighborPos, pos);

                var matches = _matcher.FindMatches(grid);

                // 복구
                grid[pos] = tileA;
                grid[neighborPos] = tileB;
                (tileA.GridPosition, tileB.GridPosition) = (pos, neighborPos);

                if (matches.Count >= 3)
                {
                    Debug.Log($"Hint: Swap {pos} <-> {neighborPos} to match!");
                    return;
                }
            }
        }

        Debug.Log("No matchable hints found.");
    }

    private Vector2Int[] GetOffsetNeighbors(int col)
    {
        if ((col & 1) == 1) // 홀수 열 (odd-q offset)
        {
            return new[] {
                new Vector2Int( 1,  0), // →
                new Vector2Int( 0,  1), // ↑
                new Vector2Int(-1,  0), // ←
                new Vector2Int( 0, -1), // ↓
                new Vector2Int( 1, -1), // ↗
                new Vector2Int(-1, -1)  // ↙
            };
        }
        else // 짝수 열
        {
            return new[] {
                new Vector2Int( 1,  0), // →
                new Vector2Int( 0,  1), // ↑
                new Vector2Int(-1,  0), // ←
                new Vector2Int( 0, -1), // ↓
                new Vector2Int( 1,  1), // ↗
                new Vector2Int(-1,  1)  // ↙
            };
        }
    }
}
