using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum GameState
    {
        Initializing,
        MainMenu,
        Login,
        Playing,
        Paused,
        GameOver
    }

    private GameState currentState = GameState.Initializing;
    private NetworkManager networkManager;
    private VersionManager versionManager;
    private LoginController loginController;

    public GameState CurrentState
    {
        get { return currentState; }
        private set
        {
            if (currentState != value)
            {
                GameState previousState = currentState;
                currentState = value;
                EventManager.Instance.InvokeEvent("GameStateChanged", new GameStateChangedArgs
                {
                    previousState = previousState,
                    currentState = value
                });
                Logger.Instance.LogInfo($"Game state changed from {previousState} to {value}", "GameManager");
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        InitializeManagers();
        InitializeExtensionModules();
    }

    private void InitializeManagers()
    {
        Logger.Instance.LogInfo("Initializing GameManager", "GameManager");

        if (networkManager == null)
        {
            networkManager = gameObject.AddComponent<NetworkManager>();
        }

        if (versionManager == null)
        {
            versionManager = gameObject.AddComponent<VersionManager>();
        }

        loginController = new LoginController();

        Logger.Instance.LogInfo("GameManager initialized", "GameManager");
    }

    private void InitializeExtensionModules()
    {
        Logger.Instance.LogInfo("Initializing Extension Modules", "GameManager");

        // 性能监控
        var performanceMonitor = PerformanceMonitor.Instance;
        Logger.Instance.LogInfo("PerformanceMonitor initialized");

        // 数据分析
        var analyticsManager = AnalyticsManager.Instance;
        Logger.Instance.LogInfo("AnalyticsManager initialized");

        // 多语言
        var languageManager = LanguageManager.Instance;
        Logger.Instance.LogInfo($"LanguageManager initialized: {languageManager.GetLanguageName(languageManager.GetCurrentLanguage())}");

        // 存档系统
        var saveManager = SaveManager.Instance;
        Logger.Instance.LogInfo($"SaveManager initialized: {saveManager.GetUsedSlotCount()} save slots used");
    }

    private void Start()
    {
        CurrentState = GameState.Initializing;
        RegisterEvents();
        StartVersionCheck();
    }

    private void RegisterEvents()
    {
        EventManager.Instance.AddListener("SceneLoaded", OnSceneLoaded);
        EventManager.Instance.AddListener("SceneUnloaded", OnSceneUnloaded);
    }

    private void OnSceneLoaded(object data)
    {
        string sceneName = data as string;
        Logger.Instance.LogInfo($"Scene loaded: {sceneName}", "GameManager");
    }

    private void OnSceneUnloaded(object data)
    {
        string sceneName = data as string;
        Logger.Instance.LogInfo($"Scene unloaded: {sceneName}", "GameManager");
    }

    private void StartVersionCheck()
    {
        if (versionManager.enableVersionCheck)
        {
            Logger.Instance.LogInfo("Starting version check", "GameManager");
            versionManager.CheckVersion(OnVersionCheckComplete);
        }
        else
        {
            Logger.Instance.LogInfo("Version check disabled, skipping", "GameManager");
            CurrentState = GameState.MainMenu;
            ConnectToServer();
        }
    }

    private void OnVersionCheckComplete(bool success, string message)
    {
        Logger.Instance.LogInfo(message, "GameManager");

        if (success)
        {
            Logger.Instance.LogInfo("Version check passed", "GameManager");
            CurrentState = GameState.MainMenu;
        }
        else
        {
            Logger.Instance.LogWarning(message + ", using local version", "GameManager");
            CurrentState = GameState.MainMenu;
        }

        ConnectToServer();
    }

    private void ConnectToServer()
    {
        ConfigManager.AppConfig config = ConfigManager.Instance.GetConfig();
        Logger.Instance.LogInfo($"Connecting to server: {config.serverIP}:{config.serverPort}", "GameManager");

        networkManager.OnConnected += () =>
        {
            Logger.Instance.LogInfo("Connected to server", "GameManager");
            EnterLoginState();
        };

        networkManager.OnDisconnected += () =>
        {
            Logger.Instance.LogWarning("Server connection failed or lost, running in offline mode", "GameManager");
            EnterLoginState();
        };

        networkManager.OnError += (error) =>
        {
            Logger.Instance.LogWarning($"Server error: {error}, running in offline mode", "GameManager");
        };

        networkManager.Connect(config.serverIP, config.serverPort);
    }

    public void EnterLoginState()
    {
        CurrentState = GameState.Login;
        EventManager.Instance.InvokeEvent("EnterLoginState");
    }

    public void EnterPlayingState()
    {
        CurrentState = GameState.Playing;
        EventManager.Instance.InvokeEvent("EnterPlayingState");
    }

    public void EnterPausedState()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
            EventManager.Instance.InvokeEvent("EnterPausedState");
        }
    }

    public void ResumeFromPaused()
    {
        if (CurrentState == GameState.Paused)
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            EventManager.Instance.InvokeEvent("ResumeFromPaused");
        }
    }

    public void EnterGameOverState()
    {
        CurrentState = GameState.GameOver;
        Time.timeScale = 1f;
        EventManager.Instance.InvokeEvent("EnterGameOverState");
    }

    public void ReturnToMainMenu()
    {
        CurrentState = GameState.MainMenu;
        Time.timeScale = 1f;
        EventManager.Instance.InvokeEvent("ReturnToMainMenu");
    }

    public void OnLoginSuccess()
    {
        Logger.Instance.LogInfo("Login success", "GameManager");
        EventManager.Instance.InvokeEvent("LoginSuccess");
        EnterPlayingState();
        LoadGameBundle();
    }

    public void OnLoginFailed(string message)
    {
        Logger.Instance.LogError($"Login failed: {message}", "GameManager");
        EventManager.Instance.InvokeEvent("LoginFailed", message);
    }

    private void LoadGameBundle()
    {
        Logger.Instance.LogInfo("Loading game bundle", "GameManager");
        versionManager.LoadBundle("gameassets", (bundle, error) =>
        {
            if (bundle != null)
            {
                Logger.Instance.LogInfo("Game bundle loaded successfully", "GameManager");
                EventManager.Instance.InvokeEvent("GameBundleLoaded", bundle);
            }
            else
            {
                Logger.Instance.LogError($"Failed to load game bundle: {error}", "GameManager");
            }
        });
    }

    public LoginController GetLoginController()
    {
        return loginController;
    }

    public NetworkManager GetNetworkManager()
    {
        return networkManager;
    }

    public VersionManager GetVersionManager()
    {
        return versionManager;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        EventManager.Instance.RemoveListener("SceneLoaded", OnSceneLoaded);
        EventManager.Instance.RemoveListener("SceneUnloaded", OnSceneUnloaded);

        if (networkManager != null && networkManager.IsConnected)
        {
            networkManager.Disconnect();
        }
        Time.timeScale = 1f;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Logger.Instance.LogInfo("Application paused", "GameManager");
            if (CurrentState == GameState.Playing)
            {
                DataManager.Instance.SaveData("GameState", new GameStateData
                {
                    state = CurrentState,
                    timestamp = System.DateTime.Now
                });
            }
        }
        else
        {
            Logger.Instance.LogInfo("Application resumed", "GameManager");
        }
    }
}

[System.Serializable]
public class GameStateData
{
    public GameManager.GameState state;
    public System.DateTime timestamp;
}

public class GameStateChangedArgs
{
    public GameManager.GameState previousState;
    public GameManager.GameState currentState;
}