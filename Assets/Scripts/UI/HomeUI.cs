using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class HomeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform levelGrid;
    [SerializeField] private GameObject levelCardPrefab;
    [SerializeField] private TextMeshProUGUI playButtonLabel;
    [SerializeField] private Button playButton;

    [Header("Tabs")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject cardsPanel;
    [SerializeField] private GameObject dailyPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button tabHome;
    [SerializeField] private Button tabCards;
    [SerializeField] private Button tabDaily;
    [SerializeField] private Transform cardsGrid;
    [SerializeField] private GameObject thumbCardPrefab;

    [Header("Daily")]
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private Image dailyPreview;
    [SerializeField] private Button dailyButton;
    [SerializeField] private TextMeshProUGUI dailyButtonLabel;

    [Header("Anim Refs")]
    [SerializeField] private RectTransform settingsBtn;
    [SerializeField] private RectTransform trophyBtn;
    [SerializeField] private RectTransform titleBoard;
    [SerializeField] private RectTransform bottomBar;

    private int totalLevels;
    private int chapter;

    private void Start()
    {
        DailySystem.PlayingDaily = false;

        totalLevels = LevelConfig.TotalLevels;
        chapter = (totalLevels > 0)
            ? Mathf.Clamp(LevelConfig.GetChapter(SaveSystem.Instance.CurrentLevel), 0, LevelConfig.ChapterCount - 1)
            : 0;

        BuildLevelGrid();
        BuildCardsGallery();
        UpdatePlayButton();
        SetupDaily();

        AudioManager.Instance.PlayHomeMusic();

        playButton.onClick.AddListener(() =>
        {
            if (totalLevels <= 0) return;
            AudioManager.Instance.PlayClick();

            if (SaveSystem.Instance.AllLevelsCompleted() && SaveSystem.Instance.CurrentLevel > totalLevels)
                SaveSystem.Instance.SetCurrentLevel(1);

            TransitionManager.Instance.LoadScene("GameScene");
        });
        tabHome.onClick.AddListener(() => { AudioManager.Instance.PlayClick(); ShowTab(0); });
        tabCards.onClick.AddListener(() => { AudioManager.Instance.PlayClick(); ShowTab(1); });
        tabDaily.onClick.AddListener(() => { AudioManager.Instance.PlayClick(); ShowTab(2); });

        AddPressPunch(playButton);
        AddPressPunch(tabHome);
        AddPressPunch(tabCards);
        AddPressPunch(tabDaily);
        AddPressPunch(settingsBtn.GetComponent<Button>());
        AddPressPunch(trophyBtn.GetComponent<Button>());

        ShowTab(0);
        PlayEntrance();
    }

    private void ShowTab(int tab)
    {
        homePanel.SetActive(tab == 0);
        cardsPanel.SetActive(tab == 1);
        if (dailyPanel != null) dailyPanel.SetActive(tab == 2);

        titleText.text = tab == 0 ? "Picture Solitaire" : tab == 1 ? "My Cards" : "Daily Puzzle";

        tabHome.GetComponent<TabView>().SetActive(tab == 0);
        tabCards.GetComponent<TabView>().SetActive(tab == 1);
        tabDaily.GetComponent<TabView>().SetActive(tab == 2);

        if (tab == 0) AnimatePanel(homePanel);
        else if (tab == 1) AnimatePanel(cardsPanel);
        else AnimatePanel(dailyPanel);
    }

    private void AnimatePanel(GameObject panel)
    {
        if (panel == null || !panel.activeSelf) return;

        RectTransform rt = panel.transform as RectTransform;
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        rt.DOKill();
        cg.DOKill();
        rt.localScale = Vector3.one * 0.92f;
        cg.alpha = 0f;
        rt.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        cg.DOFade(1f, 0.25f).SetEase(Ease.OutQuad);
    }

    private void AddPressPunch(Button btn)
    {
        if (btn == null) return;
        btn.onClick.AddListener(() =>
        {
            RectTransform rt = btn.transform as RectTransform;
            rt.DOKill();
            rt.localScale = Vector3.one * 0.9f;
            rt.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        });
    }

    private void PlayEntrance()
    {
        settingsBtn.localScale = Vector3.zero;
        trophyBtn.localScale = Vector3.zero;
        titleBoard.localScale = Vector3.zero;
        playButton.transform.localScale = Vector3.zero;

        settingsBtn.DOScale(1f, 0.4f).SetDelay(0.10f).SetEase(Ease.OutBack);
        trophyBtn.DOScale(1f, 0.4f).SetDelay(0.20f).SetEase(Ease.OutBack);
        titleBoard.DOScale(1f, 0.5f).SetDelay(0.30f).SetEase(Ease.OutBack);
        playButton.transform.DOScale(1f, 0.45f).SetDelay(0.90f).SetEase(Ease.OutBack);

        float barY = bottomBar.anchoredPosition.y;
        bottomBar.anchoredPosition = new Vector2(bottomBar.anchoredPosition.x, barY - 220f);
        bottomBar.DOAnchorPosY(barY, 0.5f).SetDelay(0.35f).SetEase(Ease.OutCubic);
    }

    private void BuildLevelGrid()
    {
        if (totalLevels <= 0) return;

        int start = chapter * LevelConfig.LevelsPerChapter + 1;
        int end = Mathf.Min(totalLevels, start + LevelConfig.LevelsPerChapter - 1);

        Sprite[] tiles = null;
        Texture2D chapterTex = LevelConfig.GetChapterImage(chapter);
        if (chapterTex != null)
        {
            Texture2D cropped = PuzzleSystem.CenterCrop(chapterTex, 0.73f);
            tiles = PuzzleSystem.SliceTexture(cropped, 5, 5, 24f);
        }

        for (int i = start; i <= end; i++)
        {
            GameObject card = Instantiate(levelCardPrefab, levelGrid);
            card.name = "LevelCard_" + i;

            LevelCardView view = card.GetComponent<LevelCardView>();
            view.SetNumber(i);

            bool completed = SaveSystem.Instance.IsLevelCompleted(i);
            bool showReveal = completed && tiles != null;

            if (showReveal)
                view.ShowReveal(tiles[(i - start) % 25]);
            else
                view.ShowLocked();

            RectTransform rt = card.transform as RectTransform;
            rt.localScale = Vector3.zero;
            rt.DOScale(1f, 0.35f).SetDelay(0.55f + (i - start) * 0.04f).SetEase(Ease.OutBack);
        }
    }

    private void BuildCardsGallery()
    {
        for (int i = 1; i <= totalLevels; i++)
        {
            GameObject card = Instantiate(thumbCardPrefab, cardsGrid);
            card.name = "ThumbCard_" + i;

            ThumbCardView view = card.GetComponent<ThumbCardView>();

            bool completed = SaveSystem.Instance.IsLevelCompleted(i);
            var entry = LevelConfig.GetEntry(i);

            if (completed && entry != null && entry.image != null)
                view.ShowCard(PuzzleSystem.MakeThumb(entry.image, 240, 240, 24f));
            else
                view.ShowLocked();

            RectTransform rt = card.transform as RectTransform;
            rt.localScale = Vector3.zero;
            rt.DOScale(1f, 0.3f).SetDelay(0.15f + (i - 1) * 0.02f).SetEase(Ease.OutBack);
        }
    }

    private void SetupDaily()
    {
        if (dailyPanel == null || totalLevels <= 0) return;

        dateText.text = System.DateTime.Now.ToString("dd MMMM yyyy");

        int dailyLevel = (DailySystem.DailyIndex % totalLevels) + 1;
        var entry = LevelConfig.GetEntry(dailyLevel);
        if (entry != null && entry.image != null)
        {
            dailyPreview.sprite = PuzzleSystem.MakeThumb(entry.image, 420, 420, 20f);
        }

        bool done = DailySystem.IsCompleted;

        dailyButtonLabel.text = done ? "Completed!" : "Play Daily";
        dailyButton.interactable = !done;

        if (done)
        {
            dailyButton.GetComponent<Image>().color = new Color32(0x9E, 0x9E, 0x9E, 255);
        }
        else
        {
            dailyButton.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlayClick();
                DailySystem.PlayingDaily = true;
                TransitionManager.Instance.LoadScene("GameScene");
            });
        }
    }

    private void UpdatePlayButton()
    {
        if (totalLevels <= 0)
        {
            playButtonLabel.text = "No Levels";
            playButton.interactable = false;
            return;
        }

        int current = Mathf.Clamp(SaveSystem.Instance.CurrentLevel, 1, totalLevels);

        // Agar game complete ho chuki hai to "Play Again" likho
        if (SaveSystem.Instance.AllLevelsCompleted())
            playButtonLabel.text = "Play Again";
        else
            playButtonLabel.text = "Play Level " + current;
    }
}