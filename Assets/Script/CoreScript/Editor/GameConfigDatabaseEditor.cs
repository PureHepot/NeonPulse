using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameConfigDatabase))]
public class GameConfigDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "StatDefinitions are auto-synced from project assets and schema/module references. Use the button below to refresh all config lists.",
            MessageType.Info);

        if (GUILayout.Button("Auto Collect All Configs"))
        {
            var database = (GameConfigDatabase)target;
            Undo.RecordObject(database, "Auto Collect Game Config Assets");
            database.AutoCollectConfigAssets();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }
}
