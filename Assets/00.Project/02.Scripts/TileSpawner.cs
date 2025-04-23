using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform spawnAnchor;

    [Header("Timings")]
    [Tooltip("한 칸 낙하에 걸리는 시간(초)")]
    public float fallDuration = 0.1f;
    [Tooltip("슬라이드/스왑 애니메이션 시간(초)")]
    public float swapDuration = 0.08f;

    [Tooltip("열 슬라이드를 위한 임계 높이(중앙 열 제외)")]
    public float slideThreshold = 1.17f;

    private int MidColumn => gridManager.columnHeights.Length / 2;
    private bool lastSlideSuccess;

    /// <summary>
    /// 1) 빈 공간 수직 압축 애니메이션
    /// 2) 중앙을 제외한 인접 열 탐색 후 필요 시 슬라이드
    /// 3) 마지막으로 중앙 열 슬라이드 검사(가능한만큼 반복)
    /// 4) 신규 타일 생성 및 낙하
    /// </summary>
    public IEnumerator FillEmptyTiles()
    {
        // 1) 빈 공간 collapse
        yield return StartCoroutine(CollapseVerticalAnimated());
        // 2) 중앙 제외한 열 슬라이드
        yield return StartCoroutine(SlideColumnsExceptCentral());
        // 3) 중앙 열 슬라이드 가능한 만큼 반복
        do
        {
            yield return StartCoroutine(SlideFromColumn(MidColumn));
        } while (lastSlideSuccess);
        // 4) 신규 타일 생성 및 낙하
        var grid = gridManager.Grid;
        int totalSlots = gridManager.columnHeights.Sum();
        int missing = totalSlots - grid.Count;
        for (int i = 0; i < missing; i++)
            yield return StartCoroutine(SpawnAndSlideNewTile());
    }

    private IEnumerator CollapseVerticalAnimated()
    {
        var grid = gridManager.Grid;
        int cols = gridManager.columnHeights.Length;
        var moves = new List<(Tile tile, Vector2Int dst)>();

        for (int x = 0; x < cols; x++)
        {
            var colKeys = grid.Keys.Where(p => p.x == x).OrderBy(p => p.y).ToList();
            for (int i = 0; i < colKeys.Count; i++)
            {
                var src = colKeys[i];
                var dst = new Vector2Int(x, i);
                if (src != dst)
                    moves.Add((grid[src], dst));
            }
        }

        foreach (var (tile, dst) in moves)
        {
            grid.Remove(tile.GridPosition);
            tile.GridPosition = dst;
            grid[dst] = tile;
        }

        foreach (var (tile, dst) in moves)
        {
            Vector3 wPos = GetWorldPosition(dst);
            StartCoroutine(MoveTo(tile.transform, wPos, fallDuration));
        }

        yield return new WaitForSeconds(fallDuration);
    }

    private IEnumerator SlideColumnsExceptCentral()
    {
        int[] heights = gridManager.columnHeights;
        int colCount = heights.Length;
        int mid = MidColumn;
        var order = new List<int>();
        for (int offset = 0; offset < mid; offset++)
        {
            order.Add(offset);
            order.Add(colCount - 1 - offset);
        }
        foreach (int srcCol in order)
            yield return StartCoroutine(SlideFromColumn(srcCol));
    }

    private IEnumerator SlideFromColumn(int srcCol)
    {
        lastSlideSuccess = false;
        var grid = gridManager.Grid;
        int[] heights = gridManager.columnHeights;

        // 초기 위치
        var srcKeys = grid.Keys.Where(k => k.x == srcCol).ToList();
        if (srcKeys.Count == 0) yield break;
        int srcY = srcKeys.Max(k => k.y);
        Vector2Int srcPos = new Vector2Int(srcCol, srcY);

        // 중앙 열일 때엔 연속 슬라이드
        if (srcCol == MidColumn)
        {
            bool movedAny = false;
            // 현재 포지션 추적
            Vector2Int current = srcPos;
            while (true)
            {
                // 이웃 열 중 빈 슬롯 확인 및 우선순위
                var neighbors = new List<int>();
                if (current.x - 1 >= 0) neighbors.Add(current.x - 1);
                if (current.x + 1 < heights.Length) neighbors.Add(current.x + 1);
                neighbors = neighbors
                    .OrderByDescending(n => heights[n] - grid.Keys.Count(k => k.x == n))
                    .ToList();

                bool moved = false;
                foreach (var dstCol in neighbors)
                {
                    int dstCount = grid.Keys.Count(k => k.x == dstCol);
                    // 아래로 이동 가능한지 확인
                    if (current.y <= dstCount) continue;
                    Vector2Int dstPos = new Vector2Int(dstCol, dstCount);
                    Vector3 dstWorld = GetWorldPosition(dstPos);
                    // threshold 없이 바로 슬라이드
                    var tile = grid[current];

                    // 그리드 업데이트
                    grid.Remove(current);
                    current = dstPos;
                    tile.GridPosition = dstPos;
                    grid[dstPos] = tile;

                    // 애니메이션
                    yield return StartCoroutine(MoveTo(tile.transform, dstWorld, swapDuration));
                    moved = true;
                    movedAny = true;
                    break;
                }
                if (!moved) break;
            }
            lastSlideSuccess = movedAny;
            yield break;
        }

        // 중앙 제외 일반 열 한 칸 슬라이드
        var neighborsStd = new List<int>();
        if (srcCol - 1 >= 0) neighborsStd.Add(srcCol - 1);
        if (srcCol + 1 < heights.Length) neighborsStd.Add(srcCol + 1);
        Vector3 srcWorld = GetWorldPosition(srcPos);
        foreach (var dstCol in neighborsStd)
        {
            int dstCount = grid.Keys.Count(k => k.x == dstCol);
            if (srcY <= dstCount) continue;
            Vector2Int dstPos = new Vector2Int(dstCol, dstCount);
            Vector3 dstWorld = GetWorldPosition(dstPos);
            // threshold 확인
            if (srcWorld.y <= dstWorld.y + slideThreshold) continue;

            lastSlideSuccess = true;
            var tile = grid[srcPos];
            grid.Remove(srcPos);
            tile.GridPosition = dstPos;
            grid[dstPos] = tile;
            yield return StartCoroutine(MoveTo(tile.transform, dstWorld, swapDuration));
            yield break;
        }
    }

    private IEnumerator SpawnAndSlideNewTile()
    {
        var grid = gridManager.Grid;
        var heights = gridManager.columnHeights;
        int mid = MidColumn;

        var startPos = new Vector2Int(mid, heights[mid]);
        var tile = TilePool.Instance.GetTile(startPos);
        tile.transform.SetParent(gridManager.transform, true);
        tile.transform.position = spawnAnchor.position;

        var current = startPos;
        while (true)
        {
            Vector2Int next;
            if (IsValid(next = current + Vector2Int.down) && !grid.ContainsKey(next)) { }
            else if (IsValid(next = current + new Vector2Int(-1, -1)) && !grid.ContainsKey(next)) { }
            else if (IsValid(next = current + new Vector2Int(1, -1)) && !grid.ContainsKey(next)) { }
            else break;
            Vector3 wPos = GetWorldPosition(next);
            yield return StartCoroutine(MoveTo(tile.transform, wPos, fallDuration));
            current = next;
        }
        tile.GridPosition = current;
        grid[current] = tile;
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
        return p.x >= 0 && p.x < gridManager.columnHeights.Length
            && p.y >= 0 && p.y < gridManager.columnHeights[p.x];
    }

    private Vector3 GetWorldPosition(Vector2Int gp)
    {
        int x = gp.x, y = gp.y;
        int h = gridManager.columnHeights[x];
        float offsetY = (h % 2 == 0) ? (h / 2f - 0.5f) : (h / 2f);
        float logicalY = y - offsetY;
        var w2 = gridManager.GetTileWorldPosition(x, logicalY);
        return new Vector3(w2.x, w2.y, 0f);
    }
}