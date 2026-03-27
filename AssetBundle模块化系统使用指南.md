# AssetBundle 模块化系统使用指南

## 📋 目录
1. [系统概述](#系统概述)
2. [核心组件](#核心组件)
3. [编辑器工具使用](#编辑器工具使用)
4. [运行时加载器](#运行时加载器)
5. [热更新系统](#热更新系统)
6. [完整工作流程](#完整工作流程)
7. [常见问题](#常见问题)

---

## 系统概述

本系统提供了一套完整的模块化 AssetBundle 解决方案，包括：
- **模块化构建**：将资源按公共/模块分类打包
- **依赖管理**：自动处理 Bundle 之间的依赖关系
- **优先级加载**：按配置优先级顺序自动加载
- **热更新**：支持增量更新、断点续传、文件校验
- **智能加载**：自动解析依赖、路径自动选择

---

## 核心组件

### 1. AssetBundleModuleBuilder（编辑器工具）
**位置**：`Assets/Scripts/Editor/AssetBundleModuleBuilder.cs`

**功能**：
- 可视化配置 Bundle
- 设置依赖关系和优先级
- 自动生成加载顺序
- 构建 AB 包和版本清单
- 验证依赖关系

### 2. AssetBundleLoader（运行时加载器）
**位置**：`Assets/Scripts/Core/AssetBundleLoader.cs`

**功能**：
- 自动依赖解析加载
- 按优先级排序加载
- 支持单/多 Bundle 加载
- 从 Bundle 加载资源
- 版本信息管理

### 3. AssetBundleUpdater（热更新管理器）
**位置**：`Assets/Scripts/Core/AssetBundleUpdater.cs`

**功能**：
- 版本检查和比较
- 增量更新下载
- 断点续传支持
- 文件哈希校验
- 备份和回滚

### 4. DownloadManager（下载管理器）
**位置**：`Assets/Scripts/Core/DownloadManager.cs`

**功能**：
- 并发下载管理
- 优先级队列
- 断点续传
- 下载进度跟踪

### 5. ResourceManager（资源管理器）
**位置**：`Assets/Scripts/Core/ResourceManager.cs`

**功能**：
- 统一资源加载接口
- 本地/AB 包模式切换
- 版本管理集成

---

## 编辑器工具使用

### 打开工具

Unity 菜单：`Tools/AssetBundle工具/模块化Bundle构建`

### 界面说明

#### 1. 构建设置
```
┌─────────────────────────────────┐
│ 构建设置                         │
├─────────────────────────────────┤
│ 输出路径: AssetBundles           │
│ 版本号: 1.0.0                    │
└─────────────────────────────────┘
```

#### 2. Bundle 配置
预置 Bundle 列表：

| Bundle 名称 | 优先级 | 依赖 | 说明 |
|------------|-------|------|------|
| common_textures | 0 | 无 | 公共纹理资源 |
| common_materials | 1 | common_textures | 公共材质 |
| common_audio | 2 | 无 | 公共音频 |
| common_ui | 3 | common_textures, common_materials | 公共 UI 预制 |
| config_tables | 4 | 无 | 配置表数据 |
| chess_resources | 10 | common_textures, common_audio | 象棋资源 |
| chess_prefabs | 11 | chess_resources, common_ui | 象棋预制 |

**操作按钮**：
- **选择文件夹**：选择资源所在目录
- **配置依赖**：弹出窗口配置该 Bundle 的依赖项
- **清空依赖**：清除所有依赖关系
- **设置该Bundle资源标签**：为该 Bundle 设置资源标签
- **删除**：删除 Bundle 配置

**资源类型过滤**：
```
资源类型: [t:Texture2D ×] [t:Material ×] [+]
```
- 支持添加/删除资源类型过滤
- 常用类型：`t:Texture2D`, `t:Sprite`, `t:Material`, `t:AudioClip`, `t:Prefab`, `t:TextAsset`

#### 3. 构建操作

**标签管理**：
```
┌─────────────────────────────────┐
│ [设置所有Bundle标签] [清除所有标签] │
└─────────────────────────────────┘
```

**构建 Bundle**：
```
┌─────────────────────────────────┐
│ [构建所有Bundle] [构建公共Bundle] [构建模块Bundle] │
└─────────────────────────────────┘
```

**版本和部署**：
```
┌─────────────────────────────────┐
│ [生成版本清单]                 │
│ [复制到StreamingAssets]         │
└─────────────────────────────────┘
```

**验证工具**：
```
┌─────────────────────────────────┐
│ [查看依赖顺序] [验证依赖关系]    │
└─────────────────────────────────┘
```

#### 4. 当前 Bundle 列表
显示所有已设置标签的 Bundle 及其资源数量。

---

## 运行时加载器

### 基础使用

#### 1. 初始化
```csharp
// 获取单例
var loader = AssetBundleLoader.Instance;

// 加载版本信息
loader.LoadVersionInfo((success) => {
    if (success) {
        Debug.Log("版本信息加载成功");
    }
});
```

#### 2. 加载单个 Bundle（自动加载依赖）
```csharp
AssetBundleLoader.Instance.LoadBundle("chess_prefabs", (success, bundle) => {
    if (success) {
        Debug.Log("Bundle 加载成功");
        // bundle 已自动加载其依赖
    }
});
```

#### 3. 加载多个 Bundle（按优先级顺序）
```csharp
var bundles = new List<string> { "chess_prefabs", "common_ui" };

AssetBundleLoader.Instance.LoadBundles(bundles, (success, loadedBundles) => {
    if (success) {
        Debug.Log($"成功加载 {loadedBundles.Count} 个 Bundle");
        // 依赖已按优先级自动加载
    }
});
```

#### 4. 从 Bundle 加载资源
```csharp
// 加载单个资源
AssetBundleLoader.Instance.LoadAsset<GameObject>("chess_prefabs", "ChessBoard", 
    (success, prefab) => {
        if (success) {
            Instantiate(prefab);
        }
    });

// 加载 Bundle 中所有同类资源
AssetBundleLoader.Instance.LoadAllAssets<Sprite>("chess_resources", 
    (success, sprites) => {
        if (success) {
            Debug.Log($"加载了 {sprites.Length} 个 Sprite");
        }
    });
```

#### 5. 卸载 Bundle
```csharp
// 卸载单个 Bundle（不卸载已加载的对象）
AssetBundleLoader.Instance.UnloadBundle("chess_prefabs", false);

// 卸载所有 Bundle
AssetBundleLoader.Instance.UnloadAllBundles(false);
```

### 高级功能

#### 检查加载状态
```csharp
// 检查 Bundle 是否已加载
bool isLoaded = AssetBundleLoader.Instance.IsBundleLoaded("common_ui");

// 获取已加载的 Bundle
AssetBundle bundle = AssetBundleLoader.Instance.GetLoadedBundle("common_ui");

// 获取 Bundle Manifest
AssetBundleManifest manifest = AssetBundleLoader.Instance.GetBundleManifest("common_ui");
```

#### 监听加载进度
```csharp
// 通过协程检查进度
IEnumerator MonitorLoading() {
    while (AssetBundleLoader.Instance.CurrentStatus == AssetBundleLoader.LoadStatus.LoadingBundle) {
        float progress = AssetBundleLoader.Instance.LoadProgress;
        Debug.Log($"加载进度: {progress * 100}%");
        yield return null;
    }
}
```

---

## 热更新系统

### 热更新流程

#### 1. 检查更新
```csharp
AssetBundleUpdater.Instance.CheckUpdate((needUpdate, message, remoteVersion) => {
    if (needUpdate) {
        Debug.Log(message); // "发现新版本: 1.0.1, 需要下载 3 个文件, 总大小: 15.2 MB"
        
        // 显示更新对话框
        ShowUpdateDialog(message, () => {
            // 开始更新
            AssetBundleUpdater.Instance.StartUpdate((success, result) => {
                if (success) {
                    Debug.Log("更新成功: " + result);
                    
                    // 重新加载版本信息
                    AssetBundleLoader.Instance.LoadVersionInfo((loaded) => {
                        // 继续游戏流程
                    });
                }
            });
        });
    } else {
        Debug.Log("当前已是最新版本");
    }
});
```

#### 2. 监听更新进度
```csharp
IEnumerator MonitorUpdateProgress() {
    while (AssetBundleUpdater.Instance.CurrentStatus == AssetBundleUpdater.UpdateStatus.Downloading) {
        float progress = AssetBundleUpdater.Instance.DownloadProgress;
        string file = AssetBundleUpdater.Instance.CurrentDownloadFile;
        Debug.Log($"下载 {file}: {progress * 100}%");
        yield return null;
    }
}
```

#### 3. 取消更新
```csharp
// 用户取消
AssetBundleUpdater.Instance.CancelUpdate();

// 清理临时文件
AssetBundleUpdater.Instance.ClearCache();
```

### 版本信息结构

```json
{
  "version": "1.0.0",
  "updateTime": 1672531200,
  "bundles": [
    {
      "fileName": "common_textures",
      "hash": "a1b2c3d4e5f6...",
      "size": 5242880,
      "priority": 0,
      "dependencies": []
    },
    {
      "fileName": "common_ui",
      "hash": "f6e5d4c3b2a1...",
      "size": 1048576,
      "priority": 3,
      "dependencies": ["common_textures", "common_materials"]
    }
  ]
}
```

---

## 完整工作流程

### 开发阶段

#### 1. 资源整理
```
Assets/
├── Textures/
│   └── Common/          # 公共纹理
├── Materials/
│   └── Common/          # 公共材质
├── Audio/
│   └── Common/          # 公共音频
├── Prefabs/
│   └── UI/
│       └── Common/      # 公共 UI
├── Configs/             # 配置表
└── Chess/
    ├── Resources/       # 象棋资源
    └── Prefabs/         # 象棋预制
```

#### 2. 配置 Bundle
1. 打开 `Tools/AssetBundle工具/模块化Bundle构建`
2. 为每个 Bundle 配置文件夹路径
3. 配置依赖关系（如有）
4. 设置资源类型过滤（可选）

#### 3. 设置标签
1. 点击 **设置所有Bundle标签**
2. 或逐个点击 **设置该Bundle资源标签**

#### 4. 构建 Bundle
```
构建顺序：
1. 点击 [构建公共Bundle]      # 优先构建公共资源
2. 点击 [构建模块Bundle]      # 再构建模块资源
3. 点击 [生成版本清单]         # 生成 version.json
4. 点击 [复制到StreamingAssets] # 随包发布
```

#### 5. 验证
```
1. 点击 [查看依赖顺序]         # 检查加载顺序
2. 点击 [验证依赖关系]         # 检查循环依赖
```

### 运行时加载

#### 游戏启动流程
```csharp
IEnumerator GameStartFlow() {
    // 1. 检查热更新
    AssetBundleUpdater.Instance.CheckUpdate((needUpdate, message, remoteVersion) => {
        if (needUpdate) {
            // 显示更新对话框
            ShowUpdateUI(message, remoteVersion);
        } else {
            // 直接进入游戏
            StartGameLoad();
        }
    });
    
    yield return null;
}

void StartGameLoad() {
    // 2. 加载公共 Bundle
    var commonBundles = new List<string> { 
        "common_textures", 
        "common_materials", 
        "common_ui" 
    };
    
    AssetBundleLoader.Instance.LoadBundles(commonBundles, (success, bundles) => {
        if (success) {
            // 3. 加载游戏模块
            LoadGameModule();
        }
    });
}

void LoadGameModule() {
    // 4. 加载象棋模块
    var gameBundles = new List<string> { 
        "chess_resources", 
        "chess_prefabs" 
    };
    
    AssetBundleLoader.Instance.LoadBundles(gameBundles, (success, bundles) => {
        if (success) {
            // 5. 加载具体资源
            LoadGameResources();
        }
    });
}

void LoadGameResources() {
    AssetBundleLoader.Instance.LoadAsset<GameObject>("chess_prefabs", "ChessBoard", 
        (success, prefab) => {
            if (success) {
                Instantiate(prefab);
                EnterGameScene();
            }
        });
}
```

### 发布流程

#### 1. 构建版本
```
1. 更新版本号（如 1.0.0 -> 1.0.1）
2. 点击 [构建所有Bundle]
3. 点击 [生成版本清单]
4. 点击 [复制到StreamingAssets]
```

#### 2. 上传到服务器
```
服务器目录结构：
http://your-server.com/AssetBundles/
├── version.json
├── Windows/
│   ├── common_textures
│   ├── common_ui
│   └── ...
├── Android/
│   └── ...
└── iOS/
    └── ...
```

#### 3. 更新服务器 URL
```csharp
// AssetBundleUpdater.cs
[SerializeField] private string updateServerUrl = "http://your-server.com/AssetBundles";

// AssetBundleLoader.cs
[SerializeField] private string remoteBundleUrl = "http://your-server.com/AssetBundles";
```

---

## 常见问题

### Q1: 为什么公共 Bundle 优先级是 0？
A: 优先级数值越小越先加载。公共 Bundle 被其他模块依赖，必须最先加载。

### Q2: 如何检测循环依赖？
A: 使用工具的 **验证依赖关系** 按钮，系统会自动检测并报告循环依赖。

### Q3: Bundle 依赖如何配置？
A: 点击 Bundle 的 **配置依赖** 按钮，勾选需要的依赖 Bundle。系统会自动计算加载顺序。

### Q4: 如何添加新模块？
A: 
1. 点击 **添加新Bundle**
2. 配置名称、Bundle名称、文件夹路径
3. 设置优先级（模块建议 20+）
4. 配置依赖关系

### Q5: 热更新失败如何处理？
A: 
1. 检查服务器 URL 是否正确
2. 查看日志确认具体错误
3. 调用 `AssetBundleUpdater.Instance.ClearCache()` 清理临时文件
4. 重新检查更新

### Q6: 资源类型过滤如何使用？
A: 在 Bundle 配置中，支持以下格式：
- `t:Texture2D` - 纹理
- `t:Sprite` - 精灵
- `t:Material` - 材质
- `t:AudioClip` - 音频
- `t:Prefab` - 预制
- `t:TextAsset` - 文本资源

### Q7: 如何卸载已加载的资源？
A: 
```csharp
// 卸载 Bundle 但保留对象
AssetBundleLoader.Instance.UnloadBundle("chess_prefabs", false);

// 卸载 Bundle 并卸载对象
AssetBundleLoader.Instance.UnloadBundle("chess_prefabs", true);
```

### Q8: StreamingAssets 和本地更新路径的区别？
A: 
- **StreamingAssets**: 随包发布的初始资源，只读
- **本地更新路径**: 下载的更新资源，可读写
- 加载器优先使用本地更新路径，回退到 StreamingAssets

### Q9: 如何查看 Bundle 的依赖顺序？
A: 点击 **查看依赖顺序** 按钮，会显示按优先级和依赖关系排序的加载列表。

### Q10: 断点续传如何工作？
A: 下载过程中会生成 `.tmp` 临时文件，如果下载中断，重新下载时会从断点继续。

---

## 联系与反馈

如有问题或建议，请联系开发团队。
