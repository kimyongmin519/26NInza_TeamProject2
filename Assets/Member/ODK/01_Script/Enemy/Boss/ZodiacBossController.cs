using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Zodiac
{
    public class ZodiacBossController : EnemyController
    {
        private const float SegmentHealth = 1212f;
        private const int SegmentCount = 12;

        [Header("Arena")]
        [SerializeField] private ZodiacArena2D arenaPrefab;

        [Header("Patterns")]
        [SerializeField] private ZodiacPattern[] patterns;
        [SerializeField] private float firstPatternDelay = 2f;
        [SerializeField] private float patternInterval = 1.5f;

        [Header("Phase")]
        [SerializeField]
        private float speedIncreasePerBreak = 0.08f;

        [Header("Constellations")]
        [SerializeField]
        private ZodiacConstellation[] constellations;

        private HealthModule health;
        private ZodiacArena2D arena;

        private int brokenConstellationCount;
        private int previousPatternIndex = -1;

        private readonly List<Transform> players = new();

        public float PatternSpeed =>
            1f + brokenConstellationCount * speedIncreasePerBreak;

        public ZodiacArena2D Arena => arena;

        public IReadOnlyList<Transform> Players => players;

        protected override void AfterInitializeModules()
        {
            base.AfterInitializeModules();

            health = GetModule<HealthModule>();

            Debug.Assert(
                health != null,
                "Zodiac에게 HealthModule이 없습니다."
            );

            health.SetMaxHealth(
                SegmentHealth * SegmentCount
            );

            health.OnHealthChanged += HandleHealthChanged;

            SpawnArena();
            FindPlayers();
            InitializePatterns();

            StartCoroutine(PatternLoop());
        }

        private void SpawnArena()
        {
            if (arenaPrefab == null)
                return;

            arena = Instantiate(
                arenaPrefab,
                transform.position,
                Quaternion.identity
            );

            arena.Initialize(this);
        }

        private void FindPlayers()
        {
            players.Clear();

            GameObject[] foundPlayers =
                GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject player in foundPlayers)
            {
                players.Add(player.transform);
            }
        }

        private void InitializePatterns()
        {
            foreach (ZodiacPattern pattern in patterns)
            {
                if (pattern == null)
                    continue;

                pattern.Initialize(this);
            }
        }

        private IEnumerator PatternLoop()
        {
            yield return new WaitForSeconds(firstPatternDelay);

            while (health != null && !health.IsDead)
            {
                ZodiacPattern pattern = GetNextPattern();

                if (pattern == null)
                    yield break;

                yield return pattern.Execute();

                float wait =
                    patternInterval /
                    Mathf.Max(0.01f, PatternSpeed);

                yield return new WaitForSeconds(wait);
            }
        }

        private ZodiacPattern GetNextPattern()
        {
            if (patterns == null || patterns.Length == 0)
                return null;

            if (patterns.Length == 1)
                return patterns[0];

            int index;

            do
            {
                index = Random.Range(0, patterns.Length);
            }
            while (index == previousPatternIndex);

            previousPatternIndex = index;

            return patterns[index];
        }

        private void HandleHealthChanged(
            float currentHealth,
            float maxHealth)
        {
            float lostHealth =
                maxHealth - currentHealth;

            int shouldBeBroken =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        lostHealth / SegmentHealth
                    ),
                    0,
                    SegmentCount
                );

            while (
                brokenConstellationCount <
                shouldBeBroken)
            {
                BreakNextConstellation();
            }
        }

        private void BreakNextConstellation()
        {
            if (
                brokenConstellationCount <
                constellations.Length)
            {
                constellations[
                    brokenConstellationCount
                ]?.Break();
            }

            brokenConstellationCount++;
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnHealthChanged -=
                    HandleHealthChanged;
        }
    }
}