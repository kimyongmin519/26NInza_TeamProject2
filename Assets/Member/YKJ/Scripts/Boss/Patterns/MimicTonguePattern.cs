using System;
using System.Collections.Generic;
using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK._01_Script;
using Member.ODK.Scripts;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Member.YKJ.Bosses
{
    [Serializable]
    public sealed class MimicTonguePattern : MimicPattern
    {
        [SerializeField] private LineRenderer tongueLine;
        [SerializeField, Min(0f)] private float warningTime = 0.5f;
        [SerializeField, Min(0.01f)] private float extendTime = 0.2f;
        [SerializeField, Min(0.01f)] private float retractTime = 0.35f;
        [SerializeField, Min(0.1f)] private float reach = 14f;
        [SerializeField, Min(0.01f)] private float width = 0.4f;
        [SerializeField, Min(0f)] private float damage = 15f;
        [SerializeField] private LayerMask playerLayers = 1 << 6;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color attackColor = new Color(1f, 0.2f, 0.35f);

        private enum Step { Warning, Extending, Retracting }
        private readonly HashSet<IDamageable> _damaged = new HashSet<IDamageable>();
        private readonly List<IGrabbable> _captured = new List<IGrabbable>();
        private Transform _capturePoint;
        private Vector3 _origin;
        private Vector3 _end;
        private Step _step;
        private float _elapsed;

        public override bool CanStart() => Boss != null && Boss.Target != null && tongueLine != null;

        public override void OnStart()
        {
            _step = Step.Warning;
            _elapsed = 0f;
            _damaged.Clear();
            _captured.Clear();
            _origin = Boss.MouthPosition;
            Vector2 direction = Boss.Target.position - _origin;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.left;
            _end = _origin + (Vector3)(direction.normalized * reach);
            _capturePoint = new GameObject("Mimic Tongue Capture").transform;
            _capturePoint.position = _origin;
            tongueLine.useWorldSpace = true;
            tongueLine.positionCount = 2;
            tongueLine.enabled = true;
            DrawLine(_end, true);
        }

        public override void OnUpdate(float deltaTime)
        {
            _elapsed += deltaTime;
            switch (_step)
            {
                case Step.Warning:
                    if (_elapsed < warningTime)
                        return;
                    _elapsed = 0f;
                    _step = Step.Extending;
                    DrawLine(_origin, false);
                    break;
                case Step.Extending:
                    float outward = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, extendTime));
                    _capturePoint.position = Vector3.Lerp(_origin, _end, outward);
                    DrawLine(_capturePoint.position, false);
                    AffectSegment(_capturePoint.position);
                    if (Boss.Patterns.Current != this)
                        return;
                    if (outward >= 1f)
                    {
                        _elapsed = 0f;
                        _step = Step.Retracting;
                    }
                    break;
                case Step.Retracting:
                    float inward = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, retractTime));
                    _capturePoint.position = Vector3.Lerp(_end, _origin, inward);
                    DrawLine(_capturePoint.position, false);
                    if (inward >= 1f)
                    {
                        ReleaseCaptured(true);
                        EndPattern();
                    }
                    break;
            }
        }

        private void AffectSegment(Vector3 tip)
        {
            Vector2 delta = tip - _origin;
            if (delta.sqrMagnitude < 0.0001f)
                return;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Collider2D[] hits = Physics2D.OverlapBoxAll((_origin + tip) * 0.5f,
                new Vector2(delta.magnitude, width), angle, playerLayers.value | GrabbableLayer.Mask);
            foreach (Collider2D hit in hits)
            {
                if (Boss.Patterns.Current != this)
                    return;
                if (hit.transform.IsChildOf(Boss.transform))
                    continue;

                IGrabbable grabbable = hit.GetComponentInParent<IGrabbable>();
                if (grabbable != null)
                {
                    if (grabbable.CanBeGrabbed && !_captured.Contains(grabbable))
                    {
                        grabbable.Grab(_capturePoint, Boss.gameObject);
                        _captured.Add(grabbable);
                    }
                    continue;
                }

                if ((playerLayers.value & (1 << hit.gameObject.layer)) == 0)
                    continue;
                IDamageable receiver = hit.GetComponentInParent<IDamageable>();
                if (receiver != null && _damaged.Add(receiver))
                    receiver.TakeDamage(new DamageData(damage, DamageType.Melee));
            }
        }

        private void DrawLine(Vector3 end, bool warning)
        {
            tongueLine.SetPosition(0, _origin);
            tongueLine.SetPosition(1, end);
            tongueLine.startColor = tongueLine.endColor = warning ? warningColor : attackColor;
            tongueLine.startWidth = tongueLine.endWidth = warning ? width * 0.2f : width;
        }

        private void ReleaseCaptured(bool consume)
        {
            foreach (IGrabbable item in _captured)
            {
                if (item is not Component component || component == null || item.GrabTransform == null)
                    continue;
                GameObject capturedObject = item.GrabTransform.gameObject;
                item.Release();
                if (consume)
                {
                    capturedObject.SetActive(false);
                    Object.Destroy(capturedObject);
                }
            }
            _captured.Clear();
        }

        public override void OnEnd()
        {
            ReleaseCaptured(false);
            if (_capturePoint != null)
                Object.Destroy(_capturePoint.gameObject);
            _capturePoint = null;
            if (tongueLine != null)
                tongueLine.enabled = false;
        }
    }
}
