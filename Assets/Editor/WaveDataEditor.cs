using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(WaveManager))]
public class WaveManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制默认的 Inspector
        DrawDefaultInspector();

        // 添加帮助信息
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("在 Waves Data 中配置波次数据", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}

// 为 WaveData 创建自定义 Editor
[CustomPropertyDrawer(typeof(WaveData))]
public class WaveDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 获取属性
        SerializedProperty waveNameProp = property.FindPropertyRelative("waveName");
        SerializedProperty enemiesProp = property.FindPropertyRelative("enemies");
        SerializedProperty groupsProp = property.FindPropertyRelative("groups");
        SerializedProperty waveDurationProp = property.FindPropertyRelative("waveDuration");

        // 绘制折叠框
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded, label);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float yOffset = position.y + EditorGUIUtility.singleLineHeight;

            // 波次名称
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight),
                waveNameProp);
            yOffset += EditorGUIUtility.singleLineHeight;

            // 敌人列表
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight),
                enemiesProp);
            yOffset += EditorGUI.GetPropertyHeight(enemiesProp);

            // 波次持续时间
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight),
                waveDurationProp);
            yOffset += EditorGUIUtility.singleLineHeight;

            // 组列表
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, EditorGUI.GetPropertyHeight(groupsProp)),
                groupsProp);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        float height = EditorGUIUtility.singleLineHeight; // Foldout
        height += EditorGUIUtility.singleLineHeight; // waveName
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("enemies"));
        height += EditorGUIUtility.singleLineHeight; // waveDuration
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("groups"));

        return height;
    }
}

// 为 WaveGroup 创建自定义 Drawer，实现下拉菜单
[CustomPropertyDrawer(typeof(WaveGroup))]
public class WaveGroupDrawer : PropertyDrawer
{
    private bool showEnemyList = true;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 获取属性
        SerializedProperty enemyEntriesProp = property.FindPropertyRelative("enemyEntries");
        SerializedProperty groupDurationProp = property.FindPropertyRelative("groupDuration");
        SerializedProperty directionProp = property.FindPropertyRelative("direction");

        // 获取父级 WaveData 的 enemies 列表
        SerializedProperty waveDataProp = GetParentWaveData(property);
        SerializedProperty enemiesProp = waveDataProp?.FindPropertyRelative("enemies");

