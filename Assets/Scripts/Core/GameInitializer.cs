using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    [SerializeField]
    private bool showLogger = true;

    [SerializeField]
    private LogLevel defaultLogLevel = LogLevel.Debug;

    private void Awake()
    {
    }

    private void Start()
    {
        InitializeCoreSystems();
        RegisterEvents();
    }

    private void InitializeCoreSystems()
    {
        Logger.Instance.SetLogLevel(defaultLogLevel);
        Logger.Instance.LogInfo("Initializing Game Core Systems", "GameInitializer");

        // 检查所有 Manager 是否已挂载，只引用不创建
        ValidateManager(nameof(EventManager), EventManager.Instance);
        ValidateManager(nameof(ResourceManager), ResourceManager.Instance);
        ValidateManager(nameof(UIManager), UIManager.Instance);
        ValidateManager(nameof(SceneManager), SceneManager.Instance);
        ValidateManager(nameof(AudioManager), AudioManager.Instance);
        ValidateManager(nameof(DataManager), DataManager.Instance);
        ValidateManager(nameof(ConfigManager), ConfigManager.Instance);

        Logger.Instance.LogInfo("Core Systems initialized", "GameInitializer");
    }

    private void ValidateManager(string managerName, MonoBehaviour manager)
    {
        if (manager == null)
        {
            Logger.Instance.LogError($"{managerName} not found in scene! Please attach it to the Managers GameObject in the editor.", "GameInitializer");
        }
        else
        {
            Logger.Instance.LogInfo($"{managerName} initialized successfully", "GameInitializer");
        }
    }

    private void RegisterEvents()
    {
        EventManager.Instance.AddListener("SceneLoaded", OnSceneLoaded);
        EventManager.Instance.AddListener("SceneUnloaded", OnSceneUnloaded);

        // Don't automatically load Login scene - it should be managed by GameManager
        // SceneManager.Instance.LoadScene("Login", OnLoginSceneLoaded);
    }

    private void OnSceneLoaded(object data)
    {
        string sceneName = data as string;
        Logger.Instance.LogInfo($"Scene loaded: {sceneName}", "Scene");
    }

    private void OnSceneUnloaded(object data)
    {
        string sceneName = data as string;
        Logger.Instance.LogInfo($"Scene unloaded: {sceneName}", "Scene");
    }

    private void OnLoginSceneLoaded()
    {
        Logger.Instance.LogInfo("Login scene loaded, initializing login system", "GameInitializer");
        EventManager.Instance.InvokeEvent("LoginSceneReady");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ShowLogger();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            Logger.Instance.ExportLogs();
        }
    }

    private void ShowLogger()
    {
        showLogger = !showLogger;
        Logger.Instance.LogInfo($"Logger visibility: {showLogger}", "GameInitializer");
    }

    private void OnDestroy()
    {
        EventManager.Instance.RemoveListener("SceneLoaded", OnSceneLoaded);
        EventManager.Instance.RemoveListener("SceneUnloaded", OnSceneUnloaded);
    }
}
