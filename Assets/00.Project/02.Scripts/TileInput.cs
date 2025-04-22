using UnityEngine;

/// <summary>
/// 유저 드래그를 통해 헥사 인접 타일 스왑을 처리하는 입력 매니저
/// - odd‑r offset(pointy‑top) 그리드에 맞춘 6방향 이웃 인식
/// - 드래그 방향과 가장 유사한 이웃 타일로 Swap&Match 호출
/// - 인접하지 않거나 존재하지 않는 타일 스왑 방지
/// </summary>
public class TileInput : MonoBehaviour
{
    private Tile selectedTile;
    private Camera cam;
    private GridManager gridManager;

    private void Start()
    {
        cam = Camera.main;
        gridManager = GameManager.Instance.gridManager;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = Physics2D.OverlapPoint(mouseWorld);
            if (col != null)
                selectedTile = col.GetComponent<Tile>();
        }

        if (Input.GetMouseButtonUp(0) && selectedTile != null)
        {
            Vector2 worldUp = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dragDir = (worldUp - (Vector2)selectedTile.transform.position).normalized;

            Vector2Int startPos = selectedTile.GridPosition;
            var neighbors = GetOffsetNeighbors(startPos.x);

            // 드래그 방향에 가장 잘 맞는 이웃 오프셋 찾기
            Vector2Int bestOff = GetBestDirection(dragDir, startPos, neighbors);
            Vector2Int targetPos = startPos + bestOff;

            // 인접 여부 및 존재 여부 확인
            if (!IsAdjacent(startPos, targetPos) || !gridManager.Grid.ContainsKey(targetPos))
            {
                selectedTile = null;
                return;
            }

            // 스왑 및 매치 실행
            var neighborTile = gridManager.Grid[targetPos];
            StartCoroutine(GameManager.Instance.SwapAndMatch(selectedTile, neighborTile));
            selectedTile = null;
        }
    }

    /// <summary>
    /// 두 그리드 좌표가 인접한지 검사
    /// </summary>
    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        var neighbors = GetOffsetNeighbors(a.x);
        foreach (var off in neighbors)
        {
            if (a + off == b)
                return true;
        }
        return false;
    }

    /// <summary>
    /// odd‑r offset(pointy‑top) 헥사 그리드의 6방향 이웃 델타
    /// - 홀/짝 열에 따른 대각선 방향 처리
    /// </summary>
    private Vector2Int[] GetOffsetNeighbors(int col)
    {
        // even-col: 아래 대각선
        Vector2Int[] evenCol = new[] {
            new Vector2Int( 1,  0),
            new Vector2Int( 0,  1),
            new Vector2Int(-1,  0),
            new Vector2Int( 0, -1),
            new Vector2Int( 1, -1),
            new Vector2Int(-1, -1)
        };
        // odd-col: 위 대각선
        Vector2Int[] oddCol = new[] {
            new Vector2Int( 1,  0),
            new Vector2Int( 0,  1),
            new Vector2Int(-1,  0),
            new Vector2Int( 0, -1),
            new Vector2Int( 1,  1),
            new Vector2Int(-1,  1)
        };
        // 열 패리티 반전 적용
        return (col % 2 == 1) ? evenCol : oddCol;
    }

    /// <summary>
    /// 드래그 방향과 가장 유사한 이웃 델타 반환
    /// </summary>
    private Vector2Int GetBestDirection(Vector2 dragDir, Vector2Int gridPos, Vector2Int[] neighbors)
    {
        float maxDot = float.NegativeInfinity;
        Vector2Int bestOff = Vector2Int.zero;

        // 기준 월드 좌표
        Vector2 origin = gridManager.GetTileWorldPosition(
            gridPos.x,
            gridPos.y - ((gridManager.columnHeights[gridPos.x] % 2 == 0)
                ? (gridManager.columnHeights[gridPos.x] / 2f - 0.5f)
                : (gridManager.columnHeights[gridPos.x] / 2f))
        );

        foreach (var off in neighbors)
        {
            Vector2Int np = gridPos + off;
            if (!gridManager.Grid.ContainsKey(np))
                continue;
            Vector2 neighborWorld = gridManager.GetTileWorldPosition(
                np.x,
                np.y - ((gridManager.columnHeights[np.x] % 2 == 0)
                    ? (gridManager.columnHeights[np.x] / 2f - 0.5f)
                    : (gridManager.columnHeights[np.x] / 2f))
            );
            float dot = Vector2.Dot(dragDir, (neighborWorld - origin).normalized);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestOff = off;
            }
        }
        return bestOff;
    }
}
