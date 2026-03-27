# Unity2D 游戏框架说明文档

## 一、框架概述

本框架是一个基于 Unity 2022 的 2D 游戏开发框架，采用分层架构设计，提供完整的游戏开发基础设施，包括资源管理、UI系统、网络通信、热更新、配置管理、性能监控、数据分析、多语言支持、存档系统等功能模块。

### 1.1 设计理念

- **模块化设计**: 各功能模块独立，低耦合高内聚
- **单例模式**: 核心管理器采用单例模式，确保全局唯一
- **事件驱动**: 使用事件系统实现模块间解耦通信
- **热更新支持**: 支持 AssetBundle 资源热更新和代码热更新
- **易扩展性**: 提供基类和接口，便于功能扩展
- **数据驱动**: 完善的数据分析和性能监控系统
- **国际化**: 内置多语言支持，方便本地化
- **云端同步**: 支持云端存档和数据分析

### 1.2 技术栈

- **引擎**: Unity 2022
- **语言**: C#
- **架构模式**: MVC + 单例 + 事件驱动
- **资源管理**: AssetBundle
- **热更新**: 文件校验 + 增量下载

---

## 二、项目结构

```
Assets/Scripts/
├── Core/                  # 核心模块
│   ├── GameInitializer.cs      # 游戏初始化器
│   ├── ResourceManager.cs      # 资源管理器
│   ├── AssetBundleUpdater.cs   # 资源热更新器
│   ├── DownloadManager.cs      # 下载管理器
│   ├── FileVerification.cs     # 文件校验器
│   ├── Logger.cs              # 日志系统
│   ├── Singleton.cs           # 单例基类
│   ├── EventManager.cs        # 事件管理器
│   ├── AudioManager.cs        # 音频管理器
│   ├── DataManager.cs        # 数据管理器
│   ├── ConfigManager.cs       # 配置管理器
│   ├── ConfigTableManager.cs  # 配置表管理器
│   └── SceneManager.cs        # 场景管理器
├── Utils/                 # 工具模块
│   ├── GameManager.cs         # 游戏主管理器
│   ├── VersionManager.cs      # 版本管理器
│   ├── TimerManager.cs        # 定时器管理器
│   ├── CoroutineManager.cs    # 协程管理器
│   ├── PrefabPool.cs          # 预制体对象池
│   ├── ObjectPool.cs          # 通用对象池
│   └── UIInitializer.cs       # UI初始化器
├── Network/               # 网络模块
│   ├── NetworkManager.cs      # 网络管理器
│   ├── SecureNetworkManager.cs # 安全网络管理器
│   ├── SocketClient.cs        # Socket客户端
│   ├── MessageProtocol.cs     # 消息协议
│   ├── ProtoBufSerializer.cs  # ProtoBuf序列化器
│   ├── MessageSerializer.cs   # 消息序列化器
│   ├── CryptoManager.cs       # 加密管理器
│   ├── ConfigExamples.cs      # 配置示例
│   └── NetworkUsageExample.cs # 网络使用示例
├── UI/                    # UI模块
│   ├── UIManager.cs           # UI管理器
│   ├── BasePanel.cs           # 面板基类
│   └── UIFactory.cs           # UI工厂
├── Performance/           # 性能监控模块
│   └── PerformanceMonitor.cs  # 性能监控器
├── Analytics/            # 数据分析模块
│   └── AnalyticsManager.cs    # 数据分析管理器
├── Localization/         # 多语言模块
│   └── LanguageManager.cs     # 多语言管理器
├── SaveSystem/           # 存档系统模块
│   └── SaveManager.cs         # 存档管理器
├── Controller/            # 控制器层
│   ├── BaseController.cs      # 控制器基类
│   ├── UpdateController.cs   # 更新控制器
│   └── LoginController.cs     # 登录控制器
├── View/                  # 视图层
│   ├── BaseView.cs            # 视图基类
│   ├── UpdateView.cs          # 更新视图
│   └── LoginView.cs           # 登录视图
├── Model/                 # 模型层
│   ├── BaseModel.cs           # 模型基类
│   └── LoginModel.cs          # 登录模型
└── Editor/                # 编辑器工具
    ├── SceneFixer.cs          # 场景修复工具
    ├── AssetBundleBuilder.cs  # AB包构建工具
    ├── AssetBundleLabeler.cs  # AB包标记工具
    ├── UpdateEditor.cs        # 更新编辑器工具
    └── ConfigTableEditor.cs   # 配置表编辑器工具
```

---

## 三、新增核心模块说明

### 3.0 配置表管理系统

#### ConfigTableManager - 配置表管理器

