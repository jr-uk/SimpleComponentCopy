using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

/// <summary>
/// A component copier - should filter out unwanted components and handle most errors
/// 
/// The code needs a refactor for readability
/// </summary>
public class SimpleComponentCopy : EditorWindow
{
    private GameObject sourceGameObject;
    private Dictionary<Component, bool> componentToCopyDictionary = new Dictionary<Component, bool>();
    private List<Component> sourceComponents = new List<Component>();
    private Type[] nonCopyableTypes = { typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer) };

    [MenuItem("Tools/SimpleComponentCopy")]
    public static void ShowWindow()
    {
        GetWindow<SimpleComponentCopy>("SimpleComponentCopy");
    }

    void OnGUI()
    {
        DrawSourceGameObjectButton();
        DrawSourceGameObjectComponents(); // Ensure this is called here to draw toggles
        DrawCopyToSelectedGameObjectsButton();
        
    }

    private void DrawSourceGameObjectComponents()
    {
        if (componentToCopyDictionary != null && componentToCopyDictionary.Count > 0)
        {
            foreach (var componentEntry in componentToCopyDictionary.ToList()) // Use ToList to allow modification
            {
                // Draw a toggle for each component and update its value in the dictionary
                bool newValue = EditorGUILayout.Toggle(componentEntry.Key.GetType().Name, componentEntry.Value);
                componentToCopyDictionary[componentEntry.Key] = newValue;
            }
        }
    }

    private void DrawCopyToSelectedGameObjectsButton()
    {
        GUI.enabled = componentToCopyDictionary.Any(kvp => kvp.Value);

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
            DrawSourceGameObjectComponents();
        }
    }

    /// <summary>
    /// Set the source object and get the components 
    /// </summary>
    private void SetSourceGameObject()
    {
        var selectedObjects = SimpleComponentCopyUtils.GetSelectedObjects();
        if (selectedObjects.Count != 1)
        {
            SendMessageToUser("Select only one source object");
            return;
        }

        sourceGameObject = selectedObjects.FirstOrDefault(); // Get first object
        sourceComponents = SimpleComponentCopyUtils.GetObjectComponents(sourceGameObject); // Get Components of object
        componentToCopyDictionary.Clear(); // Clear the dictionary

        foreach (var component in sourceComponents)
        {
            if (component != null && !nonCopyableTypes.Contains(component.GetType()))
            {
                componentToCopyDictionary[component] = false; // Initialize to false
            }
        }

        SendMessageToUser("Source Set: " + sourceGameObject.name);
    }

    private void CopyComponentsToSelectedGameObjects()
    {
        foreach (var item in componentToCopyDictionary.Where(kvp => kvp.Value))
        {
            foreach (GameObject obj in SimpleComponentCopyUtils.GetSelectedObjects())
            {
                SimpleComponentCopyUtils.Copy(item.Key ,obj);
            }
        }
    }

    void SendMessageToUser(string message)
    {
        // For simplicity, could use Debug.Log for now
        Debug.Log(message);
    }
}

public static class SimpleComponentCopyUtils {
    // private static Type[] nonCopyableTypes = { typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer) };
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
                catch (Exception e)
                {
                    Debug.Log("Property copy error");
                    Debug.LogException(e); 
                }
            }
        }
    }

    public static List<GameObject> GetSelectedObjects()
    {
        List<GameObject> selectedGameObjects = Selection.gameObjects.ToList(); // Ensures list is not null
        return selectedGameObjects;
    }

    public static List<Component> GetObjectComponents (GameObject obj)
    {
        List<Component> components = new List<Component>();

        components.AddRange(obj.GetComponents<Component>());
        
        return components;
    }
}