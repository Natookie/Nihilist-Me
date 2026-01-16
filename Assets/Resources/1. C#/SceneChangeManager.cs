using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class SceneChangeManager : MonoBehaviour
{
    static public SceneChangeManager Instance {private set; get;}
    [SerializeField] private Computer computer;

    static private bool openDesktopOnSceneChange = false;

    void Awake(){
        Instance = this;
    }

    IEnumerator Start(){
        if(openDesktopOnSceneChange){
            Assert.IsNotNull(computer, "computer is missing");
            yield return new WaitUntil(() => computer.didStart);
            computer.InteractImmediately();
            openDesktopOnSceneChange = false;
        }
        
        yield return new WaitForEndOfFrame();
        if(GameManager.Instance != null) GameManager.Instance.ResetReference();
    }

    public void ChangeToMenu(){
        SceneManager.LoadScene("Main Menu Scene");
        StartCoroutine(DelayedReset());
    }

    public void ChangeToGame(){
        SceneManager.LoadScene("Main Scene");
        StartCoroutine(DelayedReset());
    }

    public void ChangeToDesktop(){
        openDesktopOnSceneChange = true;
        SceneManager.LoadScene("Main Scene");
        StartCoroutine(DelayedReset());
    }

    IEnumerator DelayedReset(){
        yield return new WaitForSeconds(.1f);
        if(GameManager.Instance != null) GameManager.Instance.ResetReference();
    }
}