using System.Collections.Generic;
using KimLIb.EventSystem;
using KimLIb.SoundSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.UI
{
    [DisallowMultipleComponent]
    public sealed class UIButtonSoundBinder : MonoBehaviour
    {
        [Header("공통 UI 버튼 사운드")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundClipSO hoverSound;
        [SerializeField] private SoundClipSO clickSound;

        private readonly List<Button> _buttons = new();

        private void OnEnable()
        {
            RefreshButtons();
        }

        private void OnDisable()
        {
            foreach (Button button in _buttons)
            {
                if (button == null)
                    continue;

                button.onClick.RemoveListener(PlayClick);
                button.GetComponent<UIButtonHoverSoundRelay>()?.Unbind(this);
            }

            _buttons.Clear();
        }

        // 실행 중 생성한 버튼이 있다면 생성 후 호출한다.
        public void RefreshButtons()
        {
            for (int i = _buttons.Count - 1; i >= 0; i--)
            {
                Button button = _buttons[i];
                if (button != null && button.GetComponentInParent<UIButtonSoundBinder>(true) == this)
                    continue;

                if (button != null)
                {
                    button.onClick.RemoveListener(PlayClick);
                    button.GetComponent<UIButtonHoverSoundRelay>()?.Unbind(this);
                }

                _buttons.RemoveAt(i);
            }

            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponentInParent<UIButtonSoundBinder>(true) != this ||
                    _buttons.Contains(button))
                    continue;

                UIButtonHoverSoundRelay relay = button.GetComponent<UIButtonHoverSoundRelay>();
                if (relay == null)
                    relay = button.gameObject.AddComponent<UIButtonHoverSoundRelay>();

                relay.Bind(this, button);
                button.onClick.AddListener(PlayClick);
                _buttons.Add(button);
            }
        }

        internal void PlayHover()
        {
            if (isActiveAndEnabled)
                Play(hoverSound);
        }

        private void PlayClick()
        {
            if (isActiveAndEnabled)
                Play(clickSound);
        }

        private void Play(SoundClipSO clip)
        {
            if (soundChannel == null || clip == null || clip.audioClip == null)
                return;

            soundChannel.RaiseEvent(
                SoundEvent.PlaySoundEvent.InitData(Vector3.zero, clip));
        }
    }
}
