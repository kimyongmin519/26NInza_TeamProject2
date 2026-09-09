using System.Collections;
using System.Collections.Generic;
using Member.ODK.Scripts.Enemys.Bosses;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public class MoonBoss : PhasedBossController
    {
        protected override IEnumerable<BossSkill> GetAttacks()
        {
            yield break;
        }

        protected override IEnumerator PhaseOneLoop()
        {
            yield return AttackWait();
        }

        protected override IEnumerator PhaseTwoLoop()
        {
            yield return AttackWait();
        }
    }
}
