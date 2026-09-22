using UnityEngine;

namespace Member.YKJ.Bosses
{
    public sealed class MimicCoinRainPattern : MimicPattern
    {
        protected override int DefaultSkillId => 4;
        [SerializeField] private MimicHazard coinPrefab;
        [SerializeField] private SpriteRenderer chestRenderer;
        [SerializeField] private Sprite openChestSprite;
        [SerializeField, Min(1)] private int coinCount = 40;
        [SerializeField, Min(0.01f)] private float interval = 0.08f;
        [SerializeField, Min(0.01f)] private float anticipation = 0.6f;
        [SerializeField] private Camera flightCamera;
        [SerializeField] private Vector2 heightAboveScreen = new Vector2(2f, 4f);
        [SerializeField, Min(0.01f)] private float gravity = 2f;
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0.1f)] private float lifetime = 10f;
        private Sprite _previousSprite;
        private bool _opened;
        private float _timer;
        private int _emitted;

        public int EmittedCount => _emitted;
        public override bool CanStart() => Boss != null && coinPrefab != null && chestRenderer != null && openChestSprite != null &&
            (flightCamera != null || Camera.main != null) && Physics2D.gravity.y < 0f;

        public override void OnStart()
        {
            _emitted = 0;
            _timer = Mathf.Max(0.01f, anticipation);
            _previousSprite = chestRenderer.sprite;
            _opened = true;
            chestRenderer.sprite = openChestSprite;
            Boss.BodyAnimator?.PrepareTreasure(_timer);
        }

        public override void OnUpdate(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f) return;
            if (_emitted >= Mathf.Max(1, coinCount))
            {
                EndPattern();
                return;
            }

            Camera view = flightCamera != null ? flightCamera : Camera.main;
            Vector3 origin = Boss.MouthPosition;
            if (view == null || Physics2D.gravity.y >= 0f ||
                !ViewportOnPlane(view, new Vector2(0.1f, 1f), origin.z, out Vector3 topLeft) ||
                !ViewportOnPlane(view, new Vector2(0.9f, 1f), origin.z, out Vector3 topRight) ||
                !ViewportOnPlane(view, new Vector2(0.5f, 0f), origin.z, out Vector3 bottom))
            {
                EndPattern();
                return;
            }
            float extraHeight = Random.Range(Mathf.Max(1f, heightAboveScreen.x),
                Mathf.Max(1f, Mathf.Max(heightAboveScreen.x, heightAboveScreen.y)));
            float apex = Mathf.Max(origin.y + 1f, Mathf.Max(topLeft.y, topRight.y) + extraHeight);
            float acceleration = -Physics2D.gravity.y * Mathf.Max(0.01f, gravity);
            float speedY = Mathf.Sqrt(2f * acceleration * (apex - origin.y));
            float flightTime = speedY / acceleration + Mathf.Sqrt(2f * Mathf.Max(0f, apex - bottom.y) / acceleration);
            // Aim within the screen at the bottom so the longer airtime does not scatter coins off the sides.
            float targetX = Random.Range(topLeft.x, topRight.x);
            Vector2 velocity = new Vector2((targetX - origin.x) / flightTime -
                0.5f * Physics2D.gravity.x * gravity * flightTime, speedY);
            Boss.SpawnHazard(coinPrefab, origin, velocity, Mathf.Max(0.01f, gravity), damage,
                Mathf.Max(lifetime, flightTime + 2f));
            Boss.BodyAnimator?.Spit(interval);
            _emitted++;
            _timer = Mathf.Max(0.01f, interval);
        }

        public override void OnEnd()
        {
            if (!_opened) return;
            if (chestRenderer != null) chestRenderer.sprite = _previousSprite;
            _opened = false;
            Boss.BodyAnimator?.ResetPose();
        }

        private static bool ViewportOnPlane(Camera view, Vector2 viewport, float z, out Vector3 point)
        {
            Ray ray = view.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, z));
            bool hit = plane.Raycast(ray, out float distance);
            point = ray.GetPoint(distance);
            return hit;
        }

        public override void OnDie() => OnEnd();
        private void OnDisable() => OnEnd();
    }
}
