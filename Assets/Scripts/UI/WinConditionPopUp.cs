using UnityEngine;

public class WinConditionPopUp : MonoBehaviour
{
    [SerializeField] private GameObject root;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }
}