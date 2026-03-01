using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralIsland))]
public class ProceduralIslandEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ProceduralIsland island = (ProceduralIsland)target;

        GUILayout.Space(10);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🌍 Generate Island (Random Seed)", GUILayout.Height(40)))
        {
            Undo.RecordObject(island, "Generate Island");

            island.RandomizeSeed();
            island.Generate();

            EditorUtility.SetDirty(island); // żeby zapisało seed w inspectorze
        }


        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("🧹 Clear Island", GUILayout.Height(30)))
        {
            island.Clear();
        }

        GUI.backgroundColor = Color.white;
    }
}
