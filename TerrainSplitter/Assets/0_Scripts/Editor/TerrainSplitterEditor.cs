using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Landfall.Editor
{
    [System.Serializable]
    public class TerrainSplitterEditorSettings : ScriptableObject
    {
        [SerializeField]
        public Terrain Terrain;
        [SerializeField]
        public Vector3Int ChunkSize;
        [SerializeField]
        public string SavePath = "Assets/SplitTerrains/";
        
        private string m_configPath;

        private SerializedObject m_serializedObject;
        public SerializedObject SerializedObject { get{ return m_serializedObject; } private set { m_serializedObject = value; }}

        public static TerrainSplitterEditorSettings LoadSettings(string path)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TerrainSplitterEditorSettings>(path);

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<TerrainSplitterEditorSettings>();
            }
            settings.m_configPath = path;
            settings.SerializedObject = new SerializedObject(settings);

            return settings;
        }

        public void SaveSettings()
        {
            m_serializedObject.ApplyModifiedProperties();
            if (AssetDatabase.Contains(this))
            {
                AssetDatabase.SaveAssets();
            }
            else
            {
                AssetDatabase.CreateAsset(this, m_configPath);
            }
        }
    }

    public class TerrainSplitterEditor : EditorWindow
    {
        private TerrainSplitterEditorSettings m_settings;

        [MenuItem("Landfall/Tools/Terrain Divider")]
        static void Init() 
        {
            TerrainSplitterEditor editor = (TerrainSplitterEditor)EditorWindow.GetWindow(typeof(TerrainSplitterEditor));
            editor.Show();   
        }

        private void OnEnable() 
        {
            m_settings = TerrainSplitterEditorSettings.LoadSettings("Assets/TerrainSplitterSettings.asset");
        }

        private void OnDisable() 
        {
            
        }

        private void OnGUI() 
        {
            DrawSettings();

            if (GUILayout.Button("Split Terrain"))
            {
                SplitTerrain();
            }
        }

        private void SplitTerrain()
        {
            List<Terrain> terrains = Landfall.Editor.TerrainDivider.SplitIntoChunks(m_settings.ChunkSize.x, m_settings.ChunkSize.z, m_settings.Terrain, m_settings.SavePath);

            Debug.Log(String.Format("Split Terrain {0} into {1} chunks.", m_settings.Terrain.name, terrains.Count));
        }

        private Vector2 _scrollPos = Vector2.zero;
        private void DrawSettings()
        {
            // display serializedProperty with selected mode
            Type type = typeof(SerializedObject);
            PropertyInfo infor = type.GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
            if (infor != null)
            {
                infor.SetValue(m_settings.SerializedObject, InspectorMode.Normal, null);
            }

            EditorGUI.BeginChangeCheck();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            SerializedProperty iterator = m_settings.SerializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script")
                    continue;
                
                EditorGUILayout.PropertyField(iterator, true);
            }
            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                m_settings.SaveSettings();
            }
        }
    }
}

