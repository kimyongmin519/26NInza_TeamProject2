using System;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public enum VolcanusFeedbackType
    {
        Ready,
        Step,
        Impact,
        Quake,
        Boulder,
        Damaged,
        PhaseTwo,
        Death
    }

    [Serializable]
    public class VolcanusFeedbackSlot
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private AudioClip audioClip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private BossPositionEvent onPlayed;

        public void Play(AudioSource source, Vector3 position)
        {
            if (effectPrefab != null)
            {
                GameObject effect = UnityEngine.Object.Instantiate(
                    effectPrefab,
                    position,
                    Quaternion.identity
                );
                UnityEngine.Object.Destroy(effect, GetLifeTime(effect));
            }
            if (source != null && audioClip != null) source.PlayOneShot(audioClip, volume);
            onPlayed?.Invoke(position);
        }

        private static float GetLifeTime(GameObject effect)
        {
            float lifeTime = 5f;
            foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particle.main;
                lifeTime = Mathf.Max(lifeTime, main.duration + main.startLifetime.constantMax);
            }
            return lifeTime;
        }
    }

    [DisallowMultipleComponent]
    public class VolcanusFeedback : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private VolcanusFeedbackSlot ready = new VolcanusFeedbackSlot();
        [SerializeField] private VolcanusFeedbackSlot step = new VolcanusFeedbackSlot();
        [SerializeField] private VolcanusFeedbackSlot impact = new VolcanusFeedbackSlot();
        [SerializeField] private VolcanusFeedbackSlot quake = new VolcanusFeedbackSlot();
        [SerializeField] private VolcanusFeedbackSlot boulder = new VolcanusFeedbackSlot();
        [SerializeField] private VolcanusFeedbackSlot damaged = new VolcanusFeedbackSlot();
        [SerializeField] private VolcanusFeedbackSlot phaseTwo = new VolcanusFeedbackSlot();
        [SerializeField] private VolcanusFeedbackSlot death = new VolcanusFeedbackSlot();

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        public void Play(VolcanusFeedbackType type, Vector3 position)
        {
            GetSlot(type)?.Play(audioSource, position);
        }

        private VolcanusFeedbackSlot GetSlot(VolcanusFeedbackType type)
        {
            return type switch
            {
                VolcanusFeedbackType.Ready => ready,
                VolcanusFeedbackType.Step => step,
                VolcanusFeedbackType.Impact => impact,
                VolcanusFeedbackType.Quake => quake,
                VolcanusFeedbackType.Boulder => boulder,
                VolcanusFeedbackType.Damaged => damaged,
                VolcanusFeedbackType.PhaseTwo => phaseTwo,
                VolcanusFeedbackType.Death => death,
                _ => null
            };
        }
    }
}
