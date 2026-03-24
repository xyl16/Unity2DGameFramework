using System.Collections.Generic;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    private static TimerManager instance;
    public static TimerManager Instance { get { return instance; } }

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

    private List<Timer> activeTimers = new List<Timer>();
    private List<Timer> timersToRemove = new List<Timer>();

    private void Update()
    {
        for (int i = 0; i < activeTimers.Count; i++)
        {
            Timer timer = activeTimers[i];

            if (!timer.isPaused)
            {
                timer.elapsedTime += Time.deltaTime;

                if (timer.elapsedTime >= timer.duration)
                {
                    timer.onComplete?.Invoke();

                    if (timer.isLooping)
                    {
                        timer.elapsedTime = 0f;
                    }
                    else
                    {
                        timersToRemove.Add(timer);
                    }
                }
            }
        }

        for (int i = 0; i < timersToRemove.Count; i++)
        {
            activeTimers.Remove(timersToRemove[i]);
        }

        timersToRemove.Clear();
    }

    public Timer CreateTimer(float duration, System.Action onComplete, bool looping = false)
    {
        Timer timer = new Timer
        {
            duration = duration,
            elapsedTime = 0f,
            onComplete = onComplete,
            isLooping = looping,
            isPaused = false
        };

        activeTimers.Add(timer);
        return timer;
    }

    public void CancelTimer(Timer timer)
    {
        if (timer != null && activeTimers.Contains(timer))
        {
            activeTimers.Remove(timer);
        }
    }

    public void PauseTimer(Timer timer)
    {
        if (timer != null)
        {
            timer.isPaused = true;
        }
    }

    public void ResumeTimer(Timer timer)
    {
        if (timer != null)
        {
            timer.isPaused = false;
        }
    }

    public void CancelAllTimers()
    {
        activeTimers.Clear();
    }
}

public class Timer
{
    public float duration;
    public float elapsedTime;
    public System.Action onComplete;
    public bool isLooping;
    public bool isPaused;

    public float Progress
    {
        get { return duration > 0 ? elapsedTime / duration : 0f; }
    }

    public bool IsCompleted
    {
        get { return elapsedTime >= duration; }
    }
}
