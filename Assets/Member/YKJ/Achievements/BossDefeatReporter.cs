using Member.ODK.Scripts;
using UnityEngine;

namespace Member.Wst.Scripts.Achievements
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthModule))]
    public sealed class BossDefeatReporter : MonoBehaviour
    {
        [SerializeField, Tooltip("조건 SO와 동일한 고정 이름. 오브젝트 이름을 바꿔도 이 값은 유지하세요.")]
        private string bossName;
        private HealthModule _health;
        private Member.ODK.Scripts.Enemys.Bosses.PhasedBossController _phasedBoss;
        private bool _reported;

        private void Awake()
        {
            _health = GetComponent<HealthModule>();
            _phasedBoss = GetComponent<Member.ODK.Scripts.Enemys.Bosses.PhasedBossController>();
            if (_phasedBoss != null) _phasedBoss.OnDefeated += HandleDeath;
            else _health.OnDeath += HandleDeath;
            _health.OnHealthChanged += HandleHealthChanged;
            if (string.IsNullOrWhiteSpace(bossName))
                Debug.LogWarning("BossDefeatReporter에 Boss Name을 지정하세요.", this);
        }

        private void HandleHealthChanged(float current, float maximum)
        {
            if (current > 0f && !_health.IsDead)
                _reported = false;
        }

        private void HandleDeath()
        {
            if (_reported || (_phasedBoss != null ? !_phasedBoss.IsDead : !_health.IsDead)) return;
            _reported = true;
            BossDefeatRecord.Record(bossName);
        }

        private void OnDestroy()
        {
            if (_health == null) return;
            _health.OnDeath -= HandleDeath;
            if (_phasedBoss != null) _phasedBoss.OnDefeated -= HandleDeath;
            _health.OnHealthChanged -= HandleHealthChanged;
        }
    }
}
