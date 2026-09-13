using UnityEngine;
using UnityEngine.UI;

public class ThumbCardView : MonoBehaviour
{
    [SerializeField] private GameObject tile;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Image thumb;
    [SerializeField] private GameObject frame;

    public void ShowLocked()
    {
        tile.SetActive(true);
        lockIcon.SetActive(true);
        thumb.gameObject.SetActive(false);
        frame.SetActive(false);
    }

    public void ShowCard(Sprite sprite)
    {
        tile.SetActive(false);
        lockIcon.SetActive(false);
        thumb.gameObject.SetActive(true);
        frame.SetActive(true);

        thumb.sprite = sprite;
        thumb.color = Color.white;
    }
}