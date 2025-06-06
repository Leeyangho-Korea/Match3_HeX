using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// 타일 매칭 관리 클래스
/// 타일의 매칭 여부 판단 
/// 매칭 타일들을 그룹별로 나누기
/// 매칭 타일 제거
/// 타일들의 매칭 시뮬레이션 (힌트, 타일 재배치 여부판단)
/// </summary>

public class TileMatcher : MonoBehaviour
{
    private GridManager gridManager => GameManager.Instance.gridManager;

    private static Vector2Int[] _leftOffsets = new[]
    {
        new Vector2Int( 0, 1),      //상 
        new Vector2Int( 0, -1),     //하
        new Vector2Int( 1, 1),      //우상
        new Vector2Int(-1, -1),     //좌하
        new Vector2Int(-1, 0),      //좌상
        new Vector2Int( 1, 0)       //우하
    };

 
    private static Vector2Int[] _rightOffsets = new[]
    {
        new Vector2Int( 0, 1),      //상
        new Vector2Int( 0, -1),     //하
        new Vector2Int( 1, 0),      //우상
        new Vector2Int(-1, 0),      //좌하
        new Vector2Int(-1, 1),      //좌상
        new Vector2Int( 1, -1)      //우하
    };

    private static Vector2Int[] _centerOffsets = new[]
    {
        new Vector2Int( 0, 1),      //상 
        new Vector2Int( 0, -1),     //하
        new Vector2Int( 1, 0),      //우상
        new Vector2Int(-1, -1),     //좌하
        new Vector2Int(-1, 0),      //좌상
        new Vector2Int( 1, -1)      //우하
    };

    // 그리드 열별 방향 오프셋 반환
    public static Vector2Int[] GetOffsetNeighbors(int col)
    {
        int mid = GameManager.Instance.gridManager.columnHeights.Length / 2;
    
        if (col < mid) 
           return _leftOffsets;
        else if (col > mid) 
          return _rightOffsets;
        else 
          return _centerOffsets;
    }

    // 타일의 매칭 가능여부 판단 (장애물이 아니고 같은 타입일 경우에만 true 반환)
    private bool IsMatchableTile(Tile tile, TileType type)
    {
        return tile.Type == type && tile.Type != TileType.Egg;
    }

    // 전체 보드에서 매칭되는 타일 리스트 찾기
    public List<Tile> FindMatches(Dictionary<Vector2Int, Tile> grid)
    {
        var matched = new HashSet<Tile>();
        var dirPairs = new[] { (0, 1), (2, 3), (4, 5) };

        foreach (var kv in grid)
        {
            var centerPos = kv.Key;
            var centerTile = kv.Value;
            var centerType = centerTile.Type;
            if (centerType == TileType.Egg) continue;
            var centerOffsets = GetOffsetNeighbors(centerPos.x);

            foreach (var (i, j) in dirPairs)
            {
                var linePos = new List<Vector2Int> { centerPos };

                // i 방향
                var cur = centerPos + centerOffsets[i];
                while (grid.TryGetValue(cur, out var t1) && IsMatchableTile(t1, centerType))
                {
                    linePos.Add(cur);
                    cur += GetOffsetNeighbors(cur.x)[i];
                }

                // j 방향
                cur = centerPos + centerOffsets[j];
                while (grid.TryGetValue(cur, out var t2) && IsMatchableTile(t2, centerType))
                {
                    linePos.Add(cur);
                    cur += GetOffsetNeighbors(cur.x)[j];
                }

                if (linePos.Count >= 3)
                {
                    foreach (var pos in linePos)
                        matched.Add(grid[pos]);
                }
            }
        }

        matched.UnionWith(FindDiamondMatches(grid));
        return matched.ToList();
    }

