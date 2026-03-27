using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// 文件校验工具: 提供文件完整性检查功能
/// </summary>
public class FileVerification
{
    /// <summary>
    /// 计算文件MD5哈希
    /// </summary>
    public static string CalculateMD5(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Logger.Instance.LogError($"文件不存在: {filePath}", "FileVerification");
                return null;
            }

            using (MD5 md5 = MD5.Create())
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hash)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"计算MD5失败: {filePath}, 错误: {e.Message}", "FileVerification");
            return null;
        }
    }

    /// <summary>
    /// 计算文件SHA1哈希
    /// </summary>
    public static string CalculateSHA1(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Logger.Instance.LogError($"文件不存在: {filePath}", "FileVerification");
                return null;
            }

            using (SHA1 sha1 = SHA1.Create())
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    byte[] hash = sha1.ComputeHash(stream);
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hash)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"计算SHA1失败: {filePath}, 错误: {e.Message}", "FileVerification");
            return null;
        }
    }

    /// <summary>
    /// 计算文件SHA256哈希
    /// </summary>
    public static string CalculateSHA256(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Logger.Instance.LogError($"文件不存在: {filePath}", "FileVerification");
                return null;
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    byte[] hash = sha256.ComputeHash(stream);
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hash)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"计算SHA256失败: {filePath}, 错误: {e.Message}", "FileVerification");
            return null;
        }
    }

    /// <summary>
    /// 验证文件哈希
    /// </summary>
    public static bool VerifyFileHash(string filePath, string expectedHash, string algorithm = "MD5")
    {
        string actualHash = null;

        switch (algorithm.ToUpper())
        {
            case "MD5":
                actualHash = CalculateMD5(filePath);
                break;
            case "SHA1":
                actualHash = CalculateSHA1(filePath);
                break;
            case "SHA256":
                actualHash = CalculateSHA256(filePath);
                break;
            default:
                Logger.Instance.LogError($"不支持的哈希算法: {algorithm}", "FileVerification");
                return false;
        }

        if (actualHash == null)
        {
            return false;
        }

        bool valid = actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);

        if (!valid)
        {
            Logger.Instance.LogWarning($"文件哈希验证失败: {filePath}, 预期: {expectedHash}, 实际: {actualHash}", "FileVerification");
        }

        return valid;
    }

    /// <summary>
    /// 验证文件大小
    /// </summary>
    public static bool VerifyFileSize(string filePath, long expectedSize)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Logger.Instance.LogError($"文件不存在: {filePath}", "FileVerification");
                return false;
            }

            long actualSize = new FileInfo(filePath).Length;
            bool valid = actualSize == expectedSize;

            if (!valid)
            {
                Logger.Instance.LogWarning($"文件大小验证失败: {filePath}, 预期: {expectedSize}, 实际: {actualSize}", "FileVerification");
            }

            return valid;
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"验证文件大小失败: {filePath}, 错误: {e.Message}", "FileVerification");
            return false;
        }
    }

    /// <summary>
    /// 验证文件完整性(哈希+大小)
    /// </summary>
    public static bool VerifyFileIntegrity(string filePath, string expectedHash, long expectedSize, string algorithm = "MD5")
    {
        bool hashValid = VerifyFileHash(filePath, expectedHash, algorithm);
        bool sizeValid = VerifyFileSize(filePath, expectedSize);

        return hashValid && sizeValid;
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    public static FileInfo GetFileInfo(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Logger.Instance.LogError($"文件不存在: {filePath}", "FileVerification");
                return null;
            }

            FileInfo info = new FileInfo(filePath);
            Logger.Instance.LogInfo($"文件信息: {info.Name}, 大小: {info.Length} bytes, 修改时间: {info.LastWriteTime}", "FileVerification");
            return info;
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"获取文件信息失败: {filePath}, 错误: {e.Message}", "FileVerification");
            return null;
        }
    }

    /// <summary>
    /// 计算目录中所有文件的哈希
    /// </summary>
    public static System.Collections.Generic.Dictionary<string, string> CalculateDirectoryHash(string directoryPath, string searchPattern = "*")
    {
        var fileHashes = new System.Collections.Generic.Dictionary<string, string>();

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Logger.Instance.LogError($"目录不存在: {directoryPath}", "FileVerification");
                return fileHashes;
            }

            string[] files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);
            Logger.Instance.LogInfo($"开始计算目录哈希: {directoryPath}, 文件数: {files.Length}", "FileVerification");

            foreach (string file in files)
            {
                string hash = CalculateMD5(file);
                if (hash != null)
                {
                    string relativePath = file.Substring(directoryPath.Length).TrimStart(Path.DirectorySeparatorChar);
                    fileHashes[relativePath] = hash;
                }
            }

            Logger.Instance.LogInfo($"目录哈希计算完成: {fileHashes.Count} 个文件", "FileVerification");
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"计算目录哈希失败: {directoryPath}, 错误: {e.Message}", "FileVerification");
        }

        return fileHashes;
    }

    /// <summary>
    /// 比较两个目录的差异
    /// </summary>
    public static DirectoryDiff CompareDirectories(string dir1, string dir2, string searchPattern = "*")
    {
        DirectoryDiff diff = new DirectoryDiff();

        try
        {
            if (!Directory.Exists(dir1))
            {
                Logger.Instance.LogError($"目录不存在: {dir1}", "FileVerification");
                return diff;
            }

            if (!Directory.Exists(dir2))
            {
                Logger.Instance.LogError($"目录不存在: {dir2}", "FileVerification");
                return diff;
            }

            var hash1 = CalculateDirectoryHash(dir1, searchPattern);
            var hash2 = CalculateDirectoryHash(dir2, searchPattern);

            // 查找新增文件
            foreach (var kvp in hash2)
            {
                if (!hash1.ContainsKey(kvp.Key))
                {
                    diff.AddedFiles.Add(kvp.Key);
                }
            }

            // 查找删除文件
            foreach (var kvp in hash1)
            {
                if (!hash2.ContainsKey(kvp.Key))
                {
                    diff.RemovedFiles.Add(kvp.Key);
                }
            }

            // 查找修改文件
            foreach (var kvp in hash1)
            {
                if (hash2.ContainsKey(kvp.Key) && hash2[kvp.Key] != kvp.Value)
                {
                    diff.ModifiedFiles.Add(kvp.Key);
                }
            }

            Logger.Instance.LogInfo($"目录比较完成: 新增 {diff.AddedFiles.Count}, 修改 {diff.ModifiedFiles.Count}, 删除 {diff.RemovedFiles.Count}", "FileVerification");
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"比较目录失败: {dir1} vs {dir2}, 错误: {e.Message}", "FileVerification");
        }

        return diff;
    }

    /// <summary>
    /// 目录差异结果
    /// </summary>
    public class DirectoryDiff
    {
        public System.Collections.Generic.List<string> AddedFiles = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> ModifiedFiles = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> RemovedFiles = new System.Collections.Generic.List<string>();

        public bool HasChanges
        {
            get { return AddedFiles.Count > 0 || ModifiedFiles.Count > 0 || RemovedFiles.Count > 0; }
        }

        public void Clear()
        {
            AddedFiles.Clear();
            ModifiedFiles.Clear();
            RemovedFiles.Clear();
        }
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}
