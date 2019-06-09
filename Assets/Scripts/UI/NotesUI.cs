using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Catneep.UI;
using Catneep.Utils;


public class NotesUI : MonoBehaviour
{

    #region Classes

    [Serializable]
    public class NoteIndicator
    {
        public const float curveSpacing = 0.05f;
        public const float curveResolution = 0.01f;


        public Color color = Color.white;

        private NoteIndicatorUI indicatorUI;
        public Transform GetTransform { get { return indicatorUI.transform; } }

        private PrecomputedEvenCurve curvePath = new PrecomputedEvenCurve();
        public PrecomputedEvenCurve GetCurvePath { get { return curvePath; } }

        public void Initialize(NotesUI owner, RectTransform rectParent, QuadraticCurve curve, int input)
        {
            if (indicatorUI == null)
            {
                indicatorUI = Instantiate(owner.noteIndicatorPrefab, rectParent);
            }

            // Colocamos el indicador segun nos indique la curva
            float scale = rectParent.rect.height * 2;
            indicatorUI.Initialize(curve.FromPoint * scale, this.color, input);

            // Hacemos que la curva empiece desde el punto 0 para que sea relativo al indicador
            // y tratamos de conseguir un camino para las notas
            curve.FromPoint = Vector2.zero;
            curvePath.UpdateCurve(curve, curveSpacing, curveResolution, scale);
        }
        public void ClearIndicator()
        {
            if (indicatorUI != null)
            {
                Destroy(indicatorUI.gameObject);
            }
        }

        public Vector2 GetLerp(float t, float horizontal = 0)
        {
            return curvePath.Lerp(t, horizontal);
        }

        public void OnIconEvent(NoteEventType eventType)
        {
            switch (eventType)
            {
                case NoteEventType.Hit:
                    indicatorUI.OnNoteHit();
                    break;
            }
        } 

    }

    /// <summary>
    /// Representa un grupo de íconos en la UI que representan un grupo de notas de SongManagerNoteInput.
    /// </summary>
    private class GroupIconInstances : IDisposable
    {

        // Están readonly, por lo que sólo se modifican en el constructor
        public readonly NoteGroup group;
        public readonly bool longNote;

        public readonly NoteIcon[] noteIcons;
        public readonly NotesUI owner;

        public readonly float durationScale;
        //public bool LongNote { get { return durationScale > 0; } }

        // Cuantos íconos quedan en este grupo
        private int noteCount = 0;
        // Para eliminar este grupo de íconos, incluyendo este objeto que los referencia
        private readonly Action Remove;

        /// <summary>
        /// Constructor para instanciar un nuevo ícono de nota, asignando sus respectivas variables.
        /// </summary>
        /// <param name="group">La nota de referencia.</param>
        /// <param name="index">El índice de la nota en el array de SongManagerNoteInput.</param>
        /// <param name="owner">La UI dueña.</param>
        public GroupIconInstances(NoteGroup group, NotesUI owner, Action<GroupIconInstances> onRemove, bool nextGroup = false)
        {
            this.group = group;
            this.owner = owner;
            this.longNote = group.IsLongNote;
            this.durationScale = group.GetDuration * Subdivision.subtepSize / owner.beatRange;

            // Tomar el indicador de notas respectivo y asignarle el mismo color al ícono de nota
            noteIcons = new NoteIcon[SongManagerNoteInput.GetMaxInputs];
            for (int i = 0; i < noteIcons.Length; i++)
            {
                if (group.GetNote(i))
                {
                    NoteIndicator indicator = owner.noteIndicators[i];
                    noteIcons[i] = Instantiate(owner.noteIconPrefab, indicator.GetTransform);
                    noteIcons[i].Initialize(indicator, durationScale, i, DestroyIcon);
                    if (nextGroup) noteIcons[i].OnNextNote();

                    noteCount++;
                }
            }

            group.onNoteEvent += OnIconEvent;
            Remove = () => onRemove(this);
        }

