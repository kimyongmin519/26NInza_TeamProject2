using System;
using KimLIb.AnimatorSystems;
using Member.KYM.Scripts.CoreSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.BossSkills.TutoBoss
{
    public class DoveSummonSkill : AbstractBossSkill
    {
        [SerializeField] private AnimParamSO skillParam;

        [Header("소환 관련")]
        [SerializeField] private Transform summonTrm;
        [SerializeField] private Bat batPrefab;
        [SerializeField] private int summonNumber;
        [SerializeField] private float summonInterval;
        
        private AnimatorTrigger _trigger;
        
        private void Awake()
        {
            _trigger = _boss.GetModule<AnimatorTrigger>();
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return IsUsing == false && NormalizedCooldown >= 1f;
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            
            _renderer.PlayClip(skillParam.ParamHash);
            _trigger.OnSpecialEvent += HandleDoveSpawn;
        }

        private async void HandleDoveSpawn()
        {
            for (int i = 0; i < summonNumber; i++)
            {
                Bat spawnBat = Instantiate(batPrefab, summonTrm.position, Quaternion.identity);
                await Awaitable.WaitForSecondsAsync(summonInterval);
            }
        }
        
        
    }
}
