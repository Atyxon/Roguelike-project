#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TreeSpawner))]
public class TreeSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var spawner = (TreeSpawner)target;

        GUILayout.Space(8);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🌲 Generate Trees", GUILayout.Height(35)))
        {
            Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Generate Trees");
            spawner.GenerateTrees();
            EditorUtility.SetDirty(spawner);
        }

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("🧹 Clear Trees", GUILayout.Height(35)))
        {
            Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Clear Trees");
            spawner.ClearTrees();
            EditorUtility.SetDirty(spawner);
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif
