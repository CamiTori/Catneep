using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class BeatTestUI : MonoBehaviour
{

    // El manager al que esta UI está conectada
    public SongManager manager;
    // El prefab del icono que representa una nota
    public Image beatIconPrefab;
    // El parent que se les asigna a los iconos que se instancian
    public Transform beatBar;
    [Space]
    // Para mostrar si el jugador fallo o acertó la nota (Miss, Good, Perfect...)
    public Text noteInputScore;
    [Space]
    // Indica desde donde y hasta donde se mueven los iconos del ritmo
    public float beatIconSpawnDistance = 700f;
    // Cuanto se adelanta en beats los iconos para spawnearse antes de que suene el beat que le corresponde
    public float beatSpawnTime = 2.5f;
    [Space]
    // Para calibrar el input adelantandolo o atrasándolo
    public float inputCalibration = 0;
    // Las distintas puntuaciones que se pueden obtener al tocar una nota, tiene que ordenarse
    // del tiempo más pequeño al más grande para que funcione correctamente
    public NoteScoreIndicator[] noteScores;
    // La puntuación si falla
    public NoteScoreIndicator missScore;

    // Variable que se calcula de la tolerancia de tiempo máxima de noteScores
    float maxTimeTolerance = 0;

    /// <summary>
    /// Para contener la información de cada puntuación posible, y cual debe mostrar la UI
    /// </summary>
    [System.Serializable]
    public class NoteScoreIndicator
    {
        // El mensaje que se muestra
        public string displayText;
        // La máxima diferencia de tiempo para que salga esta puntuación
        public float timeTolerance = 0.1f;
        // El color que el correspondiente icono de nota cambia
        public Color iconTint = Color.white;
    }

    // Lista de todos los iconos spawneados
    List<BeatIconInstance> beatIconList = new List<BeatIconInstance>();

    // Indicadores de que nota se debe spawnear y que nota se debe obtener el input
    int nextBeatToSpawn = 1, nextBeatInput = 1;

    // struct que contiene la información de un icono beat, y el número de beat al que pertenece
    struct BeatIconInstance
    {
        public readonly Image icon;
        public readonly uint beatNumber;

        public BeatIconInstance (BeatTestUI owner, uint beatNumber)
        {
            // Cuando se construye este struct, se spawnea un icono 
            icon = Instantiate(owner.beatIconPrefab, owner.beatBar);
            this.beatNumber = beatNumber;
        }

        public void DestroyIcon ()
        {
            Destroy(icon.gameObject);
        }
    }

    private void Start()
    {
        // Conseguir el mayor tiempo de tolerancia de las puntuaciones
        foreach (var score in noteScores)
        {
            if (score.timeTolerance > maxTimeTolerance) maxTimeTolerance = score.timeTolerance;
        }
    }

    private void LateUpdate()
    {
        // Asegurarse de que haya un manager asignado
        if (!manager)
        {
            Debug.LogWarning("No se ha asignado ningún manager al BeatTestUI.");
            this.enabled = false;
            return;
        }

        UpdateBeatUI();
        HandleInput();
    }

    void UpdateBeatUI()
    {
        // Spawnear los iconos de beat necesarios
        float currentBeatToSpawn = manager.CurrentBeat + beatSpawnTime;
        while (nextBeatToSpawn < currentBeatToSpawn)
        {
            beatIconList.Add(new BeatIconInstance(this, (uint)nextBeatToSpawn));
            nextBeatToSpawn++;
        }

        // Posicionar los iconos dependiendo des su numero de beat y el beat actual de la canción
        foreach (var beat in beatIconList.ToArray())
        {
            float horizontalPos = ((beat.beatNumber - currentBeatToSpawn) * beatIconSpawnDistance / beatSpawnTime) + 
                beatIconSpawnDistance;

            // Si se va muy a la izquierda, despawnear el icono
            if (horizontalPos < -beatIconSpawnDistance)
            {
                beatIconList.Remove(beat);
                beat.DestroyIcon();
                continue;
            }

            // Posicionar el icono
            beat.icon.rectTransform.anchoredPosition = new Vector2(horizontalPos, 0);
        }
    }

    float timeDifference;
    void HandleInput()
    {
        // Conseguir el siguiente beat que se tiene que tocar
        BeatIconInstance targetBeat = GetBeat(nextBeatInput);
        // Si el número es 0 (No se encontró el beat) no seguir con esta función
        if (targetBeat.beatNumber <= 0) return;

        // La diferencia de tiempo en segundos del tiempo que le corresponde a la nota del tiempo actual
        timeDifference = (manager.CurrentBeat - nextBeatInput) * manager.BeatDuration;
        timeDifference -= inputCalibration;

        // Cuando se presione espacio, dar la puntuación correspondiente
        if (Input.GetKeyDown(KeyCode.Space))
        {
            foreach (var score in noteScores)
            {
                if (Mathf.Abs(timeDifference) <= score.timeTolerance)
                {
                    GoToNextBeat(targetBeat, score);
                    return;
                }
            }
            DisplayText(missScore);
        }

        if (timeDifference > maxTimeTolerance) GoToNextBeat(targetBeat, missScore);
    }
    void GoToNextBeat(BeatIconInstance currentBeat, NoteScoreIndicator score)
    {
        DisplayText(score);
        currentBeat.icon.color = score.iconTint;

        nextBeatInput++;
    }
    void DisplayText (NoteScoreIndicator score)
    {
        noteInputScore.text = string.Format("{0} ({1})", score.displayText,
            timeDifference.ToString("+0.000;-0.000;0"));
    }

    BeatIconInstance GetBeat(int number)
    {
        foreach (var beat in beatIconList)
        {
            if (beat.beatNumber == number) return beat;
        }
        return default(BeatIconInstance);
    }

}
