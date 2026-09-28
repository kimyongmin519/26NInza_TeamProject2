using UnityEngine;

namespace Member.YKJ.Bosses
{
    public sealed class MimicFallingWeaponsPattern : MimicPattern
    {
        protected override int DefaultSkillId => 5;
        [Tooltip("1: lower left, 2: upper left, 3: upper right, 4: lower right.")]
        [SerializeField] private Collider2D[] platforms = new Collider2D[4];
        [SerializeField] private Camera flightCamera;
        [SerializeField, Min(0.01f)] private float interval = 0.8f;
        [SerializeField, Min(0.1f)] private float spawnOutsideScreen = 1f;
        [SerializeField, Min(0.1f)] private float spawnHeight = 1f;
        [SerializeField, Min(0.05f)] private float arcHeight = 0.6f;
        [Header("Concurrent Jump")]
        [SerializeField] private MimicJumpPattern jumpPattern;
        [Header("Incoming Weapon Warning")]
        [SerializeField] private Material warningMaterial;
        [SerializeField, Min(0.1f)] private float warningTime = 0.65f;
        [SerializeField] private Color warningColor = new Color(1f, 0.02f, 0.02f, 1f);
        [SerializeField, Range(0.02f, 0.15f)] private float warningScreenSize = 0.065f;
        [SerializeField, Range(0.03f, 0.2f)] private float warningScreenInset = 0.06f;
        private enum Step { Warning, Interval, WaitingForLanding }
        private Step _step;
        private LineRenderer _warning;
        private readonly MimicWeapon[] _wave = new MimicWeapon[4];
        private int _emitted;
        private bool _reverse;
        private float _timer;

        public int EmittedCount => _emitted;
        public bool IsWarning => _step == Step.Warning && _warning != null && _warning.enabled;
        public static int PlatformIndex(int shot, bool reverse) => reverse ? 3 - shot : shot;
        public override bool CanStart() => Boss != null && Boss.HasWeaponPrefabs && warningMaterial != null && Physics2D.gravity.y < 0f &&
            (jumpPattern == null || (jumpPattern.isActiveAndEnabled && jumpPattern.GetComponentInParent<MimicBoss>() == Boss && jumpPattern.CanStart())) &&
            (flightCamera != null || Camera.main != null) && platforms != null && platforms.Length == 4 &&
            System.Array.TrueForAll(platforms, item => item != null && item.enabled && !item.isTrigger && item.gameObject.activeInHierarchy);

        public override void OnStart()
        {
            _reverse = Random.value < 0.5f;
            _emitted = 0;
            System.Array.Clear(_wave, 0, _wave.Length);
            if (_warning == null)
            {
                var visual = new GameObject("IncomingWeaponWarning");
                visual.transform.SetParent(transform, false);
                _warning = visual.AddComponent<LineRenderer>();
                _warning.useWorldSpace = true;
                _warning.positionCount = 3;
                _warning.alignment = LineAlignment.View;
                _warning.numCornerVertices = 2;
                _warning.numCapVertices = 2;
                _warning.sortingLayerName = "Weapon";
                _warning.sortingOrder = 50;
            }
            _warning.sharedMaterial = warningMaterial;
            BeginWarning();
            if (jumpPattern != null) jumpPattern.StartAlongside(this);
        }

        public override void OnUpdate(float deltaTime)
        {
            bool waveFinished = _step == Step.WaitingForLanding && !System.Array.Exists(_wave,
                item => item != null && item.gameObject.activeInHierarchy && item.State == MimicWeapon.WeaponState.BossFlight);
            if (waveFinished && jumpPattern != null) jumpPattern.RequestStopAlongside(this);
            if (jumpPattern != null && jumpPattern.IsRunningAlongside(this))
            {
                jumpPattern.OnUpdate(deltaTime);
                // Landing damage/feedback can synchronously cancel the entire encounter.
                if (Boss.Patterns.Current != this) return;
            }
            if (_step == Step.WaitingForLanding)
            {
                // The pattern ends after its last airborne weapon lands, is caught, or hits.
                if (!waveFinished) return;
                if (jumpPattern != null && jumpPattern.IsRunningAlongside(this))
                    return;
                EndPattern();
                return;
            }
            if (_step == Step.Interval)
            {
                _timer -= deltaTime;
                if (_timer <= 0f) BeginWarning();
                return;
            }
            if (!TryGetShot(out Camera view, out Collider2D platform, out Vector3 origin))
            {
                EndPattern();
                return;
            }
            _timer += deltaTime;
            DrawWarning(view, origin);
            if (_timer < Mathf.Max(0.1f, warningTime)) return;

            _warning.enabled = false;
            _wave[_emitted] = Boss.EmitFallingWeapon(origin, platform, arcHeight);
            _emitted++;
            _step = _emitted == _wave.Length ? Step.WaitingForLanding : Step.Interval;
            // Keep the original shot cadence when the interval accommodates the full warning.
            _timer = Mathf.Max(0f, interval - Mathf.Max(0.1f, warningTime));
        }

