using UnityEngine;
using UnityEditor;


public static class EditorUtils
{

    public const float defaultHeight = 16f;

    private static GUIStyle richTextStyle = null;
    public static GUIStyle GetRichTextStyle
    {
        get
        {
            if (richTextStyle == null)
            {
                richTextStyle = new GUIStyle()
                {
                    richText = true,
                };
            }
            return richTextStyle;
        }
    }

    public static void Header(string text)
    {
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }

    public static void DrawArray(SerializedProperty serializedArray, bool drawSize = true)
    {
        SerializedProperty arraySizeProp = serializedArray.FindPropertyRelative("Array.size");
        if (drawSize) EditorGUILayout.PropertyField(arraySizeProp);
        int arraySize = arraySizeProp.intValue;

        EditorGUI.indentLevel++;
        for (int i = 0; i < arraySize; i++)
        {
            EditorGUILayout.PropertyField(serializedArray.GetArrayElementAtIndex(i));
        }
        EditorGUI.indentLevel--;
    }

}
