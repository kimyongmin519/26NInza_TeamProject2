using KimLIb.AnimatorSystems;

namespace Member.KYM.Scripts.Enemies.Boss.Splines.SplineEvents
{
    public struct SplineEventContext
    {
        public AbstractBoss Boss { get; private set; }
        public AnimParamSO AnimParam { get; private set; }

        public SplineEventContext(AbstractBoss boss, AnimParamSO animParam)
        {
            Boss = boss;
            AnimParam = animParam;
        }
    }
}