        private void BeginWarning()
        {
            _step = Step.Warning;
            _timer = 0f;
            _warning.enabled = false;
            if (TryGetShot(out Camera view, out _, out Vector3 origin))
                DrawWarning(view, origin);
        }

        private bool TryGetShot(out Camera view, out Collider2D platform, out Vector3 origin)
        {
            view = flightCamera != null ? flightCamera : Camera.main;
            int index = PlatformIndex(_emitted, _reverse);
            platform = platforms[index];
            origin = Vector3.zero;
            if (view == null || platform == null || !platform.enabled || !platform.gameObject.activeInHierarchy)
                return false;
            bool left = index < 2;
            Ray ray = view.ViewportPointToRay(new Vector3(left ? 0f : 1f, 0.5f, 0f));
            Plane plane = new Plane(Vector3.forward, Boss.transform.position);
            if (!plane.Raycast(ray, out float distance))
                return false;
            origin = ray.GetPoint(distance);
            origin.x = left ? Mathf.Min(origin.x, platform.bounds.min.x) - spawnOutsideScreen :
                Mathf.Max(origin.x, platform.bounds.max.x) + spawnOutsideScreen;
            origin.y = platform.bounds.max.y + spawnHeight;
            return true;
        }

        private void DrawWarning(Camera view, Vector3 origin)
        {
            bool left = PlatformIndex(_emitted, _reverse) < 2;
            Vector3 viewport = view.WorldToViewportPoint(origin);
            float size = Mathf.Clamp(warningScreenSize, 0.02f, 0.15f);
            float halfWidth = size * 0.5f / Mathf.Max(0.1f, view.aspect);
            float inset = Mathf.Clamp(Mathf.Max(warningScreenInset, halfWidth * 1.5f), 0.03f, 0.4f);
            viewport.x = left ? inset : 1f - inset;
            viewport.y = Mathf.Clamp(viewport.y, size, 1f - size);
            Plane plane = new Plane(Vector3.forward, Boss.transform.position);
            if (!PointOnPlane(view, plane, viewport, out Vector3 center) ||
                !PointOnPlane(view, plane, viewport + Vector3.up * (size * 0.5f), out Vector3 top) ||
                !PointOnPlane(view, plane, viewport + Vector3.right * halfWidth, out Vector3 right))
            {
                _warning.enabled = false;
                return;
            }
            Vector3 up = top - center;
            Vector3 forward = (right - center) * (left ? 1f : -1f);
            _warning.SetPosition(0, center - forward + up);
            _warning.SetPosition(1, center + forward);
            _warning.SetPosition(2, center - forward - up);
            _warning.startWidth = _warning.endWidth = up.magnitude * 0.3f;
            Color color = warningColor;
            color.a *= Mathf.Lerp(0.35f, 1f, 0.5f + 0.5f * Mathf.Cos(_timer * Mathf.PI * 8f));
            _warning.startColor = _warning.endColor = color;
            _warning.enabled = true;
        }

        private static bool PointOnPlane(Camera view, Plane plane, Vector3 viewport, out Vector3 point)
        {
            Ray ray = view.ViewportPointToRay(viewport);
            point = Vector3.zero;
            if (!plane.Raycast(ray, out float distance)) return false;
            point = ray.GetPoint(distance);
            return true;
        }

        public override void OnEnd()
        {
            if (jumpPattern != null) jumpPattern.StopAlongside(this);
            if (_warning != null) _warning.enabled = false;
        }
        public override void OnDie() => OnEnd();
        private void OnDisable() => OnEnd();
    }
}
