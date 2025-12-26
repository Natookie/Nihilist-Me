using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Camera currentCamera;
    public void Start()
    {
        currentCamera.enabled = true;
    }

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
