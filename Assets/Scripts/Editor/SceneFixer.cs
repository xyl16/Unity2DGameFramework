using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public class SceneFixer : EditorWindow
{
    [MenuItem("Tools/Create Manager Hierarchy")]
    public static void CreateManagerHierarchy()
    {
        GameObject managersObject = GameObject.Find("Managers");

        if (managersObject == null)
        {
            managersObject = new GameObject("Managers");
            Debug.Log("Created Managers GameObject");
        }
        else
        {
            Debug.Log("Managers GameObject already exists");
        }

        AddComponentIfNotExists(managersObject, "EventManager");
        AddComponentIfNotExists(managersObject, "ResourceManager");
        AddComponentIfNotExists(managersObject, "UIManager");
        AddComponentIfNotExists(managersObject, "SceneManager");
        AddComponentIfNotExists(managersObject, "AudioManager");
        AddComponentIfNotExists(managersObject, "DataManager");
        AddComponentIfNotExists(managersObject, "ConfigManager");
        AddComponentIfNotExists(managersObject, "Logger");
        AddComponentIfNotExists(managersObject, "PrefabPool");
        AddComponentIfNotExists(managersObject, "TimerManager");
        AddComponentIfNotExists(managersObject, "CoroutineManager");
        AddComponentIfNotExists(managersObject, "GameInitializer");

        CreateGameManager();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Manager hierarchy created/updated successfully");

        // 打印当前 Managers 节点的所有组件
        PrintManagersStatus();
    }

    [MenuItem("Tools/Show Managers Components")]
    public static void ShowManagersComponents()
    {
        PrintManagersStatus();
    }

    private static void PrintManagersStatus()
    {
        GameObject managersObject = GameObject.Find("Managers");
        if (managersObject == null)
        {
            Debug.LogError("Managers GameObject not found!");
            return;
        }

        Component[] components = managersObject.GetComponents<Component>();
        Debug.Log($"=== Managers GameObject has {components.Length} components ===");
        foreach (var comp in components)
        {
            if (comp != null && comp.GetType() != typeof(Transform))
            {
                Debug.Log($"- {comp.GetType().Name}");
            }
        }
    }

    private static void CreateGameManager()
    {
        GameObject gameManager = GameObject.Find("GameManager");

        if (gameManager == null)
        {
            gameManager = new GameObject("GameManager");
            AddComponentIfNotExists(gameManager, "GameManager");
            AddComponentIfNotExists(gameManager, "VersionManager");
            Debug.Log("Created GameManager");
        }
        else
        {
            Debug.Log("GameManager already exists");
        }
    }

    private static void AddComponentIfNotExists(GameObject obj, string componentName)
    {
        System.Type componentType = FindType(componentName);

        if (componentType == null)
        {
            Debug.LogWarning($"Component type {componentName} not found!");
            return;
        }

        Component existingComponent = obj.GetComponent(componentType);
        if (existingComponent == null)
        {
            obj.AddComponent(componentType);
            Debug.Log($"Added {componentName} to {obj.name}");
        }
    }

    private static System.Type FindType(string typeName)
    {
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName && typeof(MonoBehaviour).IsAssignableFrom(t));
            if (type != null)
                return type;
        }
        return null;
    }
}
