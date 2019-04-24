/*
using UnityEditor;
using UnityEngine;


//[CustomPropertyDrawer(typeof(StepsToBeatsLabelAttribute))]
public sealed class StepsToBeatsDrawer : PropertyDrawer
{

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        float beats = property.intValue * Subdivision.subtepSize;

        EditorGUI.LabelField(position, label.text, beats + " beats");
    }

}


public enum NoteInput { Green = 0, Red = 1, Yellow = 2 }

//[CustomPropertyDrawer(typeof(IntToNoteInputAttribute))]
public sealed class IntToNoteSelector : PropertyDrawer
{

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        property.intValue = (int)(NoteInput)EditorGUI.EnumPopup(position, label, (NoteInput)property.intValue);
    }

}
*/

// Clase que ya no se usa, y probablemente ya no sirva de nada
/*
[CustomPropertyDrawer(typeof(Note))]
public class EditorNoteTime : PropertyDrawer
{

    const float controlHeight = 64;

    bool showing = false;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!showing) return 16;

        return base.GetPropertyHeight(property, label) + controlHeight;
    }

    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        showing = EditorGUI.Foldout(SetRectForProperty(new Rect(rect.x, rect.y, rect.width, 16)),
            showing, label, true);

        if (!showing) return;

        EditorGUI.indentLevel++;
        EditorGUI.BeginChangeCheck();

        // Dibujar la propiedad note tal como se dibujaría normalmente
        EditorGUI.PropertyField(GetRectForProperty(), property.FindPropertyRelative("note"),
            new GUIContent("Note Input"));

        // Dibujar la propiedad del tiempo relativo desde la última nota
        EditorGUI.PropertyField(GetRectForProperty(), property.FindPropertyRelative("relativeTime"));

        // Mostrar el tiempo relativo desde que empieza la canción
        EditorGUI.LabelField(GetRectForProperty(), "Start Time", 
            property.FindPropertyRelative("startTime").floatValue.ToString());

        EditorGUI.EndChangeCheck();
        EditorGUI.indentLevel--;
    }


    Rect currentRect;
    Rect SetRectForProperty(Rect rect)
    {
        return currentRect = rect;
    }
    Rect GetRectForProperty()
    {
        return GetRectForProperty(20);
    }
    Rect GetRectForProperty(float yChange)
    {
        currentRect.y += yChange;
        return currentRect;
    }

}
*/
