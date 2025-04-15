using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class GameButton : MonoBehaviour
{
    
    void Start()
    {
        
    }

    // button to go back to the main menu
    public void BackToMenu()
    {
        SceneManager.LoadScene("Main Screen");
    }
}
