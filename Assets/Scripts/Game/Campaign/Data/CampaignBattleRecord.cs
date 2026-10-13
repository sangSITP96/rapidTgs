using System;
using Game.Battle;
using UnityEngine;

namespace Game.Campaign
{
    [Serializable]
    public sealed class CampaignBattleRecord
    {
        [SerializeField] private int battleIndex;
        [SerializeField] private int gameDayIndex;
        [SerializeField] private double totalGameSeconds;
        [SerializeField] private BattleResolveMode resolveMode;

        [SerializeField] private ArmyComposition commanderTroopsSent;
        [SerializeField] private ArmyComposition fistTroopsSent;
        [SerializeField] private ArmyComposition commanderSurviving;
        [SerializeField] private ArmyComposition fistSurviving;

        [SerializeField] private float commanderWinningPercentage;
        [SerializeField] private float fistWinningPercentage;
        [SerializeField] private CampaignBattleSideResult sideResult;

        [SerializeField] private int seedUsed;
        [SerializeField] private bool resolved;

        public int BattleIndex => battleIndex;
        public int GameDayIndex => gameDayIndex;
        public double TotalGameSeconds => totalGameSeconds;
        public BattleResolveMode ResolveMode => resolveMode;

        public ArmyComposition CommanderTroopsSent => commanderTroopsSent;
        public ArmyComposition FistTroopsSent => fistTroopsSent;
        public ArmyComposition CommanderSurviving => commanderSurviving;
        public ArmyComposition FistSurviving => fistSurviving;

        public float CommanderWinningPercentage => commanderWinningPercentage;
        public float FistWinningPercentage => fistWinningPercentage;
        public CampaignBattleSideResult SideResult => sideResult;

        public int SeedUsed => seedUsed;
        public bool Resolved => resolved;

        public static CampaignBattleRecord CreateResolved(
            int battleIndex,
            int gameDayIndex,
            double totalGameSeconds,
            BattleResult battleResult,
            bool commanderIsAttacker,
            float commanderWinningPercentage,
            float fistWinningPercentage,
            CampaignBattleSideResult sideResult)
        {
            ArmyComposition cmdSent = commanderIsAttacker
                ? battleResult.AttackerStarting
                : battleResult.DefenderStarting;
            ArmyComposition fistSent = commanderIsAttacker
                ? battleResult.DefenderStarting
                : battleResult.AttackerStarting;
            ArmyComposition cmdSurv = commanderIsAttacker
                ? battleResult.AttackerRemaining
                : battleResult.DefenderRemaining;
            ArmyComposition fistSurv = commanderIsAttacker
                ? battleResult.DefenderRemaining
                : battleResult.AttackerRemaining;

            return new CampaignBattleRecord
            {
                battleIndex = battleIndex,
                gameDayIndex = gameDayIndex,
                totalGameSeconds = totalGameSeconds,
                resolveMode = battleResult.Mode,
                commanderTroopsSent = cmdSent,
                fistTroopsSent = fistSent,
                commanderSurviving = cmdSurv,
                fistSurviving = fistSurv,
                commanderWinningPercentage = commanderWinningPercentage,
                fistWinningPercentage = fistWinningPercentage,
                sideResult = sideResult,
                seedUsed = battleResult.SeedUsed,
                resolved = true
            };
        }
    }
}
