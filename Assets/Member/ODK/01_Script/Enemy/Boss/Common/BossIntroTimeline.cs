using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using KimLIb.SoundSystem;
using Member.KYM.Scripts.UI;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    public enum BossIntroAction
    {
        Letterbox,
        LetterboxHide,
        CameraFocus,
        CameraRelease,
        MoveFromOffset,
        FadeIn,
        ScalePunch,
        Shake,
        Title,
        Animation,
        Sound,
        Event
    }

    [Serializable]
    public class BossIntroStep
    {
        public string label;
        public BossIntroAction action;
        [Min(0f)] public float time;
        [Min(0f)] public float duration = 0.5f;
        public float value = 1f;
        public Vector2 vector;
        public string text;
        public string subText;
        public Ease ease = Ease.OutCubic;
        public SoundClipSO sound;
        public UnityEvent onPlay;

        public static BossIntroStep Create(BossIntroAction action, float time, float duration = 0.5f)
        {
            return new BossIntroStep { label = action.ToString(), action = action, time = time, duration = duration };
        }
    }

    [DisallowMultipleComponent]
    public class BossIntroTimeline : MonoBehaviour
    {
        private enum Preset
        {
            Auto,
            Moon,
            LostSoul,
            Volcanus,
            Swordmaster,
            Custom
        }

        [Header("Target")]
        [SerializeField] private PhasedBossController boss;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private float startDelay = 0.2f;
        [SerializeField] private Preset preset = Preset.Auto;

        [Header("Title")]
        [SerializeField] private string bossTitle;
        [SerializeField] private string bossSubtitle;
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color subtitleColor = new Color(1f, 1f, 1f, 0.7f);
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private string fallbackFontKeyword = "Hangul";

        [Header("Camera")]
        [SerializeField] private Vector2 focusOffset = new Vector2(0f, 1f);
        [SerializeField] private int focusPriority = 500;

        [Header("Timeline")]
        [SerializeField] private List<BossIntroStep> steps = new List<BossIntroStep>();
        [SerializeField] private float endPadding = 0.15f;

        [Header("Events")]
        [SerializeField] private UnityEvent onIntroStarted;
        [SerializeField] private UnityEvent onIntroFinished;

        public bool IsPlaying { get; private set; }
        public bool AutoBeginBattle { get; set; } = true;
        public bool HasFinished { get; private set; }
        public event Action Started;
        public event Action Finished;

        private Vector3 finalPosition;
        private Vector3 finalScale;
        private CinemachineCamera focusCamera;
        private Canvas overlayCanvas;
        private CinematicViewUI cinematicView;
        private RectTransform topBar;
        private RectTransform bottomBar;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subtitleText;
        private CanvasGroup titleGroup;
        private readonly List<(SpriteRenderer renderer, Color color)> fadeTargets = new List<(SpriteRenderer, Color)>();
        private readonly List<Coroutine> running = new List<Coroutine>();

        private void Awake()
        {
            if (boss == null) boss = GetComponentInParent<PhasedBossController>();
            if (boss == null) boss = GetComponentInChildren<PhasedBossController>();
            if (boss != null && playOnStart) boss.SetAutoStart(false);
            if (visualRoot == null && boss != null) visualRoot = boss.transform;
        }

        private void Start()
        {
            if (playOnStart) StartCoroutine(PlayAfterDelay());
        }

        private IEnumerator PlayAfterDelay()
        {
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
            Play();
        }

        [ContextMenu("Play Intro")]
        public void Play()
        {
            if (IsPlaying || boss == null) return;
            if (steps == null || steps.Count == 0 || preset != Preset.Custom) steps = BuildPreset(ResolvePreset());
            StartCoroutine(PlayRoutine());
        }

        [ContextMenu("Skip Intro")]
        public void Skip()
        {
            if (!IsPlaying) return;
            StopAllCoroutines();
            foreach (Coroutine routine in running)
                if (routine != null) StopCoroutine(routine);
            running.Clear();
            Finish();
        }

        private IEnumerator PlayRoutine()
        {
            IsPlaying = true;
            finalPosition = boss.transform.position;
            finalScale = visualRoot.localScale;
            BossBgmPlayback bgmPlayback =
                GetComponentInChildren<BossBgmPlayback>(true);
            if (bgmPlayback == null)
                bgmPlayback = GetComponentInParent<BossBgmPlayback>();
            bgmPlayback?.Play();
            Started?.Invoke();
            onIntroStarted?.Invoke();
            ApplyInitialStates();

            float end = 0f;
            foreach (BossIntroStep step in steps)
            {
                if (step == null) continue;
                end = Mathf.Max(end, step.time + step.duration);
                running.Add(StartCoroutine(RunStep(step)));
            }
            yield return new WaitForSeconds(end + endPadding);
            running.Clear();
            Finish();
        }

        private void Finish()
        {
            DOTween.Kill(this);
            boss.transform.position = finalPosition;
            visualRoot.localScale = finalScale;
            RestoreFade();
            ReleaseCamera();
            SetLetterbox(false, 0.25f);
            if (titleGroup != null) titleGroup.DOFade(0f, 0.25f).SetTarget(this);
            IsPlaying = false;
            HasFinished = true;
            onIntroFinished?.Invoke();
            Finished?.Invoke();
            if (AutoBeginBattle) boss.BeginBattle();
            if (overlayCanvas != null) Destroy(overlayCanvas.gameObject, 0.6f);
            overlayCanvas = null;
        }

        private void ApplyInitialStates()
        {
            foreach (BossIntroStep step in steps)
            {
                if (step == null) continue;
                if (step.action == BossIntroAction.MoveFromOffset)
                    boss.transform.position = finalPosition + (Vector3)step.vector;
                else if (step.action == BossIntroAction.FadeIn)
                    PrepareFade();
            }
        }

        private IEnumerator RunStep(BossIntroStep step)
        {
            if (step.time > 0f) yield return new WaitForSeconds(step.time);
            step.onPlay?.Invoke();
            switch (step.action)
            {
                case BossIntroAction.Letterbox:
                    SetLetterbox(true, step.duration);
                    break;
                case BossIntroAction.LetterboxHide:
                    SetLetterbox(false, step.duration);
                    break;
                case BossIntroAction.CameraFocus:
                    FocusCamera(step.value, (Vector2)step.vector + focusOffset);
                    break;
                case BossIntroAction.CameraRelease:
                    ReleaseCamera();
                    break;
                case BossIntroAction.MoveFromOffset:
                    yield return boss.transform.DOMove(finalPosition, Mathf.Max(0.01f, step.duration))
                        .SetEase(step.ease).SetTarget(this).WaitForCompletion();
                    break;
                case BossIntroAction.FadeIn:
                    FadeIn(step.duration, step.ease);
                    break;
                case BossIntroAction.ScalePunch:
                    visualRoot.DOPunchScale(finalScale * step.value, Mathf.Max(0.05f, step.duration), 6, 0.6f).SetTarget(this);
                    break;
                case BossIntroAction.Shake:
                    boss.ShakeCamera(step.value);
                    break;
                case BossIntroAction.Title:
                    ShowTitle(
                        string.IsNullOrEmpty(step.text) ? DefaultTitle() : step.text,
                        string.IsNullOrEmpty(step.subText) ? DefaultSubtitle() : step.subText,
                        step.duration);
                    break;
                case BossIntroAction.Animation:
                    PlayBossAnimation(step.text);
                    break;
                case BossIntroAction.Sound:
                    ODKSoundPlayback.Play(step.sound, boss.transform.position);
                    break;
            }
        }

        private Preset ResolvePreset()
        {
            if (preset != Preset.Auto && preset != Preset.Custom) return preset;
            string typeName = boss.GetType().Name;
            if (typeName.Contains("Moon")) return Preset.Moon;
            if (typeName.Contains("LostSoul")) return Preset.LostSoul;
            if (typeName.Contains("Volcanus")) return Preset.Volcanus;
            if (typeName.Contains("Swordmaster")) return Preset.Swordmaster;
            return Preset.Moon;
        }

        private string DefaultTitle()
        {
            if (!string.IsNullOrEmpty(bossTitle)) return bossTitle;
            switch (ResolvePreset())
            {
                case Preset.Moon: return "달";
                case Preset.LostSoul: return "어둠의 사신";
                case Preset.Volcanus: return "VOLCANUS";
                case Preset.Swordmaster: return "소드 마스터";
                default: return boss.name.ToUpperInvariant();
            }
        }

        private string DefaultSubtitle()
        {
            if (!string.IsNullOrEmpty(bossSubtitle)) return bossSubtitle;
            switch (ResolvePreset())
            {
                case Preset.Moon: return "추락한 위성";
                case Preset.LostSoul: return "잊힌 자들의 망령";
                case Preset.Volcanus: return "Molten Colossus";
                case Preset.Swordmaster: return "끝없는 탑의 검객";
                default: return string.Empty;
            }
        }

        private List<BossIntroStep> BuildPreset(Preset target)
        {
            List<BossIntroStep> list = new List<BossIntroStep>();
            BossIntroStep Add(BossIntroAction action, float time, float duration = 0.5f)
            {
                BossIntroStep step = BossIntroStep.Create(action, time, duration);
                list.Add(step);
                return step;
            }

            switch (target)
            {
                case Preset.LostSoul:
                    Add(BossIntroAction.Letterbox, 0f, 0.35f);
                    Add(BossIntroAction.CameraFocus, 0f, 0f).value = 4.5f;
                    Add(BossIntroAction.FadeIn, 0.2f, 1.5f).ease = Ease.InQuad;
                    Add(BossIntroAction.Animation, 1.7f, 0f).text = "attack";
                    Add(BossIntroAction.Shake, 1.75f, 0f).value = 0.9f;
                    Add(BossIntroAction.ScalePunch, 1.75f, 0.35f).value = 0.12f;
                    Add(BossIntroAction.Title, 1.9f, 1.9f);
                    Add(BossIntroAction.CameraRelease, 3.9f, 0f);
                    Add(BossIntroAction.LetterboxHide, 3.9f, 0.35f);
                    break;
                case Preset.Swordmaster:
                    Add(BossIntroAction.Letterbox, 0f, 0.3f);
                    Add(BossIntroAction.CameraFocus, 0f, 0f).value = 5f;
                    BossIntroStep drop = Add(BossIntroAction.MoveFromOffset, 0.1f, 0.9f);
                    drop.vector = new Vector2(0f, 10f);
                    drop.ease = Ease.OutCubic;
                    Add(BossIntroAction.Animation, 0.1f, 0f).text = "Fall";
                    Add(BossIntroAction.Animation, 1.05f, 0f).text = "Attack1";
                    Add(BossIntroAction.Shake, 1.1f, 0f).value = 1.1f;
                    Add(BossIntroAction.ScalePunch, 1.1f, 0.3f).value = 0.12f;
                    Add(BossIntroAction.Title, 1.3f, 1.7f);
                    Add(BossIntroAction.CameraRelease, 3.1f, 0f);
                    Add(BossIntroAction.LetterboxHide, 3.1f, 0.3f);
                    break;
                case Preset.Volcanus:
                    Add(BossIntroAction.Letterbox, 0f, 0.4f);
                    Add(BossIntroAction.CameraFocus, 0f, 0f).value = 8f;
                    BossIntroStep rise = Add(BossIntroAction.MoveFromOffset, 0.1f, 1.9f);
                    rise.vector = new Vector2(0f, -12f);
                    rise.ease = Ease.OutQuad;
                    Add(BossIntroAction.Shake, 0.2f, 0f).value = 0.5f;
                    Add(BossIntroAction.Shake, 0.7f, 0f).value = 0.6f;
                    Add(BossIntroAction.Shake, 1.2f, 0f).value = 0.7f;
                    Add(BossIntroAction.Shake, 2.0f, 0f).value = 2.2f;
                    Add(BossIntroAction.ScalePunch, 2.0f, 0.45f).value = 0.08f;
                    Add(BossIntroAction.Title, 2.2f, 2f);
                    Add(BossIntroAction.CameraRelease, 4.3f, 0f);
                    Add(BossIntroAction.LetterboxHide, 4.3f, 0.4f);
                    break;
                default:
                    Add(BossIntroAction.Letterbox, 0f, 0.35f);
                    Add(BossIntroAction.CameraFocus, 0f, 0f).value = 5.5f;
                    BossIntroStep fall = Add(BossIntroAction.MoveFromOffset, 0.15f, 1.3f);
                    fall.vector = new Vector2(0f, 14f);
                    fall.ease = Ease.InQuad;
                    Add(BossIntroAction.Shake, 1.45f, 0f).value = 1.8f;
                    Add(BossIntroAction.ScalePunch, 1.45f, 0.4f).value = 0.2f;
                    Add(BossIntroAction.Title, 1.7f, 1.9f);
                    Add(BossIntroAction.CameraRelease, 3.7f, 0f);
                    Add(BossIntroAction.LetterboxHide, 3.7f, 0.35f);
                    break;
            }
            return list;
        }

        private void FocusCamera(float orthographicSize, Vector2 offset)
        {
            if (focusCamera == null)
            {
                CinemachineCamera source = FindFirstObjectByType<CinemachineCamera>();
                GameObject cameraObject = new GameObject(name + " Intro Camera");
                focusCamera = cameraObject.AddComponent<CinemachineCamera>();
                if (source != null) focusCamera.Lens = source.Lens;
            }
            float z = Camera.main != null ? Camera.main.transform.position.z : -10f;
            focusCamera.transform.position = new Vector3(finalPosition.x + offset.x, finalPosition.y + offset.y, z);
            if (orthographicSize > 0f)
            {
                LensSettings lens = focusCamera.Lens;
                lens.OrthographicSize = orthographicSize;
                focusCamera.Lens = lens;
            }
            focusCamera.Priority = focusPriority;
            focusCamera.gameObject.SetActive(true);
        }

        private void ReleaseCamera()
        {
            if (focusCamera == null) return;
            Destroy(focusCamera.gameObject);
            focusCamera = null;
        }

        private void PrepareFade()
        {
            if (fadeTargets.Count > 0) return;
            foreach (SpriteRenderer renderer in visualRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                fadeTargets.Add((renderer, renderer.color));
                Color hidden = renderer.color;
                hidden.a = 0f;
                renderer.color = hidden;
            }
        }

        private void FadeIn(float duration, Ease ease)
        {
            PrepareFade();
            foreach ((SpriteRenderer renderer, Color color) in fadeTargets)
            {
                if (renderer == null) continue;
                renderer.DOColor(color, Mathf.Max(0.01f, duration)).SetEase(ease).SetTarget(this);
            }
        }

        private void RestoreFade()
        {
            foreach ((SpriteRenderer renderer, Color color) in fadeTargets)
                if (renderer != null) renderer.color = color;
            fadeTargets.Clear();
        }

        private void PlayBossAnimation(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            foreach (MethodInfo method in boss.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (method.Name != "PlayAnimation") continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(string)) continue;
                object[] arguments = new object[parameters.Length];
                arguments[0] = state;
                bool usable = true;
                for (int i = 1; i < parameters.Length; i++)
                {
                    if (!parameters[i].HasDefaultValue)
                    {
                        usable = false;
                        break;
                    }
                    arguments[i] = parameters[i].DefaultValue;
                }
                if (!usable) continue;
                method.Invoke(boss, arguments);
                return;
            }
        }

        private void EnsureOverlay()
        {
            if (overlayCanvas != null) return;
            GameObject canvasObject = new GameObject(name + " Intro Overlay");
            overlayCanvas = canvasObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 900;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            topBar = CreateBar(canvasObject.transform, "Top Bar", new Vector2(0f, 1f));
            bottomBar = CreateBar(canvasObject.transform, "Bottom Bar", new Vector2(0f, 0f));

            GameObject titleRoot = new GameObject("Title");
            titleRoot.transform.SetParent(canvasObject.transform, false);
            RectTransform titleRect = titleRoot.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(1f, 0f);
            titleRect.pivot = new Vector2(0.5f, 0f);
            titleRect.anchoredPosition = new Vector2(0f, 190f);
            titleRect.sizeDelta = new Vector2(0f, 200f);
            titleGroup = titleRoot.AddComponent<CanvasGroup>();
            titleGroup.alpha = 0f;

            TMP_FontAsset font = ResolveFont();
            titleText = CreateText(titleRoot.transform, "Name", 96f, titleColor, new Vector2(0f, 60f));
            if (font != null) titleText.font = font;
            titleText.fontStyle = FontStyles.Bold;
            titleText.characterSpacing = 18f;
            subtitleText = CreateText(titleRoot.transform, "Subtitle", 40f, subtitleColor, new Vector2(0f, -20f));
            if (font != null) subtitleText.font = font;
            subtitleText.fontStyle = FontStyles.Italic;
        }

        private static RectTransform CreateBar(Transform parent, string barName, Vector2 anchor)
        {
            GameObject bar = new GameObject(barName);
            bar.transform.SetParent(parent, false);
            RectTransform rect = bar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, anchor.y);
            rect.anchorMax = new Vector2(1f, anchor.y);
            rect.pivot = new Vector2(0.5f, anchor.y);
            rect.sizeDelta = new Vector2(0f, 0f);
            Image image = bar.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            return rect;
        }

        private TMP_FontAsset ResolveFont()
        {
            if (titleFont != null) return titleFont;
            if (string.IsNullOrEmpty(fallbackFontKeyword)) return null;
            foreach (TMP_FontAsset candidate in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (candidate != null && candidate.name.IndexOf(fallbackFontKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    titleFont = candidate;
                    return candidate;
                }
            }
            return null;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string textName, float size, Color color, Vector2 position)
        {
            GameObject textObject = new GameObject(textName);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(0f, size * 1.4f);
            rect.anchoredPosition = position;
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private void SetLetterbox(bool show, float duration)
        {
            if (cinematicView == null)
                cinematicView = FindFirstObjectByType<CinematicViewUI>(FindObjectsInactive.Include);
            if (cinematicView != null)
            {
                if (!cinematicView.gameObject.activeSelf) cinematicView.gameObject.SetActive(true);
                if (show) cinematicView.Show();
                else cinematicView.Hide();
                return;
            }
            if (!show && overlayCanvas == null) return;
            EnsureOverlay();
            float height = show ? 130f : 0f;
            topBar.DOSizeDelta(new Vector2(0f, height), Mathf.Max(0.01f, duration)).SetEase(Ease.OutCubic).SetTarget(this);
            bottomBar.DOSizeDelta(new Vector2(0f, height), Mathf.Max(0.01f, duration)).SetEase(Ease.OutCubic).SetTarget(this);
        }

        private void ShowTitle(string title, string subtitle, float duration)
        {
            EnsureOverlay();
            titleText.text = title;
            subtitleText.text = subtitle;
            titleGroup.alpha = 0f;
            RectTransform nameRect = titleText.rectTransform;
            Vector2 namePosition = new Vector2(0f, 60f);
            nameRect.anchoredPosition = namePosition + Vector2.left * 60f;
            titleText.characterSpacing = 40f;

            Sequence sequence = DOTween.Sequence().SetTarget(this);
            sequence.Append(titleGroup.DOFade(1f, 0.3f));
            sequence.Join(nameRect.DOAnchorPos(namePosition, 0.5f).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => titleText.characterSpacing, v => titleText.characterSpacing = v, 18f, 0.6f).SetEase(Ease.OutCubic));
            sequence.AppendInterval(Mathf.Max(0f, duration - 0.6f));
            sequence.Append(titleGroup.DOFade(0f, 0.3f));
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            if (focusCamera != null) Destroy(focusCamera.gameObject);
            if (overlayCanvas != null) Destroy(overlayCanvas.gameObject);
        }
    }
}
