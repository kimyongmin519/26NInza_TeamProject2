using System.Collections;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public abstract class LostSoulSkill : ODKBossSkill
    {
        [Header("Direct Scene References")]
        [SerializeField] private DamageCaster damageCaster;
        [SerializeField] private string animationStateName;

        protected LostSoul Boss { get; private set; }
        protected DamageCaster Caster => damageCaster;

        protected sealed override void OnInitialize()
        {
            Boss = Owner as LostSoul;
            if (damageCaster == null) damageCaster = GetComponent<DamageCaster>();
            Debug.Assert(Boss != null, "LostSoulSkill owner must be LostSoul.", this);
            Debug.Assert(damageCaster != null, "LostSoulSkill requires a scene DamageCaster.", this);
            OnLostSoulInitialize();
        }

        protected sealed override IEnumerator Execute(GameObject target)
        {
            Boss.PlayAnimation(animationStateName);
            yield return ExecuteLostSoul(target);
        }

        protected void ReplayAnimation() => Boss.PlayAnimation(animationStateName, 0.02f);
        protected virtual void OnLostSoulInitialize() { }
        protected abstract IEnumerator ExecuteLostSoul(GameObject target);

        protected override void OnCompleted()
        {
            Caster?.DisableCasting();
            Boss?.PlayIdle();
            OnLostSoulCompleted();
        }

        protected override void OnCancel()
        {
            Caster?.DisableCasting();
            Boss?.SetDarkness(false, 0.05f);
            Boss?.PlayIdle();
            OnLostSoulCancelled();
        }

        protected virtual void OnLostSoulCompleted() { }
        protected virtual void OnLostSoulCancelled() { }
    }
}
