using TMPro;
using UnityEngine;

public class WinConditionPopUp : MonoBehaviour
{
    [System.Serializable]
    public struct PopupData
    {
        public string title;
        [TextArea] public string body;
    }

    [Header("UI References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Presets")]
    [SerializeField] private PopupData winData;
    [SerializeField] private PopupData gameOverData;

    private void Awake()
    {
        Hide();
    }

    public void ShowWin()
    {
        Show(winData);
    }

    public void ShowGameOver()
    {
        Show(gameOverData);
    }

    private void Show(PopupData data)
    {
        if (titleText != null)
            titleText.text = data.title;

        if (bodyText != null)
            bodyText.text = data.body;

        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }
}