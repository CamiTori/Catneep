using System;
using UnityEngine;
using UnityEditor;

using Catneep.Data;
using Catneep.Utils;


[CustomEditor(typeof(Song))]
public class EditorSong : Editor
{

    #region Ajustador de BPM

    // Constante del volumen para reproducir el audio (0f-1f)
    const float playVolume = 0.1f;

    // Estilo de texto que nos permite mostrar rich text (Letras en negrita, cursiva, etc...)
    static GUIStyle richTextStyle;
    // El mensaje que el botón de ajuste muestra
    static string ButtonMessage { get { return EditingBPM ? "Editando BPM..." : "Detectar BPM manualmente."; } }

    // La canción (Clase, no audio) que se está ajustando
    static Song bpmEditSong = null;
    // Para indicar si se está ajustando o no una canción, básicamente si actualSong no es nula
    static bool EditingBPM { get { return bpmEditSong != null; } }

    // La cantidad de pulsos totales, el primero se usa para contar el tiempo inicial
    static int pressCount = 0;
    // Si la cantidad de pulsos es suficiente como para ajustar el valor de BPM
    static bool EnoughPresses { get { return pressCount > 1; } }

    // El tiempo del sistema en el que el primer pulso ocurre
    static DateTime startTime;
    // El BPM actual para mostrar mientras se ajusta y para aplicar cuando se termine
    static float currentBPM;

    // Instancia del audio source para reproducir la música
    static AudioSource musicPlayer;
    /// <summary>
    /// Revisar si ya existe una instancia del audio source
    /// </summary>
    static void CheckIfPlayerExists()
    {
        // No hacer nada de lo de abajo si ya hay una instancia
        if (musicPlayer) return;

        // Crear la instancia y añadir un componente AudioSource para asignarlo a la variable musicPlayer
        // Despues escondemos el objeto para que no salga en la Hierarchy y no se guarde en la escena
        musicPlayer = new GameObject("Editor Music Player").AddComponent<AudioSource>();
        musicPlayer.gameObject.hideFlags = HideFlags.HideAndDontSave;

        // Asignarle el volumen
        musicPlayer.volume = playVolume;
    }

    void OnEditingBPM()
    {
        // Conseguir el evento actual
        if (e.type == EventType.KeyDown)
        {
            // Si se presiona la tecla (KeyDown), realizar la acción correspondiente
            switch (e.keyCode)
            {
                case KeyCode.Space:
                    // En espacio llamar el método note press para hacer saber que presionamos una nota
                    OnEditNotePress();
                    break;
                case KeyCode.Escape:
                case KeyCode.Backspace:
                    // Cancelar si se presiona Esc o Backspace (Arriba del Enter)
                    CancelEditing();
                    e.Use();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    // Aplicar el BPM actual cuando se presione Enter o Intro
                    EndEditing();
                    break;
            }
        }
    }
    void OnEditNotePress()
    {
        // Aumentar la cantidad de pulsos
        pressCount++;
        if (pressCount <= 1)
        {
            // Si era el primer pulso, sólo asignar el tiempo inicial
            startTime = DateTime.Now;
        }
        else
        {
            // Cuando ya tengamos más pulsos, calcular el BPM actual
            currentBPM = (float)((pressCount - 1) / DateTime.Now.Subtract(startTime).TotalMinutes);
        }

        // Decirle a Unity que vuelva a dibujar este inspector
        Repaint();
    }
    /// <summary>
    /// Devuelve un string con la cantidad de pulsos actuales.
    /// Si no son suficientes, muestra un mensaje de que faltan pulsos.
    /// </summary>
    /// <returns>Cantidad de pulsos</returns>
    static string GetCurrentBPMText()
    {
        if (!EnoughPresses)
        {
            return "(Se requieren más pulsos)";
        }
        else return currentBPM.ToString("0.0");
    }


