using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 타일 빈칸 채우기 로직 (중력에 따른 자연 낙하 및 신규 스폰)
/// - 헥사 그리드의 중력 방향으로 타일 낙하
/// - 같은 열 및 대각선 아래 방향(진짜 아래 방향)으로만 타일이 이동
/// - 빈 위치를 정확히 찾아 신규 스폰, 겹침 방지
/// </summary>
public class TileSpawner : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform spawnAnchor;
    [Tooltip("한 칸 이동당 걸리는 시간")] public float fallDuration = 0.1f;
    public float swapDuration = 0.08f;

    public IEnumerator FillEmptyTiles()
    {
        // 1) 빈 공간으로 자연 낙하 (순차 이동)
        yield return StartCoroutine(PhysicsCollapseSequential());

        // 2) 정확한 빈 위치에 신규 타일 스폰 및 낙하
        var grid = gridManager.Grid;
        var heights = gridManager.columnHeights;
        var newTiles = new List<Tile>();

        for (int x = 0; x < heights.Length; x++)
        {
            // 각 열의 0~height-1 중 그리드에 없는 위치를 모두 스폰
            for (int y = 0; y < heights[x]; y++)
            {
                var pos = new Vector2Int(x, y);
                if (!grid.ContainsKey(pos))
                {
                    var tile = TilePool.Instance.GetTile(pos);
                    tile.transform.SetParent(gridManager.transform);
                    tile.transform.position = spawnAnchor.position;
                    grid[pos] = tile;
                    newTiles.Add(tile);
                }
            }
        }

        // 신규 타일 낙하 애니메이션 (오직 아래로)
        foreach (var tile in newTiles)
        {
            var dest = tile.GridPosition;
            Vector3 targetWorld = GetWorldPosition(dest);
            yield return StartCoroutine(MoveTo(tile.transform, targetWorld, fallDuration));
        }
    }

    /// <summary>
    /// 순차적으로 한 타일씩 이동하며 중력 낙하 처리
    /// </summary>
    private IEnumerator PhysicsCollapseSequential()
    {
        var grid = gridManager.Grid;
        bool moved;
        do
        {
            moved = false;
            // 낮은 y부터 순회
            var tiles = grid.OrderBy(kv => kv.Key.y)
                            .ThenBy(kv => kv.Key.x)
                            .Select(kv => kv.Value)
                            .ToList();
            foreach (var tile in tiles)
            {
                var src = tile.GridPosition;
                foreach (var d in GetFallOffsets())
                {
                    var dest = src + d;
                    if (IsValidPosition(dest) && !grid.ContainsKey(dest))
                    {
                        // 그리드 업데이트
                        grid.Remove(src);
                        tile.GridPosition = dest;
                        grid[dest] = tile;
                        // 이동 애니메이션 후 대기
                        Vector3 worldPos = GetWorldPosition(dest);
                        yield return StartCoroutine(MoveTo(tile.transform, worldPos, fallDuration));
                        moved = true;
                        break;
                    }
                }
                if (moved)
                    break;
            }
        } while (moved);
    }

    /// <summary>
    /// 그리드 좌표 → 월드 좌표
    /// </summary>
    private Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        int x = gridPos.x;
        int y = gridPos.y;
        int height = gridManager.columnHeights[x];
        float yCenterOffset = (height % 2 == 0) ? (height / 2f - 0.5f) : (height / 2f);
        float logicalY = y - yCenterOffset;
        Vector2 world2D = gridManager.GetTileWorldPosition(x, logicalY);
        return new Vector3(world2D.x, world2D.y, 0f);
    }

    /// <summary>
    /// 중력 낙하 델타: 직하 및 좌/우 대각선 아래만 허용
    /// </summary>
    private Vector2Int[] GetFallOffsets()
    {
        return new[]
        {
            new Vector2Int(0, -1),  // 직하
            new Vector2Int(-1, -1), // 좌 대각선 아래
            new Vector2Int(1, -1)   // 우 대각선 아래
        };
    }

    /// <summary>
    /// 그리드 범위 내 유효 위치 확인
    /// </summary>
    private bool IsValidPosition(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= gridManager.columnHeights.Length)
            return false;
        if (pos.y < 0 || pos.y >= gridManager.columnHeights[pos.x])
            return false;
        return true;
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
}
