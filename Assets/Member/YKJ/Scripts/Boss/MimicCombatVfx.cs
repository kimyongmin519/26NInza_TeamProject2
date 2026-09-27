using System.Collections.Generic;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using UnityEngine;

namespace Member.YKJ.Bosses
{
    // Cosmetic geometry only. These effects never own collision, damage, or the global clock.
    [DisallowMultipleComponent]
    public sealed class MimicCombatVfx : MonoBehaviour
    {
        [SerializeField] private MimicBoss boss;
        [SerializeField] private SpriteRenderer chestRenderer;
        [SerializeField] private BoxCollider2D bodyCollider;
        [SerializeField] private Material glowMaterial;
        [SerializeField] private EventChannelSO cameraChannel;
        [Header("Palette")]
        [ColorUsage(true, true)] [SerializeField] private Color attackColor = new Color(3f, 0.06f, 0.03f, 1f);
        [ColorUsage(true, true)] [SerializeField] private Color counterColor = new Color(0.15f, 2.3f, 2.5f, 1f);
        [Header("Intensity")]
        [SerializeField, Min(0.1f)] private float mouthSize = 0.65f;
        [SerializeField, Min(0.1f)] private float impactSize = 1.4f;
        [SerializeField, Min(0f)] private float hitShakePower = 0.16f;
        [SerializeField, Min(0.01f)] private float hitFlashTime = 0.11f;
        [SerializeField, Min(0.02f)] private float afterimageInterval = 0.065f;
        [SerializeField, Range(8, 128)] private int maxStrokes = 80;

        private sealed class Stroke
        {
            public LineRenderer Line;
            public Vector3 Origin, Velocity;
            public Vector2 From, To;
            public Color Color;
            public float Age, Duration, Width;
        }
        private sealed class Ghost
        {
            public SpriteRenderer Renderer;
            public float Age;
        }
        private readonly List<Stroke> _strokes = new List<Stroke>();
        private readonly List<Ghost> _ghosts = new List<Ghost>();
        private Transform _root;
        private LineRenderer _chargeRing, _chargeCore;
        private SpriteRenderer _flash;
        private float _charge, _flashRemaining, _lastGhostTime = float.NegativeInfinity;
        private bool _muzzleActive, _sustaining;
        private int _burstIndex;
        private MaterialPropertyBlock _spriteProperties;
        private static readonly Vector3[] Segment = { Vector3.zero, Vector3.right };
        private static readonly Vector3[] Diamond = { Vector3.up, Vector3.right, Vector3.down, Vector3.left };

        private bool Ready => isActiveAndEnabled && glowMaterial != null && boss != null;

        private Transform EffectRoot()
        {
            if (_root != null) return _root;
            var root = new GameObject("Mimic Combat Effects");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            _root = root.transform;
            return _root;
        }

        private LineRenderer NewLine(string label, int order)
        {
            var line = new GameObject(label).AddComponent<LineRenderer>();
            line.transform.SetParent(EffectRoot(), false);
            line.sharedMaterial = glowMaterial;
            line.useWorldSpace = false;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 0;
            line.sortingLayerName = "Weapon";
            line.sortingOrder = order;
            line.enabled = false;
            return line;
        }

        private Stroke Emit(Vector3 position, Vector3[] points, bool loop, Vector2 from, Vector2 to,
            float width, float duration, Color color, Vector3 velocity = default, Material material = null)
        {
            if (!Ready) return null;
            Stroke stroke = _strokes.Find(item => !item.Line.enabled);
            if (stroke == null)
            {
                if (_strokes.Count >= Mathf.Max(8, maxStrokes)) return null;
                stroke = new Stroke { Line = NewLine("ImpactStroke", 42) };
                _strokes.Add(stroke);
            }
            stroke.Origin = position;
            stroke.Velocity = velocity;
            stroke.From = from;
            stroke.To = to;
            stroke.Width = width;
            stroke.Duration = Mathf.Max(0.01f, duration);
            stroke.Color = color;
            stroke.Age = 0f;
            LineRenderer line = stroke.Line;
            line.sharedMaterial = material != null ? material : glowMaterial;
            line.loop = loop;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.transform.SetPositionAndRotation(position, Quaternion.identity);
            line.transform.localScale = new Vector3(from.x, from.y, 1f);
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color;
            line.enabled = true;
            return stroke;
        }

