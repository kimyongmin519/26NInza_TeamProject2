using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public abstract class VolcanusSkill : BossSkill
    {
        [field: Header("Damage")]
        [field: SerializeField] protected DamageCaster DamageCaster { get; private set; }

        [Header("Animation")]
        [SerializeField] private string animationStateName;

        protected Volcanus Boss => _volcanus;
        private VolcanusPiece animatedPiece;

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

        protected void CastDamage(float damage, DamageType type)
        {
            CastDamage(DamageCaster, damage, type);
        }

        protected void CastDamage(DamageCaster damageCaster, float damage, DamageType type)
        {
            if (damageCaster == null) return;
            damageCaster.Cast(new DamageData(damage, type));
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

        private Volcanus _volcanus;
    }
}
