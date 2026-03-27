using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 模块化AssetBundle加载器
/// 支持自动依赖解析、优先级加载、热更新
/// </summary>
public class AssetBundleLoader : MonoBehaviour
{
    private static AssetBundleLoader instance;
    public static AssetBundleLoader Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("AssetBundleLoader");
                instance = obj.AddComponent<AssetBundleLoader>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    [Header("路径配置")]
    [SerializeField] private string localBundlePath = "AssetBundles";
    [SerializeField] private string streamingAssetsPath = "AssetBundles";

    [Header("运行时设置")]
    [SerializeField] private bool checkUpdateOnStart = false;
    [SerializeField] private bool loadDependenciesAutomatically = true;

    // 加载状态
    public enum LoadStatus
    {
        Idle,
        LoadingDependencies,
        LoadingBundle,
        Completed,
        Failed
    }

    public LoadStatus CurrentStatus { get; private set; }
    public float LoadProgress { get; private set; }

    // Bundle缓存
    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
    private Dictionary<string, AssetBundleManifest> bundleManifests = new Dictionary<string, AssetBundleManifest>();

    // 版本信息
    [System.Serializable]
    public class BundleVersionInfo
    {
        public string version;
        public long updateTime;
        public List<BundleFile> bundles;
    }

    [System.Serializable]
    public class BundleFile
    {
        public string fileName;
        public string hash;
        public long size;
        public int priority;
        public List<string> dependencies;
    }

    private BundleVersionInfo versionInfo;
    private string platformPath;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializePlatformPath();
        
