using System.Collections;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int GridPosition { get; set; }
    public TileType Type { get; private set; }

    private SpriteRenderer spriteRenderer;
    public bool IsHintAnimating { get; private set; } = false;
    private Coroutine _hintRoutine;

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

    // 매칭 제거 애니메이션
    public IEnumerator PlayDestroyAnimation(float duration = 0.3f)
    {
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        //  초기값 한 번 반영
        transform.localScale = originalScale * 1.5f;
        sr.color = new Color(1f, 1f, 1f, 1f);

        yield return null; // 첫 프레임 적용 후 계속 애니메이션

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.localScale = originalScale * (1f + 0.5f * Mathf.Sin(Mathf.PI * t));
            sr.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        transform.localScale = originalScale;
        sr.color = new Color(1f, 1f, 1f, 0f);
    }

    // 힌트 애니메이션
    private Vector3 originalPosition;

    public IEnumerator PlayHintAnimation(float duration = 0.4f, float amplitude = 0.1f, int frequency = 4)
    {
        if (IsHintAnimating)
            yield break;

        IsHintAnimating = true;
        originalPosition = transform.position;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            float offset = Mathf.Sin(percent * frequency * 2 * Mathf.PI) * amplitude;
            transform.position = originalPosition + Vector3.right * offset;
            yield return null;
        }

        transform.position = originalPosition;
        IsHintAnimating = false;
    }

    public void StopHintAnimation()
    {
        if (!IsHintAnimating) return;

        StopAllCoroutines(); // 안전하게 전체 중단
        transform.position = originalPosition;
        IsHintAnimating = false;
    }

}
