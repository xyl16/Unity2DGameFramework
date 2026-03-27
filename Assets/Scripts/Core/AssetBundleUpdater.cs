using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// AB包热更新管理器
/// 提供增量更新、断点续传、文件校验等功能
/// </summary>
public class AssetBundleUpdater : MonoBehaviour
{
    private static AssetBundleUpdater instance;
    public static AssetBundleUpdater Instance { get { return instance; } }

    [Header("Update Settings")]
    // [SerializeField] private bool enableAutoUpdate = true;
    [SerializeField] private string updateServerUrl = "http://localhost:8080/AssetBundles";
    [SerializeField] private string localBundlePath = "AssetBundles";
    [SerializeField] private string versionFileName = "version.json";

    [Header("Download Settings")]
    [SerializeField] private int maxRetryCount = 3;
    [SerializeField] private float downloadTimeout = 30f;
    [SerializeField] private bool enableResume = true;
    [SerializeField] private string tempFileExtension = ".tmp";

    // 更新状态
    public enum UpdateStatus
    {
        Idle,
        CheckingVersion,
        Downloading,
        Verifying,
        Applying,
        Completed,
        Failed
    }

    public UpdateStatus CurrentStatus { get; private set; }
    public float DownloadProgress { get; private set; }
    public string CurrentDownloadFile { get; private set; }

    // 版本信息
    [System.Serializable]
    public class VersionManifest
    {
        public string version;
        public long updateTime;
        public List<BundleFile> files;
    }

    [System.Serializable]
    public class BundleFile
    {
        public string fileName;
        public string hash;
        public long size;
        public string downloadUrl;
    }

