using System.Collections;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    public abstract class SwordmasterSkill : ODKBossSkill
    {
        protected Swordmaster Boss { get; private set; }
        protected virtual bool ReturnSwordsOnComplete => true;

        protected sealed override void OnInitialize()
        {
            Boss = Owner as Swordmaster;
            Debug.Assert(Boss != null, "SwordmasterSkill owner must be Swordmaster.", this);
            OnSwordmasterInitialize();
        }

        protected sealed override IEnumerator Execute(GameObject target)
        {
            Boss?.SetActing(true);
            if (Boss != null) yield return Boss.SummonSwords(DurationScale);
            yield return ExecuteSwordmaster(target);
        }

        protected virtual void OnSwordmasterInitialize() { }
        protected abstract IEnumerator ExecuteSwordmaster(GameObject target);

        protected override void OnCompleted()
        {
            if (ReturnSwordsOnComplete)
                Boss?.ReturnControlledSwords();
            Boss?.SetActing(false);
        }

        protected override void OnCancel()
        {
            Boss?.StopMotion();
            Boss?.ReturnControlledSwords();
            OnSwordmasterCancel();
            Boss?.SetActing(false);
        }

        protected virtual void OnSwordmasterCancel() { }
    }
}