    void StartEditing(Song editSong)
    {
        // Primero asegurarse de que haya una instancia del reproductor
        CheckIfPlayerExists();

        // Asignar la canción actual y la cantidad de pulsos a 0
        bpmEditSong = editSong;
        pressCount = 0;

        // Asignar el clip actual del reproductor al de la canción que queremos editar y la reproducimos
        if (editSong.Audio != null)
        {
            musicPlayer.clip = editSong.Audio;
            musicPlayer.Play();
        }
        else Debug.LogWarningFormat("No se ha asignado ningun audio a la canción {0}.", target);
    }
    void EndEditing()
    {
        // Revisar si estabamos editando una canción o si son suficientes pulsos
        // y asignamos el BPM actual
        if (EditingBPM && EnoughPresses)
        {
            serializedObject.FindProperty("bpm").floatValue = currentBPM;
        }

        // Llamamos CancelEditing para parar la canción y terminar de editarla
        CancelEditing();
    }
    void CancelEditing()
    {
        // Parar con el audio actual y asignar actualSong a null
        if (musicPlayer) musicPlayer.Stop();
        bpmEditSong = null;

        // Redibujar el inspector
        Repaint();
    }

    #endregion


    private const string groupNotesName = "notes";
    private const string relativeTimeName = "relativeTime";
    private const string durationName = "duration";


    private const string curvesArrayName = "uiCurves";

    private const string curveFromName = "from";
    private const string curveToLocalName = "toLocal";
    private const string curveCurvatureLocalName = "curvatureLocal";


    private static Vector2 UIOffset;
    private static float UIScale = 1f;

    private static readonly Difficulty[] difficulties = (Difficulty[])Enum.GetValues(typeof(Difficulty));

    private Song song;
    private static readonly string[] noteLabels = { "First", "Second", "Third", "Fourth", "Fifth", "Sixth" };
    private GUIContent[] groupElementMenu;

    private Event e;

    private void OnEnable()
    {
        SceneView.onSceneGUIDelegate += OnSceneViewGUI;

        song = (Song)target;

        // Cuando seleccionamos una instancia de Song crear y asignar la GUIStyle de richTextStyle
        // Esto se hace en este método porque Unity lo deja crear en este método
        richTextStyle = EditorUtils.GetRichTextStyle;
        groupElementMenu = new GUIContent[] { new GUIContent("Duplicate Group"), new GUIContent("Delete Group") };

        UpdateScale();
    }
    private void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneViewGUI;

