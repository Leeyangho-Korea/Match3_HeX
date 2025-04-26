using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타일 풀링으로 관리하는 클래스
/// </summary>

public class TilePool : MonoBehaviour
{
    public static TilePool Instance;

    [Header("Tile Prefabs")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform tileParent;
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
            tileObj.transform.SetParent(tileParent);
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

        // 장애물 개수 제한 + 확률 체크
        bool spawnPeg = currentSpinningTopCount < spinningTopLimit && Random.value < 0.1f;

        if (spawnPeg)
        {
            type = TileType.Egg;
            currentSpinningTopCount++;

            //  컴포넌트 동적으로 생성
            if (tile.GetComponent<EggObstacle>() == null)
                tile.gameObject.AddComponent<EggObstacle>();
        }
        else
        {
            // 일반 타일 타입 선택
            int max = tileSprites.Length;
            type = (TileType)Random.Range(0, 6);

       
            // 기존에 붙어있던 컴포넌트 제거 (재사용된 경우 대비)
            var peg = tile.GetComponent<EggObstacle>();
            if (peg != null)
                Destroy(peg);
        }

        // 위치 설정 포함 초기화
        tile.Initialize(type, gridPos, tileSprites[(int)type]);
        tile.GetComponent<SpriteRenderer>().color = Color.white; // 색상 초기화
        tile.SetBomb(false, null);
        tile.name = type.ToString() + "_" + gridPos.x + "_" + gridPos.y;
        tile.transform.position = GameManager.Instance.gridManager.GetTileWorldPosition(gridPos.x, gridPos.y);

        return tile;
    }

    public void ReturnTile(Tile tile)
    {
        if (tile.Type == TileType.Egg)
        {
            currentSpinningTopCount--;
        }

        tile.gameObject.SetActive(false);
        tile.transform.SetParent(transform);
        tilePool.Enqueue(tile);
    }

    public Sprite GetSprite(TileType type)
    {
        return tileSprites[(int)type];
    }

    public Sprite GetBombSprite(TileType tileType)
    {
        Sprite sprite = null;
        switch (tileType)
        {
            case TileType.Blue:
                sprite = tileSprites[(int)TileType.BlueBomb];
                break;
            case TileType.Green:
                sprite = tileSprites[(int)TileType.GreenBomb];
                break;
            case TileType.Orange:
                sprite = tileSprites[(int)TileType.OrangeBomb];
                break;
            case TileType.Purple:
                sprite = tileSprites[(int)TileType.PurpleBomb];
                break;
            case TileType.Red:
                sprite = tileSprites[(int)TileType.RedBomb];
                break;
            case TileType.Yellow:
                sprite = tileSprites[(int)TileType.YellowBomb];
                break;
        }

        return sprite;
    }
}
