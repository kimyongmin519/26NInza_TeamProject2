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

            if (healthBar == null)
                return false;

            if (!healthBar.gameObject.activeSelf)
                healthBar.gameObject.SetActive(true);

            healthBar.Bind(healthModule);
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
