using System.Linq;
using UnityEngine;

public class TileInput : MonoBehaviour
{
    private Tile _selected;
    private Camera _cam;
    private GridManager _gm;
    private float _neighborThreshold;

    private void Start()
    {
        _cam = Camera.main;
        _gm = GameManager.Instance.gridManager;

        // 대각선 이웃까지 커버하도록 반지름 계산 (타일 크기에 맞춰 조정)
        float xOff = _gm.tileWidth * 0.75f;
        float yOff = _gm.tileHeight;
        _neighborThreshold = Mathf.Sqrt(xOff * xOff + yOff * yOff) + 0.05f;
    }

    private void Update()
    {

        if (GameManager.Instance.IsInputBlocked || GameManager.Instance.IsSwapping)
            return;


        // 1) 클릭 시 선택
        if (Input.GetMouseButtonDown(0))
        {
            GameManager.Instance.UpdateInteraction();
            Vector2 wp = _cam.ScreenToWorldPoint(Input.mousePosition);
            var col = Physics2D.OverlapPoint(wp);
            if (col != null) _selected = col.GetComponent<Tile>();
        }

        // 2) 드래그 후 릴리즈 시
        if (Input.GetMouseButtonUp(0) && _selected != null)
        {
            //  다시 한 번 차단 검사
            if (GameManager.Instance.IsInputBlocked || GameManager.Instance.IsSwapping)
            {
                _selected = null;
                return;
            }

            Vector2 release = _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dragDir = (release - (Vector2)_selected.transform.position).normalized;

            var candidates = _gm.Grid.Values
                .Where(t => t != _selected
                         && Vector2.Distance(t.transform.position,
                                            _selected.transform.position)
                            <= _neighborThreshold)
                .ToList();

            if (candidates.Count == 0)
            {
                _selected = null;
                return;
            }

            float bestDot = float.NegativeInfinity;
            Tile bestTile = null;
            foreach (var t in candidates)
            {
                Vector2 dir = ((Vector2)t.transform.position
                              - (Vector2)_selected.transform.position).normalized;
                float dot = Vector2.Dot(dragDir, dir);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestTile = t;
                }
            }

            // 💥 한번 더: 실행 직전 차단 확인
            if (bestTile != null && !GameManager.Instance.IsInputBlocked)
            {
                StartCoroutine(GameManager.Instance.SwapAndMatch(_selected, bestTile));
            }

            _selected = null;
        }
    }
}
