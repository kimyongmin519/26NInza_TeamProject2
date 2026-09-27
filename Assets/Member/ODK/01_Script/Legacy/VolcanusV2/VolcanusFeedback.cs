using KimLIb.SoundSystem;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus.Legacy
{
    public class VolcanusFeedback : MonoBehaviour
    {
        [Header("SoundClipSO Slots")]
        [SerializeField] private SoundClipSO readyClip;
        [SerializeField] private SoundClipSO impactClip;
        [SerializeField] private SoundClipSO laserClip;
        [SerializeField] private SoundClipSO missileClip;
        [SerializeField] private SoundClipSO phaseClip;
        [SerializeField] private SoundClipSO damageClip;
        [SerializeField] private SoundClipSO deathClip;

        public void PlayReady() => Play(readyClip);
        public void PlayImpact() => Play(impactClip);
        public void PlayLaser() => Play(laserClip);
        public void PlayMissile() => Play(missileClip);
        public void PlayPhase() => Play(phaseClip);
        public void PlayDamage() => Play(damageClip);
        public void PlayDeath() => Play(deathClip);

        private void Play(SoundClipSO clip)
        {
            if (clip != null) ODKSoundPlayback.Play(clip, transform.position);
        }
    }
}
