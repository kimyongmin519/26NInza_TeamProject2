using KimLIb.AnimatorSystems;
using Member.KYM.Scripts.Enemies.Boss.Splines;

namespace Member.KYM.Scripts.Enemies.Boss.Splines.SplineEvents
{
    public struct SplineEventContext
    {
        public AbstractBoss Boss { get; private set; }
        public AnimParamSO AnimParam { get; private set; }
        public BossSplineMover SplineMover { get; private set; }
        public int KnotIndex { get; private set; }

        public SplineEventContext(
            AbstractBoss boss,
            AnimParamSO animParam,
            BossSplineMover splineMover = null,
            int knotIndex = -1)
        {
            Boss = boss;
            AnimParam = animParam;
            SplineMover = splineMover;
            KnotIndex = knotIndex;
        }

        public SplineEventContext WithKnotIndex(int knotIndex)
        {
            return new SplineEventContext(
                Boss,
                AnimParam,
                SplineMover,
                knotIndex);
        }
    }
}