        // 绘制折叠框
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded, label);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float yOffset = position.y + EditorGUIUtility.singleLineHeight;

            // Group Duration
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight),
                groupDurationProp);
            yOffset += EditorGUIUtility.singleLineHeight;

            // Direction
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight),
                directionProp);
            yOffset += EditorGUIUtility.singleLineHeight;

            // 敌人配置列表
            if (enemiesProp != null && enemiesProp.arraySize > 0)
            {
                // 显示敌人列表标题
                EditorGUI.LabelField(new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight),
                    "敌人配置", EditorStyles.boldLabel);
                yOffset += EditorGUIUtility.singleLineHeight;

                // 绘制每个 EnemyEntry
                for (int i = 0; i < enemyEntriesProp.arraySize; i++)
                {
                    SerializedProperty entryProp = enemyEntriesProp.GetArrayElementAtIndex(i);
                    SerializedProperty enemyIndexProp = entryProp.FindPropertyRelative("enemyIndex");
                    SerializedProperty spawnRateProp = entryProp.FindPropertyRelative("spawnRate");

                    // 计算布局 - 为"生成速率"标签留出空间
                    float labelWidth = 35f; // "敌人"标签宽度
                    float enemyWidth = (position.width - labelWidth - 70f) * 0.5f; // 敌人下拉菜单宽度
                    float rateLabelWidth = 55f; // "生成速率"标签宽度
                    float rateWidth = (position.width - labelWidth - enemyWidth - rateLabelWidth - 10f); // 生成速率输入框宽度

                    // 敌人标签
                    Rect labelRect = new Rect(position.x, yOffset, labelWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(labelRect, "敌人");

                    // 敌人下拉菜单
                    Rect indexRect = new Rect(position.x + labelWidth, yOffset, enemyWidth, EditorGUIUtility.singleLineHeight);

                    // 生成速率标签
                    Rect rateLabelRect = new Rect(position.x + labelWidth + enemyWidth + 5, yOffset, rateLabelWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(rateLabelRect, "生成速率");

                    // 生成速率输入框
                    Rect rateRect = new Rect(position.x + labelWidth + enemyWidth + rateLabelWidth + 10, yOffset, rateWidth, EditorGUIUtility.singleLineHeight);

                    // 获取敌人名称列表
                    string[] enemyNames = new string[enemiesProp.arraySize];
                    for (int j = 0; j < enemiesProp.arraySize; j++)
                    {
                        GameObject enemyObj = enemiesProp.GetArrayElementAtIndex(j).objectReferenceValue as GameObject;
                        enemyNames[j] = enemyObj != null ? enemyObj.name : $"Enemy {j}";
                    }

                    // 绘制下拉菜单
                    int currentIndex = enemyIndexProp.intValue;
                    if (currentIndex >= enemiesProp.arraySize)
                        currentIndex = 0;

                    EditorGUI.BeginChangeCheck();
                    int newIndex = EditorGUI.Popup(indexRect, currentIndex, enemyNames);
                    if (EditorGUI.EndChangeCheck())
                    {
                        enemyIndexProp.intValue = newIndex;
                    }

                    // 绘制生成速率
                    EditorGUI.PropertyField(rateRect, spawnRateProp, GUIContent.none);

                    yOffset += EditorGUIUtility.singleLineHeight;
                }

                // 添加/删除按钮
                Rect buttonRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(buttonRect, "添加敌人配置"))
                {
                    enemyEntriesProp.arraySize++;
                    SerializedProperty newEntry = enemyEntriesProp.GetArrayElementAtIndex(enemyEntriesProp.arraySize - 1);
                    newEntry.FindPropertyRelative("enemyIndex").intValue = 0;
                    newEntry.FindPropertyRelative("spawnRate").floatValue = 1f;
                }

                yOffset += EditorGUIUtility.singleLineHeight;

                // 删除按钮
                if (enemyEntriesProp.arraySize > 0)
                {
                    Rect deleteRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                    if (GUI.Button(deleteRect, "删除最后一个敌人配置"))
                    {
                        enemyEntriesProp.arraySize--;
                    }
                }
            }
            else
            {
                EditorGUI.HelpBox(new Rect(position.x, yOffset, position.width, 40),
                    "请先在 WaveData 中添加敌人预制体", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        float height = EditorGUIUtility.singleLineHeight; // Foldout
        height += EditorGUIUtility.singleLineHeight; // groupDuration
        height += EditorGUIUtility.singleLineHeight; // direction
        height += EditorGUIUtility.singleLineHeight; // 标题

        SerializedProperty enemyEntriesProp = property.FindPropertyRelative("enemyEntries");
        height += enemyEntriesProp.arraySize * EditorGUIUtility.singleLineHeight;

        height += EditorGUIUtility.singleLineHeight; // 添加按钮
        if (enemyEntriesProp.arraySize > 0)
            height += EditorGUIUtility.singleLineHeight; // 删除按钮

        return height;
    }

    private SerializedProperty GetParentWaveData(SerializedProperty property)
    {
        // 向上查找 WaveData
        var current = property.GetParent();
        while (current != null && current.type != "WaveData")
        {
            current = current.GetParent();
        }
        return current;
    }
}

// 扩展方法：获取父级 SerializedProperty
public static class SerializedPropertyExtensions
{
    public static SerializedProperty GetParent(this SerializedProperty prop)
    {
        string propertyPath = prop.propertyPath;
        if (string.IsNullOrEmpty(propertyPath))
            return null;

        string[] parts = propertyPath.Split('.');
        if (parts.Length == 1)
            return null;

        string parentPath = string.Join(".", parts, 0, parts.Length - 1);
        return prop.serializedObject.FindProperty(parentPath);
    }
}