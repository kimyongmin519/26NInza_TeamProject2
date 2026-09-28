using System;
using GGMLib.ObjectPool.Runtime;
using KimLIb.SoundSystem;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    public enum SwordmasterCue
    {
        Teleport,
        SummonSwords,
        DashReady,
        DashStart,
        DashEnd,
        ThrustGather,
        ThrustStrike,
        CrossfireReady,
        VolleyWarning,
        VolleyFire,
        FinalCross,
        SwordLaunch,
        SwordDispelled,
        SwordRecall,
        SwordImpact,
        SwordBlink,
        Hit,
        Death
    }

    [Serializable]
    public class SwordmasterFeedbackSlot
    {
        [SerializeField] private PoolItemSO effectItem;
        [SerializeField] private Color effectTint = Color.white;
        [SerializeField] private float rotationOffset;
        [SerializeField] private SoundClipSO soundClip;
        [SerializeField] private BossPositionEvent onPlayed;

        public void Play(
            SwordmasterVfxPool vfxPool,
            Vector3 position,
            float rotationZ = 0f,
            Vector3? effectPosition = null,
            int sortingOrderOverride = int.MinValue)
        {
            vfxPool?.Play(
                effectItem,
                effectPosition ?? position,
                Quaternion.Euler(0f, 0f, rotationZ + rotationOffset),
                effectTint,
                sortingOrderOverride
            );

            ODKSoundPlayback.Play(soundClip, position);
            onPlayed?.Invoke(position);
        }
    }

    [DisallowMultipleComponent]
    public class SwordmasterFeedback : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private SwordmasterFeedbackSlot teleport = new SwordmasterFeedbackSlot();
        [SerializeField] private SwordmasterFeedbackSlot summonSwords = new SwordmasterFeedbackSlot();

        [Header("Dash Volley")]
        [SerializeField] private SwordmasterFeedbackSlot dashReady = new SwordmasterFeedbackSlot();
        [SerializeField] private SwordmasterFeedbackSlot dashStart = new SwordmasterFeedbackSlot();
        [SerializeField] private SwordmasterFeedbackSlot dashEnd = new SwordmasterFeedbackSlot();

        [Header("Triple Thrust")]
        [SerializeField] private SwordmasterFeedbackSlot thrustGather = new SwordmasterFeedbackSlot();
        [SerializeField] private SwordmasterFeedbackSlot thrustStrike = new SwordmasterFeedbackSlot();

        [Header("Crossfire")]
        [SerializeField] private SwordmasterFeedbackSlot crossfireReady = new SwordmasterFeedbackSlot();
        [SerializeField] private SwordmasterFeedbackSlot volleyWarning = new SwordmasterFeedbackSlot();
        [SerializeField] private SwordmasterFeedbackSlot volleyFire = new SwordmasterFeedbackSlot();
        [SerializeField] private SwordmasterFeedbackSlot finalCross = new SwordmasterFeedbackSlot();

        [Header("Sword")]
        [SerializeField] private SwordmasterFeedbackSlot swordLaunch = new SwordmasterFeedbackSlot();
        [SerializeField] private SwordmasterFeedbackSlot swordDispelled = new SwordmasterFeedbackSlot();
        [SerializeField] private SwordmasterFeedbackSlot swordRecall = new SwordmasterFeedbackSlot();
        [SerializeField] private SwordmasterFeedbackSlot swordImpact = new SwordmasterFeedbackSlot();
        [SerializeField] private SwordmasterFeedbackSlot swordBlink = new SwordmasterFeedbackSlot();
        [SerializeField] private float swordCueInterval = 0.06f;
        [SerializeField] private float volleyCueInterval = 0.08f;

        [Header("Damage")]
        [SerializeField] private SwordmasterFeedbackSlot hit = new SwordmasterFeedbackSlot();
        [SerializeField] private SwordmasterFeedbackSlot death = new SwordmasterFeedbackSlot();

        private float nextSwordCueTime;
        private float nextVolleyCueTime;
        private SwordmasterVfxPool vfxPool;

        private void Awake()
        {
            vfxPool = GetComponent<SwordmasterVfxPool>();
        }

        public void Play(
            SwordmasterCue cue,
            Vector3 position,
            float rotationZ = 0f,
            Vector3? effectPosition = null,
            int sortingOrderOverride = int.MinValue)
        {
            if (IsSwordCue(cue))
            {
                if (Time.time < nextSwordCueTime) return;
                nextSwordCueTime = Time.time + swordCueInterval;
            }
            if (cue == SwordmasterCue.VolleyWarning || cue == SwordmasterCue.VolleyFire)
            {
                if (Time.time < nextVolleyCueTime) return;
                nextVolleyCueTime = Time.time + volleyCueInterval;
            }
            GetSlot(cue)?.Play(vfxPool, position, rotationZ, effectPosition, sortingOrderOverride);
        }

        private static bool IsSwordCue(SwordmasterCue cue) =>
            cue == SwordmasterCue.SwordLaunch ||
            cue == SwordmasterCue.SwordDispelled ||
            cue == SwordmasterCue.SwordRecall ||
            cue == SwordmasterCue.SwordImpact;

        private SwordmasterFeedbackSlot GetSlot(SwordmasterCue cue)
        {
            switch (cue)
            {
                case SwordmasterCue.Teleport: return teleport;
                case SwordmasterCue.SummonSwords: return summonSwords;
                case SwordmasterCue.DashReady: return dashReady;
                case SwordmasterCue.DashStart: return dashStart;
                case SwordmasterCue.DashEnd: return dashEnd;
                case SwordmasterCue.ThrustGather: return thrustGather;
                case SwordmasterCue.ThrustStrike: return thrustStrike;
                case SwordmasterCue.CrossfireReady: return crossfireReady;
                case SwordmasterCue.VolleyWarning: return volleyWarning;
                case SwordmasterCue.VolleyFire: return volleyFire;
                case SwordmasterCue.FinalCross: return finalCross;
                case SwordmasterCue.SwordLaunch: return swordLaunch;
                case SwordmasterCue.SwordDispelled: return swordDispelled;
                case SwordmasterCue.SwordRecall: return swordRecall;
                case SwordmasterCue.SwordImpact: return swordImpact;
                case SwordmasterCue.SwordBlink: return swordBlink;
                case SwordmasterCue.Hit: return hit;
                case SwordmasterCue.Death: return death;
                default: return null;
            }
        }
    }
}
