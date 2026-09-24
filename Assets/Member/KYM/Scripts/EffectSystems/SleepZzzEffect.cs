using TMPro;
using UnityEngine;

namespace Member.KYM.Scripts.EffectSystems
{
    public sealed class SleepZzzEffect : MonoBehaviour
    {
        [Header("표시 위치와 글꼴")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector3 spawnOffset = new(-0.25f, 0.8f, 0f);
        [SerializeField] private Color color = new(1f, 1f, 1f, 0.9f);
        [SerializeField] private string sortingLayerName = "Agent";
        [SerializeField] private int sortingOrder = 20;

        [Header("움직임")]
        [SerializeField, Min(0.05f)] private float spawnInterval = 0.48f;
        [SerializeField, Min(0.05f)] private float lifetime = 1.35f;
        [SerializeField, Min(0f)] private float riseDistance = 0.75f;
        [SerializeField, Min(0f)] private float swayDistance = 0.12f;
        [SerializeField] private Vector2 scaleRange = new(0.08f, 0.12f);

        private const int PoolSize = 5;
        private readonly ZParticle[] _particles = new ZParticle[PoolSize];
        private float _spawnTimer;
        private bool _isEmitting = true;

        private sealed class ZParticle
        {
            public TextMeshPro Text;
            public Vector3 StartPosition;
            public float Age;
            public float SwayPhase;
        }

        private void Awake()
        {
            if (followTarget == null || font == null)
            {
                Debug.LogError("수면 Z 이펙트에 플레이어와 글꼴을 연결해야 합니다.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < PoolSize; i++)
            {
                GameObject particleObject = new($"Sleep Z {i}");
                particleObject.transform.SetParent(transform, false);

                TextMeshPro text = particleObject.AddComponent<TextMeshPro>();
                text.font = font;
                text.text = "Z";
                text.fontSize = 4f;
                text.alignment = TextAlignmentOptions.Center;
                text.enableWordWrapping = false;
                text.color = color;

                MeshRenderer renderer = particleObject.GetComponent<MeshRenderer>();
                renderer.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
                renderer.sortingOrder = sortingOrder;

                _particles[i] = new ZParticle { Text = text };
                particleObject.SetActive(false);
            }
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;

            if (_isEmitting)
            {
                _spawnTimer -= deltaTime;
                if (_spawnTimer <= 0f)
                {
                    Spawn();
                    _spawnTimer = spawnInterval;
                }
            }

            foreach (ZParticle particle in _particles)
            {
                if (particle?.Text == null || !particle.Text.gameObject.activeSelf)
                    continue;

                particle.Age += deltaTime;
                float progress = Mathf.Clamp01(particle.Age / lifetime);
                if (progress >= 1f)
                {
                    particle.Text.gameObject.SetActive(false);
                    continue;
                }

                float sway = Mathf.Sin(progress * Mathf.PI * 2f + particle.SwayPhase)
                             * swayDistance * progress;
                particle.Text.transform.position = particle.StartPosition +
                    new Vector3(sway, riseDistance * progress, 0f);

                Color currentColor = color;
                currentColor.a *= Mathf.Min(progress / 0.2f, 1f) *
                                  Mathf.Clamp01((1f - progress) / 0.45f);
                particle.Text.color = currentColor;
            }
        }

        public void StopEmitting()
        {
            _isEmitting = false;
        }

        public void StartEmitting()
        {
            _isEmitting = true;
            _spawnTimer = 0f;
        }

        private void Spawn()
        {
            foreach (ZParticle particle in _particles)
            {
                if (particle.Text.gameObject.activeSelf)
                    continue;

                particle.Age = 0f;
                particle.SwayPhase = Random.Range(0f, Mathf.PI * 2f);
                particle.StartPosition = followTarget.position + spawnOffset +
                    Vector3.right * Random.Range(-0.08f, 0.08f);

                particle.Text.text = Random.value < 0.25f ? "z" : "Z";
                particle.Text.transform.position = particle.StartPosition;
                particle.Text.transform.localScale = Vector3.one *
                    Random.Range(scaleRange.x, scaleRange.y);
                particle.Text.color = new Color(color.r, color.g, color.b, 0f);
                particle.Text.gameObject.SetActive(true);
                return;
            }
        }

        private void OnValidate()
        {
            spawnInterval = Mathf.Max(0.05f, spawnInterval);
            lifetime = Mathf.Max(0.05f, lifetime);
            riseDistance = Mathf.Max(0f, riseDistance);
            swayDistance = Mathf.Max(0f, swayDistance);
            scaleRange.x = Mathf.Max(0.01f, scaleRange.x);
            scaleRange.y = Mathf.Max(scaleRange.x, scaleRange.y);
        }
    }
}
