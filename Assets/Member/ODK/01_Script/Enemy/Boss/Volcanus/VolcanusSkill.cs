using System.Collections;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using Member.ODK.Scripts.Enemys.Skills;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "VolcanusSkill")]
    public abstract class VolcanusSkill : AbstractEnemySkill
    {
        [field: Header("Damage")]
        [field: SerializeField] protected DamageCaster DamageCaster { get; private set; }

        [Header("Animation")]
        [SerializeField] private string animationStateName;

        protected Volcanus Boss => _volcanus;
        protected float DurationScale { get; private set; } = 1f;

        protected Volcanus _volcanus;

        private Coroutine attackCoroutine;
        private VolcanusPiece animatedPiece;
        private bool isInitialized;

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _volcanus = _enemy as Volcanus;
            Debug.Assert(_volcanus != null, "Volcanus Attack의 소유자가 Volcanus가 아닙니다.", this);
            if (DamageCaster == null)
                DamageCaster = GetComponentInChildren<DamageCaster>(true);
            OnInitialize();
            isInitialized = true;
        }

        public void InitializeAttack(Volcanus volcanus)
        {
            _volcanus = volcanus;
            if (isInitialized) return;
            OnInitialize();
            isInitialized = true;
        }

        public IEnumerator Play(float durationScale = 1f)
        {
            if (_volcanus == null)
                _volcanus = GetComponentInParent<Volcanus>();
            if (!CanUseSkill(_volcanus != null && _volcanus.Target != null ? _volcanus.Target.gameObject : null))
                yield break;

            DurationScale = Mathf.Max(0.01f, durationScale);
            UseSkill(_volcanus.Target != null ? _volcanus.Target.gameObject : null);
            yield return new WaitWhile(() => IsUsing);
        }

        public override void UseSkill(GameObject target = null)
        {
            if (IsUsing) return;
            if (_volcanus == null)
                _volcanus = GetComponentInParent<Volcanus>();

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
            EndAttackAnimation();
            if (IsUsing) base.StopSkill();
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnCancel() { }
        protected abstract IEnumerator Execute(GameObject target);

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

        private IEnumerator RunAttack(GameObject target)
        {
            yield return Execute(target);
            EndAttackAnimation();
            attackCoroutine = null;
            if (IsUsing) base.StopSkill();
        }

        protected virtual void OnDestroy()
        {
            StopSkill();
        }
    }
}
