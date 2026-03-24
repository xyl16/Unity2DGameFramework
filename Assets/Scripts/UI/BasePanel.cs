using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// UI 面板基类，所有面板继承此类
/// 提供面板生命周期管理、事件注册和定时器管理功能
/// </summary>
public class BasePanel : MonoBehaviour
{
    /// <summary>
    /// 面板名称，用于标识和管理
    /// </summary>
    public string PanelName { get; set; }
    
    /// <summary>
    /// 面板是否已打开
    /// </summary>
    public bool IsOpen { get; internal set; }

    /// <summary>
    /// 已注册的事件键列表，用于面板关闭时自动注销
    /// </summary>
    protected List<string> _eventKeys = new List<string>();
    
    /// <summary>
    /// 已创建的定时器列表，用于面板关闭时自动清除
    /// </summary>
    protected List<Timer> _timers = new List<Timer>();

    /// <summary>
    /// 打开面板
    /// </summary>
    /// <param name="args">传递给面板的参数对象，可为 null</param>
    public virtual void OnOpen(object args = null)
    {
        gameObject.SetActive(true);
        IsOpen = true;
    }

    /// <summary>
    /// 关闭面板
    /// </summary>
    public virtual void OnClose()
    {
        Cleanup();
        gameObject.SetActive(false);
        IsOpen = false;
    }

    /// <summary>
    /// 销毁面板
    /// 清理所有资源后销毁对象
    /// </summary>
    public virtual void OnDestroyPanel()
    {
        Cleanup();
        IsOpen = false;
    }

    /// <summary>
    /// 清理资源：注销事件、清除定时器
    /// 在面板关闭或销毁时调用
    /// </summary>
    protected virtual void Cleanup()
    {
        // 清除所有定时器
        ClearTimers();

        // 注销所有事件
        UnregisterAllEvents();
    }

    /// <summary>
    /// 注册事件（自动追踪）
    /// 注册的事件会在面板关闭时自动注销
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="callback">事件回调函数</param>
    protected void RegisterEvent(string eventName, Action<object> callback)
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener(eventName, callback);
            // 将事件键加入追踪列表，以便后续自动注销
            if (!_eventKeys.Contains(eventName))
            {
                _eventKeys.Add(eventName);
            }
        }
    }

    /// <summary>
    /// 注册事件（无参数版本）
    /// 自动追踪，面板关闭时自动注销
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="callback">无参数的事件回调函数</param>
    protected void RegisterEvent(string eventName, Action callback)
    {
        RegisterEvent(eventName, (obj) => callback?.Invoke());
    }

    /// <summary>
    /// 注销所有本面板注册的事件
    /// </summary>
    protected void UnregisterAllEvents()
    {
        if (EventManager.Instance == null) return;

        foreach (var key in _eventKeys)
        {
            EventManager.Instance.RemoveListener(key, null);
        }
        _eventKeys.Clear();
    }

    /// <summary>
    /// 启动定时器（自动追踪）
    /// 创建的定时器会在面板关闭时自动清除
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="callback">定时器触发时的回调函数</param>
    /// <param name="loop">是否循环触发，默认为 false</param>
    /// <returns>创建的定时器对象</returns>
    protected Timer StartTimer(float delay, Action callback, bool loop = false)
    {
        if (TimerManager.Instance == null) return null;

        Timer timer = TimerManager.Instance.CreateTimer(delay, callback, loop);
        if (timer != null && !_timers.Contains(timer))
        {
            _timers.Add(timer);
        }
        return timer;
    }

    /// <summary>
    /// 停止指定的定时器
    /// </summary>
    /// <param name="timer">要停止的定时器对象</param>
    protected void StopTimer(Timer timer)
    {
        if (TimerManager.Instance != null && timer != null)
        {
            TimerManager.Instance.CancelTimer(timer);
            _timers.Remove(timer);
        }
    }

    /// <summary>
    /// 清除所有本面板创建的定时器
    /// </summary>
    protected void ClearTimers()
    {
        if (TimerManager.Instance == null) return;

        foreach (var timer in _timers)
        {
            TimerManager.Instance.CancelTimer(timer);
        }
        _timers.Clear();
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="eventName">要触发的事件名称</param>
    /// <param name="args">传递给监听器的参数，可为 null</param>
    protected void InvokeEvent(string eventName, object args = null)
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.InvokeEvent(eventName, args);
        }
    }
}
