using System.Collections;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public abstract class VolcanusSkill : ODKBossSkill
    {
        [Header("Animation")]
        [SerializeField] private string animationStateName;

        protected Volcanus Boss { get; private set; }
        protected DamageCaster DamageCaster { get; private set; }

        protected sealed override void OnInitialize()
        {
            Boss = Owner as Volcanus;
            Debug.Assert(Boss != null, "VolcanusSkill owner must be Volcanus.", this);
            GameObject casterObject = new GameObject(name + " Damage Caster");
            casterObject.transform.SetParent(transform, false);
            DamageCaster = casterObject.AddComponent<DamageCaster>();
            OnVolcanusInitialize();
        }

        protected sealed override IEnumerator Execute(GameObject target)
        {
            Boss.FaceTargetImmediately();
            Boss.SetAnimationSpeed(DurationScale);
            Boss.PlayAnimation(animationStateName);
            yield return ExecuteVolcanus(target);
        }

        protected void ReplayAnimation(float fadeDuration = 0.04f)
        {
            Boss?.PlayAnimation(animationStateName, fadeDuration);
        }

        protected virtual void OnVolcanusInitialize() { }
        protected abstract IEnumerator ExecuteVolcanus(GameObject target);

        protected override void OnCompleted()
        {
            DamageCaster?.DisableCasting();
            Boss?.ResetVisual();
            Boss?.SetAnimationSpeed(1f);
            Boss?.PlayIdle();
        }

        protected override void OnCancel()
        {
            DamageCaster?.DisableCasting();
            OnVolcanusCancel();
            Boss?.ResetVisual(0.08f);
            Boss?.SetAnimationSpeed(1f);
            Boss?.PlayIdle();
        }

        protected virtual void OnVolcanusCancel() { }
    }
}
