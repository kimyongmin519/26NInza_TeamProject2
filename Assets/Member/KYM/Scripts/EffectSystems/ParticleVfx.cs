using UnityEngine;

namespace Member.KYM.Scripts.EffectSystems
{
    public class ParticleVfx : MonoBehaviour, IPlayableVfx
    {
        [field: SerializeField, Min(0f)] public float Duration { get; private set; } = 1f;
        [SerializeField] private ParticleSystem[] particles;

        public void PlayVfx()
        {
            foreach (ParticleSystem particle in particles)
            {
                if (particle != null)
                    particle.Play();
            }
        }

        public void StopVfx()
        {
            foreach (ParticleSystem particle in particles)
            {
                if (particle == null)
                    continue;

                particle.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
