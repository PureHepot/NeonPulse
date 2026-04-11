using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 存档读写服务（纯静态，无 MonoBehaviour 依赖）
/// 存档路径: Application.persistentDataPath/save.json
/// </summary>
public static class SaveService
{
    private const string FileName = "save.json";
    private const string TempFileName = "save.json.tmp";

    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
    private static string TempPath => Path.Combine(Application.persistentDataPath, TempFileName);

    /// <summary>
    /// 将 SaveRoot 序列化写入磁盘（先写临时文件再 rename，防止写入中断导致损坏）
    /// </summary>
    public static void Save(SaveRoot data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(TempPath, json);

            // 原子替换：删除旧文件，重命名临时文件
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            File.Move(TempPath, SavePath);

            Debug.Log($"[SaveService] 存档成功: {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveService] 存档失败: {e.Message}");
        }
    }

    /// <summary>
    /// 从磁盘读取存档，不存在或失败时返回 null
    /// </summary>
    public static SaveRoot Load()
    {
        if (!File.Exists(SavePath))
            return null;

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveRoot data = JsonUtility.FromJson<SaveRoot>(json);
            MigrateIfNeeded(data);
            Debug.Log("[SaveService] 读档成功");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveService] 读档失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 删除存档文件
    /// </summary>
    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[SaveService] 存档已删除");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveService] 删除存档失败: {e.Message}");
        }
    }

    /// <summary>
    /// 是否存在存档文件
    /// </summary>
    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    /// <summary>
    /// 版本迁移（预留）
    /// </summary>
    private static void MigrateIfNeeded(SaveRoot data)
    {
        if (data == null) return;

        // 当前版本为 1，暂无需迁移
        // 未来示例：
        // if (data.version < 2) { MigrateV1ToV2(data); data.version = 2; }
    }
}
