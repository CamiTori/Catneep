using UnityEditor;

using Catneep.Utils;
using UnityEngine;

[CustomPropertyDrawer(typeof(Wave))]
public class PropertyDrawers : PropertyDrawer
{

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorUtils.defaultHeight * 2f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Rect drawRect = new Rect(position);
        EditorGUI.LabelField(drawRect, "Wave");

        EditorGUI.indentLevel++;

        drawRect.y += EditorUtils.defaultHeight;
        drawRect.height -= EditorUtils.defaultHeight;
        drawRect.width = position.width * 0.3f;
        float half = position.width * 0.5f;
        float quarter = half * 0.5f;
        float diff = drawRect.width - quarter;


        // Intensity
        EditorGUI.LabelField(drawRect, "Intensity");

        drawRect.x += quarter - diff;
        SerializedProperty subProperty = property.FindPropertyRelative("intensity");
        subProperty.floatValue = EditorGUI.DelayedFloatField(drawRect, subProperty.floatValue);


        // Frequency
        drawRect.x = position.x + half;
        EditorGUI.LabelField(drawRect, "Hz");

        drawRect.x += quarter - diff;
        subProperty = property.FindPropertyRelative("adjustedFreq");
        subProperty.floatValue = 
            EditorGUI.DelayedFloatField(drawRect, subProperty.floatValue * Wave.PI2Inverted) * Wave.PI2;

        EditorGUI.indentLevel--;
    }

}
