using System.Collections.Generic;
using KimLIb.EventSystem;
using UnityEngine;
using YKJ_Script.Feedbacks;

namespace Member.YKJ.Bosses
{
    [DisallowMultipleComponent]
    public sealed class MimicParticleFeedback : MonoBehaviour
    {
        [SerializeField] private MimicBoss boss;
        [SerializeField] private BoxCollider2D bodyCollider;
        [SerializeField] private EventChannelSO feedbackChannel;
        [SerializeField] private FeedbackSO spitFeedback;
        [SerializeField] private FeedbackSO landingFeedback;
        [SerializeField] private ParticleSystem dustPrefab;
        [SerializeField] private Material sparkMaterial;
        [SerializeField] private Material cloudMaterial;
        [Header("Spit")]
        [SerializeField] private Vector3 spitOffset = new Vector3(0f, 0.1f, 0f);
        [SerializeField, Min(0.01f)] private float spitScale = 0.4f;
        [SerializeField] private Color spitColor = new Color(1f, 0.88f, 0.58f, 0.85f);
        [Header("Landing")]
        [SerializeField, Min(0.01f)] private float landingScale = 0.6f;
        [SerializeField] private Color landingColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

        private readonly List<ParticleSystem> _activeEffects = new List<ParticleSystem>();

        private void OnEnable()
        {
            if (feedbackChannel != null)
                feedbackChannel.AddListener<PlayFeedBack>(HandleFeedback);
        }

        private void OnDisable()
        {
            if (feedbackChannel != null)
                feedbackChannel.RemoveListener<PlayFeedBack>(HandleFeedback);
            foreach (ParticleSystem effect in _activeEffects)
                if (effect != null) Destroy(effect.gameObject);
            _activeEffects.Clear();
        }

        private void HandleFeedback(PlayFeedBack evt)
        {
            if (evt == null || boss == null || !boss.IsEncounterActive)
                return;
            if (spitFeedback != null && evt.FeedbackId == spitFeedback.FeedBackId)
                Spawn(boss.MouthPosition + spitOffset, spitScale, spitColor);
            else if (landingFeedback != null && evt.FeedbackId == landingFeedback.FeedBackId && bodyCollider != null)
            {
                // Use local collider geometry: the landing transform may not have synced with physics yet.
                Vector2 feet = bodyCollider.offset + Vector2.down * (bodyCollider.size.y * 0.5f) + Vector2.up * 0.05f;
                Vector2 side = Vector2.right * (bodyCollider.size.x * 0.4f);
                Spawn(bodyCollider.transform.TransformPoint(feet - side), landingScale, landingColor);
                Spawn(bodyCollider.transform.TransformPoint(feet + side), landingScale, landingColor);
            }
        }

        private void Spawn(Vector3 position, float scale, Color color)
        {
            if (dustPrefab == null || sparkMaterial == null || cloudMaterial == null)
                return;
            ParticleSystem effect = Instantiate(dustPrefab, position, dustPrefab.transform.rotation);
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.transform.localScale *= Mathf.Max(0.01f, scale);
            foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particles.main;
                main.loop = false;
                main.stopAction = ParticleSystemStopAction.None;
                main.startColor = color;
                var renderer = particles.GetComponent<ParticleSystemRenderer>();
                if (renderer == null) continue;
                renderer.sharedMaterial = particles == effect ? sparkMaterial : cloudMaterial;
                renderer.sortingLayerName = "Weapon";
                renderer.sortingOrder = 40;
            }
            effect.Play(true);
            _activeEffects.Add(effect);
        }

        private void Update()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                ParticleSystem effect = _activeEffects[i];
                if (effect != null && effect.IsAlive(true)) continue;
                if (effect != null) Destroy(effect.gameObject);
                _activeEffects.RemoveAt(i);
            }
        }
    }
}
