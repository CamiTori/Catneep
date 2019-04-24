using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Catneep.Utils;


public class SongManagerNoteInput : SongManager
{

    [Header("Notes")]

    // El máximo tiempo de diferencia para que podamos acertar con el input una nota
    // en segundos
    [SerializeField]
    private float maxTimeTolerance = 0.1f;

    [Header("Score")]

    [SerializeField]
    private int tapScoreAmount = 5;
    [SerializeField]
    private int longNoteScorePerStep = 1;

    [SerializeField]
    private int noteStreakPerMultiplier = 20;
    [SerializeField]
    private int maxMultiplier = 3;

    private int currentScore = 0;
    private bool shouldUpdateScore = false;

    private int currentNoteStreak = 0;
    private int streakMultiplier = 1;
    public int StreakMultiplier
    {
        set
        {
            if (value == streakMultiplier) return;

            streakMultiplier = value;
            UpdateMultiplier();
        }
    }

    private float currentMultiplier = 1;

    public event Action<int> OnScoreUpdate;

    // The delta substep since the last update
    private int lastSubstep = 0;

    // La dificultad o cantidad de notas
    private Difficulty useDifficulty = Difficulty.Easy;
    private float difficultyMultiplier = 1;

    // La partitura actual de notas
    private NoteSheet noteSheet;
    public NoteSheet CurrentNotesheet { get { return noteSheet; } }
    // El array de grupo de notas de la canción que estamos tocando
    private NoteGroup[] currentNotes = new NoteGroup[0];


    private static byte currentNoteInputs = 1;
    public static byte GetMaxInputs { get { return currentNoteInputs; } }
    // Un array de bits que nos indica que notas nos faltan tocar en el grupo (1: falta tocar)
    private BitArray currentNotesLeft = new BitArray(0);

    // Notas largas
    private struct LongNoteInfo
    {
        public int groupIndex;
        public int startStep;
        public int duration;

        public bool IsActive { get { return duration > 0; } }
        public int EndStep { get { return startStep + duration; } }

        public LongNoteInfo(NoteGroup noteGroup, int groupIndex)
        {
            this.startStep = noteGroup.Substep;
            this.duration = noteGroup.GetDuration;
            this.groupIndex = groupIndex;
        }
    }
    // Array que contiene todas las notas largas que se están tocando en este momento
    private LongNoteInfo[] currentLongNotes = new LongNoteInfo[0];

    // El índice del grupo actual
    private int currentGroupIndex;
    public int GetCurrentGroupIndex { get { return currentGroupIndex; } }
    private NoteGroup currentGroup;
    public NoteGroup GetCurrentGroup { get { return currentGroup; } }

    // La diferencia de tiempo actual con el grupo de notas actual
    private float currentGroupTimeDifference;


    /// <summary>
    /// Revisa cada uno de los valores de nextNotesLeft para ver si queda alguna nota por tocar.
    /// (Alguna está en true)
    /// </summary>
    /// <returns>Si quedan notas</returns>
    private bool AreNotesLeft()
    {
        foreach (bool value in currentNotesLeft)
        {
            if (value) return true;
        }
        return false;
    }

    public void SetDifficulty(Difficulty difficulty, float scoreMultiplier)
    {
        // To ensure we are assigning a valid difficulty and not any number
        if (Enum.IsDefined(typeof(Difficulty), difficulty))
        {
            useDifficulty = difficulty;
            difficultyMultiplier = scoreMultiplier;
            UpdateMultiplier();
        }
    }

    public void UpdateMultiplier()
    {
        currentMultiplier = streakMultiplier * difficultyMultiplier;
    }


    /// <summary>
    /// Para obtener un rando de grupo de notas que estén a partir de un índice
    /// con un rango de tiempo en segundos desde el tiempo actual.
    /// </summary>
    /// <param name="startIndex">Índice inicial</param>
    /// <param name="beatRange">Rango de tiempo en segundos</param>
    /// <returns></returns>
    public IEnumerable<NoteGroup> GetGroupRange(int startIndex, float beatRange)
    {
        // Tratar de conseguir el grupo con ese índice
        NoteGroup startGroup = GetNoteGroup(startIndex);
        if (beatRange <= 0 || startGroup == null)
        {
            // Devolver una colección vacía si el tiempo era igual o menor a 0
            // o si no se encontró ningún grupo.
            return Enumerable.Empty<NoteGroup>();
        }

        // Conseguir el máximo rango de tiempo
        float maxBeatTime = CurrentBeat + beatRange;
        if (startGroup.BeatTime >= maxBeatTime)
        {
            // Si el grupo inicial supera el rango de tiempo, devolvemos una colección vacía
            return Enumerable.Empty<NoteGroup>();
        }

        // Encontrar hasta que grupo se cumple el rango de tiempo
        int toIndex = startIndex;
        for (int i = startIndex + 1; i < currentNotes.Length && currentNotes[i].BeatTime <= maxBeatTime; i++)
        {
            toIndex = i;
        }

        // Devolver la colección de notas con su rango correspondiente que encontramos
        return currentNotes.Skip(startIndex).Take(toIndex - startIndex + 1);
    }

