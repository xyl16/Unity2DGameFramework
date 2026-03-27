using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// AB包标签编辑器工具
/// 方便地为资源设置AB包标签
/// </summary>
public class AssetBundleLabeler : EditorWindow
{
    [MenuItem("Tools/AssetBundle工具/AssetBundle标记工具")]
    public static void ShowWindow()
    {
        GetWindow<AssetBundleLabeler>("AB包标签设置");
    }

    private string bundleName = "";
    private string variant = "";
    private Vector2 scrollPosition;
    private List<string> selectedPaths = new List<string>();

    private void OnGUI()
    {
        GUILayout.Label("AB包标签设置工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 输入框
        EditorGUILayout.LabelField("设置标签", EditorStyles.boldLabel);
        bundleName = EditorGUILayout.TextField("Bundle名称:", bundleName);
        variant = EditorGUILayout.TextField("变体:", variant);

        EditorGUILayout.Space();

        // 按钮操作
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("设置选中资源", GUILayout.Height(30)))
        {
            SetSelectedAssets();
        }

        if (GUILayout.Button("批量设置文件夹", GUILayout.Height(30)))
        {
            SetFolderAssets();
        }

        if (GUILayout.Button("清除标签", GUILayout.Height(30)))
        {
            ClearSelectedAssets();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 快捷预设
        EditorGUILayout.LabelField("快捷预设", EditorStyles.boldLabel);
        DrawPresets();

        EditorGUILayout.Space();

        // 资源列表
        EditorGUILayout.LabelField("选中的资源", EditorStyles.boldLabel);
        DrawSelectedAssets();

        EditorGUILayout.Space();

        // AB包列表
        EditorGUILayout.LabelField("所有AB包", EditorStyles.boldLabel);
        DrawAllBundles();
    }

    /// <summary>
    /// 设置选中资源的AB包标签
    /// </summary>
    private void SetSelectedAssets()
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            EditorUtility.DisplayDialog("错误", "请输入Bundle名称!", "确定");
            return;
        }

        Object[] selectedObjects = Selection.objects;
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选中资源!", "确定");
            return;
        }

        int count = 0;
        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer != null)
                {
                    importer.assetBundleName = bundleName;
                    importer.assetBundleVariant = variant;
                    count++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成", $"已设置 {count} 个资源的AB包标签!", "确定");
        Debug.Log($"设置了 {count} 个资源的AB包标签: {bundleName}");
    }

    /// <summary>
    /// 批量设置文件夹中的资源
    /// </summary>
    private void SetFolderAssets()
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            EditorUtility.DisplayDialog("错误", "请输入Bundle名称!", "确定");
            return;
        }

        string folderPath = EditorUtility.OpenFolderPanel("选择文件夹", "Assets", "");
        if (string.IsNullOrEmpty(folderPath))
        {
            return;
        }

        // 转换为Unity相对路径
        if (folderPath.Contains(Application.dataPath))
        {
            folderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);
        }
        else
        {
            EditorUtility.DisplayDialog("错误", "请选择Assets文件夹下的文件夹!", "确定");
            return;
        }

        int count = 0;
        string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null && !AssetDatabase.IsValidFolder(path))
            {
                importer.assetBundleName = bundleName;
                importer.assetBundleVariant = variant;
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成", $"已设置 {count} 个资源的AB包标签!", "确定");
        Debug.Log($"设置了 {count} 个资源的AB包标签: {bundleName}");
    }

    /// <summary>
    /// 清除选中资源的AB包标签
    /// </summary>
    private void ClearSelectedAssets()
    {
        Object[] selectedObjects = Selection.objects;
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选中资源!", "确定");
            return;
        }

        int count = 0;
        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer != null && !string.IsNullOrEmpty(importer.assetBundleName))
                {
                    importer.assetBundleName = "";
                    importer.assetBundleVariant = "";
                    count++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成", $"已清除 {count} 个资源的AB包标签!", "确定");
        Debug.Log($"清除了 {count} 个资源的AB包标签");
    }

    /// <summary>
    /// 绘制快捷预设
    /// </summary>
    private void DrawPresets()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("预制件", GUILayout.Width(80)))
        {
            bundleName = "prefabs";
            variant = "";
        }
        if (GUILayout.Button("材质", GUILayout.Width(80)))
        {
            bundleName = "materials";
            variant = "";
        }
        if (GUILayout.Button("纹理", GUILayout.Width(80)))
        {
            bundleName = "textures";
            variant = "";
        }
        if (GUILayout.Button("音频", GUILayout.Width(80)))
        {
            bundleName = "audio";
            variant = "";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("UI预制", GUILayout.Width(80)))
        {
            bundleName = "ui_prefabs";
            variant = "";
        }
        if (GUILayout.Button("UI纹理", GUILayout.Width(80)))
        {
            bundleName = "ui_textures";
            variant = "";
        }
        if (GUILayout.Button("场景", GUILayout.Width(80)))
        {
            bundleName = "scenes";
            variant = "";
        }
        if (GUILayout.Button("数据", GUILayout.Width(80)))
        {
            bundleName = "data";
            variant = "";
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制选中的资源
    /// </summary>
    private void DrawSelectedAssets()
    {
        Object[] selectedObjects = Selection.objects;
        if (selectedObjects.Length == 0)
        {
            EditorGUILayout.HelpBox("未选中任何资源", MessageType.Info);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                string bundleLabel = string.IsNullOrEmpty(importer.assetBundleName)
                    ? "(无标签)"
                    : $"{importer.assetBundleName}";

                if (!string.IsNullOrEmpty(importer.assetBundleVariant))
                {
                    bundleLabel += $".{importer.assetBundleVariant}";
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(obj.name, GUILayout.Width(200));
                GUILayout.Label(bundleLabel);
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制所有AB包
    /// </summary>
    private void DrawAllBundles()
    {
        var bundles = AssetDatabase.GetAllAssetBundleNames();
        if (bundles.Length == 0)
        {
            EditorGUILayout.HelpBox("没有设置任何AB包标签", MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox($"共 {bundles.Length} 个AB包", MessageType.Info);

        foreach (string bundle in bundles)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(bundle);

            if (GUILayout.Button("查看资源", GUILayout.Width(80)))
            {
                ShowBundleAssets(bundle);
            }

            if (GUILayout.Button("清除", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("确认", $"确定要清除AB包标签: {bundle} ?", "确定", "取消"))
                {
                    ClearBundle(bundle);
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 显示AB包中的资源
    /// </summary>
    private void ShowBundleAssets(string bundleName)
    {
        string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
        if (assetPaths.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", $"AB包 {bundleName} 中没有资源", "确定");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"AB包: {bundleName}");
        sb.AppendLine($"资源数量: {assetPaths.Length}");
        sb.AppendLine("\n资源列表:");

        foreach (string path in assetPaths)
        {
            string fileName = System.IO.Path.GetFileName(path);
            sb.AppendLine($"  - {fileName}");
        }

        EditorUtility.DisplayDialog("AB包资源", sb.ToString(), "确定");
    }

    /// <summary>
    /// 清除AB包标签
    /// </summary>
    private void ClearBundle(string bundleName)
    {
        string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
        foreach (string path in assetPaths)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.assetBundleName = "";
                importer.assetBundleVariant = "";
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成", $"已清除AB包标签: {bundleName}", "确定");
        Debug.Log($"清除了AB包标签: {bundleName}");
    }
}
