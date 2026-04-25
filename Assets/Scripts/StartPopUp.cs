using UnityEngine;

public class StartPopUp : MonoBehaviour
{
    [Header("UI")]
    public GameObject popupUI;

    void Start()
    {
        
        popupUI.SetActive(true);

        
        Time.timeScale = 0f;
    }

    public void StartGame()
    {
        
        popupUI.SetActive(false);

        
        Time.timeScale = 1f;
    }
}