    /// <summary>
    /// Trata de conseguir la siguiente nota con el número de indice que tenga,
    /// si no tiene ninguna nota en ese indice devuelve null
    /// </summary>
    /// <param name="i">Número de índice</param>
    /// <returns>La nota</returns>
    private NoteGroup GetNoteGroup(int i)
    {
        if (i >= 0 && i < currentNotes.Length)
        {
            return currentNotes[i];
        }
        else return null;
    }


    // Cuando la canción se cargue, obtenemos las notas que tenga la misma
    protected override void OnSongLoaded()
    {
        // Almacenamos el momento que empezamos a obtener las notas
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Conseguimos la cantidad máxima cantidad de notas
        // Y seteamos la longitud de los bit array de notas actuales
        currentNoteInputs = (byte)useDifficulty;
        currentNotesLeft.Length = currentNoteInputs;
        currentLongNotes = new LongNoteInfo[currentNoteInputs];

        // Obtenemos los grupos de notas, válidos, con tiempos únicos y
        // ordenados para evitar cualquier clase de problemas
        noteSheet = CurrentSong.GetNotesheet(useDifficulty);
        currentNotes = noteSheet.GetNoteGroupsValidated.Where(g => g.HasNotes)
            .Distinct(NoteGroup.timeComparer).OrderBy(n => n.Substep).ToArray();

        // Debugeamos cuanto tardamos en obtener las notas
        stopwatch.Stop();
        Debug.LogFormat("Loaded note sheet in {0} ms.", stopwatch.Elapsed.TotalMilliseconds);
    }

    // Cuando se setee la canción calcular por cual grupo de notas debemos empezar
    protected override void OnSongTimeSet()
    {
        // Asignar el currentGroupIndex a -1, indicando que busque el primer grupo de notas (0)
        currentGroupIndex = FindCurrentGroup() - 1;

        // Conseguir el grupo actual de notas
        GetNextGroup();

        // Assign the last substep
        lastSubstep = CurrentSubstep;
    }
    private int FindCurrentGroup()
    {
        float currentStep = CurrentBeat * Subdivision.substepDivision;
        for (int i = 0; i < currentNotes.Length; i++)
        {
            if (currentNotes[i].Substep >= currentStep) return i;
        }
        return -1;
    }

    /// <summary>
    /// Método que a partir del indice del siguiente grupo (nextGroupIndex), agrupando las notas 
    /// con el mismo tiempo. 
    /// Una vez encontrada, asigna a nextGroupIndex el siguiente índice para la siguiente búsqueda.
    /// </summary>
    private void GetNextGroup()
    {
        // Empezar haciendo que todas las notas por tocar estén en falso
        currentNotesLeft.SetAll(false);

        // Conseguir la primera nota del grupo, para tener un tiempo de referencia para comparar
        // el tiempo de las siguientes notas. Si lo conseguimos la agregamos a la lista.
        // En caso contrario de que sea nula, cancelamos esta función.
        currentGroup = GetNoteGroup(++currentGroupIndex);
        if (currentGroup == null) return;

        // Make next notes glow
        currentGroup.CallAllNoteEvents(NoteEventType.NextNote);

        // Copiar el array de notas para tocar del grupo actual
        currentNotesLeft = currentGroup.GetNoteArray(currentNoteInputs);
    }

    protected override void OnUpdateSongTime(float seconds, float beats)
    {
        // Revisamos que tengamos un grupo de notas por tocar
        NoteGroup group = GetCurrentGroup;
        if (group != null)
        {
            // Calcular la diferencia de tiempo actual de ese grupo
            currentGroupTimeDifference = (beats - group.BeatTime) * BeatDuration;
            bool canHitNotes = Mathf.Abs(currentGroupTimeDifference) <= maxTimeTolerance;
            //Debug.LogFormat("Siguiente grupo de notas en: {0:0.000} s", currentGroupTimeDifference);

            // Detectar cada input que se presione, con su número de nota correspondiente
            for (int i = currentNoteInputs; i > 0; i--)
            {
                if (Input.GetButtonDown("Note " + i))
                {
                    if (!canHitNotes)
                    {
                        ResetNoteStreak();
                        break;
                    }

                    HandleInputDown((byte)(i - 1));
                }
            }

            // Check if it's note too late to hit the notes
            if (currentGroupTimeDifference > maxTimeTolerance)
            {
                // En caso contrario, lo consideramos un fallo y seguimos al siguiente grupo
                MissCurrentGroup();
            }
        }

        // Actualizamos las notas largas
        UpdateLongNotes();

        // Update the last substep
        lastSubstep = CurrentSubstep;
    }

