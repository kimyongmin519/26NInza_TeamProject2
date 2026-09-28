using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulFourDirectionsAttack : LostSoulSkill
    {
        [Header("Direction Cue")]
        [SerializeField] private SpriteRenderer[] directionArrows;
        [SerializeField] private LineRenderer eyeSparkle;
        [SerializeField] private LineRenderer slashEffect;
        [SerializeField] private LineRenderer slashGlowEffect;
        [SerializeField] private Vector2 eyeOffset = new Vector2(0f, 2.25f);
        [SerializeField] private float eyeSparkleDuration = 0.34f;
        [SerializeField] private float darknessFadeDuration = 0.18f;
        [SerializeField] private float arrowDuration = 0.78f;
        [SerializeField] private float arrowStartScale = 0.62f;
        [SerializeField] private float arrowPeakScale = 1.05f;
        [SerializeField] private int arrowSortingOrder = 500;
        [SerializeField] private string arrowSortingLayer = "UI";
        [SerializeField] private float arrowMoveDistance = 0.7f;
        [SerializeField] private float responseTime = 0.12f;
        [SerializeField] private float slashRevealDuration = 0.18f;
        [SerializeField] private float betweenCuts = 0.05f;
        [SerializeField] private Vector2Int cutCountRange = new Vector2Int(3, 7);

        [Header("Avoid Check")]
        [SerializeField] private float moveDistance = 1.35f;
        [SerializeField] private float jumpDistance = 0.85f;
        [SerializeField] private float crouchHeightRatio = 0.82f;
        [SerializeField] private float damage = 1f;
        [SerializeField] private float damageRadius = 1.35f;

        private static readonly Vector3[] Directions =
        {
            Vector3.left,
            Vector3.right,
            Vector3.up,
            Vector3.down
        };

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.MoveToArenaCenter();
            yield return PlayEyeSparkle();
            Boss.SetDarkness(true, darknessFadeDuration, 1f);
            yield return new WaitForSeconds(darknessFadeDuration / DurationScale);

            int minimumCuts = Mathf.Max(1, Mathf.Min(cutCountRange.x, cutCountRange.y));
            int maximumCuts = Mathf.Max(minimumCuts, Mathf.Max(cutCountRange.x, cutCountRange.y));
            int[] order = new int[Random.Range(minimumCuts, maximumCuts + 1)];
            for (int i = 0; i < order.Length; i++)
                order[i] = Random.Range(0, Directions.Length);

            Collider2D targetCollider = target.GetComponentInChildren<Collider2D>();
            for (int i = 0; i < order.Length; i++)
            {
                int cue = order[i];
                Vector3 before = target.transform.position;
                float beforeHeight = targetCollider != null ? targetCollider.bounds.size.y : 1f;

                Boss.PlayDirectionCueFeedback();
                yield return ShowDirectionCue(cue);
                yield return new WaitForSeconds(responseTime / DurationScale);

                MoveBossToCue(cue);
                Boss.PlaySwing("attack", 0f);
                Boss.PlayFadeSlashFeedback();
                Boss.ShakeCamera(0.82f);
                StartCoroutine(Boss.PulseOutline(slashRevealDuration / DurationScale));
                StartCoroutine(Boss.FlashDarknessReveal(slashRevealDuration / DurationScale));
                yield return PlaySlashEffect(cue, target.transform.position);

                Vector3 after = target.transform.position;
                float afterHeight = targetCollider != null ? targetCollider.bounds.size.y : beforeHeight;
                bool avoided = cue switch
                {
                    0 => after.x <= before.x - moveDistance,
                    1 => after.x >= before.x + moveDistance,
                    2 => after.y >= before.y + jumpDistance,
                    _ => afterHeight <= beforeHeight * crouchHeightRatio || after.y <= before.y - 0.2f
                };

                if (!avoided)
                {
                    Caster.ConfigureCircle(damageRadius, Boss.PlayerLayer);
                    Caster.SetWorldPosition(after);
                    Caster.Cast(new DamageData(damage, DamageType.Special));
                }

                yield return new WaitForSeconds(betweenCuts / DurationScale);
            }

            CleanupVisuals();
            Boss.SetDarkness(false, 0.16f);
            yield return new WaitForSeconds(0.16f / DurationScale);
        }

        private IEnumerator PlayEyeSparkle()
        {
            if (eyeSparkle == null) yield break;
            Vector3 center = Boss.transform.position + (Vector3)eyeOffset;
            eyeSparkle.useWorldSpace = true;
            eyeSparkle.positionCount = 5;
            eyeSparkle.loop = true;
            eyeSparkle.enabled = true;
            float scale = 0.05f;
            float alpha = 0f;

            void Refresh()
            {
                eyeSparkle.SetPosition(0, center + Vector3.up * scale);
                eyeSparkle.SetPosition(1, center + Vector3.right * scale * 0.36f);
                eyeSparkle.SetPosition(2, center + Vector3.down * scale);
                eyeSparkle.SetPosition(3, center + Vector3.left * scale * 0.36f);
                eyeSparkle.SetPosition(4, center + Vector3.up * scale);
                Color color = new Color(0.92f, 0.72f, 1f, alpha);
                eyeSparkle.startColor = color;
                eyeSparkle.endColor = color;
                eyeSparkle.widthMultiplier = 0.08f + scale * 0.03f;
            }

            Sequence sequence = DOTween.Sequence().SetTarget(eyeSparkle);
            sequence.Append(DOTween.To(() => scale, value => { scale = value; Refresh(); }, 0.72f,
                eyeSparkleDuration * 0.5f / DurationScale).SetEase(Ease.OutBack));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; Refresh(); }, 1f,
                eyeSparkleDuration * 0.32f / DurationScale));
            sequence.Append(DOTween.To(() => scale, value => { scale = value; Refresh(); }, 0.04f,
                eyeSparkleDuration * 0.5f / DurationScale).SetEase(Ease.InQuad));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; Refresh(); }, 0f,
                eyeSparkleDuration * 0.42f / DurationScale));
            yield return sequence.WaitForCompletion();
            eyeSparkle.enabled = false;
        }

        private IEnumerator ShowDirectionCue(int cue)
        {
            HideArrows();
            if (directionArrows == null || cue < 0 || cue >= directionArrows.Length || directionArrows[cue] == null)
                yield break;

            SpriteRenderer arrow = directionArrows[cue];
            Vector3 center = GetScreenCenter();
            Vector3 direction = Directions[cue];
            float duration = arrowDuration / DurationScale;
            Color transparent = new Color(0.88f, 0.52f, 1f, 0f);
            Color visible = new Color(0.98f, 0.84f, 1f, 0.82f);

            arrow.transform.position = center - direction * (arrowMoveDistance * 0.35f);
            arrow.transform.rotation = Quaternion.identity;
            arrow.transform.localScale = Vector3.one * arrowStartScale;
            arrow.sortingLayerName = arrowSortingLayer;
            arrow.sortingOrder = arrowSortingOrder;
            arrow.color = transparent;
            arrow.enabled = true;

            Sequence sequence = DOTween.Sequence().SetTarget(arrow);
            sequence.Append(arrow.transform.DOScale(arrowPeakScale, duration * 0.38f).SetEase(Ease.OutBack));
            sequence.Join(arrow.transform.DOMove(center + direction * arrowMoveDistance,
                duration * 0.72f).SetEase(Ease.OutCubic));
            sequence.Join(arrow.DOColor(visible, duration * 0.2f));
            sequence.Append(arrow.transform.DOScale(arrowPeakScale * 0.76f, duration * 0.24f).SetEase(Ease.InOutSine));
            sequence.Append(arrow.transform.DOScale(arrowPeakScale * 0.94f, duration * 0.18f).SetEase(Ease.OutQuad));
            sequence.Append(arrow.DOColor(transparent, duration * 0.2f).SetEase(Ease.InQuad));
            yield return sequence.WaitForCompletion();
            arrow.enabled = false;
        }

        private IEnumerator PlaySlashEffect(int cue, Vector3 playerPosition)
        {
            if (slashEffect == null)
            {
                yield return new WaitForSeconds(slashRevealDuration / DurationScale);
                yield break;
            }

            Vector3 axis = cue <= 1 ? Vector3.right : Vector3.up;
            float extent = cue <= 1 ? Boss.ArenaHalfWidth * 1.4f : Boss.ArenaHalfHeight * 1.8f;
            Vector3 start = playerPosition - axis * extent;
            Vector3 end = playerPosition + axis * extent;
            slashEffect.useWorldSpace = true;
            slashEffect.positionCount = 2;
            slashEffect.SetPosition(0, start);
            slashEffect.SetPosition(1, end);
            slashEffect.enabled = true;
            slashEffect.widthMultiplier = 0.12f;
            Color color = new Color(1f, 0.98f, 1f, 1f);
            slashEffect.startColor = color;
            slashEffect.endColor = color;

            if (slashGlowEffect != null)
            {
                slashGlowEffect.useWorldSpace = true;
                slashGlowEffect.positionCount = 2;
                slashGlowEffect.SetPosition(0, start);
                slashGlowEffect.SetPosition(1, end);
                slashGlowEffect.widthMultiplier = 1.8f;
                Color glow = new Color(0.78f, 0.22f, 1f, 0.7f);
                slashGlowEffect.startColor = glow;
                slashGlowEffect.endColor = glow;
                slashGlowEffect.enabled = true;
            }

            float width = slashEffect.widthMultiplier;
            float alpha = 1f;
            float glowWidth = slashGlowEffect != null ? slashGlowEffect.widthMultiplier : 0f;
            float glowAlpha = 0.7f;

            Sequence sequence = DOTween.Sequence().SetTarget(slashEffect);
            sequence.Append(DOTween.To(() => width, value =>
            {
                width = value;
                slashEffect.widthMultiplier = value;
            }, 0.42f, slashRevealDuration * 0.16f / DurationScale).SetEase(Ease.OutExpo));
            sequence.Append(DOTween.To(() => width, value =>
            {
                width = value;
                slashEffect.widthMultiplier = value;
            }, 0.015f, slashRevealDuration * 0.84f / DurationScale).SetEase(Ease.InExpo));
            sequence.Join(DOTween.To(() => alpha, value =>
            {
                alpha = value;
                Color faded = color;
                faded.a = value;
                slashEffect.startColor = faded;
                slashEffect.endColor = faded;
            }, 0f, slashRevealDuration * 0.84f / DurationScale));

            if (slashGlowEffect != null)
            {
                sequence.Insert(0f, DOTween.To(() => glowWidth, value =>
                {
                    glowWidth = value;
                    slashGlowEffect.widthMultiplier = value;
                }, 0.08f, slashRevealDuration / DurationScale).SetEase(Ease.InExpo));
                sequence.Insert(0f, DOTween.To(() => glowAlpha, value =>
                {
                    glowAlpha = value;
                    Color glow = new Color(0.78f, 0.22f, 1f, value);
                    slashGlowEffect.startColor = glow;
                    slashGlowEffect.endColor = glow;
                }, 0f, slashRevealDuration / DurationScale).SetEase(Ease.InQuad));
            }

            yield return sequence.WaitForCompletion();
            slashEffect.enabled = false;
            if (slashGlowEffect != null) slashGlowEffect.enabled = false;
        }

        private void MoveBossToCue(int cue)
        {
            Vector3 center = Boss.ArenaCenter;
            Vector3 position = cue switch
            {
                0 => center + Vector3.right * (Boss.ArenaHalfWidth - 1.2f),
                1 => center + Vector3.left * (Boss.ArenaHalfWidth - 1.2f),
                2 => center + Vector3.down * (Boss.ArenaHalfHeight - 1.2f),
                _ => center + Vector3.up * (Boss.ArenaHalfHeight - 1.2f)
            };
            position.z = Boss.transform.position.z;
            Boss.transform.position = position;
        }

        private Vector3 GetScreenCenter()
        {
            Vector3 center = Boss.ArenaCenter;
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return center;
            float depth = Mathf.Abs(mainCamera.transform.position.z - Boss.transform.position.z);
            center = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
            center.z = Boss.transform.position.z;
            return center;
        }

        private void HideArrows()
        {
            if (directionArrows == null) return;
            foreach (SpriteRenderer arrow in directionArrows)
            {
                if (arrow == null) continue;
                DOTween.Kill(arrow);
                arrow.transform.DOKill();
                arrow.enabled = false;
            }
        }

        private void CleanupVisuals()
        {
            HideArrows();
            if (eyeSparkle != null)
            {
                DOTween.Kill(eyeSparkle);
                eyeSparkle.enabled = false;
            }
            if (slashEffect != null)
            {
                DOTween.Kill(slashEffect);
                slashEffect.enabled = false;
            }
            if (slashGlowEffect != null)
            {
                DOTween.Kill(slashGlowEffect);
                slashGlowEffect.enabled = false;
            }
        }

        protected override void OnLostSoulCompleted() => CleanupVisuals();

        protected override void OnLostSoulCancelled()
        {
            CleanupVisuals();
            Boss?.SetDarkness(false, 0.05f);
        }
    }
}
