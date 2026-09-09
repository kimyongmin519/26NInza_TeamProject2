using System;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.BossSkills
{
    public abstract class AbstractBossSkill : MonoBehaviour, ISkill
    {
        [field:SerializeField] public SkillDataSO SkillData { get; private set; }
        public event Action OnSkillEnd;
        public bool IsUsing { get; private set; }
        private ISkillModule _bossSkillModule;
        protected IAnimateRenderer _renderer;
        protected IMover _mover;
        protected AbstractBoss _boss;
        private float _lastUseTime;
        
        public float NormalizedCooldown
        {
            get
            {
                if (SkillData == null)
                    return 0f;

                if (Mathf.Approximately(SkillData.Cooldown, 0f))
                    return 1f;

                return Mathf.Clamp01(
                    (Time.time - _lastUseTime) /
                    SkillData.Cooldown);
            }
        }
        public void InitializeSkill(ISkillModule skillModule)
        {
            _bossSkillModule = skillModule;
            Debug.Assert(_bossSkillModule != null, "보스 스킬은 반드시 보스 스킬 모듈의 자식이어야 함");
            
            _boss = skillModule.Owner as AbstractBoss;
            Debug.Assert(_boss != null, "보스 스킬인데 오너가 보스가 아닙니다!");
            _renderer = _boss.GetModule<IAnimateRenderer>();
            Debug.Assert(_renderer != null, "보스에 렌더러 모듈이 없음");
            _mover = _boss.GetModule<IMover>();
            Debug.Assert(_mover != null, "보스의 이동모듈이 없음");
            IsUsing = false;
            
            _lastUseTime = Time.time - SkillData.Cooldown;
        }

        public abstract bool CanUseSkill(GameObject target = null);

        public virtual void UseSkill(GameObject target = null)
        {
            IsUsing = true;
            _lastUseTime = Time.time;
        }

        public virtual void StopSkill()
        {
            IsUsing = false;
            OnSkillEnd?.Invoke();
        }
    }
}
