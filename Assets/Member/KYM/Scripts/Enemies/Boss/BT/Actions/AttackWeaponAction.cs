using System;
using Member.KYM.Scripts.CombatSystems.WeaponSystems;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "AttackWeapon",
        story: "[Enemy] attacks [TargetGameObject] with equipped weapon",
        category: "Action",
        id: "41f680ccce5d4c8384b38bd47f5fb033")]
    public partial class AttackWeaponAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        private IWeaponModule _weaponModule;
        private bool _isAttackEnd;

        protected override Status OnStart()
        {
            if (Enemy?.Value == null || Enemy.Value.WeaponModule == null)
            {
                return Status.Failure;
            }

            _weaponModule = Enemy.Value.WeaponModule;
            _isAttackEnd = false;

            // 공격 시작 과정에서 즉시 종료될 수도 있으므로 먼저 구독한다.
            _weaponModule.OnCurrentWeaponAttackEnd += HandleAttackEnd;

            if (_weaponModule.TryAttack(TargetGameObject?.Value))
                return Status.Running;

            _weaponModule.OnCurrentWeaponAttackEnd -= HandleAttackEnd;
            _weaponModule = null;
            return Status.Failure;
        }

        protected override Status OnUpdate()
        {
            return _isAttackEnd ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
            if (_weaponModule == null)
                return;

            _weaponModule.OnCurrentWeaponAttackEnd -= HandleAttackEnd;
            _weaponModule.StopCurrentAttack();
            _weaponModule = null;
        }

        private void HandleAttackEnd()
        {
            _isAttackEnd = true;
        }
    }
}
