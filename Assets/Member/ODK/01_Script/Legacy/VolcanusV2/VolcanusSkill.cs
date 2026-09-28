using System.Collections;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus.Legacy
{
    public abstract class VolcanusSkill : ODKBossSkill
    {
        [field: Header("Damage")]
        [field: SerializeField] protected DamageCaster DamageCaster { get; private set; }

        [Header("Animation")]
        [SerializeField] private string animationStateName;

        protected Volcanus Boss => _volcanus;
        protected float Speed => Mathf.Max(0.01f, DurationScale);
        private VolcanusPiece animatedPiece;
        private Volcanus _volcanus;

        protected sealed override void OnInitialize()
        {
            _volcanus = Owner as Volcanus;
            Debug.Assert(_volcanus != null, "VolcanusSkill의 소유자가 Volcanus가 아닙니다.", this);
            if (DamageCaster == null)
                DamageCaster = GetComponentInChildren<DamageCaster>(true);
            OnVolcanusInitialize();
        }

        protected sealed override void OnCancel()
        {
            OnVolcanusCancel();
            EndAttackAnimation();
        }

        protected override void OnCompleted()
        {
            EndAttackAnimation();
        }

        protected virtual void OnVolcanusInitialize() { }
        protected virtual void OnVolcanusCancel() { }

        protected float Scaled(float duration) => duration / Speed;

        protected void CastDamage(float damage, DamageType type)
        {
            CastDamage(DamageCaster, damage, type);
        }

        protected void CastDamage(DamageCaster damageCaster, float damage, DamageType type)
        {
            if (damageCaster == null) return;
            damageCaster.Cast(new DamageData(damage, type));
        }

        protected IEnumerator StrikeWindow(
            DamageCaster caster,
            Volcanus.StrikePart part,
            float duration,
            float damage,
            DamageType type,
            float radiusRatio = 0.6f)
        {
            if (caster == null || Boss == null)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            caster.SetRange(Boss.GetStrikeRadius(part, radiusRatio));
            caster.SetWorldPosition(Boss.GetStrikeCenter(part));
            caster.EnableCasting(new DamageData(damage, type), duration);
            float elapsed = 0f;
            while (elapsed < duration && caster.IsCasting)
            {
                caster.SetWorldPosition(Boss.GetStrikeCenter(part));
                elapsed += Time.deltaTime;
                yield return null;
            }
            caster.DisableCasting();
        }

        protected void CastAtStrike(DamageCaster caster, Volcanus.StrikePart part, float damage, DamageType type, float radiusRatio = 0.6f)
        {
            if (caster == null || Boss == null) return;
            caster.SetRange(Boss.GetStrikeRadius(part, radiusRatio));
            caster.SetWorldPosition(Boss.GetStrikeCenter(part));
            caster.Cast(new DamageData(damage, type));
        }

        protected void PlayAttackAnimation(VolcanusPiece piece, string stateName = null)
        {
            EndAttackAnimation();
            string playStateName = string.IsNullOrWhiteSpace(stateName) ? animationStateName : stateName;
            if (piece != null && piece.PlayAttackAnimation(playStateName))
                animatedPiece = piece;
        }

        protected void EndAttackAnimation()
        {
            if (animatedPiece != null) animatedPiece.PlayDefaultAnimation();
            animatedPiece = null;
        }
    }
}
