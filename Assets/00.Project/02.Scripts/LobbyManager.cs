using System.Collections;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 로비 씬 관리 클래스
/// </summary>
public class LobbyManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text messageText;
    [SerializeField] private string nextSceneName = "GameScene";

    [Header("Timing")]
    [SerializeField] private float blinkSpeed = 0.7f;

    private bool isReadyToStart = false;
    private bool sceneLoading = false;

    private void Start()
    {
        FadeManager.Instance.fadeCanvasGroup = canvasGroup;
        StartCoroutine(InitializeLobby());
    }

    private IEnumerator InitializeLobby()
    {
        // 1. 페이드인
        yield return StartCoroutine(FadeManager.Instance.FadeIn());

        // 2. 텍스트 깜빡이기 시작
        isReadyToStart = true;
        StartCoroutine(BlinkMessage());
    }

    private void Update()
    {
        if (!isReadyToStart || sceneLoading)
            return;

        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            sceneLoading = true;
            FadeManager.Instance.TransitionToScene(nextSceneName);
        }
    }

    private IEnumerator BlinkMessage()
    {
        Color originalColor = messageText.color;

        while (true)
        {
            // 보이게
            messageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
            yield return new WaitForSeconds(blinkSpeed);

            // 안보이게
            messageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            yield return new WaitForSeconds(blinkSpeed);
        }
    }

}