    private void UpdateLongNotes()
    {
        // Revisar que se mantengan las notas largas
        for (int i = 0; i < currentNoteInputs; i++)
        {
            // Actualizar el estado de la nota larga si está activa
            if (currentLongNotes[i].IsActive)
            {
                //Debug.Log(i);
                int countFrom = Math.Max(lastSubstep, currentLongNotes[i].startStep);
                int countTo = Math.Min(CurrentSubstep, currentLongNotes[i].EndStep);
                AddScore((countTo - countFrom) * longNoteScorePerStep);

                // Si ya pasamos o no el tiempo de la nota larga
                bool offTime = CurrentSubstep >= currentLongNotes[i].EndStep;

                // Detectar cuando termina la nota larga
                if (offTime || Input.GetButtonUp("Note " + (i + 1)))
                {
                    // Llamamos el evento de soltar la nota larga
                    // si soltamos la nota en lugar de esperar a que termine
                    if (!offTime)
                    {
                        currentNotes[currentLongNotes[i].groupIndex].CallNoteEvent(NoteEventType.Release, i);
                        //Debug.Log("Nota larga suelta.");
                    }

                    // Borramos la información de la nota larga actual
                    currentLongNotes[i] = default(LongNoteInfo);
                }
            }
        }
    }

    private bool HandleInputDown(int i)
    {
        // Cuando toquemos una tecla revisamos si hay una nota o no que nos queda para tocar
        if (currentNotesLeft.Get(i))
        {
            // Si hay una nota seteamos el bitArray de notas que nos quedan en ese bit
            // como falso y llamamos el evento de hit.
            currentNotesLeft.Set(i, false);
            currentGroup.CallNoteEvent(NoteEventType.Hit, i);

            // Si es una nota larga, la añadimos al array de notas largas
            if (currentGroup.IsLongNote)
            {
                //Debug.Log("Long note start");
                currentLongNotes[i] = new LongNoteInfo(currentGroup, currentGroupIndex);
            }

            // Add the score and the note streak
            AddScore(tapScoreAmount);
            IncrementNoteStreak();

            // Si no quedan notas, pasamos al siguiente grupo
            if (!AreNotesLeft())
            {
                GetNextGroup();
            }

            return true;
        }
        else
        {
            // Si teniamos que tocar esa nota, fallamos el grupo actual
            MissCurrentGroup();
            return false;
        }
    }
    private void MissCurrentGroup()
    {
        // Llamamos el evento de fallo de notas
        // con las que nos quedan
        for (byte i = 0; i < currentNotesLeft.Length; i++)
        {
            if (currentNotesLeft[i]) currentGroup.CallNoteEvent(NoteEventType.Miss, i);
        }

        // Reset notestreak
        ResetNoteStreak();

        // Después de eso buscamos el siguiente grupo
        GetNextGroup();
    }

    protected void AddScore(int score)
    {
        if (score <= 0) return;

        currentScore += (int)(score * currentMultiplier);
        shouldUpdateScore = true;
    }

    private void IncrementNoteStreak()
    {
        SetNoteStreak(currentNoteStreak + 1);
    }
    private void ResetNoteStreak()
    {
        SetNoteStreak(0);
    }
    private void SetNoteStreak(int streak)
    {
        currentNoteStreak = streak;
        StreakMultiplier = Mathf.Min(streak / noteStreakPerMultiplier + 1, maxMultiplier);
    }

    protected virtual void LateUpdate()
    {
        if (shouldUpdateScore)
        {
            if (OnScoreUpdate != null) OnScoreUpdate(currentScore);
            shouldUpdateScore = false;
        }
    }

    protected override IEnumerable<string> DebugInfo()
    {
        foreach(var debug in base.DebugInfo())
        {
            yield return debug;
        }

        yield return null;

        yield return "Next group: " + currentNotesLeft.ToBinaryText();
        yield return "Long notes: " + currentLongNotes.ToBinaryText<LongNoteInfo>(l => l.IsActive);

        yield return null;

        yield return "Difficulty: " + useDifficulty;
        yield return "Score: " + currentScore;
        yield return "Note Streak: " + currentNoteStreak;
        yield return "Multiplier: x" + currentMultiplier;
    }

}
