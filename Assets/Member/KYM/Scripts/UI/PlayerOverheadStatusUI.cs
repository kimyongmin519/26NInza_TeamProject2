using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using TMPro;
using UnityEngine;

namespace Member.KYM.Scripts.UI
{
    public class PlayerOverheadStatusUI : MonoBehaviour
    {
        [Header("플레이어 상태")]
        [SerializeField] private EventChannelSO uiChannel;

        [Header("머리 위 월드 스페이스 UI")]
        [SerializeField] private Vector3 headOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private TMP_Text jumpText;
        [SerializeField] private TMP_Text dashReadyText;
        [SerializeField] private string previousJumpText = "남은 점프 횟수:";
        [SerializeField] private string dashReadyMessage = "대시 준비!";
        [SerializeField, Min(0.01f)] private float messageDuration = 0.8f;

        [Header("대시 충전 발광")]
        [SerializeField] private SpriteRenderer[] bodyRenderers;
        [SerializeField] private string emissionProperty = "_EmissionStrength";
        [SerializeField, Min(0f)] private float emissionBoost = 3f;
        [SerializeField, Min(0.01f)] private float riseDuration = 0.05f;
        [SerializeField, Min(0.01f)] private float returnDuration = 0.45f;

        private MaterialPropertyBlock _propertyBlock;
        private float[] _baseEmission;
        private bool[] _supportsEmission;
        private float[] _baseFill;
        private bool[] _supportsFill;
        private static readonly int EmissionFillId = Shader.PropertyToID("_EmissionFill");
        private int _emissionId;
        private bool _isFlashing;
        private float _flashTime;
        private float _messageTime;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _emissionId = Shader.PropertyToID(emissionProperty);
            int count = bodyRenderers != null ? bodyRenderers.Length : 0;
            _baseEmission = new float[count];
            _supportsEmission = new bool[count];
            _baseFill = new float[count];
            _supportsFill = new bool[count];
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer body = bodyRenderers[i];
                if (body == null || body.sharedMaterial == null ||
                    !body.sharedMaterial.HasProperty(_emissionId))
                    continue;

                _supportsEmission[i] = true;
                body.GetPropertyBlock(_propertyBlock);
                _baseEmission[i] = _propertyBlock.HasProperty(_emissionId)
                    ? _propertyBlock.GetFloat(_emissionId)
                    : body.sharedMaterial.GetFloat(_emissionId);
                _supportsFill[i] = body.sharedMaterial.HasProperty(EmissionFillId);
                if (_supportsFill[i])
                    _baseFill[i] = _propertyBlock.HasProperty(EmissionFillId)
                        ? _propertyBlock.GetFloat(EmissionFillId)
                        : body.sharedMaterial.GetFloat(EmissionFillId);
            }
        }

        private void OnEnable()
        {
            SetTextVisible(dashReadyText, false);
            SetTextVisible(jumpText, false);
            if (uiChannel == null) return;
            uiChannel.AddListener<PlayerUIStateEvent>(HandleState);
            uiChannel.AddListener<PlayerUIPositionEvent>(HandlePosition);
            uiChannel.RaiseEvent(new PlayerUIStateRequest());
        }

        private void Update()
        {
            if (_messageTime > 0f)
            {
                _messageTime -= Time.deltaTime;
                if (_messageTime <= 0f)
                    SetTextVisible(dashReadyText, false);
            }

            if (!_isFlashing)
                return;

            float previousTime = _flashTime;
            _flashTime += Time.deltaTime;
            float rise = Mathf.Max(0.01f, riseDuration);
            float duration = Mathf.Max(0.01f, returnDuration);
            float amount = _flashTime < rise
                ? Mathf.SmoothStep(0f, 1f, _flashTime / rise)
                : 1f - Mathf.SmoothStep(0f, 1f, (_flashTime - rise) / duration);
            // 프레임이 상승 종료를 지나쳐도 최대 채움 상태를 한 번은 표시한다.
            if (previousTime < rise && _flashTime >= rise)
            {
                amount = 1f;
                _flashTime = rise;
            }
            SetEmission(amount * emissionBoost, emissionBoost > 0f ? amount : 0f);
            if (_flashTime >= rise + duration)
            {
                _isFlashing = false;
                SetEmission(0f);
            }
        }

        private void HandlePosition(PlayerUIPositionEvent evt)
        {
            transform.position = evt.Position + headOffset;
        }

        private void HandleState(PlayerUIStateEvent evt)
        {
            if (evt.DashRecharged && !evt.IsDead)
                PlayRechargeFeedback();
            if (jumpText == null)
                return;

            bool show = !evt.IsDead && evt.RemainingJumps < evt.MaxJumps;
            SetTextVisible(jumpText, show);
            if (show)
                jumpText.SetText($"{previousJumpText} {evt.RemainingJumps}");
        }

        private void PlayRechargeFeedback()
        {
            if (dashReadyText != null)
                dashReadyText.text = dashReadyMessage;
            SetTextVisible(dashReadyText, true);
            _messageTime = Mathf.Max(0.01f, messageDuration);
            _flashTime = 0f;
            _isFlashing = true;
        }

        private void SetEmission(float boost, float fill = 0f)
        {
            for (int i = 0; i < _baseEmission.Length; i++)
            {
                SpriteRenderer body = bodyRenderers[i];
                if (!_supportsEmission[i] || body == null)
                    continue;

                // 다른 연출의 프로퍼티는 유지하고 이미션 강도만 변경한다.
                body.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(_emissionId, _baseEmission[i] + boost);
                if (_supportsFill[i])
                    _propertyBlock.SetFloat(EmissionFillId, Mathf.Lerp(_baseFill[i], 1f, fill));
                body.SetPropertyBlock(_propertyBlock);
            }
        }

        private static void SetTextVisible(TMP_Text text, bool visible)
        {
            if (text != null)
                text.enabled = visible;
        }

        private void OnDisable()
        {
            if (uiChannel != null)
            {
                uiChannel.RemoveListener<PlayerUIStateEvent>(HandleState);
                uiChannel.RemoveListener<PlayerUIPositionEvent>(HandlePosition);
            }
            _isFlashing = false;
            _messageTime = 0f;
            if (_baseEmission != null)
                SetEmission(0f);
            SetTextVisible(jumpText, false);
            SetTextVisible(dashReadyText, false);
        }
    }
}
