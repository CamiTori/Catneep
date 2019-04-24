using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenu : MonoBehaviour {

    public void PlayGame()
    {
        // Hacer que cargue la escena del selector, por nombre en lugar de número
        // por si cambiamos el orden de las escenas.
        SceneManager.LoadScene("Song Selector");
    }

    public void QuitGame ()
    {
        Debug.Log("QUIT!"); //Para ver si funciona 
        Application.Quit();
    }
    








}