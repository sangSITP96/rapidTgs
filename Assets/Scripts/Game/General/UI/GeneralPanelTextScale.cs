using TMPro;
using UnityEngine;

namespace Game.General.UI
{
    /// <summary>
    /// One-time font bump for General / Campaign panels already baked in the scene.
    /// </summary>
    public static class GeneralPanelTextScale
    {
        public const float ExtraSize = 5f;

        public static void ApplyOnce(MonoBehaviour host, ref bool applied)
        {
            if (applied || host == null)
                return;

            applied = true;
            TMP_Text[] texts = host.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                    texts[i].fontSize += ExtraSize;
            }
        }
    }
}
