using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
   
    public void PlayGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("SampleScene");
    }

    
    public void LoadMainMenu()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("MainMenu"); 
    }

    
    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}