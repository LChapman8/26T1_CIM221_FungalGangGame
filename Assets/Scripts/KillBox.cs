using UnityEngine;
using UnityEngine.SceneManagement;

public class KillBox : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if it's the player
        if (collision.CompareTag("Player"))
        {
            // Reload current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}