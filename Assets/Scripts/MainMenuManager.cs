using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    
    public void PlayGame()
    {
        SceneManager.LoadScene("SampleScene"); 
    }

    
    public void QuitGame()
    {
        Debug.Log("Quit Game"); 
        Application.Quit();
    }
}