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

            // Mark scene dirty so changes are saved
            EditorUtility.SetDirty(spawner);
        }

        // NEW: Use savedGrass.Count instead of hasGenerated
        if (spawner != null && spawner.SavedGrassCount > 0)
        {
            if (GUILayout.Button("🗑 Clear Foliage"))
            {
                spawner.ClearGrass();

                // Mark scene dirty so clearing is saved
                EditorUtility.SetDirty(spawner);
            }
        }
    }
}