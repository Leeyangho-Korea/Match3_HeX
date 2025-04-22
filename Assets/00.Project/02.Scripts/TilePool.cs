using System.Collections.Generic;
using UnityEngine;

public class TilePool : MonoBehaviour
{
    public static TilePool Instance;

    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Sprite[] tileSprites;
    [SerializeField] private int initialSize = 100;

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

        // 랜덤 타입 지정
        TileType type = (TileType)Random.Range(0, tileSprites.Length);
        tile.Initialize(type, gridPos, tileSprites[(int)type]);

        return tile;
    }

    public void ReturnTile(Tile tile)
    {
        tile.gameObject.SetActive(false);
        tile.transform.SetParent(transform);
        tilePool.Enqueue(tile);
    }
}
