using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스플래시 관리 클래스
/// </summary>
public class Splash : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [Header("Splash Timing")]
    [SerializeField] private float splashDuration = 2.5f; // 로고 보여지는 시간
    [SerializeField] private string nextSceneName = "MainScene"; // 다음 씬 이름
    [SerializeField] private Text _text_version;
    private void Start()
    {
        _text_version.text = "Version " + Application.version;
        StartCoroutine(PlaySplashSequence());
    }

    private IEnumerator PlaySplashSequence()
    {
        // 1. 페이드 인 (화면 밝아짐)
        yield return StartCoroutine(FadeManager.Instance.FadeIn());

        // 2. 로고 노출 시간 유지
        yield return new WaitForSeconds(splashDuration);

        // 3. 다음 씬으로 자연스럽게 전환 (페이드 아웃 + 로딩 포함)
        FadeManager.Instance.TransitionToScene(nextSceneName);
    }
}
