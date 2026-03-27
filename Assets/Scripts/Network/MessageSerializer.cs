using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 消息序列化器 - 支持多种序列化方式
/// </summary>
public class MessageSerializer
{
    /// <summary>
    /// 序列化类型
    /// </summary>
    public enum SerializeType
    {
        JSON,        // JSON序列化
        ProtoBuf,    // ProtoBuf序列化
        Binary,      // 二进制序列化
        XML          // XML序列化
    }

    private SerializeType currentType = SerializeType.JSON;

    public MessageSerializer(SerializeType type = SerializeType.JSON)
    {
        currentType = type;
    }

    public void SetSerializeType(SerializeType type)
    {
        currentType = type;
    }

    /// <summary>
    /// 序列化对象
    /// </summary>
    public byte[] Serialize(object obj)
    {
        if (obj == null)
        {
            return new byte[0];
        }

        switch (currentType)
        {
            case SerializeType.JSON:
                return SerializeToJson(obj);
            case SerializeType.ProtoBuf:
                return ProtoBufSerializer.Serialize(obj);
            case SerializeType.Binary:
                return SerializeToBinary(obj);
            case SerializeType.XML:
                return SerializeToXML(obj);
            default:
                return SerializeToJson(obj);
        }
    }

    /// <summary>
    /// 反序列化对象
    /// </summary>
    public T Deserialize<T>(byte[] data) where T : class, new()
    {
        if (data == null || data.Length == 0)
        {
            return default(T);
        }

        switch (currentType)
        {
            case SerializeType.JSON:
                return DeserializeFromJson<T>(data);
            case SerializeType.ProtoBuf:
                return ProtoBufSerializer.Deserialize<T>(data);
            case SerializeType.Binary:
                return DeserializeFromBinary<T>(data);
            case SerializeType.XML:
                return DeserializeFromXML<T>(data);
            default:
                return DeserializeFromJson<T>(data);
        }
    }

    /// <summary>
    /// JSON序列化
    /// </summary>
    private byte[] SerializeToJson(object obj)
    {
        try
        {
            string json = JsonUtility.ToJson(obj);
            return Encoding.UTF8.GetBytes(json);
        }
        catch (Exception e)
        {
            Logger.Instance?.LogError($"JSON serialization failed: {e.Message}", "Serializer");
            return new byte[0];
        }
    }

    /// <summary>
    /// JSON反序列化
    /// </summary>
    private T DeserializeFromJson<T>(byte[] data) where T : class, new()
    {
        try
        {
            string json = Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception e)
        {
            Logger.Instance?.LogError($"JSON deserialization failed: {e.Message}", "Serializer");
            return default(T);
        }
    }