    // 다이아몬드 패턴 매칭 찾기 (4타일 조건)
    private HashSet<Tile> FindDiamondMatches(Dictionary<Vector2Int, Tile> grid)
    {
        var matched = new HashSet<Tile>();

        foreach (var kv in grid)
        {
            var center = kv.Key;
            var centerTile = kv.Value;
            var centerType = centerTile.Type;
            if (centerType == TileType.Egg) continue;
            var centerOffsets = GetOffsetNeighbors(center.x);

            // 탐색 그룹별 위치 조합
            var combos = new[] {
                (0, 2, 5), (0, 4, 3),
                (1, 3, 4), (1, 5, 2),
                (1, 3, 5), (0, 2, 4)
            };

            foreach (var (i1, i2, i3) in combos)
            {
                var p1 = center + centerOffsets[i1];
                var p2 = center + centerOffsets[i2];
                var p3 = center + centerOffsets[i3];

                if (
                    grid.TryGetValue(p1, out var t1) && IsMatchableTile(t1, centerType) &&
                    grid.TryGetValue(p2, out var t2) && IsMatchableTile(t2, centerType) &&
                    grid.TryGetValue(p3, out var t3) && IsMatchableTile(t3, centerType)
                )
                {
                    matched.Add(centerTile);
                    matched.Add(t1);
                    matched.Add(t2);
                    matched.Add(t3);

                    foreach (var basePos in new[] { center, p1, p2, p3 })
                    {
                        var offs = GetOffsetNeighbors(basePos.x);
                        foreach (var off in offs)
                        {
                            var adj = basePos + off;
                            if (grid.TryGetValue(adj, out var extra) && IsMatchableTile(extra, centerType))
                                matched.Add(extra);
                        }
                    }

                    break;
                }
            }
        }

        return matched;
    }

    // 매칭 타일 제거
    public IEnumerator ClearMatches(List<Tile> matchedTiles)
    {
        GameManager.Instance.BlockInput(true);

        var grid = gridManager.Grid;
        float duration = 0.35f;

        // 폭탄이 된 개수
        int transfomrBombCount =0; 
        
        // 1. 매칭 그룹 분리
        var groups = GroupMatches(matchedTiles);

        // 폭탄 생성 제외하고 일단 매칭된 애들 모음
        var finalMatched = new HashSet<Tile>();

        // 알에게 알릴 매칭타일
        var forObstacles = new HashSet<Tile>();
        foreach (var group in groups)
        {
            if (group.Count >= 4)
            {
                int bombIndex = group.Count / 2;
                var bombTile = group[bombIndex];
                bombTile.SetBomb(true, TilePool.Instance.GetBombSprite(bombTile.Type));
                forObstacles.Add(bombTile);
                transfomrBombCount ++;
                foreach (var tile in group)
                {
                    if (tile != bombTile)
                        finalMatched.Add(tile);
                }
            }
            else
            {
                finalMatched.UnionWith(group);
            }
        }

        // 연쇄 폭발은 그룹 루프 끝나고 처리
        var alreadyExploded = new HashSet<Tile>();
        bool chainReaction;
        do
        {
            chainReaction = false;

            var extraBombs = finalMatched
                .Where(t => t.IsBomb)
                .Where(bomb => !alreadyExploded.Contains(bomb))
                .ToList();

            foreach (var bomb in extraBombs)
            {
                alreadyExploded.Add(bomb);
                var explosion = GetBombExplosion(bomb);

                if (explosion.Any(t => !finalMatched.Contains(t)))
                    chainReaction = true;

                finalMatched.UnionWith(explosion);
            }

        } while (chainReaction);



        forObstacles.UnionWith(finalMatched);
        // 장애물에 알림
        var hitObstacles = GameManager.Instance.obstacleManager.NotifyNearbyMatches(forObstacles);
        
        yield return null;

        // 애니메이션 실행
        foreach (var tile in finalMatched)
        {
            if (tile.Type != TileType.Egg)
            {
                GameManager.Instance.StartCoroutine(tile.PlayDestroyAnimation(duration));
            }
        }

        foreach (var obsTile in hitObstacles)
        {
            if (obsTile.GetComponent<IObstacle>()?.IsRemovable == true)
            {
                GameManager.Instance.StartCoroutine(obsTile.GetComponent<IObstacle>().PlayDestroyEffect());
            }
        }

        yield return new WaitForSeconds(duration);

        // 타일 제거
        foreach (var tile in finalMatched)
        {
            if (tile.Type != TileType.Egg)
            {
                grid.Remove(tile.GridPosition);
                tile.gameObject.SetActive(false);
                tile.SetBomb(false, null);
                TilePool.Instance.ReturnTile(tile);
            }
        }

        foreach (var tile in hitObstacles)
        {
            if (tile.GetComponent<IObstacle>()?.IsRemovable == true)
            {
                grid.Remove(tile.GridPosition);
                tile.gameObject.SetActive(false);
                TilePool.Instance.ReturnTile(tile);
            }
        }

        // UI 갱신
        int normalMatchCount = finalMatched.Count(t => t.Type != TileType.Egg);
        GameManager.Instance.AddTile(normalMatchCount+ transfomrBombCount);

        int removedHearts = hitObstacles.Count(t =>
            t.Type == TileType.Egg &&
            t.GetComponent<IObstacle>()?.IsRemovable == true);

        GameManager.Instance.AddHeart(removedHearts);

        GameManager.Instance.BlockInput(false);
    }


