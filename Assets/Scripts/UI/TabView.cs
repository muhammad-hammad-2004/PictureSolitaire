using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TabView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI label;

    public void SetActive(bool active)
    {
        icon.color = active ? Color.white : new Color(0.72f, 0.68f, 0.60f, 1f);
        label.color = active ? new Color32(0xFF, 0xD2, 0x50, 255) : new Color32(0xF2, 0xE8, 0xD5, 255);
    }
}