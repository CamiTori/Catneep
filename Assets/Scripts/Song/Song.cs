using System;
using System.Collections;
using UnityEngine;


/// <summary>
/// Clase que contiene toda la información de una canción (Metadata).
/// Como el título, bpm, las notas y el propio audio de la canción.
/// Gracias al [CreateAssetMenu] podemos crear una instancia en Assets > Create > Song Info
/// </summary>
[CreateAssetMenu(fileName = "New Song", menuName = menuPath + "Song Info")]
public sealed class Song : ScriptableObject
{

    public const string menuPath = "Songs/";

    /// <summary>
    /// Si la canción no es nula y es válida, usa <see cref="IsValid"/>.
    /// </summary>
    public static bool CheckValid(Song song)
    {
        return song != null && song.IsValid;
    }
    /// <summary>
    /// Si la canción es válida y tiene el audio asignado.
    /// </summary>
    public bool IsValid
    {
        get
        {
            return audio != null;
        }
    }

    [SerializeField, HideInInspector]
    private AudioClip audio;
    /// <summary>
    /// El clip de audio que usa la canción.
    /// </summary>
    public AudioClip Audio { get { return audio; } }

    [Space]

    [SerializeField, HideInInspector]
    private string title = "unnamed song";
    /// <summary>
    /// El título de esta canción.
    /// </summary>
    public string Title { get { return title; } }

    [SerializeField, HideInInspector]
    private string author = "unknown";
    public string Author { get { return author; } }

    [TextArea(3, 6)]
    [SerializeField, HideInInspector]
    private string description;

    [Space]

    [SerializeField, HideInInspector]
    private float bpm = 100;
    /// <summary>
    /// Cuantos beats o pulsos hay en la canción por segundo.
    /// </summary>
    public float BPM { get { return bpm; } }

    [Tooltip("En segundos, cuanto tarda en empezar el primer beat.")]
    [SerializeField, HideInInspector]
    private float offset = 0f;
    /// <summary>
    /// En segundos, cuanto tarda en empezar el primer beat.
    /// </summary>
    public float BeatOffset { get { return offset; } }

    [Space]

    [SerializeField]
    private Sprite backgroundImage;
    public Sprite BackgroundImage { get { return backgroundImage; } }

    [Space]

    [SerializeField, HideInInspector]
    private NoteSheet easyNoteSheet;

    [SerializeField, HideInInspector]
    private NoteSheet hardNoteSheet;

    public NoteSheet GetNotesheet(Difficulty difficulty)
    {
        switch(difficulty)
        {
            case Difficulty.Easy:
                return easyNoteSheet;
            case Difficulty.Hard:
                return hardNoteSheet;
            default:
                return null;
        }
    }


    /// <summary>
    /// Trata de cargar el audio de una canción y espera a que se termine de cargar,
    /// se tiene que llamar desde una coroutine.
    /// Esto funciona gracias a la opcion "Load in Background" que tiene cada audio.
    /// </summary>
    internal IEnumerator LoadSong()
    {
        // Si no hay audio o ya ha cargado, volvemos
        if (!audio || audio.loadState == AudioDataLoadState.Loaded) yield break;

        // Guardamos el tiempo que empezamos a cargar el audio
        Debug.Log("Loading song audio...");
        DateTime startTime = DateTime.Now;

        // Cargamos el audio y esperamos mientras se carga
        audio.LoadAudioData();
        yield return new WaitWhile(() => audio.loadState == AudioDataLoadState.Loading);

        // Debugear cuantos milisegundos tardamos en cargar
        Debug.Log("Audio loaded in " + DateTime.Now.Subtract(startTime).TotalMilliseconds + " ms.");
    }

    /// <summary>
    /// Para que cuando queramos esta canción en string, cuando la debugeemos.
    /// Nos devuelva el título de la canción.
    /// </summary>
    /// <returns>Título de la canción.</returns>
    public override string ToString()
    {
        return title;
    }

}
