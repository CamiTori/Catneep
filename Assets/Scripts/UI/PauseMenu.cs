using UnityEngine;
using UnityEngine.SceneManagement;


public class PauseMenu : MonoBehaviour
{

    public GameObject pauseMenuUI;


    private void Awake()
    {
        PauseManager.OnSetPause += SetPause;
    }

    private void OnDisable()
    {
        PauseManager.OnSetPause -= SetPause;
    }

    // Simplificado la función resume y pause en una sola
    void SetPause(bool pause)
    {
        pauseMenuUI.SetActive(pause);
    }


    public void Resume()
    {
        PauseManager.SetPause(true);
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
    public void QuitGame()
    {
        Debug.Log("Quit Game...");
        Application.Quit();
    }
}
