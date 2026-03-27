using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 更新视图: 处理更新界面的显示和交互
/// </summary>
public class UpdateView : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject updatePanel;
    [SerializeField] private Text currentVersionText;
    [SerializeField] private Text remoteVersionText;
    [SerializeField] private Text updateMessageText;
    [SerializeField] private Text downloadProgressText;
    [SerializeField] private Text currentFileText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Button updateButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button closeButton;

    [Header("Settings")]
    [SerializeField] private bool showProgressPercentage = true;
    // [SerializeField] private bool showFileSize = true;

    private void Start()
    {
        RegisterEvents();
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (updatePanel != null)
        {
            updatePanel.SetActive(false);
        }

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        SetupButtonListeners();
    }

    private void SetupButtonListeners()
    {
        if (updateButton != null)
        {
            updateButton.onClick.AddListener(OnUpdateButtonClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    private void RegisterEvents()
    {
        EventManager.Instance.AddListener("UpdateAvailable", OnUpdateAvailable);
        EventManager.Instance.AddListener("UpdateCompleted", OnUpdateCompleted);
        EventManager.Instance.AddListener("UpdateFailed", OnUpdateFailed);
        EventManager.Instance.AddListener("UpdateCheckFailed", OnUpdateCheckFailed);
        EventManager.Instance.AddListener("NoUpdate", OnNoUpdate);
    }

    #region Event Handlers

    private void OnUpdateAvailable(object data)
    {
        UpdateController.UpdateInfo info = data as UpdateController.UpdateInfo;
        if (info != null)
        {
            ShowUpdatePanel(info);
        }
    }

    private void OnUpdateCompleted(object data)
    {
        string message = data as string;
        ShowCompletionPanel(message);
    }

    private void OnUpdateFailed(object data)
    {
        string message = data as string;
        ShowErrorPanel(message);
    }

    private void OnUpdateCheckFailed(object data)
    {
        string message = data as string;
        ShowErrorPanel($"检查更新失败: {message}");
    }

    private void OnNoUpdate(object data)
    {
        if (updatePanel != null && updatePanel.activeSelf)
        {
            ShowNoUpdatePanel();
        }
    }

    #endregion

    #region UI Display

    private void ShowUpdatePanel(UpdateController.UpdateInfo info)
    {
        if (updatePanel == null)
        {
            Logger.Instance.LogWarning("UpdatePanel not assigned!", "UpdateView");
            return;
        }

        updatePanel.SetActive(true);

        if (currentVersionText != null)
        {
            currentVersionText.text = $"当前版本: {info.currentVersion}";
        }

        if (remoteVersionText != null)
        {
            remoteVersionText.text = $"最新版本: {info.remoteVersion}";
        }

        if (updateMessageText != null)
        {
            updateMessageText.text = info.message;
        }

        if (downloadProgressText != null)
        {
            downloadProgressText.text = "等待开始下载...";
        }

        if (currentFileText != null)
        {
            currentFileText.text = "";
        }

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        ShowUpdateButton(true);
        ShowCancelButton(false);
        ShowCloseButton(true);

        // 开始监听下载进度
        StopAllCoroutines();
        StartCoroutine(UpdateProgressCoroutine());
    }

    private void ShowCompletionPanel(string message)
    {
        if (updatePanel != null)
        {
            updatePanel.SetActive(true);
        }

        if (updateMessageText != null)
        {
            updateMessageText.text = message;
        }

        if (downloadProgressText != null)
        {
            downloadProgressText.text = "更新完成!";
        }

        if (progressBar != null)
        {
            progressBar.value = 1f;
        }

        ShowUpdateButton(false);
        ShowCancelButton(false);
        ShowCloseButton(true);
    }

    private void ShowErrorPanel(string message)
    {
        if (updatePanel != null)
        {
            updatePanel.SetActive(true);
        }

        if (updateMessageText != null)
        {
            updateMessageText.text = message;
            updateMessageText.color = Color.red;
        }

        if (downloadProgressText != null)
        {
            downloadProgressText.text = "更新失败!";
        }

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        ShowUpdateButton(true);
        ShowCancelButton(false);
        ShowCloseButton(true);
    }

    private void ShowNoUpdatePanel()
    {
        if (updatePanel != null)
        {
            updatePanel.SetActive(true);
        }

        if (updateMessageText != null)
        {
            updateMessageText.text = "当前已是最新版本";
            updateMessageText.color = Color.green;
        }

        if (downloadProgressText != null)
        {
            downloadProgressText.text = "";
        }

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
        }

        ShowUpdateButton(false);
        ShowCancelButton(false);
        ShowCloseButton(true);
    }

    #endregion

    #region Progress Update

    private System.Collections.IEnumerator UpdateProgressCoroutine()
    {
        while (UpdateController.Instance.GetState() == UpdateController.UpdateState.Downloading)
        {
            float progress = UpdateController.Instance.GetDownloadProgress();
            string currentFile = UpdateController.Instance.GetCurrentDownloadFile();

            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (downloadProgressText != null)
            {
                string progressText = showProgressPercentage ? $"{progress * 100:F1}%" : "";
                downloadProgressText.text = $"下载进度: {progressText}";
            }

            if (currentFileText != null)
            {
                currentFileText.text = $"当前文件: {currentFile}";
            }

            ShowUpdateButton(false);
            ShowCancelButton(true);
            ShowCloseButton(false);

            yield return new WaitForSeconds(0.1f);
        }
    }

    #endregion

    #region Button Actions

    private void OnUpdateButtonClicked()
    {
        EventManager.Instance.InvokeEvent("UpdateRequested", null);
    }

    private void OnCancelButtonClicked()
    {
        EventManager.Instance.InvokeEvent("UpdateCanceled", null);
    }

    private void OnCloseButtonClicked()
    {
        if (updatePanel != null)
        {
            updatePanel.SetActive(false);
        }

        // 重置消息文本颜色
        if (updateMessageText != null)
        {
            updateMessageText.color = Color.white;
        }

        // 显示进度条
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
        }
    }

    #endregion

    #region UI Helpers

    private void ShowUpdateButton(bool show)
    {
        if (updateButton != null)
        {
            updateButton.gameObject.SetActive(show);
        }
    }

    private void ShowCancelButton(bool show)
    {
        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(show);
        }
    }

    private void ShowCloseButton(bool show)
    {
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(show);
        }
    }

    #endregion

    private void OnDestroy()
    {
        EventManager.Instance.RemoveListener("UpdateAvailable", OnUpdateAvailable);
        EventManager.Instance.RemoveListener("UpdateCompleted", OnUpdateCompleted);
        EventManager.Instance.RemoveListener("UpdateFailed", OnUpdateFailed);
        EventManager.Instance.RemoveListener("UpdateCheckFailed", OnUpdateCheckFailed);
        EventManager.Instance.RemoveListener("NoUpdate", OnNoUpdate);
    }
}
