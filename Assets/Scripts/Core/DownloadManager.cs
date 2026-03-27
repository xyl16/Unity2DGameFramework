using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 下载管理器: 负责统一管理所有下载任务
/// 支持并发下载、优先级队列、断点续传
/// </summary>
public class DownloadManager : MonoBehaviour
{
    private static DownloadManager instance;
    public static DownloadManager Instance { get { return instance; } }

    [Header("Download Settings")]
    [SerializeField] private int maxConcurrentDownloads = 3;
    [SerializeField] private int maxRetryCount = 3;
    [SerializeField] private float downloadTimeout = 30f;
    [SerializeField] private bool enableResume = true;
    [SerializeField] private string defaultDownloadPath = "Downloads";

    public enum DownloadPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    public enum DownloadStatus
    {
        Pending,
        Downloading,
        Completed,
        Failed,
        Paused,
        Cancelled
    }

    /// <summary>
    /// 下载任务
    /// </summary>
    public class DownloadTask
    {
        public string taskId;
        public string url;
        public string savePath;
        public long fileSize;
        public DownloadPriority priority;
        public DownloadStatus status;
        public float progress;
        public long downloadedBytes;
        public float downloadSpeed;

        public Action<bool, string> onComplete;
        public Action<float> onProgress;

        private UnityWebRequest webRequest;
        private string tempPath;

        public DownloadTask(string taskId, string url, string savePath, DownloadPriority priority = DownloadPriority.Normal)
        {
            this.taskId = taskId;
            this.url = url;
            this.savePath = savePath;
            this.priority = priority;
            this.status = DownloadStatus.Pending;
            this.progress = 0f;
            this.downloadedBytes = 0;
            this.downloadSpeed = 0f;
            this.tempPath = savePath + ".tmp";
        }

        public UnityWebRequest WebRequest
        {
            get { return webRequest; }
            set { webRequest = value; }
        }

        public string TempPath
        {
            get { return tempPath; }
        }
    }

    private PriorityQueue<DownloadTask> pendingTasks = new PriorityQueue<DownloadTask>();
    private Dictionary<string, DownloadTask> activeTasks = new Dictionary<string, DownloadTask>();
    private Dictionary<string, DownloadTask> allTasks = new Dictionary<string, DownloadTask>();
    private int activeDownloadCount = 0;

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

    private void Update()
    {
        UpdateDownloadSpeed();
        ProcessPendingTasks();
    }

    /// <summary>
    /// 添加下载任务
    /// </summary>
    public string AddDownloadTask(string url, string savePath, DownloadPriority priority = DownloadPriority.Normal,
        Action<bool, string> onComplete = null, Action<float> onProgress = null)
    {
        string taskId = Guid.NewGuid().ToString();
        DownloadTask task = new DownloadTask(taskId, url, savePath, priority);
        task.onComplete = onComplete;
        task.onProgress = onProgress;

        allTasks[taskId] = task;
        pendingTasks.Enqueue(task, (int)priority);

        Logger.Instance.LogInfo($"Download task added: {taskId}, URL: {url}", "DownloadManager");
        return taskId;
    }

    /// <summary>
    /// 处理待下载任务
    /// </summary>
    private void ProcessPendingTasks()
    {
        while (activeDownloadCount < maxConcurrentDownloads && pendingTasks.Count > 0)
        {
            DownloadTask task = pendingTasks.Dequeue();
            if (task.status == DownloadStatus.Cancelled)
                continue;

            StartDownload(task);
        }
    }

    /// <summary>
    /// 开始下载
    /// </summary>
    private void StartDownload(DownloadTask task)
    {
        task.status = DownloadStatus.Downloading;
        activeTasks[task.taskId] = task;
        activeDownloadCount++;

        StartCoroutine(DownloadCoroutine(task));
    }

