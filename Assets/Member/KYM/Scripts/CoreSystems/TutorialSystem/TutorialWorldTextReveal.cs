using System.Collections.Generic;
using Member.KYM.Scripts.Players;
using TMPro;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.TutorialSystem
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class TutorialWorldTextReveal : MonoBehaviour
    {
        [Header("월드 텍스트 (TextMeshPro 3D)")]
        [SerializeField] private TextMeshPro worldText;
        [SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;

        [Header("페이드 시간")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.4f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.25f;

        private readonly HashSet<Collider2D> _playerColliders = new();
        private float _currentAlpha;

        private void Awake()
        {
            if (worldText == null)
                worldText = GetComponentInChildren<TextMeshPro>(true);

            GetComponent<BoxCollider2D>().isTrigger = true;
            SetAlpha(0f);
        }

        private void Update()
        {
            if (worldText == null)
                return;

            bool shouldShow = _playerColliders.Count > 0;
            float targetAlpha = shouldShow ? visibleAlpha : 0f;
            float duration = shouldShow ? fadeInDuration : fadeOutDuration;
            float step = duration <= 0f ? visibleAlpha : visibleAlpha * Time.deltaTime / duration;
            SetAlpha(Mathf.MoveTowards(_currentAlpha, targetAlpha, step));
        }

        private void OnTriggerEnter2D(Collider2D other) => TryRegisterPlayer(other);

        private void OnTriggerStay2D(Collider2D other) => TryRegisterPlayer(other);

        private void OnTriggerExit2D(Collider2D other)
        {
            _playerColliders.Remove(other);
        }

        private void TryRegisterPlayer(Collider2D other)
        {
            if (other != null && !_playerColliders.Contains(other) &&
                other.GetComponentInParent<PlayerController>() != null)
            {
                _playerColliders.Add(other);
            }
        }

        private void SetAlpha(float alpha)
        {
            _currentAlpha = alpha;
            if (worldText == null)
                return;

            Color color = worldText.color;
            color.a = alpha;
            worldText.color = color;
        }

        private void OnDisable()
        {
            _playerColliders.Clear();
            SetAlpha(0f);
        }

        private void OnValidate()
        {
            BoxCollider2D area = GetComponent<BoxCollider2D>();
            if (area != null)
                area.isTrigger = true;

            visibleAlpha = Mathf.Clamp01(visibleAlpha);
            fadeInDuration = Mathf.Max(0f, fadeInDuration);
            fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        }
    }
}
