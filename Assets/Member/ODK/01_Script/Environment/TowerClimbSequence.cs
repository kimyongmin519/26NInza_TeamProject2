using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Swordmaster;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

namespace Member.ODK.Scripts.Environment
{
    public class TowerClimbSequence : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float climbDuration = 40f;

        [Header("Background")]
        [SerializeField] private VerticalLoopScroller[] scrollers;
        [SerializeField] private float climbSpeedScale = 1f;
        [SerializeField] private float fallSpeedScale = -4.5f;
        [SerializeField] private float fallRampDuration = 0.9f;

        [Header("World")]
        [SerializeField] private float worldFallSpeed = 4.5f;
        [SerializeField] private float shaftHalfWidth = 0f;
        [SerializeField] private float shaftPadding = 1.2f;
        [SerializeField] private LayerMask playerLayer = 1 << 6;
        [SerializeField] private float hazardDamage = 1f;

        [Header("Tile Obstacles")]
        [SerializeField] private TileBase obstacleTile;
        [SerializeField] private string obstacleSortingLayer = "Prop";
        [SerializeField] private int obstacleSortingOrder = 5;
        [SerializeField] private Vector2 obstacleInterval = new Vector2(1.6f, 2.8f);
        [SerializeField] private Vector2Int barLengthRange = new Vector2Int(3, 6);
        [SerializeField] private Vector2 rotateSpeedRange = new Vector2(60f, 130f);
        [SerializeField] private Vector2 sweepAmplitudeRange = new Vector2(2f, 4.5f);
        [SerializeField] private Vector2 sweepFrequencyRange = new Vector2(0.25f, 0.5f);
        [SerializeField, Range(0f, 1f)] private float rotateChance = 0.35f;
        [SerializeField, Range(0f, 1f)] private float sweepChance = 0.4f;

        [Header("Falling Swords")]
        [SerializeField] private Sprite swordSprite;
        [SerializeField] private Color swordTint = new Color(0.9f, 0.7f, 1f, 1f);
        [SerializeField] private float swordScale = 0.07f;
        [SerializeField] private Vector2 swordInterval = new Vector2(1.1f, 2.2f);
        [SerializeField] private Vector2Int swordsPerVolley = new Vector2Int(1, 3);
        [SerializeField] private float swordWarning = 0.7f;
        [SerializeField] private float swordDropSpeed = 24f;

        [Header("Swordmaster Arrival")]
        [SerializeField] private Swordmaster swordmaster;
        [SerializeField] private Swordmaster swordmasterPrefab;
        [SerializeField] private Transform swordmasterAppearPoint;
        [SerializeField] private float appearDuration = 1.1f;
        [SerializeField] private float giantSwordWindup = 0.8f;
        [SerializeField] private float giantSwordScale = 0.32f;
        [SerializeField] private float giantSwordSpeed = 38f;
        [SerializeField] private Transform giantSwordTarget;
        [SerializeField] private float impactShake = 1.6f;
        [SerializeField] private float battleStartDelay = 1f;

        [Header("Events")]
        [SerializeField] private UnityEvent onClimbStarted;
        [SerializeField] private UnityEvent onClimbFinished;
        [SerializeField] private UnityEvent onFallStarted;

        public float WorldFallSpeed { get; private set; }
        public float DespawnY => ViewCenter.y - ViewHalfHeight - 4f;
        public bool IsClimbing { get; private set; }

        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly Dictionary<VerticalLoopScroller, float> baseSpeeds = new Dictionary<VerticalLoopScroller, float>();
        private Coroutine climbRoutine;
        private Coroutine obstacleRoutine;
        private Coroutine swordRoutine;
        private bool finished;
        private Material lineMaterial;

        private Camera ViewCamera => Camera.main;
        private Vector3 ViewCenter => ViewCamera != null ? ViewCamera.transform.position : transform.position;
        private float ViewHalfHeight => ViewCamera != null && ViewCamera.orthographic ? ViewCamera.orthographicSize : 8f;
        private float ViewHalfWidth => ViewCamera != null && ViewCamera.orthographic ? ViewCamera.orthographicSize * ViewCamera.aspect : 12f;
        private float ShaftHalfWidth => shaftHalfWidth > 0f ? shaftHalfWidth : Mathf.Max(1f, ViewHalfWidth - shaftPadding);

