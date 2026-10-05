#if UNITY_EDITOR
using Game.Battle.DebugTools;
using UnityEditor;
using UnityEngine;

namespace Game.Battle.Editor
{
    public static class BattleAssetsMenu
    {
        private const string Root = "Assets/Scripts/Game/Battle";
        private const string DataFolder = Root + "/Data/Assets";
        private const string PrefabFolder = Root + "/Prefabs";

        [MenuItem("Tools/Battle/Create Default Battle Assets And Prefab")]
        public static void CreateDefaultAssetsAndPrefab()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(PrefabFolder);

            BattleSettings settings = CreateOrLoad<BattleSettings>($"{DataFolder}/BattleSettings.asset");
            EditorUtility.SetDirty(settings);

            CreateTroop(TroopType.Infantry, $"{DataFolder}/Troop_Infantry.asset");
            CreateTroop(TroopType.Cavalry, $"{DataFolder}/Troop_Cavalry.asset");
            CreateTroop(TroopType.Archers, $"{DataFolder}/Troop_Archers.asset");

            CreateOrUpdateTestPrefab($"{PrefabFolder}/BattleTestPanel.prefab", settings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Battle] Default assets and BattleTestPanel prefab created/updated.");
        }

        [MenuItem("Tools/Battle/Run Smoke Resolve (Editor)")]
        public static void RunSmokeResolve()
        {
            BattleSettings settings = AssetDatabase.LoadAssetAtPath<BattleSettings>(
                $"{DataFolder}/BattleSettings.asset");
            if (settings == null)
            {
                Debug.LogError("[Battle] BattleSettings.asset missing. Run Create Default Battle Assets first.");
                return;
            }

            var small = BattleResolver.Resolve(
                new BattleInput(new ArmyComposition(5, 0, 0), new ArmyComposition(0, 5, 0)),
                new BattleContext(applyDefenderAdvantage: true, seed: 16),
                settings);
            Debug.Log(
                $"[Battle Smoke] SMALL seed={small.SeedUsed} α={small.SampledCounterAdvantage:F3} " +
                $"P(A)={small.AttackerWinProbability:P1} P(D)={small.DefenderWinProbability:P1} mode={small.Mode}");

            var large = BattleResolver.Resolve(
                new BattleInput(new ArmyComposition(10, 0, 0), new ArmyComposition(0, 10, 0)),
                new BattleContext(applyDefenderAdvantage: true, seed: 16),
                settings);
            Debug.Log(
                $"[Battle Smoke] LARGE seed={large.SeedUsed} α={large.SampledCounterAdvantage:F3} " +
                $"deaths A={large.AttackerDeaths.Total} D={large.DefenderDeaths.Total} " +
                $"remain A={large.AttackerRemaining.Total} D={large.DefenderRemaining.Total} mode={large.Mode}");

            var large2 = BattleResolver.Resolve(
                new BattleInput(new ArmyComposition(10, 0, 0), new ArmyComposition(0, 10, 0)),
                new BattleContext(applyDefenderAdvantage: true, seed: 16),
                settings);
            bool same =
                large.AttackerDeaths.Total == large2.AttackerDeaths.Total &&
                large.DefenderDeaths.Total == large2.DefenderDeaths.Total &&
                Mathf.Approximately(large.SampledCounterAdvantage, large2.SampledCounterAdvantage);
            Debug.Log(same
                ? "[Battle Smoke] Seed reproducibility OK"
                : "[Battle Smoke] Seed reproducibility FAILED");
        }

        private static void CreateTroop(TroopType type, string path)
        {
            TroopDefinition troop = CreateOrLoad<TroopDefinition>(path);
            SerializedObject so = new SerializedObject(troop);
            so.FindProperty("troopType").enumValueIndex = (int)type;
            so.FindProperty("displayName").stringValue = type.ToString();
            so.FindProperty("attack").intValue = 1;
            so.FindProperty("defense").intValue = 1;
            so.FindProperty("health").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(troop);
        }

        private static void CreateOrUpdateTestPrefab(string path, BattleSettings settings)
        {
            GameObject root = new GameObject("BattleTestPanel");
            try
            {
                BattleTestController controller = root.AddComponent<BattleTestController>();
                SerializedObject so = new SerializedObject(controller);
                so.FindProperty("_settings").objectReferenceValue = settings;
                so.FindProperty("_showOverlay").boolValue = false;
                so.FindProperty("_attackerInfantry").intValue = 5;
                so.FindProperty("_defenderCavalry").intValue = 5;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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
