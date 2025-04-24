using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 타일의 생성, 낙하, 슬라이딩 로직을 담당하는 클래스
/// </summary>
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

    [Tooltip("슬라이드를 허용할 수 있는 높이 차이 한계")]
    public float slideThreshold = 1.17f;

    private int MidColumn => gridManager.columnHeights.Length / 2;
    private bool lastSlideSuccess;
    private int[] explorationOrder;

    private void Awake()
    {
        explorationOrder = new int[gridManager.columnHeights.Length];
    }

    private void Start()
    {
        int[] heights = gridManager.columnHeights;
        int colCount = heights.Length;
        int mid = MidColumn;

        var order = new List<int>();
        for (int offset = 0; offset < mid; offset++)
        {
            order.Add(offset);
            order.Add((colCount - 1) - offset);
        }
        order.Add(mid); // 중앙 열 마지막에 추가
        explorationOrder = order.ToArray();
    }

    public IEnumerator FillEmptyTiles(bool isReshuffle = false)
    {
        GameManager.Instance.BlockInput(true);

        yield return StartCoroutine(CollapseVerticalAnimated());

        bool changed;
        do
        {
            changed = false;

            foreach (int srcCol in explorationOrder)
            {
                do
                {
                    yield return StartCoroutine(SlideFromColumn(srcCol));
                    if (lastSlideSuccess)
                    {
                        changed = true;
                    }
                }
                while (lastSlideSuccess);
            }

        } while (changed);

        var grid = gridManager.Grid;


        if (isReshuffle)
        {
            for (int x = 0; x < gridManager.columnHeights.Length; x++)
            {
                int height = gridManager.columnHeights[x];
                for (int y = 0; y < height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);

                    if (!grid.ContainsKey(pos) || grid[pos] == null || !grid[pos].gameObject.activeInHierarchy)
                    {
                        yield return StartCoroutine(SpawnAndSlideNewTileTo(pos));
                    }
                }
            }
        }
        else
        {
            // 기존 방식: 위에서 랜덤으로 빈 칸 채움
            int totalSlots = gridManager.columnHeights.Sum();
            int missing = totalSlots - grid.Count;

            for (int i = 0; i < missing; i++)
                yield return StartCoroutine(SpawnAndSlideNewTile());
        }

        GameManager.Instance.BlockInput(false);
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

    private IEnumerator SlideFromColumn(int srcCol)
    {
        lastSlideSuccess = false;
        var grid = gridManager.Grid;
        int[] heights = gridManager.columnHeights;

        var srcKeys = grid.Keys.Where(k => k.x == srcCol).ToList();
        if (srcKeys.Count == 0)
        {
            yield break;
        }

        int srcY = srcKeys.Max(k => k.y);
        Vector2Int srcPos = new Vector2Int(srcCol, srcY);

        if (srcCol == MidColumn)
        {
            bool movedAny = false;
            Vector2Int current = srcPos;

            while (true)
            {
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
                    if (current.y <= dstCount) continue;

                    Vector2Int dstPos = new Vector2Int(dstCol, dstCount);
                    Vector3 dstWorld = GetWorldPosition(dstPos);

                    var tile = grid[current];
                    grid.Remove(current);
                    current = dstPos;
                    tile.GridPosition = dstPos;
                    grid[dstPos] = tile;

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

        var neighborsStd = new List<int>();
        if (srcCol - 1 >= 0) neighborsStd.Add(srcCol - 1);
        if (srcCol + 1 < heights.Length) neighborsStd.Add(srcCol + 1);

        foreach (var dstCol in neighborsStd)
        {
            int dstCount = grid.Keys.Count(k => k.x == dstCol);
            if (srcY <= dstCount) continue;

            Vector2Int dstPos = new Vector2Int(dstCol, dstCount);

            // 최신 위치 기준으로 매번 계산
            Vector3 srcWorld = GetWorldPosition(srcPos);
            Vector3 dstWorld = GetWorldPosition(dstPos);

            if (!grid.ContainsKey(dstPos))
            {
                var tile = grid[srcPos];
                grid.Remove(srcPos);
                tile.GridPosition = dstPos;
                grid[dstPos] = tile;

                yield return StartCoroutine(MoveTo(tile.transform, GetWorldPosition(dstPos), swapDuration));
                lastSlideSuccess = true;
                yield break;
            }
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


    // Reshulle
    public IEnumerator SpawnAndSlideNewTileTo(Vector2Int gridPos)
    {
        var tile = TilePool.Instance.GetTile(gridPos);
        Vector3 startPos =   GetSpawnWorldPosition(gridPos.x); // 위쪽에서 생성
        Vector3 targetPos = gridManager.GridToWorld(gridPos);

        tile.transform.position = startPos;
        tile.GridPosition = gridPos;

        gridManager.Grid[gridPos] = tile;

        yield return StartCoroutine(MoveTo(tile.transform, targetPos, fallDuration));
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
        yield return null;
    }

    private bool IsValid(Vector2Int p)
    {
        return p.x >= 0 && p.x < gridManager.columnHeights.Length &&
               p.y >= 0 && p.y < gridManager.columnHeights[p.x];
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

    private Vector3 GetSpawnWorldPosition(int columnIndex)
{
    // 열의 높이만큼 위로 생성 (한 칸 위)
    int spawnY = gridManager.columnHeights[columnIndex];
    Vector2Int spawnGridPos = new Vector2Int(columnIndex, spawnY);

    return GetWorldPosition(spawnGridPos);
}
}
