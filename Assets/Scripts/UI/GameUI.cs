using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GameUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform board;
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button hintButton;
    [SerializeField] private TextMeshProUGUI hintNumber;
    [SerializeField] private Sprite[] cornerSprites;

    [SerializeField] private GameObject winPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Image winImage;
    [SerializeField] private RectTransform confettiRoot;
    [SerializeField] private Sprite confettiSprite;

    [SerializeField] private RectTransform[] stars;
    [SerializeField] private TextMeshProUGUI winTitle;

    private Texture2D currentTexture;

    private AsyncOperationHandle<Texture2D> textureHandle;
    private bool hasHandle;

    [Header("Settings")]
    [SerializeField] private float dealDelay = 0.06f;
    [SerializeField] private float swapDuration = 0.18f;

    private int level, rows, cols, total;
    private Vector2 boardSize;
    private float cellW, cellH;
    private Vector2 artSize;

    private PuzzlePiece[] pieces;
    private PuzzlePiece[] cellToPiece;
    private Dictionary<PuzzlePiece, int> pieceCell = new Dictionary<PuzzlePiece, int>();
    private List<List<PuzzlePiece>> groups = new List<List<PuzzlePiece>>();
    private List<PuzzlePiece> dragGroup;
    private List<PuzzlePiece> draggingGroup;
    private Vector3 dragStartPos;
    private bool won;
    private int settleId;

    private const float BlockGap = 2f;
    private Dictionary<PuzzlePiece, Vector3> restPos = new Dictionary<PuzzlePiece, Vector3>();
    private Dictionary<PuzzlePiece, Vector2> restSize = new Dictionary<PuzzlePiece, Vector2>();

    [Header("Win Screen New")]
    [SerializeField] private Image dim;
    [SerializeField] private RectTransform card;
    [SerializeField] private RectTransform crown;
    [SerializeField] private RectTransform ribbon;
    [SerializeField] private RectTransform completeImg;
    [SerializeField] private RectTransform photoFrame;
    [SerializeField] private RectTransform messagePill;
    [SerializeField] private RectTransform leafL;
    [SerializeField] private RectTransform leafR;
    [SerializeField] private RectTransform grassL;
    [SerializeField] private RectTransform grassR;

    private Vector2 crownHome;
    private float dimAlpha;

    private void Start()
    {
        int totalLevels = Mathf.Max(1, LevelConfig.TotalLevels);
        level = Mathf.Clamp(SaveSystem.Instance.CurrentLevel, 1, totalLevels);

        titleText.text = DailySystem.PlayingDaily ? "Daily Puzzle" : "Level " + level;
        hintNumber.text = SaveSystem.Instance.Hints.ToString();
        hintButton.interactable = SaveSystem.Instance.Hints > 0;

        backButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClick();
            TransitionManager.Instance.LoadScene("HomeScene");
        });
        hintButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClick();
            UseHint();
        });
        continueButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClick();
            TransitionManager.Instance.LoadScene("HomeScene");
        });

        AudioManager.Instance.PlayGameMusic();
        crownHome = crown.anchoredPosition;
        dimAlpha = dim.color.a;
        StartCoroutine(LoadAndBuildBoard());
    }

    private IEnumerator LoadAndBuildBoard()
    {
        int dataLevel = DailySystem.PlayingDaily
            ? (DailySystem.DailyIndex % Mathf.Max(1, LevelConfig.TotalLevels)) + 1
            : level;

        var entry = LevelConfig.GetEntry(dataLevel);
        if (entry == null || string.IsNullOrEmpty(entry.imageAddress))
        {
            Debug.LogError("Level " + dataLevel + " not configured in LevelConfig!");
            yield break;
        }

        var handle = Addressables.LoadAssetAsync<Texture2D>(entry.imageAddress);
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Debug.LogError("Addressables se picture load nahi hui: " + entry.imageAddress);
            yield break;
        }

        Debug.Log("✅ Picture Addressables se load hui: " + entry.imageAddress);

        textureHandle = handle;
        hasHandle = true;
        BuildBoard(entry, handle.Result);
    }
    private void OnDestroy()
    {
        if (hasHandle)
        {
            Addressables.Release(textureHandle);
            hasHandle = false;
        }
    }
    private void BuildBoard(LevelConfig.Entry entry, Texture2D puzzleTexture)
    {
        rows = Mathf.Max(1, entry.rows);
        cols = Mathf.Max(1, entry.cols);
        total = rows * cols;
        currentTexture = puzzleTexture;

        // Board FIXED size — har level par same
        boardSize = new Vector2(board.rect.width, board.rect.height);
        float aspect = boardSize.x / boardSize.y;
        Texture2D slicedTex = PuzzleSystem.CenterCrop(puzzleTexture, aspect);

        cellW = boardSize.x / cols;
        cellH = boardSize.y / rows;

        const int bleed = 6;
        Sprite[] sprites = PuzzleSystem.SliceTexture(slicedTex, rows, cols, 0f, bleed);

        // Art ka EXACT scale (bleed samet) — line khatam
        float cwT = slicedTex.width / (float)cols;
        float chT = slicedTex.height / (float)rows;
        artSize = new Vector2(cellW + 2 * bleed * cellW / cwT, cellH + 2 * bleed * cellH / chT);

        List<int> shuffled = new List<int>();
        for (int i = 0; i < total; i++) shuffled.Add(i);

        int attempts = 0;
        do
        {
            for (int i = 0; i < total; i++)
            {
                int r = Random.Range(i, total);
                (shuffled[i], shuffled[r]) = (shuffled[r], shuffled[i]);
            }
            attempts++;
        } while (HasStartMatch(shuffled) && attempts < 500);

        pieces = new PuzzlePiece[total];
        cellToPiece = new PuzzlePiece[total];

        for (int i = 0; i < total; i++)
        {
            GameObject go = Instantiate(piecePrefab, board);
            PuzzlePiece piece = go.GetComponent<PuzzlePiece>();
            piece.Setup(i, sprites[i]);
            piece.SetArtSize(artSize);
            piece.OnDragStarted = OnDragStarted;
            piece.OnDragged = OnPieceDragged;
            piece.OnDropped = OnPieceDropped;

            pieces[i] = piece;
            SetPieceCell(piece, shuffled[i]);

            RectTransform rt = piece.transform as RectTransform;
            Image art = piece.transform.Find("Art").GetComponent<Image>();
            art.enabled = false;

            Sequence seq = DOTween.Sequence();
            seq.AppendInterval(i * dealDelay);
            seq.Append(rt.DORotate(new Vector3(0f, 90f, 0f), 0.12f).SetEase(Ease.InQuad));
            seq.AppendCallback(() => art.enabled = true);
            seq.Append(rt.DORotate(Vector3.zero, 0.2f).SetEase(Ease.OutQuad));
        }

        RefreshGroups();
    }

    private bool HasStartMatch(List<int> s)
    {
        for (int c = 0; c < total; c++)
        {
            if (s[c] == c) return true;
            if ((c % cols) < cols - 1 && (s[c] % cols) < cols - 1 && s[c + 1] == s[c] + 1) return true;
            if (c + cols < total && s[c + cols] == s[c] + cols) return true;
        }
        return false;
    }

    private Vector3 CellPosition(int cell)
    {
        int row = cell / cols;
        int col = cell % cols;
        float x = -boardSize.x * 0.5f + col * cellW + cellW * 0.5f;
        float y = boardSize.y * 0.5f - row * cellH - cellH * 0.5f;
        return new Vector3(x, y, 0f);
    }

    private void SetPieceCell(PuzzlePiece piece, int cell)
    {
        cellToPiece[cell] = piece;
        pieceCell[piece] = cell;
        piece.transform.localPosition = CellPosition(cell);
        (piece.transform as RectTransform).sizeDelta = new Vector2(cellW - 2f * BlockGap, cellH - 2f * BlockGap);
    }

    private void ScheduleSettle()
    {
        int id = ++settleId;
        DOVirtual.DelayedCall(swapDuration + 0.05f, () =>
        {
            if (id == settleId)
            {
                RefreshGroups();
                CheckWin();
            }
        });
    }

    private List<PuzzlePiece> ComputeGroup(PuzzlePiece start)
    {
        List<PuzzlePiece> result = new List<PuzzlePiece>();

        Stack<PuzzlePiece> stack = new Stack<PuzzlePiece>();
        HashSet<PuzzlePiece> seen = new HashSet<PuzzlePiece>();
        stack.Push(start);
        seen.Add(start);

        while (stack.Count > 0)
        {
            PuzzlePiece p = stack.Pop();
            result.Add(p);
            int c = pieceCell[p];
            int col = c % cols;

            if (col < cols - 1 && (p.CorrectIndex % cols) < cols - 1)
                TryExpand(c + 1, p.CorrectIndex + 1);
            if (col > 0 && (p.CorrectIndex % cols) > 0)
                TryExpand(c - 1, p.CorrectIndex - 1);
            if (c + cols < total)
                TryExpand(c + cols, p.CorrectIndex + cols);
            if (c - cols >= 0)
                TryExpand(c - cols, p.CorrectIndex - cols);
        }

        return result;

        void TryExpand(int cell, int correct)
        {
            PuzzlePiece o = cellToPiece[cell];
            if (o == null || seen.Contains(o)) return;
            if (o.CorrectIndex != correct) return;
            seen.Add(o);
            stack.Push(o);
        }
    }

    private PuzzlePiece Find(Dictionary<PuzzlePiece, PuzzlePiece> parent, PuzzlePiece x)
    {
        while (parent[x] != x)
        {
            parent[x] = parent[parent[x]];
            x = parent[x];
        }
        return x;
    }

    private int GetMaxGroupSize()
    {
        int max = 0;
        foreach (var g in groups)
            if (g.Count > max) max = g.Count;
        return max;
    }

    private void RefreshGroups()
    {
        int prevMaxGroup = GetMaxGroupSize();

        groups.Clear();
        Dictionary<PuzzlePiece, PuzzlePiece> parent = new Dictionary<PuzzlePiece, PuzzlePiece>();
        foreach (var p in pieces) parent[p] = p;

        for (int c = 0; c < total; c++)
        {
            PuzzlePiece p = cellToPiece[c];
            if (p == null) continue;
            int col = c % cols;
            int row = c / cols;

            if (col < cols - 1)
            {
                PuzzlePiece r = cellToPiece[c + 1];
                if (r != null
                    && (p.CorrectIndex % cols) < cols - 1
                    && r.CorrectIndex == p.CorrectIndex + 1)
                {
                    parent[Find(parent, r)] = Find(parent, p);
                }
            }
            if (row < rows - 1)
            {
                PuzzlePiece d = cellToPiece[c + cols];
                if (d != null && d.CorrectIndex == p.CorrectIndex + cols)
                {
                    parent[Find(parent, d)] = Find(parent, p);
                }
            }
        }

        Dictionary<PuzzlePiece, List<PuzzlePiece>> map = new Dictionary<PuzzlePiece, List<PuzzlePiece>>();
        foreach (var p in pieces)
        {
            var root = Find(parent, p);
            if (!map.ContainsKey(root)) map[root] = new List<PuzzlePiece>();
            map[root].Add(p);
        }
        groups.AddRange(map.Values);

        int newMaxGroup = GetMaxGroupSize();
        if (prevMaxGroup > 0 && newMaxGroup > prevMaxGroup)
        {
            AudioManager.Instance.PlayMerge();

            // Poora group aik sath pop — settle (0.2s) ke baad
            foreach (var g in groups)
            {
                if (g.Count == newMaxGroup)
                {
                    List<Transform> ts = new List<Transform>();
                    foreach (var p in g) ts.Add(p.transform);

                    DOVirtual.DelayedCall(0.22f, () =>
                    {
                        if (draggingGroup == null)
                            FX.JoinPopGroup(ts, 1.06f, 0.12f);
                    });
                    break;
                }
            }
        }

        const float seam = 1.5f;

        foreach (var g in groups)
        {
            if (draggingGroup != null)
            {
                bool dragging = false;
                foreach (var p in g)
                {
                    if (draggingGroup.Contains(p)) { dragging = true; break; }
                }
                if (dragging) continue;
            }

            HashSet<int> cells = new HashSet<int>();
            foreach (var p in g) cells.Add(pieceCell[p]);

            foreach (var p in g)
            {
                int c = pieceCell[p];
                int col = c % cols;
                bool up = c - cols >= 0 && cells.Contains(c - cols);
                bool down = c + cols < total && cells.Contains(c + cols);
                bool left = col > 0 && cells.Contains(c - 1);
                bool right = col < cols - 1 && cells.Contains(c + 1);

                int mask = 0;
                if (!up && !left) mask |= 1;
                if (!up && !right) mask |= 2;
                if (!down && !right) mask |= 4;
                if (!down && !left) mask |= 8;

                p.SetBorderSprite(cornerSprites[mask]);

                float l = -cellW * 0.5f + (left ? -seam : BlockGap);
                float r = cellW * 0.5f + (right ? seam : -BlockGap);
                float b = -cellH * 0.5f + (down ? -seam : BlockGap);
                float t = cellH * 0.5f + (up ? seam : -BlockGap);

                Vector3 cellPos = CellPosition(c);
                Vector3 pos = cellPos + new Vector3((l + r) * 0.5f, (b + t) * 0.5f, 0f);
                Vector2 size = new Vector2(r - l, t - b);

                restPos[p] = pos;
                restSize[p] = size;

                RectTransform rt = p.transform as RectTransform;
                rt.DOKill();
                rt.DOSizeDelta(size, 0.2f).SetEase(Ease.OutQuad);
                rt.DOLocalMove(pos, 0.2f).SetEase(Ease.OutQuad);

                // Art ko cell par EXACT lock karo (line khatam)
                p.TweenArt((Vector2)cellPos - (Vector2)pos, 0.2f);
            }
        }
    }

    private void OnDragStarted(PuzzlePiece piece)
    {
        AudioManager.Instance.PlayPickup();
        dragGroup = ComputeGroup(piece);
        draggingGroup = dragGroup;
        dragStartPos = piece.transform.localPosition;

        foreach (var p in dragGroup)
        {
            RectTransform rt = p.transform as RectTransform;
            rt.DOKill();
            rt.localPosition = restPos[p];
            rt.sizeDelta = restSize[p];
            p.SnapArt((Vector2)CellPosition(pieceCell[p]) - (Vector2)restPos[p]);
            rt.SetAsLastSibling();
        }
    }

    private void OnPieceDragged(PuzzlePiece piece, Vector2 delta)
    {
        if (dragGroup == null) return;
        foreach (var p in dragGroup)
        {
            (p.transform as RectTransform).anchoredPosition += delta;
        }
    }

    private void OnPieceDropped(PuzzlePiece piece)
    {
        if (dragGroup == null) return;
        List<PuzzlePiece> g = dragGroup;
        dragGroup = null;
        draggingGroup = null;

        Vector3 now = piece.transform.localPosition;
        int dCol = Mathf.RoundToInt((now.x - dragStartPos.x) / cellW);
        int dRow = Mathf.RoundToInt((dragStartPos.y - now.y) / cellH);

        if (dCol == 0 && dRow == 0)
        {
            TweenGroupBack(g);
            ScheduleSettle();
            return;
        }

        if (!TryShiftGroup(g, dRow, dCol))
        {
            TweenGroupBack(g);
            ScheduleSettle();
        }
    }

    private void TweenGroupBack(List<PuzzlePiece> g)
    {
        foreach (var p in g)
        {
            RectTransform rt = p.transform as RectTransform;
            rt.DOKill();
            rt.DOLocalMove(restPos[p], swapDuration).SetEase(Ease.OutQuad);
            rt.DOSizeDelta(restSize[p], swapDuration).SetEase(Ease.OutQuad);
        }
    }

    private bool TryShiftGroup(List<PuzzlePiece> g, int dRow, int dCol)
    {
        HashSet<int> footprint = new HashSet<int>();
        foreach (var p in g) footprint.Add(pieceCell[p]);

        List<int> targets = new List<int>();
        foreach (var p in g)
        {
            int c = pieceCell[p];
            int nr = c / cols + dRow;
            int nc = c % cols + dCol;
            if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) return false;
            targets.Add(nr * cols + nc);
        }

        HashSet<int> T = new HashSet<int>(targets);
        HashSet<int> freed = new HashSet<int>();
        foreach (var f in footprint) if (!T.Contains(f)) freed.Add(f);
        HashSet<int> newCover = new HashSet<int>();
        foreach (var t in T) if (!footprint.Contains(t)) newCover.Add(t);

        List<PuzzlePiece> displaced = new List<PuzzlePiece>();
        foreach (var t in newCover)
        {
            PuzzlePiece o = cellToPiece[t];
            if (o == null) return false;
            displaced.Add(o);
        }

        Dictionary<PuzzlePiece, int> dest = new Dictionary<PuzzlePiece, int>();
        foreach (var o in displaced)
        {
            int r = pieceCell[o] / cols - dRow;
            int col = pieceCell[o] % cols - dCol;
            int nc = -1;

            for (int k = 0; k < rows + cols; k++)
            {
                if (r < 0 || r >= rows || col < 0 || col >= cols) break;
                int cell = r * cols + col;
                if (freed.Contains(cell)) { nc = cell; freed.Remove(cell); break; }
                r -= dRow;
                col -= dCol;
            }

            if (nc < 0)
            {
                if (freed.Count == 0) return false;
                foreach (var f in freed) { nc = f; freed.Remove(f); break; }
            }
            dest[o] = nc;
        }

        if (freed.Count != 0) return false;

        foreach (var p in g) cellToPiece[pieceCell[p]] = null;
        foreach (var o in displaced) cellToPiece[pieceCell[o]] = null;

        foreach (var p in g)
        {
            int c = pieceCell[p];
            int nc = (c / cols + dRow) * cols + (c % cols + dCol);
            pieceCell[p] = nc;
            cellToPiece[nc] = p;
        }
        foreach (var o in displaced)
        {
            int c = pieceCell[o];
            int nc = dest[o];
            pieceCell[o] = nc;
            cellToPiece[nc] = o;
        }

        RefreshGroups();

        ScheduleSettle();
        AudioManager.Instance.PlayDrop();
        return true;
    }

    private void CheckWin()
    {
        if (won) return;
        foreach (var p in pieces)
        {
            if (pieceCell[p] != p.CorrectIndex) return;
        }

        won = true;
        DOVirtual.DelayedCall(0.6f, ShowWin);
    }

    private void ShowWin()
    {
        bool gameComplete = false;

        // 1. Save Progress
        if (DailySystem.PlayingDaily)
        {
            DailySystem.MarkCompleted();
        }
        else
        {
            SaveSystem.Instance.CompleteLevel(level);

            // Check if this was the LAST level of the game
            if (level >= LevelConfig.TotalLevels)
            {
                gameComplete = SaveSystem.Instance.AllLevelsCompleted();
            }
        }

        // 2. Win Image Setup
        if (currentTexture != null && winImage != null)
        {
            Texture2D cropped = PuzzleSystem.CenterCrop(currentTexture, 500f / 460f);
            Texture2D rounded = PuzzleSystem.ApplyRoundedAlpha(cropped, 50f);
            winImage.sprite = Sprite.Create(rounded,
                new Rect(0, 0, rounded.width, rounded.height),
                new Vector2(0.5f, 0.5f));
        }

        // 3. Title Logic (Game Complete vs Level Complete)
        if (DailySystem.PlayingDaily)
            winTitle.text = "Daily Puzzle";
        else if (gameComplete)
            winTitle.text = "Game Complete!";
        else
            winTitle.text = "Level " + level;

        // ---- hidden starting states ----
        dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, 0f);
        card.localScale = Vector3.one * 0.8f;
        crown.anchoredPosition = crownHome + new Vector2(0f, 260f);
        ribbon.localScale = new Vector3(0.001f, 1f, 1f);
        winTitle.rectTransform.localScale = Vector3.zero;
        completeImg.localScale = Vector3.zero;
        photoFrame.localScale = Vector3.zero;
        messagePill.localScale = Vector3.zero;
        leafL.localScale = Vector3.zero;
        leafR.localScale = Vector3.zero;
        grassL.localScale = Vector3.zero;
        grassR.localScale = Vector3.zero;
        continueButton.transform.localScale = Vector3.zero;
        for (int i = 0; i < stars.Length; i++) stars[i].localScale = Vector3.zero;

        winPanel.SetActive(true);

        // ---- animation sequence ----
        dim.DOFade(dimAlpha, 0.3f);
        card.DOScale(1f, 0.35f).SetEase(Ease.OutBack);

        crown.DOAnchorPos(crownHome, 0.55f).SetDelay(0.2f).SetEase(Ease.OutBounce);

        ribbon.DOScale(Vector3.one, 0.3f).SetDelay(0.45f).SetEase(Ease.OutQuad);
        winTitle.rectTransform.DOScale(Vector3.one, 0.3f).SetDelay(0.6f).SetEase(Ease.OutBack);

        completeImg.DOScale(Vector3.one, 0.35f).SetDelay(0.75f).SetEase(Ease.OutBack);

        for (int i = 0; i < stars.Length; i++)
        {
            float d = 1.0f + i * 0.18f;
            stars[i].DOScale(1f, 0.4f).SetDelay(d).SetEase(Ease.OutBack);
            DOVirtual.DelayedCall(d, () => AudioManager.Instance.PlayStar());
        }

        photoFrame.DOScale(Vector3.one, 0.4f).SetDelay(1.5f).SetEase(Ease.OutBack);
        leafL.DOScale(Vector3.one, 0.35f).SetDelay(1.7f).SetEase(Ease.OutBack);
        leafR.DOScale(Vector3.one, 0.35f).SetDelay(1.8f).SetEase(Ease.OutBack);
        messagePill.DOScale(Vector3.one, 0.35f).SetDelay(1.95f).SetEase(Ease.OutBack);
        grassL.DOScale(Vector3.one, 0.3f).SetDelay(2.05f).SetEase(Ease.OutBack);
        grassR.DOScale(Vector3.one, 0.3f).SetDelay(2.1f).SetEase(Ease.OutBack);

        continueButton.transform.DOScale(Vector3.one, 0.4f).SetDelay(2.2f).SetEase(Ease.OutBack);
        continueButton.transform.DOScale(1.04f, 0.6f).SetDelay(2.8f)
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);

        DOVirtual.DelayedCall(0.2f, () => AudioManager.Instance.PlayWin());
        DOVirtual.DelayedCall(1.6f, SpawnConfetti);

        // 🎉 GRAND FINALE: Extra Confetti for Game Complete
        if (gameComplete)
        {
            DOVirtual.DelayedCall(2.0f, SpawnConfetti);
            DOVirtual.DelayedCall(2.4f, SpawnConfetti);
        }
    }

    private void SpawnConfetti()
    {
        Color[] colors =
        {
            new Color32(0x3A, 0xA6, 0x55, 255),
            new Color32(0xFF, 0xC9, 0x3C, 255),
            new Color32(0xFF, 0xFF, 0xFF, 255),
            new Color32(0xF2, 0x8C, 0x28, 255),
            new Color32(0x4A, 0x90, 0xD9, 255),
        };

        for (int i = 0; i < 60; i++)
        {
            GameObject go = new GameObject("Confetti");
            go.transform.SetParent(confettiRoot, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            Image img = go.AddComponent<Image>();
            img.sprite = confettiSprite;
            img.color = colors[Random.Range(0, colors.Length)];
            img.raycastTarget = false;

            rt.anchoredPosition = new Vector2(Random.Range(-300f, 300f), Random.Range(200f, 500f));
            rt.sizeDelta = new Vector2(Random.Range(10f, 22f), Random.Range(14f, 28f));
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            float dur = Random.Range(0.9f, 1.8f);
            rt.DOAnchorPosY(rt.anchoredPosition.y - Random.Range(700f, 1100f), dur).SetEase(Ease.InQuad);
            rt.DORotate(new Vector3(0, 0, Random.Range(-720f, 720f)), dur, RotateMode.FastBeyond360);
            img.DOFade(0f, dur);
            Destroy(go, dur + 0.1f);
        }
    }

    private void UseHint()
    {
        int hints = SaveSystem.Instance.Hints;
        if (hints <= 0) return;

        foreach (var p in pieces)
        {
            int c = pieceCell[p];
            int t = p.CorrectIndex;
            if (c == t) continue;
            int dRow = t / cols - c / cols;
            int dCol = t % cols - c % cols;

            if (TryShiftGroup(ComputeGroup(p), dRow, dCol))
            {
                SaveSystem.Instance.SetHints(hints - 1);
                hintNumber.text = (hints - 1).ToString();
                if (hints - 1 <= 0) hintButton.interactable = false;
                return;
            }
        }
    }
}