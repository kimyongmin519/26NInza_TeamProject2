using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    [DisallowMultipleComponent]
    public sealed class BossArenaAmbience : MonoBehaviour
    {
        [Header("Activation")]
        [SerializeField] private string targetBossType;
        [SerializeField, Min(0f)] private float lightFadeDuration = 1.25f;
        [SerializeField, Range(0f, 0.5f)] private float lightPulseAmount = 0.12f;
        [SerializeField, Min(0.1f)] private float lightPulseDuration = 2.4f;

        [Header("Scene Effects")]
        [SerializeField] private ParticleSystem[] ambientParticles;
        [SerializeField] private Light2D[] arenaLights;
        [SerializeField] private float[] lightIntensities;

        public bool IsActive { get; private set; }

        private BossIntroTimeline introTimeline;
        private Coroutine resolveRoutine;

        private void OnEnable()
        {
            PrepareEffects();
            resolveRoutine = StartCoroutine(ResolveIntro());
        }

        private IEnumerator ResolveIntro()
        {
            const int maximumFrames = 300;
            for (int frame = 0; frame < maximumFrames; frame++)
            {
                introTimeline = FindTargetIntro();
                if (introTimeline != null)
                    break;
                yield return null;
            }

            resolveRoutine = null;
            if (introTimeline == null || introTimeline.HasFinished)
            {
                Activate();
                yield break;
            }

            introTimeline.Finished += Activate;
        }

        private BossIntroTimeline FindTargetIntro()
        {
            BossIntroTimeline[] intros = FindObjectsByType<BossIntroTimeline>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (BossIntroTimeline intro in intros)
            {
                if (intro == null)
                    continue;

                PhasedBossController boss =
                    intro.GetComponentInParent<PhasedBossController>();
                if (boss == null)
                    boss = intro.GetComponentInChildren<PhasedBossController>(true);

                if (boss == null)
                    continue;

                if (string.IsNullOrWhiteSpace(targetBossType) ||
                    boss.GetType().Name.Contains(
                        targetBossType,
                        System.StringComparison.OrdinalIgnoreCase
                    ))
                {
                    return intro;
                }
            }

            return null;
        }

        [ContextMenu("Activate Ambience")]
        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
            if (introTimeline != null)
                introTimeline.Finished -= Activate;

            if (ambientParticles != null)
            {
                foreach (ParticleSystem particles in ambientParticles)
                {
                    if (particles != null)
                        particles.Play(true);
                }
            }

            if (arenaLights == null)
                return;

            for (int i = 0; i < arenaLights.Length; i++)
            {
                Light2D light = arenaLights[i];
                if (light == null)
                    continue;

                float target = lightIntensities != null && i < lightIntensities.Length
                    ? Mathf.Max(0f, lightIntensities[i])
                    : 0.5f;
                float delay = i * 0.08f;
                DOTween.To(
                        () => light.intensity,
                        value => light.intensity = value,
                        target,
                        lightFadeDuration
                    )
                    .SetDelay(delay)
                    .SetEase(Ease.OutSine)
                    .SetTarget(this)
                    .OnComplete(() => StartLightPulse(light, target));
            }
        }

        private void StartLightPulse(Light2D light, float baseIntensity)
        {
            if (light == null || !isActiveAndEnabled || lightPulseAmount <= 0f)
                return;

            float pulseTarget = baseIntensity * (1f + lightPulseAmount);
            DOTween.To(
                    () => light.intensity,
                    value => light.intensity = value,
                    pulseTarget,
                    lightPulseDuration
                )
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(this);
        }

        private void PrepareEffects()
        {
            IsActive = false;
            DOTween.Kill(this);

            if (ambientParticles != null)
            {
                foreach (ParticleSystem particles in ambientParticles)
                {
                    if (particles == null)
                        continue;
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            if (arenaLights == null)
                return;

            foreach (Light2D light in arenaLights)
            {
                if (light != null)
                    light.intensity = 0f;
            }
        }

        private void OnDisable()
        {
            if (resolveRoutine != null)
            {
                StopCoroutine(resolveRoutine);
                resolveRoutine = null;
            }

            if (introTimeline != null)
                introTimeline.Finished -= Activate;

            DOTween.Kill(this);
        }
    }
}
