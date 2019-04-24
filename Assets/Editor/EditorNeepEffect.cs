using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

using Catneep.Neeps;
using Catneep.Neeps.Modules;
using System.Reflection;

[CustomEditor(typeof(NeepEffect))]
public class EditorNeepEffect : Editor
{

    private NeepEffect effect;

    private GenericMenu modulesMenu;
    private Editor[] moduleEditors;

    private SerializedProperty modulesProp = null;

    private void OnEnable()
    {
        IEnumerable<Type> moduleTypes = Assembly.GetAssembly(typeof(EffectModule))
            .GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(EffectModule)));
        modulesMenu = new GenericMenu();
        foreach (Type item in moduleTypes)
        {
            CustomEffectModuleAttribute attribute = 
                (CustomEffectModuleAttribute)Attribute.GetCustomAttribute(item, typeof(CustomEffectModuleAttribute));
            if (attribute == null || string.IsNullOrEmpty(attribute.MenuName)) continue;
            /* For optional attribute
            string name = attribute != null && !string.IsNullOrEmpty(attribute.MenuName) ?
                attribute.MenuName : ObjectNames.NicifyVariableName(item.Name);
            */

            //Debug.Log("Added to menu: " + item);
            modulesMenu.AddItem(new GUIContent(attribute.MenuName), 
                false, 
                () => AddModule(item));
        }

        effect = (NeepEffect)target;
        //effect.OnAddModulePress += OpenModulesMenu;
    }

    private void OnDisable()
    {
        //effect.OnAddModulePress -= OpenModulesMenu;
    }

    private void OpenModulesMenu()
    {
        modulesMenu.ShowAsContext();
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.indentLevel++;
        base.OnInspectorGUI();
        EditorGUI.indentLevel--;

        serializedObject.UpdateIfRequiredOrScript();

        EditorGUI.indentLevel++;
        EditorGUILayout.Separator();
        EditorUtils.Header("Effect Modules");
        EditorGUI.indentLevel--;

        //EditorGUILayout.PropertyField(serializedObject.FindProperty("effectModules"), true);

        modulesProp = serializedObject.FindProperty("effectModules");

        // Make sure the editor array is the same size than the module list
        if (moduleEditors == null || moduleEditors.Length != modulesProp.arraySize)
        {
            Array.Resize(ref moduleEditors, modulesProp.arraySize);
        }

        // Get the module list and draw the editor for each one
        int i = 0;
        while (i < modulesProp.arraySize)
        {
            SerializedProperty moduleProp = modulesProp.GetArrayElementAtIndex(i);
            EffectModule moduleObj = (EffectModule)moduleProp.objectReferenceValue;

            // If the module is null, add the index so we remove the module later
            if (moduleObj == null)
            {
                modulesProp.DeleteArrayElementAtIndex(i);
                continue;
            }

            CreateCachedEditor(moduleObj, null, ref moduleEditors[i]);

            EditorGUILayout.Separator();
            moduleProp.isExpanded = EditorGUILayout.InspectorTitlebar(moduleProp.isExpanded, moduleObj, true);
            if (moduleProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                moduleEditors[i].OnInspectorGUI();
                EditorGUI.indentLevel--;
            }

            i++;
        }

        // Draw buttons
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        EditorGUI.indentLevel++;

        if (GUILayout.Button(NeepEffect.addModuleText))
        {
            OpenModulesMenu();
        }

        /*
        if (GUILayout.Button("Clear Modules"))
        {
            effect.ClearModules();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(effect));
        }
        */

        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();
    }

    private void AddModule(Type moduleType)
    {
        if (!moduleType.IsSubclassOf(typeof(EffectModule))) return;

        EffectModule newModule = (EffectModule)CreateInstance(moduleType);
        if (newModule != null)
        {
            newModule.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(newModule, effect);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(newModule));

            int newIndex = modulesProp.arraySize;
            modulesProp.InsertArrayElementAtIndex(newIndex);
            modulesProp.GetArrayElementAtIndex(newIndex).objectReferenceValue = newModule;
            //Debug.LogFormat("Added module ({0}), new count: {1}", newModule, effectModules.Count);

            serializedObject.ApplyModifiedProperties();
        }
    }

    
    public override bool UseDefaultMargins()
    {
        return false;
    }

}
