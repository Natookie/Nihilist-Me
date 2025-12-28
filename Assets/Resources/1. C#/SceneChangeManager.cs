using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeManager : MonoBehaviour
{
    static public SceneChangeManager Instance {private set; get;}

    private void Awake()
    {
        Instance = this;
    }

    public void ChangeToMenu()
    {
        SceneManager.LoadScene("Menu Scene");
    }

    public void ChangeToGame()
    {
        SceneManager.LoadScene("Main Scene");
    }
}
