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

            var neighbors = TileMatcher.GetOffsetNeighbors(pos.x);
            foreach (var dir in neighbors)
            {
                var neighborPos = pos + dir;
                if (!grid.ContainsKey(neighborPos)) continue;

                // 정확한 방향만
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

                    // 애니메이션 시작
                    GameManager.Instance.StartCoroutine(tileA.PlayHintAnimation());
                    GameManager.Instance.StartCoroutine(tileB.PlayHintAnimation());
                    return;
                }
            }
        }

        Debug.Log("No matchable hints found.");
    }



}
