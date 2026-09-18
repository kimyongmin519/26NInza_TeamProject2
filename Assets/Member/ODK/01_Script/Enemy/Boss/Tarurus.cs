using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Zodiac
{
    public class TaurusPattern : ZodiacPattern
    {
        [Header("Spawn")]
        [SerializeField] private Transform taurusSpawnPoint;
        [SerializeField] private ZodiacBullCharge bullPrefab;

        [Header("Split")]
        [SerializeField] private float splitDistance = 1.5f;
        [SerializeField] private float splitDuration = 0.35f;

        [Header("Aim")]
        [SerializeField] private float aimDuration = 0.8f;

        [Header("Charge")]
        [SerializeField] private float chargeSpeed = 18f;
        [SerializeField] private float chargeDuration = 3f;

        [Header("Pattern")]
        [SerializeField] private float endDelay = 0.4f;

        private int activeBullCount;

        public override IEnumerator Execute()
        {
            if (bullPrefab == null)
                yield break;

            if (Players == null || Players.Count == 0)
                yield break;

            int bullCount = Mathf.Max(
                1,
                Mathf.CeilToInt(Players.Count * 0.5f)
            );

            activeBullCount = bullCount;

            List<Transform> targets =
                CreateTargetList(bullCount);

            Vector2 spawnPosition =
                taurusSpawnPoint != null
                    ? taurusSpawnPoint.position
                    : Boss.transform.position;

            for (int i = 0; i < bullCount; i++)
            {
                float offset =
                    GetSplitOffset(i, bullCount);

                Vector2 splitPosition =
                    spawnPosition +
                    Vector2.right * offset;

                ZodiacBullCharge bull =
                    Instantiate(
                        bullPrefab,
                        spawnPosition,
                        Quaternion.identity
                    );

                bull.Initialize(
                    owner: Boss,
                    target: targets[i],
                    splitPosition: splitPosition,

                    splitDuration:
                        splitDuration / Speed,

                    aimDuration:
                        aimDuration / Speed,

                    chargeSpeed:
                        chargeSpeed * Speed,

                    chargeDuration:
                        chargeDuration / Speed,

                    arena: Arena
                );

                bull.OnFinished += HandleBullFinished;
            }

            while (activeBullCount > 0)
            {
                yield return null;
            }

            yield return Wait(endDelay);
        }

        private List<Transform> CreateTargetList(
            int count)
        {
            List<Transform> availableTargets =
                new List<Transform>();

            foreach (Transform player in Players)
            {
                if (player != null)
                    availableTargets.Add(player);
            }

            // 간단한 셔플
            for (
                int i = availableTargets.Count - 1;
                i > 0;
                i--)
            {
                int randomIndex =
                    Random.Range(0, i + 1);

                (
                    availableTargets[i],
                    availableTargets[randomIndex]
                ) =
                (
                    availableTargets[randomIndex],
                    availableTargets[i]
                );
            }

            List<Transform> result =
                new List<Transform>();

            for (int i = 0; i < count; i++)
            {
                if (availableTargets.Count == 0)
                    break;

                result.Add(
                    availableTargets[
                        i % availableTargets.Count
                    ]
                );
            }

            return result;
        }

        private float GetSplitOffset(
            int index,
            int count)
        {
            if (count <= 1)
                return 0f;

            // 예:
            //
            // 3마리
            // -1.5 / 0 / +1.5
            //
            // 4마리
            // -1.5 / -0.5 / +0.5 / +1.5

            float normalized =
                index / (float)(count - 1);

            return Mathf.Lerp(
                -splitDistance,
                splitDistance,
                normalized
            );
        }

        private void HandleBullFinished(
            ZodiacBullCharge bull)
        {
            bull.OnFinished -=
                HandleBullFinished;

            activeBullCount =
                Mathf.Max(
                    0,
                    activeBullCount - 1
                );
        }
    }
}