功能特性：
- 支持 CSV 格式配置文件加载
- 支持从 AssetBundle 加载配置
- 支持强类型配置解析
- 支持缓存管理，避免重复解析
- 提供 GetInt/GetFloat/GetString/GetBool 等便捷方法

使用示例：
```csharp
// 从CSV文件加载
ConfigTableManager.Instance.LoadConfigFromCSV("ItemConfig", "Assets/Config/ItemConfig.csv");

// 从AssetBundle加载
ConfigTableManager.Instance.LoadConfigFromAB("ItemConfig", "ItemConfig");

// 直接获取配置值
int price = ConfigTableManager.Instance.GetIntValue("ItemConfig", 1, "price", 0);
string name = ConfigTableManager.Instance.GetStringValue("ItemConfig", 1, "name", "");

// 加载为强类型配置
ConfigTableManager.Instance.LoadConfig<ItemConfig>("ItemConfig");
ItemConfig item = ConfigTableManager.Instance.GetConfig<ItemConfig>(1);
```

CSV文件格式：
```
字段说明,字段名,字段类型
道具ID,id,int
道具名称,name,string
道具类型,type,int
稀有度,rarity,int

1,新手剑,1,1
2,铁剑,1,2
```

### 3.1 ProtoBuf 序列化系统

#### ProtoBufSerializer - ProtoBuf序列化器

功能特性：
- 高效的二进制序列化
- 比 JSON 更小的数据体积
- 更快的序列化/反序列化速度
- 支持复杂对象结构

使用示例：
```csharp
// 定义消息类
public class LoginRequest : ProtoMessage
{
    public override ushort MessageId { get { return 1001; } }
    public string username;
    public string password;
}

// 序列化
LoginRequest request = new LoginRequest { username = "player", password = "123" };
byte[] data = request.Serialize();

// 反序列化
LoginRequest result = LoginRequest.Deserialize<LoginRequest>(data);
```

### 3.2 消息加密解密系统

#### CryptoManager - 加密管理器

功能特性：
- AES 加密/解密（CBC模式）
- XOR 加密/解密（快速混淆）
- MD5 / SHA256 哈希
- HMAC-SHA256 签名
- RSA 加密/解密
- CRC32 校验和
- 消息完整性验证
- Base64 编码/解码

使用示例：
```csharp
CryptoManager crypto = CryptoManager.Instance;

// 初始化会话加密
byte[] key = crypto.GenerateRandomKey(32);
byte[] iv = crypto.GenerateRandomIV(16);
crypto.InitializeSessionEncryption(key, iv);

// AES加密/解密
byte[] encrypted = crypto.AESEncrypt(plainBytes, key, iv);
byte[] decrypted = crypto.AESDecrypt(encrypted, key, iv);

// 哈希计算
string md5 = crypto.MD5Hash("data");
string sha256 = crypto.SHA256Hash("data");

// CRC32校验
uint crc = crypto.CRC32(data);

// HMAC签名
string hmac = crypto.HMACSHA256("message", "secret");
```

### 3.3 消息序列化系统

#### MessageSerializer - 消息序列化器

功能特性：
- 支持多种序列化方式（JSON/ProtoBuf/Binary/XML）
- 支持数据压缩（GZip）
- 统一的消息格式
- 消息完整性校验

序列化类型：
```csharp
public enum SerializeType
{
    JSON,        // JSON序列化
    ProtoBuf,    // ProtoBuf序列化
    Binary,      // 二进制序列化
    XML          // XML序列化
}
```

使用示例：
```csharp
// 创建序列化器
MessageSerializer serializer = new MessageSerializer(MessageSerializer.SerializeType.JSON);

// 序列化
byte[] data = serializer.Serialize(obj);

// 反序列化
MyClass obj = serializer.Deserialize<MyClass>(data);

// 压缩
byte[] compressed = serializer.Compress(data);

// 解压
byte[] decompressed = serializer.Decompress(compressed);
```

#### NetworkMessage - 网络消息包
```csharp
public class NetworkMessage
{
    public ushort messageId;     // 消息ID
    public uint sequence;        // 消息序列
    public long timestamp;       // 时间戳
    public byte[] data;          // 消息数据
    public uint checksum;        // 校验和
}
```

#### MessagePacketBuilder - 消息包构建器

使用示例：
```csharp
// 构建消息包
byte[] packet = MessagePacketBuilder.BuildPacket(
    messageId: 1001,
    data: requestObj,
    serializeType: MessageSerializer.SerializeType.JSON
);

// 解析消息包
NetworkMessage message = MessagePacketBuilder.ParsePacket(packet);

// 提取数据
MyData data = MessagePacketBuilder.ExtractData<MyData>(message, serializeType);
```

### 3.4 安全网络管理器

#### SecureNetworkManager - 安全网络管理器