    //매칭 그룹을 BFS로 분리
    private List<List<Tile>> GroupMatches(List<Tile> matched)
    {
        var grid = gridManager.Grid;
        var visited = new HashSet<Tile>();
        var groups = new List<List<Tile>>();

        foreach (var tile in matched)
        {
            // 방문했던 타일 무시
            if (visited.Contains(tile)) continue;

            var group = new List<Tile>();
            var queue = new Queue<Tile>(); // BFS 탐색용 큐
            queue.Enqueue(tile);
            visited.Add(tile);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                group.Add(current);

                var neighbors = TileMatcher.GetOffsetNeighbors(current.GridPosition.x);
                
                // 6방향 체크
                foreach (var off in neighbors)
                {
                    // 인접 타일 좌표
                    Vector2Int adj = current.GridPosition + off;
                    
                    // 유효하고, 매칭된 목록에 있고, 방문하지 않았고, 같은 색이면
                    if (grid.TryGetValue(adj, out var neighbor) &&
                        matched.Contains(neighbor) &&
                        !visited.Contains(neighbor) &&
                        neighbor.Type == current.Type)
                    {
                        visited.Add(neighbor);
                        // 다음 탐색
                        queue.Enqueue(neighbor);
                    }
                }
            }

            groups.Add(group);
        }

        return groups;
    }


    // 폭탄 주변 영향받는 타일 구하기
    private HashSet<Tile> GetBombExplosion(Tile bomb)
    {
        var result = new HashSet<Tile> { bomb };
        var grid = gridManager.Grid;

        // 중심 타일도 포함
        result.Add(bomb);

        var offsets = GetOffsetNeighbors(bomb.GridPosition.x);
        foreach (var offset in offsets)
        {
            Vector2Int adj = bomb.GridPosition + offset;
            if (grid.TryGetValue(adj, out var neighbor))
            {
                result.Add(neighbor);
            }
        }

        return result;
    }


    // 교환 가능한 두 타일 구하기 (힌트 및 보드 재배열 여부에서 사용)
    public bool TryFindFirstValidSwap(Dictionary<Vector2Int, Tile> grid, out Tile tileA, out Tile tileB)
    {
        tileA = null;
        tileB = null;

        var positions = grid.Keys.ToList();

        foreach (var pos in positions)
        {
            if (!grid.TryGetValue(pos, out var tA)) continue;
            if (tA.Type == TileType.Egg) continue;

            var neighbors = GetOffsetNeighbors(pos.x);
            foreach (var dir in neighbors)
            {
                Vector2Int nPos = pos + dir;
                if (!grid.TryGetValue(nPos, out var tB)) continue;
                if (tB.Type == TileType.Egg) continue;

                // 시뮬레이션용 그리드 생성
                var simulatedGrid = new Dictionary<Vector2Int, Tile>(grid);

                // 스왑
                simulatedGrid[pos] = tB;
                simulatedGrid[nPos] = tA;

                // 타일 좌표도 시뮬레이션
                var originalPosA = tA.GridPosition;
                var originalPosB = tB.GridPosition;

                tA.GridPosition = nPos;
                tB.GridPosition = pos;

                // 매칭 탐색
                var matches = FindMatches(simulatedGrid);

                // 원상 복구
                tA.GridPosition = originalPosA;
                tB.GridPosition = originalPosB;

                if (matches.Count >= 3)
                {
                    tileA = tA;
                    tileB = tB;
                    return true;
                }
            }
        }

        return false;
    }

}
