using UnityEngine;

namespace Member.KYM.Scripts.EffectSystems
{
    public class SpriteVfxTint : MonoBehaviour, IVfxContextReceiver
    {
        [SerializeField] private SpriteRenderer targetRenderer;

        private Color _originalColor = Color.white;

        private void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<SpriteRenderer>();

            if (targetRenderer != null)
                _originalColor = targetRenderer.color;
        }

        public void ApplyContext(in VfxSpawnContext context)
        {
            if (targetRenderer != null)
                targetRenderer.color = _originalColor * context.Tint;
        }

        public void ResetContext()
        {
            if (targetRenderer != null)
                targetRenderer.color = _originalColor;
        }
    }
}
