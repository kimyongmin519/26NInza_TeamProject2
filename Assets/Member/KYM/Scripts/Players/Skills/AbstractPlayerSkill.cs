using System;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Players.Skills
{
    public abstract class AbstractPlayerSkill : MonoBehaviour, ISkill
    {
        [field:SerializeField] public SkillDataSO SkillData { get; private set; }
        public event Action OnSkillEnd;
        public bool IsUsing { get; private set; }
        protected PlayerController _player;
        private ISkillModule _playerSkillModule;
        protected IAnimateRenderer _renderer;
        protected IMover _mover;
        protected float _lastUseTime;
        
        public float NormalizedCooldown
        {
            get
            {
                if (_playerSkillModule != null)
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
        
        public void InitializeSkill(ISkillModule skillModule)
        {
            _playerSkillModule = skillModule;
            Debug.Assert(_playerSkillModule != null, "플레이어 스킬은 반드시 플레이어 스킬 모듈의 자식이어야 함");
            
            _player = skillModule.Owner as PlayerController;
            Debug.Assert(_player != null, "플레이어 스킬인데 오너가 플레이어가 아닙니다!");
            _renderer = _player.GetModule<IAnimateRenderer>();
            Debug.Assert(_renderer != null, "플레이어에 렌더러 모듈이 없음");
            _mover = _player.GetModule<IMover>();
            Debug.Assert(_mover != null, "플레이어의 이동모듈이 없음");
            IsUsing = false;
            
            _lastUseTime = Time.time - SkillData.Cooldown;
        }

        public abstract bool CanUseSkill(GameObject target = null);

        public virtual void UseSkill(GameObject target = null)
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