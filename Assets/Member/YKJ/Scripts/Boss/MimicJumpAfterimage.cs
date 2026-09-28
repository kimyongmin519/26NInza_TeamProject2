using UnityEngine;
using UnityEngine.SceneManagement;

namespace Member.YKJ.Bosses
{
    [DisallowMultipleComponent]
    public sealed class MimicJumpAfterimage : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sourceRenderer;
        [SerializeField] private Material afterimageMaterial;
        [SerializeField, Min(0.01f)] private float emissionInterval = 0.05f;
        [SerializeField, Min(0.01f)] private float lifetime = 0.22f;
        [SerializeField] private Color tint = new Color(1f, 0.18f, 0.12f, 0.38f);
        [SerializeField, Range(2, 24)] private int poolSize = 8;

        private GameObject _poolRoot;
        private SpriteRenderer[] _images;
        private float[] _ages;
        private float[] _alphas;
        private int _next;
        private float _elapsed;
        private bool _emitting;

        public void Begin()
        {
            if (!isActiveAndEnabled || sourceRenderer == null)
                return;
            EnsurePool();
            _elapsed = 0f;
            _emitting = true;
        }

        public void Stop(bool clear = false)
        {
            _emitting = false;
            if (!clear || _images == null)
                return;
            foreach (SpriteRenderer image in _images)
                if (image != null) image.enabled = false;
        }

        private void EnsurePool()
        {
            if (_poolRoot != null)
                return;
            // Keep snapshots outside the moving/scaling boss hierarchy.
            _poolRoot = new GameObject(name + " Jump Afterimages");
            SceneManager.MoveGameObjectToScene(_poolRoot, gameObject.scene);
            int count = Mathf.Clamp(poolSize, 2, 24);
            _images = new SpriteRenderer[count];
            _ages = new float[count];
            _alphas = new float[count];
            for (int i = 0; i < count; i++)
            {
                var image = new GameObject("Afterimage " + i).AddComponent<SpriteRenderer>();
                image.transform.SetParent(_poolRoot.transform, false);
                image.gameObject.layer = sourceRenderer.gameObject.layer;
                image.sharedMaterial = afterimageMaterial != null ? afterimageMaterial : sourceRenderer.sharedMaterial;
                image.enabled = false;
                _images[i] = image;
            }
        }

        private void LateUpdate() => Tick(Time.deltaTime);

        private void Tick(float deltaTime)
        {
            if (_images == null || deltaTime <= 0f)
                return;
            float duration = Mathf.Max(0.01f, lifetime);
            for (int i = 0; i < _images.Length; i++)
            {
                SpriteRenderer image = _images[i];
                if (!image.enabled) continue;
                _ages[i] += deltaTime;
                float remaining = Mathf.Clamp01(1f - _ages[i] / duration);
                Color color = image.color;
                color.a = _alphas[i] * remaining * remaining;
                image.color = color;
                image.enabled = remaining > 0f;
            }

            if (!_emitting || sourceRenderer == null || !sourceRenderer.enabled ||
                !sourceRenderer.gameObject.activeInHierarchy || sourceRenderer.sprite == null)
                return;
            _elapsed += deltaTime;
            float interval = Mathf.Max(0.01f, emissionInterval);
            if (_elapsed < interval) return;
            // At most one snapshot per frame; a hitch must not stack identical images.
            _elapsed %= interval;
            SpriteRenderer snapshot = _images[_next];
            Transform source = sourceRenderer.transform;
            snapshot.transform.SetPositionAndRotation(source.position, source.rotation);
            snapshot.transform.localScale = source.lossyScale;
            snapshot.sprite = sourceRenderer.sprite;
            snapshot.flipX = sourceRenderer.flipX;
            snapshot.flipY = sourceRenderer.flipY;
            snapshot.sortingLayerID = sourceRenderer.sortingLayerID;
            snapshot.sortingOrder = sourceRenderer.sortingOrder - 1;
            snapshot.spriteSortPoint = sourceRenderer.spriteSortPoint;
            Color colorTint = tint;
            colorTint.a *= sourceRenderer.color.a;
            snapshot.color = colorTint;
            _alphas[_next] = colorTint.a;
            _ages[_next] = 0f;
            snapshot.enabled = true;
            _next = (_next + 1) % _images.Length;
        }

        private void OnDisable() => Stop(true);

        private void OnDestroy()
        {
            if (_poolRoot != null) Destroy(_poolRoot);
        }
    }
}
