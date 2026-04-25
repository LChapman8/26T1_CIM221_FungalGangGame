using UnityEngine;

public class NodePopupTrigger : MonoBehaviour
{
    [Header("UI")]
    public GameObject popupUI;

    [Header("Settings")]
    public bool onlyTriggerOnce = true;

    private bool hasTriggered = false;

    private void Start()
    {
        if (popupUI != null)
            popupUI.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (onlyTriggerOnce && hasTriggered) return;

        hasTriggered = true;

        popupUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ContinueGame()
    {
        popupUI.SetActive(false);
        Time.timeScale = 1f;
    }
}