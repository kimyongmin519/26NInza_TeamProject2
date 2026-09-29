using Member.KYM.Scripts.CombatSystems.Projectiles;
using Member.KYM.Scripts.Players.RobotArm;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    [DisallowMultipleComponent]
    public sealed class ODKGrabInfoDisplay : MonoBehaviour, IGrabbableDisplayData
    {
        [SerializeField] private ProjectileDataSO projectileData;

        public ProjectileDataSO ProjectileData => projectileData;
        public ProjectileDataSO DisplayData => projectileData;
        public bool IsHeld { get; private set; }

        // 기존 투사체의 호출을 유지한다. UI 갱신은 PlayerUIEventPublisher가 담당한다.
        public void SetHeld(bool value) => IsHeld = value;

        private void OnDisable() => IsHeld = false;
    }
}
