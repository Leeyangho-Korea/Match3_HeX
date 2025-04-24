using System.Collections;
using UnityEngine;

public class SpinningTopObstacle : MonoBehaviour, IObstacle
{
    private int hitCount = 0;
    private const int requiredHits = 2;
    private Tile tile;
    private SpriteRenderer sr;

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
        // float factor = 1f - 0.5f * hitCount;
        // sr.color = new Color(factor, factor, factor, 1f);
        sr.color = Color.white;
    }

    public IEnumerator PlayDestroyEffect()
    {
        yield return StartCoroutine(tile.PlayDestroyAnimation());
        gameObject.SetActive(false);
    }

}
