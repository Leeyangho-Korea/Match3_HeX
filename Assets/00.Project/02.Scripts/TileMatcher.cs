using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileMatcher : MonoBehaviour
{
    // GridManager에 접근하기 위한 참조 (GameManager 통해 간접 접근)
    private GridManager gridManager => GameManager.Instance.gridManager;

    // 헥사 그리드의 오프셋 방식 (odd-q 기준)으로 인접 타일 6방향 반환
    private Vector2Int[] GetOffsetNeighbors(int col)
    {
        // 중앙보다 왼쪽에 있는 열
        if (col < gridManager.columnHeights.Length / 2)
        {
            return new[] {
                new Vector2Int( 0, 1), // 0 ↑ 상
                new Vector2Int( 0,  -1),  // 1 ↓ 하
                new Vector2Int( 1, 1), // 2 ↗ 우상
                new Vector2Int(-1, -1), // 3 ↙ 좌하
                new Vector2Int(-1, 0), // 4 좌상
                new Vector2Int(1, 0) // 5 우하
            };
        }
        // 중앙보다 오른쪽에 있는 열
        else if (col > gridManager.columnHeights.Length / 2)
        {
            return new[] {
                new Vector2Int( 0, 1), // 0↑ 상
                new Vector2Int( 0,  -1),  // 1 ↓ 하
                new Vector2Int( 1,  0), // 2 ↗ 우상
                new Vector2Int(-1,  0), // 3 ↙ 좌하
                new Vector2Int(-1, 1), // 4 좌상
                new Vector2Int(1, -1) // 5 우하
            };
        }
        // 중앙 열
        else
        {
            return new[] {
                new Vector2Int( 0, 1), // 0↑ 상
                new Vector2Int( 0,  1),  // 1 ↓ 하
                new Vector2Int( 1,  0), // 2 ↗ 우상
                new Vector2Int(-1,  -1), // 3 ↙ 좌하
                new Vector2Int(-1, 0), // 4 좌상
                new Vector2Int(1, -1) // 5 우하
            };
        }
    }

    // 현재 그리드에서 매칭되는 타일들을 찾아서 반환
    public List<Tile> FindMatches(Dictionary<Vector2Int, Tile> grid)
    {
        var matched = new HashSet<Tile>();

        // 방향쌍 인덱스만 정의 (타일마다 실제 방향 오프셋은 GetOffsetNeighbors로 동적으로 결정)
        var dirPairs = new[] {
        (0, 1), // ↑ ↓
        (2, 3), // ↗ ↙
        (4, 5)  // ↖ ↘ (예시)
    };

        foreach (var kv in grid)
        {
            var centerPos = kv.Key;
            var centerType = kv.Value.Type;
            var centerOffsets = GetOffsetNeighbors(centerPos.x); // 이 타일 기준 오프셋 사용

            foreach (var (i, j) in dirPairs)
            {
                var linePos = new List<Vector2Int> { centerPos };

                // i 방향 확장
                var cur = centerPos + centerOffsets[i];
                while (grid.TryGetValue(cur, out var t1) && t1.Type == centerType)
                {
                    if (!linePos.Contains(cur)) linePos.Add(cur);
                    cur += GetOffsetNeighbors(cur.x)[i]; // 다음 확장할 열 기준 오프셋 사용
                }

                // j 방향 확장
                cur = centerPos + centerOffsets[j];
                while (grid.TryGetValue(cur, out var t2) && t2.Type == centerType)
                {
                    if (!linePos.Contains(cur)) linePos.Add(cur);
                    cur += GetOffsetNeighbors(cur.x)[j]; // 다음 확장할 열 기준 오프셋 사용
                }

                if (linePos.Count >= 3 && IsLinear(centerPos, linePos))
                {
                    foreach (var pos in linePos)
                        matched.Add(grid[pos]);

                    Debug.Log($"[매치 성공] {linePos.Count}개: {string.Join(", ", linePos)}");
                }
            }
        }

        return matched.ToList();
    }

    // 모든 타일이 중심 기준으로 동일한 방향으로 정렬되었는지 검사
    private bool IsLinear(Vector2Int center, List<Vector2Int> line)
    {
        return true;

        if (line.Count < 3) return false;

        // 중심 제외한 좌표
        var others = line.Where(p => p != center).ToList();

        // 기준 벡터
        var baseVec = (others[0] - center);

        for (int i = 1; i < others.Count; i++)
        {
            var delta = others[i] - center;

            // 외적이 0이면 같은 직선상에 있음
            int cross = baseVec.x * delta.y - baseVec.y * delta.x;
            if (cross != 0)
                return false;
        }

        return true;
    }

    // 매칭된 타일들을 그리드에서 제거하고 풀로 반환
    public void ClearMatches(List<Tile> matchedTiles)
    {
        var grid = gridManager.Grid;

        foreach (var tile in matchedTiles)
        {
            Debug.Log($"{tile.GridPosition} 제거 ");
            grid.Remove(tile.GridPosition);            // 그리드에서 제거
            tile.gameObject.SetActive(false);          // 비활성화
             TilePool.Instance.ReturnTile(tile);        // 풀에 반환
        }
    }




  

}
