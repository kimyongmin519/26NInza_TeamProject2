using System.Collections;
using KimLIb.SoundSystem;
using Member.KYM.Scripts.CoreSystems;
using Member.ODK.Scripts;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    [DisallowMultipleComponent]
    public sealed class BossBgmPlayback : MonoBehaviour
    {
        [SerializeField] private BgmManager managerPrefab;
        [SerializeField] private SoundClipSO bossBgm;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.7f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.8f;

        private HealthModule healthModule;
        private BossIntroTimeline introTimeline;
        private Coroutine playRoutine;

        private void OnEnable()
        {
            healthModule = GetComponent<HealthModule>();
            if (healthModule != null)
                healthModule.OnDeath += HandleBossDeath;

            playRoutine = StartCoroutine(PlayNextFrame());
        }

        private IEnumerator PlayNextFrame()
        {
            yield return null;
            playRoutine = null;

            introTimeline = GetComponentInChildren<BossIntroTimeline>(true);
            if (introTimeline == null)
                introTimeline = GetComponentInParent<BossIntroTimeline>();

            if (introTimeline != null &&
                !introTimeline.IsPlaying &&
                !introTimeline.HasFinished)
                yield break;

            Play();
        }

        public void Play()
        {
            if (BgmManager.Instance == null && managerPrefab != null)
                Instantiate(managerPrefab);

            if (bossBgm != null)
                BgmManager.Instance?.PlayBgm(bossBgm, fadeInDuration);
        }

        private void HandleBossDeath()
        {
            StopBossBgm();
        }

        private void OnDisable()
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }

            if (healthModule != null)
                healthModule.OnDeath -= HandleBossDeath;

            StopBossBgm();
        }

        private void StopBossBgm()
        {
            BgmManager manager = BgmManager.Instance;
            if (manager != null && manager.CurrentBgm == bossBgm)
                manager.StopBgm(fadeOutDuration);
        }
    }
}
