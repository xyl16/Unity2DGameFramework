using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// ProtoBuf序列化器
/// 注意：这是一个简化版本，实际项目中建议使用Google.Protobuf或protobuf-net
/// </summary>
public static class ProtoBufSerializer
{
    /// <summary>
    /// 序列化对象到字节数组
    /// </summary>
    public static byte[] Serialize<T>(T obj) where T : class
    {
        if (obj == null)
        {
            return new byte[0];
        }

        try
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                SerializeObject(writer, obj);
                return ms.ToArray();
            }
        }
        catch (Exception e)
        {
            Logger.Instance?.LogError($"ProtoBuf serialization failed: {e.Message}", "ProtoBuf");
            return new byte[0];
        }
    }

    /// <summary>
    /// 从字节数组反序列化对象
    /// </summary>
    public static T Deserialize<T>(byte[] data) where T : class, new()
    {
        if (data == null || data.Length == 0)
        {
            return default(T);
        }

        try
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                return DeserializeObject<T>(reader);
            }
        }
        catch (Exception e)
        {
            Logger.Instance?.LogError($"ProtoBuf deserialization failed: {e.Message}", "ProtoBuf");
            return default(T);
        }
    }

    /// <summary>
    /// 内部序列化方法（供ProtoMessage使用）
    /// </summary>
    internal static byte[] SerializeInternal(object obj)
    {
        if (obj == null)
        {
            return new byte[0];
        }

        try
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                SerializeObject(writer, obj);
                return ms.ToArray();
            }
        }
        catch (Exception e)
        {
            Logger.Instance?.LogError($"ProtoBuf serialization failed: {e.Message}", "ProtoBuf");
            return new byte[0];
        }
    }

    /// <summary>
    /// 简化的序列化实现（用于演示）
    /// 实际项目中应该使用 protobuf-net 库
    /// </summary>
    private static void SerializeObject(BinaryWriter writer, object obj)
    {
        var properties = obj.GetType().GetProperties();
        foreach (var prop in properties)
        {
            if (!prop.CanRead || !prop.CanWrite) continue;

            var value = prop.GetValue(obj, null);
            var fieldType = prop.PropertyType;

            if (fieldType == typeof(int))
            {
                writer.Write((int)value);
            }
            else if (fieldType == typeof(uint))
            {
                writer.Write((uint)value);
            }
            else if (fieldType == typeof(long))
            {
                writer.Write((long)value);
            }
            else if (fieldType == typeof(ulong))
            {
                writer.Write((ulong)value);
            }
            else if (fieldType == typeof(short))
            {
                writer.Write((short)value);
            }
            else if (fieldType == typeof(ushort))
            {
                writer.Write((ushort)value);
            }
            else if (fieldType == typeof(float))
            {
                writer.Write((float)value);
            }
            else if (fieldType == typeof(double))
            {
                writer.Write((double)value);
            }
            else if (fieldType == typeof(bool))
            {
                writer.Write((bool)value);
            }
            else if (fieldType == typeof(string))
            {
                string strValue = value as string ?? string.Empty;
                writer.Write(strValue.Length);
                writer.Write(System.Text.Encoding.UTF8.GetBytes(strValue));
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = (System.Collections.IList)value;
                writer.Write(list.Count);
                foreach (var item in list)
                {
                    if (item != null)
                    {
                        SerializeObject(writer, item);
                    }
                }
            }
            // 更多类型可以继续添加...
        }
    }

    /// <summary>
    /// 简化的反序列化实现
    /// </summary>
    private static T DeserializeObject<T>(BinaryReader reader) where T : class, new()
    {
        T obj = new T();
        var properties = obj.GetType().GetProperties();

        foreach (var prop in properties)
        {
            if (!prop.CanRead || !prop.CanWrite) continue;

            var fieldType = prop.PropertyType;

            try
            {
                if (fieldType == typeof(int))
                {
                    prop.SetValue(obj, reader.ReadInt32(), null);
                }
                else if (fieldType == typeof(uint))
                {
                    prop.SetValue(obj, reader.ReadUInt32(), null);
                }
                else if (fieldType == typeof(long))
                {
                    prop.SetValue(obj, reader.ReadInt64(), null);
                }
                else if (fieldType == typeof(ulong))
                {
                    prop.SetValue(obj, reader.ReadUInt64(), null);
                }
                else if (fieldType == typeof(short))
                {
                    prop.SetValue(obj, reader.ReadInt16(), null);
                }
                else if (fieldType == typeof(ushort))
                {
                    prop.SetValue(obj, reader.ReadUInt16(), null);
                }
                else if (fieldType == typeof(float))
                {
                    prop.SetValue(obj, reader.ReadSingle(), null);
                }
                else if (fieldType == typeof(double))
                {
                    prop.SetValue(obj, reader.ReadDouble(), null);
                }
                else if (fieldType == typeof(bool))
                {
                    prop.SetValue(obj, reader.ReadBoolean(), null);
                }
                else if (fieldType == typeof(string))
                {
                    int length = reader.ReadInt32();
                    byte[] bytes = reader.ReadBytes(length);
                    string strValue = System.Text.Encoding.UTF8.GetString(bytes);
                    prop.SetValue(obj, strValue, null);
                }
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    int count = reader.ReadInt32();
                    Type itemType = fieldType.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(itemType);
                    var list = (System.Collections.IList)Activator.CreateInstance(listType);

                    for (int i = 0; i < count; i++)
                    {
                        var item = Deserialize(reader, itemType);
                        if (item != null)
                        {
                            list.Add(item);
                        }
                    }

                    prop.SetValue(obj, list, null);
                }
            }
            catch (EndOfStreamException)
            {
                Logger.Instance?.LogWarning($"Unexpected end of stream while deserializing {prop.Name}", "ProtoBuf");
                break;
            }
        }

        return obj;
    }

    /// <summary>
    /// 通用反序列化方法
    /// </summary>
    private static object Deserialize(BinaryReader reader, Type type)
    {
        if (type == typeof(int))
        {
            return reader.ReadInt32();
        }
        else if (type == typeof(uint))
        {
            return reader.ReadUInt32();
        }
        else if (type == typeof(float))
        {
            return reader.ReadSingle();
        }
        else if (type == typeof(string))
        {
            int length = reader.ReadInt32();
            byte[] bytes = reader.ReadBytes(length);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        else if (type == typeof(bool))
        {
            return reader.ReadBoolean();
        }
        else
        {
            return null;
        }
    }
}

/// <summary>
/// ProtoBuf消息基类
/// </summary>
[Serializable]
public abstract class ProtoMessage
{
    /// <summary>
    /// 消息ID
    /// </summary>
    public abstract ushort MessageId { get; }

    /// <summary>
    /// 序列化
    /// </summary>
    public virtual byte[] Serialize()
    {
        return ProtoBufSerializer.SerializeInternal(this);
    }

    /// <summary>
    /// 反序列化
    /// </summary>
    public static T Deserialize<T>(byte[] data) where T : ProtoMessage, new()
    {
        return ProtoBufSerializer.Deserialize<T>(data);
    }
}
