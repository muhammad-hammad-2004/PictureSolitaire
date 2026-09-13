using UnityEngine;
using DG.Tweening;

public class UIFloat : MonoBehaviour
{
    [SerializeField] private float amplitude = 8f;
    [SerializeField] private float duration = 1.4f;

    private void Start()
    {
        float baseY = transform.localPosition.y;
        transform.DOLocalMoveY(baseY + amplitude, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }
}