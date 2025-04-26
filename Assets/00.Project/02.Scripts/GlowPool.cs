using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 힌트용 오브젝트 풀
/// </summary>
public class GlowPool : MonoBehaviour
{
    [SerializeField] private GameObject glowPrefab;
    [SerializeField] private Transform glowParent;
    private Queue<GameObject> pool = new();

    public static GlowPool Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public GameObject GetGlow()
    {
        if (pool.Count > 0)
            return pool.Dequeue();

        return Instantiate(glowPrefab, glowParent);
    }

    public void ReturnGlow(GameObject glow)
    {
        glow.SetActive(false);
        pool.Enqueue(glow);
    }

    public void ClearAll()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
            pool.Enqueue(child.gameObject);
        }
    }
}