功能特性：
- 支持消息加密传输
- 支持多种序列化方式
- 支持消息压缩
- 自动心跳检测
- 消息完整性校验
- 泛型消息处理器

使用示例：
```csharp
// 设置序列化方式
SecureNetworkManager.Instance.SetSerializeType(MessageSerializer.SerializeType.JSON);

// 注册消息处理器
SecureNetworkManager.Instance.RegisterMessageHandler<LoginResponse>(
    MessageID.LOGIN_RESPONSE,
    (response) => {
        Debug.Log($"Login result: {response.code}");
    }
);

// 连接服务器（启用加密）
SecureNetworkManager.Instance.Connect("127.0.0.1", 8888, enableEncryption: true);

// 发送消息
SecureNetworkManager.Instance.SendMessage(MessageID.LOGIN_REQUEST, loginRequest);

// 监听连接事件
SecureNetworkManager.Instance.OnConnected += () => {
    Debug.Log("Connected!");
};
```

配置选项：
```csharp
public MessageSerializer.SerializeType SerializeType = MessageSerializer.SerializeType.JSON;
public bool EnableEncryption = false;   // 是否启用加密
public bool EnableCompression = false;  // 是否启用压缩
```

---

## 四、网络消息示例

### 4.1 消息定义

```csharp
// 登录请求
public class LoginRequest : ProtoMessage
{
    public override ushort MessageId { get { return 1001; } }
    public string username;
    public string password;
    public string deviceId;
}

// 登录响应
public class LoginResponse : ProtoMessage
{
    public override ushort MessageId { get { return 1002; } }
    public int code;
    public string message;
    public string token;
    public PlayerInfo playerInfo;
}
```

### 4.2 消息ID常量

```csharp
public static class MessageID
{
    // 系统消息 (1000-1999)
    public const ushort LOGIN_REQUEST = 1001;
    public const ushort LOGIN_RESPONSE = 1002;
    
    // 战斗消息 (2000-2999)
    public const ushort BATTLE_START_REQUEST = 2001;
    public const ushort BATTLE_START_RESPONSE = 2002;
    
    // 聊天消息 (3000-3999)
    public const ushort CHAT_SEND_REQUEST = 3001;
    // ... 更多消息ID
}
```

---

## 五、编辑器工具

### 5.1 ConfigTableEditor - 配置表编辑器

功能：
- 加载和管理 CSV 配置文件
- 查看配置内容
- 创建示例配置
- 打开配置目录

使用方法：
1. 打开菜单 `Tools/Config Table Manager`
2. 选择 CSV 文件或输入路径
3. 加载配置到系统中

---

## 六、核心模块说明

### 3.1 游戏初始化流程

```
启动游戏
    ↓
GameInitializer.Start()
    ↓
InitializeCoreSystems() - 初始化核心系统
    ├─ Logger 初始化
    ├─ 验证所有 Manager 是否挂载
    └─ 注册事件监听
    ↓
GameManager.Start()
    ├─ 设置游戏状态为 Initializing
    └─ 开始版本检查
    ↓
VersionManager.CheckVersion()
    ├─ 检查本地版本
    ├─ 对比服务器版本
    └─ 执行热更新（如需要）
    ↓
进入主菜单状态
    ↓
连接服务器
    ↓
进入登录状态
```

### 3.2 核心管理器（Managers 节点）

Managers 节点是整个框架的核心，挂载了所有必要的管理器组件：

| 组件名称 | 功能说明 |
|---------|---------|
| EventManager | 事件系统，管理模块间通信 |
| ResourceManager | 资源管理，负责 AssetBundle 加载 |
| UIManager | UI 管理，负责面板的加载、显示、隐藏 |
| SceneManager | 场景管理，负责场景切换 |
| AudioManager | 音频管理，负责音效和背景音乐播放 |
| DataManager | 数据管理，负责数据存储和读取 |
| ConfigManager | 配置管理，负责游戏配置加载 |
| PrefabPool | 预制体对象池，优化对象创建销毁 |
| TimerManager | 定时器管理，提供定时功能 |
| CoroutineManager | 协程管理，统一管理协程 |
| NetworkManager | 网络管理，处理服务器通信 |

### 3.3 游戏主管理器（GameManager 节点）

GameManager 节点负责游戏状态管理和游戏流程控制：

| 组件名称 | 功能说明 |
|---------|---------|
| GameManager | 游戏主控制器，管理游戏状态机 |
| VersionManager | 版本管理，负责版本检查和热更新 |

**游戏状态机：**
- `Initializing` - 初始化中
- `MainMenu` - 主菜单
- `Login` - 登录
- `Playing` - 游戏进行中
- `Paused` - 暂停
- `GameOver` - 游戏结束

