using System.Collections;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public abstract class MoonSkill : ODKBossSkill
    {
        [Header("Animation")]
        [SerializeField] private string animationStateName;

        protected MoonBoss Boss { get; private set; }

        protected sealed override void OnInitialize()
        {
            Boss = Owner as MoonBoss;
            Debug.Assert(Boss != null, "MoonSkill의 소유자가 MoonBoss가 아닙니다.", this);
            OnMoonInitialize();
        }

        protected sealed override IEnumerator Execute(GameObject target)
        {
            Boss?.PlayAnimation(animationStateName);
            yield return ExecuteMoon(target);
        }

        protected virtual void OnMoonInitialize() { }
        protected abstract IEnumerator ExecuteMoon(GameObject target);
    }
}
