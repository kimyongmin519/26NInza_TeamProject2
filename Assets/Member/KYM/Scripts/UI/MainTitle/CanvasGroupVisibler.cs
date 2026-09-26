using DG.Tweening;
using UnityEngine;

namespace Member.KYM.Scripts.UI.MainTitle
{
    public class CanvasGroupVisibler : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

        public void ShowCanvasGroup(float duration)
        {
            canvasGroup.DOFade(1, duration).SetEase(Ease.Linear);
        }
        
        public void HideCanvasGroup(float duration)
        {
            canvasGroup.DOFade(0, duration).SetEase(Ease.Linear);
        }
    }
}