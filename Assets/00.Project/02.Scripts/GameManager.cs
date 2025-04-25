using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] CanvasGroup fadeCanvasGroup;
    public GridManager gridManager;
    public TileMatcher tileMatcher;
    public ObstacleManager obstacleManager;
    [SerializeField] private TileSpawner _tileSpawner;
    public Hint hint;

    [Header("UI")]
    [SerializeField] Text _text_Heart;
    [SerializeField] Text _text_Time;
    [SerializeField] Text _text_Tile;
    // 시간 표현
    private float elapsedTime = 0f;
    private float _timeUpdateTick = 0f;

    // 시작했는지?
    private bool isStart = false;
    // 힌트 표현
    private float _timeSinceLastMatch = 0f;
    private const float autoHintDelay = 3f;
    private bool _waitingForAutoHint = false;

    private bool _isFilling = false; // 타일이 채워지는 중인지
    public bool IsFilling { get => _isFilling; set => _isFilling = value; } 

    // 입력 차단 카운터 방식
    private int _blockCounter = 0;
    public bool IsInputBlocked => _blockCounter > 0;

    bool _reshuffleBoard = false; //보드 셔플중인지
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
        FadeManager.Instance.fadeCanvasGroup = fadeCanvasGroup;
        gridManager.GenerateGrid();
        StartCoroutine(GameStartSequence());
    }

    private void Update()
    {
        if (_reshuffleBoard || isStart == false)
        {
            return; // 보드 셔플 중에는 업데이트 하지 않음
        }

        elapsedTime += Time.deltaTime;
        _timeUpdateTick += Time.deltaTime;

        if(IsFilling == false)
            _timeSinceLastMatch += Time.deltaTime;

        if (_timeUpdateTick >= 1f)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            _text_Time.text = $"{minutes:D2}:{seconds:D2}";
            _timeUpdateTick = 0f;
        }

        //  자동 힌트
        if (_timeSinceLastMatch >= autoHintDelay && !_waitingForAutoHint && IsFilling == false)
        {
            _waitingForAutoHint = true;
            StartCoroutine(AutoHint());
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
           StartCoroutine(ReshuffleBoard());
        }
        else if(Input.GetKeyDown(KeyCode.H))
        {
            StartCoroutine(AutoHint());
        }
    }

    private IEnumerator GameStartSequence()
    {
        BlockInput(true);

        yield return StartCoroutine(FadeManager.Instance.FadeOut());

        // 준비 ~ 시작 애니메이션
        yield return StartCoroutine(PlayReadyStartUI());


        yield return StartCoroutine(_tileSpawner.FillEmptyTiles());
        yield return StartCoroutine(CheckMatches());
        isStart = true;
        BlockInput(false);
    }

    [SerializeField] private RectTransform readyText;
    [SerializeField] private RectTransform goText;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float holdTime = 0.8f;

    private IEnumerator PlayReadyStartUI()
    {
        yield return StartCoroutine(PlayTextZoom(readyText));
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(PlayTextZoom(goText));
        yield return new WaitForSeconds(0.3f);
    }
    private IEnumerator PlayTextZoom(RectTransform target)
    {
        TextMeshProUGUI textComp = target.GetComponent<TextMeshProUGUI>();

        target.gameObject.SetActive(true);

        float duration = 1.5f;
        float elapsed = 0f;

        Vector3 initialScale = Vector3.zero;
        Vector3 peakScale = Vector3.one * 1.5f;
        Vector3 finalScale = Vector3.zero;

        // 커지기
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            target.localScale = Vector3.Lerp(initialScale, peakScale, t);
            yield return null;
        }

        elapsed = 0f;

        // 작아지기
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            target.localScale = Vector3.Lerp(peakScale, finalScale, t);
            yield return null;
        }

        target.localScale = finalScale;
        target.gameObject.SetActive(false);
    }

    private IEnumerator SlideRectTransform(RectTransform rt, Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rt.anchoredPosition = Vector2.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        rt.anchoredPosition = to;
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

            // 기존 힌트 애니메이션 중단 → Glow 힌트 제거로 변경
            hint.ClearHint();

            yield return StartCoroutine(tileMatcher.ClearMatches(matches));
            yield return StartCoroutine(_tileSpawner.FillEmptyTiles());
        }

        if (!tileMatcher.TryFindFirstValidSwap(gridManager.Grid, out _, out _))
        {
            yield return StartCoroutine(ReshuffleBoard());
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

        // 스왑한 두 타일이 모두 폭탄이면 특수 폭발 처리
        if (a.IsBomb && b.IsBomb)
        {
            {
                hint.ClearHint();
                UpdateInteraction(); // 힌트 초기화
            }
            yield return StartCoroutine(TriggerDoubleBombExplosion(a, b));
            BlockInput(false);
            _isSwapping = false;
            yield break;
        }



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

    // 타일 재셔플
    [SerializeField] private GameObject _info_Shuffle;

    private IEnumerator ReshuffleBoard()
    {
        Debug.Log("[Reshuffle]");
        BlockInput(true);
         _reshuffleBoard = true;

        // 힌트가 있을 때 보드 초기화 확률은 없지만 이중 방지
        {
            hint.ClearHint();
            UpdateInteraction(); // 힌트 초기화
        }

        var grid = gridManager.Grid;

        yield return new WaitForSeconds(0.2f);

        _info_Shuffle.gameObject.SetActive(true);
        
        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(ShakeBoardEffect(0.4f, 0.1f)); // (지속 시간, 강도)

        // 1. 기존 타일 중 egg 외 제거
        foreach (var kv in grid.ToList())
        {
            var tile = kv.Value;

            if (tile.Type != TileType.Egg && tile.IsBomb == false)
            {
             TileType type = (TileType)Random.Range(0, 6);
                tile.SetType(type, TilePool.Instance.GetSprite(type));
            }
        }

        yield return new WaitForSeconds(1f);
        _info_Shuffle.gameObject.SetActive(false);
        _reshuffleBoard = false;
        yield return null;

        yield return StartCoroutine(CheckMatches());
        BlockInput(false);
    }


    private IEnumerator ShakeBoardEffect(float duration, float magnitude)
    {
        Transform board = gridManager.transform; // 또는 tile container parent
        Vector3 originalPos = board.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;

            board.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        board.localPosition = originalPos;
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
        hint.ShowHint();
        _timeSinceLastMatch = 0f;
        _waitingForAutoHint = false;
    }

    // 인터랙션이 있을 때 힌트 동작 X
    public void UpdateInteraction()
    {
        _timeSinceLastMatch = 0f;
        _waitingForAutoHint = false;
    }

    // 스페셜 폭발기능
    private IEnumerator TriggerDoubleBombExplosion(Tile a, Tile b)
    {
        var grid = gridManager.Grid;

        var explosionTiles = GetChainedExplosionTiles(a, b, 2, 1); // 스페셜 : 2링, 그 안에 있는 폭탄은 1링

        // 애니메이션
        float duration = 0.35f;
        foreach (var tile in explosionTiles)
        {
            if (tile.Type != TileType.Egg)
            {
                StartCoroutine(tile.PlayDestroyAnimation(duration));
            }
        }

        // 알에게도 영향 주기
        var hitObstacles = obstacleManager.NotifyNearbyMatches(explosionTiles);

        foreach (var obs in hitObstacles)
        {
            if (obs.GetComponent<IObstacle>()?.IsRemovable == true)
            {
                StartCoroutine(obs.GetComponent<IObstacle>().PlayDestroyEffect());
            }
        }

        yield return new WaitForSeconds(duration);

        // 제거 처리
        foreach (var tile in explosionTiles)
        {
            if (tile.Type != TileType.Egg)
            {
                grid.Remove(tile.GridPosition);
                tile.SetBomb(false, null); // bomb 제거
                tile.gameObject.SetActive(false);
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

        AddTile(explosionTiles.Count(t => t.Type != TileType.Egg));
        AddHeart(hitObstacles.Count(t =>
            t.Type == TileType.Egg &&
            t.GetComponent<IObstacle>()?.IsRemovable == true));

        yield return StartCoroutine(_tileSpawner.FillEmptyTiles());
        yield return StartCoroutine(CheckMatches());
    }
    // 스페셜 폭발
    private HashSet<Tile> GetExplosionTiles(Tile center, int radius)
    {
        var result = new HashSet<Tile>();
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<(Vector2Int pos, int depth)>();

        var grid = gridManager.Grid;

        queue.Enqueue((center.GridPosition, 0));
        visited.Add(center.GridPosition);

        while (queue.Count > 0)
        {
            var (currentPos, depth) = queue.Dequeue();

            if (grid.TryGetValue(currentPos, out var tile))
                result.Add(tile);

            if (depth >= radius)
                continue;

            var offsets = TileMatcher.GetOffsetNeighbors(currentPos.x);
            foreach (var off in offsets)
            {
                var next = currentPos + off;
                if (visited.Add(next)) // add는 중복이면 false라서 한 번만 들어감
                {
                    queue.Enqueue((next, depth + 1));
                }
            }
        }

        return result;
    }

    // 스페셜 폭발 반경의 폭발 반경
    private HashSet<Tile> GetChainedExplosionTiles(Tile a, Tile b, int firstRadius, int chainRadius)
    {
        var result = new HashSet<Tile>();
        var visitedBombs = new HashSet<Tile>();
        var queue = new Queue<(Tile tile, int radius)>();

        // 처음 폭탄 두 개는 firstRadius로 시작 (e.g. 2)
        queue.Enqueue((a, firstRadius));
        queue.Enqueue((b, firstRadius));

        while (queue.Count > 0)
        {
            var (center, radius) = queue.Dequeue();

            if (!visitedBombs.Add(center)) continue;

            var area = GetExplosionTiles(center, radius);
            result.UnionWith(area);

            // 그 안에 있는 추가 폭탄 → chainRadius(=1)로 확장
            foreach (var tile in area)
            {
                if (tile.IsBomb && !visitedBombs.Contains(tile))
                    queue.Enqueue((tile, chainRadius));
            }
        }

        return result;
    }
}
