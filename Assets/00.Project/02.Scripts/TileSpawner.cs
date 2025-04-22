using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 타일 빈칸 채우기 로직 (기존 타일 수직 낙하 후 신규 스폰)
/// </summary>
public class TileSpawner : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform spawnAnchor;
    [Tooltip("한 칸 이동당 걸리는 시간")]
    public float fallDuration = 0.1f;    // 타일 낙하 속도 (초)
    public float swapDuration = 0.08f;    // 스왑 애니메이션 속도 (초)

    /// <summary>
    /// 빈칸 채우기: 1) 기존 타일 낙하, 2) 신규 타일 스폰
    /// </summary>
    public IEnumerator FillEmptyTiles()
    {
        // 1) 기존 타일 수직 압축
        CollapseColumns();
        yield return AnimateCollapse();

        // 2) 신규 타일 스폰
        var grid = gridManager.Grid;
        var heights = gridManager.columnHeights;
        var newTiles = new List<Tile>();

        for (int x = 0; x < heights.Length; x++)
        {
            int count = 0;
            foreach (var kv in grid)
                if (kv.Key.x == x) count++;

            for (int y = count; y < heights[x]; y++)
            {
                var pos = new Vector2Int(x, y);
                var tile = TilePool.Instance.GetTile(pos);
                tile.transform.SetParent(gridManager.transform);
                // spawnAnchor위치에서 시작
                tile.transform.position = spawnAnchor.position;
                grid[pos] = tile;
                newTiles.Add(tile);
            }
        }

        // 신규 타일 낙하 애니메이션
        foreach (var tile in newTiles)
        {
            int x = tile.GridPosition.x;
            int y = tile.GridPosition.y;
            float yOff = ((gridManager.columnHeights[x] % 2 == 0) ? (gridManager.columnHeights[x] / 2f - 0.5f) : (gridManager.columnHeights[x] / 2f));
            Vector3 target = gridManager.GetTileWorldPosition(x, y - yOff);
            yield return StartCoroutine(MoveTo(tile.transform, target, fallDuration));
        }
    }

    /// <summary>
    /// 각 열의 타일을 수직 아래로 압축
    /// </summary>
    private void CollapseColumns()
    {
        var grid = gridManager.Grid;
        var newGrid = new Dictionary<Vector2Int, Tile>();

        for (int x = 0; x < gridManager.columnHeights.Length; x++)
        {
            var columnTiles = new List<Tile>();
            foreach (var kv in grid)
                if (kv.Key.x == x)
                    columnTiles.Add(kv.Value);

            columnTiles.Sort((a, b) => a.GridPosition.y.CompareTo(b.GridPosition.y));

            for (int i = 0; i < columnTiles.Count; i++)
            {
                var tile = columnTiles[i];
                var newPos = new Vector2Int(x, i);
                tile.GridPosition = newPos;
                newGrid[newPos] = tile;
            }
        }

        grid.Clear();
        foreach (var kv in newGrid)
            grid[kv.Key] = kv.Value;
    }

    /// <summary>
    /// 모든 타일 병렬 이동 (딜레이 최소화)
    /// </summary>
    private IEnumerator AnimateCollapse()
    {
        var tiles = gridManager.Grid.Values.ToList();
        // 모든 타일을 동시에 이동
        foreach (var t in tiles)
        {
            int x = t.GridPosition.x;
            int y = t.GridPosition.y;
            float yOff = ((gridManager.columnHeights[x] % 2 == 0)
                ? (gridManager.columnHeights[x] / 2f - 0.5f)
                : (gridManager.columnHeights[x] / 2f));
            Vector3 target = gridManager.GetTileWorldPosition(x, y - yOff);
            StartCoroutine(MoveTo(t.transform, target, fallDuration));
        }
        // 낙하 시간만큼만 대기
        yield return new WaitForSeconds(fallDuration);
    }

    /// <summary>
    /// 위치 보간 이동
    /// </summary>
    public IEnumerator MoveTo(Transform obj, Vector3 target, float duration)
    {
        Vector3 start = obj.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            obj.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        obj.position = target;
    }
}
