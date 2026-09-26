using System;
using KimLIb.SoundSystem;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    [Serializable]
    public class MoonFeedbackSlot
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private SoundClipSO soundClip;
        [SerializeField] private BossPositionEvent onPlayed;

        public void Play(Vector3 position)
        {
            if (effectPrefab != null)
            {
                GameObject effect = UnityEngine.Object.Instantiate(
                    effectPrefab,
                    position,
                    Quaternion.identity
                );
                UnityEngine.Object.Destroy(effect, GetEffectLifeTime(effect));
            }

            ODKSoundPlayback.Play(soundClip, position);

            onPlayed?.Invoke(position);
        }

        private static float GetEffectLifeTime(GameObject effect)
        {
            float lifeTime = 5f;
            ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particle in particles)
            {
                ParticleSystem.MainModule main = particle.main;
                float particleLife = main.duration + main.startLifetime.constantMax;
                lifeTime = Mathf.Max(lifeTime, particleLife);
            }
            return lifeTime;
        }
    }

    [DisallowMultipleComponent]
    public class MoonBossFeedback : MonoBehaviour
    {
        [Header("Jump")]
        [SerializeField] private MoonFeedbackSlot jump = new MoonFeedbackSlot();
        [SerializeField] private MoonFeedbackSlot landing = new MoonFeedbackSlot();
        [SerializeField] private MoonFeedbackSlot strongLanding = new MoonFeedbackSlot();

        [Header("Projectile")]
        [SerializeField] private MoonFeedbackSlot fragment = new MoonFeedbackSlot();
        [SerializeField] private MoonFeedbackSlot rockExplosion = new MoonFeedbackSlot();
        [SerializeField] private MoonFeedbackSlot cloneThrow = new MoonFeedbackSlot();

        [Header("Light / Dash")]
        [SerializeField] private MoonFeedbackSlot laserFire = new MoonFeedbackSlot();
        [SerializeField] private MoonFeedbackSlot shrink = new MoonFeedbackSlot();
        [SerializeField] private MoonFeedbackSlot dash = new MoonFeedbackSlot();

        [Header("Phase")]
        [SerializeField] private MoonFeedbackSlot phaseTwo = new MoonFeedbackSlot();

        public void PlayJump(Vector3 position) => jump?.Play(position);
        public void PlayLanding(Vector3 position, bool strong)
        {
            if (strong) strongLanding?.Play(position);
            else landing?.Play(position);
        }
        public void PlayFragment(Vector3 position) => fragment?.Play(position);
        public void PlayRockExplosion(Vector3 position) => rockExplosion?.Play(position);
        public void PlayCloneThrow(Vector3 position) => cloneThrow?.Play(position);
        public void PlayLaserFire(Vector3 position) => laserFire?.Play(position);
        public void PlayShrink(Vector3 position) => shrink?.Play(position);
        public void PlayDash(Vector3 position) => dash?.Play(position);
        public void PlayPhaseTwo(Vector3 position) => phaseTwo?.Play(position);
    }
}
