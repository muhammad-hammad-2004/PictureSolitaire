using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;

public class PuzzlePiece : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public int CorrectIndex { get; private set; }

    [Header("References")]
    [SerializeField] private Image borderImage;
    [SerializeField] private Image artImage;

    public Action<PuzzlePiece> OnDragStarted;
    public Action<PuzzlePiece, Vector2> OnDragged;
    public Action<PuzzlePiece> OnDropped;

    private RectTransform artRt;
    private Canvas canvas;

    private void Awake()
    {
        artRt = (RectTransform)artImage.transform;
        canvas = GetComponentInParent<Canvas>();
    }

    public void Setup(int correctIndex, Sprite sprite)
    {
        CorrectIndex = correctIndex;
        artImage.sprite = sprite;

        // Art center-anchored (stretch nahi) — grid par exact scale
        artRt.anchorMin = new Vector2(0.5f, 0.5f);
        artRt.anchorMax = new Vector2(0.5f, 0.5f);
        artRt.anchoredPosition = Vector2.zero;
    }

    public void SetArtSize(Vector2 size)
    {
        artRt.sizeDelta = size;
    }

    public void SnapArt(Vector2 localPos)
    {
        artRt.DOKill();
        artRt.anchoredPosition = localPos;
    }

    public void TweenArt(Vector2 localPos, float dur)
    {
        artRt.DOKill();
        artRt.DOAnchorPos(localPos, dur).SetEase(Ease.OutQuad);
    }

    public void SetBorderSprite(Sprite sprite)
    {
        borderImage.sprite = sprite;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDragStarted?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnDragged?.Invoke(this, eventData.delta / canvas.scaleFactor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnDropped?.Invoke(this);
    }
}