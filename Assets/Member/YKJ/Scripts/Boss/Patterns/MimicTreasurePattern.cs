using System;
using UnityEngine;

namespace Member.YKJ.Bosses
{
    [Serializable]
    public sealed class MimicTreasurePattern : MimicPattern
    {
        [SerializeField, Min(1)] private int weaponCount = 15;
        [SerializeField, Min(0.01f)] private float interval = 0.5f;
        [Tooltip("Extra arc height above the higher endpoint, not the landing height.")]
        [SerializeField, Min(0.05f)] private float arcHeight = 0.75f;
        private MimicEmissionProgress _progress;
        private MimicTreasureHeightCycle _heights;

        public int EmittedCount => _progress?.Count ?? 0;
        public override bool CanStart() => Boss != null && Boss.CanEmitWeapons;

        public override void OnStart()
        {
            _progress = new MimicEmissionProgress(Mathf.Max(1, weaponCount), Mathf.Max(0.01f, interval));
            _heights = new MimicTreasureHeightCycle();
        }

        public override void OnUpdate(float deltaTime)
        {
            if (!_progress.Advance(deltaTime))
                return;

            Boss.EmitTreasureWeapon(_heights.Next(), arcHeight);
            if (_progress.Finished)
            {
                EndPattern();
                return;
            }

            if (_progress.IsTongueCheckpoint)
                InterruptWith(Boss.Tongue);
        }
    }
}
