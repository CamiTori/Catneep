using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections.ObjectModel;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Catneep.Utils;


/// <summary>
/// Clase que sirve para guardar un array de grupo de notas para ser guardada en Song.
/// </summary>
[Serializable]
public class NoteSheet
{

    // El tamaño de subpaso que usa esta partitura, en caso de que hagamos un cambio a 
    // SongManagerNoteInput.substepDivision y podamos adaptar los tiempo a esta partitura.
    // (Es probable que se tengan que borrar grupos con esto no sin antes dar una advertencia)
    //[SerializeField]
    //uint localSubstepDivision = SongManagerNoteInput.substepDivision;

    // Curvas por defecto para la UI
    const float defaultY = -0.145f;
    const float defaultXSpacing = 0.07f;
    const float defaultHeight = 0.45f;
    /// <summary>
    /// Devuelve las curvas por defecto que se usaría para la UI personalizada de
    /// los indicadores de notas.
    /// </summary>
    /// <param name="quantity">Cantidad de indicadores de notas.</param>
    /// <returns>Curvas para la UI.</returns>
    public static QuadraticCurve[] GetDefaultCurves (int quantity)
    {
        QuadraticCurve[] curves = new QuadraticCurve[quantity];

        float half = (quantity - 1) / 2f;
        for (int i = 0; i < quantity; i++)
        {
            curves[i] = new QuadraticCurve(
                new Vector2(defaultXSpacing * (i - half), defaultY),
                new Vector2(0, defaultHeight), 
                Vector2.zero);
        }

        return curves;
    }

    [SerializeField]
    QuadraticCurve[] uiCurves = null;
    /// <summary>
    /// 
    /// </summary>
    public ReadOnlyCollection<QuadraticCurve> GetUICurves
    {
        get
        {
            return Array.AsReadOnly(uiCurves);
        }
    }

    [SerializeField]
    NoteGroup[] notes;
    public ReadOnlyCollection<NoteGroup> GetNoteGroups { get { return Array.AsReadOnly(notes); } }

    /// <summary>
    /// Devuelve la colección de grupos de notas de la partitura, con los tiempos validados.
    /// </summary>
    /// <returns>Colección de grupos de notas.</returns>
    public ReadOnlyCollection<NoteGroup> GetNoteGroupsValidated
    {
        get
        {
            UpdateGroupTimes();
            return GetNoteGroups;
        }
    }

    // Actualiza el tiempo de inicio de los grupos de notas, dependiendo se sus tiempos relativos.
    // Esto es necesario porque los tiempos absolutos no se serializan ni guardan en el asset.
    void UpdateGroupTimes()
    {
        int previousTime = 0;
        foreach (var group in notes)
        {
            group.UpdateStartTime(previousTime);
            previousTime = group.Substep;
        }
    }

}

/* Versión vieja de las notas
/// <summary>
/// Atributo que nos permite cambiar un int de subpasos a beats en el inspector, 
/// y que además se vea como de sólo lectura. Se usa para mostrar el tiempo de inicio de una nota.
/// </summary>
public sealed class StepsToBeatsLabelAttribute : PropertyAttribute { }

/// <summary>
/// Atributo que nos permite cambiar un int a un enum que dice el nombre de las notas en el inspector.
/// </summary>
public sealed class IntToNoteInputAttribute : PropertyAttribute { }



// Posiblemente esta variable se deje de usar y 
// se cree una clase nueva para representarla mejor en el inspector
// Es probable que usemos la misma clase que usa SongManagerNoteInput, NoteGroup
// añadiendo en el inspector el tiempo relativo al grupo anterior.
/// <summary>
/// Clase que representa una sóla nota para que lo use Song, y sea más visible para el inspector
/// usando tiempo relativo
/// </summary>
[Serializable]
public class Note
{

    // El número de input que representa esta nota
    [SerializeField]
    [IntToNoteInput]
    byte note;
    public byte Input { get { return note; } }

    // Tiempo en subpasos que aparece esta nota respecto a la anterior 
    // (0 significa que está en el mismo grupo que la anterior)
    [SerializeField]
    uint relativeTime = SongManagerNoteInput.substepDivision;
    public uint GetRelativeTime { get { return relativeTime; } }

    // Tiempo en substeps que aparece la nota desde que empieza la canción
    [SerializeField]
    [StepsToBeatsLabel]
    uint startTime;
    public uint StartTime { get { return startTime; } }

    /// <summary>
    /// Constructor que nos permite crear la nota con el tiempo relativo en beats.
    /// </summary>
    /// <param name="note">Número de la nota.</param>
    /// <param name="relativeBeatTime">Tiempo relativo en beats.</param>
    public Note(byte note, float relativeBeatTime)
    {
        this.note = note;
        this.relativeTime = (uint)(Mathf.Max(relativeBeatTime, 0) * SongManagerNoteInput.substepDivision);
    }
    public Note(byte note, uint relativeSubstepsTime)
    {
        this.note = note;
        this.relativeTime = relativeSubstepsTime;
    }


    // Esta función se llama en OnValidate, para asegurarse que todas la variables estén correctas.
    // Asignar el startTime dependiendo de la nota anterior y el relativeTime.
    internal uint ValidateNote(uint previousTime)
    {
        // Asegurarse que el tiempo relativo no sea menor que 0
        if (relativeTime < 0) relativeTime = 0;
        // Asignar el tiempo de inicio de la nota, al tiempo anterior + el tiempo relativo de esta nota
        // Y devolver el tiempo de esta nota
        startTime = previousTime + relativeTime;
        return startTime;
    }

}
*/

#region Probable Versión nueva para el inspector

[Serializable]
public class NoteSheetPrototype
{
    #region Classes
    [Serializable]
    public class NoteGroupEditor
    {
        // Cuanto tiempo después aparece este grupo
        public ushort relativeTime = 1;

        public ushort duration = 0;

        public byte notes;

    }

    [Serializable]
    public class Pattern
    {
        // Id del patrón, única dentro de este objeto solamente
        // unity no soporta diccionarios en el editor, por lo que en su lugar
        // habrá que usar un editor script que se ocupe de que se vea como lista/array
        // en el inspector y que se asegure de que haya sólo un patrón por id.
        // Después cuando se llame GetNotes(), que dicha lista se convierta en diccionario
        // para que el método anterior acceda más rápido a los patrones.
        public string id = "pattern";

        // Un Array con todos los grupos de notas de este patrón
        public NoteGroupEditor[] noteGroups;

    }

    [Serializable]
    public class PatternGroup
    {

        // La id del patrón que vamos a usar.
        // El script de editor tiene que revisar si existe un patrón con esa id.
        public string usePattern = "pattern";

        // Cuantas veces se repite el patrón en cuestión.
        // No puede ser menor que 1.
        public uint repeat = 1;

        // Cuantos substeps más adelante aparece el grupo de patrones
        // respecto al anterior. No tiene que ser menor que 0.
        public uint offset = 0;

    }
    #endregion

    public Pattern[] patterns;

    public PatternGroup sheet;

    public IEnumerable<NoteGroup> GetNoteGroups()
    {
        return Enumerable.Empty<NoteGroup>();
    }

}

#endregion