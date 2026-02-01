using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveDebugger : MonoBehaviour
{
    private bool showDebug = true;

    void Update()
    {
        // 按 F3 切换显示/隐藏
        if (Input.GetKeyDown(KeyCode.F3))
            showDebug = !showDebug;
    }

    void OnGUI()
    {
        if (!showDebug) return;

        // 设置左上角的样式
        GUIStyle style = new GUIStyle();
        style.fontSize = 25; // 字体大一点，方便看
        style.normal.textColor = Color.yellow;
        style.fontStyle = FontStyle.Bold;

        GUILayout.BeginArea(new Rect(10, 10, 600, 500));

        if (WaveManager.Instance == null)
        {
            GUILayout.Label("WaveManager Instance is NULL!", style);
        }
        else
        {
            var wm = WaveManager.Instance;

            GUILayout.Label($"=== WAVE DEBUG SYSTEM ===", style);
            // 这里我们无法直接访问 private 变量，
            // 建议你把 WaveManager 里的 activeEnemies 和 currentWaveIndex 暂时改为 public，
            // 或者写一个 public 属性来获取它们。
            // 假设你已经把 activeEnemies 改为 public 或者写了 Get 方法：

            GUILayout.Label($"Active Enemies Count: {wm.activeEnemies.Count}", style);

            // 如果列表里有怪，列出前几个的名字，看看是不是幽灵怪
            if (wm.activeEnemies.Count > 0)
            {
                style.fontSize = 15;
                GUILayout.Label("List Content:", style);
                int count = 0;
                foreach (var enemy in wm.activeEnemies)
                {
                    if (count > 5) break; // 只显示前5个
                    string info = enemy == null ? "NULL" : $"{enemy.name} (HP:{enemy.currentHp})";
                    GUILayout.Label($" - {info}", style);
                    count++;
                }
                style.fontSize = 25;
            }
        }

        GUILayout.EndArea();
    }
}
