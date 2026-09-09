using System;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "UseSkill", story: "[Enemy] use [SkillNumber] to [TargetGameObject]", category: "Action", id: "eeae717575af6eb626d1cb5f6fe62f1a")]
    public partial class UseSkillAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<int> SkillNumber;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        private ISkillModule _skillModule;
        private bool _isSkillEnd;
        
        protected override Status OnStart()
        {
            if (Enemy.Value == null || SkillNumber.Value < 0)
            {
                Debug.LogWarning("Enemy 또는 Number가 할당되지 않았습니다. 행동이 실패로 간주됩니다.");
                return Status.Failure;
            }

            _skillModule = Enemy.Value.SkillModule;

            _isSkillEnd = false;
            _skillModule.UseSkill(SkillNumber.Value, TargetGameObject.Value);
            _skillModule.OnCurrentSkillEnd += HandleSkillEnd;
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            return _isSkillEnd ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
            if(_skillModule != null)
                _skillModule.OnCurrentSkillEnd -= HandleSkillEnd;
        }

        private void HandleSkillEnd() => _isSkillEnd = true;
    }
}

