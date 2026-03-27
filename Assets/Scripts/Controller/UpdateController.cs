using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 更新控制器: 负责处理游戏的热更新逻辑
/// 协调各个更新相关模块,提供统一的更新流程
/// </summary>
public class UpdateController : MonoBehaviour
{
    private static UpdateController instance;
    public static UpdateController Instance { get { return instance; } }

    [Header("Update Settings")]
    [SerializeField] private bool checkUpdateOnStart = true;
    [SerializeField] private bool autoDownloadUpdate = false;
    // [SerializeField] private string updateSceneName = "UpdateScene";

    // 更新状态
    public enum UpdateState
    {
        Idle,
        Checking,
        HasUpdate,
        Downloading,
        Verifying,
        Applying,
        Completed,
        Failed
    }

    private UpdateState currentState = UpdateState.Idle;
    private AssetBundleUpdater.VersionManifest remoteVersionInfo;
    private bool updateRequired = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (checkUpdateOnStart)
        {
            CheckForUpdate();
        }

        RegisterEvents();
    }

    private void RegisterEvents()
    {
        EventManager.Instance.AddListener("UpdateRequested", OnUpdateRequested);
        EventManager.Instance.AddListener("UpdateCanceled", OnUpdateCanceled);
    }

    #region Update Flow

    /// <summary>
    /// 检查更新
    /// </summary>
    public void CheckForUpdate()
    {
        if (currentState != UpdateState.Idle)
        {
            Logger.Instance.LogWarning($"当前状态不支持检查更新: {currentState}", "UpdateController");
            return;
        }

        StartCoroutine(CheckUpdateCoroutine());
    }

    private IEnumerator CheckUpdateCoroutine()
    {
        currentState = UpdateState.Checking;
        Logger.Instance.LogInfo("开始检查更新...", "UpdateController");

        // 检查AB包更新
        AssetBundleUpdater.Instance.CheckUpdate((success, message, versionInfo) =>
        {
            if (success && versionInfo != null)
            {
                if (message.Contains("发现新版本"))
                {
                    currentState = UpdateState.HasUpdate;
                    remoteVersionInfo = versionInfo;
                    updateRequired = true;

                    Logger.Instance.LogInfo($"发现更新: {message}", "UpdateController");
                    EventManager.Instance.InvokeEvent("UpdateAvailable", new UpdateInfo
                    {
                        currentVersion = AssetBundleUpdater.Instance.GetLocalVersion(),
                        remoteVersion = versionInfo.version,
                        message = message
                    });

                    // 自动下载
                    if (autoDownloadUpdate)
                    {
                        StartUpdate();
                    }
                }
                else
                {
                    currentState = UpdateState.Completed;
                    Logger.Instance.LogInfo(message, "UpdateController");
                    EventManager.Instance.InvokeEvent("NoUpdate", null);
                }
            }
            else
            {
                currentState = UpdateState.Failed;
                Logger.Instance.LogError(message, "UpdateController");
                EventManager.Instance.InvokeEvent("UpdateCheckFailed", message);
            }
        });

        yield return new WaitWhile(() => currentState == UpdateState.Checking);
    }

    /// <summary>
    /// 开始更新
    /// </summary>
    public void StartUpdate()
    {
        if (currentState != UpdateState.HasUpdate)
        {
            Logger.Instance.LogWarning($"当前状态不支持开始更新: {currentState}", "UpdateController");
            return;
        }

        StartCoroutine(UpdateCoroutine());
    }

    private IEnumerator UpdateCoroutine()
    {
        currentState = UpdateState.Downloading;
        Logger.Instance.LogInfo("开始下载更新...", "UpdateController");

        AssetBundleUpdater.Instance.StartUpdate((success, message) =>
        {
            if (success)
            {
                currentState = UpdateState.Completed;
                Logger.Instance.LogInfo($"更新完成: {message}", "UpdateController");

                // 重新加载资源
                RefreshResources();

                EventManager.Instance.InvokeEvent("UpdateCompleted", message);
            }
            else
            {
                currentState = UpdateState.Failed;
                Logger.Instance.LogError($"更新失败: {message}", "UpdateController");
                EventManager.Instance.InvokeEvent("UpdateFailed", message);
            }
        });

        yield return new WaitWhile(() => currentState == UpdateState.Downloading ||
                                     currentState == UpdateState.Verifying ||
                                     currentState == UpdateState.Applying);
    }

    /// <summary>
    /// 取消更新
    /// </summary>
    public void CancelUpdate()
    {
        if (currentState == UpdateState.Downloading ||
            currentState == UpdateState.Verifying ||
            currentState == UpdateState.Applying)
        {
            AssetBundleUpdater.Instance.CancelUpdate();
            currentState = UpdateState.Idle;
            updateRequired = false;

            Logger.Instance.LogInfo("更新已取消", "UpdateController");
            EventManager.Instance.InvokeEvent("UpdateCanceled", null);
        }
    }

    /// <summary>
    /// 刷新资源
    /// </summary>
    private void RefreshResources()
    {
        // 卸载所有AB包
        ResourceManager.Instance.UnloadAllBundles(false);

        // 清理资源缓存
        ResourceManager.Instance.UnloadAllAssets();

        // 切换到RemoteAB模式
        ResourceManager.Instance.SetLoadMode(ResourceManager.LoadMode.RemoteAB);

        Logger.Instance.LogInfo("资源已刷新", "UpdateController");
    }

    #endregion

    #region Event Handlers

    private void OnUpdateRequested(object data)
    {
        if (currentState == UpdateState.HasUpdate)
        {
            StartUpdate();
        }
        else if (currentState == UpdateState.Idle)
        {
            CheckForUpdate();
        }
    }

    private void OnUpdateCanceled(object data)
    {
        CancelUpdate();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public UpdateState GetState()
    {
        return currentState;
    }

    /// <summary>
    /// 是否需要更新
    /// </summary>
    public bool IsUpdateRequired()
    {
        return updateRequired;
    }

    /// <summary>
    /// 获取更新信息
    /// </summary>
    public UpdateInfo GetUpdateInfo()
    {
        if (remoteVersionInfo == null)
            return null;

        return new UpdateInfo
        {
            currentVersion = AssetBundleUpdater.Instance.GetLocalVersion(),
            remoteVersion = remoteVersionInfo.version,
            message = $"当前版本: {AssetBundleUpdater.Instance.GetLocalVersion()}, 最新版本: {remoteVersionInfo.version}"
        };
    }

    /// <summary>
    /// 获取下载进度
    /// </summary>
    public float GetDownloadProgress()
    {
        return AssetBundleUpdater.Instance.DownloadProgress;
    }

    /// <summary>
    /// 获取当前下载文件
    /// </summary>
    public string GetCurrentDownloadFile()
    {
        return AssetBundleUpdater.Instance.CurrentDownloadFile;
    }

    #endregion

    #region Data Structures

    /// <summary>
    /// 更新信息
    /// </summary>
    public class UpdateInfo
    {
        public string currentVersion;
        public string remoteVersion;
        public string message;
    }

    #endregion

    private void OnDestroy()
    {
        EventManager.Instance.RemoveListener("UpdateRequested", OnUpdateRequested);
        EventManager.Instance.RemoveListener("UpdateCanceled", OnUpdateCanceled);
    }
}
