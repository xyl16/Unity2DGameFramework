using UnityEngine;
using System;

/// <summary>
/// 网络使用示例
/// </summary>
public class NetworkUsageExample : MonoBehaviour
{
    private void Start()
    {
        // 示例1: 使用普通的NetworkManager (JSON序列化)
        Example_BasicNetworkManager();

        // 示例2: 使用SecureNetworkManager (支持加密和多种序列化方式)
        Example_SecureNetworkManager();

        // 示例3: 加密功能
        Example_Encryption();

        // 示例4: 配置表加载
        Example_ConfigTable();
    }

    /// <summary>
    /// 示例: 基础网络管理器使用
    /// </summary>
    private void Example_BasicNetworkManager()
    {
        // 注册消息处理器
        NetworkManager.Instance.RegisterMessageHandler(MessageID.LOGIN_RESPONSE, HandleLoginResponse);

        // 连接服务器
        ConfigManager.AppConfig config = ConfigManager.Instance.GetConfig();
        NetworkManager.Instance.Connect(config.serverIP, config.serverPort);

        // 监听连接事件
        NetworkManager.Instance.OnConnected += () =>
        {
            Debug.Log("Connected to server");
            // 发送登录请求
            var loginRequest = new LoginRequest
            {
                username = "player1",
                password = "password123",
                deviceId = SystemInfo.deviceUniqueIdentifier,
                platform = Application.platform.ToString(),
                version = 1
            };
            NetworkManager.Instance.SendMessage(MessageID.LOGIN_REQUEST, loginRequest);
        };
    }

    private void HandleLoginResponse(byte[] data)
    {
        LoginResponse response = JsonUtility.FromJson<LoginResponse>(System.Text.Encoding.UTF8.GetString(data));
        Debug.Log($"Login result: {response.code}, message: {response.message}");
    }

    /// <summary>
    /// 示例: 安全网络管理器使用
    /// </summary>
    private void Example_SecureNetworkManager()
    {
        // 设置序列化方式
        SecureNetworkManager.Instance.SetSerializeType(MessageSerializer.SerializeType.JSON);

        // 注册消息处理器 (泛型版本)
        SecureNetworkManager.Instance.RegisterMessageHandler<LoginResponse>(MessageID.LOGIN_RESPONSE, (response) =>
        {
            Debug.Log($"Login result: {response.code}, token: {response.token}");
            if (response.code == 0)
            {
                // 登录成功，加载玩家信息
                LoadPlayerInfo(response.playerInfo);
            }
        });

        // 连接服务器 (启用加密)
        ConfigManager.AppConfig config = ConfigManager.Instance.GetConfig();
        SecureNetworkManager.Instance.Connect(config.serverIP, config.serverPort, enableEncryption: true);

        // 监听连接事件
        SecureNetworkManager.Instance.OnConnected += () =>
        {
            Debug.Log("Connected to secure server");

            // 发送登录请求
            var loginRequest = new LoginRequest
            {
                username = "player1",
                password = "password123",
                deviceId = SystemInfo.deviceUniqueIdentifier,
                platform = Application.platform.ToString(),
                version = 1
            };
            SecureNetworkManager.Instance.SendMessage(MessageID.LOGIN_REQUEST, loginRequest);
        };
    }

    private void LoadPlayerInfo(PlayerInfo playerInfo)
    {
        Debug.Log($"Player: {playerInfo.nickname}, Level: {playerInfo.level}, Coin: {playerInfo.coin}");
    }

