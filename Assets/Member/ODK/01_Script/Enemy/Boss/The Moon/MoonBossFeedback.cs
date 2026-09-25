using System;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    [Serializable]
    public class MoonFeedbackSlot
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private AudioClip audioClip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private BossPositionEvent onPlayed;

        public void Play(MonoBehaviour owner, AudioSource audioSource, Vector3 position)
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

            if (audioSource != null && audioClip != null)
                audioSource.PlayOneShot(audioClip, volume);

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
        [Header("Audio Output")]
        [SerializeField] private AudioSource audioSource;

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

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        public void PlayJump(Vector3 position) => jump?.Play(this, audioSource, position);
        public void PlayLanding(Vector3 position, bool strong)
        {
            if (strong) strongLanding?.Play(this, audioSource, position);
            else landing?.Play(this, audioSource, position);
        }
        public void PlayFragment(Vector3 position) => fragment?.Play(this, audioSource, position);
        public void PlayRockExplosion(Vector3 position) => rockExplosion?.Play(this, audioSource, position);
        public void PlayCloneThrow(Vector3 position) => cloneThrow?.Play(this, audioSource, position);
        public void PlayLaserFire(Vector3 position) => laserFire?.Play(this, audioSource, position);
        public void PlayShrink(Vector3 position) => shrink?.Play(this, audioSource, position);
        public void PlayDash(Vector3 position) => dash?.Play(this, audioSource, position);
        public void PlayPhaseTwo(Vector3 position) => phaseTwo?.Play(this, audioSource, position);
    }
}
