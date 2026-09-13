using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public static class FX
{
    // Poora group aik sath pop — koi white gap nahi
    public static void JoinPopGroup(List<Transform> pieces, float peak = 1.06f, float speed = 0.12f)
    {
        if (pieces == null || pieces.Count == 0) return;

        // Group ka center nikalo
        Vector2 center = Vector2.zero;
        for (int i = 0; i < pieces.Count; i++)
            center += (Vector2)pieces[i].localPosition;
        center /= pieces.Count;

        // Har piece: center se bahar scale+move, phir wapis — sab same timing
        for (int i = 0; i < pieces.Count; i++)
        {
            Transform t = pieces[i];
            t.DOKill();

            Vector3 rest = t.localPosition;
            Vector3 popped = (Vector3)(center + ((Vector2)rest - center) * peak);
            popped.z = rest.z;

            Sequence s = DOTween.Sequence();
            s.Append(t.DOLocalMove(popped, speed).SetEase(Ease.OutQuad));
            s.Insert(0f, t.DOScale(peak, speed).SetEase(Ease.OutQuad));
            s.Append(t.DOLocalMove(rest, speed * 1.4f).SetEase(Ease.OutBack));
            s.Insert(speed, t.DOScale(1f, speed * 1.4f).SetEase(Ease.OutBack));
        }
    }
}