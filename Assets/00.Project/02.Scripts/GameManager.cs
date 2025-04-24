using System.Collections;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GridManager gridManager;
    public TileMatcher tileMatcher;
    public ObstacleManager obstacleManager;
    [SerializeField] private TileSpawner _tileSpawner;
    [SerializeField] private Hint _hint;

    [Header("UI")]
    [SerializeField] Text _text_Heart;
    [SerializeField] Text _text_Time;
    [SerializeField] Text _text_Tile;
    // 시간 표현
    private float elapsedTime = 0f;
    private float _timeUpdateTick = 0f;

    // 힌트 표현
    private float _timeSinceLastMatch = 0f;
    private const float autoHintDelay = 5f;
    private bool _waitingForAutoHint = false;

    // 입력 차단 카운터 방식
    private int _blockCounter = 0;
    public bool IsInputBlocked => _blockCounter > 0;

    public void BlockInput(bool block)
    {
        if (block)
            _blockCounter++;
        else
            _blockCounter = Mathf.Max(0, _blockCounter - 1);

        //Debug.Log($"[GameManager] Input Block Count: {_blockCounter}");
    }

    private void Awake() => Instance = this;

    private void Start()
    {
        gridManager.GenerateGrid();
        StartCoroutine(GameFlow());
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        _timeUpdateTick += Time.deltaTime;
        _timeSinceLastMatch += Time.deltaTime;

        if (_timeUpdateTick >= 1f)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            _text_Time.text = $"{minutes:D2}:{seconds:D2}";
            _timeUpdateTick = 0f;
        }

        //  자동 힌트
        if (_timeSinceLastMatch >= autoHintDelay && !_waitingForAutoHint)
        {
            _waitingForAutoHint = true;
            StartCoroutine(AutoHint());
        }
    }

    private IEnumerator GameFlow()
    {
        BlockInput(true);
        yield return StartCoroutine(_tileSpawner.FillEmptyTiles());
        yield return StartCoroutine(CheckMatches());
        BlockInput(false);
    }

    // 매칭 검사
    private IEnumerator CheckMatches()
    {
        BlockInput(true);

        while (true)
        {
            var matches = tileMatcher.FindMatches(gridManager.Grid);
            if (matches.Count == 0) break;

            _timeSinceLastMatch = 0f;
            _waitingForAutoHint = false;

            // 타일 제거
            yield return StartCoroutine(tileMatcher.ClearMatches(matches));
            // 타일 채우기
            yield return StartCoroutine(_tileSpawner.FillEmptyTiles());
        }

        BlockInput(false);
    }

    private bool _isSwapping = false;
    public bool IsSwapping => _isSwapping;
    // 스왑한 후 매칭
    public IEnumerator SwapAndMatch(Tile a, Tile b)
    {
        if (_isSwapping)
            yield break; // 중복 실행 방지

        _isSwapping = true;
        BlockInput(true);

        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;

        StartCoroutine(_tileSpawner.MoveTo(a.transform, posB, _tileSpawner.swapDuration));
        StartCoroutine(_tileSpawner.MoveTo(b.transform, posA, _tileSpawner.swapDuration));
        yield return new WaitForSeconds(_tileSpawner.swapDuration);

        SwapGrid(a, b);

        var matches = tileMatcher.FindMatches(gridManager.Grid);
        if (matches.Count > 0)
        {
            yield return StartCoroutine(CheckMatches());
        }
        else
        {
            StartCoroutine(_tileSpawner.MoveTo(a.transform, posA, _tileSpawner.swapDuration));
            StartCoroutine(_tileSpawner.MoveTo(b.transform, posB, _tileSpawner.swapDuration));
            yield return new WaitForSeconds(_tileSpawner.swapDuration);
            SwapGrid(a, b);
        }

        BlockInput(false);
        _isSwapping = false;
    }

    // 스왑기능
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


    public int tileCount { get; private set; } = 0;
    public int heartCount { get; private set; } = 0;
    public void AddTile(int amount)
    {
        tileCount += amount;
        _text_Tile.text = $"{tileCount}";
    }

    public void AddHeart(int amount)
    {
        heartCount += amount;
        _text_Heart.text = $"{heartCount}";
    }


    private IEnumerator AutoHint()
    {
        yield return new WaitForEndOfFrame(); // 살짝 딜레이
        _hint.ShowHint();
        _timeSinceLastMatch = 0f;
        _waitingForAutoHint = false;
    }
}
