#if UNITY_EDITOR
using System.Collections.Generic;
using GGMLib.ObjectPool.Runtime;
using Member.KYM.Scripts.EffectSystems;
using Member.ODK.Scripts.Enemys.LostSoul;
using UnityEditor;
using UnityEngine;

namespace Member.ODK.Editor
{
    public static class LostSoulVfxBuilder
    {
        private const string BossPrefabPath =
            "Assets/Member/ODK/02_Prefab/Boss/LostSoul/LostSoul.prefab";
        private const string PrefabFolder =
            "Assets/Member/ODK/02_Prefab/Boss/LostSoul/VFX";
        private const string PoolFolder =
            "Assets/Member/ODK/05_Database/Pool/LostSoulVfx";
        private const string PoolManagerPath =
            PoolFolder + "/LostSoulVfxPoolManager.asset";

        private static readonly Color LightPurple =
            new Color(0.82f, 0.58f, 1f, 1f);
        private static readonly Color Purple =
            new Color(0.68f, 0.18f, 1f, 1f);
        private static readonly Color Cyan =
            new Color(0.2f, 0.9f, 1f, 1f);

        private readonly struct EffectProfile
        {
            public readonly string Name;
            public readonly string SourcePath;
            public readonly float Scale;
            public readonly int InitialCount;
            public readonly Color Color;

            public EffectProfile(
                string name,
                string sourcePath,
                float scale,
                int initialCount,
                Color color)
            {
                Name = name;
                SourcePath = sourcePath;
                Scale = scale;
                InitialCount = initialCount;
                Color = color;
            }
        }

        [MenuItem("Tools/ODK/Build Lost Soul Pooled VFX")]
        public static void Build()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(PoolFolder);

            PoolItemSO teleport = BuildEffect(new EffectProfile(
                "LostSoulTeleportVfx",
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/MagicSoftExplosion/MagicSoftExplosionPink.prefab",
                0.62f,
                3,
                LightPurple
            ));
            PoolItemSO cast = BuildEffect(new EffectProfile(
                "LostSoulCastVfx",
                "Assets/Epic Toon FX/Prefabs/Combat/Magic/Charge/MagicChargeBlue.prefab",
                0.5f,
                3,
                LightPurple
            ));
            PoolItemSO purpleProjectile = BuildEffect(new EffectProfile(
                "LostSoulPurpleProjectileVfx",
                "Assets/Epic Toon FX/Prefabs/Combat/Muzzleflash/SoulMuzzle/SoulMuzzlePurple.prefab",
                0.25f,
                10,
                Purple
            ));
            PoolItemSO cyanProjectile = BuildEffect(new EffectProfile(
                "LostSoulCyanProjectileVfx",
                "Assets/Epic Toon FX/Prefabs/Combat/Muzzleflash/EnergyNovaSmallMuzzle/EnergyNovaMuzzleSmallBlue.prefab",
                0.22f,
                8,
                Cyan
            ));
            PoolItemSO purpleImpact = BuildEffect(new EffectProfile(
                "LostSoulPurpleImpactVfx",
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/MagicSoftExplosion/MagicSoftExplosionPink.prefab",
                0.24f,
                10,
                Purple
            ));
            PoolItemSO cyanImpact = BuildEffect(new EffectProfile(
                "LostSoulCyanImpactVfx",
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/MagicSoftExplosion/MagicSoftExplosionBlue.prefab",
                0.22f,
                8,
                Cyan
            ));
            PoolItemSO hit = BuildEffect(new EffectProfile(
                "LostSoulHitVfx",
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/MagicSoftExplosion/MagicSoftExplosionPink.prefab",
                0.3f,
                4,
                LightPurple
            ));
            PoolItemSO death = BuildEffect(new EffectProfile(
                "LostSoulDeathVfx",
                "Assets/Epic Toon FX/Prefabs/Combat/Death/Souls/SoulMysticDeath.prefab",
                0.68f,
                2,
                LightPurple
            ));

            PoolManagerSO manager = GetOrCreateAsset<PoolManagerSO>(PoolManagerPath);
            manager.itemList = new List<PoolItemSO>
            {
                teleport,
                cast,
                purpleProjectile,
                cyanProjectile,
                purpleImpact,
                cyanImpact,
                hit,
                death
            };
            EditorUtility.SetDirty(manager);

            ConfigureBoss(
                manager,
                teleport,
                cast,
                purpleProjectile,
                cyanProjectile,
                purpleImpact,
                cyanImpact,
                hit,
                death
            );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Lost Soul pooled VFX built and connected.");
        }