    /// <summary>
    /// 二进制序列化
    /// </summary>
    private byte[] SerializeToBinary(object obj)
    {
        try
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                SerializeBinary(writer, obj);
                return ms.ToArray();
            }
        }
        catch (Exception e)
        {
            Logger.Instance?.LogError($"Binary serialization failed: {e.Message}", "Serializer");
            return new byte[0];
        }
    }

    /// <summary>
    /// 二进制反序列化
    /// </summary>
    private T DeserializeFromBinary<T>(byte[] data) where T : class, new()
    {
        try
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                return DeserializeBinary<T>(reader);
            }
        }
        catch (Exception e)
        {
            Logger.Instance?.LogError($"Binary deserialization failed: {e.Message}", "Serializer");
            return default(T);
        }
    }

    /// <summary>
    /// 递归二进制序列化
    /// </summary>
    private void SerializeBinary(BinaryWriter writer, object obj)
    {
        if (obj == null)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        Type type = obj.GetType();

        if (type == typeof(int))
        {
            writer.Write((int)obj);
        }
        else if (type == typeof(uint))
        {
            writer.Write((uint)obj);
        }
        else if (type == typeof(long))
        {
            writer.Write((long)obj);
        }
        else if (type == typeof(ulong))
        {
            writer.Write((ulong)obj);
        }
        else if (type == typeof(short))
        {
            writer.Write((short)obj);
        }
        else if (type == typeof(ushort))
        {
            writer.Write((ushort)obj);
        }
        else if (type == typeof(float))
        {
            writer.Write((float)obj);
        }
        else if (type == typeof(double))
        {
            writer.Write((double)obj);
        }
        else if (type == typeof(bool))
        {
            writer.Write((bool)obj);
        }
        else if (type == typeof(string))
        {
            string str = obj as string;
            writer.Write(str.Length);
            writer.Write(Encoding.UTF8.GetBytes(str));
        }
        else if (type == typeof(byte[]))
        {
            byte[] bytes = obj as byte[];
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
        else if (type.IsClass && !type.IsArray)
        {
            // 处理自定义类
            var properties = type.GetProperties();
            foreach (var prop in properties)
            {
                if (prop.CanRead)
                {
                    var value = prop.GetValue(obj, null);
                    SerializeBinary(writer, value);
                }
            }
        }
        else if (type.IsArray)
        {
            var array = obj as Array;
            writer.Write(array.Length);
            foreach (var item in array)
            {
                SerializeBinary(writer, item);
            }
        }
    }

    /// <summary>
    /// 递归二进制反序列化
    /// </summary>
    private T DeserializeBinary<T>(BinaryReader reader) where T : class, new()
    {
        bool hasValue = reader.ReadBoolean();
        if (!hasValue)
        {
            return null;
        }

        object obj = new T();
        Type type = typeof(T);

        if (type == typeof(int))
        {
            return reader.ReadInt32() as T;
        }
        else if (type == typeof(uint))
        {
            return reader.ReadUInt32() as T;
        }
        else if (type == typeof(long))
        {
            return reader.ReadInt64() as T;
        }
        else if (type == typeof(ulong))
        {
            return reader.ReadUInt64() as T;
        }
        else if (type == typeof(float))
        {
            return reader.ReadSingle() as T;
        }
        else if (type == typeof(double))
        {
            return reader.ReadDouble() as T;
        }
        else if (type == typeof(bool))
        {
            return reader.ReadBoolean() as T;
        }
        else if (type == typeof(string))
        {
            int length = reader.ReadInt32();
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes) as T;
        }
        else if (type == typeof(byte[]))
        {
            int length = reader.ReadInt32();
            return reader.ReadBytes(length) as T;
        }
        else if (type.IsClass && !type.IsArray)
        {
            var properties = type.GetProperties();
            foreach (var prop in properties)
            {
                if (prop.CanWrite)
                {
                    var value = DeserializeBinary(reader);
                    prop.SetValue(obj, value, null);
                }
            }
            return obj as T;
        }

        return obj as T;
    }

    /// <summary>
    /// 反序列化通用对象
    /// </summary>
    private object DeserializeBinary(BinaryReader reader)
    {
        bool hasValue = reader.ReadBoolean();
        if (!hasValue)
        {
            return null;
        }

        // 这里简化处理，实际需要更完整的实现
        int length = reader.ReadInt32();
        if (length == 0)
        {
            return null;
        }

        byte[] bytes = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// XML序列化
    /// </summary>
    private byte[] SerializeToXML(object obj)
    {
        try
        {
            using (MemoryStream ms = new MemoryStream())
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(obj.GetType());
                serializer.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        catch (Exception e)
        {
            Logger.Instance?.LogError($"XML serialization failed: {e.Message}", "Serializer");
            return new byte[0];
        }
    }

    /// <summary>
    /// XML反序列化
    /// </summary>
    private T DeserializeFromXML<T>(byte[] data) where T : class, new()
    {
        try
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                return serializer.Deserialize(ms) as T;
            }
        }
        catch (Exception e)
        {
            Logger.Instance?.LogError($"XML deserialization failed: {e.Message}", "Serializer");
            return default(T);
        }
    }

    /// <summary>
    /// 压缩数据
    /// </summary>
    public byte[] Compress(byte[] data)
    {
        try
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionMode.Compress))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }
        catch (Exception e)
        {
            Logger.Instance?.LogError($"Compression failed: {e.Message}", "Serializer");
            return data;
        }
    }

    /// <summary>
    /// 解压数据
    /// </summary>
    public byte[] Decompress(byte[] data)
    {
        try
        {
            using (MemoryStream input = new MemoryStream(data))
            using (System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress))
            using (MemoryStream output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }
        catch (Exception e)
        {
            Logger.Instance?.LogError($"Decompression failed: {e.Message}", "Serializer");
            return data;
        }
    }
}

