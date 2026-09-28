#if UNITY_EDITOR
using KimLIb.SoundSystem;
using Member.KYM.Scripts.CombatSystems.Projectiles;
using Member.KYM.Scripts.CoreSystems;
using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Member.ODK.Editor
{
    // Builds the ODK-owned prefab and data wiring used by boss audio and grab HUDs.
    public static class ODKBossAudioGrabSetupBuilder
    {
        private const string MoonPrefabPath =
            "Assets/Member/ODK/02_Prefab/Moon.prefab";
        private const string SwordmasterPrefabPath =
            "Assets/Member/ODK/02_Prefab/Swordmaster.prefab";
        private const string LostSoulPrefabPath =
            "Assets/Member/ODK/02_Prefab/Boss/LostSoul/LostSoul.prefab";

        private const string MoonRockPrefabPath =
            "Assets/Member/ODK/02_Prefab/Boss/Moon/MoonRock.prefab";
        private const string SwordPrefabPath =
            "Assets/Member/ODK/02_Prefab/Boss/Swordmaster/EnchantedSword.prefab";
        private const string WeakSoulPrefabPath =
            "Assets/Member/ODK/02_Prefab/Boss/LostSoul/LostSoulWeakProjectile.prefab";

        private const string BgmFolder =
            "Assets/Member/ODK/03_Sound/BGM";
        private const string GrabInfoFolder =
            "Assets/Member/ODK/05_Database/GrabInfo";
        private const string SystemPrefabFolder =
            "Assets/Member/ODK/02_Prefab/System";
        private const string ManagerPrefabPath =
            SystemPrefabFolder + "/ODKBgmManager.prefab";

        [MenuItem("Tools/ODK/Build Boss BGM And Grab Info")]
        public static void Build()
        {
            EnsureFolder(BgmFolder);
            EnsureFolder(GrabInfoFolder);
            EnsureFolder(SystemPrefabFolder);

            BgmManager managerPrefab = BuildBgmManagerPrefab();
            SoundClipSO moonBgm = BuildBgm(
                "MoonBossBgm",
                "Assets/Member/ODK/03_Sound/창공의 성채.mp3",
                0.62f
            );
            SoundClipSO swordmasterBgm = BuildBgm(
                "SwordmasterBgm",
                "Assets/Member/ODK/03_Sound/혼돈의 사도들.mp3",
                0.62f
            );
            SoundClipSO lostSoulBgm = BuildBgm(
                "LostSoulBgm",
                "Assets/Member/ODK/03_Sound/볼카닉 헥스.mp3",
                0.58f
            );

            ConfigureBossIntro(MoonPrefabPath, "달", "추락한 위성");
            ConfigureBossIntro(
                SwordmasterPrefabPath,
                "소드 마스터",
                "끝없는 탑의 검객"
            );
            ConfigureBossIntro(
                LostSoulPrefabPath,
                "어둠의 사신",
                "잊힌 자들의 망령"
            );

            ConfigureBossBgm(MoonPrefabPath, managerPrefab, moonBgm);
            ConfigureBossBgm(SwordmasterPrefabPath, managerPrefab, swordmasterBgm);
            ConfigureBossBgm(LostSoulPrefabPath, managerPrefab, lostSoulBgm);

            ConfigureGrabInfo(
                MoonRockPrefabPath,
                "MoonFragmentInfo",
                "달 파편"
            );
            ConfigureGrabInfo(
                SwordPrefabPath,
                "SwordmasterSwordInfo",
                "소드마스터의 검"
            );
            ConfigureGrabInfo(
                WeakSoulPrefabPath,
                "FadedSoulProjectileInfo",
                "흐린 영혼 탄막"
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ODK boss intro, BGM, and grab information configured.");
        }

        private static BgmManager BuildBgmManagerPrefab()
        {
            GameObject root = new GameObject("ODK Bgm Manager");
            try
            {
                BgmManager manager = root.AddComponent<BgmManager>();
                AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(
                    "Assets/KimLIb/SoundSystem/Master.mixer"
                );
                AudioMixerGroup[] groups = mixer != null
                    ? mixer.FindMatchingGroups("Bgm")
                    : System.Array.Empty<AudioMixerGroup>();

                SerializedObject managerObject = new SerializedObject(manager);
                if (groups.Length > 0)
                {
                    managerObject.FindProperty("bgmGroup").objectReferenceValue =
                        groups[0];
                }
                managerObject.FindProperty("defaultFadeDuration").floatValue = 0.7f;
                managerObject.FindProperty("startingBgm").objectReferenceValue = null;
                managerObject.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    ManagerPrefabPath
                );
                return prefab.GetComponent<BgmManager>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static SoundClipSO BuildBgm(
            string assetName,
            string clipPath,
            float volume)
        {
            string assetPath = $"{BgmFolder}/{assetName}.asset";
            SoundClipSO bgm = AssetDatabase.LoadAssetAtPath<SoundClipSO>(assetPath);
            if (bgm == null)
            {
                bgm = ScriptableObject.CreateInstance<SoundClipSO>();
                AssetDatabase.CreateAsset(bgm, assetPath);
            }

            bgm.audioTypes = AudioTypes.Music;
            bgm.audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            bgm.loop = true;
            bgm.randomizePitch = false;
            bgm.randomPitchModifier = 0f;
            bgm.volume = volume;
            bgm.pitch = 1f;
            EditorUtility.SetDirty(bgm);
            return bgm;
        }

        private static void ConfigureBossBgm(
            string prefabPath,
            BgmManager managerPrefab,
            SoundClipSO bgm)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                RemoveMissingScripts(root);
                BossBgmPlayback playback = root.GetComponent<BossBgmPlayback>();
                if (playback == null)
                    playback = root.AddComponent<BossBgmPlayback>();

                SerializedObject playbackObject = new SerializedObject(playback);
                playbackObject.FindProperty("managerPrefab").objectReferenceValue =
                    managerPrefab;
                playbackObject.FindProperty("bossBgm").objectReferenceValue = bgm;
                playbackObject.FindProperty("fadeInDuration").floatValue = 0.7f;
                playbackObject.FindProperty("fadeOutDuration").floatValue = 0.8f;
                playbackObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureBossIntro(
            string prefabPath,
            string title,
            string subtitle)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                RemoveMissingScripts(root);
                BossIntroTimeline intro = root.GetComponent<BossIntroTimeline>();
                if (intro == null)
                    intro = root.AddComponent<BossIntroTimeline>();

                SerializedObject introObject = new SerializedObject(intro);
                introObject.FindProperty("bossTitle").stringValue = title;
                introObject.FindProperty("bossSubtitle").stringValue = subtitle;
                introObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureGrabInfo(
            string prefabPath,
            string assetName,
            string displayName)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                RemoveMissingScripts(root);
                SpriteRenderer renderer =
                    root.GetComponentInChildren<SpriteRenderer>(true);
                ProjectileDataSO data = GetOrCreateProjectileData(assetName);
                SerializedObject dataObject = new SerializedObject(data);
                dataObject.FindProperty("<Icon>k__BackingField").objectReferenceValue =
                    renderer != null ? renderer.sprite : null;
                dataObject.FindProperty("<MoveSpeed>k__BackingField").floatValue = 0f;
                dataObject.FindProperty("<Name>k__BackingField").stringValue =
                    displayName;
                dataObject.FindProperty("<ImpactItem>k__BackingField").objectReferenceValue =
                    null;
                dataObject.FindProperty("<ImpactColor>k__BackingField").colorValue =
                    Color.white;
                dataObject.ApplyModifiedPropertiesWithoutUndo();

                ODKGrabInfoDisplay display = root.GetComponent<ODKGrabInfoDisplay>();
                if (display == null)
                    display = root.AddComponent<ODKGrabInfoDisplay>();
                SerializedObject displayObject = new SerializedObject(display);
                displayObject.FindProperty("projectileData").objectReferenceValue = data;
                displayObject.ApplyModifiedPropertiesWithoutUndo();

                int grabbableLayer = GrabbableLayer.Index;
                if (grabbableLayer >= 0)
                    root.layer = grabbableLayer;

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static ProjectileDataSO GetOrCreateProjectileData(string assetName)
        {
            string path = $"{GrabInfoFolder}/{assetName}.asset";
            ProjectileDataSO data = AssetDatabase.LoadAssetAtPath<ProjectileDataSO>(path);
            if (data != null)
                return data;

            data = ScriptableObject.CreateInstance<ProjectileDataSO>();
            AssetDatabase.CreateAsset(data, path);
            return data;
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

        private static void RemoveMissingScripts(GameObject root)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in transforms)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }
    }
}
#endif
