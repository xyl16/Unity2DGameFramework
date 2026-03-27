using UnityEditor;
using UnityEngine;

/// <summary>
/// 热更新系统配置编辑器
/// </summary>
[CustomEditor(typeof(AssetBundleUpdater))]
public class AssetBundleUpdaterEditor : Editor
{
    private SerializedProperty enableAutoUpdateProp;
    private SerializedProperty updateServerUrlProp;
    private SerializedProperty localBundlePathProp;
    private SerializedProperty versionFileNameProp;
    private SerializedProperty maxRetryCountProp;
    private SerializedProperty downloadTimeoutProp;
    private SerializedProperty enableResumeProp;

    private void OnEnable()
    {
        enableAutoUpdateProp = serializedObject.FindProperty("enableAutoUpdate");
        updateServerUrlProp = serializedObject.FindProperty("updateServerUrl");
        localBundlePathProp = serializedObject.FindProperty("localBundlePath");
        versionFileNameProp = serializedObject.FindProperty("versionFileName");
        maxRetryCountProp = serializedObject.FindProperty("maxRetryCount");
        downloadTimeoutProp = serializedObject.FindProperty("downloadTimeout");
        enableResumeProp = serializedObject.FindProperty("enableResume");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader();
        EditorGUILayout.Space();
        DrawUpdateSettings();
        EditorGUILayout.Space();
        DrawDownloadSettings();
        EditorGUILayout.Space();
        DrawRuntimeInfo();
        EditorGUILayout.Space();
        DrawButtons();

        serializedObject.ApplyModifiedProperties();
    }

    private new void DrawHeader()
    {
        EditorGUILayout.LabelField("AB包热更新管理器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("负责AB包的增量更新、断点续传、文件校验等功能", MessageType.Info);
    }

    private void DrawUpdateSettings()
    {
        EditorGUILayout.LabelField("更新设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableAutoUpdateProp, new GUIContent("启用自动更新"));
        EditorGUILayout.PropertyField(updateServerUrlProp, new GUIContent("更新服务器URL"));
        EditorGUILayout.PropertyField(localBundlePathProp, new GUIContent("本地AB包路径"));
        EditorGUILayout.PropertyField(versionFileNameProp, new GUIContent("版本文件名"));
    }

    private void DrawDownloadSettings()
    {
        EditorGUILayout.LabelField("下载设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(maxRetryCountProp, new GUIContent("最大重试次数"));
        EditorGUILayout.PropertyField(downloadTimeoutProp, new GUIContent("下载超时(秒)"));
        EditorGUILayout.PropertyField(enableResumeProp, new GUIContent("启用断点续传"));
    }

    private void DrawRuntimeInfo()
    {
        AssetBundleUpdater updater = (AssetBundleUpdater)target;
        EditorGUILayout.LabelField("运行时信息", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.EnumPopup("当前状态", updater.CurrentStatus);
        EditorGUILayout.Slider("下载进度", updater.DownloadProgress, 0f, 1f);
        EditorGUILayout.TextField("当前下载文件", updater.CurrentDownloadFile);
        EditorGUILayout.TextField("本地版本", updater.GetLocalVersion());
        EditorGUILayout.TextField("远程版本", updater.GetRemoteVersion());
        EditorGUI.EndDisabledGroup();
    }

    private void DrawButtons()
    {
        EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

        if (GUILayout.Button("打开AB包构建工具"))
        {
            AssetBundleBuilder.ShowWindow();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("清理缓存"))
        {
            AssetBundleUpdater updater = (AssetBundleUpdater)target;
            if (updater != null)
            {
                updater.ClearCache();
            }
        }

        if (GUILayout.Button("刷新编辑器"))
        {
            AssetDatabase.Refresh();
        }
        EditorGUILayout.EndHorizontal();
    }
}

[CustomEditor(typeof(UpdateController))]
public class UpdateControllerEditor : Editor
{
    private SerializedProperty checkUpdateOnStartProp;
    private SerializedProperty autoDownloadUpdateProp;
    private SerializedProperty updateSceneNameProp;

    private void OnEnable()
    {
        checkUpdateOnStartProp = serializedObject.FindProperty("checkUpdateOnStart");
        autoDownloadUpdateProp = serializedObject.FindProperty("autoDownloadUpdate");
        updateSceneNameProp = serializedObject.FindProperty("updateSceneName");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader();
        EditorGUILayout.Space();
        DrawSettings();
        EditorGUILayout.Space();
        DrawRuntimeInfo();

        serializedObject.ApplyModifiedProperties();
    }

    private new void DrawHeader()
    {
        EditorGUILayout.LabelField("更新控制器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("负责协调各个更新相关模块,提供统一的更新流程", MessageType.Info);
    }

    private void DrawSettings()
    {
        EditorGUILayout.LabelField("设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(checkUpdateOnStartProp, new GUIContent("启动时检查更新"));
        EditorGUILayout.PropertyField(autoDownloadUpdateProp, new GUIContent("自动下载更新"));
        EditorGUILayout.PropertyField(updateSceneNameProp, new GUIContent("更新场景名称"));
    }

    private void DrawRuntimeInfo()
    {
        UpdateController controller = (UpdateController)target;
        EditorGUILayout.LabelField("运行时信息", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.EnumPopup("当前状态", controller.GetState());
        EditorGUILayout.Toggle("需要更新", controller.IsUpdateRequired());

        UpdateController.UpdateInfo updateInfo = controller.GetUpdateInfo();
        if (updateInfo != null)
        {
            EditorGUILayout.TextField("当前版本", updateInfo.currentVersion);
            EditorGUILayout.TextField("远程版本", updateInfo.remoteVersion);
            EditorGUILayout.TextField("更新消息", updateInfo.message);
        }

        EditorGUILayout.Slider("下载进度", controller.GetDownloadProgress(), 0f, 1f);
        EditorGUILayout.TextField("当前下载文件", controller.GetCurrentDownloadFile());
        EditorGUI.EndDisabledGroup();
    }
}
