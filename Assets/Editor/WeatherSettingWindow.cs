#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public class WeatherSettingWindow : EditorWindow
{
    private WeatherSettingsDatabase _database;
    private SerializedObject _serializedObject;
    
    private Vector2 _scrollPos;
    
    private const string DATABASE_PATH = "Assets/Weather/Data/WeatherSettingsData.asset";

    [MenuItem("Tools/Weather Settings")]
    public static void OpenWindow()
    {
        GetWindow<WeatherSettingWindow>("Weather Settings");
    }

    private void OnEnable()
    {
        if (_database == null)
        {
            _database = AssetDatabase.LoadAssetAtPath<WeatherSettingsDatabase>(DATABASE_PATH);

            if (_database != null)
            {
                _serializedObject = new SerializedObject(_database);
            }
        }
    }

    private void OnGUI()
    {
        _database = (WeatherSettingsDatabase)EditorGUILayout.ObjectField(
            "Database",
            _database,
            typeof(WeatherSettingsDatabase), 
            false);

        if (_database == null)
        {
            EditorGUILayout.HelpBox("Assign a WeatherSettingsDatabase asset.", MessageType.Info);
            return;
        }
        
        SerializedObject so = new SerializedObject(_database);
        so.Update();
        SerializedProperty entriesProp = so.FindProperty("entries");
        
        EditorGUILayout.Space();
        
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        for (int i = 0; i < entriesProp.arraySize; i++)
        {
            SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
            SerializedProperty typeProp = entryProp.FindPropertyRelative("Type");
            SerializedProperty isActiveProp = entryProp.FindPropertyRelative("isActive");
            SerializedProperty lowProp = entryProp.FindPropertyRelative("lowValue");
            SerializedProperty highProp = entryProp.FindPropertyRelative("highValue");
            SerializedProperty refMinProp = entryProp.FindPropertyRelative("referenceMin");
            SerializedProperty refMaxProp = entryProp.FindPropertyRelative("referenceMax");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"=====> {((WeatherType)typeProp.enumValueIndex).ToString()} <=====", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(isActiveProp, new GUIContent("Active"));

            float refMin = refMinProp.floatValue;
            float refMax = refMaxProp.floatValue;
            
            EditorGUILayout.LabelField($"Reference Range: {refMin:0.00} - {refMax:0.00}");
            
            EditorGUILayout.Slider(lowProp, refMin, refMax, new GUIContent("Low"));
            EditorGUILayout.Slider(highProp, refMin, refMax, new GUIContent("High"));
            
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();

        so.ApplyModifiedProperties();
        GUI.FocusControl(null);
    }
}

#endif
