#if UNITY_EDITOR
using System.Collections.Generic;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Member.ODK.Editor
{
    public static class BossArenaPolishBuilder
    {
        private const string LostSoulArenaPrefab =
            "Assets/Member/ODK/02_Prefab/Boss/Arenas/LostSoulArena.prefab";
        private const string SwordmasterArenaPrefab =
            "Assets/Member/ODK/02_Prefab/Boss/Arenas/SwordmasterArena.prefab";
        private const string LostSoulScene =
            "Assets/Member/ODK/08_Scene/ODK_ DarkReaper.unity";
        private const string SwordmasterScene =
            "Assets/Member/ODK/08_Scene/ODK_ Swordmaster.unity";

        private const string MagicLineMaterial =
            "Assets/Epic Toon FX/Materials/Misc/Magic/magic_line_ADD.mat";
        private const string MagicOrbMaterial =
            "Assets/Epic Toon FX/Materials/Misc/Magic/magic_orb2_ADD.mat";
        private const string SparkleMaterial =
            "Assets/Epic Toon FX/Materials/Basics/sparkle_ADD.mat";

        private readonly struct Theme
        {
            public readonly string bossType;
            public readonly Color primary;
            public readonly Color secondary;
            public readonly Color accent;

            public Theme(
                string bossType,
                Color primary,
                Color secondary,
                Color accent)
            {
                this.bossType = bossType;
                this.primary = primary;
                this.secondary = secondary;
                this.accent = accent;
            }
        }

        [MenuItem("Tools/ODK/Polish Dark Reaper And Swordmaster Arenas")]
        public static void Build()
        {
            Theme lostSoul = new Theme(
                "LostSoul",
                new Color(0.62f, 0.24f, 1f, 0.62f),
                new Color(0.2f, 0.72f, 1f, 0.48f),
                new Color(0.88f, 0.55f, 1f, 0.68f)
            );
            Theme swordmaster = new Theme(
                "Swordmaster",
                new Color(0.72f, 0.2f, 1f, 0.62f),
                new Color(1f, 0.18f, 0.82f, 0.46f),
                new Color(0.48f, 0.35f, 1f, 0.7f)
            );

            ConfigureArenaPrefab(LostSoulArenaPrefab, lostSoul);
            ConfigureArenaPrefab(SwordmasterArenaPrefab, swordmaster);
            ConfigureArenaScene(LostSoulScene, lostSoul);
            ConfigureArenaScene(SwordmasterScene, swordmaster);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dark Reaper and Swordmaster arena ambience configured.");
        }

        private static void ConfigureArenaPrefab(string path, Theme theme)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                BossArena arena = root.GetComponentInChildren<BossArena>(true);
                if (arena == null)
                {
                    Debug.LogWarning($"BossArena is missing from {path}.");
                    return;
                }

                BuildAmbience(arena.gameObject, theme);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureArenaScene(string path, Theme theme)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            try
            {
                BossArena arena = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    arena = root.GetComponentInChildren<BossArena>(true);
                    if (arena != null)
                        break;
                }

                if (arena == null)
                {
                    Debug.LogWarning($"BossArena is missing from {path}.");
                    return;
                }

                BuildAmbience(arena.gameObject, theme);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (!wasLoaded && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void BuildAmbience(GameObject arenaRoot, Theme theme)
        {
            Transform oldRoot = arenaRoot.transform.Find("Arena Atmosphere");
            if (oldRoot != null)
                Object.DestroyImmediate(oldRoot.gameObject);

            BossArenaAmbience ambience =
                arenaRoot.GetComponent<BossArenaAmbience>();
            if (ambience == null)
                ambience = arenaRoot.AddComponent<BossArenaAmbience>();

            GameObject atmosphere = new GameObject("Arena Atmosphere");
            atmosphere.transform.SetParent(arenaRoot.transform, false);

            Material lineMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(MagicLineMaterial);
            Material orbMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(MagicOrbMaterial);
            Material sparkleMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(SparkleMaterial);
            List<ParticleSystem> particles = new List<ParticleSystem>
            {
                CreateParticles(
                    atmosphere.transform,
                    "Drifting Motes",
                    new Vector3(0f, 1f, 0f),
                    new Vector3(27f, 11f, 0.1f),
                    5f,
                    9f,
                    0.08f,
                    0.2f,
                    4.5f,
                    48,
                    theme.primary,
                    theme.secondary,
                    orbMaterial,
                    -2,
                    new Vector2(-0.08f, 0.08f),
                    new Vector2(0.08f, 0.24f)
                ),
                CreateParticles(
                    atmosphere.transform,
                    "High Arcane Sparks",
                    new Vector3(0f, 3.5f, 0f),
                    new Vector3(24f, 5f, 0.1f),
                    2.2f,
                    4.2f,
                    0.1f,
                    0.28f,
                    2.5f,
                    24,
                    theme.accent,
                    theme.primary,
                    sparkleMaterial != null ? sparkleMaterial : lineMaterial,
                    -1,
                    new Vector2(-0.16f, 0.16f),
                    new Vector2(-0.03f, 0.12f)
                )
            };

            List<Light2D> lights = new List<Light2D>();
            List<float> intensities = new List<float>();
            AddLight(lights, intensities, atmosphere.transform, "Core Glow",
                new Vector3(0f, 1f, 0f), theme.primary, 2.2f, 10f, 0.58f);
            AddLight(lights, intensities, atmosphere.transform, "Left Edge Glow",
                new Vector3(-9.5f, 0f, 0f), theme.secondary, 1.2f, 6.5f, 0.42f);
            AddLight(lights, intensities, atmosphere.transform, "Right Edge Glow",
                new Vector3(9.5f, 0f, 0f), theme.secondary, 1.2f, 6.5f, 0.42f);
            AddLight(lights, intensities, atmosphere.transform, "Left Crown Glow",
                new Vector3(-5.5f, 5f, 0f), theme.accent, 0.8f, 4.8f, 0.32f);
            AddLight(lights, intensities, atmosphere.transform, "Right Crown Glow",
                new Vector3(5.5f, 5f, 0f), theme.accent, 0.8f, 4.8f, 0.32f);

            SerializedObject serialized = new SerializedObject(ambience);
            serialized.FindProperty("targetBossType").stringValue = theme.bossType;
            serialized.FindProperty("lightFadeDuration").floatValue = 1.25f;
            serialized.FindProperty("lightPulseAmount").floatValue = 0.12f;
            serialized.FindProperty("lightPulseDuration").floatValue = 2.4f;
            SetObjectArray(serialized.FindProperty("ambientParticles"), particles);
            SetObjectArray(serialized.FindProperty("arenaLights"), lights);
            SerializedProperty intensityProperty =
                serialized.FindProperty("lightIntensities");
            intensityProperty.arraySize = intensities.Count;
            for (int i = 0; i < intensities.Count; i++)
                intensityProperty.GetArrayElementAtIndex(i).floatValue = intensities[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ParticleSystem CreateParticles(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 shapeScale,
            float lifetimeMin,
            float lifetimeMax,
            float sizeMin,
            float sizeMax,
            float rate,
            int maxParticles,
            Color colorA,
            Color colorB,
            Material material,
            int sortingOrder,
            Vector2 horizontalVelocity,
            Vector2 verticalVelocity)
        {
            GameObject particleObject = new GameObject(name);
            particleObject.transform.SetParent(parent, false);
            particleObject.transform.localPosition = position;

            ParticleSystem system = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.duration = 8f;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.maxParticles = maxParticles;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = shapeScale;

            ParticleSystem.VelocityOverLifetimeModule velocity =
                system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(
                horizontalVelocity.x,
                horizontalVelocity.y
            );
            velocity.y = new ParticleSystem.MinMaxCurve(
                verticalVelocity.x,
                verticalVelocity.y
            );
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Low;
            noise.strength = 0.18f;
            noise.frequency = 0.16f;
            noise.scrollSpeed = 0.08f;
            noise.damping = true;

            ParticleSystemRenderer renderer =
                particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;

            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return system;
        }

        private static void AddLight(
            ICollection<Light2D> lights,
            ICollection<float> intensities,
            Transform parent,
            string name,
            Vector3 position,
            Color color,
            float innerRadius,
            float outerRadius,
            float intensity)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = position;

            Light2D light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(color.r, color.g, color.b, 1f);
            light.intensity = 0f;
            light.pointLightInnerRadius = innerRadius;
            light.pointLightOuterRadius = outerRadius;
            light.falloffIntensity = 0.65f;

            lights.Add(light);
            intensities.Add(intensity);
        }

        private static void SetObjectArray<T>(
            SerializedProperty property,
            IReadOnlyList<T> values)
            where T : Object
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
#endif
