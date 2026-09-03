using System.Collections;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using Member.ODK.Scripts.Enemys.Skills;
using UnityEngine;

public abstract class VolcanusSkill : AbstractEnemySkill
{
    [field: Header("Damage")]
    [field: SerializeField] protected DamageCaster DamageCaster { get; private set; }

    protected Volcanus Boss => _volcanus;
    protected float DurationScale { get; private set; } = 1f;

    protected Volcanus _volcanus;

    private Coroutine attackCoroutine;
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

    private IEnumerator RunAttack(GameObject target)
    {
        yield return Execute(target);
        attackCoroutine = null;
        if (IsUsing) base.StopSkill();
    }

    protected virtual void OnDestroy()
    {
        StopSkill();
    }
}
