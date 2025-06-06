using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 장애물 관리 스크립트 
/// 주변의 타일 매칭 이벤트 여부 확인
/// </summary>

public class ObstacleManager : MonoBehaviour
{
    private GridManager gridManager;

    private void Start()
    {
        gridManager = GameManager.Instance.gridManager;
    }

    public Sprite[] eggSprite;

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

                if (neighbor.Type == TileType.Egg && !hitObstacles.Contains(neighbor))
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
