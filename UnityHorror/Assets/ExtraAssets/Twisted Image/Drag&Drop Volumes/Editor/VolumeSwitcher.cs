using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace SetaVolumeSwitcher
{
    public class VolumeSwitcher : EditorWindow
    {
        private enum VolumeTab{All,Realistic,Stylized}
        private VolumeTab currentTab = VolumeTab.All;
        private List<GameObject> prefabs = new List<GameObject>();
        private Dictionary<GameObject, Texture2D> prefabThumbnails = new Dictionary<GameObject, Texture2D>();
        private GameObject currentInstance = null;
        private Vector2 prefabScrollPosition;

        [MenuItem("Tools/Seta/Volume Switcher")]
        public static void ShowWindow()
        {
            GetWindow<VolumeSwitcher>("Volume Switcher");
        }

        private void OnEnable()
        {
            LoadPrefabs();
            CheckExistingPrefab();
        }

        private void LoadPrefabs()
        {
            prefabs.Clear();
            prefabThumbnails.Clear();
            string folderPath = "Assets/Twisted Image/Drag&Drop Volumes/Prefab";
            LoadSpecificPrefabs(folderPath);
            string folderPathBonus = "Assets/Twisted Image/Drag&Drop Volumes/Prefab/Bonus Volume";
            LoadPrefabsFromFolder(folderPathBonus);
        }

        private void LoadSpecificPrefabs(string folderPath)
        {
            string[] prefabNames = { "Dawn.prefab", "Dusk.prefab", "Day.prefab", "Night.prefab" };

            foreach (var prefabName in prefabNames)
            {
                string prefabPath = folderPath + "/" + prefabName;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (prefab != null)
                {
                    prefabs.Add(prefab);
                    LoadThumbnail(prefab, prefabPath);
                }
            }
        }

        private void LoadPrefabsFromFolder(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                    LoadThumbnail(prefab, path);
                }
            }
        }

        private void LoadThumbnail(GameObject prefab, string prefabPath)
        {
            string thumbnailPath = prefabPath.Replace(".prefab", ".jpg");
            Texture2D thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(thumbnailPath);

            if (thumbnail != null)
            {
                prefabThumbnails[prefab] = thumbnail;
            }
        }

        private void CheckExistingPrefab()
        {
            foreach (var prefab in prefabs)
            {
                GameObject existingPrefab = GameObject.Find(prefab.name);
                if (existingPrefab != null)
                {
                    currentInstance = existingPrefab;
                    break;
                }
            }
        }

        private void OnGUI()
        {
            GUIStyle redStyle = new GUIStyle(EditorStyles.boldLabel);
            redStyle.normal.textColor = Color.red;
            redStyle.hover.textColor = Color.red;

            GUILayout.Label("Before Using for the First Time:", redStyle);
            GUILayout.Label("1. Remove the Directional Light and any other global volumes from your scene.", EditorStyles.wordWrappedLabel);
            GUILayout.Label("2. Enable Volumetric Clouds, and if you want SSGI:", EditorStyles.wordWrappedLabel);
            GUILayout.Label("- Go to Edit > Project Settings > Quality > HDRP.", EditorStyles.wordWrappedLabel);
            GUILayout.Label("- Enable Screen Space Global Illumination and Volumetric Clouds.", EditorStyles.wordWrappedLabel);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);
            currentTab = (VolumeTab)GUILayout.Toolbar((int)currentTab,new string[] { "All", "Realistic", "Stylized" });
            GUILayout.Space(10);
            GUILayout.Label("Select Volume:", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUILayout.Height(600));
            prefabScrollPosition = GUILayout.BeginScrollView(prefabScrollPosition, GUILayout.ExpandHeight(true));

            float windowWidth = position.width;
            int columns = Mathf.Max(1, (int)(windowWidth / 160));
            int index = 0;
            float totalWidth = columns * 160;
            float horizontalPadding = Mathf.Max(0, (windowWidth - totalWidth) / 2);

            GUILayout.BeginHorizontal();
            GUILayout.Space(horizontalPadding);

            foreach (var prefab in prefabs)
            {
                if (prefab == null)
                    continue;

                bool isStylized = prefab.name.StartsWith("Stylized");

                if (currentTab == VolumeTab.Realistic && isStylized)
                    continue;

                if (currentTab == VolumeTab.Stylized && !isStylized)
                    continue;

                GUILayout.BeginVertical(GUILayout.Width(160));
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (prefabThumbnails.TryGetValue(prefab, out Texture2D preview) && preview != null)
                {
                    float aspectRatio = (float)preview.width / preview.height;
                    GUILayout.Label(preview, GUILayout.Width(160), GUILayout.Height(160 / aspectRatio));
                }
                else
                {
                    GUILayout.Box("No Image", GUILayout.Width(160), GUILayout.Height(90));
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(prefab.name, GUILayout.Width(160)))
                {
                    SpawnPrefab(prefab);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                index++;
                if (index % columns == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(horizontalPadding);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);
        }

        private void SpawnPrefab(GameObject prefab)
        {
            if (currentInstance != null)
            {
                DestroyImmediate(currentInstance);
            }

            currentInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            currentInstance.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(currentInstance, "Spawn Prefab");
        }
    }
}
