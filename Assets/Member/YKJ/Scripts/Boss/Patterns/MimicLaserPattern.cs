using System.Collections.Generic;
using Member.ODK._01_Script;
using Member.ODK.Scripts;
using UnityEngine;

namespace Member.YKJ.Bosses
{
    public sealed class MimicLaserPattern : MimicPattern
    {
        protected override int DefaultSkillId => 6;
        [SerializeField] private Material laserMaterial;
        [SerializeField, Min(1)] private int shotCount = 5;
        [SerializeField] private Vector2 angleRange = new Vector2(-30f, 210f);
        [SerializeField, Min(0f)] private float warningTime = 0.6f;
        [SerializeField, Min(0.01f)] private float shotInterval = 0.12f;
        [SerializeField, Min(0f)] private float rotationWarningTime = 0.6f;
        [SerializeField] private Color rotationWarningColor = new Color(1f, 0.85f, 0.1f, 1f);
        [SerializeField, Min(0.01f)] private float rotationDuration = 3f;
        [SerializeField] private bool clockwise = true;
        [SerializeField, Min(0.1f)] private float length = 40f;
        [SerializeField, Min(0.01f)] private float width = 0.65f;
        [SerializeField, Min(0f)] private float damage = 15f;
        [SerializeField] private LayerMask playerLayers = 1 << 6;
        [SerializeField] private Color warningColor = new Color(1f, 0.25f, 0.25f, 0.35f);
        [SerializeField] private Color fireColor = new Color(1f, 0.05f, 0.05f, 1f);
        private sealed class Beam
        {
            public LineRenderer Line;
            public float Angle;
            public readonly HashSet<IDamageable> Damaged = new HashSet<IDamageable>();
        }
        private enum Step { Warning, Building, RotationWarning, Rotating }
        private Step _step;
        private float _elapsed;
        private int _count;
        private readonly List<Beam> _beams = new List<Beam>();
        public int FiredCount { get; private set; }
        public float RotationDegrees { get; private set; }
        public override bool CanStart() => Boss != null && laserMaterial != null;

        public override void OnStart()
        {
            _count = Mathf.Max(1, shotCount);
            while (_beams.Count < _count)
            {
                var visual = new GameObject("LaserBeam" + (_beams.Count + 1));
                visual.transform.SetParent(transform, false);
                var line = visual.AddComponent<LineRenderer>();
                line.sharedMaterial = laserMaterial;
                line.useWorldSpace = true;
                line.textureMode = LineTextureMode.Stretch;
                line.alignment = LineAlignment.View;
                line.positionCount = 2;
                line.sortingLayerName = "Weapon";
                line.sortingOrder = 30;
                _beams.Add(new Beam { Line = line });
            }
            FiredCount = 0;
            RotationDegrees = 0f;
            _elapsed = 0f;
            _step = Step.Warning;
            float startAngle = Random.Range(angleRange.x, angleRange.y);
            for (int i = 0; i < _beams.Count; i++)
            {
                Beam beam = _beams[i];
                beam.Damaged.Clear();
                beam.Line.enabled = i < _count;
                if (i >= _count) continue;
                beam.Angle = startAngle + i * (360f / _count);
                Draw(beam, 0f, true);
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_step == Step.Warning)
            {
                if (_elapsed < warningTime) return;
                _elapsed = 0f;
                _step = Step.Building;
                FireNext();
            }
            else if (_step == Step.Building)
            {
                if (_elapsed >= Mathf.Max(0.01f, shotInterval))
                {
                    _elapsed = 0f;
                    FireNext();
                }
            }
            else if (_step == Step.RotationWarning)
            {
                if (_elapsed >= Mathf.Max(0f, rotationWarningTime))
                {
                    _step = Step.Rotating;
                    _elapsed = 0f;
                }
            }
            else
            {
                float next = (clockwise ? -360f : 360f) * Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, rotationDuration));
                // Sample the swept arc so fast beams cannot skip a player between frames.
                int samples = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(next - RotationDegrees) / 2f));
                for (int sample = 1; sample <= samples; sample++)
                {
                    float angle = Mathf.Lerp(RotationDegrees, next, (float)sample / samples);
                    for (int i = 0; i < FiredCount; i++)
                    {
                        DamageBeam(_beams[i], angle);
                        if (Boss.Patterns.Current != this) return;
                    }
                }
                RotationDegrees = next;
            }

            for (int i = 0; i < FiredCount; i++)
            {
                Draw(_beams[i], RotationDegrees, false);
                if (_step == Step.RotationWarning)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Cos(_elapsed * Mathf.PI * 10f);
                    Color color = Color.Lerp(fireColor, rotationWarningColor, pulse);
                    _beams[i].Line.startColor = _beams[i].Line.endColor = color;
                }
                DamageBeam(_beams[i], RotationDegrees);
                if (Boss.Patterns.Current != this) return;
            }
            if (_step == Step.Rotating && _elapsed >= Mathf.Max(0.01f, rotationDuration))
                EndPattern();
        }

        private void FireNext()
        {
            FiredCount++;
            Boss.BodyAnimator?.Spit(shotInterval);
            if (FiredCount == _count)
            {
                _step = Step.RotationWarning;
                _elapsed = 0f;
            }
        }

        private Vector3 Tip(Beam beam, float rotation) => Boss.MouthPosition +
            new Vector3(Mathf.Cos((beam.Angle + rotation) * Mathf.Deg2Rad),
                Mathf.Sin((beam.Angle + rotation) * Mathf.Deg2Rad), 0f) * length;

        private void Draw(Beam beam, float rotation, bool warning)
        {
            beam.Line.SetPosition(0, Boss.MouthPosition);
            beam.Line.SetPosition(1, Tip(beam, rotation));
            beam.Line.startWidth = beam.Line.endWidth = warning ? width * 0.15f : width;
            beam.Line.startColor = beam.Line.endColor = warning ? warningColor : fireColor;
            beam.Line.enabled = true;
        }

        private void DamageBeam(Beam beam, float rotation)
        {
            Vector3 origin = Boss.MouthPosition;
            Vector3 end = Tip(beam, rotation);
            Vector2 delta = end - origin;
            foreach (Collider2D hit in Physics2D.OverlapBoxAll((origin + end) * 0.5f,
                         new Vector2(delta.magnitude, width), beam.Angle + rotation, playerLayers))
            {
                if (Boss.Patterns.Current != this) return;
                if (hit.transform.IsChildOf(Boss.transform)) continue;
                IDamageable receiver = hit.GetComponentInParent<IDamageable>();
                if (receiver != null && beam.Damaged.Add(receiver))
                    receiver.TakeDamage(new DamageData(damage, DamageType.Projectile));
            }
        }

        public override void OnEnd()
        {
            foreach (Beam beam in _beams)
            {
                if (beam.Line != null) beam.Line.enabled = false;
                beam.Damaged.Clear();
            }
            Boss?.BodyAnimator?.ResetPose();
        }
        public override void OnDie() => OnEnd();
        private void OnDisable() => OnEnd();
    }
}
