using UnityEngine;
using DG.Tweening;

public class LoadingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform[] dots;
    [SerializeField] private RectTransform logoCircle;
    [SerializeField] private RectTransform loadingText;

    [Header("Settings")]
    [SerializeField] private float loadDuration = 3f;

    private void Start()
    {
        // Wooden board pop-in (0.7 se 1)
        if (logoCircle != null)
        {
            logoCircle.localScale = Vector3.one * 0.7f;
            logoCircle.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        }

        // Flowers (dots) bounce — staggered, softer feel
        for (int i = 0; i < dots.Length; i++)
        {
            dots[i].DOScale(0.4f, 0.4f)
                .SetDelay(i * 0.22f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        // Loading text halka sa pulse
        if (loadingText != null)
        {
            loadingText.DOScale(1.06f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        // Duration ke baad HomeScene load
        DOTween.Sequence()
            .AppendInterval(loadDuration)
            .AppendCallback(() =>
            {
                if (TransitionManager.Instance != null)
                    TransitionManager.Instance.LoadScene("HomeScene");
            });
    }
}