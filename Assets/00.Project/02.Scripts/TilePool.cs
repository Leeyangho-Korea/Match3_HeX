using System.Collections.Generic;
using UnityEngine;

public class TilePool : MonoBehaviour
{
    public static TilePool Instance;

    [Header("Tile Prefabs")]
    [SerializeField] private GameObject tilePrefab;

    [Header("Tile Settings")]
    [SerializeField] private Sprite[] tileSprites;
    [SerializeField] private int initialSize = 100;
    [SerializeField] private int spinningTopLimit = 10;

    private int currentSpinningTopCount = 0;
    private Queue<Tile> tilePool = new Queue<Tile>();

    private void Awake()
    {
        Instance = this;
        FillPool();
    }

    private void FillPool()
    {
        for (int i = 0; i < initialSize; i++)
        {
            GameObject tileObj = Instantiate(tilePrefab);
            tileObj.SetActive(false);
            tileObj.transform.SetParent(transform);
            tilePool.Enqueue(tileObj.GetComponent<Tile>());
        }
    }

    public Tile GetTile(Vector2Int gridPos)
    {
        if (tilePool.Count == 0)
            FillPool();

        Tile tile = tilePool.Dequeue();
        tile.gameObject.SetActive(true);

        TileType type;

        // 팽이 개수 제한 + 확률 체크
        bool spawnPeg = currentSpinningTopCount < spinningTopLimit && Random.value < 0.1f;

        if (spawnPeg)
        {
            type = TileType.Heart;
            currentSpinningTopCount++;
            tile.GetComponent<SpriteRenderer>().color = Color.red; // 색상 초기화
            // 팽이 컴포넌트 동적으로 붙이기 (프리팹 따로 안 써도 됨!)
            if (tile.GetComponent<SpinningTopObstacle>() == null)
                tile.gameObject.AddComponent<SpinningTopObstacle>();
        }
        else
        {
            // 일반 타일 타입 선택
            int max = tileSprites.Length;
            type = (TileType)Random.Range(0, max - 1); // 팽이를 enum 마지막에 배치했다는 가정
            tile.GetComponent<SpriteRenderer>().color = Color.white; // 색상 초기화

            // 기존에 붙어있던 컴포넌트 제거 (재사용된 경우 대비)
            var peg = tile.GetComponent<SpinningTopObstacle>();
            if (peg != null)
                Destroy(peg);
        }

        // 위치 설정 포함 초기화 (꼭 필요!)
        tile.Initialize(type, gridPos, tileSprites[(int)type]);
        tile.name = type.ToString() + "_" + gridPos.x + "_" + gridPos.y;
        tile.transform.position = GameManager.Instance.gridManager.GetTileWorldPosition(gridPos.x, gridPos.y);

        return tile;
    }

    public void ReturnTile(Tile tile)
    {
        if (tile.Type == TileType.Heart)
        {
            currentSpinningTopCount--;
        }

        tile.gameObject.SetActive(false);
        tile.transform.SetParent(transform);
        tilePool.Enqueue(tile);
    }
}
