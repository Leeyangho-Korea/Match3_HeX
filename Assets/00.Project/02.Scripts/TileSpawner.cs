using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform spawnAnchor; // 중앙 상단에 배치된 빈 GameObject

    [Header("Timings")]
    [Tooltip("한 칸 낙하에 걸리는 시간(초)")]
    public float fallDuration = 0.1f;
    [Tooltip("스왑 애니메이션 시간(초)")]
    public float swapDuration = 0.08f;   // ← 이 부분을 반드시 추가!

    /// <summary>
    /// 1) 남은 타일 vertical collapse 애니메이션
    /// 2) 빈 칸 개수만큼 중앙에서 SpawnAndSlideNewTile 호출
    /// </summary>
    public IEnumerator FillEmptyTiles()
    {
        yield return StartCoroutine(CollapseVerticalAnimated());

        var grid = gridManager.Grid;
        int totalSlots = gridManager.columnHeights.Sum();
        int missing = totalSlots - grid.Count;

        for (int i = 0; i < missing; i++)
            yield return StartCoroutine(SpawnAndSlideNewTile());

    }

    private IEnumerator SpawnAndSlideNewTile()
    {
        var grid = gridManager.Grid;
        var heights = gridManager.columnHeights;
        int midX = heights.Length / 2;

        // 중앙 열 바로 위 임시 그리드 좌표
        Vector2Int startPos = new Vector2Int(midX, heights[midX]);
        var tile = TilePool.Instance.GetTile(startPos);
        tile.transform.SetParent(gridManager.transform, worldPositionStays: true);
        tile.transform.position = spawnAnchor.position;

        Vector2Int current = startPos;
        while (true)
        {
            Vector2Int next;
            if (IsValid(next = current + Vector2Int.down) && !grid.ContainsKey(next)) { }
            else if (IsValid(next = current + new Vector2Int(-1, -1)) && !grid.ContainsKey(next)) { }
            else if (IsValid(next = current + new Vector2Int(1, -1)) && !grid.ContainsKey(next)) { }
            else break;

            // 한 칸씩 애니메이션 이동
            Vector3 wPos = GetWorldPosition(next);
            yield return StartCoroutine(MoveTo(tile.transform, wPos, fallDuration));
            current = next;
        }

        // 최종 위치에 배치
        tile.GridPosition = current;
        grid[current] = tile;
    }

    private IEnumerator CollapseVerticalAnimated()
    {
        var grid = gridManager.Grid;
        int cols = gridManager.columnHeights.Length;
        var moves = new List<(Tile tile, Vector2Int dst)>();

        // src → dst 매핑 수집
        for (int x = 0; x < cols; x++)
        {
            var colKeys = grid.Keys
                              .Where(p => p.x == x)
                              .OrderBy(p => p.y)
                              .ToList();
            for (int i = 0; i < colKeys.Count; i++)
            {
                var src = colKeys[i];
                var dst = new Vector2Int(x, i);
                if (src != dst)
                    moves.Add((grid[src], dst));
            }
        }

        // 그리드 업데이트
        foreach (var (tile, dst) in moves)
        {
            grid.Remove(tile.GridPosition);
            tile.GridPosition = dst;
            grid[dst] = tile;
        }

        // 병렬 애니메이션 낙하
        foreach (var (tile, dst) in moves)
        {
            Vector3 wPos = GetWorldPosition(dst);
            StartCoroutine(MoveTo(tile.transform, wPos, fallDuration));
        }

        yield return new WaitForSeconds(fallDuration);
    }

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

    private bool IsValid(Vector2Int p)
    {
        return p.x >= 0
            && p.x < gridManager.columnHeights.Length
            && p.y >= 0
            && p.y < gridManager.columnHeights[p.x];
    }

    private Vector3 GetWorldPosition(Vector2Int gp)
    {
        int x = gp.x, y = gp.y;
        int h = gridManager.columnHeights[x];
        float offsetY = (h % 2 == 0)
            ? (h / 2f - 0.5f)
            : (h / 2f);
        float logicalY = y - offsetY;
        Vector2 w2 = gridManager.GetTileWorldPosition(x, logicalY);
        return new Vector3(w2.x, w2.y, 0f);
    }
}
