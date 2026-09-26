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
        [SerializeField] private float startupDelay = 0.1f;
        [SerializeField] private float recoveryDelay = 0.14f;

        protected LostSoul Boss { get; private set; }
        protected DamageCaster Caster => damageCaster;

        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && Boss != null && target != null && target.activeInHierarchy;
        }

        protected sealed override void OnInitialize()
        {
            Boss = Owner as LostSoul;
            if (damageCaster == null) damageCaster = GetComponent<DamageCaster>();
            Debug.Assert(Boss != null, "LostSoulSkill owner must be LostSoul.", this);
            OnLostSoulInitialize();
        }

        protected sealed override IEnumerator Execute(GameObject target)
        {
            Boss.PlayAnimation(animationStateName);
            if (startupDelay > 0f)
                yield return new WaitForSeconds(startupDelay * DurationScale);
            yield return ExecuteLostSoul(target);
            if (recoveryDelay > 0f)
                yield return new WaitForSeconds(recoveryDelay * DurationScale);
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
