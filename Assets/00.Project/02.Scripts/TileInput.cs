using System.Linq;
using UnityEngine;


/// <summary>
/// 타일의 유저 Input 관리 클래스
/// </summary>

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

        // 그리드 세팅 중 또는 타일의 상태 변경상태일 때는 오류 방지를 위해 return 
        if (GameManager.Instance.IsInputBlocked || GameManager.Instance.IsSwapping)
            return;


        // 1) 클릭 시 선택
        if (Input.GetMouseButtonDown(0))
        {
            // 힌트가 있다면 힌트 오브젝트 비활성화, 자동힌트 시간 초기화
            GameManager.Instance.hint.ClearHint();
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

            // 마우스 포지션 월드좌표 변환
            Vector2 release = _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dragDir = (release - (Vector2)_selected.transform.position).normalized;

            // 선택한 타일 주변의 이웃타일들만 가져오기. 
            // (_neigborThreshold는 헥사 이웃 거리 허용범위로 그 이하인 타일만 필터링)
            var candidates = _gm.Grid.Values
                .Where(t => t != _selected
                         && Vector2.Distance(t.transform.position,
                                            _selected.transform.position)
                            <= _neighborThreshold)
                .ToList();


            // 주변 스왑가능한 이웃이 없으면 그냥 반환
            if (candidates.Count == 0)
            {
                _selected = null;
                return;
            }


            // 드래그 끝냈을 때 가장 드래그 방향에 맞는 이웃 타일 찾기
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

            // 실행 직전 게임 상태 확인 후 스왑 진행
            if (bestTile != null && !GameManager.Instance.IsInputBlocked)
            {
                StartCoroutine(GameManager.Instance.SwapAndMatch(_selected, bestTile));
            }

            _selected = null;
        }
    }
}
