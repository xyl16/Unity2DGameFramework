using System.Collections;
using UnityEngine;

/// <summary>
/// 场景管理器：负责游戏场景的加载、卸载和切换
/// 支持异步加载和完成回调
/// </summary>
public class SceneManager : MonoBehaviour
{
    /// <summary>
    /// 场景管理器的单例实例
    /// </summary>
    private static SceneManager instance;
    
    /// <summary>
    /// 获取场景管理器实例
    /// </summary>
    public static SceneManager Instance { get { return instance; } }

    /// <summary>
    /// Awake 方法：确保场景管理器为单例模式
    /// </summary>
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

    /// <summary>
    /// 当前场景名称
    /// </summary>
    public string currentScene { get; private set; }

    /// <summary>
    /// 加载场景（单一模式，会卸载当前场景）
    /// </summary>
    /// <param name="sceneName">要加载的场景名称</param>
    /// <param name="onComplete">加载完成后的回调函数</param>
    public void LoadScene(string sceneName, System.Action onComplete = null)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName, onComplete));
    }

    /// <summary>
    /// 异步加载场景的协程
    /// </summary>
    private IEnumerator LoadSceneCoroutine(string sceneName, System.Action onComplete)
    {
        // 开始异步加载场景
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

        // 等待加载完成
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 更新当前场景名称
        currentScene = sceneName;
        
        // 触发场景加载完成事件
        EventManager.Instance.InvokeEvent("SceneLoaded", sceneName);
        
        // 执行完成回调
        onComplete?.Invoke();
    }

    /// <summary>
    /// 加载场景（附加模式，保留当前场景）
    /// 用于同时加载多个场景的场景组合
    /// </summary>
    /// <param name="sceneName">要加载的场景名称</param>
    /// <param name="onComplete">加载完成后的回调函数</param>
    public void LoadSceneAdditive(string sceneName, System.Action onComplete = null)
    {
        StartCoroutine(LoadSceneAdditiveCoroutine(sceneName, onComplete));
    }

    /// <summary>
    /// 异步附加加载场景的协程
    /// </summary>
    private IEnumerator LoadSceneAdditiveCoroutine(string sceneName, System.Action onComplete)
    {
        // 使用 Additive 模式异步加载场景
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);

        // 等待加载完成
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 触发场景加载完成事件
        EventManager.Instance.InvokeEvent("SceneLoaded", sceneName);
        
        // 执行完成回调
        onComplete?.Invoke();
    }

    /// <summary>
    /// 卸载场景
    /// </summary>
    /// <param name="sceneName">要卸载的场景名称</param>
    /// <param name="onComplete">卸载完成后的回调函数</param>
    public void UnloadScene(string sceneName, System.Action onComplete = null)
    {
        StartCoroutine(UnloadSceneCoroutine(sceneName, onComplete));
    }

    /// <summary>
    /// 异步卸载场景的协程
    /// </summary>
    private IEnumerator UnloadSceneCoroutine(string sceneName, System.Action onComplete)
    {
        // 开始异步卸载场景
        AsyncOperation asyncUnload = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);

        // 等待卸载完成
        while (!asyncUnload.isDone)
        {
            yield return null;
        }

        // 触发场景卸载完成事件
        EventManager.Instance.InvokeEvent("SceneUnloaded", sceneName);
        
        // 执行完成回调
        onComplete?.Invoke();
    }
}
