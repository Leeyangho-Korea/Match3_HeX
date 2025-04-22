using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 헥사 offset(pointy-top) 3매치 매칭기
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
        foreach (var kv in grid)
        {
            var startPos = kv.Key;
            var startTile = kv.Value;
            var type = startTile.Type;
            var neighbors = GetOffsetNeighbors(startPos.x);
            for (int axis = 0; axis < 3; axis++)
            {
                var dir = neighbors[axis];
                var inv = neighbors[axis + 3];
                var line = new List<Tile> { startTile };
                Vector2Int pos;
                pos = startPos;
                while (grid.TryGetValue(pos + dir, out var next) && next.Type == type)
                {
                    line.Add(next);
                    pos += dir;
                }
                pos = startPos;
                while (grid.TryGetValue(pos + inv, out var prev) && prev.Type == type)
                {
                    line.Add(prev);
                    pos += inv;
                }
                if (line.Count >= 3)
                    foreach (var t in line) matched.Add(t);
            }
        }
        return new List<Tile>(matched);
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