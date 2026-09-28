using GGMLib.ObjectPool.Runtime;
using KimLIb.SoundSystem;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulFeedback : MonoBehaviour
    {
        [Header("Pooled VFX")]
        [SerializeField] private PoolItemSO teleportEffect;
        [SerializeField] private PoolItemSO castEffect;
        [SerializeField] private PoolItemSO portalEffect;
        [SerializeField] private PoolItemSO eyeFlashEffect;
        [SerializeField] private PoolItemSO purpleProjectileEffect;
        [SerializeField] private PoolItemSO cyanProjectileEffect;
        [SerializeField] private PoolItemSO purpleImpactEffect;
        [SerializeField] private PoolItemSO cyanImpactEffect;
        [SerializeField] private PoolItemSO hitEffect;
        [SerializeField] private PoolItemSO deathEffect;
        [SerializeField] private Vector3 bodyEffectOffset = new Vector3(0f, 1.2f, 0f);

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
        [SerializeField] private float beamSoundInterval = 0.08f;

        private float nextProjectileSoundTime;
        private float nextBeamWarningTime;
        private float nextBeamFireTime;
        private LostSoulVfxPool vfxPool;

        private void Awake()
        {
            vfxPool = GetComponent<LostSoulVfxPool>();
        }

        public void PlayTeleport()
        {
            Play(teleport);
            PlayEffect(teleportEffect, transform.position + bodyEffectOffset);
        }

        public void PlayPortalOpen(Vector3 position)
        {
            Play(cast);
            PlayEffect(portalEffect != null ? portalEffect : castEffect, position);
        }

        public void PlayPortalPulse(Vector3 position)
        {
            PlayEffect(portalEffect != null ? portalEffect : castEffect, position);
        }

        public void PlayEyeFlash(Vector3 position)
        {
            PlayEffect(eyeFlashEffect != null ? eyeFlashEffect : purpleImpactEffect, position);
        }

        public void PlayCenterMove()
        {
            Play(centerMove);
            PlayEffect(teleportEffect, transform.position + bodyEffectOffset);
        }

        public void PlaySlash() => Play(slash);
        public void PlayCast()
        {
            Play(cast);
            PlayEffect(castEffect, transform.position + bodyEffectOffset);
        }

        public void PlayWeakSoul(Vector3 position)
        {
            Play(weakSoulProjectile);
            PlayEffect(cyanProjectileEffect, position);
        }
        public void PlayBeamWarning()
        {
            if (Time.time < nextBeamWarningTime) return;
            nextBeamWarningTime = Time.time + beamSoundInterval;
            Play(beamWarning);
        }
        public void PlayBeamFire()
        {
            if (Time.time < nextBeamFireTime) return;
            nextBeamFireTime = Time.time + beamSoundInterval;
            Play(beamFire);
        }
        public void PlayFadeOut() => Play(fadeOut);
        public void PlayFadeSlash() => Play(fadeSlash);
        public void PlayDesperationScream() => Play(desperationScream);
        public void PlayDirectionCue() => Play(directionCue);
        public void PlayHit()
        {
            Play(hit);
            PlayEffect(hitEffect, transform.position + bodyEffectOffset);
        }

        public void PlayDeath()
        {
            Play(death);
            PlayEffect(deathEffect, transform.position + bodyEffectOffset);
        }

        public void PlaySoulProjectile(Vector3 position)
        {
            PlayEffect(purpleProjectileEffect, position);
            if (Time.time >= nextProjectileSoundTime)
            {
                nextProjectileSoundTime = Time.time + projectileSoundInterval;
                Play(soulProjectile);
            }
        }

        public void PlaySoulImpact(Vector3 position) =>
            PlayEffect(purpleImpactEffect, position);

        public void PlayWeakSoulImpact(Vector3 position) =>
            PlayEffect(cyanImpactEffect, position);

        private void Play(SoundClipSO clip)
        {
            ODKSoundPlayback.Play(clip, transform.position);
        }

        private void PlayEffect(PoolItemSO item, Vector3 position)
        {
            vfxPool?.Play(item, position, Quaternion.identity);
        }
    }
}
