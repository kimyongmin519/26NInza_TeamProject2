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
        private readonly MimicWeapon[] _wave = new MimicWeapon[4];
        private int _emitted;
        private bool _reverse;
        private float _timer;

        public int EmittedCount => _emitted;
        public static int PlatformIndex(int shot, bool reverse) => reverse ? 3 - shot : shot;
        public override bool CanStart() => Boss != null && Boss.HasWeaponPrefabs && Physics2D.gravity.y < 0f &&
            (flightCamera != null || Camera.main != null) && platforms != null && platforms.Length == 4 &&
            System.Array.TrueForAll(platforms, item => item != null && item.enabled && !item.isTrigger && item.gameObject.activeInHierarchy);

        public override void OnStart()
        {
            _reverse = Random.value < 0.5f;
            _emitted = 0;
            _timer = 0f;
            System.Array.Clear(_wave, 0, _wave.Length);
        }

        public override void OnUpdate(float deltaTime)
        {
            if (_emitted == 4)
            {
                // The pattern ends after its last airborne weapon lands, is caught, or hits.
                foreach (MimicWeapon item in _wave)
                    if (item != null && item.gameObject.activeInHierarchy && item.State == MimicWeapon.WeaponState.BossFlight)
                        return;
                EndPattern();
                return;
            }
            _timer -= deltaTime;
            if (_timer > 0f) return;
            Camera view = flightCamera != null ? flightCamera : Camera.main;
            int index = PlatformIndex(_emitted, _reverse);
            Collider2D platform = platforms[index];
            if (view == null || platform == null || !platform.enabled)
            {
                EndPattern();
                return;
            }
            bool left = index < 2;
            Ray ray = view.ViewportPointToRay(new Vector3(left ? 0f : 1f, 0.5f, 0f));
            Plane plane = new Plane(Vector3.forward, Boss.transform.position);
            if (!plane.Raycast(ray, out float distance))
            {
                EndPattern();
                return;
            }
            Vector3 origin = ray.GetPoint(distance);
            origin.x = left ? Mathf.Min(origin.x, platform.bounds.min.x) - spawnOutsideScreen :
                Mathf.Max(origin.x, platform.bounds.max.x) + spawnOutsideScreen;
            origin.y = platform.bounds.max.y + spawnHeight;
            _wave[_emitted] = Boss.EmitFallingWeapon(origin, platform, arcHeight);
            _emitted++;
            _timer = Mathf.Max(0.01f, interval);
        }
    }
}
