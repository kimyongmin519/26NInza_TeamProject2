using UnityEngine;

namespace Member.KYM.Scripts.EffectSystems
{
    public class VerticalBob : MonoBehaviour
    {
        [Header("상하 왕복")]
        [SerializeField, Min(0f)] private float height = 0.2f;
        [SerializeField, Min(0.01f)] private float cycleDuration = 2f;
        [SerializeField] private bool startDirIsDown = false;

        private Vector3 _startLocalPosition;
        private float _elapsed;

        private void OnEnable()
        {
            _startLocalPosition = transform.localPosition;
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float offsetY = Mathf.Sin(_elapsed * Mathf.PI * 2f / cycleDuration) * height;
            Vector2 dir = startDirIsDown ? Vector2.up : Vector2.down;
            transform.localPosition = _startLocalPosition + (Vector3)dir * offsetY;
        }

        private void OnDisable()
        {
            transform.localPosition = _startLocalPosition;
        }
    }
}
