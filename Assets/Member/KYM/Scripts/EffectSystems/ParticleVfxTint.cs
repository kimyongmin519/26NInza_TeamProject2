using UnityEngine;

namespace Member.KYM.Scripts.EffectSystems
{
    public class ParticleVfxTint : MonoBehaviour, IVfxContextReceiver
    {
        [SerializeField] private ParticleSystem targetParticle;

        private Color _originalColor = Color.white;

        private void Awake()
        {
            if (targetParticle == null)
                targetParticle = GetComponent<ParticleSystem>();

            if (targetParticle != null)
                _originalColor = targetParticle.main.startColor.color;
        }

        public void ApplyContext(in VfxSpawnContext context)
        {
            SetColor(_originalColor * context.Tint);
        }

        public void ResetContext()
        {
            SetColor(_originalColor);
        }

        private void SetColor(Color color)
        {
            if (targetParticle == null)
                return;

            ParticleSystem.MainModule main = targetParticle.main;
            main.startColor = color;
        }
    }
}
