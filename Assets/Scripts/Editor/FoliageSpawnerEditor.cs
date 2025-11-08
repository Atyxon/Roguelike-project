using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FoliageSpawner))]
public class FoliageSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FoliageSpawner spawner = (FoliageSpawner)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Editor Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("🌿 Generate Foliage"))
        {
            spawner.SpawnGrass();
        }

        if (spawner.hasGenerated)
        {
            if (GUILayout.Button("🗑 Clear Foliage"))
            {
                spawner.ClearGrass();
            }
        }
    }
}