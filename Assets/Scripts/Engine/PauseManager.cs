using System;
using UnityEngine;


public class PauseManager : MonoBehaviour
{

    static bool paused = false;
    public static bool IsPaused { get { return paused; } }

    public static event Action<bool> OnSetPause;


    public static void TogglePause()
    {
        SetPause(!paused);
    }
    public static void SetPause(bool pause)
    {
        paused = pause;
        Time.timeScale = pause ? 0 : 1;

        if (OnSetPause != null) OnSetPause.Invoke(pause);
    }


    private void OnDisable()
    {
        SetPause(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
    }

}
