using System.Collections;
using DG.Tweening;
using Member.KYM.Scripts.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    [DisallowMultipleComponent]
    public sealed class BossBarHideGuard : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Vector2 hiddenPosition;
        private bool holding;
        private bool released;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            AttachAll();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => AttachAll();

        private static void AttachAll()
        {
            BossHealthBarUI[] bars = FindObjectsByType<BossHealthBarUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (BossHealthBarUI bar in bars)
            {
                if (bar == null) continue;
                UIShowFromTop showFromTop = bar.GetComponentInParent<UIShowFromTop>(true);
                if (showFromTop == null || showFromTop.GetComponent<BossBarHideGuard>() != null) continue;
                showFromTop.gameObject.AddComponent<BossBarHideGuard>();
            }
        }

        public static void ReleaseFor(Component child)
        {
            if (child == null) return;
            UIShowFromTop showFromTop = child.GetComponentInParent<UIShowFromTop>(true);
            if (showFromTop == null) return;
            BossBarHideGuard guard = showFromTop.GetComponent<BossBarHideGuard>();
            if (guard != null) guard.Release();
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private IEnumerator Start()
        {
            yield return null;
            if (released || rectTransform == null) yield break;
            hiddenPosition = rectTransform.anchoredPosition;
            holding = true;
            Canvas.willRenderCanvases += HoldHidden;
        }

        public void Release()
        {
            released = true;
            if (!holding) return;
            holding = false;
            Canvas.willRenderCanvases -= HoldHidden;
        }

        private void HoldHidden()
        {
            if (!holding || rectTransform == null) return;
            if (DOTween.IsTweening(rectTransform))
            {
                Release();
                return;
            }
            if (rectTransform.anchoredPosition != hiddenPosition)
                rectTransform.anchoredPosition = hiddenPosition;
        }

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= HoldHidden;
        }
    }
}
