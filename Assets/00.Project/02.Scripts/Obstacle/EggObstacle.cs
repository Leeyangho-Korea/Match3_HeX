using System.Collections;
using UnityEngine;

public class EggObstacle : MonoBehaviour, IObstacle
{
    private int hitCount = 0;
    private const int requiredHits = 2;
    private Tile tile;
    private SpriteRenderer sr;

    Sprite crackEgg => GameManager.Instance.obstacleManager.eggSprite[0];
    Sprite brokeEgg => GameManager.Instance.obstacleManager.eggSprite[1];

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        tile = GetComponent<Tile>();
    }

    public void OnNearbyMatch()
    {
        hitCount++;
        UpdateVisual();
    }

    public bool IsRemovable => hitCount >= requiredHits;

    private void UpdateVisual()
    {
        sr.sprite = crackEgg;
    }

    public IEnumerator PlayDestroyEffect()
    {
        sr.sprite = brokeEgg;
        yield return StartCoroutine(tile.PlayDestroyAnimation());
        gameObject.SetActive(false);
    }

}
