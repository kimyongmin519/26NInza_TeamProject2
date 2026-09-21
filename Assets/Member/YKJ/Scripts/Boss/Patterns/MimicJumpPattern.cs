using System;
using UnityEngine;

namespace Member.YKJ.Bosses
{
    [Serializable]
    public sealed class MimicJumpPattern : MimicPattern
    {
        [SerializeField, Min(1)] private int jumpCount = 10;
        [SerializeField, Min(0f)] private float warningTime = 0.35f;
        [SerializeField, Min(0.01f)] private float flightTime = 0.75f;
        [SerializeField, Min(0f)] private float jumpHeight = 5f;
        [SerializeField, Min(0f)] private float landingDelay = 0.3f;
        [SerializeField, Min(0.01f)] private float damageRadius = 2f;
        [SerializeField, Min(0f)] private float landingDamage = 20f;
        [SerializeField] private LayerMask playerLayers = 1 << 6;
        [SerializeField, Min(1)] private int rocksPerLanding = 3;
        [SerializeField, Min(0f)] private float rockDamage = 10f;
        [SerializeField, Min(0.01f)] private float rockGravity = 1.5f;
        [SerializeField, Min(0.1f)] private float rockLifetime = 8f;
        [SerializeField] private LineRenderer landingWarning;

        private enum Step { Warning, Flight, Landed }
        private Step _step;
        private Vector3 _start;
        private Vector3 _landing;
        private float _elapsed;
        private int _zone;
        private int _landedCount;

        public int LandedCount => _landedCount;
        public override bool CanStart() => Boss != null && Boss.Arena != null && Boss.Arena.IsConfigured &&
            Boss.RockPrefab != null && landingWarning != null;

        public override void OnStart()
        {
            _zone = -1;
            _landedCount = 0;
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
            _elapsed = 0f;
            _step = Step.Warning;
            landingWarning.useWorldSpace = true;
            landingWarning.positionCount = 33;
            for (int i = 0; i <= 32; i++)
            {
                float angle = i * Mathf.PI * 2f / 32f;
                landingWarning.SetPosition(i, _landing + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * damageRadius);
            }
            landingWarning.enabled = true;
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
            MimicCombat.DamageCircle(_landing, damageRadius, playerLayers, landingDamage, Boss.transform);
            if (Boss.Patterns.Current != this)
                return;
            for (int i = 0; i < rocksPerLanding; i++)
                Boss.SpawnHazard(Boss.RockPrefab, Boss.Arena.RandomRockPosition(_zone), Vector2.zero,
                    rockGravity, rockDamage, rockLifetime);

            if (_landedCount == Mathf.CeilToInt(jumpCount * 0.5f) && _landedCount < jumpCount)
                InterruptWith(Boss.Tongue);
        }

        public override void OnEnd()
        {
            if (landingWarning != null)
                landingWarning.enabled = false;
            if (_step == Step.Flight && Boss != null)
                Boss.transform.position = _start;
        }
    }
}
