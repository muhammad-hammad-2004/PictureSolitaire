using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SettingsUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private RectTransform settingsCard;
    [SerializeField] private Image dimImage;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Toggles")]
    [SerializeField] private Button musicToggle;
    [SerializeField] private RectTransform musicKnob;
    [SerializeField] private Button soundToggle;
    [SerializeField] private RectTransform soundKnob;

    [Header("Reset")]
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI resetLabel;

    private bool musicOn, soundOn;
    private bool confirmReset;
    private CanvasGroup cardGroup;

    private void Start()
    {
        cardGroup = settingsCard.GetComponent<CanvasGroup>();
        if (cardGroup == null)
            cardGroup = settingsCard.gameObject.AddComponent<CanvasGroup>();

        musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Close);

        musicToggle.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClick();
            musicOn = !musicOn;
            PlayerPrefs.SetInt("MusicOn", musicOn ? 1 : 0);
            PlayerPrefs.Save();
            Refresh();
            AudioManager.Instance.RefreshSettings();
        });

        soundToggle.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClick();
            soundOn = !soundOn;
            PlayerPrefs.SetInt("SoundOn", soundOn ? 1 : 0);
            PlayerPrefs.Save();
            Refresh();
            AudioManager.Instance.RefreshSettings();
        });

        resetButton.onClick.AddListener(OnReset);

        Refresh();
    }

    private void Open()
    {
        AudioManager.Instance.PlayClick();
        confirmReset = false;
        resetLabel.text = "Reset Progress";
        settingsPanel.SetActive(true);

        cardGroup.alpha = 1f;
        settingsCard.localScale = Vector3.one * 0.8f;
        settingsCard.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
        dimImage.DOFade(0.59f, 0.25f).From(0f);
    }

    private void Close()
    {
        AudioManager.Instance.PlayClick();
        settingsCard.DOScale(0.85f, 0.25f).SetEase(Ease.InQuad);
        cardGroup.DOFade(0f, 0.25f).SetEase(Ease.InQuad);
        dimImage.DOFade(0f, 0.25f).OnComplete(() =>
        {
            settingsPanel.SetActive(false);
            dimImage.color = new Color(0f, 0f, 0f, 0.59f);
            cardGroup.alpha = 1f;
        });
    }

    private void OnReset()
    {
        if (!confirmReset)
        {
            confirmReset = true;
            resetLabel.text = "Are You Sure?";
            return;
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        TransitionManager.Instance.LoadScene("HomeScene");
    }

    private void Refresh()
    {
        SetToggle(musicToggle, musicKnob, musicOn);
        SetToggle(soundToggle, soundKnob, soundOn);
    }

    private void SetToggle(Button btn, RectTransform knob, bool on)
    {
        btn.GetComponent<Image>().color = on
            ? new Color32(0x3A, 0xA6, 0x55, 255)
            : new Color32(0x9E, 0x9E, 0x9E, 255);
        knob.anchoredPosition = new Vector2(on ? 35f : -35f, 0f);
    }
}