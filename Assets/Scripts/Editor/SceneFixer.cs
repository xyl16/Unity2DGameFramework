using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public class SceneFixer : EditorWindow
{
    [MenuItem("Tools/修复缺失脚本 (Fix Missing Scripts)")]
    public static void FixMissingScripts()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
        int fixedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            SerializedObject so = new SerializedObject(obj);
            SerializedProperty prop = so.FindProperty("m_Component");

            if (prop != null)
            {
                for (int i = prop.arraySize - 1; i >= 0; i--)
                {
                    SerializedProperty componentProp = prop.GetArrayElementAtIndex(i);
                    if (componentProp.objectReferenceValue == null)
                    {
                        prop.DeleteArrayElementAtIndex(i);
                        fixedCount++;
                        Debug.Log($"Removed missing script from {obj.name}");
                    }
                }
                so.ApplyModifiedProperties();
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("修复完成", $"已删除 {fixedCount} 个缺失的脚本组件。", "OK");
        Debug.Log($"Fixed {fixedCount} missing script components.");
    }

    [MenuItem("Tools/场景修复工具/重建所有游戏对象")]
    public static void RebuildAllGameObjects()
    {
        if (!EditorUtility.DisplayDialog("确认", "这将删除并重新创建 Managers 和 GameManager 节点。继续吗?", "Yes", "No"))
        {
            return;
        }

        // 删除所有节点
        DeleteManagers();
        DeleteGameManager();

        // 重新创建所有节点
        CreateCleanManagers();
        CreateGameManager();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("重建完成", "所有节点已重新创建。", "OK");

        Debug.Log("=== 重建完成 ===");
        ShowAllGameObjectsStatus();
    }

    [MenuItem("Tools/场景修复工具/显示所有对象状态")]
    public static void ShowAllGameObjectsStatus()
    {
        Debug.Log("=== 游戏对象状态 ===");
        PrintManagersStatus();
        PrintGameManagerStatus();
    }

    #region Delete Methods

    private static void DeleteManagers()
    {
        GameObject managersObject = GameObject.Find("Managers");
        if (managersObject != null)
        {
            DestroyImmediate(managersObject);
            Debug.Log("已删除 Managers 节点");
        }
    }

    private static void DeleteGameManager()
    {
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager != null)
        {
            DestroyImmediate(gameManager);
            Debug.Log("已删除 GameManager 节点");
        }
    }

    #endregion

    #region Create Methods

    private static void CreateCleanManagers()
    {
        GameObject managersObject = new GameObject("Managers");

        // 添加所有管理器组件 (Logger 是普通类，不是 MonoBehaviour 组件)
        managersObject.AddComponent<EventManager>();
        managersObject.AddComponent<ResourceManager>();
        managersObject.AddComponent<UIManager>();
        managersObject.AddComponent<SceneManager>();
        managersObject.AddComponent<AudioManager>();
        managersObject.AddComponent<DataManager>();
        managersObject.AddComponent<ConfigManager>();
        managersObject.AddComponent<PrefabPool>();
        managersObject.AddComponent<TimerManager>();
        managersObject.AddComponent<CoroutineManager>();
        managersObject.AddComponent<NetworkManager>();

        Debug.Log("已创建 Managers 节点及其所有组件");
    }

    private static void CreateGameManager()
    {
        GameObject gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();
        gameManager.AddComponent<VersionManager>();

        Debug.Log("已创建 GameManager 节点及其组件");
    }

    #endregion

    #region Print Status Methods

    private static void PrintManagersStatus()
    {
        GameObject managersObject = GameObject.Find("Managers");
        if (managersObject == null)
        {
            Debug.Log("Managers: 不存在");
            return;
        }

        Component[] components = managersObject.GetComponents<Component>();
        Debug.Log($"Managers: {components.Length} 个组件");
        foreach (var comp in components)
        {
            if (comp != null && comp.GetType() != typeof(Transform))
            {
                Debug.Log($"  - {comp.GetType().Name}");
            }
        }
    }

    private static void PrintGameManagerStatus()
    {
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager == null)
        {
            Debug.Log("GameManager: 不存在");
            return;
        }

        Component[] components = gameManager.GetComponents<Component>();
        Debug.Log($"GameManager: {components.Length} 个组件");
        foreach (var comp in components)
        {
            if (comp != null && comp.GetType() != typeof(Transform))
            {
                Debug.Log($"  - {comp.GetType().Name}");
            }
        }
    }

    #endregion

    #region Legacy Methods

    [MenuItem("Tools/场景修复工具/清理Manager重复组件")]
    public static void CleanDuplicateComponents()
    {
        GameObject managersObject = GameObject.Find("Managers");
        if (managersObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Managers GameObject not found!", "OK");
            return;
        }

        int removedCount = 0;
        Component[] components = managersObject.GetComponents<Component>();

        System.Collections.Generic.Dictionary<System.Type, int> componentCount = new System.Collections.Generic.Dictionary<System.Type, int>();

        foreach (Component comp in components)
        {
            if (comp != null && comp.GetType() != typeof(Transform))
            {
                System.Type type = comp.GetType();
                if (!componentCount.ContainsKey(type))
                {
                    componentCount[type] = 0;
                }
                componentCount[type]++;
            }
        }

        foreach (var kvp in componentCount)
        {
            if (kvp.Value > 1)
            {
                Component[] duplicates = managersObject.GetComponents(kvp.Key);
                for (int i = 1; i < duplicates.Length; i++)
                {
                    Component duplicate = duplicates[i];
                    if (duplicate != null)
                    {
                        DestroyImmediate(duplicate);
                        removedCount++;
                        Debug.Log($"Removed duplicate {kvp.Key.Name} from Managers");
                    }
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Clean Complete", $"Removed {removedCount} duplicate component(s).", "OK");
        Debug.Log($"Cleaned {removedCount} duplicate components from Managers.");

        PrintManagersStatus();
    }

    [MenuItem("Tools/场景修复工具/重置Manager GameObject")]
    public static void ResetManagersGameObject()
    {
        if (!EditorUtility.DisplayDialog("Confirm", "This will destroy current Managers GameObject and create a new one. Continue?", "Yes", "No"))
        {
            return;
        }

        GameObject managersObject = GameObject.Find("Managers");
        if (managersObject != null)
        {
            DestroyImmediate(managersObject);
            Debug.Log("Destroyed old Managers GameObject");
        }

        CreateCleanManagers();
        EditorUtility.DisplayDialog("Reset Complete", "Managers GameObject has been reset.", "OK");
    }

    [MenuItem("Tools/场景修复工具/创建Manager层级")]
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
        AddComponentIfNotExists(managersObject, "PrefabPool");
        AddComponentIfNotExists(managersObject, "TimerManager");
        AddComponentIfNotExists(managersObject, "CoroutineManager");
        AddComponentIfNotExists(managersObject, "NetworkManager");

        CreateGameManagerLegacy();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Manager hierarchy created/updated successfully");

        PrintManagersStatus();
    }

    [MenuItem("Tools/场景修复工具/显示Manager组件")]
    public static void ShowManagersComponents()
    {
        PrintManagersStatus();
    }

    private static void CreateGameManagerLegacy()
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

    #endregion
}