    private VersionManifest localVersion;
    private VersionManifest remoteVersion;
    private List<BundleFile> needUpdateFiles = new List<BundleFile>();
    private Dictionary<string, string> downloadedFiles = new Dictionary<string, string>();

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
        LoadLocalVersion();
    }

    #region Version Management

    /// <summary>
    /// 加载本地版本信息
    /// </summary>
    private void LoadLocalVersion()
    {
        string versionPath = Path.Combine(localBundlePath, versionFileName);
        if (File.Exists(versionPath))
        {
            try
            {
                string json = File.ReadAllText(versionPath);
                localVersion = JsonUtility.FromJson<VersionManifest>(json);
                Logger.Instance.LogInfo($"Local version loaded: {localVersion.version}", "AssetBundleUpdater");
            }
            catch (Exception e)
            {
                Logger.Instance.LogWarning($"Failed to load local version: {e.Message}", "AssetBundleUpdater");
                localVersion = new VersionManifest { version = "1.0.0", files = new List<BundleFile>() };
            }
        }
        else
        {
            localVersion = new VersionManifest { version = "1.0.0", files = new List<BundleFile>() };
        }
    }

    /// <summary>
    /// 保存本地版本信息
    /// </summary>
    private void SaveLocalVersion()
    {
        try
        {
            string versionPath = Path.Combine(localBundlePath, versionFileName);
            Directory.CreateDirectory(localBundlePath);
            string json = JsonUtility.ToJson(localVersion, true);
            File.WriteAllText(versionPath, json);
            Logger.Instance.LogInfo($"Local version saved: {localVersion.version}", "AssetBundleUpdater");
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"Failed to save local version: {e.Message}", "AssetBundleUpdater");
        }
    }

    /// <summary>
    /// 检查远程版本
    /// </summary>
    public void CheckUpdate(Action<bool, string, VersionManifest> callback)
    {
        StartCoroutine(CheckUpdateCoroutine(callback));
    }

    private IEnumerator CheckUpdateCoroutine(Action<bool, string, VersionManifest> callback)
    {
        CurrentStatus = UpdateStatus.CheckingVersion;
        Logger.Instance.LogInfo("Checking remote version...", "AssetBundleUpdater");

        string url = Path.Combine(updateServerUrl, versionFileName);
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 30;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    remoteVersion = JsonUtility.FromJson<VersionManifest>(request.downloadHandler.text);
                    Logger.Instance.LogInfo($"Remote version: {remoteVersion.version}", "AssetBundleUpdater");

                    bool needUpdate = CompareVersion(remoteVersion.version, localVersion.version) > 0;
                    if (needUpdate)
                    {
                        CalculateNeedUpdateFiles();
                        long totalSize = CalculateTotalDownloadSize();
                        callback?.Invoke(true, $"发现新版本: {remoteVersion.version}, 需要下载 {needUpdateFiles.Count} 个文件, 总大小: {FormatSize(totalSize)}", remoteVersion);
                    }
                    else
                    {
                        callback?.Invoke(true, "当前已是最新版本", remoteVersion);
                    }
                }
                catch (Exception e)
                {
                    CurrentStatus = UpdateStatus.Failed;
                    callback?.Invoke(false, $"解析远程版本失败: {e.Message}", null);
                }
            }
            else
            {
                CurrentStatus = UpdateStatus.Failed;
                Logger.Instance.LogError($"检查远程版本失败: {request.error}", "AssetBundleUpdater");
                callback?.Invoke(false, $"连接服务器失败: {request.error}", null);
            }

            CurrentStatus = UpdateStatus.Idle;
        }
    }

    /// <summary>
    /// 计算需要更新的文件
    /// </summary>
    private void CalculateNeedUpdateFiles()
    {
        needUpdateFiles.Clear();

        if (remoteVersion == null || remoteVersion.files == null)
            return;

        // 构建本地文件哈希字典
        Dictionary<string, string> localFileHashes = new Dictionary<string, string>();
        if (localVersion != null && localVersion.files != null)
        {
            foreach (var file in localVersion.files)
            {
                localFileHashes[file.fileName] = file.hash;
            }
        }

        // 比较远程文件与本地文件
        foreach (var remoteFile in remoteVersion.files)
        {
            if (!localFileHashes.ContainsKey(remoteFile.fileName))
            {
                // 新文件
                needUpdateFiles.Add(remoteFile);
                Logger.Instance.LogInfo($"New file: {remoteFile.fileName}", "AssetBundleUpdater");
            }
            else if (localFileHashes[remoteFile.fileName] != remoteFile.hash)
            {
                // 文件已修改
                needUpdateFiles.Add(remoteFile);
                Logger.Instance.LogInfo($"Modified file: {remoteFile.fileName}", "AssetBundleUpdater");
            }
        }
    }

    /// <summary>
    /// 计算总下载大小
    /// </summary>
    private long CalculateTotalDownloadSize()
    {
        long totalSize = 0;
        foreach (var file in needUpdateFiles)
        {
            totalSize += file.size;
        }
        return totalSize;
    }

    /// <summary>
    /// 比较版本号
    /// </summary>
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

    #region Download

    /// <summary>
    /// 开始更新下载
    /// </summary>
    public void StartUpdate(Action<bool, string> callback)
    {
        StartCoroutine(UpdateCoroutine(callback));
    }

    private IEnumerator UpdateCoroutine(Action<bool, string> callback)
    {
        if (needUpdateFiles.Count == 0)
        {
            callback?.Invoke(true, "无需更新文件");
            yield break;
        }

        CurrentStatus = UpdateStatus.Downloading;
        downloadedFiles.Clear();

        Logger.Instance.LogInfo($"开始更新, 共 {needUpdateFiles.Count} 个文件", "AssetBundleUpdater");

        // 确保目录存在
        Directory.CreateDirectory(localBundlePath);

        // 下载所有需要更新的文件
        for (int i = 0; i < needUpdateFiles.Count; i++)
        {
            BundleFile file = needUpdateFiles[i];
            CurrentDownloadFile = file.fileName;
            DownloadProgress = (float)i / needUpdateFiles.Count;

            bool downloadSuccess = false;
            yield return StartCoroutine(DownloadFileCoroutine(file, maxRetryCount, (success) => {
                downloadSuccess = success;
            }));

            if (!downloadSuccess)
            {
                CurrentStatus = UpdateStatus.Failed;
                callback?.Invoke(false, $"下载失败: {file.fileName}");
                yield break;
            }

            Logger.Instance.LogInfo($"下载完成: {file.fileName} ({i + 1}/{needUpdateFiles.Count})", "AssetBundleUpdater");
        }

        // 验证下载的文件
        CurrentStatus = UpdateStatus.Verifying;
        bool allVerified = true;
        yield return StartCoroutine(VerifyFilesCoroutine());

        if (!allVerified)
        {
            CurrentStatus = UpdateStatus.Failed;
            callback?.Invoke(false, "文件验证失败");
            yield break;
        }

        // 应用更新
        CurrentStatus = UpdateStatus.Applying;
        bool applied = true;
        yield return StartCoroutine(ApplyUpdateCoroutine());

        if (!applied)
        {
            CurrentStatus = UpdateStatus.Failed;
            callback?.Invoke(false, "应用更新失败");
            yield break;
        }

        CurrentStatus = UpdateStatus.Completed;
        DownloadProgress = 1f;
        Logger.Instance.LogInfo($"更新完成, 版本: {remoteVersion.version}", "AssetBundleUpdater");
        callback?.Invoke(true, $"更新成功, 当前版本: {remoteVersion.version}");
    }

    /// <summary>
    /// 下载单个文件
    /// </summary>
    private IEnumerator DownloadFileCoroutine(BundleFile file, int retryCount, System.Action<bool> onComplete)
    {
        for (int retry = 0; retry < retryCount; retry++)
        {
            string url = string.IsNullOrEmpty(file.downloadUrl)
                ? Path.Combine(updateServerUrl, file.fileName)
                : file.downloadUrl;

            string tempFilePath = Path.Combine(localBundlePath, file.fileName + tempFileExtension);
            string finalFilePath = Path.Combine(localBundlePath, file.fileName);

            // 检查是否有未完成的临时文件(断点续传)
            long existingLength = 0;
            if (enableResume && File.Exists(tempFilePath))
            {
                existingLength = new FileInfo(tempFilePath).Length;
                Logger.Instance.LogInfo($"发现未完成下载: {file.fileName}, 已下载: {FormatSize(existingLength)}", "AssetBundleUpdater");
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                if (enableResume && existingLength > 0)
                {
                    request.SetRequestHeader("Range", $"bytes={existingLength}-");
                }

                request.timeout = (int)downloadTimeout;

                DownloadHandlerBuffer handler = new DownloadHandlerBuffer();
                request.downloadHandler = handler;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        byte[] data = handler.data;

                        // 断点续传: 追加数据
                        if (enableResume && existingLength > 0 && request.responseCode == 206)
                        {
                            byte[] existingData = File.ReadAllBytes(tempFilePath);
                            byte[] combinedData = new byte[existingData.Length + data.Length];
                            Buffer.BlockCopy(existingData, 0, combinedData, 0, existingData.Length);
                            Buffer.BlockCopy(data, 0, combinedData, existingData.Length, data.Length);
                            data = combinedData;
                        }

                        // 验证文件大小
                        if (file.size > 0 && data.Length != file.size)
                        {
                            Logger.Instance.LogWarning($"文件大小不匹配: {file.fileName}, 预期: {file.size}, 实际: {data.Length}", "AssetBundleUpdater");
                            continue;
                        }

                        // 写入临时文件
                        Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath));
                        File.WriteAllBytes(tempFilePath, data);

                        downloadedFiles[file.fileName] = tempFilePath;
                        onComplete?.Invoke(true);
                        yield break; // 下载成功,退出重试循环
                    }
                    catch (Exception e)
                    {
                        Logger.Instance.LogError($"写入文件失败: {file.fileName}, 错误: {e.Message}", "AssetBundleUpdater");
                    }
                }
                else
                {
                    Logger.Instance.LogWarning($"下载失败: {file.fileName}, 重试: {retry + 1}/{retryCount}, 错误: {request.error}", "AssetBundleUpdater");
                }
            }
        }

        Logger.Instance.LogError($"下载失败: {file.fileName}", "AssetBundleUpdater");
        onComplete?.Invoke(false);
    }

    #endregion

    #region Verify

    /// <summary>
    /// 验证下载的文件
    /// </summary>
    private IEnumerator VerifyFilesCoroutine()
    {
        Logger.Instance.LogInfo("开始验证下载文件...", "AssetBundleUpdater");

        foreach (var kvp in downloadedFiles)
        {
            string fileName = kvp.Key;
            string tempPath = kvp.Value;

            if (!File.Exists(tempPath))
            {
                Logger.Instance.LogError($"文件不存在: {fileName}", "AssetBundleUpdater");
                yield break;
            }

            // 计算文件哈希
            string actualHash = CalculateFileHash(tempPath);
            BundleFile fileInfo = needUpdateFiles.Find(f => f.fileName == fileName);

            if (fileInfo == null)
            {
                Logger.Instance.LogError($"找不到文件信息: {fileName}", "AssetBundleUpdater");
                yield break;
            }

            if (actualHash != fileInfo.hash)
            {
                Logger.Instance.LogError($"文件哈希不匹配: {fileName}, 预期: {fileInfo.hash}, 实际: {actualHash}", "AssetBundleUpdater");
                yield break;
            }

            Logger.Instance.LogInfo($"文件验证通过: {fileName}", "AssetBundleUpdater");
        }
    }

    /// <summary>
    /// 计算文件哈希(MD5)
    /// </summary>
    private string CalculateFileHash(string filePath)
    {
        using (MD5 md5 = MD5.Create())
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }

    #endregion

    #region Apply

    /// <summary>
    /// 应用更新
    /// </summary>
    private IEnumerator ApplyUpdateCoroutine()
    {
        Logger.Instance.LogInfo("应用更新...", "AssetBundleUpdater");

        try
        {
            // 备份旧文件
            Dictionary<string, string> backupFiles = new Dictionary<string, string>();
            foreach (var kvp in downloadedFiles)
            {
                string fileName = kvp.Key;
                string tempPath = kvp.Value;
                string finalPath = Path.Combine(localBundlePath, fileName);

                if (File.Exists(finalPath))
                {
                    string backupPath = finalPath + ".bak";
                    File.Copy(finalPath, backupPath, true);
                    backupFiles[fileName] = backupPath;
                }
            }

            // 替换文件
            foreach (var kvp in downloadedFiles)
            {
                string fileName = kvp.Key;
                string tempPath = kvp.Value;
                string finalPath = Path.Combine(localBundlePath, fileName);

                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }
                File.Move(tempPath, finalPath);
                Logger.Instance.LogInfo($"文件已更新: {fileName}", "AssetBundleUpdater");
            }

            // 更新版本信息
            localVersion = JsonUtility.FromJson<VersionManifest>(JsonUtility.ToJson(remoteVersion));
            SaveLocalVersion();

            // 清理临时文件和备份
            CleanupTempFiles();
            CleanupBackupFiles(backupFiles);
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"应用更新失败: {e.Message}", "AssetBundleUpdater");
        }

        yield return null;
    }

    /// <summary>
    /// 清理临时文件
    /// </summary>
    private void CleanupTempFiles()
    {
        if (Directory.Exists(localBundlePath))
        {
            string[] tempFiles = Directory.GetFiles(localBundlePath, $"*{tempFileExtension}", SearchOption.AllDirectories);
            foreach (string tempFile in tempFiles)
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception e)
                {
                    Logger.Instance.LogWarning($"删除临时文件失败: {tempFile}, {e.Message}", "AssetBundleUpdater");
                }
            }
        }
    }

    /// <summary>
    /// 清理备份文件
    /// </summary>
    private void CleanupBackupFiles(Dictionary<string, string> backupFiles)
    {
        foreach (var kvp in backupFiles)
        {
            try
            {
                if (File.Exists(kvp.Value))
                {
                    File.Delete(kvp.Value);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.LogWarning($"删除备份文件失败: {kvp.Value}, {e.Message}", "AssetBundleUpdater");
            }
        }
    }

    #endregion

    #region Utilities

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 取消更新
    /// </summary>
    public void CancelUpdate()
    {
        if (CurrentStatus == UpdateStatus.Downloading || CurrentStatus == UpdateStatus.Verifying)
        {
            Logger.Instance.LogInfo("更新已取消", "AssetBundleUpdater");
            CurrentStatus = UpdateStatus.Idle;
            DownloadProgress = 0f;
            CurrentDownloadFile = string.Empty;
        }
    }

    /// <summary>
    /// 清理所有临时文件
    /// </summary>
    public void ClearCache()
    {
        CleanupTempFiles();
        Logger.Instance.LogInfo("缓存已清理", "AssetBundleUpdater");
    }

    /// <summary>
    /// 获取当前版本
    /// </summary>
    public string GetLocalVersion()
    {
        return localVersion?.version ?? "1.0.0";
    }

    /// <summary>
    /// 获取远程版本
    /// </summary>
    public string GetRemoteVersion()
    {
        return remoteVersion?.version ?? "1.0.0";
    }

    #endregion
}