        if (checkUpdateOnStart)
        {
            StartCoroutine(CheckVersionCoroutine());
        }
    }

    /// <summary>
    /// 初始化平台路径
    /// </summary>
    private void InitializePlatformPath()
    {
        platformPath = GetPlatformFolder();
        Logger.Instance.LogInfo($"Platform path: {platformPath}", "AssetBundleLoader");
    }

    /// <summary>
    /// 加载版本信息
    /// </summary>
    public void LoadVersionInfo(Action<bool> onComplete)
    {
        StartCoroutine(LoadVersionInfoCoroutine(onComplete));
    }

    private IEnumerator LoadVersionInfoCoroutine(Action<bool> onComplete)
    {
        string versionPath = GetVersionPath();

        if (!File.Exists(versionPath))
        {
            Logger.Instance.LogWarning($"Version file not found: {versionPath}", "AssetBundleLoader");
            onComplete?.Invoke(false);
            yield break;
        }

        string json = File.ReadAllText(versionPath);
        versionInfo = JsonUtility.FromJson<BundleVersionInfo>(json);

        if (versionInfo != null)
        {
            Logger.Instance.LogInfo($"Version loaded: {versionInfo.version}, Bundles: {versionInfo.bundles.Count}", "AssetBundleLoader");
            onComplete?.Invoke(true);
        }
        else
        {
            Logger.Instance.LogError("Failed to parse version file", "AssetBundleLoader");
            onComplete?.Invoke(false);
        }
    }

    /// <summary>
    /// 检查版本更新
    /// </summary>
    public IEnumerator CheckVersionCoroutine(Action<bool, string> onUpdateAvailable = null)
    {
        // 这里可以集成 AssetBundleUpdater 进行热更检查
        Logger.Instance.LogInfo("Checking for updates...", "AssetBundleLoader");
        yield return null;
        onUpdateAvailable?.Invoke(false, "No updates available");
    }

    /// <summary>
    /// 加载单个Bundle
    /// </summary>
    public void LoadBundle(string bundleName, Action<bool, AssetBundle> onComplete)
    {
        StartCoroutine(LoadBundleCoroutine(bundleName, onComplete));
    }

    private IEnumerator LoadBundleCoroutine(string bundleName, Action<bool, AssetBundle> onComplete)
    {
        CurrentStatus = LoadStatus.LoadingBundle;
        LoadProgress = 0f;

        // 检查是否已加载
        if (loadedBundles.ContainsKey(bundleName))
        {
            Logger.Instance.LogInfo($"Bundle already loaded: {bundleName}", "AssetBundleLoader");
            onComplete?.Invoke(true, loadedBundles[bundleName]);
            yield break;
        }

        // 自动加载依赖
        if (loadDependenciesAutomatically && versionInfo != null)
        {
            var bundleInfo = versionInfo.bundles.Find(b => b.fileName == bundleName);
            if (bundleInfo != null && bundleInfo.dependencies != null && bundleInfo.dependencies.Count > 0)
            {
                CurrentStatus = LoadStatus.LoadingDependencies;
                Logger.Instance.LogInfo($"Loading dependencies for {bundleName}: {string.Join(", ", bundleInfo.dependencies)}", "AssetBundleLoader");

                foreach (var dep in bundleInfo.dependencies)
                {
                    bool depLoaded = false;
                    yield return StartCoroutine(LoadBundleCoroutine(dep, (success, bundle) =>
                    {
                        depLoaded = success;
                    }));

                    if (!depLoaded)
                    {
                        Logger.Instance.LogError($"Failed to load dependency: {dep}", "AssetBundleLoader");
                        CurrentStatus = LoadStatus.Failed;
                        onComplete?.Invoke(false, null);
                        yield break;
                    }
                }
            }
        }

        // 加载Bundle
        string bundlePath = GetBundlePath(bundleName);
        var loadRequest = AssetBundle.LoadFromFileAsync(bundlePath);

        while (!loadRequest.isDone)
        {
            LoadProgress = 0.5f + loadRequest.progress * 0.5f;
            yield return null;
        }

        if (loadRequest.assetBundle == null)
        {
            Logger.Instance.LogError($"Failed to load bundle: {bundleName}", "AssetBundleLoader");
            CurrentStatus = LoadStatus.Failed;
            onComplete?.Invoke(false, null);
            yield break;
        }

        loadedBundles[bundleName] = loadRequest.assetBundle;

        // 加载Manifest（主Bundle才包含Manifest）
        if (bundleName == bundleName.Replace(Path.GetExtension(bundleName), "") ||
            !loadedBundles.ContainsKey(bundleName.Replace(Path.GetExtension(bundleName), "")))
        {
            var manifestRequest = loadRequest.assetBundle.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
            yield return manifestRequest;

            if (manifestRequest.asset != null)
            {
                bundleManifests[bundleName] = (AssetBundleManifest)manifestRequest.asset;
                Logger.Instance.LogInfo($"Bundle manifest loaded: {bundleName}", "AssetBundleLoader");
            }
        }

        CurrentStatus = LoadStatus.Completed;
        LoadProgress = 1f;

        Logger.Instance.LogInfo($"Bundle loaded successfully: {bundleName}", "AssetBundleLoader");
        onComplete?.Invoke(true, loadRequest.assetBundle);
    }

    /// <summary>
    /// 加载多个Bundle（按优先级顺序）
    /// </summary>
    public void LoadBundles(List<string> bundleNames, Action<bool, Dictionary<string, AssetBundle>> onComplete)
    {
        StartCoroutine(LoadBundlesCoroutine(bundleNames, onComplete));
    }

    private IEnumerator LoadBundlesCoroutine(List<string> bundleNames, Action<bool, Dictionary<string, AssetBundle>> onComplete)
    {
        var loadedBundlesResult = new Dictionary<string, AssetBundle>();

        // 按优先级排序
        var sortedBundles = SortBundlesByPriority(bundleNames);
        Logger.Instance.LogInfo($"Loading bundles in order: {string.Join(" -> ", sortedBundles)}", "AssetBundleLoader");

        // 依次加载
        foreach (string bundleName in sortedBundles)
        {
            bool loaded = false;
            AssetBundle bundle = null;

            yield return StartCoroutine(LoadBundleCoroutine(bundleName, (success, loadedBundle) =>
            {
                loaded = success;
                bundle = loadedBundle;
            }));

            if (loaded && bundle != null)
            {
                loadedBundlesResult[bundleName] = bundle;
            }
            else
            {
                Logger.Instance.LogError($"Failed to load bundle: {bundleName}", "AssetBundleLoader");
                onComplete?.Invoke(false, loadedBundlesResult);
                yield break;
            }
        }

        onComplete?.Invoke(true, loadedBundlesResult);
    }

    /// <summary>
    /// 从Bundle加载资源
    /// </summary>
    public void LoadAsset<T>(string bundleName, string assetName, Action<bool, T> onComplete) where T : UnityEngine.Object
    {
        StartCoroutine(LoadAssetCoroutine(bundleName, assetName, onComplete));
    }

    private IEnumerator LoadAssetCoroutine<T>(string bundleName, string assetName, Action<bool, T> onComplete) where T : UnityEngine.Object
    {
        if (!loadedBundles.ContainsKey(bundleName))
        {
            bool loaded = false;
            yield return StartCoroutine(LoadBundleCoroutine(bundleName, (success, bundle) =>
            {
                loaded = success;
            }));

            if (!loaded)
            {
                Logger.Instance.LogError($"Failed to load bundle: {bundleName}", "AssetBundleLoader");
                onComplete?.Invoke(false, null);
                yield break;
            }
        }

        var assetRequest = loadedBundles[bundleName].LoadAssetAsync<T>(assetName);
        yield return assetRequest;

        if (assetRequest.asset == null)
        {
            Logger.Instance.LogError($"Failed to load asset: {assetName}", "AssetBundleLoader");
            onComplete?.Invoke(false, null);
            yield break;
        }

        onComplete?.Invoke(true, (T)assetRequest.asset);
    }

    /// <summary>
    /// 从Bundle加载所有资源
    /// </summary>
    public void LoadAllAssets<T>(string bundleName, Action<bool, T[]> onComplete) where T : UnityEngine.Object
    {
        StartCoroutine(LoadAllAssetsCoroutine(bundleName, onComplete));
    }

    private IEnumerator LoadAllAssetsCoroutine<T>(string bundleName, Action<bool, T[]> onComplete) where T : UnityEngine.Object
    {
        if (!loadedBundles.ContainsKey(bundleName))
        {
            bool loaded = false;
            yield return StartCoroutine(LoadBundleCoroutine(bundleName, (success, bundle) =>
            {
                loaded = success;
            }));

            if (!loaded)
            {
                Logger.Instance.LogError($"Failed to load bundle: {bundleName}", "AssetBundleLoader");
                onComplete?.Invoke(false, null);
                yield break;
            }
        }

        var assets = loadedBundles[bundleName].LoadAllAssets<T>();
        onComplete?.Invoke(true, assets);
    }

    /// <summary>
    /// 卸载Bundle
    /// </summary>
    public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
    {
        if (loadedBundles.ContainsKey(bundleName))
        {
            loadedBundles[bundleName].Unload(unloadAllLoadedObjects);
            loadedBundles.Remove(bundleName);
            bundleManifests.Remove(bundleName);
            Logger.Instance.LogInfo($"Bundle unloaded: {bundleName}", "AssetBundleLoader");
        }
    }

    /// <summary>
    /// 卸载所有Bundle
    /// </summary>
    public void UnloadAllBundles(bool unloadAllLoadedObjects = false)
    {
        foreach (var kvp in loadedBundles)
        {
            kvp.Value.Unload(unloadAllLoadedObjects);
        }
        loadedBundles.Clear();
        bundleManifests.Clear();
        Logger.Instance.LogInfo("All bundles unloaded", "AssetBundleLoader");
    }

    /// <summary>
    /// 按优先级排序Bundle
    /// </summary>
    private List<string> SortBundlesByPriority(List<string> bundleNames)
    {
        var result = new List<string>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (string bundleName in bundleNames)
        {
            if (!visited.Contains(bundleName))
            {
                VisitBundleForSort(bundleName, visited, visiting, result);
            }
        }

        // 按版本信息中的优先级进一步排序
        if (versionInfo != null)
        {
            result.Sort((a, b) =>
            {
                int priorityA = 999;
                int priorityB = 999;
                var infoA = versionInfo.bundles.Find(x => x.fileName == a);
                var infoB = versionInfo.bundles.Find(x => x.fileName == b);
                if (infoA != null) priorityA = infoA.priority;
                if (infoB != null) priorityB = infoB.priority;
                return priorityA.CompareTo(priorityB);
            });
        }

        return result;
    }

    /// <summary>
    /// 深度优先遍历排序（处理依赖）
    /// </summary>
    private void VisitBundleForSort(string bundleName, HashSet<string> visited, HashSet<string> visiting, List<string> result)
    {
        if (visiting.Contains(bundleName))
        {
            Logger.Instance.LogError($"Circular dependency detected: {bundleName}", "AssetBundleLoader");
            return;
        }

        if (visited.Contains(bundleName))
        {
            return;
        }

        visiting.Add(bundleName);

        if (versionInfo != null)
        {
            var bundleInfo = versionInfo.bundles.Find(b => b.fileName == bundleName);
            if (bundleInfo != null && bundleInfo.dependencies != null)
            {
                foreach (var dep in bundleInfo.dependencies)
                {
                    VisitBundleForSort(dep, visited, visiting, result);
                }
            }
        }

        visiting.Remove(bundleName);
        visited.Add(bundleName);
        result.Add(bundleName);
    }

    /// <summary>
    /// 获取Bundle路径
    /// </summary>
    private string GetBundlePath(string bundleName)
    {
        // 优先使用本地更新路径
        string localPath = Path.Combine(localBundlePath, platformPath, bundleName);
        if (File.Exists(localPath))
        {
            return localPath;
        }

        // 使用StreamingAssets路径
        string streamingPath = Path.Combine(Application.streamingAssetsPath, streamingAssetsPath, platformPath, bundleName);
        if (File.Exists(streamingPath))
        {
            return streamingPath;
        }

        Logger.Instance.LogError($"Bundle file not found: {bundleName}", "AssetBundleLoader");
        return localPath;
    }

    /// <summary>
    /// 获取版本文件路径
    /// </summary>
    private string GetVersionPath()
    {
        // 优先使用本地更新路径
        string localPath = Path.Combine(localBundlePath, "version.json");
        if (File.Exists(localPath))
        {
            return localPath;
        }

        // 使用StreamingAssets路径
        string streamingPath = Path.Combine(Application.streamingAssetsPath, "version.json");
        return streamingPath;
    }

    /// <summary>
    /// 获取平台文件夹
    /// </summary>
    private string GetPlatformFolder()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
                return "Windows";
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                return "OSX";
            case RuntimePlatform.LinuxPlayer:
                return "Linux";
            case RuntimePlatform.IPhonePlayer:
                return "iOS";
            case RuntimePlatform.Android:
                return "Android";
            case RuntimePlatform.WebGLPlayer:
                return "WebGL";
            default:
                return Application.platform.ToString();
        }
    }

    /// <summary>
    /// 获取已加载的Bundle
    /// </summary>
    public AssetBundle GetLoadedBundle(string bundleName)
    {
        return loadedBundles.ContainsKey(bundleName) ? loadedBundles[bundleName] : null;
    }

    /// <summary>
    /// 获取Bundle Manifest
    /// </summary>
    public AssetBundleManifest GetBundleManifest(string bundleName)
    {
        return bundleManifests.ContainsKey(bundleName) ? bundleManifests[bundleName] : null;
    }

    /// <summary>
    /// 检查Bundle是否已加载
    /// </summary>
    public bool IsBundleLoaded(string bundleName)
    {
        return loadedBundles.ContainsKey(bundleName);
    }

    private void OnDestroy()
    {
        UnloadAllBundles(false);
    }
}
