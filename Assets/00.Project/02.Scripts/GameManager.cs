using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GridManager gridManager;
    [SerializeField] private TileMatcher tileMatcher;
    [SerializeField] private TileSpawner tileSpawner;

    private void Awake() => Instance = this;
    private void Start()
    {
        gridManager.GenerateGrid();
        StartCoroutine(tileSpawner.FillEmptyTiles());
        StartCoroutine(CheckMatches());
    }

    private IEnumerator CheckMatches()
    {
        while (true)
        {
            var matches = tileMatcher.FindMatches(gridManager.Grid);
            if (matches.Count == 0) yield break;
            tileMatcher.ClearMatches(matches);
            yield return StartCoroutine(tileSpawner.FillEmptyTiles());
        }
    }

    public IEnumerator SwapAndMatch(Tile a, Tile b)
    {
        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;
        StartCoroutine(tileSpawner.MoveTo(a.transform, posB, tileSpawner.swapDuration));
        StartCoroutine(tileSpawner.MoveTo(b.transform, posA, tileSpawner.swapDuration));
        yield return new WaitForSeconds(tileSpawner.swapDuration);

        SwapGrid(a, b);

        var matches = tileMatcher.FindMatches(gridManager.Grid);
        if (matches.Count > 0)
        {
            // 1. 매칭된 타일 제거
            tileMatcher.ClearMatches(matches);
            yield return new WaitForSeconds(tileSpawner.swapDuration);

            // 2. 떨어뜨리고 연쇄 매치까지 모두 처리
            yield return StartCoroutine(tileSpawner.FillEmptyTiles());
            yield return StartCoroutine(CheckMatches());
        }
        else
        {
            StartCoroutine(tileSpawner.MoveTo(a.transform, posA, tileSpawner.swapDuration));
            StartCoroutine(tileSpawner.MoveTo(b.transform, posB, tileSpawner.swapDuration));
            yield return new WaitForSeconds(tileSpawner.swapDuration);
            SwapGrid(a, b);
        }
    }

    private void SwapGrid(Tile a, Tile b)
    {
        var grid = gridManager.Grid;
        Vector2Int pa = a.GridPosition;
        Vector2Int pb = b.GridPosition;
        grid[pa] = b;
        grid[pb] = a;
        a.GridPosition = pb;
        b.GridPosition = pa;
    }
}
