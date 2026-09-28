using System;
using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public abstract class VolcanusSkill : ODKBossSkill
    {
        [Header("Animation")]
        [SerializeField] private string animationStateName;
        [SerializeField, Range(0.3f, 1f)] private float recoveryNormalized = 0.85f;

        [field: Header("Damage")]
        [field: SerializeField] protected DamageCaster DamageCaster { get; private set; }

        protected Volcanus Boss { get; private set; }
        protected string ActionState => string.IsNullOrWhiteSpace(animationStateName) ? DefaultActionState : animationStateName;
        protected virtual string DefaultActionState => Volcanus.SlamState;
        protected float ActionSpeed => Boss != null ? Mathf.Max(0.01f, DurationScale) : 1f;
        protected float Scale => Boss != null ? Boss.SizeScale : 1f;

        protected sealed override void OnInitialize()
        {
            Boss = Owner as Volcanus;
            Debug.Assert(Boss != null, "VolcanusSkill owner must be Volcanus.", this);
            if (DamageCaster == null) DamageCaster = GetComponent<DamageCaster>();
            Debug.Assert(DamageCaster != null, $"{name} DamageCaster is not connected.", this);
            OnVolcanusInitialize();
        }

        protected sealed override IEnumerator Execute(GameObject target)
        {
            Boss.SetActing(true);
            Boss.ResetVisual(0.05f);
            Boss.FaceTargetImmediately();
            yield return ExecuteVolcanus(target);
        }

        protected IEnumerator PerformAction(string state, Action<int, float> onImpact)
        {
            float speed = ActionSpeed;
            float length = Boss.PlayAction(state, speed);
            float[] impacts = Boss.GetImpactTimes(state, speed);
            float elapsed = 0f;

            for (int i = 0; i < impacts.Length && !Boss.IsDead; i++)
            {
                float wait = impacts[i] - elapsed;
                if (wait > 0f) yield return new WaitForSeconds(wait);
                elapsed = impacts[i];
                onImpact?.Invoke(i, impacts.Length > 1 ? (float)i / (impacts.Length - 1) : 1f);
            }

            float recovery = length * recoveryNormalized - elapsed;
            if (recovery > 0f) yield return new WaitForSeconds(recovery);
        }

        protected float GetFirstImpactTime(string state)
        {
            float[] impacts = Boss.GetImpactTimes(state, ActionSpeed);
            return impacts.Length > 0 ? impacts[0] : 0f;
        }

        protected IEnumerator WalkTo(float x, bool run)
        {
            Tween move = Boss.MoveTo(x, run ? Boss.RunSpeed * ActionSpeed : Boss.WalkSpeed * ActionSpeed, run);
            if (move != null) yield return move.WaitForCompletion();
            Boss.FaceTargetImmediately();
        }

        protected float ClampToArena(float x, float padding = 1f)
        {
            float pad = padding * Scale;
            return Mathf.Clamp(x, Boss.ArenaCenter.x - Boss.ArenaHalfWidth + pad, Boss.ArenaCenter.x + Boss.ArenaHalfWidth - pad);
        }

        protected virtual void OnVolcanusInitialize() { }
        protected abstract IEnumerator ExecuteVolcanus(GameObject target);

        protected override void OnCompleted()
        {
            DamageCaster?.DisableCasting();
            if (Boss == null) return;
            Boss.SetActing(false);
            if (Boss.IsActing) return;
            Boss.ResetGolemPieces();
            Boss.ResetVisual();
            Boss.PlayIdle();
        }

        protected override void OnCancel()
        {
            DamageCaster?.DisableCasting();
            OnVolcanusCancel();
            if (Boss == null) return;
            Boss.SetActing(false);
            if (Boss.IsActing) return;
            Boss.transform.DOKill();
            Boss.ResetGolemPieces(0.08f);
            Boss.ResetVisual(0.08f);
            if (!Boss.IsDead) Boss.PlayIdle();
        }

        protected virtual void OnVolcanusCancel() { }
    }
}
