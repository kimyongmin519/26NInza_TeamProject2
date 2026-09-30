using System;
using System.Collections.Generic;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;

namespace Member.ODK.Scripts.Environment
{
    public enum ODKAmbienceTheme
    {
        SoulCrypt,
        SkyTower
    }

    [DisallowMultipleComponent]
    public class ODKArenaAmbience : MonoBehaviour
    {
        private enum Area
        {
            Full,
            Bottom,
            Top,
            Left
        }

        private enum Look
        {
            Dot,
            Glow,
            Fog,
            Star,
            Diamond
        }

        private class Layer
        {
            public string Name;
            public Look Look;
            public bool Additive;
            public string SortingLayer;
            public int Order;
            public float Rate;
            public Vector2 Life;
            public Vector2 Size;
            public Color ColorA;
            public Color ColorB;
            public Vector2 VelocityX;
            public Vector2 VelocityY;
            public float Noise;
            public float NoiseFrequency = 0.3f;
            public float Spin;
            public Area Area;
            public int MaxParticles = 100;
            public bool Pop;
            public bool Twinkle;
            public bool Stretch;
            public float FadeIn = 0.2f;
            public float FadeOut = 0.7f;
        }

        [SerializeField] private ODKAmbienceTheme theme = ODKAmbienceTheme.SoulCrypt;
        [SerializeField] private Material alphaTemplate;
        [SerializeField] private Material additiveTemplate;
        [SerializeField] private string backSortingLayer = "Default";
        [SerializeField] private string frontSortingLayer = "Default";
        [SerializeField, Min(0f)] private float density = 1f;
        [SerializeField, Min(1f)] private float areaPadding = 1.3f;
        [SerializeField] private bool followCamera = true;
        [SerializeField] private bool waitForArenaAmbience = true;

        private static readonly Dictionary<Look, Texture2D> Textures = new Dictionary<Look, Texture2D>();

        private readonly List<ParticleSystem> systems = new List<ParticleSystem>();
        private readonly List<Layer> layers = new List<Layer>();
        private readonly List<Material> materials = new List<Material>();
        private Transform root;
        private Camera targetCamera;
        private Vector2 appliedHalfSize;
        private BossArenaAmbience arenaAmbience;
        private bool playing;

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            root = new GameObject("Generated Ambience").transform;
            root.SetParent(transform, false);
            targetCamera = Camera.main;
            SnapToCamera();
            layers.AddRange(theme == ODKAmbienceTheme.SkyTower ? SkyTowerLayers() : SoulCryptLayers());
            Vector2 half = GetHalfSize();
            foreach (Layer layer in layers)
                systems.Add(CreateSystem(layer, half, root, CreateMaterial));
            appliedHalfSize = half;
            if (waitForArenaAmbience) arenaAmbience = FindFirstObjectByType<BossArenaAmbience>(FindObjectsInactive.Include);
        }

        private void LateUpdate()
        {
            if (root == null) return;
            if (!playing && (arenaAmbience == null || arenaAmbience.IsActive))
            {
                playing = true;
                foreach (ParticleSystem system in systems)
                    if (system != null) system.Play(true);
            }
            if (targetCamera == null) targetCamera = Camera.main;
            SnapToCamera();
            Vector2 half = GetHalfSize();
            if ((half - appliedHalfSize).sqrMagnitude < 0.01f) return;
            appliedHalfSize = half;
            for (int i = 0; i < systems.Count && i < layers.Count; i++)
                if (systems[i] != null) ApplyShape(systems[i], layers[i], half);
        }

        public ODKAmbienceTheme Theme => theme;

        public List<ParticleSystem> BakeSystems(Transform parent, Vector2 halfSize, Func<string, bool, Texture2D, Material> materialFactory)
        {
            List<ParticleSystem> baked = new List<ParticleSystem>();
            IEnumerable<Layer> bakedLayers = theme == ODKAmbienceTheme.SkyTower ? SkyTowerLayers() : SoulCryptLayers();
            foreach (Layer layer in bakedLayers)
            {
                ParticleSystem system = CreateSystem(
                    layer,
                    halfSize,
                    parent,
                    source => materialFactory(theme + "_" + source.Name.Replace(" ", string.Empty), source.Additive, GetTexture(source.Look)));
                baked.Add(system);
            }
            return baked;
        }

