using DG.Tweening;
using UnityEngine;

namespace Member.YKJ.Bosses
{
    // Only the sprite moves/spins. The root and collider keep their original ballistic trajectory.
    [DisallowMultipleComponent]
    public sealed class MimicSpitPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private Material revealMaterial;
        [SerializeField, Min(0.02f)] private float revealTime = 0.12f;
        [SerializeField, Min(0f)] private float spinSpeed = 540f;
        [SerializeField, Min(0f)] private float exitDistance = 0.35f;
        [SerializeField, Min(0.01f)] private float puffSize = 0.2f;
        private MimicWeapon _weapon;
        private Rigidbody2D _body;
        private Transform _visual;
        private Vector3 _restPosition, _restScale;
        private Quaternion _restRotation;
        private Material _originalMaterial;
        private MaterialPropertyBlock _originalProperties, _properties;
        private Sequence _reveal;
        private ParticleSystem _puff;
        private float _age, _spin;
        private bool _playing;
        private bool _revealed;
        private static readonly int ClipEnabled = Shader.PropertyToID("_ClipEnabled");
        private static readonly int MouthPlane = Shader.PropertyToID("_MouthPlane");

        private void Awake()
        {
            _weapon = GetComponent<MimicWeapon>();
            _body = GetComponent<Rigidbody2D>();
            if (weaponRenderer == null) weaponRenderer = GetComponentInChildren<SpriteRenderer>();
            if (weaponRenderer == null || weaponRenderer.transform == transform) return;
            _visual = weaponRenderer.transform;
            _restPosition = _visual.localPosition;
            _restScale = _visual.localScale;
            _restRotation = _visual.localRotation;
            _originalMaterial = weaponRenderer.sharedMaterial;
            _originalProperties = new MaterialPropertyBlock();
            _properties = new MaterialPropertyBlock();
            weaponRenderer.GetPropertyBlock(_originalProperties);
        }

        public void Play(Vector3 mouth)
        {
            if (!isActiveAndEnabled || _visual == null || revealMaterial == null || _body == null || _weapon == null) return;
            ResetVisual();
            Vector2 direction = _body.linearVelocity.sqrMagnitude > 0.001f ? _body.linearVelocity.normalized : Vector2.up;
            _playing = true;
            _revealed = false;
            _age = 0f;
            _spin = direction.x < 0f ? spinSpeed : -spinSpeed;
            weaponRenderer.sharedMaterial = revealMaterial;
            _properties.Clear();
            _properties.SetFloat(ClipEnabled, 1f);
            _properties.SetVector(MouthPlane, new Vector4(direction.x, direction.y, 0f, Vector2.Dot(direction, mouth)));
            weaponRenderer.SetPropertyBlock(_properties);
            _visual.position = mouth - (Vector3)direction * exitDistance;
            _visual.localScale = Vector3.Scale(_restScale, new Vector3(0.55f, 1.15f, 1f));
            _visual.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            _reveal = DOTween.Sequence()
                .Append(_visual.DOLocalMove(_restPosition, revealTime).SetEase(Ease.OutCubic))
                .Join(_visual.DOScale(_restScale, revealTime).SetEase(Ease.OutBack));
            EmitPuff(mouth, direction);
        }

        private void EmitPuff(Vector3 mouth, Vector2 direction)
        {
            if (_puff == null)
            {
                var go = new GameObject("Spit Pixel Puff");
                go.transform.SetParent(transform, false);
                _puff = go.AddComponent<ParticleSystem>();
                _puff.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = _puff.main;
                main.playOnAwake = false;
                main.loop = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 12;
                main.startSpeed = 0f;
                main.gravityModifier = 0.15f;
                var emission = _puff.emission;
                emission.enabled = false;
                var shape = _puff.shape;
                shape.enabled = false;
                var lifetime = _puff.colorOverLifetime;
                lifetime.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
                lifetime.color = gradient;
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = revealMaterial;
                renderer.sortingLayerID = weaponRenderer.sortingLayerID;
                renderer.sortingOrder = weaponRenderer.sortingOrder + 1;
            }
            _puff.Clear();
            _puff.Play();
            for (int i = 0; i < 8; i++)
            {
                Vector3 velocity = Quaternion.Euler(0f, 0f, Mathf.Lerp(-28f, 28f, i / 7f)) * direction;
                _puff.Emit(new ParticleSystem.EmitParams
                {
                    position = mouth,
                    velocity = velocity * (2.5f + (i % 3) * 0.8f),
                    startLifetime = 0.16f + (i % 3) * 0.045f,
                    startSize = puffSize * (0.65f + (i % 3) * 0.2f),
                    startColor = i < 2 ? new Color(1f, 0.88f, 0.55f, 0.9f) : new Color(0.8f, 0.76f, 0.68f, 0.65f)
                }, 1);
            }
        }

        private void LateUpdate()
        {
            if (!_playing) return;
            if (_weapon.State != MimicWeapon.WeaponState.BossFlight) { ResetVisual(); return; }
            _age += Time.deltaTime;
            if (_age < revealTime) return;
            // Drop the mouth clip once outside, so a returning arc cannot disappear behind that plane.
            if (!_revealed)
            {
                weaponRenderer.sharedMaterial = _originalMaterial;
                weaponRenderer.SetPropertyBlock(_originalProperties);
                _revealed = true;
            }
            _visual.Rotate(0f, 0f, _spin * Time.deltaTime, Space.Self);
        }

        private void ResetVisual()
        {
            _reveal?.Kill();
            _reveal = null;
            _playing = false;
            if (_visual == null) return;
            _visual.localPosition = _restPosition;
            _visual.localScale = _restScale;
            _visual.localRotation = _restRotation;
            weaponRenderer.sharedMaterial = _originalMaterial;
            weaponRenderer.SetPropertyBlock(_originalProperties);
        }
        private void OnDisable()
        {
            ResetVisual();
            if (_puff != null) _puff.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
