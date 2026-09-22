using UnityEngine;
using KimLIb.EventSystem;
using YKJ_Script.Feedbacks;

namespace Member.YKJ.Bosses
{
    public sealed class MimicJumpPattern : MimicPattern
    {
        protected override int DefaultSkillId => 2;
        [SerializeField, Min(1)] private int jumpCount = 10;
        [SerializeField, Min(0f)] private float warningTime = 0.35f;
        [SerializeField, Min(0.01f)] private float flightTime = 0.75f;
        [SerializeField, Min(0f)] private float jumpHeight = 5f;
        [SerializeField, Min(0f)] private float landingDelay = 0.3f;
        [SerializeField, Min(0f)] private float landingDamage = 20f;
        [SerializeField] private LayerMask playerLayers = 1 << 6;
        [SerializeField] private LineRenderer landingWarning;
        [SerializeField] private Color warningColor = new Color(1f, 0.35f, 0.35f, 0.25f);
        [Header("Landing Feedback")]
        [SerializeField] private EventChannelSO feedbackChannel;
        [SerializeField] private FeedbackSO landingFeedback;
        [Header("Ground Contact")]
        [SerializeField] private Collider2D groundSurface;
        [SerializeField] private Collider2D bossCollider;

        private enum Step { Warning, Flight, Landed }
        private Step _step;
        private Vector3 _start;
        private Vector3 _landing;
        private Rect _warningBounds;
        private float _elapsed;
        private int _zone;
        private int _landedCount;
        private bool _pendingTongue;

        public int LandedCount => _landedCount;
        public override bool CanStart() => Boss != null && Boss.Arena != null && Boss.Arena.IsConfigured &&
            landingWarning != null;

        public override void OnStart()
        {
            _zone = -1;
            _landedCount = 0;
            _pendingTongue = false;
            PrepareJump();
        }

        private void PrepareJump()
        {
            // Pick a different zone so every jump moves, including repeated cycles.
            int next = UnityEngine.Random.Range(0, Boss.Arena.ZoneCount - (_zone >= 0 ? 1 : 0));
            if (_zone >= 0 && next >= _zone) next++;
            _zone = next;
            _start = Boss.transform.position;
            _landing = Boss.Arena.LandingPosition(_zone);
            _landing = GroundedLandingPosition(_landing);
            _warningBounds = Boss.Arena.ZoneBounds(_zone);
            _elapsed = 0f;
            _step = Step.Warning;
            Boss.BodyAnimator?.PrepareJump(warningTime);
            landingWarning.useWorldSpace = true;
            // A square-ended vertical strip fills the exact zone without a separate mesh/material.
            landingWarning.loop = false;
            landingWarning.alignment = LineAlignment.View;
            landingWarning.numCapVertices = 0;
            landingWarning.numCornerVertices = 0;
            landingWarning.widthMultiplier = 1f;
            landingWarning.widthCurve = AnimationCurve.Constant(0f, 1f, _warningBounds.width);
            landingWarning.startColor = landingWarning.endColor = warningColor;
            landingWarning.positionCount = 2;
            landingWarning.SetPosition(0, new Vector3(_warningBounds.center.x, _warningBounds.yMin, _landing.z));
            landingWarning.SetPosition(1, new Vector3(_warningBounds.center.x, _warningBounds.yMax, _landing.z));
            landingWarning.enabled = true;
        }

        private Vector3 GroundedLandingPosition(Vector3 position)
        {
            if (groundSurface == null || bossCollider == null)
                return position;
            Bounds ground = groundSurface.bounds;
            Vector2 origin = new Vector2(position.x, ground.max.y + 1f);
            foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, Vector2.down, ground.size.y + 2f,
                         1 << groundSurface.gameObject.layer))
            {
                if (hit.collider != groundSurface)
                    continue;
                float bottomOffset = Boss.transform.position.y - bossCollider.bounds.min.y;
                position.y = hit.point.y + bottomOffset;
                return position;
            }
            Debug.LogWarning("Mimic landing point is outside the assigned ground surface.", this);
            return position;
        }

        public override void OnUpdate(float deltaTime)
        {
            _elapsed += deltaTime;
            switch (_step)
            {
                case Step.Warning:
                    if (_elapsed >= warningTime)
                    {
                        _elapsed = 0f;
                        _step = Step.Flight;
                        Boss.BodyAnimator?.Jump(flightTime);
                    }
                    break;
                case Step.Flight:
                    float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, flightTime));
                    Boss.transform.position = Vector3.Lerp(_start, _landing, t) + Vector3.up * (4f * jumpHeight * t * (1f - t));
                    if (t >= 1f)
                        Land();
                    break;
                case Step.Landed:
                    if (_elapsed < landingDelay)
                        return;
                    if (_pendingTongue)
                    {
                        _pendingTongue = false;
                        InterruptWith(Boss.Tongue);
                        return;
                    }
                    if (_landedCount >= jumpCount)
                        EndPattern();
                    else
                        PrepareJump();
                    break;
            }
        }

        private void Land()
        {
            _landedCount++;
            _step = Step.Landed;
            _elapsed = 0f;
            landingWarning.enabled = false;
            Boss.BodyAnimator?.Land(landingDelay);
            if (feedbackChannel != null && landingFeedback != null)
                feedbackChannel.RaiseEvent(new PlayFeedBack().Init(landingFeedback.FeedBackId));
            if (Boss.Patterns.Current != this)
                return;
            MimicCombat.DamageBox(_warningBounds, playerLayers, landingDamage, Boss.transform);
            if (Boss.Patterns.Current != this)
                return;
            if (_landedCount == Mathf.CeilToInt(jumpCount * 0.5f) && _landedCount < jumpCount)
                _pendingTongue = true;
        }

        public override void OnPause() => Boss.BodyAnimator?.ResetPose();

        public override void OnEnd()
        {
            Boss.BodyAnimator?.ResetPose();
            if (landingWarning != null)
                landingWarning.enabled = false;
            if (_step == Step.Flight && Boss != null)
                Boss.transform.position = _start;
        }
    }
}
