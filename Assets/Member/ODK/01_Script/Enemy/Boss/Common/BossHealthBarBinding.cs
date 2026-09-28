using System.Collections;
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
        }

        private IEnumerator Start()
        {
            yield return null;
            TryBind();
        }

        public bool TryBind()
        {
            CacheBossReferences();
            if (healthModule == null)
                return false;

            if (healthBar == null || healthBar.gameObject.scene != gameObject.scene)
                healthBar = FindInScene<BossHealthBarUI>();

            if (koUI == null || koUI.gameObject.scene != gameObject.scene)
                koUI = FindInScene<BossKOUI>();

            if (koUI != null)
                koUI.Bind(healthModule);

            if (healthBar == null)
                return koUI != null;

            if (!healthBar.gameObject.activeSelf)
                healthBar.gameObject.SetActive(true);

            healthBar.Bind(healthModule);
            UIShowFromTop showFromTop = healthBar.GetComponentInParent<UIShowFromTop>(true);
            BossBarHideGuard.ReleaseFor(healthBar);
            if (showFromTop != null && showFromTop.isActiveAndEnabled) showFromTop.Show();
            return true;
        }

        private T FindInScene<T>() where T : Component
        {
            T fallback = null;
            T[] candidates = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (T candidate in candidates)
            {
                if (candidate == null) continue;
                if (candidate.gameObject.scene != gameObject.scene)
                {
                    if (fallback == null) fallback = candidate;
                    continue;
                }
                if (candidate.gameObject.activeInHierarchy) return candidate;
                if (fallback == null || fallback.gameObject.scene != gameObject.scene) fallback = candidate;
            }
            return fallback;
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