### 3.4 游戏初始化器（GameInitializer 节点）

GameInitializer 节点负责游戏启动时的初始化工作：
- 验证所有管理器是否正确挂载
- 设置日志级别
- 注册全局事件监听
- 提供快捷键功能（F1显示/隐藏日志，F2导出日志）

---

## 四、核心系统详解

### 4.1 事件系统（EventManager）

EventManager 是框架的核心通信机制，实现模块间解耦。

#### 使用示例

```csharp
// 注册事件监听
EventManager.Instance.AddListener("EventName", OnEventCallback);

// 触发事件
EventManager.Instance.InvokeEvent("EventName", data);

// 移除事件监听
EventManager.Instance.RemoveListener("EventName", OnEventCallback);

// 清除所有事件
EventManager.Instance.ClearAllEvents();
```

#### 常用事件

| 事件名称 | 触发时机 | 参数 |
|---------|---------|------|
| SceneLoaded | 场景加载完成 | 场景名称（string） |
| SceneUnloaded | 场景卸载完成 | 场景名称（string） |
| GameStateChanged | 游戏状态改变 | GameStateChangedArgs |
| EnterLoginState | 进入登录状态 | - |
| LoginSuccess | 登录成功 | - |
| NetworkConnected | 网络连接成功 | - |
| NetworkDisconnected | 网络断开 | - |

### 4.2 资源管理（ResourceManager）

ResourceManager 基于 AssetBundle 实现资源加载和卸载。

#### 使用示例

```csharp
// 加载资源
GameObject prefab = ResourceManager.Instance.LoadAsset<GameObject>("Prefabs/Player");

// 异步加载资源
IEnumerator LoadAsync()
{
    AssetBundleLoadOperation operation = ResourceManager.Instance.LoadAssetAsync<GameObject>("Prefabs/Player");
    yield return operation;
    GameObject prefab = operation.GetAsset<GameObject>();
}

// 卸载资源
ResourceManager.Instance.UnloadAsset("Prefabs/Player");

// 卸载所有未使用的资源
ResourceManager.Instance.UnloadUnusedAssets();
```

### 4.3 UI系统（UIManager + BasePanel）

UI系统采用面板基类设计，统一管理所有UI界面。

#### UIManager 使用示例

```csharp
// 打开面板
GameObject panel = UIManager.Instance.OpenPanel("LoginPanel", args);

// 关闭面板
UIManager.Instance.ClosePanel("LoginPanel");

// 销毁面板
UIManager.Instance.DestroyPanel("LoginPanel");

// 预加载面板
UIManager.Instance.PreloadPanel("MainPanel");

// 获取面板组件
LoginPanel panel = UIManager.Instance.GetOpenedPanel<LoginPanel>("LoginPanel");
```

#### 自定义面板

```csharp
public class CustomPanel : BasePanel
{
    public override void OnOpen(object args = null)
    {
        base.OnOpen(args);
        // 初始化面板
        RegisterEvent("SomeEvent", OnSomeEvent);
    }

    public override void OnClose()
    {
        // 清理资源
        base.OnClose();
    }

    private void OnSomeEvent(object obj)
    {
        // 处理事件
    }
}
```

### 4.4 网络系统（NetworkManager）

NetworkManager 基于 Socket 实现网络通信，支持消息注册和分发。

#### 使用示例

```csharp
// 连接服务器
NetworkManager.Instance.Connect("127.0.0.1", 8080);

// 断开连接
NetworkManager.Instance.Disconnect();

// 注册消息处理器
NetworkManager.Instance.RegisterMessageHandler(msgId, HandleMessage);

// 发送消息
NetworkManager.Instance.SendMessage(msgId, data);

// 检查连接状态
if (NetworkManager.Instance.IsConnected)
{
    // 已连接
}
```

### 4.5 日志系统（Logger）

Logger 提供分级日志记录功能，支持日志导出。

#### 日志级别

```csharp
public enum LogLevel
{
    Debug,      // 调试信息
    Info,       // 一般信息
    Warning,    // 警告信息
    Error       // 错误信息
}
```

#### 使用示例

```csharp
// 设置日志级别
Logger.Instance.SetLogLevel(LogLevel.Info);

// 记录日志
Logger.Instance.LogInfo("这是一条信息", "ModuleName");
Logger.Instance.LogWarning("这是一条警告", "ModuleName");
Logger.Instance.LogError("这是一条错误", "ModuleName");

// 导出日志
Logger.Instance.ExportLogs();
```

### 4.6 数据管理（DataManager）

DataManager 提供数据存储和读取功能。

#### 使用示例

```csharp
// 保存数据
DataManager.Instance.SaveData("PlayerData", playerData);

// 读取数据
PlayerData data = DataManager.Instance.LoadData<PlayerData>("PlayerData");

// 删除数据
DataManager.Instance.DeleteData("PlayerData");
```