/// <summary>
/// 网络消息包
/// </summary>
[Serializable]
public class NetworkMessage
{
    /// <summary>
    /// 消息ID
    /// </summary>
    public ushort messageId;

    /// <summary>
    /// 消息序列
    /// </summary>
    public uint sequence;

    /// <summary>
    /// 时间戳
    /// </summary>
    public long timestamp;

    /// <summary>
    /// 消息数据
    /// </summary>
    public byte[] data;

    /// <summary>
    /// 校验和
    /// </summary>
    public uint checksum;

    public NetworkMessage()
    {
        timestamp = DateTime.UtcNow.Ticks;
    }

    /// <summary>
    /// 计算校验和
    /// </summary>
    public void CalculateChecksum()
    {
        checksum = CryptoManager.Instance.CalculateChecksum(data);
    }

    /// <summary>
    /// 验证校验和
    /// </summary>
    public bool VerifyChecksum()
    {
        uint calculated = CryptoManager.Instance.CalculateChecksum(data);
        return calculated == checksum;
    }
}

/// <summary>
/// 消息包构建器
/// </summary>
public class MessagePacketBuilder
{
    private static uint sequenceCounter = 0;

    /// <summary>
    /// 构建消息包
    /// </summary>
    public static byte[] BuildPacket(ushort messageId, object data, MessageSerializer.SerializeType serializeType = MessageSerializer.SerializeType.JSON)
    {
        MessageSerializer serializer = new MessageSerializer(serializeType);

        NetworkMessage message = new NetworkMessage();
        message.messageId = messageId;
        message.sequence = ++sequenceCounter;
        message.timestamp = DateTime.UtcNow.Ticks;
        message.data = serializer.Serialize(data);
        message.CalculateChecksum();

        return SerializePacket(message);
    }

    /// <summary>
    /// 序列化消息包
    /// </summary>
    public static byte[] SerializePacket(NetworkMessage message)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            writer.Write(message.messageId);
            writer.Write(message.sequence);
            writer.Write(message.timestamp);
            writer.Write(message.checksum);
            writer.Write(message.data.Length);
            writer.Write(message.data);

            return ms.ToArray();
        }
    }

    /// <summary>
    /// 解析消息包
    /// </summary>
    public static NetworkMessage ParsePacket(byte[] packetData)
    {
        using (MemoryStream ms = new MemoryStream(packetData))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            NetworkMessage message = new NetworkMessage();
            message.messageId = reader.ReadUInt16();
            message.sequence = reader.ReadUInt32();
            message.timestamp = reader.ReadInt64();
            message.checksum = reader.ReadUInt32();

            int dataLength = reader.ReadInt32();
            message.data = reader.ReadBytes(dataLength);

            return message;
        }
    }

    /// <summary>
    /// 从消息包中提取数据并反序列化
    /// </summary>
    public static T ExtractData<T>(NetworkMessage message, MessageSerializer.SerializeType serializeType = MessageSerializer.SerializeType.JSON) where T : class, new()
    {
        MessageSerializer serializer = new MessageSerializer(serializeType);
        return serializer.Deserialize<T>(message.data);
    }
}
