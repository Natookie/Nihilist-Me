using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void OnPlayPressed()
    {
        SceneManager.LoadScene("Main Scene");
    }

    public void OnQuitPressed()
    {
        Debug.Log("Quit game");
        Application.Quit();
    }
}
