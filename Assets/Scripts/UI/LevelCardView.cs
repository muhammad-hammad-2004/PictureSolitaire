using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelCardView : MonoBehaviour
{
    [SerializeField] private GameObject tile;
    [SerializeField] private TextMeshProUGUI number;
    [SerializeField] private Image revealImage;
    [SerializeField] private GameObject frame;
    [SerializeField] private GameObject crown;

    public void SetNumber(int value)
    {
        number.text = value.ToString();
    }

    public void ShowLocked()
    {
        tile.SetActive(true);
        revealImage.gameObject.SetActive(false);
        frame.SetActive(false);
        crown.SetActive(false);
    }

    public void ShowReveal(Sprite sprite)
    {
        tile.SetActive(false);
        revealImage.gameObject.SetActive(true);
        frame.SetActive(true);
        crown.SetActive(true);

        revealImage.sprite = sprite;
        revealImage.type = Image.Type.Simple;
        revealImage.preserveAspect = false;
    }
}