    /// <summary>
    /// 下载协程
    /// </summary>
    private IEnumerator DownloadCoroutine(DownloadTask task)
    {
        string directory = Path.GetDirectoryName(task.savePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 检查是否有未完成的临时文件(断点续传)
        long existingLength = 0;
        if (enableResume && File.Exists(task.TempPath))
        {
            existingLength = new FileInfo(task.TempPath).Length;
            task.downloadedBytes = existingLength;
        }

        int retryCount = 0;
        bool success = false;

        while (retryCount < maxRetryCount && !success)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(task.url))
            {
                task.WebRequest = request;

                if (enableResume && existingLength > 0)
                {
                    request.SetRequestHeader("Range", $"bytes={existingLength}-");
                }

                request.timeout = (int)downloadTimeout;

                DownloadHandlerBuffer handler = new DownloadHandlerBuffer();
                request.downloadHandler = handler;

                // 记录开始时间用于计算下载速度
                float startTime = Time.time;
                long lastDownloadedBytes = task.downloadedBytes;

                yield return request.SendWebRequest();

                // 计算下载速度
                float elapsedTime = Time.time - startTime;
                if (elapsedTime > 0)
                {
                    task.downloadSpeed = (task.downloadedBytes - lastDownloadedBytes) / elapsedTime;
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    byte[] data = handler.data;

                    // 断点续传: 追加数据
                    if (enableResume && existingLength > 0 && request.responseCode == 206)
                    {
                        byte[] existingData = File.ReadAllBytes(task.TempPath);
                        byte[] combinedData = new byte[existingData.Length + data.Length];
                        Buffer.BlockCopy(existingData, 0, combinedData, 0, existingData.Length);
                        Buffer.BlockCopy(data, 0, combinedData, existingData.Length, data.Length);
                        data = combinedData;
                    }

                    // 写入临时文件
                    File.WriteAllBytes(task.TempPath, data);
                    task.downloadedBytes = data.Length;

                    // 重命名为最终文件
                    if (File.Exists(task.savePath))
                    {
                        File.Delete(task.savePath);
                    }
                    File.Move(task.TempPath, task.savePath);

                    task.status = DownloadStatus.Completed;
                    task.progress = 1f;
                    success = true;

                    Logger.Instance.LogInfo($"Download completed: {task.taskId}", "DownloadManager");
                }
                else
                {
                    retryCount++;
                    Logger.Instance.LogWarning($"Download failed: {task.taskId}, Retry: {retryCount}/{maxRetryCount}, Error: {request.error}", "DownloadManager");
                }
            }
        }

        // 完成或失败
        if (success)
        {
            task.onComplete?.Invoke(true, "下载成功");
        }
        else
        {
            task.status = DownloadStatus.Failed;
            task.onComplete?.Invoke(false, "下载失败, 已达到最大重试次数");
        }

