using UnityEngine;

/// <summary>
/// 泛型单例基类，用于确保 MonoBehaviour 子类只有一个实例
/// 支持线程安全的延迟初始化和自动销毁检测
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    /// <summary>
    /// 单例实例的静态引用
    /// </summary>
    private static T instance;
    
    /// <summary>
    /// 线程锁对象，用于保证多线程环境下的线程安全
    /// </summary>
    private static readonly object lockObject = new object();
    
    /// <summary>
    /// 应用程序是否正在退出的标记
    /// 防止在应用程序退出时访问已销毁的单例实例
    /// </summary>
    private static bool applicationIsQuitting = false;

    /// <summary>
    /// 获取单例实例
    /// 使用惰性初始化，第一次访问时才创建实例
    /// </summary>
    public static T Instance
    {
        get
        {
            // 应用退出时返回null，避免访问已销毁的对象
            if (applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance of {typeof(T)} already destroyed on application quit. Won't create again - returning null.");
                return null;
            }

            // 使用锁确保线程安全
            lock (lockObject)
            {
                if (instance == null)
                {
                    // 尝试在场景中查找已存在的实例
                    instance = (T)FindObjectOfType(typeof(T));

                    // 检查是否存在多个单例实例（错误情况）
                    if (FindObjectsOfType(typeof(T)).Length > 1)
                    {
                        Debug.LogError($"[Singleton] Something went really wrong - there should never be more than 1 singleton! Reopening the scene might fix it.");
                        return instance;
                    }

                    // 如果找不到实例，则自动创建
                    if (instance == null)
                    {
                        GameObject singleton = new GameObject();
                        instance = singleton.AddComponent<T>();
                        singleton.name = $"(singleton) {typeof(T).ToString()}";
                    }
                }

                return instance;
            }
        }
    }

    /// <summary>
    /// Awake 方法：处理单例初始化
    /// 如果已存在实例则销毁当前对象，确保单例唯一性
    /// </summary>
    protected virtual void Awake()
    {
        if (instance == null)
        {
            // 首次初始化，保存当前实例引用
            instance = this as T;
        }
        else if (instance != this)
        {
            // 已存在其他实例，销毁重复对象
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// OnDestroy 方法：标记应用程序正在退出
    /// 防止在退出后访问已销毁的单例
    /// </summary>
    protected virtual void OnDestroy()
    {
        applicationIsQuitting = true;
    }
}
