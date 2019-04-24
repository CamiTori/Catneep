using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// Clase que se ocupa de la canción, de reproducirla y el ritmo de la misma.
/// Puede heredarse para crear una clase que, por ejemplo, se ocupe de las notas.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SongManager : MonoBehaviour
{

    // Singleton, para permitir solamente UNA instancia de SongManager a la vez.
    // Clases hijas incluidas.
    private static SongManager currentSingleton;
    public static SongManager Current { get { return currentSingleton; } }


    internal static IEnumerator LoadSongCoroutine(Song song)
    {
        yield return song.LoadSong();

        SongManager.song = song;
    }

    
    private static Song song = null;
    /// <summary>
    /// La canción actual que estamos reproduciendo.
    /// </summary>
    public static Song CurrentSong { get { return song; } }

    public Song assignSong;

    // If the audio already started at least once
    private bool audioHasStarted = false;

    // Indica si el tiempo de la canción se tiene que actualizar la posicion en segundos y beats en el Update.
    private bool playingSong = false;
    /// <summary>
    /// Indica si el tiempo de la canción se está actualizando.
    /// </summary>
    public bool IsPlaying { get { return playingSong; } }

    // Cuantos segundos esperamos para que empiece la canción
    [SerializeField]
    private float startSongDelay = 3f;

    [Space]

    /// <summary>
    /// Si debemos saltear al beat del float skipToBeat en tiempo
    /// </summary>
    [SerializeField]
    private bool skip;
    /// <summary>
    /// A que beat salteamos cuando empecemos y si el bool skip está activado.
    /// </summary>
    [SerializeField]
    private float skipToBeat = 0;


    // El último tiempo del motor de sonido del frame anterior
    private float lastDspTime;

    
    /// <summary>
    /// How long is the current song in seconds.
    /// </summary>
    public float SongLength { get; private set; }

    /// <summary>
    /// La posición actual de la canción (En segundos)
    /// </summary>
    public float CurrentSongPosition { get; private set; }

    /// <summary>
    /// From 0.0 to 1.0 the current position relative to its length.
    /// </summary>
    public float CurrentSongProgress { get { return CurrentSongPosition / SongLength; } }



    /// <summary>
    /// La posición actual de la canción (En beats)
    /// </summary>
    public float CurrentBeat { get; private set; }
    /// <summary>
    /// El beat en número entero, redondeado al número más bajo (2.8 -> 2, -0.4 -> -1)
    /// Útil para el debug
    /// </summary>
    public int CurrentBeatFloored { get { return Mathf.FloorToInt(CurrentBeat); } }


    /// <summary>
    /// La posición actual de la canción (En beats)
    /// </summary>
    public int CurrentSubstep { get; private set; }


    /// <summary>
    /// Devuelve cuantos beats hay en un segundo.
    /// </summary>
    public float BPS { get; private set; }


    /// <summary>
    /// Devuelve cuanto dura un beat en segundos.
    /// </summary>
    public float BeatDuration { get; private set; }


    /// <summary>
    /// El componente audio source que reproduce el audio de la canción.
    /// </summary>
    private AudioSource audioSource;
    public AudioSource GetAudioSource { get { return audioSource; } }

    public float Velocity { get { return audioSource.pitch; } }


    /// <summary>
    /// Evento que se llama cuando se setea una canción y se empieza.
    /// </summary>
    public event Action OnSongStartEvent;
    /// <summary>
    /// Evento que se llama después de que una canción se asigna el tiempo de
    /// la canción, se llama en SetSongTime y despues de SetSongOnLoad.
    /// </summary>
    public event Action OnSongTimeSetEvent;
    /// <summary>
    /// Cada vez que esta instancia pasa por Update, y se actualiza el tiempo.
    /// </summary>
    public event Action OnTimeUpdateEvent;
    /// <summary>
    /// Called when the song is ended.
    /// </summary>
    public event Action OnSongEndEvent;

    /// <summary>
    /// Si el GameManager tiene el modo debug activado y una instancia de SongManager entra en Start().
    /// </summary>
    public static event Action<SongManager> OnDebugStart;

    public event Func<IEnumerable<string>> AdditionalDebugInfo;


    // En el Awake, conseguimos el AudioSource, que siempre estará
    // Gracias al atributo [RequireComponent(typeof(AudioSource))]
    // Y además indicamos que este es el singleton actual, si no había uno
    private void Awake()
    {
        // Nos aseguramos de que haya sólo una instancia de SongManager
        if (!currentSingleton)
        {
            currentSingleton = this;
        }
        else if (currentSingleton != this)
        {
            DestroyImmediate(gameObject);
            Debug.LogWarning("An instance of SongManager already exists! Can't create another one.");
            return;
        }

        // Obtenemos el componente audio source
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Nos suscribimos al evento de pausa de PauseManager para poder pausar la canción
        PauseManager.OnSetPause += OnSetPause;

        OnAwake();
    }
    protected virtual void OnAwake()
    {

    }


    private void OnDestroy()
    {
        // Cuando este objeto se destruya (Como por ejemplo en la carga de escenas)
        // nos desuscribimos del evento de SetPause
        PauseManager.OnSetPause -= OnSetPause;
        song = null;
    }



    // Comienzo de la canción (No necesariamente el audio)

    /// <summary>
    /// Si tenemos un audio asignado en el inspector, empezamos con esa canción.
    /// </summary>
    private void Start()
    {
        // Si estamos en modo debug spawneamos la UI Debug
        if (GameManager.DebugMode && OnDebugStart != null) OnDebugStart.Invoke(this);

        OnStart();
    }
    protected virtual void OnStart()
    {
        // Si no hay ninguna canción asignada, asignamos la del inspector
        if (!song) song = assignSong;

        // Empezamos con la canción que tengamos asignada
        StartSong(song);
    }

    /// <summary>
    /// Comienza con la canción que está asignada en este momento.
    /// </summary>
    public void StartSong(Song song)
    {
        // Antes de comenzar nos aseguramos de que la canción sea válida
        if (!Song.CheckValid(song))
        {
            Debug.LogWarning("Invalid song assigned.");
            return;
        }

        // Paramos el AudioSource y asignamos el clip
        audioSource.Stop();
        audioSource.clip = song.Audio;

        // Asignamos la canción que nos pasaron
        SongManager.song = song;
        Debug.LogFormat("Starting song: \"{0}\"", song);

        // Calculamos cuanto beats hay por segundo y cuanto dura un beat,
        // para no calcular todo el tiempo esta variable
        BPS = song.BPM / 60f;
        BeatDuration = 60f / song.BPM;
        SongLength = song.Audio.length;

        // Empezar con la corrutina de comienzo de la canción, por si el audio no estaba cargado
        StartCoroutine(StartSongCoroutine());
    }
    /// <summary>
    /// La corrutina de comienzo para la canción, cargamos el audio de
    /// la canción en caso de que no haya cargado ya, así no hay desincronizaciones.
    /// </summary>
    private IEnumerator StartSongCoroutine()
    {
        // Por si no habiamos cargado el audio lo cargamos, si ya había cargado,
        // esto se saltea solo.
        yield return song.LoadSong();

        // Llamar el método virtual OnSongLoaded y el evento onSongLoaded
        OnSongLoaded();
        if (OnSongStartEvent != null) OnSongStartEvent.Invoke();

        // Setear el tiempo de esta canción dependiendo de la variable skip,
        // si está true, seteamos el tiempo de la canción dependiendo del skipToBeat convertido en segundos,
        // si está false, ponemos el delay negativo, haciendo que esperemos a que pase dicho tiempo
        SetSongTime(skip ? skipToBeat * BeatDuration : -startSongDelay);

        // Indicamos que podemos actualizar el tiempo de la canción
        playingSong = true;
    }
    /// <summary>
    /// Método que se llama cada vez que se setea una canción.
    /// (protected override para sobreescribir este método con una clase hija)
    /// </summary>
    protected virtual void OnSongLoaded() { }




    // Seteo de tiempo

    /// <summary>
    /// Método para cambiar el tiempo de la canción de la canción, en segundos.
    /// Se puede usar un valor negativo para que espere ese tiempo antes de reproducir el audio.
    /// </summary>
    /// <param name="timeInSeconds">El tiempo que seteamos la canción.</param>
    private void SetSongTime(float timeInSeconds)
    {
        // Debugeamos el tiempo al que seteamos la canción
        Debug.Log("Setting song time to " + timeInSeconds + " s.");

        // Primero paramos el audio de cualquier audio que se esté reproduciendo
        audioSource.Stop();
        audioHasStarted = false;

        // Almacenar el tiempo cuando una canción comienza en el tiempo dsp del audio
        // se divide por el pitch para que se corresponda el tiempo.
        CurrentSongPosition = timeInSeconds - song.BeatOffset;
        lastDspTime = (float)AudioSettings.dspTime;

        UpdateTimes();

        // Empezar con el audio de inmediato si el tiempo está en el correcto
        if (timeInSeconds >= 0 && timeInSeconds < audioSource.clip.length)
        {
            audioHasStarted = true;
            audioSource.Play();
            audioSource.time = timeInSeconds;
        }

        // Llamamos el método heredable y el evento que indican que ya seteamos el tiempo de la canción
        OnSongTimeSet();
        if (OnSongTimeSetEvent != null) OnSongTimeSetEvent.Invoke();
    }
    /// <summary>
    /// Método que se llama cada vez que se setea una canción.
    /// (protected override para sobreescribir este método con una clase hija)
    /// </summary>
    protected virtual void OnSongTimeSet() { }

    /// <summary>
    /// Reproduce el audio de la canción con un delay.
    /// </summary>
    /// <param name="delay">El delay en segundos.</param>
    /*
    void PlayAudioDelayed(float delay)
    {
        audioSource.Stop();
        audioSource.PlayDelayed(delay / Velocity);
    }
    */


    // En caso de que pausemos guardamos el tiempo que estamos y cuando resumamos
    // seteamos el start time al correspondiente
    private void OnSetPause(bool pause)
    {
        if (!playingSong) return;

        if (pause)
        {
            // Pausamos el AudioSource
            audioSource.Pause();
        }
        else
        {
            if (CurrentSongPosition < 0)
            {
                // Cuando resumamos y el tiempo en el que estábamos era menor que 0, 
                // reproducimos el audio con delay
                //PlayAudioDelayed(-songPosition);
            }
            else audioSource.UnPause(); // Si no, simplemente despausamos la canción

            // Seteamos el último tiempo dsp al actual para que retome donde debería estar la canción
            lastDspTime = (float)AudioSettings.dspTime;
        }
    }



    
    // Actualización de tiempo

    private void Update()
    {
        // Actualizamos el tiempo de la canción si estamos reproduciendola y no estamos en pausa
        if (playingSong && !PauseManager.IsPaused)
        {
            UpdateDebug();
            UpdateSongTime();
        }
    }

    // Además de calcular la última posición de la canción, llama al método virtual
    // y el evento de UpdateSongTime
    private void UpdateSongTime ()
    {
        // Calcular la posición de la canción en segundos
        //songPosition = ((float)AudioSettings.dspTime - dspStartTimeSong) * audioSource.pitch - song.offset;
        float currentDspTime = (float)AudioSettings.dspTime;
        float timeDelta = (currentDspTime - lastDspTime) * Velocity;
        CurrentSongPosition += timeDelta;
        lastDspTime = currentDspTime;

        // Para empezar con el audio si el tiempo es mayor a 0
        if (!audioHasStarted && CurrentSongPosition > 0)
        {
            audioHasStarted = true;
            audioSource.Play();
        }

        // Update all the other times besides the SongPosition
        UpdateTimes();

        // Check if the song has ended and if we should keep calling the update by setting
        // playingSong to false
        if (CurrentSongPosition > SongLength)
        {
            playingSong = false;
            if (OnSongEndEvent != null) OnSongEndEvent();
            //Debug.Log("Song Ended");
        }

        // Llamar el método para que las clases hijas puedan overridear
        // y actuar después de que se actualice el tiempo,
        // también llamar el evento de onTimeUpdate
        OnUpdateSongTime(CurrentSongPosition, CurrentBeat);
        if (OnTimeUpdateEvent != null) OnTimeUpdateEvent();

        // Update manual tween time
        DG.Tweening.DOTween.ManualUpdate(timeDelta * Time.timeScale, timeDelta);
    }
    /// <summary>
    /// Método que se llama en update después de actualizar el tiempo de una canción.
    /// (protected override para sobreescribir este método con una clase hija)
    /// </summary>
    protected virtual void OnUpdateSongTime(float seconds, float beats) { }


    private void UpdateTimes()
    {
        // Calcular la posición en beats
        CurrentBeat = CurrentSongPosition * BPS;

        // Calcular la posición en subpasos
        CurrentSubstep = (int)(CurrentBeat * Subdivision.substepDivision);
    }


    #region Debug

    /// <summary>
    /// Devuelve información del tiempo para el debug.
    /// </summary>
    protected string GetTimeDebug { get { return string.Format("Time: {0:+00;-00}:{1:00.000}", CurrentSongPosition / 60, Math.Abs(CurrentSongPosition % 60)); } }
    /// <summary>
    /// Devuelve información de la velocidad para el debug.
    /// </summary>
    protected string GetVelocityDebug { get { return string.Format("Velocity: x{0}", Velocity); } }

    // Si estamos en modo debug, llamamos este método en update
    private void UpdateDebug()
    {
        if (!GameManager.DebugMode) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            ChangeVelocity(true);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            ChangeVelocity(false);
        }
    }

    /// <summary>
    /// Devuelve toda la información escrita debug de este manager.
    /// </summary>
    /// <returns>Debug info.</returns>
    public string GetDebugInfo()
    {
        IEnumerable<string> debugInfo = DebugInfo();
        if (AdditionalDebugInfo != null)
        {
            foreach (Func<IEnumerable<string>> additionalInfo in AdditionalDebugInfo.GetInvocationList())
            {
                debugInfo = debugInfo.Concat(additionalInfo.Invoke());
            }
        }
        return string.Join("\n", debugInfo.ToArray());
    }
    protected virtual IEnumerable<string> DebugInfo()
    {
        if (song == null) yield break;

        yield return GetTimeDebug;
        yield return string.Format("Beat: {0:0.000} ({1})", CurrentBeat, CurrentBeatFloored);
        yield return string.Format("Substep: {0}", CurrentSubstep);
        yield return GetVelocityDebug;

        yield return null;

        yield return string.Format("BPM: {0}", song.BPM);
        yield return string.Format("Offset: {0} s", song.BeatOffset);
    }

    // Todas las velocidades posibles que podemos asignar
    private static readonly float[] velocities = new float[]
    {
        -3f,
        -1f,
        -0.5f,
        0f,
        0.25f,
        0.5f,
        0.75f,
        0.9f,
        1f,
        1.5f,
        2f,
        3f,
    };
    public static readonly int normalVelocityIndex = Array.IndexOf(velocities, 1f);
    public static readonly int stopVelocityIndex = Array.IndexOf(velocities, 0f);

    private int currentVelocity = normalVelocityIndex;
    public int GetCurrentVelocityIndex { get { return currentVelocity; } }

    protected void ChangeVelocity(bool increment)
    {
        int setVelocity = currentVelocity + (increment ? 1 : -1);
        if (setVelocity < 0 || setVelocity >= velocities.Length) return;

        SetVelocityIndex(setVelocity);
    }
    protected void SetVelocityIndex(int velocityIndex)
    {
        audioSource.pitch = velocities[velocityIndex];
        currentVelocity = velocityIndex;
    }

    #endregion


#if UNITY_EDITOR
    // Validar las variables del inspector para que estén el los rangos que deben estar
    private void OnValidate()
    {
        startSongDelay = Mathf.Max(startSongDelay, 0);
    }
#endif

}
