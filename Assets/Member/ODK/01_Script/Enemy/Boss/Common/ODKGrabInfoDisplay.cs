using Member.KYM.Scripts.CombatSystems.Projectiles;
using Member.KYM.Scripts.UI;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class ODKGrabInfoDisplay : MonoBehaviour
    {
        [SerializeField] private ProjectileDataSO projectileData;

        public ProjectileDataSO ProjectileData => projectileData;

        private static ODKGrabInfoDisplay activeDisplay;
        private HeldProjectileUI heldProjectileUI;
        private bool held;

        public void SetHeld(bool value)
        {
            held = value;
            if (held)
            {
                if (activeDisplay != null && activeDisplay != this)
                    activeDisplay.held = false;
                activeDisplay = this;
                Show();
                return;
            }

            if (activeDisplay != this)
                return;

            ResolveUI()?.Show(null);
            activeDisplay = null;
        }

        private void LateUpdate()
        {
            if (held)
                Show();
        }

        private void Show()
        {
            if (projectileData != null)
                ResolveUI()?.Show(projectileData);
        }

        private HeldProjectileUI ResolveUI()
        {
            if (heldProjectileUI == null)
                heldProjectileUI = FindFirstObjectByType<HeldProjectileUI>();
            return heldProjectileUI;
        }

        private void OnDisable()
        {
            SetHeld(false);
        }
    }
}
