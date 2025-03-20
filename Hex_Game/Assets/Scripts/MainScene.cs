using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainScene : MonoBehaviour
{
    public static string gameMode = "";
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void PlayLocalchangeScene()
    {
        gameMode = "PlayLocal";
        SceneManager.LoadScene("Game");
        
    }
    public void PlayAIchangeScene()
    {
        gameMode = "PlayAI";
        SceneManager.LoadScene("Game");
    }
    public void SimulateAIGames()
    {
        gameMode = "SimulateAIGame";
        SceneManager.LoadScene("Game");
    }
}
