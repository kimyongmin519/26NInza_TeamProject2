using System.Collections;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using Member.ODK.Scripts.Enemys.Skills;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    public abstract class BossSkill : AbstractEnemySkill
    {
        protected PhasedBossController Owner { get; private set; }
        protected float DurationScale { get; private set; } = 1f;

        private Coroutine attackCoroutine;
        private bool isInitialized;

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            InitializeAttack(_enemy as PhasedBossController);
        }

        public void InitializeAttack(PhasedBossController owner)
        {
            Owner = owner;
            Debug.Assert(Owner != null, "BossSkill의 소유자가 PhasedBossController가 아닙니다.", this);
            if (isInitialized || Owner == null) return;

            OnInitialize();
            isInitialized = true;
        }

        public IEnumerator Play(float durationScale = 1f)
        {
            if (Owner == null)
                InitializeAttack(GetComponentInParent<PhasedBossController>());
            if (Owner == null) yield break;

            GameObject target = Owner.Target != null ? Owner.Target.gameObject : null;
            if (!CanUseSkill(target)) yield break;

            DurationScale = Mathf.Max(0.01f, durationScale);
            UseSkill(target);
            yield return new WaitWhile(() => IsUsing);
        }

        public override void UseSkill(GameObject target = null)
        {
            if (IsUsing) return;
            if (Owner == null)
                InitializeAttack(GetComponentInParent<PhasedBossController>());
            if (Owner == null) return;

            base.UseSkill(target);
            attackCoroutine = StartCoroutine(RunAttack(target));
        }

        public override void StopSkill()
        {
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }

            OnCancel();
            if (IsUsing) base.StopSkill();
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnCancel() { }
        protected virtual void OnCompleted() { }
        protected abstract IEnumerator Execute(GameObject target);

        private IEnumerator RunAttack(GameObject target)
        {
            yield return Execute(target);
            OnCompleted();
            attackCoroutine = null;
            if (IsUsing) base.StopSkill();
        }

        protected virtual void OnDestroy()
        {
            StopSkill();
        }
    }
}