### 4.7 配置管理（ConfigManager）

ConfigManager 负责游戏配置的加载和管理。

#### 配置结构

```csharp
public class AppConfig
{
    public string serverIP;
    public int serverPort;
    // 其他配置项...
}
```

#### 使用示例

```csharp
// 获取配置
ConfigManager.AppConfig config = ConfigManager.Instance.GetConfig();

// 使用配置
string ip = config.serverIP;
int port = config.serverPort;
```

---

## 五、MVC架构说明

框架采用经典的 MVC 架构，实现业务逻辑与视图的分离。

### 5.1 Model（模型层）

Model 负责数据存储和业务逻辑处理。

```csharp
public class LoginModel : BaseModel
{
    private string username;
    private string password;

    public override void Init()
    {
        // 初始化数据
        username = DataManager.Instance.LoadData<string>("Username", "");
    }

    public bool ValidateCredentials(string user, string pass)
    {
        // 验证逻辑
        return user == "admin" && pass == "123456";
    }
}
```

### 5.2 View（视图层）

View 负责UI显示和用户输入处理。

```csharp
public class LoginView : BaseView
{
    public InputField usernameInput;
    public InputField passwordInput;
    public Button loginButton;

    public override void Init()
    {
        loginButton.onClick.AddListener(OnLoginButtonClick);
    }

    private void OnLoginButtonClick()
    {
        controller?.OnLoginRequest(usernameInput.text, passwordInput.text);
    }
}
```

### 5.3 Controller（控制器层）

Controller 协调 Model 和 View，处理业务逻辑。

```csharp
public class LoginController : BaseController<LoginView, LoginModel>
{
    public override void Init()
    {
        base.Init();
        // 初始化控制器
    }

    public void OnLoginRequest(string username, string password)
    {
        if (model.ValidateCredentials(username, password))
        {
            DataManager.Instance.SaveData("Username", username);
            view?.ShowSuccess();
            EventManager.Instance.InvokeEvent("LoginSuccess");
        }
        else
        {
            view?.ShowError("用户名或密码错误");
        }
    }
}
```

---

## 六、热更新系统

框架支持完整的 AssetBundle 热更新功能。

### 6.1 热更新流程

```
启动游戏
    ↓
FileVerification.CheckVersionFiles()
    ├─ 读取本地版本信息
    ├─ 下载服务器版本文件
    └─ 对比差异
    ↓
DownloadManager.DownloadFiles()
    ├─ 下载缺失的文件
    ├─ 下载更新的文件
    └─ 校验下载文件
    ↓
AssetBundleUpdater.ApplyUpdate()
    ├─ 替换旧文件
    ├─ 清理缓存
    └─ 完成更新
    ↓
进入游戏
```

### 6.2 热更新使用

```csharp
// 检查并执行更新
AssetBundleUpdater.Instance.CheckForUpdate((success, message) =>
{
    if (success)
    {
        Logger.Instance.LogInfo("更新成功", "Update");
    }
    else
    {
        Logger.Instance.LogError($"更新失败: {message}", "Update");
    }
});

// 取消更新
AssetBundleUpdater.Instance.CancelUpdate();
```

### 6.3 AB包构建

使用编辑器工具构建 AssetBundle：

1. **标记资源为 AB 包**
   - 菜单：`Tools > AssetBundle Labeler`
   - 选择文件夹或资源进行标记

2. **构建 AB 包**
   - 菜单：`Tools > Build AssetBundles`
   - 选择构建平台（Windows、Android、iOS等）
   - 点击开始构建

---

## 七、编辑器工具

框架提供了多个编辑器工具，提升开发效率。

### 7.1 场景修复工具（SceneFixer）

修复场景中的常见问题：

- `Tools > Fix Missing Scripts` - 删除缺失的脚本组件
- `Tools > 重建所有游戏对象` - 重建 Managers、GameManager、GameInitializer 节点
- `Tools > 显示所有对象状态` - 查看当前节点状态
- `Tools > Clean Duplicate Components` - 清理重复组件

### 7.2 AB包构建工具（AssetBundleBuilder）

构建和管理 AssetBundle：

- 设置构建平台
- 设置输出路径
- 设置压缩方式
- 清理旧的 AB 包
- 构建新的 AB 包

### 7.3 AB包标记工具（AssetBundleLabeler）

批量标记资源为 AssetBundle：

- 按文件夹标记
- 按文件类型标记
- 清除标记
- 预览标记结果

### 7.4 更新编辑器（UpdateEditor）

配置和测试热更新系统：

- 配置服务器地址
- 配置本地/服务器路径
- 测试版本检查
- 测试文件下载