        activeTasks.Remove(task.taskId);
        activeDownloadCount--;
    }

    /// <summary>
    /// 更新下载速度
    /// </summary>
    private void UpdateDownloadSpeed()
    {
        foreach (var task in activeTasks.Values)
        {
            if (task.WebRequest != null && task.WebRequest.downloadHandler != null)
            {
                long currentBytes = (long)task.WebRequest.downloadedBytes;
                if (task.status == DownloadStatus.Downloading)
                {
                    task.downloadedBytes = currentBytes;
                    task.progress = (float)currentBytes / task.fileSize;
                    task.onProgress?.Invoke(task.progress);
                }
            }
        }
    }

    /// <summary>
    /// 暂停下载任务
    /// </summary>
    public void PauseTask(string taskId)
    {
        if (allTasks.TryGetValue(taskId, out DownloadTask task))
        {
            if (task.status == DownloadStatus.Downloading)
            {
                if (task.WebRequest != null)
                {
                    task.WebRequest.Abort();
                }
                task.status = DownloadStatus.Paused;
                activeTasks.Remove(taskId);
                activeDownloadCount--;
                Logger.Instance.LogInfo($"Task paused: {taskId}", "DownloadManager");
            }
        }
    }

    /// <summary>
    /// 恢复下载任务
    /// </summary>
    public void ResumeTask(string taskId)
    {
        if (allTasks.TryGetValue(taskId, out DownloadTask task))
        {
            if (task.status == DownloadStatus.Paused)
            {
                task.status = DownloadStatus.Pending;
                pendingTasks.Enqueue(task, (int)task.priority);
                Logger.Instance.LogInfo($"Task resumed: {taskId}", "DownloadManager");
            }
        }
    }

    /// <summary>
    /// 取消下载任务
    /// </summary>
    public void CancelTask(string taskId)
    {
        if (allTasks.TryGetValue(taskId, out DownloadTask task))
        {
            if (task.WebRequest != null)
            {
                task.WebRequest.Abort();
            }

            // 删除临时文件
            if (File.Exists(task.TempPath))
            {
                File.Delete(task.TempPath);
            }

            task.status = DownloadStatus.Cancelled;
            task.onComplete?.Invoke(false, "下载已取消");

            if (activeTasks.ContainsKey(taskId))
            {
                activeTasks.Remove(taskId);
                activeDownloadCount--;
            }

            allTasks.Remove(taskId);
            Logger.Instance.LogInfo($"Task cancelled: {taskId}", "DownloadManager");
        }
    }

    /// <summary>
    /// 获取下载任务状态
    /// </summary>
    public DownloadStatus GetTaskStatus(string taskId)
    {
        if (allTasks.TryGetValue(taskId, out DownloadTask task))
        {
            return task.status;
        }
        return DownloadStatus.Failed;
    }

    /// <summary>
    /// 获取下载进度
    /// </summary>
    public float GetTaskProgress(string taskId)
    {
        if (allTasks.TryGetValue(taskId, out DownloadTask task))
        {
            return task.progress;
        }
        return 0f;
    }

    /// <summary>
    /// 清理所有临时文件
    /// </summary>
    public void CleanTempFiles()
    {
        if (Directory.Exists(defaultDownloadPath))
        {
            string[] tempFiles = Directory.GetFiles(defaultDownloadPath, "*.tmp", SearchOption.AllDirectories);
            foreach (string tempFile in tempFiles)
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception e)
                {
                    Logger.Instance.LogWarning($"删除临时文件失败: {tempFile}, {e.Message}", "DownloadManager");
                }
            }
        }
        Logger.Instance.LogInfo("临时文件已清理", "DownloadManager");
    }

    /// <summary>
    /// 清理所有下载任务
    /// </summary>
    public void ClearAllTasks()
    {
        foreach (var task in activeTasks.Values)
        {
            if (task.WebRequest != null)
            {
                task.WebRequest.Abort();
            }

            if (File.Exists(task.TempPath))
            {
                File.Delete(task.TempPath);
            }
        }

        activeTasks.Clear();
        pendingTasks.Clear();
        allTasks.Clear();
        activeDownloadCount = 0;

        Logger.Instance.LogInfo("所有下载任务已清理", "DownloadManager");
    }

    private void OnDestroy()
    {
        ClearAllTasks();
    }

    #region PriorityQueue

    /// <summary>
    /// 优先级队列实现
    /// </summary>
    private class PriorityQueue<T>
    {
        private List<(T item, int priority)> elements = new List<(T, int)>();

        public int Count => elements.Count;

        public void Enqueue(T item, int priority)
        {
            elements.Add((item, priority));
            int childIndex = elements.Count - 1;
            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2;
                if (elements[childIndex].Item2 <= elements[parentIndex].Item2)
                    break;

                var temp = elements[childIndex];
                elements[childIndex] = elements[parentIndex];
                elements[parentIndex] = temp;
                childIndex = parentIndex;
            }
        }

        public T Dequeue()
        {
            if (elements.Count == 0)
                return default(T);

            int lastIndex = elements.Count - 1;
            var frontItem = elements[0];
            elements[0] = elements[lastIndex];
            elements.RemoveAt(lastIndex);

            lastIndex--;
            int parentIndex = 0;
            while (true)
            {
                int leftChildIndex = parentIndex * 2 + 1;
                if (leftChildIndex > lastIndex)
                    break;

                int rightChildIndex = leftChildIndex + 1;
                if (rightChildIndex <= lastIndex && elements[rightChildIndex].Item2 > elements[leftChildIndex].Item2)
                    leftChildIndex = rightChildIndex;

                if (elements[parentIndex].Item2 >= elements[leftChildIndex].Item2)
                    break;

                var temp = elements[parentIndex];
                elements[parentIndex] = elements[leftChildIndex];
                elements[leftChildIndex] = temp;
                parentIndex = leftChildIndex;
            }

            return frontItem.Item1;
        }

        public void Clear()
        {
            elements.Clear();
        }
    }

    #endregion
}
