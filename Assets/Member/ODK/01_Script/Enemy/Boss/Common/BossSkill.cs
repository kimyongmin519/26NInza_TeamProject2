using System;
using System.Collections;
using System.Collections.Generic;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using Member.ODK.Scripts.Enemys.Skills;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    public abstract class ODKBossSkill : AbstractEnemySkill
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

            try
            {
                OnInitialize();
                isInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
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

            SafeInvoke(OnCancel);
            if (IsUsing) base.StopSkill();
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnCancel() { }
        protected virtual void OnCompleted() { }
        protected abstract IEnumerator Execute(GameObject target);

        private IEnumerator RunAttack(GameObject target)
        {
            bool failed = false;
            IEnumerator routine = null;

            try
            {
                routine = Execute(target);
            }
            catch (Exception e)
            {
                failed = true;
                Debug.LogException(e, this);
            }

            if (!failed && routine != null)
                yield return Guard(routine, e =>
                {
                    failed = true;
                    Debug.LogException(e, this);
                });

            attackCoroutine = null;

            if (failed) SafeInvoke(OnCancel);
            else SafeInvoke(OnCompleted);

            if (IsUsing) base.StopSkill();
        }

        private static IEnumerator Guard(IEnumerator root, Action<Exception> onError)
        {
            Stack<IEnumerator> stack = new Stack<IEnumerator>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                IEnumerator top = stack.Peek();
                bool moved;

                try
                {
                    moved = top.MoveNext();
                }
                catch (Exception e)
                {
                    onError?.Invoke(e);
                    yield break;
                }

                if (!moved)
                {
                    stack.Pop();
                    continue;
                }

                object current = top.Current;
                if (current is IEnumerator nested && current is not CustomYieldInstruction)
                {
                    stack.Push(nested);
                    continue;
                }

                yield return current;
            }
        }

        private void SafeInvoke(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        protected virtual void OnDestroy()
        {
            StopSkill();
        }
    }
}
