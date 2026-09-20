using UnityEngine;

namespace Member.YKJ.Bosses
{
    public enum MimicTreasureHeight { Ground, FirstPlatform, SecondPlatform }

    public sealed class MimicTreasureHeightCycle
    {
        private readonly MimicTreasureHeight[] _order =
        {
            MimicTreasureHeight.Ground, MimicTreasureHeight.FirstPlatform, MimicTreasureHeight.SecondPlatform
        };
        private int _next = 3;

        public MimicTreasureHeight Next()
        {
            // Every three emissions use all heights once, without a fixed repeating order.
            if (_next == _order.Length)
            {
                for (int i = _order.Length - 1; i > 0; i--)
                {
                    int other = Random.Range(0, i + 1);
                    (_order[i], _order[other]) = (_order[other], _order[i]);
                }
                _next = 0;
            }
            return _order[_next++];
        }
    }
}
