using System;
using System.Collections;
using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Zodiac
{
    public class ZodiacBullCharge : MonoBehaviour
    {
        public event Action<ZodiacBullCharge>
            OnFinished;

        private enum BullState
        {
            None,
            Splitting,
            Aiming,
            Charging,
            Finished
        }

        [Header("Visual")]
        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [SerializeField]
        private LineRenderer aimLine;

        [SerializeField]
        private Animator animator;

        [Header("Collision")]
        [SerializeField]
        private Collider2D hitbox;

        [SerializeField]
        private float damage = 100f;

        [SerializeField]
        private bool destroyOnPlayerHit = false;

        [Header("Arena")]
        [SerializeField]
        private float outOfArenaMargin = 3f;

        private Agent owner;
        private Transform target;
        private ZodiacArena2D arena;

        private Vector2 splitPosition;

        private Vector2 chargeDirection;

        private float splitDuration;
        private float aimDuration;
        private float chargeSpeed;
        private float chargeDuration;

        private BullState state;

        private bool finished;

        public void Initialize(
            Agent owner,
            Transform target,
            Vector2 splitPosition,
            float splitDuration,
            float aimDuration,
            float chargeSpeed,
            float chargeDuration,
            ZodiacArena2D arena)
        {
            this.owner = owner;
            this.target = target;
            this.splitPosition = splitPosition;

            this.splitDuration =
                Mathf.Max(
                    0.01f,
                    splitDuration
                );

            this.aimDuration =
                Mathf.Max(
                    0.01f,
                    aimDuration
                );

            this.chargeSpeed =
                Mathf.Max(
                    0f,
                    chargeSpeed
                );

            this.chargeDuration =
                Mathf.Max(
                    0.1f,
                    chargeDuration
                );

            this.arena = arena;

            finished = false;

            if (hitbox != null)
                hitbox.enabled = false;

            if (aimLine != null)
            {
                aimLine.enabled = false;
                aimLine.positionCount = 2;
            }

            StartCoroutine(
                BullRoutine()
            );
        }

        private IEnumerator BullRoutine()
        {
            yield return SplitRoutine();

            if (target == null)
            {
                Finish();
                yield break;
            }

            yield return AimRoutine();

            if (target == null)
            {
                Finish();
                yield break;
            }

            yield return ChargeRoutine();

            Finish();
        }

        #region Split

        private IEnumerator SplitRoutine()
        {
            state = BullState.Splitting;

            if (animator != null)
            {
                animator.SetBool(
                    "Split",
                    true
                );
            }

            Vector2 startPosition =
                transform.position;

            float timer = 0f;

            while (timer < splitDuration)
            {
                timer += Time.deltaTime;

                float t =
                    Mathf.Clamp01(
                        timer /
                        splitDuration
                    );

                // 약간 부드럽게 분열
                t = Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

                transform.position =
                    Vector2.Lerp(
                        startPosition,
                        splitPosition,
                        t
                    );

                yield return null;
            }

            transform.position =
                splitPosition;

            if (animator != null)
            {
                animator.SetBool(
                    "Split",
                    false
                );
            }
        }

        #endregion

        #region Aim

        private IEnumerator AimRoutine()
        {
            state = BullState.Aiming;

            if (aimLine != null)
                aimLine.enabled = true;

            if (animator != null)
            {
                animator.SetBool(
                    "Aim",
                    true
                );
            }

            float timer = 0f;

            while (timer < aimDuration)
            {
                if (target == null)
                    yield break;

                timer += Time.deltaTime;

                Vector2 direction =
                    (
                        target.position -
                        transform.position
                    ).normalized;

                RotateToDirection(
                    direction
                );

                UpdateAimLine(
                    target.position
                );

                yield return null;
            }

            // 중요:
            // 여기서 방향을 고정한다.
            //
            // 돌진 도중에는 플레이어를
            // 더 이상 추적하지 않는다.

            chargeDirection =
                (
                    target.position -
                    transform.position
                ).normalized;

            if (chargeDirection ==
                Vector2.zero)
            {
                chargeDirection =
                    transform.right;
            }

            RotateToDirection(
                chargeDirection
            );

            if (aimLine != null)
                aimLine.enabled = false;

            if (animator != null)
            {
                animator.SetBool(
                    "Aim",
                    false
                );
            }
        }

        private void UpdateAimLine(
            Vector2 targetPosition)
        {
            if (aimLine == null)
                return;

            aimLine.SetPosition(
                0,
                transform.position
            );

            aimLine.SetPosition(
                1,
                targetPosition
            );
        }

        #endregion

        #region Charge

        private IEnumerator ChargeRoutine()
        {
            state = BullState.Charging;

            if (hitbox != null)
                hitbox.enabled = true;

            if (animator != null)
            {
                animator.SetBool(
                    "Charge",
                    true
                );
            }

            float timer = 0f;

            while (timer < chargeDuration)
            {
                timer += Time.deltaTime;

                transform.position +=
                    (Vector3)(
                        chargeDirection *
                        chargeSpeed *
                        Time.deltaTime
                    );

                if (IsOutsideArena())
                    break;

                yield return null;
            }

            if (animator != null)
            {
                animator.SetBool(
                    "Charge",
                    false
                );
            }

            if (hitbox != null)
                hitbox.enabled = false;
        }

        #endregion

        private bool IsOutsideArena()
        {
            if (arena == null)
                return false;

            Vector2 position =
                transform.position;

            if (
                position.x <
                arena.MinX -
                outOfArenaMargin)
                return true;

            if (
                position.x >
                arena.MaxX +
                outOfArenaMargin)
                return true;

            if (
                position.y <
                arena.MinY -
                outOfArenaMargin)
                return true;

            if (
                position.y >
                arena.MaxY +
                outOfArenaMargin)
                return true;

            return false;
        }

        private void RotateToDirection(
            Vector2 direction)
        {
            if (direction == Vector2.zero)
                return;

            float angle =
                Mathf.Atan2(
                    direction.y,
                    direction.x
                ) *
                Mathf.Rad2Deg;

            transform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );
        }

        private void OnTriggerEnter2D(
            Collider2D other)
        {
            if (state != BullState.Charging)
                return;

            if (owner != null &&
                other.transform.IsChildOf(
                    owner.transform
                ))
            {
                return;
            }

            HealthModule health =
                other.GetComponentInParent<
                    HealthModule
                >();

            if (health == null)
                return;

            ApplyDamage(
                health
            );

            if (destroyOnPlayerHit)
            {
                Finish();
            }
        }

        private void ApplyDamage(
            HealthModule health)
        {
            /*
             * 여기만 네 DamageData 정의가
             * 지금 코드에 없어서 정확한 생성자를
             * 알 수 없음.
             *
             * 예를 들어 DamageData가:
             *
             * new DamageData(damage, owner)
             *
             * 형태라면:
             *
             * health.ApplyDamage(
             *     new DamageData(
             *         damage,
             *         owner
             *     )
             * );
             *
             * 로 연결하면 끝.
             */

            Debug.Log(
                $"Taurus hit : {health.name}, Damage = {damage}"
            );
        }

        private void Finish()
        {
            if (finished)
                return;

            finished = true;
            state = BullState.Finished;

            StopAllCoroutines();

            if (aimLine != null)
                aimLine.enabled = false;

            if (hitbox != null)
                hitbox.enabled = false;

            OnFinished?.Invoke(this);

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // 외부에서 강제로 Destroy됐을 때도
            // 패턴이 영원히 기다리지 않게 처리.

            if (finished)
                return;

            finished = true;

            OnFinished?.Invoke(this);
        }
    }
}