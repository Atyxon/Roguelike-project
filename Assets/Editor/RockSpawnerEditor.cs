#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RockSpawner))]
public class RockSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var spawner = (RockSpawner)target;

        GUILayout.Space(8);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🪨 Generate Rocks", GUILayout.Height(35)))
        {
            Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Generate Rocks");
            spawner.GenerateRocks();
            EditorUtility.SetDirty(spawner);
        }

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("🧹 Clear Rocks", GUILayout.Height(35)))
        {
            Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Clear Rocks");
            spawner.ClearRocks();
            EditorUtility.SetDirty(spawner);
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif
