using System;
using System.IO;
using System.Text;
using UnityEngine;

public class MessageProtocol
{
    public static byte[] Serialize(ushort msgId, object data)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            bw.Write(msgId);

            if (data is byte[] byteArray)
            {
                bw.Write(byteArray.Length);
                bw.Write(byteArray);
            }
            else if (data is string str)
            {
                byte[] strBytes = Encoding.UTF8.GetBytes(str);
                bw.Write(strBytes.Length);
                bw.Write(strBytes);
            }
            else if (data != null)
            {
                string json = JsonUtility.ToJson(data);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                bw.Write(jsonBytes.Length);
                bw.Write(jsonBytes);
            }
            else
            {
                bw.Write(0);
            }

            return ms.ToArray();
        }
    }

    public static T Deserialize<T>(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryReader br = new BinaryReader(ms))
        {
            // 这里实现具体的反序列化逻辑
            // 简化版本，实际项目中需要更复杂的反序列化
            return default(T);
        }
    }

    public static ushort GetMessageId(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryReader br = new BinaryReader(ms))
        {
            return br.ReadUInt16();
        }
    }
}