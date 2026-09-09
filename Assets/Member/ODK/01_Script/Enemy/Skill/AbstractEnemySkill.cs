using System;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using UnityEngine;
using Member.ODK.Scripts.Enemys;
using System.Collections.Generic;
namespace Member.ODK.Scripts.Enemys.Skills
{
    public abstract class AbstractEnemySkill : MonoBehaviour, ISkill
    {
        [field: SerializeField] public SkillDataSO SkillData { get; private set; }
        public event Action OnSkillEnd;
        public bool IsUsing { get; private set; }
        protected EnemyController _enemy;
        private ISkillModule _enemySkillModule;
        protected IAnimateRenderer _renderer;
        protected IMover _mover;
        protected float _lastUseTime;

        public float NormalizedCooldown
        {
            get
            {
                if (_enemySkillModule != null)
                {
                    return 1f;
                }

                if (Mathf.Approximately(SkillData.Cooldown, 0f))
                    return 1f;

                return Mathf.Clamp01(
                    (Time.time - _lastUseTime) /
                    SkillData.Cooldown);
            }
        }

        public virtual void InitializeSkill(ISkillModule skillModule)
        {
            _enemySkillModule = skillModule;
            Debug.Assert(_enemySkillModule != null, "적 스킬은 반드시 적 스킬 모듈의 자식이어야 함");

            _enemy = skillModule.Owner as EnemyController;
            Debug.Assert(_enemy != null, "적 스킬인데 오너가 적이 아닙니다!");
            _renderer = _enemy.GetModule<IAnimateRenderer>();
            Debug.Assert(_renderer != null, "적에 렌더러 모듈이 없음");
            _mover = _enemy.GetModule<IMover>();
            Debug.Assert(_mover != null, "적의 이동모듈이 없음");
            IsUsing = false;

            _lastUseTime = Time.time - SkillData.Cooldown;
        }

        public abstract bool CanUseSkill(GameObject target = null);

        public virtual void UseSkill(GameObject target = null)
        {
            IsUsing = true;
        }
        public virtual void UseSkill(SkillCommand command,GameObject target = null)
        {
            IsUsing = true;
        }


        public virtual void StopSkill()
        {
            IsUsing = false;
            OnSkillEnd?.Invoke();
        }
    }
}