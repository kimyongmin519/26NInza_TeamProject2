#if UNITY_EDITOR
using System.Collections.Generic;
using GGMLib.ObjectPool.Runtime;
using Member.KYM.Scripts.EffectSystems;
using Member.ODK.Scripts.Enemys.Swordmaster;
using UnityEditor;
using UnityEngine;

namespace Member.ODK.Editor
{
    public static class SwordmasterVfxBuilder
    {
        private static readonly Color SwordmasterPurple =
            new Color(0.72f, 0.28f, 1f, 1f);

        private const string BossPrefabPath =
            "Assets/Member/ODK/02_Prefab/Swordmaster.prefab";
        private const string PrefabFolder =
            "Assets/Member/ODK/02_Prefab/Boss/Swordmaster/VFX";
        private const string PoolFolder =
            "Assets/Member/ODK/05_Database/Pool/Swordmaster";
        private const string PoolManagerPath =
            PoolFolder + "/SwordmasterVfxPoolManager.asset";

        private readonly struct EffectProfile
        {
            public readonly string Name;
            public readonly string SourcePath;
            public readonly float Scale;
            public readonly int InitialCount;

            public EffectProfile(
                string name,
                string sourcePath,
                float scale,
                int initialCount)
            {
                Name = name;
                SourcePath = sourcePath;
                Scale = scale;
                InitialCount = initialCount;
            }
        }

        [MenuItem("Tools/ODK/Build Swordmaster Pooled VFX")]
        public static void Build()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(PoolFolder);

            PoolItemSO teleport = BuildEffect(new EffectProfile(
                "SwordmasterTeleportVfx",
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/MagicSoftExplosion/MagicSoftExplosionPink.prefab",
                0.72f,
                3
            ));
            PoolItemSO charge = BuildEffect(new EffectProfile(
                "SwordmasterChargeVfx",
                "Assets/Epic Toon FX/Prefabs/Combat/Magic/Charge/MagicChargeBlue.prefab",
                0.58f,
                3
            ));
            PoolItemSO impact = BuildEffect(new EffectProfile(
                "SwordmasterImpactVfx",
                "Assets/Epic Toon FX/Prefabs/Combat/Sword/Hit/SwordHitMagic/SwordHitMagicBlue.prefab",
                0.62f,
                6
            ));
            PoolItemSO finisher = BuildEffect(new EffectProfile(
                "SwordmasterFinisherVfx",
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/MagicNovaExplosion/MagicNovaExplosionPink.prefab",
                0.82f,
                2
            ));
            PoolItemSO death = BuildEffect(new EffectProfile(
                "SwordmasterDeathVfx",
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions/NovaExplosion/ExplosionNovaPink.prefab",
                0.9f,
                2
            ));

            PoolManagerSO manager = GetOrCreateAsset<PoolManagerSO>(PoolManagerPath);
            manager.itemList = new List<PoolItemSO>
            {
                teleport,
                charge,
                impact,
                finisher,
                death
            };
            EditorUtility.SetDirty(manager);

            ConfigureBoss(manager, teleport, charge, impact, finisher, death);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Swordmaster pooled VFX built and connected.");
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
                    main.startColor = Recolor(main.startColor);
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
            PoolItemSO charge,
            PoolItemSO impact,
            PoolItemSO finisher,
            PoolItemSO death)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BossPrefabPath);
            try
            {
                SwordmasterVfxPool pool = root.GetComponent<SwordmasterVfxPool>();
                if (pool == null)
                    pool = root.AddComponent<SwordmasterVfxPool>();

                SerializedObject poolObject = new SerializedObject(pool);
                poolObject.FindProperty("poolManager").objectReferenceValue = manager;
                poolObject.ApplyModifiedPropertiesWithoutUndo();

                SwordmasterFeedback feedback = root.GetComponent<SwordmasterFeedback>();
                SerializedObject feedbackObject = new SerializedObject(feedback);
                SetEffect(feedbackObject, "teleport", teleport);
                SetEffect(feedbackObject, "summonSwords", charge);
                SetEffect(feedbackObject, "dashReady", charge);
                SetEffect(feedbackObject, "dashEnd", impact);
                SetEffect(feedbackObject, "thrustGather", charge);
                SetEffect(feedbackObject, "crossfireReady", charge);
                SetEffect(feedbackObject, "volleyFire", impact);
                SetEffect(feedbackObject, "finalCross", finisher);
                SetEffect(feedbackObject, "swordImpact", impact);
                SetEffect(feedbackObject, "hit", impact);
                SetEffect(feedbackObject, "death", death);
                ClearEffect(feedbackObject, "swordLaunch");
                ClearEffect(feedbackObject, "swordDispelled");
                ClearEffect(feedbackObject, "dashStart");
                ClearEffect(feedbackObject, "thrustStrike");
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
            string slotName,
            PoolItemSO item)
        {
            SerializedProperty property = feedback.FindProperty(
                $"{slotName}.effectItem"
            );
            if (property != null)
                property.objectReferenceValue = item;
        }

        private static void ClearEffect(
            SerializedObject feedback,
            string slotName)
        {
            SetEffect(feedback, slotName, null);
        }

        private static ParticleSystem.MinMaxGradient Recolor(
            ParticleSystem.MinMaxGradient source)
        {
            switch (source.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return Recolor(source.color);
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(
                        Recolor(source.colorMin),
                        Recolor(source.colorMax)
                    );
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(
                        Recolor(source.gradient)
                    );
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(
                        Recolor(source.gradientMin),
                        Recolor(source.gradientMax)
                    );
                default:
                    return source;
            }
        }

        private static Gradient Recolor(Gradient source)
        {
            if (source == null)
                return null;

            Gradient gradient = new Gradient
            {
                mode = source.mode
            };
            GradientColorKey[] colors = source.colorKeys;
            for (int i = 0; i < colors.Length; i++)
                colors[i].color = Recolor(colors[i].color);
            gradient.SetKeys(colors, source.alphaKeys);
            return gradient;
        }

        private static Color Recolor(Color source)
        {
            float brightness = Mathf.Max(source.r, source.g, source.b);
            return new Color(
                SwordmasterPurple.r * brightness,
                SwordmasterPurple.g * brightness,
                SwordmasterPurple.b * brightness,
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
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = System.IO.Path.GetDirectoryName(folderPath)
                ?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