---

## 八、最佳实践

### 8.1 代码规范

1. **命名规范**
   - 类名：PascalCase（如 `GameManager`）
   - 方法名：PascalCase（如 `Initialize`）
   - 变量名：camelCase（如 `playerData`）
   - 常量：PascalCase（如 `MaxHealth`）

2. **注释规范**
   - 所有公共类和方法添加 XML 注释
   - 复杂逻辑添加行内注释
   - 使用中文注释

### 8.2 性能优化

1. **使用对象池**
   ```csharp
   // 从对象池获取
   GameObject obj = PrefabPool.Instance.Spawn("Bullet");

   // 回收对象到池中
   PrefabPool.Instance.Despawn(obj);
   ```

2. **使用协程管理器**
   ```csharp
   // 通过协程管理器启动协程
   CoroutineManager.Instance.StartCoroutine(MyCoroutine());
   ```

3. **预加载资源**
   ```csharp
   // 预加载面板
   UIManager.Instance.PreloadPanel("MainPanel");
   ```

### 8.3 事件使用

1. **及时注销事件**
   ```csharp
   // 在 BasePanel 中使用 RegisterEvent，会自动注销
   RegisterEvent("EventName", OnEvent);
   ```

2. **事件命名规范**
   - 使用 PascalCase
   - 描述性命名（如 `PlayerDied` 而非 `Event1`）

### 8.4 错误处理

1. **使用日志记录**
   ```csharp
   try
   {
       // 可能出错的操作
   }
   catch (System.Exception e)
   {
       Logger.Instance.LogError($"操作失败: {e.Message}", "ModuleName");
   }
   ```

2. **检查空引用**
   ```csharp
   if (EventManager.Instance != null)
   {
       EventManager.Instance.InvokeEvent("EventName");
   }
   ```

---

## 九、常见问题

### 9.1 如何添加新的管理器？

1. 创建继承自 `MonoBehaviour` 的类
2. 在 `SceneFixer.cs` 的 `CreateCleanManagers()` 方法中添加组件
3. 在 `GameInitializer.cs` 的 `InitializeCoreSystems()` 方法中验证管理器

### 9.2 如何创建新的UI面板？

1. 创建继承自 `BasePanel` 的类
2. 实现 `OnOpen` 和 `OnClose` 方法
3. 使用 `UIManager.Instance.OpenPanel("PanelName")` 打开面板

### 9.3 如何添加新的游戏状态？

1. 在 `GameManager.GameState` 枚举中添加新状态
2. 在 `GameManager` 中添加进入该状态的方法
3. 监听 `GameStateChanged` 事件处理状态变化

### 9.4 如何配置热更新服务器？

1. 编辑 `ConfigManager.AppConfig` 配置
2. 在服务器上放置版本文件（version.json）
3. 在服务器上放置 AssetBundle 文件
4. 使用 `UpdateEditor` 测试配置

### 9.5 如何调试日志？

1. 按 `F1` 键切换日志显示
2. 按 `F2` 键导出日志到文件
3. 在 `GameInitializer` 中设置 `defaultLogLevel` 控制日志级别

---

## 十、扩展模块

框架已实现以下扩展模块，提供更完善的功能支持。

### 10.1 性能监控（PerformanceMonitor）

PerformanceMonitor 提供全面的性能监控功能，帮助开发者优化游戏性能。

#### 主要功能

- **FPS 监控** - 实时帧率计算
- **内存使用监控** - 监控总内存、纹理、音频、网格等资源占用
- **渲染统计** - DrawCalls、三角形数量统计
- **加载时间统计** - 记录各资源的加载耗时
- **自动记录** - 定时记录性能数据到日志

#### 使用示例

```csharp
// 开始性能监控
PerformanceMonitor.Instance.SetEnableMonitor(true);

// 开始加载计时
PerformanceMonitor.Instance.StartLoadTimer("Scene01");

// 执行加载操作...

// 结束加载计时
PerformanceMonitor.Instance.EndLoadTimer();

// 获取加载时间
float loadTime = PerformanceMonitor.Instance.GetLoadTime("Scene01");

// 获取当前性能数据
PerformanceMonitor.PerformanceData data = PerformanceMonitor.Instance.GetCurrentData();
Debug.Log($"FPS: {data.fps}, 内存: {data.memoryUsage / 1024 / 1024:F1}MB");

// 导出性能报告
PerformanceMonitor.Instance.ExportPerformanceReport();
```

#### 性能数据结构

