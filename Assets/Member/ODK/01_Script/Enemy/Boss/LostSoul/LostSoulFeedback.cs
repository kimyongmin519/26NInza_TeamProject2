using KimLIb.SoundSystem;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulFeedback : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private SoundClipSO teleport;
        [SerializeField] private SoundClipSO centerMove;

        [Header("Attack")]
        [SerializeField] private SoundClipSO slash;
        [SerializeField] private SoundClipSO cast;
        [SerializeField] private SoundClipSO soulProjectile;
        [SerializeField] private SoundClipSO weakSoulProjectile;
        [SerializeField] private SoundClipSO beamWarning;
        [SerializeField] private SoundClipSO beamFire;

        [Header("Fade Attack")]
        [SerializeField] private SoundClipSO fadeOut;
        [SerializeField] private SoundClipSO fadeSlash;
        [SerializeField] private SoundClipSO directionCue;

        [Header("Desperation")]
        [SerializeField] private SoundClipSO desperationScream;

        [Header("Damage")]
        [SerializeField] private SoundClipSO hit;
        [SerializeField] private SoundClipSO death;

        [SerializeField] private float projectileSoundInterval = 0.07f;

        private float nextProjectileSoundTime;

        public void PlayTeleport() => Play(teleport);
        public void PlayCenterMove() => Play(centerMove);
        public void PlaySlash() => Play(slash);
        public void PlayCast() => Play(cast);
        public void PlayWeakSoul() => Play(weakSoulProjectile);
        public void PlayBeamWarning() => Play(beamWarning);
        public void PlayBeamFire() => Play(beamFire);
        public void PlayFadeOut() => Play(fadeOut);
        public void PlayFadeSlash() => Play(fadeSlash);
        public void PlayDesperationScream() => Play(desperationScream);
        public void PlayDirectionCue() => Play(directionCue);
        public void PlayHit() => Play(hit);
        public void PlayDeath() => Play(death);

        public void PlaySoulProjectile()
        {
            if (Time.time < nextProjectileSoundTime) return;
            nextProjectileSoundTime = Time.time + projectileSoundInterval;
            Play(soulProjectile);
        }

        private void Play(SoundClipSO clip)
        {
            ODKSoundPlayback.Play(clip, transform.position);
        }
    }
}
