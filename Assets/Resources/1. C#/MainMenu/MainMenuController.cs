using System;
using UnityEngine;
using UnityEngine.Assertions;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TypingEffect titleTypingEffect;

    public void Start()
    {
        // Assertion check
        Assert.IsNotNull(titleTypingEffect, "titleTypingEffect is missing");
        // Initialize main menu
        Time.timeScale = 0f;
        titleTypingEffect.EnableEffect();
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneChangeManager.Instance.ChangeToGame();
    }

    public void QuitGame()
    {
        Debug.Log("Quit game");
        Application.Quit();
    }
}