        private void Awake()
        {
            if (swordmaster != null)
            {
                swordmaster.SetAutoStart(false);
                swordmaster.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (autoStart) StartCoroutine(StartNextFrame());
        }

        private IEnumerator StartNextFrame()
        {
            yield return null;
            StartClimb();
        }

        [ContextMenu("Start Climb")]
        public void StartClimb()
        {
            if (IsClimbing) return;
            finished = false;
            IsClimbing = true;
            WorldFallSpeed = worldFallSpeed;
            CacheBaseSpeeds();
            foreach (VerticalLoopScroller scroller in scrollers)
            {
                if (scroller == null) continue;
                scroller.DOKill();
                scroller.Speed = baseSpeeds[scroller] * climbSpeedScale;
                scroller.IsPlaying = true;
            }
            obstacleRoutine = StartCoroutine(ObstacleLoop());
            swordRoutine = StartCoroutine(SwordLoop());
            climbRoutine = StartCoroutine(ClimbTimer());
            onClimbStarted?.Invoke();
        }

        [ContextMenu("Finish Climb (Test)")]
        public void FinishClimb()
        {
            if (finished) return;
            finished = true;
            IsClimbing = false;
            StopRoutine(ref climbRoutine);
            StopRoutine(ref obstacleRoutine);
            StopRoutine(ref swordRoutine);
            CacheBaseSpeeds();
            StartCoroutine(ArrivalRoutine());
        }

        private IEnumerator ClimbTimer()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, climbDuration));
            climbRoutine = null;
            FinishClimb();
        }

        private IEnumerator ObstacleLoop()
        {
            while (IsClimbing)
            {
                yield return new WaitForSeconds(Random.Range(obstacleInterval.x, obstacleInterval.y));
                if (!IsClimbing) yield break;
                SpawnTileObstacle();
            }
        }

        private IEnumerator SwordLoop()
        {
            yield return new WaitForSeconds(swordInterval.y);
            while (IsClimbing)
            {
                int count = Random.Range(Mathf.Max(1, swordsPerVolley.x), Mathf.Max(swordsPerVolley.x, swordsPerVolley.y) + 1);
                for (int i = 0; i < count; i++)
                {
                    SpawnFallingSword();
                    yield return new WaitForSeconds(0.12f);
                }
                yield return new WaitForSeconds(Random.Range(swordInterval.x, swordInterval.y));
            }
        }

        private void SpawnTileObstacle()
        {
            if (obstacleTile == null) return;
            int length = Random.Range(Mathf.Max(1, barLengthRange.x), Mathf.Max(barLengthRange.x, barLengthRange.y) + 1);
            float roll = Random.value;
            ClimbHazard.MotionType motion = roll < rotateChance
                ? ClimbHazard.MotionType.Rotate
                : roll < rotateChance + sweepChance ? ClimbHazard.MotionType.Sweep : ClimbHazard.MotionType.Descend;

            float halfWidth = ShaftHalfWidth;
            float margin = motion == ClimbHazard.MotionType.Rotate ? length * 0.5f : length * 0.5f;
            float x = ViewCenter.x + Random.Range(-halfWidth + margin, halfWidth - margin);
            if (halfWidth - margin <= -halfWidth + margin) x = ViewCenter.x;
            Vector3 position = new Vector3(x, ViewCenter.y + ViewHalfHeight + length + 1f, 0f);

            GameObject root = new GameObject("Climb Obstacle " + motion);
            root.transform.position = position;
            root.AddComponent<Grid>();
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;

            GameObject tileObject = new GameObject("Tiles");
            tileObject.transform.SetParent(root.transform, false);
            tileObject.transform.localPosition = new Vector3(-length * 0.5f, -0.5f, 0f);
            Tilemap tilemap = tileObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = tileObject.AddComponent<TilemapRenderer>();
            renderer.sortingLayerName = obstacleSortingLayer;
            renderer.sortingOrder = obstacleSortingOrder;
            for (int i = 0; i < length; i++)
                tilemap.SetTile(new Vector3Int(i, 0, 0), obstacleTile);
            TilemapCollider2D collider = tileObject.AddComponent<TilemapCollider2D>();
            collider.isTrigger = true;

            ClimbHazard hazard = root.AddComponent<ClimbHazard>();
            hazard.Initialize(this, motion, hazardDamage, playerLayer);
            if (motion == ClimbHazard.MotionType.Rotate)
            {
                float speed = Random.Range(rotateSpeedRange.x, rotateSpeedRange.y) * (Random.value < 0.5f ? -1f : 1f);
                hazard.ConfigureRotate(speed);
                root.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 180f));
            }
            else if (motion == ClimbHazard.MotionType.Sweep)
            {
                float amplitude = Mathf.Min(Random.Range(sweepAmplitudeRange.x, sweepAmplitudeRange.y), Mathf.Max(0f, halfWidth - length * 0.5f));
                hazard.ConfigureSweep(amplitude, Random.Range(sweepFrequencyRange.x, sweepFrequencyRange.y));
                root.transform.position = new Vector3(ViewCenter.x, position.y, 0f);
                hazard.Initialize(this, motion, hazardDamage, playerLayer);
            }
            Track(root);
        }

        private void SpawnFallingSword()
        {
            if (swordSprite == null) return;
            float halfWidth = ShaftHalfWidth;
            float x = ViewCenter.x + Random.Range(-halfWidth, halfWidth);
            Vector3 position = new Vector3(x, ViewCenter.y + ViewHalfHeight - 0.6f, 0f);

            GameObject root = CreateSwordObject("Falling Sword", position, swordScale, swordTint);
            LineRenderer warning = CreateWarningLine(root.transform);
            ClimbHazard hazard = root.AddComponent<ClimbHazard>();
            hazard.Initialize(this, ClimbHazard.MotionType.Drop, hazardDamage, playerLayer);
            hazard.ConfigureDrop(swordDropSpeed, swordWarning, warning);
            Track(root);
        }

        private GameObject CreateSwordObject(string objectName, Vector3 position, float scale, Color tint)
        {
            GameObject root = new GameObject(objectName);
            root.transform.position = position;
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;

            GameObject visual = new GameObject("Blade");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, -135f);
            visual.transform.localScale = Vector3.one * scale;
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = swordSprite;
            renderer.color = tint;
            renderer.sortingLayerName = obstacleSortingLayer;
            renderer.sortingOrder = obstacleSortingOrder + 1;

            CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
            collider.isTrigger = true;
            collider.direction = CapsuleDirection2D.Vertical;
            Vector2 size = swordSprite.bounds.size * scale;
            float length = Mathf.Max(size.x, size.y) * 1.2f;
            collider.size = new Vector2(length * 0.2f, length);
            return root;
        }

        private LineRenderer CreateWarningLine(Transform parent)
        {
            GameObject lineObject = new GameObject("Warning");
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = 0.08f;
            line.numCapVertices = 2;
            line.sortingLayerName = obstacleSortingLayer;
            line.sortingOrder = obstacleSortingOrder - 1;
            if (lineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) lineMaterial = new Material(shader);
            }
            if (lineMaterial != null) line.sharedMaterial = lineMaterial;
            return line;
        }

        private IEnumerator ArrivalRoutine()
        {
            DOTween.To(() => WorldFallSpeed, value => WorldFallSpeed = value, 0f, 0.8f).SetTarget(this);
            foreach (VerticalLoopScroller scroller in scrollers)
            {
                if (scroller == null) continue;
                scroller.DOKill();
                DOTween.To(() => scroller.Speed, value => scroller.Speed = value, 0f, 0.8f).SetTarget(scroller);
            }
            ClearSpawned();
            onClimbFinished?.Invoke();

            Swordmaster boss = PrepareSwordmaster();
            Vector3 appear = swordmasterAppearPoint != null
                ? swordmasterAppearPoint.position
                : ViewCenter + Vector3.up * ViewHalfHeight * 0.45f;
            appear.z = 0f;

            if (boss != null)
            {
                boss.transform.position = appear + Vector3.up * (ViewHalfHeight + 4f);
                boss.gameObject.SetActive(true);
                boss.PlayAnimation(Swordmaster.FallState);
                yield return boss.transform.DOMove(appear, appearDuration).SetEase(Ease.OutCubic).WaitForCompletion();
                boss.Cue(SwordmasterCue.Teleport, appear);
                boss.PlayAnimation(Swordmaster.JumpState);
            }
            else
            {
                yield return new WaitForSeconds(appearDuration);
            }

            yield return new WaitForSeconds(giantSwordWindup);
            yield return FireGiantSword(boss, appear);

            onFallStarted?.Invoke();
            foreach (VerticalLoopScroller scroller in scrollers)
            {
                if (scroller == null) continue;
                float target = baseSpeeds.TryGetValue(scroller, out float baseSpeed) ? baseSpeed * fallSpeedScale : fallSpeedScale;
                scroller.IsPlaying = true;
                scroller.DOKill();
                DOTween.To(() => scroller.Speed, value => scroller.Speed = value, target, fallRampDuration)
                    .SetEase(Ease.InQuad)
                    .SetTarget(scroller);
            }

            yield return new WaitForSeconds(battleStartDelay);
            if (boss != null)
            {
                if (boss.Arena == null) boss.transform.position = appear;
                boss.BeginBattle();
            }
        }

        private Swordmaster PrepareSwordmaster()
        {
            if (swordmaster == null && swordmasterPrefab != null)
            {
                swordmaster = Instantiate(swordmasterPrefab);
                swordmaster.SetAutoStart(false);
                swordmaster.gameObject.SetActive(false);
            }
            if (swordmaster != null) swordmaster.SetAutoStart(false);
            return swordmaster;
        }

        private IEnumerator FireGiantSword(Swordmaster boss, Vector3 from)
        {
            if (swordSprite == null) yield break;
            Vector3 target = giantSwordTarget != null
                ? giantSwordTarget.position
                : new Vector3(from.x, ViewCenter.y - ViewHalfHeight * 0.55f, 0f);
            Vector3 start = from + Vector3.up * 1.5f;

            boss?.PlayAnimation(Swordmaster.Attack2State);
            boss?.Cue(SwordmasterCue.FinalCross, start);

            GameObject sword = CreateSwordObject("Giant Sword", start, giantSwordScale, Color.white);
            Collider2D swordCollider = sword.GetComponent<Collider2D>();
            if (swordCollider != null) swordCollider.enabled = false;
            Vector3 direction = (target - start).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
            sword.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            float duration = Vector3.Distance(start, target) / Mathf.Max(1f, giantSwordSpeed);
            yield return sword.transform.DOMove(target, duration).SetEase(Ease.InQuad).WaitForCompletion();

            if (boss != null)
            {
                boss.ShakeCamera(impactShake);
                boss.Cue(SwordmasterCue.SwordImpact, target);
                boss.AttackImpact(target);
            }
            sword.transform.DOShakePosition(0.35f, 0.25f, 30).SetTarget(sword.transform);
            Destroy(sword, 2.5f);
        }

        public void NotifyHazardHit(Vector3 position)
        {
            if (swordmaster != null && swordmaster.gameObject.activeInHierarchy) swordmaster.ShakeCamera(0.2f);
        }

        private void CacheBaseSpeeds()
        {
            if (scrollers == null) scrollers = new VerticalLoopScroller[0];
            foreach (VerticalLoopScroller scroller in scrollers)
            {
                if (scroller == null || baseSpeeds.ContainsKey(scroller)) continue;
                baseSpeeds[scroller] = scroller.Speed;
            }
        }

        private void Track(GameObject spawnedObject)
        {
            spawned.RemoveAll(item => item == null);
            spawned.Add(spawnedObject);
        }

        private void ClearSpawned()
        {
            foreach (GameObject item in spawned)
                if (item != null) Destroy(item);
            spawned.Clear();
        }

        private void StopRoutine(ref Coroutine routine)
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
            if (scrollers == null) return;
            foreach (VerticalLoopScroller scroller in scrollers)
                if (scroller != null) DOTween.Kill(scroller);
        }

        private void OnDestroy()
        {
            if (lineMaterial != null) Destroy(lineMaterial);
        }
    }
}
