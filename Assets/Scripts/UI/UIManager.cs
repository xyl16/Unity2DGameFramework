using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// UI 管理器，统一管理所有 UI 界面的加载、打开、关闭
/// 支持面板预加载、缓存和从多种资源源加载
/// 需要手动挂载在场景的 Managers 节点下
/// </summary>
public class UIManager : MonoBehaviour
{
    /// <summary>
    /// UI 管理器的单例实例
    /// </summary>
    private static UIManager instance;

    /// <summary>
    /// 获取 UI 管理器实例
    /// </summary>
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIManager>();
                if (instance == null)
                {
                    Logger.Instance.LogError("UIManager not found in scene! Please attach it to the Managers GameObject.", "UIManager");
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// 已加载的面板字典（包括已打开和已关闭但仍在内存中的）
    /// Key: 面板名称
    /// Value: 面板的 GameObject 对象
    /// </summary>
    private Dictionary<string, GameObject> _loadedPanels = new Dictionary<string, GameObject>();

    /// <summary>
    /// 已打开的面板字典
    /// Key: 面板名称
    /// Value: 面板的 GameObject 对象
    /// </summary>
    private Dictionary<string, GameObject> _openedPanels = new Dictionary<string, GameObject>();

    /// <summary>
    /// UI 根节点，所有 UI 面板都将作为其子对象
    /// </summary>
    private Transform _uiRoot;

    /// <summary>
    /// Awake 方法：初始化单例，创建 UI 根节点
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            CreateUIRoot();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 创建 UI 根节点对象
    /// 所有 UI 面板将挂载到此节点下
    /// </summary>
    private void CreateUIRoot()
    {
        _uiRoot = new GameObject("UIRoot").transform;
        _uiRoot.SetParent(transform);
        _uiRoot.localPosition = Vector3.zero;
        _uiRoot.localScale = Vector3.one;
        _uiRoot.gameObject.layer = LayerMask.NameToLayer("UI");
    }

    /// <summary>
    /// 打开 UI 面板
    /// 支持从缓存、AssetBundle 或 Resources 加载
    /// </summary>
    /// <param name="panelName">面板名称</param>
    /// <param name="args">传递给面板的参数对象，可为 null</param>
    /// <returns>打开的面板 GameObject，失败返回 null</returns>
    public GameObject OpenPanel(string panelName, object args = null)
    {
        // 如果已经打开，直接返回并重新调用 OnOpen
        if (_openedPanels.TryGetValue(panelName, out GameObject openedPanel))
        {
            BasePanel panel = openedPanel.GetComponent<BasePanel>();
            if (panel != null && panel.IsOpen)
            {
                Logger.Instance.LogInfo($"[UIManager] Panel '{panelName}' is already opened.", "UIManager");
                panel.OnOpen(args);
                return openedPanel;
            }
            else
            {
                openedPanel.SetActive(true);
                if (panel != null)
                {
                    panel.IsOpen = true;
                    panel.OnOpen(args);
                }
                return openedPanel;
            }
        }

        // 如果已经加载但未打开，从缓存激活
        if (_loadedPanels.TryGetValue(panelName, out GameObject loadedPanel))
        {
            loadedPanel.SetActive(true);
            BasePanel panel = loadedPanel.GetComponent<BasePanel>();
            if (panel != null)
            {
                panel.IsOpen = true;
                panel.OnOpen(args);
            }
            _openedPanels[panelName] = loadedPanel;
            Logger.Instance.LogInfo($"[UIManager] Panel '{panelName}' opened from cache.", "UIManager");
            return loadedPanel;
        }

        // 尝试从 AssetBundle 加载
        GameObject panelPrefab = ResourceManager.Instance.LoadAsset<GameObject>($"UI/{panelName}");
        if (panelPrefab != null)
        {
            GameObject panelObj = Instantiate(panelPrefab, _uiRoot);
            panelObj.name = panelName;
            BasePanel panel = panelObj.GetComponent<BasePanel>();
            if (panel == null)
            {
                panel = panelObj.AddComponent<BasePanel>();
            }
            panel.PanelName = panelName;
            panel.OnOpen(args);

            _loadedPanels[panelName] = panelObj;
            _openedPanels[panelName] = panelObj;

            Logger.Instance.LogInfo($"[UIManager] Panel '{panelName}' loaded from AssetBundle and opened.", "UIManager");
            return panelObj;
        }

        // 尝试从 Resources 加载
        panelPrefab = Resources.Load<GameObject>($"UI/{panelName}");
        if (panelPrefab != null)
        {
            GameObject panelObj = Instantiate(panelPrefab, _uiRoot);
            panelObj.name = panelName;
            BasePanel panel = panelObj.GetComponent<BasePanel>();
            if (panel == null)
            {
                panel = panelObj.AddComponent<BasePanel>();
            }
            panel.PanelName = panelName;
            panel.OnOpen(args);

            _loadedPanels[panelName] = panelObj;
            _openedPanels[panelName] = panelObj;

            Logger.Instance.LogInfo($"[UIManager] Panel '{panelName}' loaded from Resources and opened.", "UIManager");
            return panelObj;
        }

        Logger.Instance.LogError($"[UIManager] Panel '{panelName}' not found in AssetBundle or Resources!", "UIManager");
        return null;
    }

    /// <summary>
    /// 关闭 UI 面板
    /// </summary>
    /// <param name="panelName">要关闭的面板名称</param>
    public void ClosePanel(string panelName)
    {
        if (_openedPanels.TryGetValue(panelName, out GameObject panelObj))
        {
            BasePanel panel = panelObj.GetComponent<BasePanel>();
            if (panel != null)
            {
                panel.OnClose();
            }
            else
            {
                panelObj.SetActive(false);
            }
            _openedPanels.Remove(panelName);
            Logger.Instance.LogInfo($"[UIManager] Panel '{panelName}' closed.", "UIManager");
        }
        else
        {
            Logger.Instance.LogWarning($"[UIManager] Panel '{panelName}' is not opened.", "UIManager");
        }
    }

    /// <summary>
    /// 关闭所有打开的面板
    /// </summary>
    public void CloseAllPanels()
    {
        var panelNames = new List<string>(_openedPanels.Keys);
        foreach (var name in panelNames)
        {
            ClosePanel(name);
        }
        Logger.Instance.LogInfo("[UIManager] All panels closed.", "UIManager");
    }

    /// <summary>
    /// 销毁面板（从内存中移除）
    /// </summary>
    /// <param name="panelName">要销毁的面板名称</param>
    public void DestroyPanel(string panelName)
    {
        ClosePanel(panelName);

        if (_loadedPanels.TryGetValue(panelName, out GameObject panelObj))
        {
            BasePanel panel = panelObj.GetComponent<BasePanel>();
            if (panel != null)
            {
                panel.OnDestroyPanel();
            }
            Destroy(panelObj);
            _loadedPanels.Remove(panelName);
            Logger.Instance.LogInfo($"[UIManager] Panel '{panelName}' destroyed.", "UIManager");
        }
    }

    /// <summary>
    /// 销毁所有面板
    /// </summary>
    public void DestroyAllPanels()
    {
        var panelNames = new List<string>(_loadedPanels.Keys);
        foreach (var name in panelNames)
        {
            DestroyPanel(name);
        }
        Logger.Instance.LogInfo("[UIManager] All panels destroyed.", "UIManager");
    }

    /// <summary>
    /// 预加载面板（不打开）
    /// 面板加载后保持在内存中，但不显示，可用于提高后续打开速度
    /// </summary>
    /// <param name="panelName">要预加载的面板名称</param>
    /// <returns>预加载的面板 GameObject，失败返回 null</returns>
    public GameObject PreloadPanel(string panelName)
    {
        if (_loadedPanels.ContainsKey(panelName))
        {
            Logger.Instance.LogInfo($"[UIManager] Panel '{panelName}' is already loaded.", "UIManager");
            return _loadedPanels[panelName];
        }

        // 尝试从 AssetBundle 加载
        GameObject panelPrefab = ResourceManager.Instance.LoadAsset<GameObject>($"UI/{panelName}");
        if (panelPrefab != null)
        {
            GameObject panelObj = Instantiate(panelPrefab, _uiRoot);
            panelObj.name = panelName;
            panelObj.SetActive(false); // 不显示

            BasePanel panel = panelObj.GetComponent<BasePanel>();
            if (panel == null)
            {
                panel = panelObj.AddComponent<BasePanel>();
            }
            panel.PanelName = panelName;
            panel.IsOpen = false;

            _loadedPanels[panelName] = panelObj;
            Logger.Instance.LogInfo($"[UIManager] Panel '{panelName}' preloaded from AssetBundle.", "UIManager");
            return panelObj;
        }

        // 尝试从 Resources 加载
        panelPrefab = Resources.Load<GameObject>($"UI/{panelName}");
        if (panelPrefab != null)
        {
            GameObject panelObj = Instantiate(panelPrefab, _uiRoot);
            panelObj.name = panelName;
            panelObj.SetActive(false); // 不显示

            BasePanel panel = panelObj.GetComponent<BasePanel>();
            if (panel == null)
            {
                panel = panelObj.AddComponent<BasePanel>();
            }
            panel.PanelName = panelName;
            panel.IsOpen = false;

            _loadedPanels[panelName] = panelObj;
            Logger.Instance.LogInfo($"[UIManager] Panel '{panelName}' preloaded from Resources.", "UIManager");
            return panelObj;
        }

        Logger.Instance.LogError($"[UIManager] Panel '{panelName}' not found for preloading!", "UIManager");
        return null;
    }

    /// <summary>
    /// 获取已打开的面板组件
    /// </summary>
    /// <typeparam name="T">面板组件类型，必须继承自 BasePanel</typeparam>
    /// <param name="panelName">面板名称</param>
    /// <returns>面板组件对象，未找到返回 null</returns>
    public T GetOpenedPanel<T>(string panelName) where T : BasePanel
    {
        if (_openedPanels.TryGetValue(panelName, out GameObject panelObj))
        {
            return panelObj.GetComponent<T>();
        }
        return null;
    }

    /// <summary>
    /// 获取已加载的面板组件（无论是否打开）
    /// </summary>
    /// <typeparam name="T">面板组件类型，必须继承自 BasePanel</typeparam>
    /// <param name="panelName">面板名称</param>
    /// <returns>面板组件对象，未找到返回 null</returns>
    public T GetLoadedPanel<T>(string panelName) where T : BasePanel
    {
        if (_loadedPanels.TryGetValue(panelName, out GameObject panelObj))
        {
            return panelObj.GetComponent<T>();
        }
        return null;
    }

    /// <summary>
    /// 检查面板是否已打开
    /// </summary>
    /// <param name="panelName">面板名称</param>
    /// <returns>已打开返回 true，否则返回 false</returns>
    public bool IsPanelOpened(string panelName)
    {
        return _openedPanels.ContainsKey(panelName);
    }

    /// <summary>
    /// 检查面板是否已加载
    /// </summary>
    /// <param name="panelName">面板名称</param>
    /// <returns>已加载返回 true，否则返回 false</returns>
    public bool IsPanelLoaded(string panelName)
    {
        return _loadedPanels.ContainsKey(panelName);
    }

    /// <summary>
    /// OnDestroy 方法：销毁所有面板和 UI 根节点
    /// </summary>
    private void OnDestroy()
    {
        DestroyAllPanels();
        if (_uiRoot != null)
        {
            Destroy(_uiRoot.gameObject);
        }
    }
}
