using System.Collections;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public abstract class MoonSkill : ODKBossSkill
    {
        [Header("Animation")]
        [SerializeField] private string animationStateName;

        protected MoonBoss Boss { get; private set; }
        protected virtual bool UsesAmbientFloating => true;

        protected sealed override void OnInitialize()
        {
            Boss = Owner as MoonBoss;
            Debug.Assert(Boss != null, "MoonSkill owner must be MoonBoss.", this);
            OnMoonInitialize();
        }

        protected sealed override IEnumerator Execute(GameObject target)
        {
            Boss?.PlayAnimation(animationStateName);
            if (!UsesAmbientFloating) Boss?.SetAmbientFloating(false);
            yield return ExecuteMoon(target);
            if (!UsesAmbientFloating) Boss?.SetAmbientFloating(true);
        }

        protected sealed override void OnCancel()
        {
            Boss?.CancelRegisteredSpawns();
            if (!UsesAmbientFloating) Boss?.SetAmbientFloating(true);
            OnMoonCancel();
        }

        protected DamageCaster CreateCaster(string casterName)
        {
            GameObject casterObject = new GameObject(casterName);
            casterObject.transform.SetParent(transform, false);
            return casterObject.AddComponent<DamageCaster>();
        }

        protected virtual void OnMoonInitialize() { }
        protected virtual void OnMoonCancel() { }
        protected abstract IEnumerator ExecuteMoon(GameObject target);
    }
}
