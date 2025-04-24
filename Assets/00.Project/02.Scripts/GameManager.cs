using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GridManager gridManager;
    public TileMatcher tileMatcher;
    public ObstacleManager obstacleManager;
    [SerializeField] private TileSpawner tileSpawner;
    [SerializeField] private Hint hint;

    // 입력 차단 카운터 방식
    private int _blockCounter = 0;
    public bool IsInputBlocked => _blockCounter > 0;

    public void BlockInput(bool block)
    {
        if (block)
            _blockCounter++;
        else
            _blockCounter = Mathf.Max(0, _blockCounter - 1);

        Debug.Log($"[GameManager] Input Block Count: {_blockCounter}");
    }

    private void Awake() => Instance = this;

    private void Start()
    {
        gridManager.GenerateGrid();
        StartCoroutine(GameFlow());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            hint.ShowHint(); // ShowHint 내에서 BlockInput(true)~false 처리
        }
    }

    private IEnumerator GameFlow()
    {
        BlockInput(true);
        yield return StartCoroutine(tileSpawner.FillEmptyTiles());
        yield return StartCoroutine(CheckMatches());
        BlockInput(false);
    }

    private IEnumerator CheckMatches()
    {
        BlockInput(true);

        while (true)
        {
            var matches = tileMatcher.FindMatches(gridManager.Grid);
            if (matches.Count == 0) break;

            // 타일 제거
            yield return StartCoroutine(tileMatcher.ClearMatches(matches));

            // 타일 채우기
            yield return StartCoroutine(tileSpawner.FillEmptyTiles());
        }

        BlockInput(false);
    }
    private bool _isSwapping = false;
    public bool IsSwapping => _isSwapping;
    public IEnumerator SwapAndMatch(Tile a, Tile b)
    {
        if (_isSwapping)
            yield break; // 중복 실행 방지

        _isSwapping = true;
        BlockInput(true);

        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;

        StartCoroutine(tileSpawner.MoveTo(a.transform, posB, tileSpawner.swapDuration));
        StartCoroutine(tileSpawner.MoveTo(b.transform, posA, tileSpawner.swapDuration));
        yield return new WaitForSeconds(tileSpawner.swapDuration);

        SwapGrid(a, b);

        var matches = tileMatcher.FindMatches(gridManager.Grid);
        if (matches.Count > 0)
        {
            yield return StartCoroutine(CheckMatches());
        }
        else
        {
            StartCoroutine(tileSpawner.MoveTo(a.transform, posA, tileSpawner.swapDuration));
            StartCoroutine(tileSpawner.MoveTo(b.transform, posB, tileSpawner.swapDuration));
            yield return new WaitForSeconds(tileSpawner.swapDuration);
            SwapGrid(a, b);
        }

        BlockInput(false);
        _isSwapping = false;
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
