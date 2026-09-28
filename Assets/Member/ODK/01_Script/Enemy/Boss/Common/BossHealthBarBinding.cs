using Member.KYM.Scripts.UI;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    [DisallowMultipleComponent]
    public sealed class BossHealthBarBinding : MonoBehaviour
    {
        [Header("Boss")]
        [SerializeField] private HealthModule healthModule;

        [Header("UI (Optional)")]
        [SerializeField] private BossHealthBarUI healthBar;
        [SerializeField] private BossKOUI koUI;

        private void Awake()
        {
            CacheBossReferences();
            TryBind();
        }

        public bool TryBind()
        {
            CacheBossReferences();
            if (healthModule == null)
                return false;

            if (healthBar == null)
            {
                healthBar = FindFirstObjectByType<BossHealthBarUI>(
                    FindObjectsInactive.Include
                );
            }

            if (koUI == null)
            {
                koUI = FindFirstObjectByType<BossKOUI>(
                    FindObjectsInactive.Include
                );
            }

            if (koUI != null)
                koUI.Bind(healthModule);

            if (healthBar == null)
                return koUI != null;

            if (!healthBar.gameObject.activeSelf)
                healthBar.gameObject.SetActive(true);

            healthBar.Bind(healthModule);
            UIShowFromTop showFromTop = healthBar.GetComponentInParent<UIShowFromTop>(true);
            BossBarHideGuard.ReleaseFor(healthBar);
            if (showFromTop != null) showFromTop.Show();
            return true;
        }

        private void CacheBossReferences()
        {
            if (healthModule == null)
                healthModule = GetComponent<HealthModule>();

            if (healthModule == null)
                healthModule = GetComponentInChildren<HealthModule>(true);
        }
    }
}
