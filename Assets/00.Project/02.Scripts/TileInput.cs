using UnityEngine;

/// <summary>
/// 유저 드래그를 통해 헥사 인접 타일 스왑을 처리하는 입력 매니저
/// - odd‑r offset(pointy‑top) 그리드에 맞춘 6방향 이웃 인식
/// - 드래그 방향과 가장 유사한 이웃 타일로 Swap&Match 호출
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
            Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dragDir = (mouseWorld - (Vector2)selectedTile.transform.position).normalized;

            Vector2Int gridPos = selectedTile.GridPosition;
            var neighbors = GetOffsetNeighbors(gridPos.x);

            // 드래그 방향에 가장 잘 맞는 이웃 오프셋 찾기
            Vector2Int bestOff = GetBestDirection(dragDir, gridPos, neighbors);
            Vector2Int neighborPos = gridPos + bestOff;

            if (gridManager.Grid.TryGetValue(neighborPos, out Tile neighbor))
                StartCoroutine(GameManager.Instance.SwapAndMatch(selectedTile, neighbor));

            selectedTile = null;
        }
    }

    /// <summary>
    /// odd‑r offset(pointy‑top) 헥사 그리드의 열짝수/홀수에 따른 6방향 이웃 델타
    /// </summary>
    private Vector2Int[] GetOffsetNeighbors(int col)
    {
        if (col % 2 == 0)
            return new[]{
                new Vector2Int( 1,  0),
                new Vector2Int( 0,  1),
                new Vector2Int(-1,  0),
                new Vector2Int( 0, -1),
                new Vector2Int( 1, -1),
                new Vector2Int(-1, -1)
            };
        else
            return new[]{
                new Vector2Int( 1,  0),
                new Vector2Int( 0,  1),
                new Vector2Int(-1,  0),
                new Vector2Int( 0, -1),
                new Vector2Int( 1,  1),
                new Vector2Int(-1,  1)
            };
    }

    /// <summary>
    /// 드래그 방향과 이웃 방향 중 유사도가 가장 높은 오프셋 반환
    /// </summary>
    private Vector2Int GetBestDirection(Vector2 dragDir, Vector2Int gridPos, Vector2Int[] neighbors)
    {
        float maxDot = float.NegativeInfinity;
        Vector2Int bestOff = Vector2Int.zero;

        // 기준 월드 좌표
        Vector2 originWorld = gridManager.GetTileWorldPosition(
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

            Vector2 dir = (neighborWorld - originWorld).normalized;
            float dot = Vector2.Dot(dragDir, dir);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestOff = off;
            }
        }
        return bestOff;
    }
}
