using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
    private List<AsyncOperationHandle> activeHandles = new List<AsyncOperationHandle>();

    private void Start()
    {
        DailySystem.PlayingDaily = false;

        totalLevels = LevelConfig.TotalLevels;
        chapter = (totalLevels > 0)
            ? Mathf.Clamp(LevelConfig.GetChapter(SaveSystem.Instance.CurrentLevel), 0, LevelConfig.ChapterCount - 1)
            : 0;

        UpdatePlayButton();

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

        // Saara content async load hoga
        StartCoroutine(LoadAllContentAsync());
    }

    private IEnumerator LoadAllContentAsync()
    {
        yield return StartCoroutine(BuildLevelGridAsync());
        yield return StartCoroutine(BuildCardsGalleryAsync());
        yield return StartCoroutine(SetupDailyAsync());
    }

    private IEnumerator BuildLevelGridAsync()
    {
        if (totalLevels <= 0) yield break;

        int start = chapter * LevelConfig.LevelsPerChapter + 1;
        int end = Mathf.Min(totalLevels, start + LevelConfig.LevelsPerChapter - 1);

        // Pehle saare cards locked state mein bana dein
        for (int i = start; i <= end; i++)
        {
            GameObject card = Instantiate(levelCardPrefab, levelGrid);
            card.name = "LevelCard_" + i;

            LevelCardView view = card.GetComponent<LevelCardView>();
            view.SetNumber(i);
            view.ShowLocked();

            RectTransform rt = card.transform as RectTransform;
            rt.localScale = Vector3.zero;
            rt.DOScale(1f, 0.35f).SetDelay(0.55f + (i - start) * 0.04f).SetEase(Ease.OutBack);
        }

        // Ab chapter image load karein
        string chapterAddr = LevelConfig.GetChapterAddress(chapter);
        if (string.IsNullOrEmpty(chapterAddr)) yield break;

        var handle = Addressables.LoadAssetAsync<Texture2D>(chapterAddr);
        activeHandles.Add(handle);
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null) yield break;

        Texture2D chapterTex = handle.Result;
        Texture2D cropped = PuzzleSystem.CenterCrop(chapterTex, 0.73f);
        Sprite[] tiles = PuzzleSystem.SliceTexture(cropped, 5, 5, 24f);

        // Completed levels ko reveal karein
        for (int i = start; i <= end; i++)
        {
            if (!SaveSystem.Instance.IsLevelCompleted(i)) continue;
            Transform cardTr = levelGrid.Find("LevelCard_" + i);
            if (cardTr == null) continue;
            LevelCardView view = cardTr.GetComponent<LevelCardView>();
            view.ShowReveal(tiles[(i - start) % 25]);
        }
    }

    private IEnumerator BuildCardsGalleryAsync()
    {
        // Pehle saare thumb cards locked state mein bana dein
        for (int i = 1; i <= totalLevels; i++)
        {
            GameObject card = Instantiate(thumbCardPrefab, cardsGrid);
            card.name = "ThumbCard_" + i;

            ThumbCardView view = card.GetComponent<ThumbCardView>();
            view.ShowLocked();

            RectTransform rt = card.transform as RectTransform;
            rt.localScale = Vector3.zero;
            rt.DOScale(1f, 0.3f).SetDelay(0.15f + (i - 1) * 0.02f).SetEase(Ease.OutBack);
        }

        // Ab sirf completed levels ki pictures load karein
        for (int i = 1; i <= totalLevels; i++)
        {
            if (!SaveSystem.Instance.IsLevelCompleted(i)) continue;

            var entry = LevelConfig.GetEntry(i);
            if (entry == null || string.IsNullOrEmpty(entry.imageAddress)) continue;

            var handle = Addressables.LoadAssetAsync<Texture2D>(entry.imageAddress);
            activeHandles.Add(handle);
            yield return handle;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null) continue;

            Transform cardTr = cardsGrid.Find("ThumbCard_" + i);
            if (cardTr == null) continue;
            ThumbCardView view = cardTr.GetComponent<ThumbCardView>();
            view.ShowCard(PuzzleSystem.MakeThumb(handle.Result, 240, 240, 24f));
        }
    }

    private IEnumerator SetupDailyAsync()
    {
        if (dailyPanel == null || totalLevels <= 0) yield break;

        dateText.text = System.DateTime.Now.ToString("dd MMMM yyyy");

        int dailyLevel = (DailySystem.DailyIndex % totalLevels) + 1;
        var entry = LevelConfig.GetEntry(dailyLevel);

        bool imageLoaded = false;
        if (entry != null && !string.IsNullOrEmpty(entry.imageAddress))
        {
            var handle = Addressables.LoadAssetAsync<Texture2D>(entry.imageAddress);
            activeHandles.Add(handle);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                dailyPreview.sprite = PuzzleSystem.MakeThumb(handle.Result, 420, 420, 20f);
                imageLoaded = true;
            }
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

    private void OnDestroy()
    {
        foreach (var h in activeHandles)
        {
            if (h.IsValid())
                Addressables.Release(h);
        }
        activeHandles.Clear();
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

    private void UpdatePlayButton()
    {
        if (totalLevels <= 0)
        {
            playButtonLabel.text = "No Levels";
            playButton.interactable = false;
            return;
        }

        int current = Mathf.Clamp(SaveSystem.Instance.CurrentLevel, 1, totalLevels);

        if (SaveSystem.Instance.AllLevelsCompleted())
            playButtonLabel.text = "Play Again";
        else
            playButtonLabel.text = "Play Level " + current;
    }
}