using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 集合处理工具类
/// </summary>
public static class CollectionHelper
{
    /// <summary>
    /// 判断集合是否为空或null
    /// </summary>
    public static bool IsNullOrEmpty<T>(IEnumerable<T> collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// 安全获取集合元素
    /// </summary>
    public static T SafeGet<T>(IList<T> list, int index, T defaultValue = default(T))
    {
        if (list == null || index < 0 || index >= list.Count)
            return defaultValue;

        return list[index];
    }

    /// <summary>
    /// 随机获取列表中的一个元素
    /// </summary>
    public static T GetRandom<T>(IList<T> list)
    {
        if (list == null || list.Count == 0)
            return default(T);

        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    /// <summary>
    /// 随机获取指定数量的不重复元素
    /// </summary>
    public static List<T> GetRandomItems<T>(IList<T> list, int count)
    {
        var result = new List<T>();

        if (list == null || list.Count == 0 || count <= 0)
            return result;

        var available = new List<T>(list);

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, available.Count);
            result.Add(available[index]);
            available.RemoveAt(index);
        }

        return result;
    }

    /// <summary>
    /// 打乱列表顺序（Fisher-Yates洗牌算法）
    /// </summary>
    public static void Shuffle<T>(IList<T> list)
    {
        if (list == null || list.Count < 2)
            return;

        System.Random random = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    /// <summary>
    /// 返回打乱后的新列表
    /// </summary>
    public static List<T> Shuffled<T>(IEnumerable<T> collection)
    {
        var result = collection.ToList();
        Shuffle(result);
        return result;
    }

    /// <summary>
    /// 将集合转换为数组
    /// </summary>
    public static T[] ToArraySafe<T>(IEnumerable<T> collection)
    {
        return collection?.ToArray() ?? new T[0];
    }

    /// <summary>
    /// 将集合转换为列表
    /// </summary>
    public static List<T> ToListSafe<T>(IEnumerable<T> collection)
    {
        return collection?.ToList() ?? new List<T>();
    }

    /// <summary>
    /// 连接多个集合
    /// </summary>
    public static IEnumerable<T> ConcatMultiple<T>(params IEnumerable<T>[] collections)
    {
        foreach (var collection in collections)
        {
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    yield return item;
                }
            }
        }
    }

    /// <summary>
    /// 去重
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(IEnumerable<T> collection, Func<T, TKey> keySelector)
    {
        var seenKeys = new HashSet<TKey>();
        foreach (var item in collection)
        {
            if (seenKeys.Add(keySelector(item)))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// 分页
    /// </summary>
    public static IEnumerable<T> Page<T>(IEnumerable<T> collection, int pageIndex, int pageSize)
    {
        if (collection == null)
            yield break;

        int skip = pageIndex * pageSize;
        var enumerator = collection.Skip(skip).Take(pageSize);

        foreach (var item in enumerator)
        {
            yield return item;
        }
    }

    /// <summary>
    /// 分组
    /// </summary>
    public static IEnumerable<List<T>> Chunk<T>(IEnumerable<T> collection, int chunkSize)
    {
        if (collection == null || chunkSize <= 0)
            yield break;

        var chunk = new List<T>(chunkSize);
        foreach (var item in collection)
        {
            chunk.Add(item);
            if (chunk.Count == chunkSize)
            {
                yield return chunk;
                chunk = new List<T>(chunkSize);
            }
        }

        if (chunk.Count > 0)
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// 查找最大元素的索引
    /// </summary>
    public static int FindMaxIndex<T>(IList<T> list) where T : IComparable<T>
    {
        if (list == null || list.Count == 0)
            return -1;

        int maxIndex = 0;
        T maxValue = list[0];

        for (int i = 1; i < list.Count; i++)
        {
            if (list[i].CompareTo(maxValue) > 0)
            {
                maxValue = list[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    /// <summary>
    /// 查找最小元素的索引
    /// </summary>
    public static int FindMinIndex<T>(IList<T> list) where T : IComparable<T>
    {
        if (list == null || list.Count == 0)
            return -1;

        int minIndex = 0;
        T minValue = list[0];

        for (int i = 1; i < list.Count; i++)
        {
            if (list[i].CompareTo(minValue) < 0)
            {
                minValue = list[i];
                minIndex = i;
            }
        }

        return minIndex;
    }

    /// <summary>
    /// 判断字典是否包含键
    /// </summary>
    public static bool ContainsKey<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
    {
        return dictionary != null && dictionary.ContainsKey(key);
    }

    /// <summary>
    /// 安全获取字典值
    /// </summary>
    public static TValue GetValueSafe<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
    {
        if (dictionary == null)
            return defaultValue;

        return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
    }

    /// <summary>
    /// 合并多个字典
    /// </summary>
    public static Dictionary<TKey, TValue> MergeDictionaries<TKey, TValue>(params IDictionary<TKey, TValue>[] dictionaries)
    {
        var result = new Dictionary<TKey, TValue>();

        foreach (var dictionary in dictionaries)
        {
            if (dictionary != null)
            {
                foreach (var kvp in dictionary)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
        }

        return result;
    }
}
