using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    public float fallDuration = 0.1f;  // 타일 낙하 시간

    [Tooltip("슬라이드/스왑 애니메이션 시간(초)")]
    public float swapDuration = 0.08f;  // 슬라이드 이동 시간

    private int MidColumn => gridManager.columnHeights.Length / 2;  // 중앙 열 계산
    private bool lastSlideSuccess; // 마지막 슬라이드 성공 여부
    private int[] explorationOrder; // 탐색 순서 저장

    private void Awake()
    {
        explorationOrder = new int[gridManager.columnHeights.Length];  // 열 수만큼 배열 생성
    }


    private void Start()
    {

         // 중앙을 기준으로 양옆 탐색 순서 설정
        int[] heights = gridManager.columnHeights;
        int colCount = heights.Length;
        int mid = MidColumn;

        var order = new List<int>();
        for (int offset = 0; offset < mid; offset++)
        {
            order.Add(offset);
            order.Add((colCount - 1) - offset);
        }
        order.Add(mid); // 마지막으로 중앙열 탐색
        explorationOrder = order.ToArray();
    }


    // 빈 타일 채우기
    public IEnumerator FillEmptyTiles()
    {
        // 유저 입력 차단
        GameManager.Instance.BlockInput(true);
        // 떨어지는 중임을 알림 (타일 채워지는 중일 때 힌트로직 돌리지 않기 위함)
        GameManager.Instance.IsFilling = true;
        
        // 그리드에서 수직 낙하
        yield return StartCoroutine(CollapseVerticalAnimated());

        bool changed;
        do
        {
            changed = false;

            // 좌우로 슬라이드 시도
            foreach (int srcCol in explorationOrder)
            {
                do
                {
                    yield return StartCoroutine(SlideFromColumn(srcCol));
                    if (lastSlideSuccess)
                    {
                        // 이동 후 다시 탐색
                        changed = true;
                    }
                }
                while (lastSlideSuccess);
            }

        } while (changed);

        // 빈칸만큼 새 타일 생성, 슬라이드
        var grid = gridManager.Grid;
        int totalSlots = gridManager.columnHeights.Sum();
        int missing = totalSlots - grid.Count;

        for (int i = 0; i < missing; i++)
            yield return StartCoroutine(SpawnAndSlideNewTile());
            
            
        // 입력 허용
        GameManager.Instance.IsFilling = false;
        GameManager.Instance.BlockInput(false);
    }


    // 수직으로 떨어지는 애니메이션
    private IEnumerator CollapseVerticalAnimated()
    {
        var grid = gridManager.Grid;
        int cols = gridManager.columnHeights.Length;
        var moves = new List<(Tile tile, Vector2Int dst)>();

        // 각 열마다 위에서 아래로 순회
        for (int x = 0; x < cols; x++)
        {
            var colKeys = grid.Keys.Where(p => p.x == x).OrderBy(p => p.y).ToList();
            for (int i = 0; i < colKeys.Count; i++)
            {
                var src = colKeys[i];
                var dst = new Vector2Int(x, i);
                // 위치가 다르면 이동
                if (src != dst)
                    moves.Add((grid[src], dst));
            }
        }


        // 실제 좌표 변경
        foreach (var (tile, dst) in moves)
        {
            grid.Remove(tile.GridPosition);
            tile.GridPosition = dst;
            grid[dst] = tile;
        }

        // 이동 애니메이션 실행
        foreach (var (tile, dst) in moves)
        {
            Vector3 wPos = GetWorldPosition(dst);
            StartCoroutine(MoveTo(tile.transform, wPos, fallDuration));
        }

        // 낙하 시간 기다림
        yield return new WaitForSeconds(fallDuration);
    }

    // 특정 열에서 좌우로 슬라이드
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

        // 제일 위쪽 타일
        int srcY = srcKeys.Max(k => k.y);
        Vector2Int srcPos = new Vector2Int(srcCol, srcY);

        // 중앙 열은 특별 처리
        if (srcCol == MidColumn)
        {
            bool movedAny = false;
            Vector2Int current = srcPos;

            while (true)
            {
                var neighbors = new List<int>();
                if (current.x - 1 >= 0) neighbors.Add(current.x - 1);
                if (current.x + 1 < heights.Length) neighbors.Add(current.x + 1);

                // 빈칸 많은 쪽 우선 탐색
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

        // 중앙 아닌 열 (일반 슬라이드)
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


    // 새로운 타일 스폰 및 슬라이드
    private IEnumerator SpawnAndSlideNewTile()
    {
        var grid = gridManager.Grid;
        var heights = gridManager.columnHeights;
        int mid = MidColumn;

        var startPos = new Vector2Int(mid, heights[mid]);
        var tile = TilePool.Instance.GetTile(startPos);
        // 화면 상단 위치
        tile.transform.position = spawnAnchor.position;

        var current = startPos;

        // 빈칸 따라 슬라이드
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
        grid[current] = tile; // 최종 그리드에 추가
    }


    // 오브젝트 이동 애니메이션
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

    // 그리드 유효 범위 체크
    private bool IsValid(Vector2Int p)
    {
        return p.x >= 0 && p.x < gridManager.columnHeights.Length &&
               p.y >= 0 && p.y < gridManager.columnHeights[p.x];
    }

    // 그리드 좌표 -> 월드 좌표 변환
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
