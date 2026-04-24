using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralWorldGenerator))]
public class ProceduralWorldGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate (Current Seed)"))
            {
                ProceduralWorldGenerator generator = (ProceduralWorldGenerator)target;
                generator.GenerateWorld(randomizeSeed: false, force: true, forceNavMeshRebuild: true);
                EditorUtility.SetDirty(generator);
            }

            if (GUILayout.Button("Generate New Random Level"))
            {
                ProceduralWorldGenerator generator = (ProceduralWorldGenerator)target;
                generator.GenerateWorld(randomizeSeed: true, force: true, forceNavMeshRebuild: true);
                EditorUtility.SetDirty(generator);
            }
        }
    }
}