        private void Ring(Vector3 position, Vector2 size, Color color, float duration)
        {
            var points = new Vector3[16];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / points.Length;
                points[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            Emit(position, points, true, size * 0.12f, size, 0.09f, duration, color);
        }

        private void Sparks(Vector3 position, Color color, int count, float radius, bool upwards = false)
        {
            // A local deterministic spread keeps cosmetic effects out of gameplay's random sequence.
            float offset = (++_burstIndex * 137.5f) % 360f;
            for (int i = 0; i < count; i++)
            {
                float angle = upwards ? Mathf.Lerp(12f, 168f, (i + 0.5f) / count) : offset + i * 360f / count;
                Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
                float length = radius * (0.18f + (i % 3) * 0.055f);
                Stroke stroke = Emit(position + direction * 0.07f, Segment, false,
                    new Vector2(length, 1f), new Vector2(length * 0.12f, 1f), 0.055f,
                    0.2f + (i % 3) * 0.045f, color, direction * radius * 3f);
                if (stroke != null) stroke.Line.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        public void SetCharge(float progress)
        {
            if (!Ready) return;
            _muzzleActive = true;
            _sustaining = false;
            _charge = Mathf.Clamp01(progress);
            DrawMuzzle();
        }

        public void LaserShot()
        {
            if (!Ready) return;
            _muzzleActive = _sustaining = true;
            _charge = 1f;
            Sparks(boss.MouthPosition, attackColor, 6, mouthSize);
            Ring(boss.MouthPosition, Vector2.one * mouthSize * 1.5f, attackColor, 0.18f);
            Emit(boss.MouthPosition, Diamond, true, Vector2.one * mouthSize,
                Vector2.one * mouthSize * 0.15f, 0.13f, 0.1f, Color.white * 2f);
            DrawMuzzle();
        }

        public void EndMuzzle()
        {
            _muzzleActive = false;
            if (_chargeRing != null) _chargeRing.enabled = false;
            if (_chargeCore != null) _chargeCore.enabled = false;
        }

        private void DrawMuzzle()
        {
            if (!_muzzleActive || !Ready) return;
            if (_chargeRing == null)
            {
                _chargeRing = NewLine("MouthChargeRing", 44);
                _chargeRing.loop = true;
                _chargeRing.positionCount = 4;
                _chargeRing.SetPositions(Diamond);
                _chargeCore = NewLine("MouthChargeCore", 45);
                _chargeCore.positionCount = 2;
                _chargeCore.SetPosition(0, Vector3.left * 0.5f);
                _chargeCore.SetPosition(1, Vector3.right * 0.5f);
            }
            float pulse = 1f + Mathf.Sin(Time.time * 28f) * (_sustaining ? 0.08f : 0.025f);
            float radius = mouthSize * (_sustaining ? 0.6f : Mathf.Lerp(1.25f, 0.45f, _charge)) * pulse;
            _chargeRing.transform.SetPositionAndRotation(boss.MouthPosition,
                Quaternion.Euler(0f, 0f, Time.time * (_sustaining ? 110f : -160f)));
            _chargeRing.transform.localScale = Vector3.one * radius;
            _chargeRing.startWidth = _chargeRing.endWidth = 0.045f + _charge * 0.025f;
            Color ringColor = attackColor;
            ringColor.a = _sustaining ? 0.65f : Mathf.Lerp(0.2f, 0.8f, _charge);
            _chargeRing.startColor = _chargeRing.endColor = ringColor;
            _chargeCore.transform.position = boss.MouthPosition;
            float core = mouthSize * Mathf.Lerp(0.1f, 0.5f, _charge) * pulse;
            _chargeCore.transform.localScale = new Vector3(core, 1f, 1f);
            _chargeCore.startWidth = _chargeCore.endWidth = core;
            _chargeCore.startColor = _chargeCore.endColor = attackColor;
            _chargeRing.enabled = _chargeCore.enabled = true;
        }

        public void Spit()
        {
            if (!Ready) return;
            Sparks(boss.MouthPosition, new Color(2.5f, 1.1f, 0.15f), 5, mouthSize * 0.7f, true);
        }

        public void Land()
        {
            if (!Ready || bodyCollider == null) return;
            Vector3 feet = bodyCollider.transform.TransformPoint(bodyCollider.offset + Vector2.down * bodyCollider.size.y * 0.5f);
            float radius = Mathf.Max(1f, bodyCollider.size.x * Mathf.Abs(bodyCollider.transform.lossyScale.x)) * 1.2f;
            Ring(feet + Vector3.up * 0.06f, new Vector2(radius, 0.35f), attackColor, 0.32f);
            Sparks(feet + Vector3.up * 0.1f, new Color(2f, 0.8f, 0.15f), 10, radius * 0.5f, true);
            // A brief horizontal impact flash, not another damaging shockwave.
            Emit(feet - Vector3.right * radius * 0.5f, Segment, false, new Vector2(radius, 1f),
                new Vector2(radius * 1.1f, 1f), 0.18f, 0.1f, Color.white);
        }

        public void JumpAfterimage()
        {
            if (!Ready || chestRenderer == null || Time.time - _lastGhostTime < afterimageInterval) return;
            _lastGhostTime = Time.time;
            Ghost ghost = _ghosts.Find(item => !item.Renderer.enabled);
            if (ghost == null)
            {
                if (_ghosts.Count >= 6) return;
                var renderer = new GameObject("JumpAfterimage").AddComponent<SpriteRenderer>();
                renderer.transform.SetParent(EffectRoot(), false);
                renderer.sharedMaterial = glowMaterial;
                ghost = new Ghost { Renderer = renderer };
                _ghosts.Add(ghost);
            }
            SpriteRenderer visual = ghost.Renderer;
            visual.sprite = chestRenderer.sprite;
            visual.flipX = chestRenderer.flipX;
            visual.flipY = chestRenderer.flipY;
            visual.sortingLayerID = chestRenderer.sortingLayerID;
            visual.sortingOrder = chestRenderer.sortingOrder - 1;
            visual.transform.SetPositionAndRotation(chestRenderer.transform.position, chestRenderer.transform.rotation);
            visual.transform.localScale = chestRenderer.transform.lossyScale;
            SetSpriteTint(visual, new Color(1.3f, 0.08f, 0.025f, 0.18f));
            visual.enabled = true;
            ghost.Age = 0f;
        }

        public void Catch(Vector3 position)
        {
            if (!Ready) return;
            Ring(position, Vector2.one * 0.7f, counterColor, 0.16f);
            Sparks(position, counterColor, 6, 0.65f);
        }

        public void Hit(Vector3 position)
        {
            if (!Ready) return;
            Sparks(position, counterColor, 12, impactSize);
            Ring(position, Vector2.one * impactSize, counterColor, 0.22f);
            Emit(position, Diamond, true, Vector2.one * impactSize * 0.8f, Vector2.one * 0.06f,
                0.17f, 0.1f, Color.white);
            Flash();
            if (cameraChannel != null && hitShakePower > 0f)
                cameraChannel.RaiseEvent(new CameraShakeEvent().InitData(hitShakePower, 0.12f));
        }

        public void PhaseBreak()
        {
            if (!Ready) return;
            EndMuzzle();
            Ring(boss.MouthPosition, Vector2.one * impactSize * 2.5f, attackColor, 0.45f);
            Sparks(boss.MouthPosition, attackColor, 16, impactSize * 1.5f);
            Flash();
        }

        public void DeathBurst()
        {
            if (!Ready) return;
            EndMuzzle();
            Ring(boss.MouthPosition, new Vector2(impactSize * 3f, impactSize), counterColor, 0.5f);
            Sparks(boss.MouthPosition, counterColor, 20, impactSize * 2f);
            Flash();
        }

        private void Flash()
        {
            if (chestRenderer == null) return;
            if (_flash == null)
            {
                _flash = new GameObject("HitSilhouette").AddComponent<SpriteRenderer>();
                _flash.transform.SetParent(chestRenderer.transform, false);
                _flash.sharedMaterial = glowMaterial;
            }
            _flashRemaining = Mathf.Max(0.01f, hitFlashTime);
            UpdateFlash();
        }

        private void UpdateFlash()
        {
            if (_flash == null || chestRenderer == null) return;
            _flash.enabled = _flashRemaining > 0f && chestRenderer.enabled;
            if (!_flash.enabled) return;
            _flash.sprite = chestRenderer.sprite;
            _flash.flipX = chestRenderer.flipX;
            _flash.flipY = chestRenderer.flipY;
            _flash.sortingLayerID = chestRenderer.sortingLayerID;
            _flash.sortingOrder = chestRenderer.sortingOrder + 1;
            SetSpriteTint(_flash, new Color(1f, 1f, 1f, Mathf.Clamp01(_flashRemaining / Mathf.Max(0.01f, hitFlashTime))));
        }

        private void SetSpriteTint(SpriteRenderer renderer, Color color)
        {
            _spriteProperties ??= new MaterialPropertyBlock();
            _spriteProperties.Clear();
            _spriteProperties.SetColor("_Tint", color);
            _spriteProperties.SetVector("_Flip", new Vector4(renderer.flipX ? -1f : 1f, renderer.flipY ? -1f : 1f, 1f, 1f));
            renderer.color = Color.white;
            renderer.SetPropertyBlock(_spriteProperties);
        }

        public void FadeBeam(Vector3 start, Vector3 end, float width, Color color, Material material)
        {
            Emit(start, new[] { Vector3.zero, end - start }, false, Vector2.one, Vector2.one,
                width, 0.16f, color, material: material);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            foreach (Stroke stroke in _strokes)
            {
                if (!stroke.Line.enabled) continue;
                stroke.Age += dt;
                float t = Mathf.Clamp01(stroke.Age / stroke.Duration);
                if (t >= 1f) { stroke.Line.enabled = false; continue; }
                float ease = 1f - (1f - t) * (1f - t);
                Vector2 scale = Vector2.Lerp(stroke.From, stroke.To, ease);
                stroke.Line.transform.localScale = new Vector3(scale.x, scale.y, 1f);
                stroke.Line.transform.position = stroke.Origin + stroke.Velocity * (stroke.Duration * ease);
                Color color = stroke.Color;
                color.a *= (1f - t) * (1f - t);
                stroke.Line.startColor = stroke.Line.endColor = color;
                stroke.Line.startWidth = stroke.Line.endWidth = stroke.Width * (1f - t);
            }
            foreach (Ghost ghost in _ghosts)
            {
                if (!ghost.Renderer.enabled) continue;
                ghost.Age += dt;
                Color color = new Color(1.3f, 0.08f, 0.025f, Mathf.Max(0f, 0.18f * (1f - ghost.Age / 0.18f)));
                SetSpriteTint(ghost.Renderer, color);
                ghost.Renderer.enabled = ghost.Age < 0.18f;
            }
            _flashRemaining = Mathf.Max(0f, _flashRemaining - dt);
            UpdateFlash();
            DrawMuzzle();
        }

        public void Clear()
        {
            EndMuzzle();
            _flashRemaining = 0f;
            _lastGhostTime = float.NegativeInfinity;
            if (_flash != null) _flash.enabled = false;
            foreach (Stroke stroke in _strokes) if (stroke.Line != null) stroke.Line.enabled = false;
            foreach (Ghost ghost in _ghosts) if (ghost.Renderer != null) ghost.Renderer.enabled = false;
        }
        private void OnDisable() => Clear();
        private void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
            if (_flash != null) Destroy(_flash.gameObject);
        }
    }
}
