using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// 加密管理器 - 提供消息加密解密功能
/// </summary>
public class CryptoManager : MonoBehaviour
{
    private static CryptoManager instance;
    public static CryptoManager Instance { get { return instance; } }

    private byte[] sessionKey;
    private byte[] iv;
    private bool encryptionEnabled = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化会话加密
    /// </summary>
    public void InitializeSessionEncryption(byte[] key, byte[] ivBytes)
    {
        sessionKey = key;
        iv = ivBytes;
        encryptionEnabled = true;
        Logger.Instance.LogInfo("Session encryption initialized", "Crypto");
    }

    /// <summary>
    /// 禁用加密
    /// </summary>
    public void DisableEncryption()
    {
        encryptionEnabled = false;
        sessionKey = null;
        iv = null;
        Logger.Instance.LogInfo("Encryption disabled", "Crypto");
    }

    /// <summary>
    /// AES加密
    /// </summary>
    public byte[] AESEncrypt(byte[] plainBytes, byte[] key, byte[] ivBytes)
    {
        try
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"AES encryption failed: {e.Message}", "Crypto");
            return null;
        }
    }

    /// <summary>
    /// AES解密
    /// </summary>
    public byte[] AESDecrypt(byte[] cipherBytes, byte[] key, byte[] ivBytes)
    {
        try
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (MemoryStream decryptedMs = new MemoryStream())
                        {
                            cs.CopyTo(decryptedMs);
                            return decryptedMs.ToArray();
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"AES decryption failed: {e.Message}", "Crypto");
            return null;
        }
    }

    /// <summary>
    /// 使用会话密钥加密
    /// </summary>
    public byte[] EncryptSession(byte[] plainBytes)
    {
        if (!encryptionEnabled || sessionKey == null || iv == null)
        {
            Logger.Instance.LogWarning("Session encryption not enabled, returning plain bytes", "Crypto");
            return plainBytes;
        }

        return AESEncrypt(plainBytes, sessionKey, iv);
    }

    /// <summary>
    /// 使用会话密钥解密
    /// </summary>
    public byte[] DecryptSession(byte[] cipherBytes)
    {
        if (!encryptionEnabled || sessionKey == null || iv == null)
        {
            Logger.Instance.LogWarning("Session encryption not enabled, returning cipher bytes", "Crypto");
            return cipherBytes;
        }

        return AESDecrypt(cipherBytes, sessionKey, iv);
    }

    /// <summary>
    /// 生成随机密钥
    /// </summary>
    public byte[] GenerateRandomKey(int keySize = 32)
    {
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            byte[] key = new byte[keySize];
            rng.GetBytes(key);
            return key;
        }
    }

    /// <summary>
    /// 生成随机IV
    /// </summary>
    public byte[] GenerateRandomIV(int ivSize = 16)
    {
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            byte[] iv = new byte[ivSize];
            rng.GetBytes(iv);
            return iv;
        }
    }

    /// <summary>
    /// XOR加密（简单加密，用于混淆）
    /// </summary>
    public byte[] XOREncrypt(byte[] data, byte[] key)
    {
        byte[] result = new byte[data.Length];
        int keyLength = key.Length;

        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ key[i % keyLength]);
        }

        return result;
    }

    /// <summary>
    /// XOR解密（与加密相同）
    /// </summary>
    public byte[] XORDecrypt(byte[] data, byte[] key)
    {
        return XOREncrypt(data, key);
    }

    /// <summary>
    /// 计算MD5哈希
    /// </summary>
    public string MD5Hash(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    /// <summary>
    /// 计算SHA256哈希
    /// </summary>
    public string SHA256Hash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    /// <summary>
    /// 计算数据的SHA256哈希
    /// </summary>
    public byte[] SHA256HashBytes(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(data);
        }
    }

    /// <summary>
    /// HMAC-SHA256签名
    /// </summary>
    public string HMACSHA256(string message, string secretKey)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
        {
            byte[] hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    /// <summary>
    /// Base64编码
    /// </summary>
    public string Base64Encode(string plainText)
    {
        byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    /// <summary>
    /// Base64解码
    /// </summary>
    public string Base64Decode(string base64EncodedData)
    {
        byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }

    /// <summary>
    /// RSA加密
    /// </summary>
    public string RSAEncrypt(string plainText, string publicKey)
    {
        try
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKey);
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] cipherBytes = rsa.Encrypt(plainBytes, false);
                return Convert.ToBase64String(cipherBytes);
            }
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"RSA encryption failed: {e.Message}", "Crypto");
            return null;
        }
    }

    /// <summary>
    /// RSA解密
    /// </summary>
    public string RSADecrypt(string cipherText, string privateKey)
    {
        try
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] plainBytes = rsa.Decrypt(cipherBytes, false);
                return Encoding.UTF8.GetString(plainBytes);
            }
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"RSA decryption failed: {e.Message}", "Crypto");
            return null;
        }
    }

    /// <summary>
    /// 生成RSA密钥对
    /// </summary>
    public void GenerateRSAKeyPair(out string publicKey, out string privateKey)
    {
        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
        {
            publicKey = rsa.ToXmlString(false);
            privateKey = rsa.ToXmlString(true);
        }
    }

    /// <summary>
    /// 计算消息校验和
    /// </summary>
    public uint CalculateChecksum(byte[] data)
    {
        uint checksum = 0;
        for (int i = 0; i < data.Length; i++)
        {
            checksum += data[i];
        }
        return checksum;
    }

    /// <summary>
    /// CRC32校验
    /// </summary>
    public uint CRC32(byte[] data)
    {
        uint[] crcTable = new uint[256];
        uint polynomial = 0xEDB88320;

        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 8; j > 0; j--)
            {
                if ((crc & 1) == 1)
                    crc = (crc >> 1) ^ polynomial;
                else
                    crc >>= 1;
            }
            crcTable[i] = crc;
        }

        uint crc32 = 0xFFFFFFFF;
        for (int i = 0; i < data.Length; i++)
        {
            crc32 = ((crc32 >> 8) & 0x00FFFFFF) ^ crcTable[(crc32 ^ data[i]) & 0xFF];
        }

        return ~crc32;
    }

    /// <summary>
    /// 添加消息完整性验证
    /// </summary>
    public byte[] AddIntegrityCheck(byte[] data)
    {
        uint crc = CRC32(data);
        byte[] crcBytes = BitConverter.GetBytes(crc);

        byte[] result = new byte[data.Length + 4];
        Array.Copy(data, 0, result, 0, data.Length);
        Array.Copy(crcBytes, 0, result, data.Length, 4);

        return result;
    }

    /// <summary>
    /// 验证消息完整性
    /// </summary>
    public bool VerifyIntegrityCheck(byte[] data)
    {
        if (data.Length < 4)
        {
            return false;
        }

        byte[] messageData = new byte[data.Length - 4];
        byte[] receivedCRC = new byte[4];

        Array.Copy(data, 0, messageData, 0, messageData.Length);
        Array.Copy(data, messageData.Length, receivedCRC, 0, 4);

        uint calculatedCRC = CRC32(messageData);
        uint receivedCRCValue = BitConverter.ToUInt32(receivedCRC, 0);

        return calculatedCRC == receivedCRCValue;
    }

    /// <summary>
    /// 密钥交换 - Diffie-Hellman（简化版）
    /// 实际项目中应使用更安全的实现
    /// 注意：Unity中不支持ECDiffieHellmanCng，这里提供一个简化版本
    /// </summary>
    public void DiffieHellmanKeyExchange(out byte[] publicKey, out byte[] privateKey)
    {
        // 简化版本：生成随机密钥对
        // 实际项目中应该使用完整的Diffie-Hellman实现
        using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
        {
            privateKey = new byte[32];
            rng.GetBytes(privateKey);

            publicKey = new byte[32];
            rng.GetBytes(publicKey);
        }

        Logger.Instance.LogWarning("Using simplified Diffie-Hellman implementation. For production, use a proper cryptographic library.", "Crypto");
    }

    /// <summary>
    /// 是否启用加密
    /// </summary>
    public bool IsEncryptionEnabled
    {
        get { return encryptionEnabled; }
    }
}
