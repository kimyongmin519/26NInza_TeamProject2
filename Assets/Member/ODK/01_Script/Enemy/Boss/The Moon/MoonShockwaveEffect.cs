using DG.Tweening;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public class MoonShockwaveEffect : MonoBehaviour
    {
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private float duration = 0.55f;
        [SerializeField] private Ease expansionEase = Ease.OutCubic;

        private Material runtimeMaterial;
        private Sequence sequence;

        public static MoonShockwaveEffect Spawn(
            Vector3 position,
            float radius,
            Color color,
            Material materialTemplate = null,
            float lifeTime = 0.55f)
        {
            GameObject effectObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            effectObject.name = "Moon Shockwave";
            effectObject.transform.position = position;
            Collider collider = effectObject.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            MoonShockwaveEffect effect = effectObject.AddComponent<MoonShockwaveEffect>();
            effect.meshRenderer = effectObject.GetComponent<MeshRenderer>();
            effect.duration = lifeTime;
            effect.Play(radius, color, materialTemplate);
            return effect;
        }

        public void Play(float radius, Color color, Material materialTemplate = null)
        {
            if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer == null) return;

            Shader shader = Shader.Find("ODK/MoonShockwave");
            if (materialTemplate != null)
                runtimeMaterial = new Material(materialTemplate);
            else if (Resources.Load<Material>("MoonShockwave") is Material resourceMaterial)
                runtimeMaterial = new Material(resourceMaterial);
            else if (shader != null)
                runtimeMaterial = new Material(shader);
            else
                runtimeMaterial = new Material(Shader.Find("Sprites/Default"));

            meshRenderer.material = runtimeMaterial;
            meshRenderer.sortingOrder = 80;
            runtimeMaterial.SetColor("_Color", color);
            runtimeMaterial.SetFloat("_Progress", 0f);

            float diameter = Mathf.Max(0.1f, radius * 2f);
            transform.localScale = Vector3.one * (diameter * 0.2f);

            sequence?.Kill();
            sequence = DOTween.Sequence().SetTarget(this);
            sequence.Join(transform.DOScale(Vector3.one * diameter, duration).SetEase(expansionEase));
            sequence.Join(DOTween.To(
                () => runtimeMaterial.GetFloat("_Progress"),
                value => runtimeMaterial.SetFloat("_Progress", value),
                1f,
                duration));
            sequence.OnComplete(() => Destroy(gameObject));
        }

        private void OnDestroy()
        {
            sequence?.Kill();
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }
}
