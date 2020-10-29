using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartManager : MonoBehaviour
{
	public Button singleGameButton;
    public Button multiplayerGameButton;
    public Button exitButton;
	public Text nameText;
	public InputField name;
	public Button startGameButton;
    public Button backButton;
    public GameObject inputNameError;
    public static string m_userName;
   
    public void OnSingleGameClick() {
        singleGameButton.gameObject.SetActive(false);
        multiplayerGameButton.gameObject.SetActive(false);
        backButton.gameObject.SetActive(true);
        exitButton.gameObject.SetActive(false);
        nameText.gameObject.SetActive(true);
        name.gameObject.SetActive(true);
        startGameButton.gameObject.SetActive(true);
    }

    public void OnMultiplayerGameClick()
    {
        
        Application.LoadLevel("MultiplayerMenu");
    }

    public void OnExitClick() {
    	Application.Quit();
    }

    public void OnClick() {
        if (name.text.Length <= 2 || name.text == "" || name.text == " " || name.text == "  " || name.text == "   ")
        {
            inputNameError.gameObject.SetActive(true);
            return;
        }
        m_userName = name.text;
    	Application.LoadLevel("Main");
    }

    public void Back()
    {
        singleGameButton.gameObject.SetActive(true);
        multiplayerGameButton.gameObject.SetActive(true);
        backButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(true);
        nameText.gameObject.SetActive(false);
        name.gameObject.SetActive(false);
        startGameButton.gameObject.SetActive(false);
    }


}
