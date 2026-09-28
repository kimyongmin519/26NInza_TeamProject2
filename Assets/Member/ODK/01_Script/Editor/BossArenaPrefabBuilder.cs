#if UNITY_EDITOR
using System.IO;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Member.ODK.Editor
{
    public static class BossArenaPrefabBuilder
    {
        private const string ArenaFolder = "Assets/Member/ODK/02_Prefab/Boss/Arenas";
        private const string CastleTileFolder = "Assets/Member/ODK/05_Database/Tilemap/CastleBuild";
        private const string DarkTileFolder = "Assets/Member/ODK/05_Database/Tilemap/DarkCastle/Tiles";

        private readonly struct ArenaProfile
        {
            public readonly string Name;
            public readonly Color Ground;
            public readonly Color Back;
            public readonly Color Accent;
            public readonly string DecorationTile;

            public ArenaProfile(string name, Color ground, Color back, Color accent, string decorationTile)
            {
                Name = name;
                Ground = ground;
                Back = back;
                Accent = accent;
                DecorationTile = decorationTile;
            }
        }

        [MenuItem("Tools/ODK/Build Boss Arena Prefabs And Summons")]
        public static void BuildAll()
        {
            EnsureFolder(ArenaFolder);

            BuildArena(new ArenaProfile(
                "Moon Arena",
                new Color(0.48f, 0.58f, 0.78f, 1f),
                new Color(0.08f, 0.12f, 0.28f, 0.82f),
                new Color(0.55f, 0.9f, 1f, 0.92f),
                $"{DarkTileFolder}/castle_env_0_12.asset"));
            BuildArena(new ArenaProfile(
                "Swordmaster Arena",
                new Color(0.52f, 0.48f, 0.62f, 1f),
                new Color(0.12f, 0.08f, 0.18f, 0.86f),
                new Color(0.86f, 0.68f, 1f, 0.94f),
                $"{DarkTileFolder}/castle_env_0_21.asset"));
            BuildArena(new ArenaProfile(
                "Lost Soul Arena",
                new Color(0.3f, 0.22f, 0.42f, 1f),
                new Color(0.035f, 0.018f, 0.07f, 0.93f),
                new Color(0.72f, 0.3f, 1f, 0.88f),
                $"{DarkTileFolder}/castle_env_0_1.asset"));
            BuildArena(new ArenaProfile(
                "Volcanus Arena",
                new Color(0.6f, 0.32f, 0.16f, 1f),
                new Color(0.16f, 0.045f, 0.025f, 0.9f),
                new Color(1f, 0.38f, 0.08f, 0.95f),
                $"{DarkTileFolder}/castle_env_0_14.asset"));

            ConfigureSummon(
                "Assets/Member/ODK/02_Prefab/Moon.prefab",
                string.Empty,
                new Vector3(0f, 6f, 0f),
                new Vector3(0.25f, 0.25f, 1f),
                -35f,
                0.4f,
                0.8f,
                0.35f,
                new Color(0.5f, 0.9f, 1f, 1f),
                0.75f);
            ConfigureSummon(
                "Assets/Member/ODK/02_Prefab/Swordmaster.prefab",
                string.Empty,
                new Vector3(0f, 1.5f, 0f),
                new Vector3(0.05f, 1.35f, 1f),
                0f,
                0.3f,
                0.48f,
                0.28f,
                new Color(0.88f, 0.64f, 1f, 1f),
                0.5f);
            ConfigureSummon(
                "Assets/Member/ODK/02_Prefab/Boss/LostSoul/LostSoul.prefab",
                string.Empty,
                new Vector3(0f, -2f, 0f),
                new Vector3(0.72f, 0.72f, 1f),
                8f,
                0.55f,
                0.88f,
                0.35f,
                new Color(0.68f, 0.25f, 1f, 1f),
                0.6f);
            ConfigureSummon(
                "Assets/Member/ODK/02_Prefab/Boss/Volcanus/Volcanus.prefab",
                string.Empty,
                new Vector3(0f, -5f, 0f),
                new Vector3(1.4f, 0.2f, 1f),
                0f,
                0.35f,
                0.75f,
                0.4f,
                new Color(1f, 0.35f, 0.08f, 1f),
                1.5f);

            ConfigureDeath(
                "Assets/Member/ODK/02_Prefab/Moon.prefab",
                0.15f,
                0.3f,
                0.8f,
                new Vector3(0f, -2.2f, 0f),
                new Vector3(0.3f, 0.12f, 1f),
                22f,
                new Color(0.45f, 0.78f, 1f, 1f),
                1.25f);
            ConfigureDeath(
                "Assets/Member/ODK/02_Prefab/Swordmaster.prefab",
                0.65f,
                0.22f,
                0.65f,
                new Vector3(0f, -0.8f, 0f),
                new Vector3(0.7f, 0.1f, 1f),
                -20f,
                new Color(0.76f, 0.46f, 1f, 1f),
                1.15f);
            ConfigureDeath(
                "Assets/Member/ODK/02_Prefab/Boss/LostSoul/LostSoul.prefab",
                0.75f,
                0.3f,
                0.9f,
                new Vector3(0f, 2.5f, 0f),
                new Vector3(0.42f, 1.35f, 1f),
                4f,
                new Color(0.54f, 0.22f, 0.85f, 1f),
                1.2f);
            ConfigureDeath(
                "Assets/Member/ODK/02_Prefab/Boss/Volcanus/Volcanus.prefab",
                0.8f,
                0.38f,
                0.85f,
                new Vector3(0f, -2f, 0f),
                new Vector3(1.12f, 0.1f, 1f),
                -4f,
                new Color(1f, 0.2f, 0.04f, 1f),
                1.8f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ODK] 4개 보스 아레나 프리팹과 보스별 소환 연출 연결 완료");
        }

        private static void BuildArena(ArenaProfile profile)
        {
            TileBase groundA = AssetDatabase.LoadAssetAtPath<TileBase>($"{CastleTileFolder}/CastleBuild_Ground_A.asset");
            TileBase groundB = AssetDatabase.LoadAssetAtPath<TileBase>($"{CastleTileFolder}/CastleBuild_Ground_B.asset");
            TileBase groundC = AssetDatabase.LoadAssetAtPath<TileBase>($"{CastleTileFolder}/CastleBuild_Ground_C.asset");
            TileBase decoration = AssetDatabase.LoadAssetAtPath<TileBase>(profile.DecorationTile);
            if (groundA == null || groundB == null || groundC == null)
                throw new FileNotFoundException("CastleBuild RuleTile을 찾을 수 없습니다.");

            GameObject root = new GameObject(profile.Name, typeof(Grid), typeof(BossArena));
            Grid grid = root.GetComponent<Grid>();
            grid.cellSize = Vector3.one;

            SerializedObject arenaObject = new SerializedObject(root.GetComponent<BossArena>());
            arenaObject.FindProperty("size").vector2Value = new Vector2(28f, 14f);
            arenaObject.FindProperty("centerOffset").vector2Value = new Vector2(0f, 1f);
            arenaObject.FindProperty("gizmoColor").colorValue = profile.Accent;
            arenaObject.ApplyModifiedPropertiesWithoutUndo();

            Tilemap background = CreateTilemap(root.transform, "Background", -20, profile.Back);
            Tilemap decorationMap = CreateTilemap(root.transform, "Decoration", -5, profile.Accent);
            Tilemap collision = CreateTilemap(root.transform, "Collision", 0, profile.Ground);
            collision.gameObject.layer = 3;

            for (int x = -15; x <= 14; x++)
            {
                collision.SetTile(new Vector3Int(x, -6, 0), groundA);
                collision.SetTile(new Vector3Int(x, -7, 0), groundA);
                background.SetTile(new Vector3Int(x, -8, 0), groundB);
                background.SetTile(new Vector3Int(x, -7, 0), groundB);
            }

            for (int y = -5; y <= 8; y++)
            {
                collision.SetTile(new Vector3Int(-15, y, 0), groundA);
                collision.SetTile(new Vector3Int(14, y, 0), groundA);
                background.SetTile(new Vector3Int(-14, y, 0), groundB);
                background.SetTile(new Vector3Int(13, y, 0), groundB);
            }

            for (int x = -13; x <= 12; x++)
            {
                if ((x + 13) % 4 == 0)
                {
                    background.SetTile(new Vector3Int(x, -4, 0), groundC);
                    background.SetTile(new Vector3Int(x, 6, 0), groundC);
                }
                if (decoration != null && (x + 13) % 3 == 0)
                    decorationMap.SetTile(new Vector3Int(x, -5, 0), decoration);
            }

            AddCollision(collision.gameObject);
            AddMarker(root.transform, "Boss Spawn Point", new Vector3(0f, -4.6f, 0f));
            AddMarker(root.transform, "Player Spawn Point", new Vector3(-7f, -4.6f, 0f));
            AddMarker(root.transform, "Camera Center", new Vector3(0f, 1f, 0f));

            string fileName = profile.Name.Replace(" ", string.Empty) + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, $"{ArenaFolder}/{fileName}");
            Object.DestroyImmediate(root);
        }

        private static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder, Color color)
        {
            GameObject tileObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            tileObject.transform.SetParent(parent, false);
            Tilemap tilemap = tileObject.GetComponent<Tilemap>();
            tilemap.color = color;
            TilemapRenderer renderer = tileObject.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            return tilemap;
        }

        private static void AddCollision(GameObject tileObject)
        {
            Rigidbody2D body = tileObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            TilemapCollider2D tileCollider = tileObject.AddComponent<TilemapCollider2D>();
            CompositeCollider2D composite = tileObject.AddComponent<CompositeCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }

        private static void AddMarker(Transform parent, string name, Vector3 localPosition)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
        }

        private static void ConfigureSummon(
            string prefabPath,
            string visualName,
            Vector3 offset,
            Vector3 scale,
            float rotation,
            float delay,
            float appearDuration,
            float settleDuration,
            Color tint,
            float shake)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform visual = string.IsNullOrWhiteSpace(visualName)
                    ? root.transform
                    : root.transform.Find(visualName) ?? FindDeep(root.transform, visualName);
                if (visual == null)
                {
                    Debug.LogWarning($"[ODK] {prefabPath} 안에서 '{visualName}'을 찾지 못해 루트를 사용합니다.");
                    visual = root.transform;
                }

                BossSummonPresentation summon = root.GetComponent<BossSummonPresentation>();
                if (summon == null) summon = root.AddComponent<BossSummonPresentation>();
                SerializedObject serialized = new SerializedObject(summon);
                serialized.FindProperty("visualRoot").objectReferenceValue = visual;
                serialized.FindProperty("renderers").ClearArray();
                serialized.FindProperty("delay").floatValue = delay;
                serialized.FindProperty("appearDuration").floatValue = appearDuration;
                serialized.FindProperty("settleDuration").floatValue = settleDuration;
                serialized.FindProperty("startLocalOffset").vector3Value = offset;
                serialized.FindProperty("startScale").vector3Value = scale;
                serialized.FindProperty("startRotation").floatValue = rotation;
                Vector3 impactSquash = new Vector3(1.12f, 0.82f, 1f);
                Vector3 impactPunch = new Vector3(0f, 0.18f, 0f);
                float impactDuration = 0.1f;
                if (prefabPath.Contains("Swordmaster"))
                {
                    impactSquash = new Vector3(1.2f, 0.88f, 1f);
                    impactPunch = new Vector3(0.16f, 0.04f, 0f);
                    impactDuration = 0.08f;
                }
                else if (prefabPath.Contains("LostSoul"))
                {
                    impactSquash = new Vector3(0.9f, 1.15f, 1f);
                    impactPunch = new Vector3(0f, 0.25f, 0f);
                    impactDuration = 0.12f;
                }
                else if (prefabPath.Contains("Volcanus"))
                {
                    impactSquash = new Vector3(1.2f, 0.7f, 1f);
                    impactPunch = new Vector3(0f, 0.12f, 0f);
                    impactDuration = 0.14f;
                }
                else
                {
                    impactSquash = new Vector3(1.18f, 0.78f, 1f);
                    impactPunch = new Vector3(0f, 0.28f, 0f);
                }
                serialized.FindProperty("impactSquash").vector3Value = impactSquash;
                serialized.FindProperty("impactPunch").vector3Value = impactPunch;
                serialized.FindProperty("impactDuration").floatValue = impactDuration;
                serialized.FindProperty("summonTint").colorValue = tint;
                serialized.FindProperty("startAlpha").floatValue = 0f;
                serialized.FindProperty("cameraShake").floatValue = shake;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PhasedBossController controller = root.GetComponent<PhasedBossController>();
                if (controller != null)
                {
                    SerializedObject controllerObject = new SerializedObject(controller);
                    controllerObject.FindProperty("summonPresentation").objectReferenceValue = summon;
                    controllerObject.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureDeath(
            string prefabPath,
            float leadTime,
            float breakDuration,
            float vanishDuration,
            Vector3 offset,
            Vector3 scale,
            float rotation,
            Color tint,
            float shake)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                BossDeathPresentation death = root.GetComponent<BossDeathPresentation>();
                if (death == null) death = root.AddComponent<BossDeathPresentation>();

                SerializedObject serialized = new SerializedObject(death);
                serialized.FindProperty("visualRoot").objectReferenceValue = root.transform;
                serialized.FindProperty("renderers").ClearArray();
                serialized.FindProperty("collectChildRenderersOnPlay").boolValue = true;
                serialized.FindProperty("animationLeadTime").floatValue = leadTime;
                serialized.FindProperty("breakDuration").floatValue = breakDuration;
                serialized.FindProperty("vanishDuration").floatValue = vanishDuration;
                serialized.FindProperty("vanishLocalOffset").vector3Value = offset;
                serialized.FindProperty("vanishScale").vector3Value = scale;
                serialized.FindProperty("vanishRotation").floatValue = rotation;
                float scatter = 0.65f;
                float lift = 0.8f;
                float fragmentRotation = 45f;
                float motionShake = 0.18f;
                int flashes = 2;
                float flashDuration = 0.11f;
                if (prefabPath.Contains("Swordmaster"))
                {
                    scatter = 2.6f;
                    lift = 1.1f;
                    fragmentRotation = 100f;
                    flashes = 3;
                    flashDuration = 0.09f;
                }
                else if (prefabPath.Contains("LostSoul"))
                {
                    scatter = 1.1f;
                    lift = 2.2f;
                    fragmentRotation = 25f;
                    flashDuration = 0.15f;
                }
                else if (prefabPath.Contains("Volcanus"))
                {
                    scatter = 1.8f;
                    lift = 1f;
                    fragmentRotation = 55f;
                    motionShake = 0.28f;
                    flashes = 3;
                    flashDuration = 0.13f;
                }
                serialized.FindProperty("fragmentScatter").floatValue = scatter;
                serialized.FindProperty("fragmentLift").floatValue = lift;
                serialized.FindProperty("fragmentRotation").floatValue = fragmentRotation;
                serialized.FindProperty("shakeStrength").floatValue = motionShake;
                serialized.FindProperty("flashCount").intValue = flashes;
                serialized.FindProperty("flashDuration").floatValue = flashDuration;
                serialized.FindProperty("deathTint").colorValue = tint;
                serialized.FindProperty("cameraShake").floatValue = shake;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PhasedBossController controller = root.GetComponent<PhasedBossController>();
                if (controller != null)
                {
                    SerializedObject controllerObject = new SerializedObject(controller);
                    controllerObject.FindProperty("deathPresentation").objectReferenceValue = death;
                    controllerObject.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform FindDeep(Transform root, string targetName)
        {
            foreach (Transform child in root)
            {
                if (child.name == targetName) return child;
                Transform result = FindDeep(child, targetName);
                if (result != null) return result;
            }
            return null;
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
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
