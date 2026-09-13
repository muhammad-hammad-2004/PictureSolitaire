using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Image fadeImage;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.35f;

    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(fadeImage.canvas.rootCanvas.gameObject);
    }

    private void Start()
    {
        fadeImage.raycastTarget = false;
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0f);
    }

    public void LoadScene(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        isTransitioning = true;
        fadeImage.raycastTarget = true;

        fadeImage.DOFade(1f, fadeDuration).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(fadeDuration);

        SceneManager.LoadScene(sceneName);
        yield return null;

        fadeImage.DOFade(0f, fadeDuration).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(fadeDuration);

        fadeImage.raycastTarget = false;
        isTransitioning = false;
    }
}