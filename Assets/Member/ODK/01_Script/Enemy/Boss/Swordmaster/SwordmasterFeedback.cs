using System;
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
        Hit,
        Death
    }

    [Serializable]
    public class SwordmasterFeedbackSlot
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private SoundClipSO soundClip;
        [SerializeField] private BossPositionEvent onPlayed;

        public void Play(Vector3 position)
        {
            if (effectPrefab != null)
            {
                GameObject effect = UnityEngine.Object.Instantiate(effectPrefab, position, Quaternion.identity);
                UnityEngine.Object.Destroy(effect, GetEffectLifeTime(effect));
            }

            ODKSoundPlayback.Play(soundClip, position);
            onPlayed?.Invoke(position);
        }

        private static float GetEffectLifeTime(GameObject effect)
        {
            float lifeTime = 3f;
            foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particle.main;
                lifeTime = Mathf.Max(lifeTime, main.duration + main.startLifetime.constantMax);
            }
            return lifeTime;
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
        [SerializeField] private float swordCueInterval = 0.06f;
        [SerializeField] private float volleyCueInterval = 0.08f;

        [Header("Damage")]
        [SerializeField] private SwordmasterFeedbackSlot hit = new SwordmasterFeedbackSlot();
        [SerializeField] private SwordmasterFeedbackSlot death = new SwordmasterFeedbackSlot();

        private float nextSwordCueTime;
        private float nextVolleyCueTime;

        public void Play(SwordmasterCue cue, Vector3 position)
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
            GetSlot(cue)?.Play(position);
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
                case SwordmasterCue.Hit: return hit;
                case SwordmasterCue.Death: return death;
                default: return null;
            }
        }
    }
}
