using UnityEngine;

namespace Member.KYM.Scripts.EffectSystems
{
    public sealed class AmbientLeafEmitter : MonoBehaviour
    {
        [Header("화면과 렌더링")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private string sortingLayerName = "Agent";
        [SerializeField] private int sortingOrder = -20;

        [Header("드문 생성")]
        [SerializeField, Min(1)] private int poolSize = 8;
        [SerializeField] private Vector2 spawnInterval = new(2f, 4f);
        [SerializeField] private Vector2 lifetime = new(7f, 11f);

        [Header("이동과 색")]
        [SerializeField] private Vector2 fallSpeed = new(0.45f, 0.75f);
        [SerializeField] private Vector2 windSpeed = new(-0.2f, 0.05f);
        [SerializeField] private Vector2 flutterDistance = new(0.06f, 0.16f);
        [SerializeField] private Vector2 flutterFrequency = new(1.2f, 2.4f);
        [SerializeField] private Vector2 spinSpeed = new(-45f, 45f);
        [SerializeField] private Vector2 scale = new(0.6f, 0.95f);
        [SerializeField] private Color colorA = new(0.94f, 0.67f, 0.28f, 0.8f);
        [SerializeField] private Color colorB = new(0.78f, 0.43f, 0.21f, 0.7f);

        private Leaf[] _leaves;
        private Sprite _leafSprite;
        private Texture2D _leafTexture;
        private float _spawnTimer;

        private sealed class Leaf
        {
            public SpriteRenderer Renderer;
            public Vector3 BasePosition;
            public Color Color;
            public float Age;
            public float Lifetime;
            public float FallSpeed;
            public float WindSpeed;
            public float FlutterDistance;
            public float FlutterFrequency;
            public float Phase;
            public float SpinSpeed;
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                Debug.LogError("낙엽 이펙트에 카메라를 연결해야 합니다.", this);
                enabled = false;
                return;
            }

            _leaves = new Leaf[Mathf.Max(1, poolSize)];
            _spawnTimer = RandomInRange(spawnInterval);
            CreateLeafSprite();

            for (int i = 0; i < _leaves.Length; i++)
            {
                GameObject leafObject = new($"Leaf {i}");
                leafObject.transform.SetParent(transform, false);

                SpriteRenderer renderer = leafObject.AddComponent<SpriteRenderer>();
                renderer.sprite = _leafSprite;
                renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = sortingOrder;

                _leaves[i] = new Leaf { Renderer = renderer };
                leafObject.SetActive(false);
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _spawnTimer -= deltaTime;
            if (_spawnTimer <= 0f)
            {
                Spawn();
                _spawnTimer = RandomInRange(spawnInterval);
            }

            foreach (Leaf leaf in _leaves)
            {
                if (!leaf.Renderer.gameObject.activeSelf)
                    continue;

                leaf.Age += deltaTime;
                if (leaf.Age >= leaf.Lifetime)
                {
                    leaf.Renderer.gameObject.SetActive(false);
                    continue;
                }

                leaf.BasePosition += new Vector3(leaf.WindSpeed, -leaf.FallSpeed, 0f) * deltaTime;
                float flutter = Mathf.Sin(leaf.Age * leaf.FlutterFrequency * Mathf.PI * 2f + leaf.Phase)
                                * leaf.FlutterDistance;
                leaf.Renderer.transform.position = leaf.BasePosition + Vector3.right * flutter;
                leaf.Renderer.transform.Rotate(0f, 0f, leaf.SpinSpeed * deltaTime);

                float fadeIn = Mathf.Clamp01(leaf.Age / 0.4f);
                float fadeOut = Mathf.Clamp01((leaf.Lifetime - leaf.Age) / 0.8f);
                Color color = leaf.Color;
                color.a *= Mathf.Min(fadeIn, fadeOut);
                leaf.Renderer.color = color;
            }
        }

        private void Spawn()
        {
            foreach (Leaf leaf in _leaves)
            {
                if (leaf.Renderer.gameObject.activeSelf)
                    continue;

                float depth = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
                Vector3 position = targetCamera.ViewportToWorldPoint(
                    new Vector3(Random.Range(0.02f, 0.98f), 1.05f, Mathf.Max(0.1f, depth)));
                position.z = transform.position.z;

                leaf.BasePosition = position;
                leaf.Age = 0f;
                leaf.Lifetime = RandomInRange(lifetime);
                leaf.FallSpeed = RandomInRange(fallSpeed);
                leaf.WindSpeed = RandomInRange(windSpeed);
                leaf.FlutterDistance = RandomInRange(flutterDistance);
                leaf.FlutterFrequency = RandomInRange(flutterFrequency);
                leaf.SpinSpeed = RandomInRange(spinSpeed);
                leaf.Phase = Random.Range(0f, Mathf.PI * 2f);
                leaf.Color = Color.Lerp(colorA, colorB, Random.value);

                leaf.Renderer.transform.position = position;
                leaf.Renderer.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                leaf.Renderer.transform.localScale = Vector3.one * RandomInRange(scale);
                leaf.Renderer.color = new Color(leaf.Color.r, leaf.Color.g, leaf.Color.b, 0f);
                leaf.Renderer.gameObject.SetActive(true);
                return;
            }
        }

        private void CreateLeafSprite()
        {
            const int size = 9;
            string[] rows =
            {
                "...##....", "..####...", ".######..", "#######..", ".######..",
                "..#####..", "...###...", "....##...", ".....#..."
            };

            _leafTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = rows[size - 1 - y][x] == '#'
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(0, 0, 0, 0);
            }

            _leafTexture.SetPixels32(pixels);
            _leafTexture.Apply();
            _leafSprite = Sprite.Create(_leafTexture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), 32f);
            _leafSprite.hideFlags = HideFlags.DontSave;
        }

        private void OnDestroy()
        {
            if (_leafSprite != null)
                Destroy(_leafSprite);
            if (_leafTexture != null)
                Destroy(_leafTexture);
        }

        private static float RandomInRange(Vector2 range)
        {
            return Random.Range(range.x, range.y);
        }

        private void OnValidate()
        {
            poolSize = Mathf.Max(1, poolSize);
            spawnInterval.x = Mathf.Max(0.1f, spawnInterval.x);
            spawnInterval.y = Mathf.Max(spawnInterval.x, spawnInterval.y);
            lifetime.x = Mathf.Max(0.1f, lifetime.x);
            lifetime.y = Mathf.Max(lifetime.x, lifetime.y);
            fallSpeed.x = Mathf.Max(0f, fallSpeed.x);
            fallSpeed.y = Mathf.Max(fallSpeed.x, fallSpeed.y);
            flutterDistance.x = Mathf.Max(0f, flutterDistance.x);
            flutterDistance.y = Mathf.Max(flutterDistance.x, flutterDistance.y);
            flutterFrequency.x = Mathf.Max(0f, flutterFrequency.x);
            flutterFrequency.y = Mathf.Max(flutterFrequency.x, flutterFrequency.y);
            scale.x = Mathf.Max(0.01f, scale.x);
            scale.y = Mathf.Max(scale.x, scale.y);
        }
    }
}
