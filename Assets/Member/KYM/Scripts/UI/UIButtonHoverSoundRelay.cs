using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Member.KYM.Scripts.UI
{
    [DisallowMultipleComponent]
    public sealed class UIButtonHoverSoundRelay : MonoBehaviour, IPointerEnterHandler
    {
        private UIButtonSoundBinder _owner;
        private Button _button;

        public void Bind(UIButtonSoundBinder owner, Button button)
        {
            _owner = owner;
            _button = button;
        }

        public void Unbind(UIButtonSoundBinder owner)
        {
            if (_owner != owner)
                return;

            _owner = null;
            _button = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_owner != null && _button != null &&
                _button.IsActive() && _button.IsInteractable())
            {
                _owner.PlayHover();
            }
        }
    }
}
