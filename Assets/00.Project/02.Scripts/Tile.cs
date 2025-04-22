using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int GridPosition { get; set; }
    public TileType Type { get; private set; }

    private SpriteRenderer spriteRenderer;

    public void Initialize(TileType type, Vector2Int gridPos, Sprite sprite)
    {
        Type = type;
        GridPosition = gridPos;

        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
    }

    public void SetType(TileType newType, Sprite newSprite)
    {
        Type = newType;
        spriteRenderer.sprite = newSprite;
    }
}
