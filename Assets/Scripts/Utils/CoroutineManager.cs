using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineManager : MonoBehaviour
{
    private static CoroutineManager instance;
    public static CoroutineManager Instance { get { return instance; } }

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

    private Dictionary<string, Coroutine> namedCoroutines = new Dictionary<string, Coroutine>();

    public new Coroutine StartCoroutine(IEnumerator routine)
    {
        return base.StartCoroutine(routine);
    }

    public void StartCoroutine(string name, IEnumerator routine)
    {
        StopCoroutine(name);
        namedCoroutines[name] = StartCoroutine(routine);
    }

    public new void StopCoroutine(string name)
    {
        if (namedCoroutines.TryGetValue(name, out Coroutine coroutine))
        {
            base.StopCoroutine(coroutine);
            namedCoroutines.Remove(name);
        }
    }

    public new void StopCoroutine(IEnumerator routine)
    {
        base.StopCoroutine(routine);
    }

    public void StopAllNamedCoroutines()
    {
        foreach (var coroutine in namedCoroutines.Values)
        {
            StopCoroutine(coroutine);
        }
        namedCoroutines.Clear();
    }

    public Coroutine DelayAction(float delay, System.Action action)
    {
        return StartCoroutine(DelayCoroutine(delay, action));
    }

    private IEnumerator DelayCoroutine(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    public Coroutine RepeatAction(float interval, int repeatCount, System.Action action)
    {
        return StartCoroutine(RepeatCoroutine(interval, repeatCount, action));
    }

    private IEnumerator RepeatCoroutine(float interval, int repeatCount, System.Action action)
    {
        for (int i = 0; i < repeatCount; i++)
        {
            action?.Invoke();
            yield return new WaitForSeconds(interval);
        }
    }

    public Coroutine RepeatForever(float interval, System.Action action)
    {
        return StartCoroutine(RepeatForeverCoroutine(interval, action));
    }

    private IEnumerator RepeatForeverCoroutine(float interval, System.Action action)
    {
        while (true)
        {
            action?.Invoke();
            yield return new WaitForSeconds(interval);
        }
    }

    public Coroutine Tween(float duration, System.Action<float> onUpdate, System.Action onComplete = null)
    {
        return StartCoroutine(TweenCoroutine(duration, onUpdate, onComplete));
    }

    private IEnumerator TweenCoroutine(float duration, System.Action<float> onUpdate, System.Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            onUpdate?.Invoke(t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        onUpdate?.Invoke(1f);
        onComplete?.Invoke();
    }
}
