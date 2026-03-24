using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : new()
{
    private Queue<T> pool = new Queue<T>();
    private System.Func<T> createFunc;
    private System.Action<T> resetFunc;
    private int maxSize = 100;

    public ObjectPool(System.Func<T> createFunc = null, System.Action<T> resetFunc = null, int initialSize = 10, int maxSize = 100)
    {
        this.createFunc = createFunc ?? (() => new T());
        this.resetFunc = resetFunc;
        this.maxSize = maxSize;

        for (int i = 0; i < initialSize; i++)
        {
            pool.Enqueue(this.createFunc());
        }
    }

    public T Get()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        return createFunc();
    }

    public void Return(T obj)
    {
        resetFunc?.Invoke(obj);

        if (pool.Count < maxSize)
        {
            pool.Enqueue(obj);
        }
    }

    public void Clear()
    {
        pool.Clear();
    }

    public int Count
    {
        get { return pool.Count; }
    }
}
