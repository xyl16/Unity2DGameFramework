using System.Collections.Generic;
using UnityEngine;

public class PrefabPool : MonoBehaviour
{
    private static PrefabPool instance;
    public static PrefabPool Instance { get { return instance; } }

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

    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    public void Preload(GameObject prefab, int amount)
    {
        string key = prefab.name;

        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary[key] = new Queue<GameObject>();
        }

        for (int i = 0; i < amount; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            poolDictionary[key].Enqueue(obj);
        }
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string key = prefab.name;

        if (!poolDictionary.ContainsKey(key) || poolDictionary[key].Count == 0)
        {
            GameObject obj = Instantiate(prefab, position, rotation);
            return obj;
        }

        GameObject pooledObj = poolDictionary[key].Dequeue();
        pooledObj.transform.position = position;
        pooledObj.transform.rotation = rotation;
        pooledObj.SetActive(true);

        return pooledObj;
    }

    public GameObject Spawn(GameObject prefab)
    {
        return Spawn(prefab, Vector3.zero, Quaternion.identity);
    }

    public void Despawn(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);

        string key = obj.name.Replace("(Clone)", "");

        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary[key] = new Queue<GameObject>();
        }

        poolDictionary[key].Enqueue(obj);
    }

    public void ClearPool(string prefabName)
    {
        if (poolDictionary.ContainsKey(prefabName))
        {
            foreach (var obj in poolDictionary[prefabName])
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            poolDictionary.Remove(prefabName);
        }
    }

    public void ClearAllPools()
    {
        foreach (var kvp in poolDictionary)
        {
            foreach (var obj in kvp.Value)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }
        poolDictionary.Clear();
    }

    public int GetPoolCount(string prefabName)
    {
        if (poolDictionary.ContainsKey(prefabName))
        {
            return poolDictionary[prefabName].Count;
        }
        return 0;
    }
}