        public static Material CreateBakeMaterial(bool additive) => CreateParticleMaterial(additive);

        private void SnapToCamera()
        {
            if (!followCamera || targetCamera == null || root == null) return;
            Vector3 cameraPosition = targetCamera.transform.position;
            root.position = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);
        }

        private Vector2 GetHalfSize()
        {
            if (targetCamera != null && targetCamera.orthographic)
            {
                float halfHeight = targetCamera.orthographicSize;
                return new Vector2(halfHeight * targetCamera.aspect, halfHeight);
            }
            return new Vector2(12f, 7f);
        }

        private IEnumerable<Layer> SoulCryptLayers()
        {
            string back = backSortingLayer;
            string front = frontSortingLayer;
            yield return new Layer
            {
                Name = "Crypt Fog", Look = Look.Fog, Additive = false, SortingLayer = back, Order = -8,
                Rate = 2.4f, Life = new Vector2(9f, 14f), Size = new Vector2(6f, 11f),
                ColorA = new Color(0.22f, 0.1f, 0.36f, 0.26f), ColorB = new Color(0.12f, 0.08f, 0.28f, 0.22f),
                VelocityX = new Vector2(-0.35f, 0.35f), VelocityY = new Vector2(-0.02f, 0.06f),
                Spin = 6f, Area = Area.Bottom, MaxParticles = 60, FadeIn = 0.3f, FadeOut = 0.65f
            };
            yield return new Layer
            {
                Name = "Spirit Orbs", Look = Look.Glow, Additive = true, SortingLayer = back, Order = -6,
                Rate = 0.6f, Life = new Vector2(6f, 9f), Size = new Vector2(0.9f, 1.7f),
                ColorA = new Color(0.62f, 0.32f, 1f, 0.32f), ColorB = new Color(0.35f, 0.8f, 1f, 0.26f),
                VelocityX = new Vector2(-0.15f, 0.15f), VelocityY = new Vector2(0.05f, 0.25f),
                Noise = 0.35f, NoiseFrequency = 0.2f, Area = Area.Full, MaxParticles = 12, Twinkle = true,
                FadeIn = 0.3f, FadeOut = 0.6f
            };
            yield return new Layer
            {
                Name = "Falling Ash", Look = Look.Dot, Additive = false, SortingLayer = back, Order = -5,
                Rate = 14f, Life = new Vector2(6f, 10f), Size = new Vector2(0.05f, 0.12f),
                ColorA = new Color(0.55f, 0.45f, 0.72f, 0.6f), ColorB = new Color(0.32f, 0.26f, 0.45f, 0.55f),
                VelocityX = new Vector2(-0.2f, 0.2f), VelocityY = new Vector2(-0.65f, -0.3f),
                Noise = 0.45f, NoiseFrequency = 0.45f, Area = Area.Full, MaxParticles = 200
            };
            yield return new Layer
            {
                Name = "Soul Wisps", Look = Look.Glow, Additive = true, SortingLayer = back, Order = -3,
                Rate = 9f, Life = new Vector2(4f, 7f), Size = new Vector2(0.18f, 0.45f),
                ColorA = new Color(0.55f, 0.95f, 1f, 0.85f), ColorB = new Color(0.82f, 0.55f, 1f, 0.85f),
                VelocityX = new Vector2(-0.2f, 0.2f), VelocityY = new Vector2(0.45f, 1.15f),
                Noise = 0.65f, NoiseFrequency = 0.35f, Area = Area.Full, MaxParticles = 150, Twinkle = true
            };
            yield return new Layer
            {
                Name = "Rising Embers", Look = Look.Dot, Additive = true, SortingLayer = back, Order = -2,
                Rate = 7f, Life = new Vector2(2.5f, 4f), Size = new Vector2(0.06f, 0.14f),
                ColorA = new Color(1f, 0.45f, 0.95f, 0.95f), ColorB = new Color(0.7f, 0.4f, 1f, 0.9f),
                VelocityX = new Vector2(-0.3f, 0.3f), VelocityY = new Vector2(1.4f, 2.8f),
                Noise = 0.55f, NoiseFrequency = 0.6f, Area = Area.Bottom, MaxParticles = 70
            };
            yield return new Layer
            {
                Name = "Rune Sparks", Look = Look.Star, Additive = true, SortingLayer = front, Order = 1,
                Rate = 3.5f, Life = new Vector2(0.6f, 1.2f), Size = new Vector2(0.25f, 0.55f),
                ColorA = new Color(1f, 0.92f, 1f, 0.9f), ColorB = new Color(0.78f, 0.62f, 1f, 0.9f),
                Spin = 90f, Area = Area.Full, MaxParticles = 20, Pop = true, FadeIn = 0.05f, FadeOut = 0.8f
            };
        }

        private IEnumerable<Layer> SkyTowerLayers()
        {
            string back = backSortingLayer;
            string front = frontSortingLayer;
            yield return new Layer
            {
                Name = "Cloud Mist", Look = Look.Fog, Additive = false, SortingLayer = back, Order = -8,
                Rate = 2f, Life = new Vector2(10f, 15f), Size = new Vector2(7f, 12f),
                ColorA = new Color(0.88f, 0.85f, 1f, 0.16f), ColorB = new Color(0.72f, 0.78f, 1f, 0.13f),
                VelocityX = new Vector2(0.35f, 0.9f), VelocityY = new Vector2(-0.05f, 0.05f),
                Spin = 4f, Area = Area.Full, MaxParticles = 50, FadeIn = 0.3f, FadeOut = 0.65f
            };
            yield return new Layer
            {
                Name = "Light Motes", Look = Look.Glow, Additive = true, SortingLayer = back, Order = -5,
                Rate = 6f, Life = new Vector2(5f, 8f), Size = new Vector2(0.12f, 0.32f),
                ColorA = new Color(1f, 0.95f, 0.85f, 0.7f), ColorB = new Color(0.85f, 0.8f, 1f, 0.7f),
                VelocityX = new Vector2(-0.15f, 0.25f), VelocityY = new Vector2(0.3f, 0.75f),
                Noise = 0.5f, NoiseFrequency = 0.3f, Area = Area.Full, MaxParticles = 70, Twinkle = true
            };
            yield return new Layer
            {
                Name = "Wind Streaks", Look = Look.Dot, Additive = true, SortingLayer = back, Order = -4,
                Rate = 6f, Life = new Vector2(0.9f, 1.5f), Size = new Vector2(0.06f, 0.11f),
                ColorA = new Color(0.92f, 0.92f, 1f, 0.45f), ColorB = new Color(0.8f, 0.72f, 1f, 0.4f),
                VelocityX = new Vector2(16f, 24f), VelocityY = new Vector2(-1.2f, 0.6f),
                Area = Area.Left, MaxParticles = 40, Stretch = true, FadeIn = 0.15f, FadeOut = 0.7f
            };
            yield return new Layer
            {
                Name = "Sword Shards", Look = Look.Diamond, Additive = true, SortingLayer = back, Order = -3,
                Rate = 4.5f, Life = new Vector2(4f, 6.5f), Size = new Vector2(0.22f, 0.48f),
                ColorA = new Color(0.86f, 0.7f, 1f, 0.85f), ColorB = new Color(0.68f, 0.86f, 1f, 0.8f),
                VelocityX = new Vector2(-0.25f, 0.25f), VelocityY = new Vector2(0.35f, 0.9f),
                Noise = 0.3f, NoiseFrequency = 0.25f, Spin = 70f, Area = Area.Full, MaxParticles = 45, Twinkle = true
            };
            yield return new Layer
            {
                Name = "Glitter", Look = Look.Star, Additive = true, SortingLayer = front, Order = 1,
                Rate = 10f, Life = new Vector2(0.4f, 0.9f), Size = new Vector2(0.14f, 0.34f),
                ColorA = new Color(1f, 1f, 1f, 0.95f), ColorB = new Color(0.86f, 0.78f, 1f, 0.9f),
                Spin = 120f, Area = Area.Full, MaxParticles = 30, Pop = true, FadeIn = 0.05f, FadeOut = 0.8f
            };
        }

        private ParticleSystem CreateSystem(Layer layer, Vector2 half, Transform parent, Func<Layer, Material> materialProvider)
        {
            GameObject systemObject = new GameObject(layer.Name);
            systemObject.transform.SetParent(parent, false);
            ParticleSystem system = systemObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = 6f;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(layer.Life.x, layer.Life.y);
            main.startSize = new ParticleSystem.MinMaxCurve(layer.Size.x, layer.Size.y);
            main.startSpeed = 0f;
            main.startColor = new ParticleSystem.MinMaxGradient(layer.ColorA, layer.ColorB);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(layer.MaxParticles * Mathf.Max(0.2f, density)));
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = layer.Rate * density;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            ApplyShape(system, layer, half);

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(layer.VelocityX.x, layer.VelocityX.y);
            velocity.y = new ParticleSystem.MinMaxCurve(layer.VelocityY.x, layer.VelocityY.y);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(FadeGradient(layer.FadeIn, layer.FadeOut));

            if (layer.Pop || layer.Twinkle)
            {
                ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, layer.Pop ? PopCurve() : TwinkleCurve());
            }

            if (layer.Spin > 0f)
            {
                ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-layer.Spin * Mathf.Deg2Rad, layer.Spin * Mathf.Deg2Rad);
            }

            if (layer.Noise > 0f)
            {
                ParticleSystem.NoiseModule noise = system.noise;
                noise.enabled = true;
                noise.strength = layer.Noise;
                noise.frequency = layer.NoiseFrequency;
                noise.scrollSpeed = 0.15f;
                noise.damping = true;
                noise.quality = ParticleSystemNoiseQuality.Medium;
            }

            ParticleSystemRenderer renderer = systemObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = materialProvider(layer);
            renderer.sortingLayerName = string.IsNullOrEmpty(layer.SortingLayer) ? "Default" : layer.SortingLayer;
            renderer.sortingOrder = layer.Order;
            renderer.maxParticleSize = 5f;
            renderer.minParticleSize = 0f;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (layer.Stretch)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 0.09f;
                renderer.lengthScale = 2f;
            }
            else
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            return system;
        }

        private void ApplyShape(ParticleSystem system, Layer layer, Vector2 half)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            float width = half.x * 2f * areaPadding;
            float height = half.y * 2f * areaPadding;
            switch (layer.Area)
            {
                case Area.Bottom:
                    shape.position = new Vector3(0f, -half.y * 0.55f, 0f);
                    shape.scale = new Vector3(width, half.y * 0.9f, 0.01f);
                    break;
                case Area.Top:
                    shape.position = new Vector3(0f, half.y + 0.6f, 0f);
                    shape.scale = new Vector3(width, 0.6f, 0.01f);
                    break;
                case Area.Left:
                    shape.position = new Vector3(-half.x * areaPadding - 1f, 0f, 0f);
                    shape.scale = new Vector3(1f, height, 0.01f);
                    break;
                default:
                    shape.position = Vector3.zero;
                    shape.scale = new Vector3(width, height, 0.01f);
                    break;
            }
        }

        private Material CreateMaterial(Layer layer)
        {
            Material template = layer.Additive
                ? additiveTemplate != null ? additiveTemplate : alphaTemplate
                : alphaTemplate != null ? alphaTemplate : additiveTemplate;
            Material material;
            if (template != null)
            {
                material = new Material(template);
            }
            else
            {
                material = CreateParticleMaterial(layer.Additive);
                if (material == null) return null;
            }
            material.name = "Ambience " + layer.Name;
            material.hideFlags = HideFlags.DontSave;
            Texture2D texture = GetTexture(layer.Look);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            materials.Add(material);
            return material;
        }

        private static Material CreateParticleMaterial(bool additive)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                Material material = new Material(shader);
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", additive ? 2f : 0f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty("_SrcBlendAlpha")) material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                if (material.HasProperty("_DstBlendAlpha")) material.SetFloat("_DstBlendAlpha", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                return material;
            }
            shader = Shader.Find("Sprites/Default");
            return shader != null ? new Material(shader) : null;
        }

        private static Gradient FadeGradient(float fadeIn, float fadeOut)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, Mathf.Clamp(fadeIn, 0.01f, 0.49f)),
                    new GradientAlphaKey(1f, Mathf.Clamp(fadeOut, 0.5f, 0.99f)),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static AnimationCurve PopCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, 1f),
                new Keyframe(0.45f, 0.55f),
                new Keyframe(1f, 0f));
        }

        private static AnimationCurve TwinkleCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.2f),
                new Keyframe(0.2f, 1f),
                new Keyframe(0.4f, 0.7f),
                new Keyframe(0.6f, 1f),
                new Keyframe(0.8f, 0.75f),
                new Keyframe(1f, 0.1f));
        }

        private static Texture2D GetTexture(Look look)
        {
            if (Textures.TryGetValue(look, out Texture2D cached) && cached != null) return cached;
            int size = look == Look.Fog || look == Look.Glow ? 128 : 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "ODK Ambience " + look,
                hideFlags = HideFlags.DontSave
            };
            Color[] pixels = new Color[size * size];
            float halfSize = (size - 1) * 0.5f;
            System.Random random = new System.Random(1234 + (int)look);
            Vector3[] blobs = new Vector3[7];
            for (int i = 0; i < blobs.Length; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float distance = (float)random.NextDouble() * 0.4f;
                blobs[i] = new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0.18f + (float)random.NextDouble() * 0.22f);
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - halfSize) / halfSize;
                    float dy = (y - halfSize) / halfSize;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha;
                    switch (look)
                    {
                        case Look.Glow:
                            alpha = Mathf.Exp(-radius * radius * 16f) + Mathf.Exp(-radius * radius * 3.2f) * 0.45f;
                            alpha *= Mathf.Clamp01(1f - radius);
                            break;
                        case Look.Fog:
                            alpha = 0f;
                            foreach (Vector3 blob in blobs)
                            {
                                float bx = dx - blob.x;
                                float by = dy - blob.y;
                                alpha += Mathf.Exp(-(bx * bx + by * by) / (blob.z * blob.z));
                            }
                            alpha = Mathf.Clamp01(alpha * 0.45f) * (1f - Mathf.SmoothStep(0.55f, 1f, radius));
                            break;
                        case Look.Star:
                            float horizontal = Mathf.Exp(-Mathf.Abs(dy) * 20f) * Mathf.Clamp01(1f - Mathf.Abs(dx));
                            float vertical = Mathf.Exp(-Mathf.Abs(dx) * 20f) * Mathf.Clamp01(1f - Mathf.Abs(dy));
                            float core = Mathf.Exp(-radius * radius * 26f);
                            alpha = Mathf.Clamp01(Mathf.Max(horizontal, vertical) + core);
                            break;
                        case Look.Diamond:
                            float edge = Mathf.Abs(dx) / 0.42f + Mathf.Abs(dy);
                            float body = Mathf.Clamp01((1f - edge) * 5f);
                            float shine = Mathf.Exp(-radius * radius * 18f) * 0.6f;
                            alpha = Mathf.Clamp01(body * 0.85f + shine);
                            break;
                        default:
                            alpha = Mathf.Clamp01(Mathf.Exp(-radius * radius * 6f) * (1f - radius) * 1.4f);
                            break;
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            Textures[look] = texture;
            return texture;
        }

        private void OnDestroy()
        {
            foreach (Material material in materials)
                if (material != null) Destroy(material);
            materials.Clear();
        }
    }
}
