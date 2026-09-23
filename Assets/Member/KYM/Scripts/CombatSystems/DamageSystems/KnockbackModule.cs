using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.DamageSystems
{
    public class KnockbackModule : MonoBehaviour, IModule, IKnockbackReceiver
    {
        [field: Header("넉백 설정")]
        [field: SerializeField, Min(0f)]
        public float KnockbackMultiplier { get; private set; } = 1f;

        [field: SerializeField]
        public bool IsImmune { get; private set; }

        private IMover _mover;

        public void Initialize(ModuleOwner owner)
        {
            _mover = owner.GetModule<IMover>();
            Debug.Assert(_mover != null, $"{owner.name}에 IMover 모듈이 없습니다.");
        }

        public void ApplyKnockback(Vector2 force)
        {
            if (IsImmune || _mover == null || force.sqrMagnitude <= Mathf.Epsilon)
                return;

            _mover.StopImmediately(true,true);
            _mover.AddForceToAgent(force * KnockbackMultiplier);
        }
    }
}
