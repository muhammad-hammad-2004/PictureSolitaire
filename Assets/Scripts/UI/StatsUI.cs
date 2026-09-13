using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class StatsUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject trophyPanel;
    [SerializeField] private RectTransform statsCard;
    [SerializeField] private Image dimImage;
    [SerializeField] private Button trophyButton;
    [SerializeField] private Button closeButton;

    [Header("Values")]
    [SerializeField] private TextMeshProUGUI levelsValue;
    [SerializeField] private TextMeshProUGUI dailiesValue;
    [SerializeField] private TextMeshProUGUI progressValue;
    [SerializeField] private Image progressFill;

    private int TotalLevels => LevelConfig.TotalLevels;
    private CanvasGroup cardGroup;

    private void Start()
    {
        cardGroup = statsCard.GetComponent<CanvasGroup>();
        if (cardGroup == null)
            cardGroup = statsCard.gameObject.AddComponent<CanvasGroup>();

        trophyButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Close);
    }

    private void Open()
    {
        AudioManager.Instance.PlayClick();

        int total = TotalLevels;
        int completed = 0;
        for (int i = 1; i <= total; i++)
            if (SaveSystem.Instance.IsLevelCompleted(i)) completed++;

        int dailies = PlayerPrefs.GetInt("DailyTotal", 0);
        float progress = completed / (float)total;

        levelsValue.text = completed + " / " + total;
        dailiesValue.text = dailies.ToString();
        progressValue.text = Mathf.RoundToInt(progress * 100f) + "%";

        progressFill.fillAmount = 0f;
        trophyPanel.SetActive(true);

        cardGroup.alpha = 1f;
        statsCard.localScale = Vector3.one * 0.8f;
        statsCard.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
        dimImage.DOFade(0.59f, 0.25f).From(0f);

        progressFill.DOFillAmount(progress, 0.8f).SetDelay(0.3f).SetEase(Ease.OutQuad);
    }

    private void Close()
    {
        AudioManager.Instance.PlayClick();
        statsCard.DOScale(0.85f, 0.25f).SetEase(Ease.InQuad);
        cardGroup.DOFade(0f, 0.25f).SetEase(Ease.InQuad);
        dimImage.DOFade(0f, 0.25f).OnComplete(() =>
        {
            trophyPanel.SetActive(false);
            dimImage.color = new Color(0f, 0f, 0f, 0.59f);
            cardGroup.alpha = 1f;
        });
    }
}