        private void OnIconEvent(NoteEventType eventType, int note)
        {
            //Debug.Log("Event = " + eventType + ":" + note);
            // Busca una instancia de ícono que tenga el mismo índice que se le pasó
            NoteIcon icon = noteIcons[note];
            // Revisa si encontró una nota con el mismo índice para continuar con el método
            if (icon == null) return;

            // En función del parámetro eventType, actualiza el ícono
            switch (eventType)
            {
                case NoteEventType.Hit:
                    HitNote(icon, note);
                    break;
                case NoteEventType.Miss:
                    icon.OnMiss(owner.missColor);
                    // TODO: Mostrar más claramente que fallamos esa nota
                    break;
                case NoteEventType.Release:
                    icon.OnRelease();
                    icon.OnMiss(owner.missColor);
                    break;
                case NoteEventType.NextNote:
                    icon.OnNextNote();
                    break;
            }

            owner.noteIndicators[note].OnIconEvent(eventType);
        }
        private void HitNote(NoteIcon icon, int index)
        {
            icon.OnHit();

            if (!longNote)
            {
                DestroyIcon(index);
            }

            // TODO: Mostrar más claramente que acertamos con la nota
        }

        public void UpdatePosition(float t, Wave noteShake = new Wave())
        {
            float horizontal = noteShake.GetY(t);
            foreach (NoteIcon icon in noteIcons)
            {
                if (icon != null) icon.UpdatePosition(t, horizontal, noteShake);
            }
        }

        private void DestroyIcon(int number)
        {
            NoteIcon icon = noteIcons[number];
            if (icon)
            {
                // Destruimos el objeto y asignamos la referencia en el array a null
                Destroy(icon.gameObject);
                noteIcons[number] = null;

                // Si todos los íconos de este grupo se borran
                // simplemente borramos este grupo
                noteCount--;
                if (noteCount <= 0) Remove();
            }
        }

        public void Dispose()
        {
            foreach (var icon in noteIcons)
            {
                if (icon) Destroy(icon.gameObject);
            }
        }
    }

    #endregion

    // Referencia al manager de la canción
    [SerializeField]
    private SongManagerNoteInput manager;
    public SongManagerNoteInput SongManager { get { return manager; } }

    [Space]
    [Header("Note Icons")]

    [Tooltip("El prefab del icono que representan las notas")]
    [SerializeField]
    private NoteIcon noteIconPrefab;

    // Cuantos segundos nos adelantamos del momento actual de la canción para spawnear las notas
    [SerializeField]
    private float maxSecondsRange = 2f;
    // En que posición de interpolación empezamos a despawnear
    [Range(-1, 0)]
    [SerializeField]
    private float despawnPosition = -.2f;

    [Tooltip("Que color adquieren las notas cuando fallamos")]
    [SerializeField]
    private Color missColor = Color.gray;

    [Space]
    [Header("Note Indicators")]

    // El prefab del indicador de notas
    [SerializeField]
    private NoteIndicatorUI noteIndicatorPrefab;
    // El rectTransform que sirve de parent para los indicadores de notas
    [SerializeField]
    private RectTransform indicatorsParent;

    [Tooltip("Los indicadores de notas, cada uno en su posición en el array respectiva al input que representan")]
    [SerializeField]
    private NoteIndicator[] noteIndicators = new NoteIndicator[(int)Difficulty.Hard];

    // Lista que contiene todas las instancias de los íconos, para actualizarlos dependiendo del manager
    private List<GroupIconInstances> groupIconsList = new List<GroupIconInstances>();
    // El siguiente índice de grupo de íconos que se debe spawnear
    private int nextGroupToSpawn = 0;

    private float beatRange;
    private float lerpScale;


    [Space]

    [Header("Effects")]

    [SerializeField]
    private Wave noteShake;
    public float NoteShakeFrequency
    {
        set { noteShake.Frequency = value; }
    }
    public float NoteShakeIntensity
    {
        set { noteShake.intensity = value; }
    }
    public void ResetShake()
    {
        noteShake = default(Wave);
    }

    [Header("Score")]

    [SerializeField]
    private Text scoreText;


