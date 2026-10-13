using UnityEngine;

namespace Game.Campaign
{
    [CreateAssetMenu(fileName = "CampaignSettings", menuName = "Game/Campaign/Campaign Settings")]
    public sealed class CampaignSettings : ScriptableObject
    {
        [Header("Battle Schedule (Alpha defaults — configurable, not permanent rules)")]
        [SerializeField, Min(1)] private int battlesPerDay = 4;
        [SerializeField, Min(0.1f)] private float battleDurationGameHours = 2f;

        [Header("Campaign Duration")]
        [Tooltip("Temporary Alpha default used in client examples. Configurable — not a permanent gameplay rule.")]
        [SerializeField, Min(0.1f)] private float campaignDurationGameHours = 24f;

        [Header("Silver Reward")]
        [SerializeField] private int baseSilverPayment = 100;
        [SerializeField] private int silverPerBattleWin = 10;
        [SerializeField] private int silverPerBattleLoss = -5;

        [Header("Force Selection (Alpha)")]
        [SerializeField] private bool equalArmySizeForAlpha = true;

        [Header("Temporary Alpha Assumptions")]
        [Tooltip(
            "TEMPORARY ASSUMPTION for Alpha end-to-end only. Client has NOT confirmed Supporting Commander Silver. " +
            "When true, only Campaign Owner is paid via CurrencySystem. Support contribution/participation data is still kept.")]
        [SerializeField] private bool temporaryAlphaRewardOwnerOnly = true;

        [Tooltip(
            "Campaign Battle Winner uses Surviving/Sent. Small Battle mode does not return survivors. " +
            "When true, Campaign Alpha refuses to resolve battles that would use SmallWinProbability.")]
        [SerializeField] private bool requireLargeBattleMode = true;

        public int BattlesPerDay => battlesPerDay;
        public float BattleDurationGameHours => battleDurationGameHours;
        public float CampaignDurationGameHours => campaignDurationGameHours;
        public double CampaignDurationGameSeconds => campaignDurationGameHours * 3600d;
        public double BattleDurationGameSeconds => battleDurationGameHours * 3600d;

        public int BaseSilverPayment => baseSilverPayment;
        public int SilverPerBattleWin => silverPerBattleWin;
        public int SilverPerBattleLoss => silverPerBattleLoss;

        public bool EqualArmySizeForAlpha => equalArmySizeForAlpha;

        /// <summary>
        /// TEMPORARY Alpha assumption — not a client-confirmed permanent rule.
        /// </summary>
        public bool TemporaryAlphaRewardOwnerOnly => temporaryAlphaRewardOwnerOnly;

        public bool RequireLargeBattleMode => requireLargeBattleMode;

#if UNITY_EDITOR
        private void OnValidate()
        {
            battlesPerDay = Mathf.Max(1, battlesPerDay);
            battleDurationGameHours = Mathf.Max(0.1f, battleDurationGameHours);
            campaignDurationGameHours = Mathf.Max(0.1f, campaignDurationGameHours);
        }
#endif
    }
}