    /// <summary>
    /// 示例: 加密功能使用
    /// </summary>
    private void Example_Encryption()
    {
        CryptoManager crypto = CryptoManager.Instance;

        // 生成密钥
        byte[] key = crypto.GenerateRandomKey(32);
        byte[] iv = crypto.GenerateRandomIV(16);

        // AES加密/解密
        string plainText = "Hello, Server!";
        byte[] encrypted = crypto.AESEncrypt(System.Text.Encoding.UTF8.GetBytes(plainText), key, iv);
        byte[] decrypted = crypto.AESDecrypt(encrypted, key, iv);
        string decryptedText = System.Text.Encoding.UTF8.GetString(decrypted);
        Debug.Log($"AES: {plainText} -> {decryptedText}");

        // XOR加密/解密
        byte[] xorKey = System.Text.Encoding.UTF8.GetBytes("secretkey");
        byte[] xorEncrypted = crypto.XOREncrypt(System.Text.Encoding.UTF8.GetBytes(plainText), xorKey);
        byte[] xorDecrypted = crypto.XORDecrypt(xorEncrypted, xorKey);
        string xorDecryptedText = System.Text.Encoding.UTF8.GetString(xorDecrypted);
        Debug.Log($"XOR: {plainText} -> {xorDecryptedText}");

        // 哈希
        string md5Hash = crypto.MD5Hash(plainText);
        string sha256Hash = crypto.SHA256Hash(plainText);
        Debug.Log($"MD5: {md5Hash}, SHA256: {sha256Hash}");

        // CRC32校验
        byte[] data = System.Text.Encoding.UTF8.GetBytes(plainText);
        uint crc = crypto.CRC32(data);
        Debug.Log($"CRC32: {crc}");

        // 消息完整性验证
        byte[] dataWithCheck = crypto.AddIntegrityCheck(data);
        bool isValid = crypto.VerifyIntegrityCheck(dataWithCheck);
        Debug.Log($"Integrity check: {isValid}");

        // HMAC签名
        string hmac = crypto.HMACSHA256(plainText, "secret");
        Debug.Log($"HMAC-SHA256: {hmac}");

        // Base64
        string base64 = crypto.Base64Encode(plainText);
        string base64Decoded = crypto.Base64Decode(base64);
        Debug.Log($"Base64: {plainText} -> {base64Decoded}");
    }

    /// <summary>
    /// 示例: 配置表加载和使用
    /// </summary>
    private void Example_ConfigTable()
    {
        ConfigTableManager configTable = ConfigTableManager.Instance;

        // 从CSV文件加载配置
        // configTable.LoadConfigFromCSV("ItemConfig", "Assets/Config/ItemConfig.csv");

        // 从AssetBundle加载配置
        // configTable.LoadConfigFromAB("ItemConfig", "ItemConfig");

        // 直接获取配置值
        int price = configTable.GetIntValue("ItemConfig", 1, "price", 0);
        string name = configTable.GetStringValue("ItemConfig", 1, "name", "");
        Debug.Log($"Item {name}, Price: {price}");

        // 加载为强类型配置
        // configTable.LoadConfig<ItemConfig>("ItemConfig");
        // ItemConfig item = configTable.GetConfig<ItemConfig>(1);
        // Debug.Log($"Item: {item.name}, Type: {item.type}");

        // 获取所有配置
        // var allItems = configTable.GetAllConfig<ItemConfig>();
        // foreach (var kvp in allItems)
        // {
        //     Debug.Log($"ID: {kvp.Key}, Name: {kvp.Value.name}");
        // }
    }

    /// <summary>
    /// 示例: ProtoBuf序列化
    /// </summary>
    private void Example_ProtoBuf()
    {
        // 序列化
        LoginRequest request = new LoginRequest
        {
            username = "player1",
            password = "password123",
            deviceId = "device123",
            platform = "Android",
            version = 1
        };

        byte[] data = request.Serialize();
        Debug.Log($"Serialized data length: {data.Length}");

        // 反序列化
        LoginRequest deserialized = LoginRequest.Deserialize<LoginRequest>(data);
        Debug.Log($"Deserialized: {deserialized.username}");
    }

    /// <summary>
    /// 示例: 多种序列化方式
    /// </summary>
    private void Example_MultipleSerialization()
    {
        var obj = new TestObject
        {
            id = 1,
            name = "Test",
            value = 100.5f
        };

        // JSON序列化
        var jsonSerializer = new MessageSerializer(MessageSerializer.SerializeType.JSON);
        byte[] jsonData = jsonSerializer.Serialize(obj);
        TestObject jsonObj = jsonSerializer.Deserialize<TestObject>(jsonData);

        // 二进制序列化
        var binarySerializer = new MessageSerializer(MessageSerializer.SerializeType.Binary);
        byte[] binaryData = binarySerializer.Serialize(obj);
        TestObject binaryObj = binarySerializer.Deserialize<TestObject>(binaryData);

        Debug.Log($"JSON: {jsonData.Length} bytes, Binary: {binaryData.Length} bytes");
    }

    [System.Serializable]
    private class TestObject
    {
        public int id;
        public string name;
        public float value;
    }
}