    private void Awake()
    {
        // Revisar que exista una referencia asignada al manager
        // y en ese caso, registrar a los eventos del mismo nuestras funciones correspondientes
        if (manager)
        {
            manager.OnSongStartEvent += OnSongStart;
            manager.OnSongTimeSetEvent += OnSongTimeSet;
            manager.OnTimeUpdateEvent += UpdateUI;
            manager.OnScoreUpdate += score => scoreText.text = score.ToString();
        }
        else
        {
            Debug.LogWarning("Ningun Manager fue asignado a NotesUI.");
            this.enabled = false;
            return;
        }
    }
    private void OnSongStart()
    {
        // Actualizamos la posición de las notas dependiendo de lo que nos indica la
        // partitura de notas
        var curves = manager.CurrentNotesheet.GetUICurves;
        for (int i = 0; i < noteIndicators.Length; i++)
        {
            if (i < curves.Count)
            {
                noteIndicators[i].Initialize(this, indicatorsParent, curves[i], i + 1);
            }
            else noteIndicators[i].ClearIndicator();
        }

        // Cuando se carga una canción calculamos la escala de beat un en la UI
        // Para posicionar y escalar correctamente las notas
        beatRange = maxSecondsRange * manager.BPS;
        lerpScale = 1f / beatRange;
    }


    private void OnSongTimeSet()
    {
        // Cuando se cambie el tiempo de la canción, asegurarse que la lista esté vacía
        // y encontrar el índice de la nota actual que está la canción.
        ClearAllGroups();
        nextGroupToSpawn = manager.GetCurrentGroupIndex;
    }

    private void UpdateUI()
    {
        // Spawnear los íconos que sean necesarios, solicitando al manager una colección de notas
        // a partir del número de nota para spawnear y un rango de tiempo máximo respecto al tiempo actual.
        // Después le pasamos esa colección al método SpawnIcons(IEnumerable<Note>)
        SpawnGroups(manager.GetGroupRange(nextGroupToSpawn, beatRange));

        // Iniciamos un loop para cada instancia de nota, usando una copia de la lista como array
        // en caso de que se modifique el tamaño de la lista y nos dé error por romper el loop.
        foreach (var gi in groupIconsList.ToArray())
        {
            // Calcular la posición respecto al tiempo
            float position = (gi.group.BeatTime - manager.CurrentBeat) * lerpScale;
            //Debug.Log(position);
            // Revisar si la posicion es menor a la posición de despawn, en ese caso
            // eliminar este ícono y seguir al siguiente directamente
            if (position + gi.durationScale < despawnPosition)
            {
                ClearGroup(gi);
                continue;
            }

            // Cambiar la anchored position, que es la posición respecto al indicador de nota
            gi.UpdatePosition(position, noteShake);
        }
    }

    /// <summary>
    /// A partir de una colleción de notas, se spawnean íconos que representan las notas en la UI.
    /// </summary>
    /// <param name="spawnNotes"></param>
    private void SpawnGroups(IEnumerable<NoteGroup> spawnGroups)
    {
        foreach (var group in spawnGroups)
        {
            // Por cada nota en la colección, crear una nueva instancia de NoteIconInstance,
            // con sus respectivas variables y agregarla a la lista.
            // Después aumentar el número de la siguiente nota para spawnear.
            groupIconsList.Add(new GroupIconInstances(group, this, ClearGroup, groupIconsList.Count < 1));
            nextGroupToSpawn++;
        }
    }
    /// <summary>
    /// Elimina un ícono de nota en específico y la borra de la lista actual.
    /// </summary>
    /// <param name="group">La instancia para eliminar.</param>
    private void ClearGroup(GroupIconInstances group)
    {
        group.Dispose();
        groupIconsList.Remove(group);
    }
    /// <summary>
    /// Elimina todos los íconos de nota en la lista y vacía la misma.
    /// </summary>
    private void ClearAllGroups()
    {
        groupIconsList.ForEach(i => i.Dispose());
        groupIconsList.Clear();
    }

}
