using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class CSVManager : Singleton<CSVManager>
{
    // 缓存所有表格数据
    // 结构：表名 -> ( 行ID -> ( 列名 -> 内容 ) )
    private Dictionary<string, Dictionary<string, Dictionary<string, string>>> tableCache =
        new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

    private const string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    private const string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    private char[] TRIM_CHARS = { '\"' };

    /// <summary>
    /// 加载并解析一个 CSV 表格
    /// </summary>
    /// <param name="csvName">Resources 文件夹下的文件名（不带后缀）</param>
    public void LoadTable(string csvName)
    {
        if (tableCache.ContainsKey(csvName)) return;

        TextAsset data = Resources.Load<TextAsset>($"Text/{csvName}");
        if (data == null)
        {
            Debug.LogError($"CSV加载失败: 找不到文件 Resources/{csvName}");
            return;
        }

        var tableData = ParseCSV(data.text);
        tableCache.Add(csvName, tableData);

        Debug.Log($"表格 [{csvName}] 加载完毕，共 {tableData.Count} 条数据。");
    }

    /// <summary>
    /// 核心解析逻辑
    /// </summary>
    private Dictionary<string, Dictionary<string, string>> ParseCSV(string text)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();

        var lines = Regex.Split(text, LINE_SPLIT_RE);
        if (lines.Length <= 1) return result;

        var headers = Regex.Split(lines[0], SPLIT_RE);

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = Regex.Split(line, SPLIT_RE);
            if (values.Length == 0 || string.IsNullOrEmpty(values[0])) continue;

            string id = values[0];
            if (id.Contains('/')) continue; // 如果第一列是注释行跳过）

            var entry = new Dictionary<string, string>();

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                string value = values[j];
                // 去除可能存在的引号，并把 Excel 的双引号转义还原
                value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\"\"", "\"");

                entry[headers[j]] = value;
            }

            if (!result.ContainsKey(id))
            {
                result.Add(id, entry);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取某张表、某一行、某一列的值（返回字符串）
    /// </summary>
    public string GetValue(string tableName, string id, string key)
    {
        if (!tableCache.ContainsKey(tableName)) LoadTable(tableName);

        if (tableCache[tableName].TryGetValue(id, out var row))
        {
            if (row.TryGetValue(key, out var val))
            {
                return val;
            }
        }

        Debug.LogWarning($"CSV查询失败: [{tableName}] ID:{id} Key:{key} 不存在");
        return "";
    }

    /// <summary>
    /// 获取整数值
    /// </summary>
    public int GetInt(string tableName, string id, string key, int defaultValue = 0)
    {
        string val = GetValue(tableName, id, key);
        if (string.IsNullOrEmpty(val)) return defaultValue;
        return int.TryParse(val, out int result) ? result : defaultValue;
    }

    /// <summary>
    /// 获取浮点数值
    /// </summary>
    public float GetFloat(string tableName, string id, string key, float defaultValue = 0f)
    {
        string val = GetValue(tableName, id, key);
        if (string.IsNullOrEmpty(val)) return defaultValue;
        return float.TryParse(val, out float result) ? result : defaultValue;
    }

    /// <summary>
    /// 获取一整行数据
    /// </summary>
    public Dictionary<string, string> GetRow(string tableName, string id)
    {
        if (!tableCache.ContainsKey(tableName)) LoadTable(tableName);

        if (tableCache[tableName].ContainsKey(id))
        {
            return tableCache[tableName][id];
        }
        return null;
    }

    /// <summary>
    /// 清理内存
    /// </summary>
    public void UnloadTable(string tableName)
    {
        if (tableCache.ContainsKey(tableName)) tableCache.Remove(tableName);
    }
}
