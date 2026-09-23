using DG.Tweening;
using UnityEngine;

namespace Member.YKJ.Bosses
{
    // Animate only the child renderer; the boss root, collider and attack anchors stay unchanged.
    public sealed class MimicBodyAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer chestRenderer;
        [Header("Jump Scale Multipliers")]
        [SerializeField] private Vector2 jumpAnticipation = new Vector2(1.2f, 0.72f);
        [SerializeField] private Vector2 jumpStretch = new Vector2(0.8f, 1.25f);
        [SerializeField] private Vector2 landingSquash = new Vector2(1.3f, 0.65f);
        [Header("Treasure Scale Multipliers")]
        [SerializeField] private Vector2 treasureAnticipation = new Vector2(1.08f, 1.15f);
        [SerializeField] private Vector2 spitRecoil = new Vector2(1.18f, 0.82f);
        [SerializeField, Min(0.01f)] private float spitRecoilTime = 0.18f;
        [Header("Tongue Scale Multipliers")]
        [SerializeField] private Vector2 tongueAnticipation = new Vector2(1.12f, 0.8f);
        [SerializeField] private Vector2 tongueExtension = new Vector2(0.88f, 1.12f);
        [SerializeField] private Vector2 tongueRecovery = new Vector2(1.12f, 0.9f);

        private Transform _visual;
        private Vector3 _restScale;
        private Vector3 _restPosition;
        private float _restBottom;
        private Sequence _pose;

        private void Awake() => CaptureRestPose();

        private bool CaptureRestPose()
        {
            if (_visual != null)
                return true;
            if (chestRenderer == null || chestRenderer.transform == transform ||
                !chestRenderer.transform.IsChildOf(transform))
                return false;
            _visual = chestRenderer.transform;
            _restScale = _visual.localScale;
            _restPosition = _visual.localPosition;
            _restBottom = _restPosition.y +
                (chestRenderer.sprite != null ? chestRenderer.sprite.bounds.min.y * _restScale.y : 0f);
            return true;
        }

        private bool BeginPose()
        {
            if (!isActiveAndEnabled || !CaptureRestPose())
                return false;
            _pose?.Kill();
            _pose = DOTween.Sequence();
            return true;
        }

        private Tween ScaleTo(Vector2 multiplier, float duration, Ease ease)
        {
            Vector3 scale = new Vector3(_restScale.x * Mathf.Max(0.1f, multiplier.x),
                _restScale.y * Mathf.Max(0.1f, multiplier.y), _restScale.z);
            return _visual.DOScale(scale, Mathf.Max(0.001f, duration)).SetEase(ease).OnUpdate(AlignFeet);
        }

        private void AlignFeet()
        {
            if (_visual == null || chestRenderer == null || chestRenderer.sprite == null)
                return;
            Vector3 position = _restPosition;
            position.y = _restBottom - chestRenderer.sprite.bounds.min.y * _visual.localScale.y;
            _visual.localPosition = position;
        }

        public void PrepareJump(float duration)
        {
            if (BeginPose())
                _pose.Append(ScaleTo(jumpAnticipation, duration, Ease.InQuad));
        }

        public void Jump(float duration)
        {
            if (!BeginPose()) return;
            _pose.Append(ScaleTo(jumpStretch, duration * 0.18f, Ease.OutQuad))
                .Append(ScaleTo(Vector2.one, duration * 0.45f, Ease.InOutSine))
                .Append(ScaleTo(jumpStretch, duration * 0.37f, Ease.InQuad));
        }

        public void Land(float duration)
        {
            if (!BeginPose()) return;
            _pose.Append(ScaleTo(landingSquash, duration * 0.2f, Ease.OutQuad))
                .Append(ScaleTo(Vector2.one, duration * 0.8f, Ease.OutBack));
        }

        public void PrepareTreasure(float duration)
        {
            if (BeginPose())
                _pose.Append(ScaleTo(treasureAnticipation, duration, Ease.InOutSine));
        }

        public void Spit(float interval)
        {
            if (!BeginPose()) return;
            float duration = SpitDuration(interval);
            _pose.Append(ScaleTo(spitRecoil, duration * 0.3f, Ease.OutQuad))
                .Append(ScaleTo(Vector2.one, duration * 0.7f, Ease.OutBack));
        }

        public float SpitDuration(float interval) => Mathf.Min(spitRecoilTime, Mathf.Max(0.01f, interval));

        public void PrepareTongue(float duration)
        {
            if (BeginPose())
                _pose.Append(ScaleTo(tongueAnticipation, duration, Ease.InQuad));
        }

        public void ExtendTongue(float duration)
        {
            if (BeginPose())
                _pose.Append(ScaleTo(tongueExtension, duration, Ease.OutQuad));
        }

        public void RetractTongue(float duration)
        {
            if (!BeginPose()) return;
            _pose.Append(ScaleTo(tongueRecovery, duration * 0.35f, Ease.OutQuad))
                .Append(ScaleTo(Vector2.one, duration * 0.65f, Ease.OutBack));
        }

        public void ResetPose()
        {
            _pose?.Kill();
            _pose = null;
            if (_visual == null)
                return;
            _visual.localScale = _restScale;
            _visual.localPosition = _restPosition;
        }

        private void OnDisable() => ResetPose();
        private void OnDestroy() => ResetPose();
    }
}
