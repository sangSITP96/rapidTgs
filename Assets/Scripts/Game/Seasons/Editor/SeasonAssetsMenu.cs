#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.Seasons.Editor
{
    public static class SeasonAssetsMenu
    {
        private const string DataFolder = "Assets/Scripts/Game/Seasons/Data";

        [MenuItem("Tools/Seasons/Create Default Season Assets")]
        public static void CreateDefaultAssets()
        {
            EnsureFolder(DataFolder);

            SeasonCalendarConfig calendar = CreateOrLoad<SeasonCalendarConfig>(
                $"{DataFolder}/SeasonCalendarConfig.asset");
            calendar.DaysPerSeason = 7;
            calendar.GameDaysPerYear = 28;
            calendar.StartingSeason = SeasonId.Spring;
            calendar.DayZeroWeekday = 0;
            calendar.SeasonChangeWeekday = 0;
            calendar.DisplayTemperatureUnit = TemperatureUnit.Fahrenheit;
            calendar.ScheduleSeed = 14;
            EditorUtility.SetDirty(calendar);

            CreateDefinitionAsset(SeasonId.Spring, $"{DataFolder}/Season_Spring.asset");
            CreateDefinitionAsset(SeasonId.Summer, $"{DataFolder}/Season_Summer.asset");
            CreateDefinitionAsset(SeasonId.Autumn, $"{DataFolder}/Season_Autumn.asset");
            CreateDefinitionAsset(SeasonId.Winter, $"{DataFolder}/Season_Winter.asset");

            SeasonWeatherMapping mapping = CreateOrLoad<SeasonWeatherMapping>(
                $"{DataFolder}/SeasonWeatherMapping.asset");
            EditorUtility.SetDirty(mapping);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Seasons] Default season assets created/updated under " + DataFolder);
        }

        private static void CreateDefinitionAsset(SeasonId id, string path)
        {
            SeasonDefinition runtime = SeasonDefinitionFactory.CreateRuntime(id);
            SeasonDefinition asset = CreateOrLoad<SeasonDefinition>(path);
            EditorUtility.CopySerialized(runtime, asset);
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(asset);
            Object.DestroyImmediate(runtime);
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
