using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Catneep.Songs;


public class GameManager : MonoBehaviour
{

    // El nombre de la escena de la pantalla de carga
    private const string loadingSceneName = "Loading Screen";

    // El singleton de esta clase
    private static GameManager singleton;


    // Modo debug
    private static bool debug = false;
    public static bool DebugMode { get { return debug; } }
#if UNITY_EDITOR
    private const string editorDebugKey = "DebugMode";

    private static int editorDebugMode = -1;
    public static bool EditorDebugMode
    {
        get
        {
            if (editorDebugMode < 0 || editorDebugMode > 1)
            {
                editorDebugMode = EditorPrefs.GetBool(editorDebugKey, false) ? 1 : 0;
            }

            return editorDebugMode != 0;
        }
        set
        {
            int intValue = value ? 1 : 0;
            if (intValue != editorDebugMode)
            {
                editorDebugMode = intValue;
                EditorPrefs.SetBool(editorDebugKey, value);
            }
        }
    }
#endif


    // Una lista de todas las canciones, se cargan cuando comienza el juego
    private static Song[] songs;
    public static Song[] GetSongs { get { return (Song[])songs.Clone(); } }


    // Gracias a este parámetro esta función comienza cuando empieza el juego
    // antes de que se cargue la primera escena.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnGameStart()
    {
        // Empezar creando un singleton, que no se destruya cuando se carga una nueva escena
        // y que podemos usar para las coroutines.
        singleton = new GameObject("Game Manager").AddComponent<GameManager>();
        DontDestroyOnLoad(singleton);


        // Leemos las lineas de comando o las preferencias del editor
        // depende en que estamos
#if UNITY_EDITOR
        debug = EditorDebugMode;
#else
        ReadCommandArgs(System.Environment.GetCommandLineArgs());
#endif


        // Cargamos la escena en la que estábamos con LoadScene de esta clase
        // (Temporal, es probable que termine empezando por cargar el menú)
        LoadScene(SceneManager.GetActiveScene().name, LoadSongs());
    }

    // Si ejecutamos el juego con argumentos de comando los leemos y hacemos las
    // acciones que debería hacer cada uno
    private static void ReadCommandArgs(string[] args)
    {
        foreach (string arg in args)
        {
            switch (arg)
            {
                // Modo debug
                case "-d":
                case "--debug":
                    debug = true;
                    break;
            }
        }
    }

    // Para cuando el juego comienza, para obtener una lista de las canciones
    private static IEnumerator LoadSongs()
    {
        yield return SongList.LoadAsyncSingleton();
        songs = SongList.Singleton.SongListCopy;

        // Old version, with asset bundles

        //var loadOperation = AssetBundleManager.LoadAllAssetsAsync<Song>("songs");
        //yield return loadOperation.Execute();

        //songs = loadOperation.Assets;
    }


    /// <summary>
    /// Carga una escena con una pantalla de carga y espera a que se cargue por completo.
    /// </summary>
    /// <param name="sceneName">Nombre de la escena a cargar.</param>
    /// <param name="onPreload">Que hacemos antes de cargar la escena, 
    /// un IEnumerator como lo usaría una coroutine.</param>
    public static void LoadScene(string sceneName, params IEnumerator[] onPreload)
    {
        // Empezamos yendo la escena de "cargado" que ya viene pre-cargada
        // (En player settings está como asset precargado)
        SceneManager.LoadScene(loadingSceneName);

        // Después empezamos con la corutina de carga
        singleton.StartCoroutine(LoadSceneCoroutine(sceneName, onPreload));
    }
    private static IEnumerator LoadSceneCoroutine(string sceneName, params IEnumerator[] onPreload)
    {
        // Primero hacemos todas las acciones pre-carga
        foreach (var loadAction in onPreload) yield return loadAction;

        // Después cargamos la escena de forma asíncrona y simplemente esperamos 
        // al progreso de carga.
        var loadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (!loadOperation.isDone)
        {
            // Acá podemos mostrar el progreso actual de carga de la escena
            // loadOperation.progress / 0.9f
            yield return null;
        }
    }


    public const string noteUISceneName = "Note UI";

    /// <summary>
    /// Método que nos permite comenzar con una canción en cualquier momento,
    /// cargando la escena de juego después de una pantalla de carga, que carga el
    /// asset bundle y el audio.
    /// </summary>
    /// <param name="song">La canción para que se reproduzca.</param>
    public static void StartSong(Song song)
    {
        LoadScene(noteUISceneName, SongManager.LoadSongCoroutine(song));
    }

    public static void RestartSong()
    {
        if (SongManager.CurrentSong == null) return;

        LoadScene(noteUISceneName, SongManager.LoadSongCoroutine(SongManager.CurrentSong));
    }

}
