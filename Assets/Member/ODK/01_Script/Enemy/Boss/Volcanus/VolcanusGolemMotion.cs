using DG.Tweening;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class VolcanusGolemMotion : MonoBehaviour
    {
        [Header("Golem Pieces")]
        [SerializeField] private Animator sourceAnimator;
        [SerializeField] private Transform skeletonRoot;
        [SerializeField] private Transform body;
        [SerializeField] private Transform head;
        [SerializeField] private Transform sawArm;
        [SerializeField] private Transform fistArm;

        private Vector3 bodyOriginPosition;
        private Quaternion bodyOriginRotation;
        private Vector3 headOriginPosition;
        private Quaternion headOriginRotation;
        private Vector3 sawOriginPosition;
        private Quaternion sawOriginRotation;
        private Vector3 fistOriginPosition;
        private Quaternion fistOriginRotation;
        private float sawGroundOffset;
        private float fistGroundOffset;
        private Sequence bodyTween;
        private Sequence headTween;
        private Sequence sawTween;
        private Sequence fistTween;
        private bool isBound;

        public Vector3 HeadCenter => TryGetPieceBounds(head, out Bounds bounds)
            ? bounds.center
            : head != null ? head.position : transform.position;

        public void Bind(Animator animator, Transform root)
        {
            sourceAnimator = animator;
            skeletonRoot = root;
            body = Find("body_000");
            head = Find("head_1_000");
            sawArm = Find("arm_l_1_000");
            fistArm = Find("arm_r_1_000");

            Debug.Assert(body != null && head != null, "Volcanus Golem body/head pieces are missing.", this);
            Debug.Assert(sawArm != null && fistArm != null, "Volcanus Golem arm pieces are missing.", this);
            if (body == null || head == null || sawArm == null || fistArm == null) return;

            bodyOriginPosition = body.localPosition;
            bodyOriginRotation = body.localRotation;
            headOriginPosition = head.localPosition;
            headOriginRotation = head.localRotation;
            sawOriginPosition = sawArm.localPosition;
            sawOriginRotation = sawArm.localRotation;
            fistOriginPosition = fistArm.localPosition;
            fistOriginRotation = fistArm.localRotation;
            sawGroundOffset = GetGroundOffset(sawArm);
            fistGroundOffset = GetGroundOffset(fistArm);
            isBound = true;
        }

        public Vector3 GetSawImpactPosition(Vector3 groundPoint)
        {
            groundPoint.y += sawGroundOffset;
            groundPoint.z = sawArm != null ? sawArm.position.z : groundPoint.z;
            return groundPoint;
        }

        public Vector3 GetFistImpactPosition(Vector3 groundPoint)
        {
            groundPoint.y += fistGroundOffset;
            groundPoint.z = fistArm != null ? fistArm.position.z : groundPoint.z;
            return groundPoint;
        }

        public void PlayPunch(Vector3 readyPosition, Vector3 punchPosition, float direction,
            float readyDuration, float punchDuration)
        {
            if (!BeginPieceMotion()) return;
            fistTween?.Kill();
            fistTween = DOTween.Sequence().SetTarget(this);
            fistTween.Append(fistArm.DOMove(readyPosition, readyDuration).SetEase(Ease.OutBack));
            fistTween.Join(fistArm.DORotate(new Vector3(0f, 0f, direction * 45f), readyDuration)
                .SetEase(Ease.OutCubic));
            fistTween.Append(fistArm.DOMove(punchPosition, punchDuration).SetEase(Ease.InQuart));
            ReactPieces(new Vector2(direction, 0.15f), direction * -8f);
        }

        public void PrepareSawSlam(Vector3 readyPosition, float readyDuration)
        {
            if (!BeginPieceMotion()) return;
            sawTween?.Kill();
            sawTween = DOTween.Sequence().SetTarget(this);
            sawTween.Append(sawArm.DOMove(readyPosition, readyDuration).SetEase(Ease.OutBack));
            sawTween.Join(sawArm.DORotate(new Vector3(0f, 0f, -42f), readyDuration)
                .SetEase(Ease.OutCubic));
            ReactPieces(Vector2.down * 0.2f, 3f);
        }

        public void StrikeSaw(Vector3 hitPosition, float slamDuration)
        {
            if (!BeginPieceMotion()) return;
            sawTween?.Kill();
            sawTween = DOTween.Sequence().SetTarget(this);
            sawTween.Append(sawArm.DOMove(hitPosition, slamDuration).SetEase(Ease.InExpo));
            sawTween.Join(sawArm.DORotate(new Vector3(0f, 0f, -54f), slamDuration)
                .SetEase(Ease.InExpo));
            ReactPieces(Vector2.down * 0.35f, 5f);
        }

        public void PlaySideSlash(Vector3 startPosition, Vector3 endPosition, float direction,
            float readyDuration, float slashDuration)
        {
            if (!BeginPieceMotion()) return;
            sawTween?.Kill();
            sawTween = DOTween.Sequence().SetTarget(this);
            sawTween.Append(sawArm.DOMove(startPosition, readyDuration).SetEase(Ease.OutBack));
            sawTween.Join(sawArm.DORotate(new Vector3(0f, 0f, -42f), readyDuration)
                .SetEase(Ease.OutCubic));
            sawTween.Append(sawArm.DOMove(endPosition, slashDuration).SetEase(Ease.InOutQuart));
            sawTween.Join(sawArm.DORotate(new Vector3(0f, 0f, -42f - direction * 720f),
                slashDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
            ReactPieces(new Vector2(-direction * 0.45f, 0.18f), direction * 10f);
        }

        public void PlayFistSlam(Vector3 readyPosition, Vector3 hitPosition,
            float readyDuration, float slamDuration)
        {
            if (!BeginPieceMotion()) return;
            fistTween?.Kill();
            fistTween = DOTween.Sequence().SetTarget(this);
            fistTween.Append(fistArm.DOMove(readyPosition, readyDuration).SetEase(Ease.OutBack));
            fistTween.Join(fistArm.DORotate(Vector3.zero, readyDuration).SetEase(Ease.OutCubic));
            fistTween.Append(fistArm.DOMove(hitPosition, slamDuration).SetEase(Ease.InExpo));
            ReactPieces(Vector2.down * 0.3f, -4f);
        }

        public void PlayLaser(Vector3 aimPosition, float readyDuration, float fireDuration)
        {
            if (!BeginPieceMotion()) return;
            ReactPieces(Vector2.up * 0.2f, 0f);
            headTween?.Kill();
            Vector3 headAim = head.position;
            headAim.x = Mathf.Lerp(headAim.x, aimPosition.x, 0.22f);
            headAim.y += 0.5f;
            headTween = DOTween.Sequence().SetTarget(this);
            headTween.Append(head.DOMove(headAim, readyDuration).SetEase(Ease.InOutSine));
            headTween.Append(head.DOMoveY(headAim.y + 0.18f, fireDuration * 0.5f)
                .SetEase(Ease.InOutSine));
            headTween.Append(head.DOMoveY(headAim.y, fireDuration * 0.5f)
                .SetEase(Ease.InOutSine));
        }

        public void PlayMissileCast(float readyDuration, float fireDuration)
        {
            if (!BeginPieceMotion()) return;
            sawTween?.Kill();
            fistTween?.Kill();
            Vector3 sawReady = sawArm.position + new Vector3(-1.2f, 0.7f, 0f);
            Vector3 fistReady = fistArm.position + new Vector3(1.2f, 0.7f, 0f);
            sawTween = DOTween.Sequence().SetTarget(this);
            sawTween.Append(sawArm.DOMove(sawReady, readyDuration).SetEase(Ease.OutBack));
            sawTween.AppendInterval(fireDuration);
            fistTween = DOTween.Sequence().SetTarget(this);
            fistTween.Append(fistArm.DOMove(fistReady, readyDuration).SetEase(Ease.OutBack));
            fistTween.AppendInterval(fireDuration);
            ReactPieces(Vector2.up * 0.25f, 0f);
        }

        public void ResetPieces(float duration = 0.25f)
        {
            if (!isBound) return;
            KillTweens();
            float safeDuration = Mathf.Max(0.01f, duration);
            Sequence sequence = DOTween.Sequence().SetTarget(this);
            sequence.Join(body.DOLocalMove(bodyOriginPosition, safeDuration).SetEase(Ease.OutCubic));
            sequence.Join(body.DOLocalRotateQuaternion(bodyOriginRotation, safeDuration).SetEase(Ease.OutCubic));
            sequence.Join(head.DOLocalMove(headOriginPosition, safeDuration).SetEase(Ease.OutCubic));
            sequence.Join(head.DOLocalRotateQuaternion(headOriginRotation, safeDuration).SetEase(Ease.OutCubic));
            sequence.Join(sawArm.DOLocalMove(sawOriginPosition, safeDuration).SetEase(Ease.OutBack));
            sequence.Join(sawArm.DOLocalRotateQuaternion(sawOriginRotation, safeDuration).SetEase(Ease.OutBack));
            sequence.Join(fistArm.DOLocalMove(fistOriginPosition, safeDuration).SetEase(Ease.OutBack));
            sequence.Join(fistArm.DOLocalRotateQuaternion(fistOriginRotation, safeDuration).SetEase(Ease.OutBack));
            sequence.OnComplete(ResumeAnimator);
            bodyTween = sequence;
        }

        private bool BeginPieceMotion()
        {
            if (!isBound) return false;
            if (sourceAnimator != null && sourceAnimator.enabled)
            {
                sourceAnimator.Play(Volcanus.IdleState, 0, 0f);
                sourceAnimator.Update(0f);
                sourceAnimator.enabled = false;
            }
            return true;
        }

        private void ReactPieces(Vector2 direction, float angle)
        {
            bodyTween?.Kill();
            headTween?.Kill();
            bodyTween = DOTween.Sequence().SetTarget(this);
            bodyTween.Append(body.DOLocalMove(bodyOriginPosition - (Vector3)direction * 0.12f, 0.06f)
                .SetEase(Ease.InQuad));
            bodyTween.Append(body.DOLocalMove(bodyOriginPosition + (Vector3)direction * 0.58f, 0.12f)
                .SetEase(Ease.OutExpo));
            bodyTween.Join(body.DOLocalRotateQuaternion(
                bodyOriginRotation * Quaternion.Euler(0f, 0f, angle), 0.12f).SetEase(Ease.OutExpo));
            bodyTween.Append(body.DOLocalMove(bodyOriginPosition + (Vector3)direction * 0.38f, 0.16f)
                .SetEase(Ease.OutBack));
            headTween = DOTween.Sequence().SetTarget(this);
            headTween.AppendInterval(0.035f);
            headTween.Append(head.DOLocalMove(headOriginPosition + (Vector3)direction * 0.82f, 0.14f)
                .SetEase(Ease.OutExpo));
            headTween.Join(head.DOLocalRotateQuaternion(
                headOriginRotation * Quaternion.Euler(0f, 0f, -angle * 1.4f), 0.14f)
                .SetEase(Ease.OutExpo));
            headTween.Append(head.DOLocalMove(headOriginPosition + (Vector3)direction * 0.52f, 0.18f)
                .SetEase(Ease.OutBack));
        }

        private void ResumeAnimator()
        {
            if (sourceAnimator == null) return;
            sourceAnimator.enabled = true;
            sourceAnimator.Play(Volcanus.IdleState, 0, 0f);
            sourceAnimator.Update(0f);
        }

        private static float GetGroundOffset(Transform piece)
        {
            return TryGetPieceBounds(piece, out Bounds bounds)
                ? Mathf.Max(0f, piece.position.y - bounds.min.y)
                : 0f;
        }

        private static bool TryGetPieceBounds(Transform piece, out Bounds bounds)
        {
            bounds = default;
            if (piece == null) return false;
            bool found = false;
            foreach (SpriteRenderer pieceRenderer in piece.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!pieceRenderer.enabled || !pieceRenderer.gameObject.activeInHierarchy ||
                    pieceRenderer.sprite == null) continue;
                if (!found)
                {
                    bounds = pieceRenderer.bounds;
                    found = true;
                }
                else bounds.Encapsulate(pieceRenderer.bounds);
            }
            return found;
        }

        private Transform Find(string targetName)
        {
            if (skeletonRoot == null) return null;
            foreach (Transform child in skeletonRoot.GetComponentsInChildren<Transform>(true))
                if (child.name == targetName) return child;
            return null;
        }

        private void KillTweens()
        {
            bodyTween?.Kill();
            headTween?.Kill();
            sawTween?.Kill();
            fistTween?.Kill();
            body?.DOKill();
            head?.DOKill();
            sawArm?.DOKill();
            fistArm?.DOKill();
        }

        private void OnDisable()
        {
            KillTweens();
            if (sourceAnimator != null) sourceAnimator.enabled = true;
        }

        private void OnDestroy() => KillTweens();
    }
}