```csharp
public class PerformanceData
{
    public float fps;              // 帧率
    public long memoryUsage;       // 内存使用（字节）
    public float frameTime;        // 帧时间（毫秒）
    public int drawCalls;          // DrawCalls 数量
    public int triangles;          // 三角形数量
    public float audioMemory;      // 音频内存（MB）
    public float textureMemory;    // 贴图内存（MB）
    public float meshMemory;        // 网格内存（MB）
    public float materialCount;    // 材质数量
    public float objectCount;      // 对象数量
}
```

### 10.2 数据分析（AnalyticsManager）

AnalyticsManager 负责收集和分析用户行为、游戏数据、错误日志等信息。

#### 主要功能

- **事件追踪** - 记录用户行为事件
- **游戏数据统计** - 收集游戏运行数据
- **错误日志上报** - 自动捕获和记录错误
- **会话管理** - 追踪用户会话信息
- **离线存储** - 网络断开时本地存储数据

#### 使用示例

```csharp
// 记录用户行为
AnalyticsManager.Instance.LogUserAction("ClickButton", "MainMenu",
    new Dictionary<string, object>
    {
        { "ButtonName", "StartButton" },
        { "ClickTime", DateTime.Now.ToString() }
    });

// 记录游戏数据
AnalyticsManager.Instance.LogGameData("PlayerStats",
    new Dictionary<string, object>
    {
        { "Level", 10 },
        { "Score", 5000 },
        { "Coins", 100 }
    });

// 记录错误
try
{
    // 可能出错的操作
}
catch (Exception e)
{
    AnalyticsManager.Instance.LogError(e.Message, e.StackTrace, "Context");
}

// 获取统计数据
var stats = AnalyticsManager.Instance.GetStatistics();

// 导出错误日志
AnalyticsManager.Instance.ExportErrorLogs();
```

#### 分析事件结构

```csharp
public class AnalyticsEvent
{
    public string eventId;
    public string eventName;           // 事件名称
    public string sessionId;           // 会话ID
    public DateTime timestamp;         // 时间戳
    public Dictionary<string, object> parameters;  // 参数
}
```

### 10.3 多语言支持（LanguageManager）

LanguageManager 提供完整的多语言支持，支持动态切换语言和资源本地化。

#### 主要功能

- **多语言支持** - 支持中文、英文、日文、韩文
- **动态切换** - 运行时无缝切换语言
- **资源本地化** - 支持文本、图片、音频等资源本地化
- **默认翻译** - 内置常用翻译
- **导入导出** - 支持导入导出语言数据

#### 使用示例

```csharp
// 设置语言
LanguageManager.Instance.SetLanguage(LanguageManager.Language.English);

// 获取翻译文本
string loginText = LanguageManager.Instance.GetText("Login");
string welcomeText = LanguageManager.Instance.GetText("Welcome", playerName);

// 获取带参数的翻译
string message = LanguageManager.Instance.GetText("LevelProgress", 5, 100);

// 获取本地化图片路径
string imagePath = LanguageManager.Instance.GetLocalizedImagePath("logo.png");

// 获取本地化音频路径
string audioPath = LanguageManager.Instance.GetLocalizedAudioPath("bgm.mp3");

// 获取当前语言
LanguageManager.Language currentLang = LanguageManager.Instance.GetCurrentLanguage();
string langCode = LanguageManager.Instance.GetCurrentLanguageCode();

// 监听语言变化事件
LanguageManager.Instance.OnLanguageChanged += (lang) =>
{
    Debug.Log($"语言已切换为: {LanguageManager.Instance.GetLanguageName(lang)}");
    // 重新加载UI等
};

// 导出语言字典
LanguageManager.Instance.ExportLanguageDictionary();
```

#### 支持的语言

| 语言 | 语言代码 | 显示名称 |
|-----|---------|---------|
| 中文 | zh | 中文 |
| 英文 | en | English |
| 日文 | ja | 日本語 |
| 韩文 | ko | 한국어 |

### 10.4 存档系统（SaveManager）

SaveManager 提供完善的存档管理功能，支持多存档槽、自动存档和云端存档同步。

#### 主要功能

- **多存档槽** - 支持多个独立存档槽
- **自动存档** - 定时自动保存游戏进度
- **存档槽管理** - 查看、删除、导入导出存档
- **云端存档** - 支持上传下载云端存档
- **灵活存储** - 支持存储任意类型数据

#### 使用示例

