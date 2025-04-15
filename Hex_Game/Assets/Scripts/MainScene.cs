using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainScene : MonoBehaviour
{
    public static string gameMode = "";
    void Start()
    {
        
    }

    // function to change scenes to play local
    public void PlayLocalchangeScene()
    {
        gameMode = "PlayLocal";
        SceneManager.LoadScene("Game");
        
    }
    // function to change scene to play AI
    public void PlayAIchangeScene()
    {
        gameMode = "PlayAI";
        SceneManager.LoadScene("Game");
    }
    // function to change scene to simulate AI games
    public void SimulateAIGames()
    {
        gameMode = "SimulateAIGame";
        SceneManager.LoadScene("Game");
    }
}
