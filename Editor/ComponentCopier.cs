using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

public class SimpleComponentCopy : EditorWindow
{
    private GameObject sourceGameObject;
    private Dictionary<Component, bool> toggleStatusDictionary = new Dictionary<Component, bool>();

    [MenuItem("Tools/Component Copier")]
    public static void ShowWindow()
    {
        GetWindow<SimpleComponentCopy>("Component Copier");
    }

    void OnGUI()
    {
        DrawSourceGameObjectButton();
        DrawCopyToSelectedGameObjectsButton();
    }

    private void DrawCopyToSelectedGameObjectsButton()
    {
        GUI.enabled = toggleStatusDictionary.Any(kvp => kvp.Value);

        if (GUILayout.Button("Copy to Selected GameObjects"))
        {
            CopyComponentsToSelectedGameObjects();
        }

        GUI.enabled = true; // Reset GUI state
    }

    private void DrawSourceGameObjectButton()
    {
        if (GUILayout.Button("Select Source GameObject"))
        {
            SetSourceGameObject();
        }
    }

    private List<GameObject> GetSelectedObjects()
    {
        List<GameObject> selectedGameObjects = Selection.gameObjects.ToList(); // Ensures list is not null
        return selectedGameObjects;
    }

    private Component[] GetObjectComponents (GameObject obj)
    {
        Component[] components = obj.GetComponents<Component>();
        return components;
    }

    private void SetSourceGameObject()
    {
        if (GetSelectedObjects().Count != 1)
        {
            SendMessageToUser("Select only one source object");
            return;
        }

        sourceGameObject = GetSelectedObjects().FirstOrDefault(); 

        Component[] sourceComponents = GetObjectComponents(sourceGameObject);
        
        toggleStatusDictionary.Clear(); // Clear previous selections
        foreach (var component in sourceComponents)
        {
            // Initialize dictionary entries without a loop
            toggleStatusDictionary[component] = false; // Default to false, assume GUI layout in another method
        }
        SendMessageToUser("Source Set: " + sourceGameObject.name);
    }

    private void CopyComponentsToSelectedGameObjects()
    {
        // Assuming targetGameObjects is initialized and populated elsewhere
        foreach (var item in toggleStatusDictionary.Where(kvp => kvp.Value))
        {
            // Assuming existence of targetGameObjects list, add error checking as needed
            foreach (GameObject obj in GetSelectedObjects())
            {
                SimpleComponentCopyUtils.Copy(item.Key ,obj);
            }
        }
    }

    void SendMessageToUser(string message)
    {
        // Implementation depends on desired method of user feedback
        // For simplicity, could use Debug.Log for now
        Debug.Log(message);
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