```csharp
// 保存到指定槽位
bool success = SaveManager.Instance.SaveToSlot(0, "存档1", "第一章完成");

// 从指定槽位加载
bool loaded = SaveManager.Instance.LoadFromSlot(0);

// 保存游戏数据
SaveManager.Instance.SetData("PlayerLevel", 10);
SaveManager.Instance.SetData("PlayerName", "Player1");
SaveManager.Instance.SetData("Inventory", items);

// 读取游戏数据
int level = SaveManager.Instance.GetData<int>("PlayerLevel", 1);
string name = SaveManager.Instance.GetData<string>("PlayerName", "Unknown");

// 获取所有存档槽
List<SaveSlot> slots = SaveManager.Instance.GetAllSaveSlots();
foreach (var slot in slots)
{
    if (slot.isUsed)
    {
        Debug.Log($"槽位 {slot.slotIndex}: {slot.saveName} - {slot.saveTime}");
    }
}

// 删除存档
SaveManager.Instance.DeleteSlot(0);

// 导出存档
string jsonData = SaveManager.Instance.ExportSave(0);

// 导入存档
SaveManager.Instance.ImportSave(0, jsonData);

// 上传到云端
SaveManager.Instance.UploadToCloud(0);

// 从云端下载
SaveManager.Instance.DownloadFromCloud(0);

// 监听存档事件
SaveManager.Instance.OnSaveCompleted += (slotIndex) =>
{
    Debug.Log($"存档完成: 槽位 {slotIndex}");
};

SaveManager.Instance.OnLoadCompleted += (slotIndex) =>
{
    Debug.Log($"加载完成: 槽位 {slotIndex}");
};
```

#### 存档槽结构

```csharp
public class SaveSlot
{
    public int slotIndex;         // 槽位索引
    public bool isUsed;           // 是否已使用
    public string saveName;       // 存档名称
    public DateTime saveTime;     // 保存时间
    public float playTime;        // 游戏时长（秒）
    public int level;             // 玩家等级
    public string description;    // 描述
}
```

#### 配置说明

在 SaveManager 中可以配置：

```csharp
public int maxSaveSlots = 5;              // 最大存档槽数量
public float autoSaveInterval = 300f;     // 自动存档间隔（秒）
public bool enableAutoSave = true;        // 是否启用自动存档
public bool enableCloudSave = false;      // 是否启用云端存档
```

---

## 十一、常见问题

### 11.1 如何添加新的管理器？

1. 创建继承自 `MonoBehaviour` 的类
2. 在 `SceneFixer.cs` 的 `CreateCleanManagers()` 方法中添加组件
3. 在 `GameInitializer.cs` 的 `InitializeCoreSystems()` 方法中验证管理器

### 11.2 如何创建新的UI面板？

1. 创建继承自 `BasePanel` 的类
2. 实现 `OnOpen` 和 `OnClose` 方法
3. 使用 `UIManager.Instance.OpenPanel("PanelName")` 打开面板

### 11.3 如何添加新的游戏状态？

1. 在 `GameManager.GameState` 枚举中添加新状态
2. 在 `GameManager` 中添加进入该状态的方法
3. 监听 `GameStateChanged` 事件处理状态变化

### 11.4 如何配置热更新服务器？

1. 编辑 `ConfigManager.AppConfig` 配置
2. 在服务器上放置版本文件（version.json）
3. 在服务器上放置 AssetBundle 文件
4. 使用 `UpdateEditor` 测试配置

### 11.5 如何调试日志？

1. 按 `F1` 键切换日志显示
2. 按 `F2` 键导出日志到文件
3. 在 `GameInitializer` 中设置 `defaultLogLevel` 控制日志级别

### 11.6 如何使用性能监控？

1. 启动性能监控：`PerformanceMonitor.Instance.SetEnableMonitor(true)`
2. 记录加载时间：`PerformanceMonitor.Instance.StartLoadTimer("ItemName")`
3. 导出报告：`PerformanceMonitor.Instance.ExportPerformanceReport()`

### 11.7 如何添加新语言的翻译？

1. 使用 `LanguageManager.Instance.AddTranslation(key, language, translation)` 添加翻译
2. 或创建语言文件放在 `Resources/Localization/` 目录下
3. 使用 `LanguageManager.Instance.GetText(key)` 获取翻译

### 11.8 如何配置自动存档？

在 `SaveManager` 中设置：
```csharp
enableAutoSave = true;           // 启用自动存档
autoSaveInterval = 300f;          // 设置间隔（秒）
```

### 11.9 如何实现云端存档？

1. 设置 `SaveManager.Instance.enableCloudSave = true`
2. 实现 `UploadToCloud` 和 `DownloadFromCloud` 方法
3. 与服务器端接口对接

### 11.10 如何收集数据分析？

1. 记录用户行为：`AnalyticsManager.Instance.LogUserAction(...)`
2. 记录游戏数据：`AnalyticsManager.Instance.LogGameData(...)`
3. 记录错误：`AnalyticsManager.Instance.LogError(...)`

---

## 十二、联系方式

如有问题或建议，请联系开发团队。

---

**文档版本**: 2.0
**最后更新**: 2026-03-28
**适用框架版本**: Unity2DGameFramework v2.0
