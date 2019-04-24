using UnityEngine.Rendering.PostProcessing;
using UnityEditor;

using Catneep.Neeps.Modules;

[CustomEditor(typeof(PostProcessProfileModule))]
public class PostProcessModuleEditor : Editor
{

    Editor profileEditor;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.UpdateIfRequiredOrScript();
        SerializedProperty profileProp = serializedObject.FindProperty("profile");
        if (profileProp.objectReferenceValue == null)
        {
            PostProcessProfile newProfile = CreateInstance<PostProcessProfile>();
            newProfile.hideFlags = UnityEngine.HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(newProfile, target);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(newProfile));
            

            profileProp.objectReferenceValue = newProfile;
            serializedObject.ApplyModifiedProperties();

            //UnityEngine.Debug.Log(newProfile + " " + profileProp.objectReferenceValue);
        }

        CreateCachedEditor(profileProp.objectReferenceValue, null, ref profileEditor);
        if (profileProp.objectReferenceValue != null && profileEditor != null) profileEditor.OnInspectorGUI();
    }

}