        private static PoolItemSO BuildEffect(EffectProfile profile)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                profile.SourcePath
            );
            if (source == null)
                throw new MissingReferenceException(profile.SourcePath);

            string prefabPath = $"{PrefabFolder}/{profile.Name}.prefab";
            string itemPath = $"{PoolFolder}/{profile.Name}.asset";
            GameObject root = new GameObject(profile.Name);
            try
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(
                    source,
                    root.transform
                );
                visual.name = "Effect";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale *= profile.Scale;

                ParticleSystem[] particles =
                    root.GetComponentsInChildren<ParticleSystem>(true);
                float duration = 0.2f;
                foreach (ParticleSystem particle in particles)
                {
                    ParticleSystem.MainModule main = particle.main;
                    main.playOnAwake = false;
                    main.loop = false;
                    main.stopAction = ParticleSystemStopAction.None;
                    main.startColor = Recolor(main.startColor, profile.Color);
                    ParticleSystemRenderer renderer =
                        particle.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                        renderer.sortingOrder = 50;
                    duration = Mathf.Max(
                        duration,
                        main.duration + main.startLifetime.constantMax
                    );
                }

                ParticleVfx playable = root.AddComponent<ParticleVfx>();
                SerializedObject playableObject = new SerializedObject(playable);
                playableObject.FindProperty("<Duration>k__BackingField").floatValue =
                    Mathf.Clamp(duration, 0.2f, 3f);
                SerializedProperty particleArray =
                    playableObject.FindProperty("particles");
                particleArray.arraySize = particles.Length;
                for (int i = 0; i < particles.Length; i++)
                {
                    particleArray.GetArrayElementAtIndex(i).objectReferenceValue =
                        particles[i];
                }
                playableObject.ApplyModifiedPropertiesWithoutUndo();

                PoolableVfx poolable = root.AddComponent<PoolableVfx>();
                SerializedObject poolableObject = new SerializedObject(poolable);
                poolableObject.FindProperty("effectObject").objectReferenceValue = root;
                poolableObject.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            PoolItemSO item = GetOrCreateAsset<PoolItemSO>(itemPath);
            item.itemName = profile.Name;
            item.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            item.initCount = profile.InitialCount;
            EditorUtility.SetDirty(item);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                PoolableVfx poolable = prefabRoot.GetComponent<PoolableVfx>();
                SerializedObject poolableObject = new SerializedObject(poolable);
                poolableObject.FindProperty("<PoolItem>k__BackingField")
                    .objectReferenceValue = item;
                poolableObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            return item;
        }

        private static void ConfigureBoss(
            PoolManagerSO manager,
            PoolItemSO teleport,
            PoolItemSO cast,
            PoolItemSO purpleProjectile,
            PoolItemSO cyanProjectile,
            PoolItemSO purpleImpact,
            PoolItemSO cyanImpact,
            PoolItemSO hit,
            PoolItemSO death)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BossPrefabPath);
            try
            {
                LostSoulVfxPool pool = root.GetComponent<LostSoulVfxPool>();
                if (pool == null)
                    pool = root.AddComponent<LostSoulVfxPool>();

                SerializedObject poolObject = new SerializedObject(pool);
                poolObject.FindProperty("poolManager").objectReferenceValue = manager;
                poolObject.ApplyModifiedPropertiesWithoutUndo();

                LostSoulFeedback feedback = root.GetComponent<LostSoulFeedback>();
                SerializedObject feedbackObject = new SerializedObject(feedback);
                SetEffect(feedbackObject, "teleportEffect", teleport);
                SetEffect(feedbackObject, "castEffect", cast);
                SetEffect(feedbackObject, "purpleProjectileEffect", purpleProjectile);
                SetEffect(feedbackObject, "cyanProjectileEffect", cyanProjectile);
                SetEffect(feedbackObject, "purpleImpactEffect", purpleImpact);
                SetEffect(feedbackObject, "cyanImpactEffect", cyanImpact);
                SetEffect(feedbackObject, "hitEffect", hit);
                SetEffect(feedbackObject, "deathEffect", death);
                feedbackObject.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetEffect(
            SerializedObject feedback,
            string field,
            PoolItemSO item)
        {
            SerializedProperty property = feedback.FindProperty(field);
            if (property != null)
                property.objectReferenceValue = item;
        }

        private static ParticleSystem.MinMaxGradient Recolor(
            ParticleSystem.MinMaxGradient source,
            Color target)
        {
            switch (source.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return Recolor(source.color, target);
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(
                        Recolor(source.colorMin, target),
                        Recolor(source.colorMax, target)
                    );
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(
                        Recolor(source.gradient, target)
                    );
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(
                        Recolor(source.gradientMin, target),
                        Recolor(source.gradientMax, target)
                    );
                default:
                    return source;
            }
        }

        private static Gradient Recolor(Gradient source, Color target)
        {
            if (source == null)
                return null;

            Gradient gradient = new Gradient
            {
                mode = source.mode
            };
            GradientColorKey[] colors = source.colorKeys;
            for (int i = 0; i < colors.Length; i++)
                colors[i].color = Recolor(colors[i].color, target);
            gradient.SetKeys(colors, source.alphaKeys);
            return gradient;
        }

        private static Color Recolor(Color source, Color target)
        {
            float brightness = Mathf.Max(source.r, source.g, source.b);
            return new Color(
                target.r * brightness,
                target.g * brightness,
                target.b * brightness,
                source.a
            );
        }

        private static T GetOrCreateAsset<T>(string path)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
