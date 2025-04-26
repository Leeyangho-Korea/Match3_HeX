using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 힌트 관리 클래스
/// </summary>
public class Hint : MonoBehaviour
{
    private TileMatcher _matcher => GameManager.Instance.tileMatcher;
    private List<GameObject> activeGlows = new();


    public void ShowHint()
    {
        var grid = GameManager.Instance.gridManager.Grid;

        if (!_matcher.TryFindFirstValidSwap(grid, out var tileA, out var tileB))
            return;

        // 시뮬레이션 후 매칭 타일 목록 얻기
        var simulatedGrid = new Dictionary<Vector2Int, Tile>(grid);
        simulatedGrid[tileA.GridPosition] = tileB;
        simulatedGrid[tileB.GridPosition] = tileA;

        var matches = _matcher.FindMatches(simulatedGrid);
        if (matches.Count == 0) return;

        // 힌트 재사용 로직
        int needed = matches.Count;
        int existing = activeGlows.Count;

        // 부족한 수만큼만 생성
        for (int i = existing; i < needed; i++)
        {
            var glow = GlowPool.Instance.GetGlow();
            glow.SetActive(false); // 일단 비활성 상태로 가져옴
            activeGlows.Add(glow);
        }

        // 필요한 만큼만 배치 및 활성화
        for (int i = 0; i < needed; i++)
        {
            var glow = activeGlows[i];
            var tile = matches[i];

            glow.transform.localPosition = tile.transform.position;
            glow.SetActive(true);
        }

        // 남는 Glow는 비활성화만 (풀에는 안 돌려도 됨 — 재사용 가능하므로)
        for (int i = needed; i < activeGlows.Count; i++)
        {
            activeGlows[i].SetActive(false);
        }
    }

    public void ClearHint()
    {
        foreach (var glow in activeGlows)
        {
            GlowPool.Instance.ReturnGlow(glow);
        }
        activeGlows.Clear();
    }


}
