using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

public class SimpleComponentCopy : EditorWindow
{
    private GameObject sourcePrefab;
    private List<Component> componentsToCopy;
    private List<GameObject> targetGameObjects;
    private Dictionary<Component, bool> toggleStatusDictionary;


    [MenuItem("Tools/Component Copier")]
    public static void ShowWindow()
    {
        GetWindow<ComponentCopier>("Component Copier");
    }

    void OnGUI()
    {



        if (GUILayout.Button("Select Source GameObject"))
        {
            try
            {
                if (Selection.gameObjects.Length == 1) // FIXME: This might need to be done via method ? GetLength
                {
                    sourcePrefab = Selection.gameObjects[0]; // FIXME: needs type safety
                    // TODO: Display what is selected

                    Component[] sourceComponents = sourcePrefab.GetComponents<Component>();

                    foreach (var component in sourceComponents)
                    {
                        // Create a label and button to select the relevant component
                        toggleStatusDictionary.Add(component, EditorGUILayout.Toggle(component.GetType().Name, false));
                    }

                }
                else
                {
                    SendMessageToUser("Select only one source object");
                }



            }
            catch (System.Exception)
            {
                throw;
            }
        }

        if (GUILayout.Button("Select Target GameObjects"))
        {
            SelectTargetObjects();
        }


        if (componentsToCopy != null)
        {
            foreach (var component in componentsToCopy)
            {
                // TODO: Display the components

            }
        }

        if (toggleStatusDictionary != null && toggleStatusDictionary.Count >= 1)
        {
            GUI.enabled = true;
        }
        else 
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Copy to Selected GameObjects"))
        {
            
            foreach (KeyValuePair<Component, bool> item in toggleStatusDictionary)
            {
                if(item.Value)
                {
                    foreach (GameObject obj in targetGameObjects)
                    {
                        CopyComponentUtils.Copy(item.Key ,obj);
                    }
                }
                //sourcePrefab
                //     GameObject instantiatedPrefab = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
                //     if (instantiatedPrefab != null)
                //     {
                //         Component copy = instantiatedPrefab.AddComponent(componentToCopy.GetType());
                //         EditorUtility.CopySerialized(componentToCopy, copy);

                //         PrefabUtility.SaveAsPrefabAsset(instantiatedPrefab, AssetDatabase.GetAssetPath(sourcePrefab));

                //         Destroy(instantiatedPrefab);
                //         Destroy(copy);
                //     }
            }
        }
    }

    private void SelectTargetObjects()
    {
        try
        {
            if (targetGameObjects == null)
            {
                targetGameObjects.AddRange(Selection.gameObjects); // FIXME: needs type safety
            }
            else
            {
                targetGameObjects.Clear();
                targetGameObjects.AddRange(Selection.gameObjects); // FIXME: needs type safety
            }
            
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    void SendMessageToUser(string message)
    {
        throw new NotImplementedException();
    }
}

public static class SimpleComponentCopyUtils {
    public static void Copy<T>(T source, GameObject target) where T : Component
    {
        T copy = target.AddComponent<T>();

        Type type = source.GetType();
        
        // Copy the fields
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            field.SetValue(copy, field.GetValue(source));
        }

            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Check if the property can be written to
            if (prop.CanWrite && prop.GetSetMethod(true) != null)
            {
                try
                {
                    // Copy the value from the original to the copy
                    prop.SetValue(copy, prop.GetValue(source, null), null);
                }
                catch
                {
                    // Handle cases where some properties might not be directly copied (e.g., properties with no setter or properties that depend on runtime state)
                }
            }
        }
    }
}