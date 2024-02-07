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
            // Check if the property can be written to and is not a known problematic property
            if (prop.CanWrite && prop.GetSetMethod(true) != null && IsNotProblematicProperty(prop))
            {
                try
                {
                    // Copy the value from the original to the copy
                    prop.SetValue(copy, prop.GetValue(source, null), null);
                }
                catch (Exception e)
                {
                    Debug.Log($"Property copy error: {prop.Name}, Error: {e.Message}");
                }
            }
        }
    }

    private static bool IsNotProblematicProperty(PropertyInfo prop)
    {
        // List of problematic property names
        var problematicProperties = new HashSet<string>
        {
            "name", // Managed by Unity, might not be unique if copied directly.
            "tag", // Similar to name, could lead to issues if not unique.
            "position", // Transform property that should be set through Transform methods.
            "rotation", // Transform property that should be set through Transform methods.
            "localPosition", // Managed by Unity's Transform component.
            "localRotation", // Managed by Unity's Transform component.
            "localScale", // Managed by Unity's Transform component.
            // Add any other properties you find problematic during copying.
        };

        // Check if the property's name is in the list of problematic properties
        if (problematicProperties.Contains(prop.Name))
        {
            return false; // Property is problematic, should not be copied.
        }

        // Example of excluding properties by declaring type to handle component-specific properties
        if (prop.DeclaringType == typeof(MeshRenderer) && prop.Name == "material")
        {
            return false; // Avoid copying the material directly as it can lead to shared material issues.
        }

        // Example of excluding properties based on custom attributes (e.g., properties marked as [NonSerialized])
        if (prop.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length > 0)
        {
            return false; // Property is marked as non-serializable and should not be copied.
        }

        // Add more conditions as needed based on the components and properties you're working with.

        return true; // If none of the conditions are met, the property is not considered problematic.
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