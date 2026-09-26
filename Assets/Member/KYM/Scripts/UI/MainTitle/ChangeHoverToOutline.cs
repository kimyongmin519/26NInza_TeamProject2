using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using Image = UnityEngine.UI.Image;

namespace Member.KYM.Scripts.UI.MainTitle
{
    public class ChangeHoverToOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image outline;
        [SerializeField] private Color hoverOutlineColor;
        private Color _originColor;

        private void Awake()
        {
            if (outline != null)
                _originColor = outline.color;
        }


        public void OnPointerEnter(PointerEventData eventData)
        {
            outline.color = hoverOutlineColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            outline.color = _originColor;
        }
    }
}