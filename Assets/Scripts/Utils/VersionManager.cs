using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class VersionManager : MonoBehaviour
{
    private static VersionManager instance;
    public static VersionManager Instance { get { return instance; } }

    public string currentVersion = "1.0.0";
    public string remoteVersionUrl = "http://localhost:8080/version.json";
    public string remoteBundleUrl = "http://localhost:8080/bundles/";
    public bool enableVersionCheck = false; // 默认禁用版本检查

    private Dictionary<string, string> bundleVersions = new Dictionary<string, string>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CheckVersion(Action<bool, string> callback)
    {
        StartCoroutine(CheckVersionCoroutine(callback));
    }

    private IEnumerator CheckVersionCoroutine(Action<bool, string> callback)
    {
        UnityWebRequest request = UnityWebRequest.Get(remoteVersionUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            VersionConfig config = JsonUtility.FromJson<VersionConfig>(json);

            if (config.version != currentVersion)
            {
                callback(false, "New version available: " + config.version);
            }
            else
            {
                bundleVersions = config.bundles;
                callback(true, "Current version is up to date");
            }
        }
        else
        {
            callback(false, "Failed to check version: " + request.error);
        }
    }

    public void LoadBundle(string bundleName, Action<AssetBundle, string> callback)
    {
        string bundleVersion = "";
        if (bundleVersions.TryGetValue(bundleName, out bundleVersion))
        {
            string bundleUrl = remoteBundleUrl + bundleName + "?v=" + bundleVersion;
            StartCoroutine(LoadBundleCoroutine(bundleUrl, callback));
        }
        else
        {
            callback(null, "Bundle not found in version config");
        }
    }

    private IEnumerator LoadBundleCoroutine(string url, Action<AssetBundle, string> callback)
    {
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
            callback(bundle, "");
        }
        else
        {
            callback(null, "Failed to load bundle: " + request.error);
        }
    }

    [Serializable]
    public class VersionConfig
    {
        public string version;
        public Dictionary<string, string> bundles;
    }
}