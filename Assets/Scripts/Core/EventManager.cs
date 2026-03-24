using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件管理器：负责事件系统的核心功能
/// 提供事件的注册、移除、触发等功能，实现模块间的解耦通信
/// </summary>
public class EventManager : MonoBehaviour
{
    /// <summary>
    /// 事件管理器的单例实例
    /// </summary>
    private static EventManager instance;
    
    /// <summary>
    /// 获取事件管理器实例
    /// </summary>
    public static EventManager Instance { get { return instance; } }

    /// <summary>
    /// Awake 方法：确保事件管理器为单例模式
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
    /// 事件字典：存储事件名称与对应的监听器列表
    /// Key: 事件名称（字符串）
    /// Value: 监听该事件的所有回调函数列表
    /// </summary>
    private Dictionary<string, List<Action<object>>> eventDictionary = new Dictionary<string, List<Action<object>>>();

    /// <summary>
    /// 添加事件监听器
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="listener">事件回调函数，接收一个 object 类型的参数</param>
    public void AddListener(string eventName, Action<object> listener)
    {
        if (eventDictionary.TryGetValue(eventName, out List<Action<object>> listenerList))
        {
            // 事件已存在，直接添加监听器
            listenerList.Add(listener);
        }
        else
        {
            // 事件不存在，创建新的事件列表
            listenerList = new List<Action<object>> { listener };
            eventDictionary[eventName] = listenerList;
        }
    }

    /// <summary>
    /// 移除事件监听器
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="listener">要移除的事件回调函数</param>
    public void RemoveListener(string eventName, Action<object> listener)
    {
        if (eventDictionary.TryGetValue(eventName, out List<Action<object>> listenerList))
        {
            // 移除指定的监听器
            listenerList.Remove(listener);
            
            // 如果该事件没有监听器了，则从字典中移除该事件
            if (listenerList.Count == 0)
            {
                eventDictionary.Remove(eventName);
            }
        }
    }

    /// <summary>
    /// 触发事件
    /// 通知所有监听该事件的回调函数
    /// </summary>
    /// <param name="eventName">要触发的事件名称</param>
    /// <param name="data">传递给监听器的数据，可为 null</param>
    public void InvokeEvent(string eventName, object data = null)
    {
        if (eventDictionary.TryGetValue(eventName, out List<Action<object>> listenerList))
        {
            // 从后向前遍历，防止在回调中移除监听器导致索引问题
            for (int i = listenerList.Count - 1; i >= 0; i--)
            {
                listenerList[i]?.Invoke(data);
            }
        }
    }

    /// <summary>
    /// 清除指定事件的所有监听器
    /// </summary>
    /// <param name="eventName">要清除的事件名称</param>
    public void ClearEvent(string eventName)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary.Remove(eventName);
        }
    }

    /// <summary>
    /// 清除所有事件
    /// 释放整个事件字典
    /// </summary>
    public void ClearAllEvents()
    {
        eventDictionary.Clear();
    }
}