        // Por si estabamos editando el bpm lo cancelamos
        CancelEditing();
    }

    private static void UpdateScale()
    {
        GetScale(out UIOffset, out UIScale);
    }
    private static void GetScale(out Vector2 offset, out float scale)
    {
        if (Camera.main != null)
        {
            scale = Camera.main.orthographicSize * 4;
            offset = Camera.main.transform.position;
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            scale = canvas.GetComponent<RectTransform>().rect.height;
            offset = canvas.transform.position;
            return;
        }

        offset = Vector2.zero;
        scale = 10;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        e = Event.current;
        serializedObject.UpdateIfRequiredOrScript();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("audio"));

        EditorGUILayout.Space();

        EditorUtils.Header("Info");

        EditorGUILayout.PropertyField(serializedObject.FindProperty("title"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("author"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorUtils.Header("Calibration");
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("offset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bpm"));
        // Mostrar el botón de ajuste y revisar si se cliquea
        if (GUILayout.Button(ButtonMessage) && !EditingBPM)
        {
            // Si se cliquea y no hay ninguna canción editándose, empezar con la edición
            // de la canción que se está seleccionando
            StartEditing(song);
        }
        else if (EditingBPM)
        {
            // Todo esto sólo pasa cuando se edita una canción

            // Llamar el método que recibe el input
            OnEditingBPM();

            // Mostrar la información de las teclas que se pueden presionar
            EditorGUILayout.LabelField("Pulsa <b>Espacio</b> al compás de la música.", richTextStyle);
            EditorGUILayout.LabelField("<b>Enter</b> para terminar y asignar el BPM.", richTextStyle);
            EditorGUILayout.LabelField("<b>Esc</b> para cancelar.", richTextStyle);

            EditorGUILayout.Space();

            // Mostrar la información que tenemos hasta ahora
            EditorGUILayout.LabelField("Pulsos totales: " + (pressCount));
            EditorGUILayout.LabelField("BPM actual: " + GetCurrentBPMText());
        }

        // Por rendimiento, si estamos editando el bpm no dibujamos el resto del inspector
        if (EditingBPM) return;

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorUtils.Header("Note Sheets");

        foreach (var difficulty in difficulties)
        {
            DrawNoteSheet(difficulty);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawNoteSheet(Difficulty difficulty)
    {
        NoteSheet noteSheet;
        SerializedProperty sheetProp;
        GetNotesheet(difficulty, out noteSheet, out sheetProp);

        EditorGUILayout.Separator();
        sheetProp.isExpanded = EditorGUILayout.Foldout(sheetProp.isExpanded, "Notesheet: " + difficulty, true);
        if (sheetProp.isExpanded)
        {
            EditorGUI.indentLevel++;

            // UI Curves
            DrawNoteSheetCurves(difficulty, sheetProp.FindPropertyRelative(curvesArrayName));

            // Notes
            DrawNotesheetNotes(difficulty, sheetProp.FindPropertyRelative("notes"));

            EditorGUI.indentLevel--;
        }
    }

    private void DrawNoteSheetCurves(Difficulty difficulty, SerializedProperty curvesArrayProp)
    {
        int targetSize = (int)difficulty;

        if (curvesArrayProp.arraySize != targetSize)
        {
            curvesArrayProp.arraySize = targetSize;
        }

        curvesArrayProp.isExpanded = EditorGUILayout.Foldout(curvesArrayProp.isExpanded, "UI personalizada", true);
        if (!curvesArrayProp.isExpanded) return;

        EditorGUI.indentLevel++;

        if (GUILayout.Button("Reset"))
        {
            QuadraticCurve[] curves = NoteSheet.GetDefaultCurves(targetSize);
            for (int i = 0; i < targetSize; i++)
            {
                SerializedProperty curveProp = curvesArrayProp.GetArrayElementAtIndex(i);
                curveProp.FindPropertyRelative(curveFromName).vector2Value = curves[i].FromPoint;
                curveProp.FindPropertyRelative(curveToLocalName).vector2Value = curves[i].ToLocal;
                curveProp.FindPropertyRelative(curveCurvatureLocalName).vector2Value = curves[i].CurvatureLocal;
            }
        }

        for (int i = 0; i < targetSize; i++)
        {
            //EditorGUILayout.PropertyField(curvesArrayProp.GetArrayElementAtIndex(i), 
            //   new GUIContent("Curve " + (i + 1)), true);

            SerializedProperty curveProp = curvesArrayProp.GetArrayElementAtIndex(i);
            curveProp.isExpanded = EditorGUILayout.Foldout(curveProp.isExpanded, "Curve " + (i + 1));
            if (!curveProp.isExpanded) continue;

            EditorGUILayout.PropertyField(curveProp.FindPropertyRelative(curveFromName));
            EditorGUILayout.PropertyField(curveProp.FindPropertyRelative(curveToLocalName));
            EditorGUILayout.PropertyField(curveProp.FindPropertyRelative(curveCurvatureLocalName));
        }

        EditorGUI.indentLevel--;

    }

    private void DrawNotesheetNotes(Difficulty difficulty, SerializedProperty notesArrayProp)
    {
        notesArrayProp.isExpanded = EditorGUILayout.Foldout(notesArrayProp.isExpanded, "Notes", true);
        if (!notesArrayProp.isExpanded) return;

        EditorGUI.indentLevel++;

        // Buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Import .txt"))
        {
            string path = EditorUtility.OpenFilePanel("Open note sheet .txt", "", "txt");

            NoteGroup[] notes = null;
            if (NotesheetText.Import(path, ref notes) && notes != null)
            {
                AssignNoteArray(notesArrayProp, notes);
            }
        }
        if (GUILayout.Button("Export .txt"))
        {
            string path = EditorUtility.SaveFilePanel("Save note sheet as .txt", "", "notesheet.txt", "txt");
            NotesheetText.Export(song, path, difficulty);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        string ext = NoteInfo.notesheetFileFormat;
        if (GUILayout.Button("Import ." + ext))
        {
            string path = EditorUtility.OpenFilePanel("Open note sheet ." + ext, "", ext);
            if (!string.IsNullOrEmpty(path))
            {
                AssignNoteArray(notesArrayProp, NoteInfo.LoadNoteSheetAsNoteGroups(path, song.BPM));
            }
        }
        if (GUILayout.Button("Export ." + ext))
        {
            string path = EditorUtility.SaveFilePanel("Save note sheet as ." + ext, "", "notesheet." + ext, ext);
            if (!string.IsNullOrEmpty(path))
            {
                NoteInfo.SaveNotesheetFromNoteGroups(song.GetNotesheet(difficulty).GetNoteGroups, (byte)difficulty, 
                    song.BPM, path);
            }
        }
        GUILayout.EndHorizontal();


        // Array
        int oldSize = notesArrayProp.arraySize;
        notesArrayProp.arraySize = Math.Max(EditorGUILayout.DelayedIntField("Size", oldSize), 0);
        if (oldSize <= 0)
        {
            for (int i = 0; i < notesArrayProp.arraySize; i++)
            {
                notesArrayProp.GetArrayElementAtIndex(i).FindPropertyRelative(relativeTimeName).intValue = 
                    Subdivision.substepDivision;
            }
        }

        string[] labels = new string[(int)difficulty];
        Array.Copy(noteLabels, labels, labels.Length);
        for (int i = 0, lastStep = 0; i < notesArrayProp.arraySize; i++)
        {
            DrawNoteGroup(difficulty, notesArrayProp.GetArrayElementAtIndex(i), i, ref lastStep, labels);
        }

        EditorGUI.indentLevel--;
    }

    private void DrawNoteGroup(Difficulty difficulty, SerializedProperty groupProp, int i,
        ref int lastStep, string[] noteLabels)
    {
        SerializedProperty relativeTimeProp = groupProp.FindPropertyRelative(relativeTimeName);
        lastStep += relativeTimeProp.intValue;
        float startBeat = lastStep * Subdivision.subtepSize;

        Rect foldoutRect = EditorGUILayout.GetControlRect();
        groupProp.isExpanded = EditorGUI.Foldout(foldoutRect, groupProp.isExpanded, 
            string.Format("Group {0}: Beat {1}", i + 1, startBeat));
        if (groupProp.isExpanded)
        {
            //EditorGUI.indentLevel++;

            SerializedProperty noteFlagsProp = groupProp.FindPropertyRelative(groupNotesName);
            SerializedProperty durationTimeProp = groupProp.FindPropertyRelative(durationName);

            byte flags = unchecked((byte)EditorGUILayout.MaskField("Notes", noteFlagsProp.intValue, noteLabels));
            noteFlagsProp.intValue = flags != 0 ? flags : 1;
            relativeTimeProp.intValue = SubstepTimeField("Relative Time", relativeTimeProp.intValue);
            durationTimeProp.intValue = SubstepTimeField("Duration Time", durationTimeProp.intValue);

            EditorGUILayout.LabelField("Start", " Beat " + startBeat);

            //EditorGUI.indentLevel--;
        }

        if (e.type == EventType.ContextClick)
        {
            Vector2 mousePos = e.mousePosition;
            if (foldoutRect.Contains(mousePos))
            {
                EditorUtility.DisplayCustomMenu(new Rect(mousePos.x, mousePos.y, 0, 0),
                    groupElementMenu, -1, OnGroupContextMenuSelected, groupProp);
                e.Use();
            }
        }
    }

    private static void OnGroupContextMenuSelected(object groupProp, string[] options, int selected)
    {
        SerializedProperty prop = (SerializedProperty)groupProp;

        switch (selected)
        {
            case 0:
                prop.DuplicateCommand();
                break;
            case 1:
                prop.DeleteCommand();
                break;
        }

        prop.serializedObject.ApplyModifiedProperties();
    }

    // Draws a field to set the time in steps and show the time in beats as a read-only
    private static int SubstepTimeField(string label, int value)
    {
        Rect controlRect = EditorGUILayout.GetControlRect();
        float intFieldX = controlRect.width * .4f;

        Rect rect = new Rect(controlRect) { width = intFieldX };
        EditorGUI.LabelField(rect, label);

        rect.x = intFieldX;
        rect.width = 65;
        value = EditorGUI.DelayedIntField(rect, value);

        rect.x += 35;
        rect.width = controlRect.width - rect.x;
        float n = value * Subdivision.subtepSize;
        EditorGUI.LabelField(rect, string.Format("/{0} = {1} {2}", Subdivision.substepDivision, 
            n, (n == 1 ? "beat" : "beats")));

        return value;
    }


    private void AssignNoteArray(SerializedProperty notesArrayProp, NoteGroup[] groupArray)
    {
        notesArrayProp.ClearArray();
        for (int i = 0; i < groupArray.Length; i++)
        {
            notesArrayProp.InsertArrayElementAtIndex(i);
            AssignNoteGroupValues(notesArrayProp.GetArrayElementAtIndex(i), groupArray[i]);
        }
    }
    private void AssignNoteGroupValues(SerializedProperty groupProp, NoteGroup noteGroup)
    {
        groupProp.FindPropertyRelative(groupNotesName).intValue = noteGroup.NoteBits;
        groupProp.FindPropertyRelative(relativeTimeName).intValue = noteGroup.GetRelativeTime;
        groupProp.FindPropertyRelative(durationName).intValue = noteGroup.GetDuration;
    }

    #region DrawSceneView

    private void OnSceneViewGUI(SceneView sv)
    {
        serializedObject.UpdateIfRequiredOrScript();

        foreach (var difficulty in difficulties)
        {
            NoteSheet noteSheet;
            SerializedProperty property;
            GetNotesheet(difficulty, out noteSheet, out property);
            //noteSheet.DrawSceneView(song, UIOffset, UIScale, property);
            DrawSceneViewNotesheet(property.FindPropertyRelative(curvesArrayName));
        }

        serializedObject.ApplyModifiedProperties();
    }

    public void DrawSceneViewNotesheet(SerializedProperty curvesArrayProp)
    {
        if (!curvesArrayProp.isExpanded) return;

        for (int i = 0; i < curvesArrayProp.arraySize; i++)
        {
            DrawSceneViewCurve(curvesArrayProp.GetArrayElementAtIndex(i), -1);
        }
        /*
        foreach (var curve in uiCurves)
        {
            curve.DrawSceneView(song, offset, scale, -1);
        }
        */
    }

    private const float arrowWidth = 25e-3f;
    private const float arrowLength = 0.05f;

    public void DrawSceneViewCurve(SerializedProperty property, int arrowDirection = 0)
    {
        // Cache values

        SerializedProperty fromProp = property.FindPropertyRelative(curveFromName);
        SerializedProperty toLocalProp = property.FindPropertyRelative(curveToLocalName);
        SerializedProperty curvatureLocalProp = property.FindPropertyRelative(curveCurvatureLocalName);

        Vector2 originalFrom = fromProp.vector2Value;
        Vector2 originalTo = toLocalProp.vector2Value + originalFrom;
        Vector2 originalCurvature = curvatureLocalProp.vector2Value + originalFrom;

        float scaleInverted = 1f / UIScale;


        // Dibujar los puntos y lineas que lo unen
        float handleSize = HandleUtility.GetHandleSize(originalFrom) * .1f;
        Handles.color = Color.blue;

        // From handle
        Vector2 from = Handles.FreeMoveHandle(originalFrom * UIScale + UIOffset, Quaternion.identity,
            handleSize, Vector2.zero, Handles.CircleHandleCap);
        fromProp.vector2Value = (from - UIOffset) * scaleInverted;
        //Handles.DrawWireDisc(this.from, Vector3.back, handleSize);

        // To handle
        Vector2 to = Handles.FreeMoveHandle(originalTo * UIScale + UIOffset, Quaternion.identity,
            handleSize, Vector2.zero, Handles.CircleHandleCap);
        toLocalProp.vector2Value = (to - UIOffset) * scaleInverted - originalFrom;
        //Handles.DrawWireDisc(ToPoint, Vector3.back, handleSize);

        // Curvature handle
        Vector2 curvature = Handles.FreeMoveHandle(originalCurvature * UIScale + UIOffset, Quaternion.identity,
            handleSize, Vector2.zero, Handles.CircleHandleCap);
        curvatureLocalProp.vector2Value = (curvature - UIOffset) * scaleInverted - originalFrom;
        //Handles.DrawWireDisc(CurvaturePoint, Vector3.back, handleSize);

        // Linea punteada que une los puntos de la curva bezier
        Handles.DrawDottedLines(new Vector3[] { from, curvature, to }, new int[] { 0, 1, 1, 2 }, 5);

        // Dibujar la curva bezier
        Handles.color = Color.yellow;
        Handles.DrawBezier(from, to, curvature, to, Color.yellow, null, 2);
        if (arrowDirection != 0)
        {
            Vector2 tip = arrowDirection > 0 ? to : from;
            Vector2 direction = arrowDirection > 0 ?
                GetDirection(from, curvature, tip) :
                GetDirection(to, curvature, tip);
            direction.Normalize();

            Vector2 right = new Vector2(direction.y, -direction.x) * .5f;
            Vector2 arrowRight = right * arrowWidth * UIScale;
            Vector2 arrowForward = direction * arrowWidth * UIScale;

            Handles.DrawLines(
                new Vector3[]
                {
                    tip + arrowRight - arrowForward, tip, tip - arrowRight - arrowForward,
                },
                new int[] { 0, 1, 1, 2 }
                );
        }
    }

    private static Vector2 GetDirection(Vector2 from, Vector2 curvature, Vector2 to)
    {
        return curvature != to ? to - curvature : to - from;
    }

    #endregion


    private void GetNotesheet(Difficulty difficulty, out NoteSheet noteSheet, out SerializedProperty property)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                property = serializedObject.FindProperty("easyNoteSheet");
                break;
            case Difficulty.Hard:
                property = serializedObject.FindProperty("hardNoteSheet");
                break;
            default:
                property = null;
                break;
        }
        noteSheet = song.GetNotesheet(difficulty);
    }

}

/* Versión Vieja, con el ajustador de BPM
// Clase que hereda de Editor, lo que nos permite cambiar como se ve el inspector
// Tiene un atributo [CustomEditor(typeof(Song))], lo que nos permite que este inspector
// se muestre cuando seleccionemos un Asset de tipo Song
public class EditorSongOld : Editor
{

    // Constante del volumen para reproducir el audio (0f-1f)
    const float playVolume = 0.1f;

    // Estilo de texto que nos permite mostrar rich text (Letras en negrita, cursiva, etc...)
    static GUIStyle richTextStyle;
    // El mensaje que el botón de ajuste muestra
    static string ButtonMessage { get { return EditingBPM ? "Editando BPM..." : "Detectar BPM manualmente."; } }

    // La canción (Clase, no audio) que se está ajustando
    static Song actualSong = null;
    // Para indicar si se está ajustando o no una canción, básicamente si actualSong no es nula
    static bool EditingBPM { get { return actualSong != null; } }

    // La cantidad de pulsos totales, el primero se usa para contar el tiempo inicial
    static int pressCount = 0;
    // Si la cantidad de pulsos es suficiente como para ajustar el valor de BPM
    static bool EnoughPresses { get { return pressCount > 1; } }

    // El tiempo del sistema en el que el primer pulso ocurre
    static DateTime startTime;
    // El BPM actual para mostrar mientras se ajusta y para aplicar cuando se termine
    static float currentBPM;

    // Instancia del audio source para reproducir la música
    static AudioSource musicPlayer;
    /// <summary>
    /// Revisar si ya existe una instancia del audio source
    /// </summary>
    static void CheckIfPlayerExists()
    {
        // No hacer nada de lo de abajo si ya hay una instancia
        if (musicPlayer) return;

        // Crear la instancia y añadir un componente AudioSource para asignarlo a la variable musicPlayer
        // Despues escondemos el objeto para que no salga en la Hierarchy y no se guarde en la escena
        musicPlayer = new GameObject("Editor Music Player").AddComponent<AudioSource>();
        musicPlayer.gameObject.hideFlags = HideFlags.HideAndDontSave;
        
        // Asignarle el volumen
        musicPlayer.volume = playVolume;
    }

    // Override del OnInspectorGUI, que empieza dibujando el inspector normal y despues el botón con
    // toda la información del ajuste
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        // Crear un espacio entre el inspector por defecto y lo que añadamos debajo
        EditorGUILayout.Space();

        // Mostrar el botón de ajuste y revisar si se cliquea
        if (GUILayout.Button(ButtonMessage) && !EditingBPM)
        {
            // Si se cliquea y no hay ninguna canción editándose, empezar con la edición
            // de la canción que se está seleccionando
            StartEditing((Song)target);
        }
        else if (EditingBPM)
        {
            // Todo esto sólo pasa cuando se edita una canción

            // Llamar el método que recibe el input
            OnEditingBPM();

            // Mostrar la información de las teclas que se pueden presionar
            EditorGUILayout.LabelField("Pulsa <b>Espacio</b> al compás de la música.", richTextStyle);
            EditorGUILayout.LabelField("<b>Enter</b> para terminar y asignar el BPM.", richTextStyle);
            EditorGUILayout.LabelField("<b>Esc</b> para cancelar.", richTextStyle);

            EditorGUILayout.Space();

            // Mostrar la información que tenemos hasta ahora
            EditorGUILayout.LabelField("Pulsos totales: " + (pressCount));
            EditorGUILayout.LabelField("BPM actual: " + GetCurrentBPMText());
        }
    }

    private void OnEnable()
    {
        // Cuando seleccionamos una instancia de Song crear y asignar la GUIStyle de richTextStyle
        // Esto se hace en este método porque Unity lo deja crear en este método
        richTextStyle = new GUIStyle()
        {
            richText = true,
        };
    }
    private void OnDisable()
    {
        // Si se deselecciona la canción, cancelar el ajuste
        CancelEditing();
    }

    void OnEditingBPM()
    {
        // Conseguir el evento actual
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            // Si se presiona la tecla (KeyDown), realizar la acción correspondiente
            switch (e.keyCode)
            {
                case KeyCode.Space:
                    // En espacio llamar el método note press para hacer saber que presionamos una nota
                    OnEditNotePress();
                    break;
                case KeyCode.Escape:
                case KeyCode.Backspace:
                    // Cancelar si se presiona Esc o Backspace (Arriba del Enter)
                    CancelEditing();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    // Aplicar el BPM actual cuando se presione Enter o Intro
                    EndEditing();
                    break;
            }
        }
    }
    void OnEditNotePress()
    {
        // Aumentar la cantidad de pulsos
        pressCount++;
        if (pressCount <= 1)
        {
            // Si era el primer pulso, sólo asignar el tiempo inicial
            startTime = DateTime.Now;
        }
        else
        {
            // Cuando ya tengamos más pulsos, calcular el BPM actual
            currentBPM = (float)((pressCount - 1) / DateTime.Now.Subtract(startTime).TotalMinutes);
        }

        // Decirle a Unity que vuelva a dibujar este inspector
        Repaint();
    }
    /// <summary>
    /// Devuelve un string con la cantidad de pulsos actuales.
    /// Si no son suficientes, muestra un mensaje de que faltan pulsos.
    /// </summary>
    /// <returns>Cantidad de pulsos</returns>
    static string GetCurrentBPMText ()
    {
        if (!EnoughPresses)
        {
            return "(Se requieren más pulsos)";
        }
        else return currentBPM.ToString("0.0");
    }


    void StartEditing (Song editSong)
    {
        // Primero asegurarse de que haya una instancia del reproductor
        CheckIfPlayerExists();

        // Asignar la canción actual y la cantidad de pulsos a 0
        actualSong = editSong;
        pressCount = 0;

        // Asignar el clip actual del reproductor al de la canción que queremos editar y la reproducimos
        if (editSong.audio != null)
        {
            musicPlayer.clip = editSong.audio;
            musicPlayer.Play();
        }
        else Debug.LogWarningFormat("No se ha asignado ningun audio a la canción {0}.", target);
    }
    void EndEditing()
    {
        // Revisar si estabamos editando una canción o si son suficientes pulsos
        // y asignamos el BPM actual
        if (EditingBPM && EnoughPresses)
        {
            actualSong.bpm = currentBPM;
        }
        
        // Llamamos CancelEditing para parar la canción y terminar de editarla
        CancelEditing();
    }
    void CancelEditing()
    {
        // Parar con el audio actual y asignar actualSong a null
        if (musicPlayer) musicPlayer.Stop();
        actualSong = null;

        // Redibujar el inspector
        Repaint();
    }

}
*/