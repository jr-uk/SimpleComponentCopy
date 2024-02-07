using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

public class SimpleComponentCopy : EditorWindow
{
    private GameObject sourceGameObject;
    private Dictionary<Component, bool> componentToCopyDictionary = new Dictionary<Component, bool>();
    private List<Component> sourceComponents = new List<Component>();

    [MenuItem("Tools/SimpleComponentCopy")]
    public static void ShowWindow()
    {
        GetWindow<SimpleComponentCopy>("SimpleComponentCopy");
    }

    void OnGUI()
    {
        DrawSourceGameObjectButton();
        DrawCopyToSelectedGameObjectsButton();
        
    }

    private void DrawSourceGameObjectComponents()
    {
        if(componentToCopyDictionary.Count > 0) {componentToCopyDictionary.Clear();} // Clear previous selections

        if(componentToCopyDictionary.Count == 0 || componentToCopyDictionary == null)
        {
            foreach (var component in sourceComponents)
            {
                bool toggleValue = EditorGUILayout.Toggle(component.name, componentToCopyDictionary[component]);
                componentToCopyDictionary.Add(component, toggleValue); 
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

    private List<GameObject> GetSelectedObjects()
    {
        List<GameObject> selectedGameObjects = Selection.gameObjects.ToList(); // Ensures list is not null
        return selectedGameObjects;
    }

    private List<Component> GetObjectComponents (GameObject obj)
    {
        List<Component> components = new List<Component>();

        components.AddRange(obj.GetComponents<Component>());
        
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

        sourceComponents = GetObjectComponents(sourceGameObject);
        if(sourceComponents == null) 
        {
            Debug.Log("No source components");
            return;
        }
        
        SendMessageToUser("Source Set: " + sourceGameObject.name);
    }

    private void CopyComponentsToSelectedGameObjects()
    {
        foreach (var item in componentToCopyDictionary.Where(kvp => kvp.Value))
        {
            foreach (GameObject obj in GetSelectedObjects())
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
}