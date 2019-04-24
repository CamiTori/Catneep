using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Cuando haya un evento de notas, que tipo de evento ocurre.
/// </summary>
public enum NoteEventType { Hit = 0, Miss = 1, Release = 2, NextNote = 3 }

/// <summary>
/// Clase que representa un grupo de notas de la canción, que se tocan al mismo tiempo.
/// </summary>
[Serializable]
public class NoteGroup
{

    /// <summary>
    /// Constructor, a partir de las notas, el tiempo relativo y la duración.
    /// Todas las variables que son serializadas.
    /// </summary>
    /// <param name="relativeTime">Tiempo relativo en subpasos, respecto al tiempo anterior</param>
    /// <param name="notes"></param>
    public NoteGroup(byte notes, int relativeTime, int duration)
    {
        // No permitir que el tiempo relativo o la duración sean menores que 0
        if (relativeTime < 0) relativeTime = 0;
        if (duration < 0) duration = 0;

        // Asignar las variables correspondientes
        this.notes = notes;
        this.relativeTime = relativeTime;
        this.duration = duration;
    }
    public NoteGroup(NoteGroup copyFrom) : this(copyFrom.notes, copyFrom.relativeTime, copyFrom.duration)
    {

    }


    // A partir del tiempo relativo, actualiza el tiempo absoluto de este grupo.
    internal void UpdateStartTime(int previousTime)
    {
        startSubstep = relativeTime + previousTime;
        UpdateBeatTime();
    }
    // Actualiza el tiempo en beats dependiendo del tiempo en subpasos
    // y la constante del tamaño de los pasos.
    private void UpdateBeatTime()
    {
        startBeatTime = startSubstep * Subdivision.subtepSize;
    }

    /// <summary>
    /// Devuelve verdadero si el grupo tiene notas y no está vacío.
    /// </summary>
    public bool HasNotes { get { return notes != 0; } }

    // Cuantos subpasos después aparece el grupo cuando empieza el anterior,
    // o si no tiene un grupo anterior, cuando empieza la canción.
    [SerializeField]
    private int relativeTime = Subdivision.substepDivision;
    public int GetRelativeTime { get { return relativeTime; } }

    // Es un unsigned int, lo que permite valores desde 0 hasta 4 294 967 295.
    [NonSerialized]
    private int startSubstep;
    /// <summary>
    /// El tiempo en el que las notas aparecen, en subpasos.
    /// </summary>
    public int Substep { get { return startSubstep; } }


    // No serializamos esta variable ya que sólo la usamos cuando estemos reproduciendo las notas
    // y no la vamos a querer guardar en el inspector.
    [NonSerialized]
    private float startBeatTime;
    /// <summary>
    /// El tiempo que aparecen las notas, en beats.
    /// </summary>
    public float BeatTime { get { return startBeatTime; } }


    /// <summary>
    /// La duración del grupo de notas en substeps.
    /// Un 0 sería una nota que sólo escucha el ButtonDown.
    /// </summary>
    // TODO: añadir notas largas.
    [SerializeField]
    private int duration;
    public int GetDuration { get { return duration; } }
    public bool IsLongNote { get { return duration > 0; } }


    // Un número en el que cada uno de sus bits representa la presencia de una nota
    // 00000101 = La primera y tercera nota están en este grupo
    [SerializeField]
    private byte notes;
    public byte NoteBits { get { return notes; } }
    /// <summary>
    /// Devuelve una copia del array de bits que guarda que notas 
    /// se deben tocar en este grupo, dependiendo de su posición.
    /// </summary>
    public BitArray GetNoteArray(int arrayLength)
    {
        return new BitArray(new int[] { notes })
        {
            Length = arrayLength
        };
    }

    /// <summary>
    /// Getter que nos permite obtener las notas como si fuera <see cref="GetNote(int)"/>
    /// </summary>
    /// <param name="noteNumber">Número de nota/input.</param>
    /// <returns>Si existe la nota.</returns>
    public bool this[int noteNumber] { get { return GetNote(noteNumber); } }
    /// <summary>
    /// Esto nos permite usar el foreach, devolviendonos los números de las notas que existen.
    /// </summary>
    /// <returns>Numerator de números de notas.</returns>
    public IEnumerator<int> GetEnumerator()
    {
        int length = SongManagerNoteInput.GetMaxInputs;
        for (int i = 0; i < length; i++)
        {
            if (GetNote(i)) yield return i;
        }
    }

    /// <summary>
    /// Ver si esa nota existe o no en el grupo, con el número de input.
    /// </summary>
    /// <param name="number">Número de nota/input.</param>
    /// <returns>Existe la nota.</returns>
    public bool GetNote(int number)
    {
        return (notes & 1 << number) != 0;
    }

    /// <summary>
    /// Evento que ocurre cuando se acierta o falla una de las notas del grupo.
    /// NoteEventType: tipo de evento.
    /// int: número de la nota del evento, relativa al grupo.
    /// </summary>
    public event Action<NoteEventType, int> onNoteEvent;

    public void CallAllNoteEvents(NoteEventType eventType)
    {
        if (onNoteEvent == null) return;

        foreach (int note in this) onNoteEvent.Invoke(eventType, note);
    }
    public void CallNoteEvent(NoteEventType eventType, int index)
    {
        onNoteEvent.Invoke(eventType, index);
    }

    public NoteGroup Clone()
    {
        return new NoteGroup(this);
    }


    /// <summary>
    /// Comparador para que el Linq.Distinct(IEqualityComparer) pueda comparar los grupos
    /// por startTime.
    /// </summary>
    public static readonly TimeComparer timeComparer = new TimeComparer();
    public class TimeComparer : IEqualityComparer<NoteGroup>
    {
        public bool Equals(NoteGroup x, NoteGroup y)
        {
            return x.startSubstep == y.startSubstep;
        }

        public int GetHashCode(NoteGroup obj)
        {
            return obj.startSubstep;
        }
    }

}

