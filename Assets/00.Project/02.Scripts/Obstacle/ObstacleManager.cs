using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    private GridManager gridManager;

    private void Start()
    {
        gridManager = GameManager.Instance.gridManager;
    }

    /// <summary>
    /// 매칭된 타일들 주변의 장애물에게 한 번만 알림
    /// </summary>
    public HashSet<Tile> NotifyNearbyMatches(HashSet<Tile> matchedTiles)
    {
        var grid = GameManager.Instance.gridManager.Grid;
        var hitObstacles = new HashSet<Tile>();

        foreach (var tile in matchedTiles)
        {
            Vector2Int[] neighbors = TileMatcher.GetOffsetNeighbors(tile.GridPosition.x);
            foreach (var offset in neighbors)
            {
                Vector2Int adj = tile.GridPosition + offset;
                if (!grid.TryGetValue(adj, out var neighbor)) continue;

                if (neighbor.Type == TileType.Heart && !hitObstacles.Contains(neighbor))
                {
                    var obs = neighbor.GetComponent<IObstacle>();
                    obs?.OnNearbyMatch();
                    hitObstacles.Add(neighbor);
                }
            }
        }

        return hitObstacles;
    }

}
