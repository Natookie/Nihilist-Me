using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class SceneChangeManager : MonoBehaviour
{
    static public SceneChangeManager Instance {private set; get;}
    [SerializeField] private Computer computer; // Only needed when the current scene can change to desktop

    static private bool openDesktopOnSceneChange = false;

    private void Awake()
    {
        Instance = this;
    }

    private IEnumerator Start()
    {
        if (openDesktopOnSceneChange)
        {
            Assert.IsNotNull(computer, "computer is missing");
            yield return new WaitUntil(() => computer.didStart);
            computer.InteractImmediately();
            openDesktopOnSceneChange = false;
        }
    }

    public void ChangeToMenu()
    {
        SceneManager.LoadScene("Menu Scene");
    }

    public void ChangeToGame()
    {
        SceneManager.LoadScene("Main Scene");
    }

    public void ChangeToDesktop()
    {
        openDesktopOnSceneChange = true;
        SceneManager.LoadScene("Main Scene");
    }
}
