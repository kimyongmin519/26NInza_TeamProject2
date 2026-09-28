using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Swordmaster;
using UnityEngine;
using UnityEngine.Events;

namespace Member.ODK.Scripts.Environment
{
    public class TowerClimbSequence : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float climbDuration = 40f;

        [Header("Background")]
        [SerializeField] private VerticalLoopScroller[] scrollers;
        [SerializeField] private float climbSpeedScale = 4f;
        [SerializeField] private float fallSpeedScale = -9f;
        [SerializeField] private float fallRampDuration = 0.7f;

        [Header("World")]
        [SerializeField] private float worldFallSpeed = 16f;
        [SerializeField] private float shaftHalfWidth = 0f;
        [SerializeField] private float shaftPadding = 1.2f;
        [SerializeField] private LayerMask playerLayer = 1 << 6;
        [SerializeField] private float hazardDamage = 1f;

        [Header("Falling Swords")]
        [SerializeField] private Sprite swordSprite;
        [SerializeField] private Color swordTint = new Color(0.9f, 0.7f, 1f, 1f);
        [SerializeField] private float swordScale = 0.07f;
        [SerializeField] private string swordSortingLayer = "Prop";
        [SerializeField] private int swordSortingOrder = 5;
        [SerializeField] private Vector2 volleyInterval = new Vector2(0.45f, 0.85f);
        [SerializeField] private Vector2Int rainCount = new Vector2Int(4, 7);
        [SerializeField] private float rainStagger = 0.05f;
        [SerializeField] private float wallSpacing = 1.3f;
        [SerializeField] private float wallGap = 2.6f;
        [SerializeField] private int aimedCount = 3;
        [SerializeField] private float aimedSpread = 1.1f;
        [SerializeField] private float swordWarning = 0.45f;
        [SerializeField] private float swordDropSpeed = 36f;

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
        private Coroutine swordRoutine;
        private bool finished;
        private Material lineMaterial;
        private Transform cachedPlayer;

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

        private IEnumerator SwordLoop()
        {
            yield return new WaitForSeconds(0.6f);
            int last = -1;
            while (IsClimbing)
            {
                int pattern;
                do pattern = Random.Range(0, 3);
                while (pattern == last && Random.value < 0.7f);
                last = pattern;

                if (pattern == 0) yield return RainVolley();
                else if (pattern == 1) WallVolley();
                else yield return AimedVolley();

                yield return new WaitForSeconds(Random.Range(volleyInterval.x, volleyInterval.y));
            }
        }

        private IEnumerator RainVolley()
        {
            int count = Random.Range(Mathf.Max(1, rainCount.x), Mathf.Max(rainCount.x, rainCount.y) + 1);
            float halfWidth = ShaftHalfWidth;
            for (int i = 0; i < count && IsClimbing; i++)
            {
                SpawnFallingSword(ViewCenter.x + Random.Range(-halfWidth, halfWidth));
                if (rainStagger > 0f) yield return new WaitForSeconds(rainStagger);
            }
        }

        private void WallVolley()
        {
            float halfWidth = ShaftHalfWidth;
            float left = ViewCenter.x - halfWidth;
            float right = ViewCenter.x + halfWidth;
            float gapCenter = Random.Range(left + wallGap, right - wallGap);
            for (float x = left; x <= right + 0.01f; x += Mathf.Max(0.4f, wallSpacing))
            {
                if (Mathf.Abs(x - gapCenter) < wallGap * 0.5f) continue;
                SpawnFallingSword(x);
            }
        }

        private IEnumerator AimedVolley()
        {
            Transform player = FindPlayer();
            float halfWidth = ShaftHalfWidth;
            for (int i = 0; i < aimedCount && IsClimbing; i++)
            {
                float targetX = player != null ? player.position.x : ViewCenter.x;
                float x = Mathf.Clamp(targetX + Random.Range(-aimedSpread, aimedSpread), ViewCenter.x - halfWidth, ViewCenter.x + halfWidth);
                SpawnFallingSword(x);
                yield return new WaitForSeconds(0.14f);
            }
        }

        private Transform FindPlayer()
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = PhasedBossController.FindPlayerTransform();
            }
            return cachedPlayer;
        }

        private void SpawnFallingSword(float x)
        {
            if (swordSprite == null) return;
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
            renderer.sortingLayerName = swordSortingLayer;
            renderer.sortingOrder = swordSortingOrder + 1;

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
            line.sortingLayerName = swordSortingLayer;
            line.sortingOrder = swordSortingOrder - 1;
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

            Vector3 appear = swordmasterAppearPoint != null
                ? swordmasterAppearPoint.position
                : ViewCenter + Vector3.up * ViewHalfHeight * 0.45f;
            appear.z = 0f;

            Swordmaster boss = SpawnSwordmaster(appear);
            BossIntroTimeline intro = boss != null ? boss.GetComponentInChildren<BossIntroTimeline>(true) : null;
            if (intro != null)
            {
                intro.AutoBeginBattle = false;
                float timeout = 12f;
                while (!intro.HasFinished && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
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

        private Swordmaster SpawnSwordmaster(Vector3 position)
        {
            if (swordmaster == null && swordmasterPrefab != null)
                swordmaster = Instantiate(swordmasterPrefab, position, Quaternion.identity);
            if (swordmaster == null) return null;
            swordmaster.SetAutoStart(false);
            Transform player = FindPlayer();
            if (player != null) swordmaster.SetTarget(player);
            swordmaster.transform.position = position;
            swordmaster.gameObject.SetActive(true);
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
