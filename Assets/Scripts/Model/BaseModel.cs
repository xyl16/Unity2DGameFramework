using System.Collections.Generic;

public class BaseModel
{
    protected Dictionary<string, object> data = new Dictionary<string, object>();

    public void SetData(string key, object value)
    {
        data[key] = value;
    }

    public T GetData<T>(string key)
    {
        if (data.TryGetValue(key, out object value))
        {
            return (T)value;
        }
        return default(T);
    }

    public virtual void Init() { }
    public virtual void Dispose() { }
}