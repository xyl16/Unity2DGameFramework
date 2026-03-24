using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ResourceManager : MonoBehaviour
{
    private static ResourceManager instance;
    public static ResourceManager Instance { get { return instance; } }

    [Header("Resource Settings")]
    [SerializeField] private bool useAssetBundles = true;
    [SerializeField] private string localBundlePath = "AssetBundles";
    [SerializeField] private string remoteBundleUrl = "http://localhost:8080/AssetBundles";
    [SerializeField] private bool enableRemoteCheck = true;

    [Header("Version Settings")]
    [SerializeField] private string versionFileName = "version.json";
    [SerializeField] private VersionInfo localVersion;
    [SerializeField] private VersionInfo remoteVersion;

    public enum LoadMode
    {
        Local,      // 本地 Resources
        LocalAB,    // 本地 AB 包
        RemoteAB    // 远程 AB 包
    }

    private LoadMode currentLoadMode = LoadMode.Local;
    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
    private Dictionary<string, UnityEngine.Object> loadedAssets = new Dictionary<string, UnityEngine.Object>();
    private Dictionary<string, string> assetToBundleMap = new Dictionary<string, string>();

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
        if (useAssetBundles)
        {
            LoadLocalVersion();
        }
    }

    #region Version Management

    [System.Serializable]
    public class VersionInfo
    {
        public string version;
        public long updateTime;
        public Dictionary<string, BundleInfo> bundles;
    }

    [System.Serializable]
    public class BundleInfo
    {
        public string bundleName;
        public string hash;
        public long size;
        public string downloadUrl;
    }

    private void LoadLocalVersion()
    {
        string versionPath = Path.Combine(localBundlePath, versionFileName);
        if (File.Exists(versionPath))
        {
            try
            {
                string json = File.ReadAllText(versionPath);
                localVersion = JsonUtility.FromJson<VersionInfo>(json);
                Logger.Instance.LogInfo($"Local version loaded: {localVersion.version}", "ResourceManager");
            }
            catch (Exception e)
            {
                Logger.Instance.LogError($"Failed to load local version: {e.Message}", "ResourceManager");
                localVersion = new VersionInfo();
            }
        }
        else
        {
            localVersion = new VersionInfo { version = "1.0.0", bundles = new Dictionary<string, BundleInfo>() };
        }
    }

    public void CheckVersion(UnityAction<bool, string> onComplete)
    {
        if (!enableRemoteCheck || !useAssetBundles)
        {
            onComplete?.Invoke(true, "Remote check disabled, using local resources");
            return;
        }

        StartCoroutine(CheckRemoteVersionCoroutine(onComplete));
    }

    private IEnumerator CheckRemoteVersionCoroutine(UnityAction<bool, string> onComplete)
    {
        string url = Path.Combine(remoteBundleUrl, versionFileName);
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    remoteVersion = JsonUtility.FromJson<VersionInfo>(request.downloadHandler.text);
                    Logger.Instance.LogInfo($"Remote version: {remoteVersion.version}", "ResourceManager");

                    bool needUpdate = CompareVersion(remoteVersion.version, localVersion.version) > 0;
                    if (needUpdate)
                    {
                        onComplete?.Invoke(true, $"New version available: {remoteVersion.version}");
                    }
                    else
                    {
                        onComplete?.Invoke(true, "Already up to date");
                    }
                }
                catch (Exception e)
                {
                    onComplete?.Invoke(false, $"Failed to parse remote version: {e.Message}");
                }
            }
            else
            {
                Logger.Instance.LogWarning($"Failed to check remote version: {request.error}", "ResourceManager");
                onComplete?.Invoke(false, $"Failed to connect to remote server: {request.error}");
            }
        }
    }

    private int CompareVersion(string v1, string v2)
    {
        string[] parts1 = v1.Split('.');
        string[] parts2 = v2.Split('.');

        for (int i = 0; i < Mathf.Max(parts1.Length, parts2.Length); i++)
        {
            int num1 = i < parts1.Length ? int.Parse(parts1[i]) : 0;
            int num2 = i < parts2.Length ? int.Parse(parts2[i]) : 0;

            if (num1 != num2)
            {
                return num1.CompareTo(num2);
            }
        }
        return 0;
    }

    #endregion

    #region AssetBundle Loading

    public void DownloadBundle(string bundleName, UnityAction<bool, AssetBundle> onComplete)
    {
        StartCoroutine(DownloadBundleCoroutine(bundleName, onComplete));
    }

    private IEnumerator DownloadBundleCoroutine(string bundleName, UnityAction<bool, AssetBundle> onComplete)
    {
        string url = Path.Combine(remoteBundleUrl, bundleName);
        using (UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);

                // 保存到本地
                string localPath = Path.Combine(localBundlePath, bundleName);
                Directory.CreateDirectory(Path.GetDirectoryName(localPath));
                File.WriteAllBytes(localPath, request.downloadHandler.data);

                loadedBundles[bundleName] = bundle;
                Logger.Instance.LogInfo($"Bundle downloaded and saved: {bundleName}", "ResourceManager");
                onComplete?.Invoke(true, bundle);
            }
            else
            {
                Logger.Instance.LogError($"Failed to download bundle: {bundleName}, error: {request.error}", "ResourceManager");
                onComplete?.Invoke(false, null);
            }
        }
    }

    private AssetBundle LoadLocalBundle(string bundleName)
    {
        if (loadedBundles.TryGetValue(bundleName, out AssetBundle bundle))
        {
            return bundle;
        }

        string localPath = Path.Combine(localBundlePath, bundleName);
        if (!File.Exists(localPath))
        {
            Logger.Instance.LogWarning($"Local bundle not found: {bundleName}", "ResourceManager");
            return null;
        }

        try
        {
            bundle = AssetBundle.LoadFromFile(localPath);
            if (bundle != null)
            {
                loadedBundles[bundleName] = bundle;
                Logger.Instance.LogInfo($"Local bundle loaded: {bundleName}", "ResourceManager");
            }
            return bundle;
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"Failed to load local bundle: {bundleName}, error: {e.Message}", "ResourceManager");
            return null;
        }
    }

    public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
    {
        if (loadedBundles.TryGetValue(bundleName, out AssetBundle bundle))
        {
            bundle.Unload(unloadAllLoadedObjects);
            loadedBundles.Remove(bundleName);
            Logger.Instance.LogInfo($"Bundle unloaded: {bundleName}", "ResourceManager");
        }
    }

    public void UnloadAllBundles(bool unloadAllLoadedObjects = false)
    {
        foreach (var bundle in loadedBundles.Values)
        {
            if (bundle != null)
            {
                bundle.Unload(unloadAllLoadedObjects);
            }
        }
        loadedBundles.Clear();
        Logger.Instance.LogInfo("All bundles unloaded", "ResourceManager");
    }

    #endregion

    #region Asset Loading

    public void SetLoadMode(LoadMode mode)
    {
        currentLoadMode = mode;
        Logger.Instance.LogInfo($"Load mode changed to: {mode}", "ResourceManager");
    }

    public void LoadAssetAsync<T>(string path, UnityAction<T> callback) where T : UnityEngine.Object
    {
        StartCoroutine(LoadAssetCoroutine(path, callback));
    }

    private IEnumerator LoadAssetCoroutine<T>(string path, UnityAction<T> callback) where T : UnityEngine.Object
    {
        string cacheKey = $"{currentLoadMode}:{path}";

        if (loadedAssets.TryGetValue(cacheKey, out UnityEngine.Object cachedAsset))
        {
            callback?.Invoke(cachedAsset as T);
            yield break;
        }

        switch (currentLoadMode)
        {
            case LoadMode.Local:
                yield return StartCoroutine(LoadFromResourcesCoroutine(path, cacheKey, callback));
                break;
            case LoadMode.LocalAB:
                yield return StartCoroutine(LoadFromLocalABCoroutine(path, cacheKey, callback));
                break;
            case LoadMode.RemoteAB:
                yield return StartCoroutine(LoadFromRemoteABCoroutine(path, cacheKey, callback));
                break;
        }
    }

    private IEnumerator LoadFromResourcesCoroutine<T>(string path, string cacheKey, UnityAction<T> callback) where T : UnityEngine.Object
    {
        ResourceRequest request = Resources.LoadAsync<T>(path);
        yield return request;

        if (request.asset != null)
        {
            loadedAssets[cacheKey] = request.asset;
            callback?.Invoke(request.asset as T);
        }
        else
        {
            Logger.Instance.LogError($"Failed to load asset from Resources: {path}", "ResourceManager");
            callback?.Invoke(null);
        }
    }

    private IEnumerator LoadFromLocalABCoroutine<T>(string path, string cacheKey, UnityAction<T> callback) where T : UnityEngine.Object
    {
        string bundleName = GetBundleNameForAsset(path);
        if (string.IsNullOrEmpty(bundleName))
        {
            Logger.Instance.LogError($"No bundle mapping found for asset: {path}", "ResourceManager");
            callback?.Invoke(null);
            yield break;
        }

        AssetBundle bundle = LoadLocalBundle(bundleName);
        if (bundle == null)
        {
            callback?.Invoke(null);
            yield break;
        }

        AssetBundleRequest request = bundle.LoadAssetAsync<T>(path);
        yield return request;

        if (request.asset != null)
        {
            loadedAssets[cacheKey] = request.asset;
            callback?.Invoke(request.asset as T);
        }
        else
        {
            Logger.Instance.LogError($"Failed to load asset from bundle: {path}", "ResourceManager");
            callback?.Invoke(null);
        }
    }

    private IEnumerator LoadFromRemoteABCoroutine<T>(string path, string cacheKey, UnityAction<T> callback) where T : UnityEngine.Object
    {
        string bundleName = GetBundleNameForAsset(path);
        if (string.IsNullOrEmpty(bundleName))
        {
            Logger.Instance.LogError($"No bundle mapping found for asset: {path}", "ResourceManager");
            callback?.Invoke(null);
            yield break;
        }

        // 先尝试本地
        AssetBundle bundle = LoadLocalBundle(bundleName);
        if (bundle == null)
        {
            // 本地没有则下载
            yield return StartCoroutine(DownloadBundleCoroutine(bundleName, (success, downloadedBundle) =>
            {
                if (success && downloadedBundle != null)
                {
                    StartCoroutine(LoadFromBundle(downloadedBundle, path, cacheKey, callback));
                }
                else
                {
                    callback?.Invoke(null);
                }
            }));
            yield break;
        }

        yield return StartCoroutine(LoadFromBundle(bundle, path, cacheKey, callback));
    }

    private IEnumerator LoadFromBundle<T>(AssetBundle bundle, string path, string cacheKey, UnityAction<T> callback) where T : UnityEngine.Object
    {
        AssetBundleRequest request = bundle.LoadAssetAsync<T>(path);
        yield return request;

        if (request.asset != null)
        {
            loadedAssets[cacheKey] = request.asset;
            callback?.Invoke(request.asset as T);
        }
        else
        {
            Logger.Instance.LogError($"Failed to load asset from bundle: {path}", "ResourceManager");
            callback?.Invoke(null);
        }
    }

    public T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
        string cacheKey = $"{currentLoadMode}:{path}";

        if (loadedAssets.TryGetValue(cacheKey, out UnityEngine.Object cachedAsset))
        {
            return cachedAsset as T;
        }

        switch (currentLoadMode)
        {
            case LoadMode.Local:
                return LoadFromResources<T>(path, cacheKey);
            case LoadMode.LocalAB:
            case LoadMode.RemoteAB:
                return LoadFromBundle<T>(path, cacheKey);
            default:
                return null;
        }
    }

    private T LoadFromResources<T>(string path, string cacheKey) where T : UnityEngine.Object
    {
        T asset = Resources.Load<T>(path);
        if (asset != null)
        {
            loadedAssets[cacheKey] = asset;
        }
        return asset;
    }

    private T LoadFromBundle<T>(string path, string cacheKey) where T : UnityEngine.Object
    {
        string bundleName = GetBundleNameForAsset(path);
        if (string.IsNullOrEmpty(bundleName))
        {
            Logger.Instance.LogError($"No bundle mapping found for asset: {path}", "ResourceManager");
            return null;
        }

        AssetBundle bundle = LoadLocalBundle(bundleName);
        if (bundle == null)
        {
            return null;
        }

        T asset = bundle.LoadAsset<T>(path);
        if (asset != null)
        {
            loadedAssets[cacheKey] = asset;
        }
        return asset;
    }

    private string GetBundleNameForAsset(string assetPath)
    {
        if (assetToBundleMap.TryGetValue(assetPath, out string bundleName))
        {
            return bundleName;
        }

        // 如果没有映射表，尝试根据本地版本信息查找
        if (localVersion != null && localVersion.bundles != null)
        {
            foreach (var bundleInfo in localVersion.bundles.Values)
            {
                if (bundleInfo.bundleName != null && assetPath.StartsWith(bundleInfo.bundleName))
                {
                    return bundleInfo.bundleName;
                }
            }
        }

        return null;
    }

    public void SetAssetBundleMapping(Dictionary<string, string> mapping)
    {
        assetToBundleMap = mapping;
        Logger.Instance.LogInfo($"Asset bundle mapping set with {mapping.Count} entries", "ResourceManager");
    }

    #endregion

    #region Unload

    public void UnloadAsset(string path)
    {
        string cacheKey = $"{currentLoadMode}:{path}";
        if (loadedAssets.TryGetValue(cacheKey, out UnityEngine.Object asset))
        {
            Resources.UnloadAsset(asset);
            loadedAssets.Remove(cacheKey);
        }
    }

    public void UnloadAllAssets()
    {
        foreach (var asset in loadedAssets.Values)
        {
            if (asset != null)
            {
                Resources.UnloadAsset(asset);
            }
        }
        loadedAssets.Clear();
        Logger.Instance.LogInfo("All assets unloaded", "ResourceManager");
    }

    #endregion

    #region Properties

    public LoadMode CurrentLoadMode
    {
        get { return currentLoadMode; }
    }

    public string LocalBundlePath
    {
        get { return localBundlePath; }
        set { localBundlePath = value; }
    }

    public string RemoteBundleUrl
    {
        get { return remoteBundleUrl; }
        set { remoteBundleUrl = value; }
    }

    public VersionInfo LocalVersion
    {
        get { return localVersion; }
    }

    public VersionInfo RemoteVersion
    {
        get { return remoteVersion; }
    }

    #endregion
}
