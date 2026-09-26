using System;
using System.Linq;
using Member.KYM.Scripts.Players;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Member.YKJ.Bosses.Editor
{
    public static class MimicBossTestSetup
    {
        private const string ScenePath = "Assets/Member/YKJ/Scene/BossTest.unity";
        private const string AssetFolder = "Assets/Member/YKJ/MimicTestAssets";
        private const string Icons = "Assets/Member/KYM/01.Graphics/Honeti/PixelArtGUI/Textures/Icons/32/";

        [MenuItem("Tools/YKJ/Setup Mimic in BossTest")]
        public static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != ScenePath)
                throw new InvalidOperationException("Open BossTest in Edit Mode first.");

            GameObject bossObject = scene.GetRootGameObjects().Single(go => go.name == "Mimic");
            if (bossObject.GetComponent<MimicBoss>() != null)
                throw new InvalidOperationException("Mimic is already configured. Edit its Inspector instead of resetting it.");
            PlayerController player = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<PlayerController>()).Single();
            Collider2D[] platforms = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<PlatformEffector2D>())
                .Select(effector => effector.GetComponent<Collider2D>()).Where(surface => surface != null)
                .OrderBy(surface => surface.bounds.max.y).ToArray();
            if (platforms.Length != 4)
                throw new InvalidOperationException("BossTest needs two lower and two upper one-way platforms.");
            Sprite chest = LoadSprite(Icons + "Chest01Outlined.png");
            Sprite sword = LoadSprite(Icons + "Sword01Outlined.png");
            Sprite rock = LoadSprite("Assets/Member/KYM/01.Graphics/Environment/Legacy-Fantasy - High Forest 2.3/Assets/Props-Rocks.png");
            Physics2D.SyncTransforms();
            RaycastHit2D floor = Physics2D.Raycast(bossObject.transform.position, Vector2.down, 20f,
                LayerMask.GetMask("Ground"));
            if (floor.collider == null)
                throw new InvalidOperationException("No ground below Mimic. Check the Ground layer/collider.");

            if (!AssetDatabase.IsValidFolder(AssetFolder))
                AssetDatabase.CreateFolder("Assets/Member/YKJ", "MimicTestAssets");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetFolder + "/MimicUnlit.mat");
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) throw new InvalidOperationException("Sprite unlit shader is missing.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, AssetFolder + "/MimicUnlit.mat");
            }
            MimicWeapon weapon = MakeProjectile<MimicWeapon>("MimicTreasureWeapon", sword, material, new Vector2(0.55f, 0.75f));
            MimicHazard hazard = MakeProjectile<MimicHazard>("MimicFallingRock", rock, material, new Vector2(0.65f, 0.55f));

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Configure BossTest Mimic");
            Undo.RecordObject(bossObject, "Mimic layer");
            bossObject.layer = LayerMask.NameToLayer("Boss");
            SpriteRenderer visual = bossObject.transform.Find("Visual").GetComponent<SpriteRenderer>();
            Undo.RecordObjects(new UnityEngine.Object[] { visual, visual.transform, bossObject.transform }, "Mimic visual");
            visual.sprite = chest;
            visual.sharedMaterial = material;
            visual.sortingLayerName = "Agent";
            visual.sortingOrder = 10;
            visual.transform.localScale = Vector3.one * (2.4f / chest.bounds.size.x);
            float height = chest.bounds.size.y * visual.transform.localScale.y;
            float floorY = floor.point.y;
            float centerY = floorY + height * 0.5f + 0.05f;
            bossObject.transform.position = new Vector3(0f, centerY, 0f);
            BoxCollider2D collider = Undo.AddComponent<BoxCollider2D>(bossObject);
            collider.size = new Vector2(2.2f, height * 0.85f);
            Rigidbody2D body = Undo.AddComponent<Rigidbody2D>(bossObject);
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            MimicBoss boss = Undo.AddComponent<MimicBoss>(bossObject);
            MimicBodyAnimator bodyAnimator = Undo.AddComponent<MimicBodyAnimator>(bossObject);
            var bodyData = new SerializedObject(bodyAnimator);
            bodyData.FindProperty("chestRenderer").objectReferenceValue = visual;
            bodyData.ApplyModifiedProperties();
            Transform mouth = Marker("Mouth", bossObject.transform, bossObject.transform.position + Vector3.up * 0.35f);

            Transform arenaRoot = Marker("MimicArena", null, Vector3.zero);
            MimicArena arena = Undo.AddComponent<MimicArena>(arenaRoot.gameObject);
            Transform left = Marker("WeaponLandingLeft", arenaRoot, new Vector3(-8f, floorY + 0.4f, 0f));
            Transform right = Marker("WeaponLandingRight", arenaRoot, new Vector3(8f, floorY + 0.4f, 0f));
            var arenaData = new SerializedObject(arena);
            arenaData.FindProperty("warningVerticalRange").vector2Value = new Vector2(floorY, 8f);
            SerializedProperty zones = arenaData.FindProperty("zones");
            zones.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                Rect bounds = arena.ZoneBounds(i);
                float x = bounds.center.x;
                Transform zone = Marker("Zone" + (i + 1), arenaRoot, Vector3.zero);
                SerializedProperty entry = zones.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("LandingPoint").objectReferenceValue =
                    Marker("LandingPoint", zone, new Vector3(x, centerY, 0f));
                entry.FindPropertyRelative("RockLeft").objectReferenceValue =
                    Marker("RockLeft", zone, new Vector3(bounds.xMin, 6.5f, 0f));
                entry.FindPropertyRelative("RockRight").objectReferenceValue =
                    Marker("RockRight", zone, new Vector3(bounds.xMax, 6.5f, 0f));
            }
            arenaData.ApplyModifiedProperties();

            Material tongueMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetFolder + "/MimicPhaseOneLaser.mat");
            LineRenderer tongue = MakeLine("TongueLine", bossObject.transform, tongueMaterial, 0.18f, Color.magenta);
            LineRenderer warning = MakeLine("LandingWarning", arenaRoot, material, 0.09f, new Color(1f, 0.35f, 0.35f, 0.25f));
            var data = new SerializedObject(boss);
            data.FindProperty("bodyAnimator").objectReferenceValue = bodyAnimator;
            data.FindProperty("boomPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ParticleSystem>(
                "Assets/Epic Toon FX/Prefabs/Combat/Explosions (Text)/Boom.prefab");
            data.FindProperty("boomCamera").objectReferenceValue = Camera.main;
            data.FindProperty("target").objectReferenceValue = player.transform;
            data.FindProperty("playOnStart").boolValue = true;
            data.FindProperty("mouth").objectReferenceValue = mouth;
            SerializedProperty weapons = data.FindProperty("weaponPrefabs");
            weapons.arraySize = 1;
            weapons.GetArrayElementAtIndex(0).objectReferenceValue = weapon;
            data.FindProperty("landingLeft").objectReferenceValue = left;
            data.FindProperty("landingRight").objectReferenceValue = right;
            SerializedProperty first = data.FindProperty("firstPlatforms");
            SerializedProperty second = data.FindProperty("secondPlatforms");
            first.arraySize = second.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                first.GetArrayElementAtIndex(i).objectReferenceValue = platforms[i];
                second.GetArrayElementAtIndex(i).objectReferenceValue = platforms[i + 2];
            }
            data.FindProperty("arena").objectReferenceValue = arena;
            data.FindProperty("rockPrefab").objectReferenceValue = hazard;
            data.ApplyModifiedProperties();
            Transform patterns = Marker("Patterns", bossObject.transform, bossObject.transform.position);
            MimicTreasurePattern treasurePattern = MakePattern<MimicTreasurePattern>("Treasure", patterns, 1);
            var treasureData = new SerializedObject(treasurePattern);
            treasureData.FindProperty("chestRenderer").objectReferenceValue = visual;
            treasureData.FindProperty("feedbackChannel").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<KimLIb.EventSystem.EventChannelSO>("Assets/Member/YKJ/GameAsset/Channel/FeedBackEventChannel.asset");
            treasureData.FindProperty("spitFeedback").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<YKJ_Script.Feedbacks.FeedbackSO>("Assets/Member/YKJ/GameAsset/FeedBackSO/mimicSpit.asset");
            treasureData.FindProperty("openChestSprite").objectReferenceValue = LoadSprite(Icons + "Chest01OpenOutlined.png");
            treasureData.ApplyModifiedProperties();
            MimicJumpPattern jumpPattern = MakePattern<MimicJumpPattern>("Jump", patterns, 2);
            MimicTonguePattern tonguePattern = MakePattern<MimicTonguePattern>("Tongue", patterns, 3);
            MimicCoinRainPattern coinPattern = MakePattern<MimicCoinRainPattern>("CoinRain", patterns, 4);
            var coinData = new SerializedObject(coinPattern);
            coinData.FindProperty("coinPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MimicHazard>(
                "Assets/Member/YKJ/MimicTestAssets/MimicCoin.prefab");
            coinData.FindProperty("chestRenderer").objectReferenceValue = visual;
            coinData.FindProperty("openChestSprite").objectReferenceValue = LoadSprite(Icons + "Chest01OpenOutlined.png");
            coinData.ApplyModifiedProperties();
            MimicFallingWeaponsPattern fallingPattern = MakePattern<MimicFallingWeaponsPattern>("FallingWeapons", patterns, 5);
            var fallingData = new SerializedObject(fallingPattern);
            Collider2D[] lower = platforms.Take(2).OrderBy(item => item.bounds.center.x).ToArray();
            Collider2D[] upper = platforms.Skip(2).OrderBy(item => item.bounds.center.x).ToArray();
            Collider2D[] ordered = { lower[0], upper[0], upper[1], lower[1] };
            SerializedProperty fallingPlatforms = fallingData.FindProperty("platforms");
            fallingPlatforms.arraySize = 4;
            for (int i = 0; i < ordered.Length; i++)
                fallingPlatforms.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
            fallingData.FindProperty("flightCamera").objectReferenceValue = Camera.main;
            fallingData.ApplyModifiedProperties();
            MimicLaserPattern laserPattern = MakePattern<MimicLaserPattern>("Laser", patterns, 6);
            var laserData = new SerializedObject(laserPattern);
            laserData.FindProperty("laserMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>(AssetFolder + "/MimicLaser.mat");
            laserData.ApplyModifiedProperties();
            var jumpData = new SerializedObject(jumpPattern);
            jumpData.FindProperty("landingWarning").objectReferenceValue = warning;
            jumpData.FindProperty("groundSurface").objectReferenceValue = floor.collider;
            jumpData.FindProperty("bossCollider").objectReferenceValue = collider;
            jumpData.FindProperty("feedbackChannel").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<KimLIb.EventSystem.EventChannelSO>("Assets/Member/YKJ/GameAsset/Channel/FeedBackEventChannel.asset");
            jumpData.FindProperty("landingFeedback").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<YKJ_Script.Feedbacks.FeedbackSO>("Assets/Member/YKJ/GameAsset/FeedBackSO/MimicDown.asset");
            jumpData.ApplyModifiedProperties();
            var tongueData = new SerializedObject(tonguePattern);
            tongueData.FindProperty("tongueLine").objectReferenceValue = tongue;
            tongueData.FindProperty("attackColor").colorValue = new Color(1f, 0.02f, 0.02f, 1f);
            tongueData.FindProperty("chestRenderer").objectReferenceValue = visual;
            tongueData.FindProperty("openChestSprite").objectReferenceValue = LoadSprite(Icons + "Chest01OpenOutlined.png");
            tongueData.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.SaveAssetIfDirty(material);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = bossObject;
            Debug.Log($"Mimic BossTest ready. Ground Y={floorY}, boss Y={centerY}, player={player.name}. " +
                "Phase 1: Treasure / Tongue / Jump. At half health: Boom / Coins, then FallingWeapons / Laser.", boss);
        }

        private static T MakePattern<T>(string name, Transform parent, int skillId) where T : MimicPattern
        {
            T pattern = Undo.AddComponent<T>(Marker(name, parent, parent.position).gameObject);
            var data = new SerializedObject(pattern);
            data.FindProperty("skillId").intValue = skillId;
            data.ApplyModifiedProperties();
            return pattern;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(item => item.name, StringComparer.Ordinal).FirstOrDefault();
            if (sprite == null) throw new InvalidOperationException("No sprite at " + path);
            return sprite;
        }

        private static T MakeProjectile<T>(string name, Sprite sprite, Material material, Vector2 size) where T : Component
        {
            string path = AssetFolder + "/" + name + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<T>();
            var root = new GameObject(name);
            try
            {
                root.layer = LayerMask.NameToLayer(typeof(T) == typeof(MimicWeapon) ? "Grabbable" : "Default");
                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sharedMaterial = material;
                renderer.sortingLayerName = "Weapon";
                renderer.sortingOrder = 10;
                visual.transform.localScale = Vector3.one * Mathf.Min(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y);
                Rigidbody2D body = root.AddComponent<Rigidbody2D>();
                body.gravityScale = 1.5f;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                BoxCollider2D projectileCollider = root.AddComponent<BoxCollider2D>();
                projectileCollider.size = size;
                if (typeof(T) == typeof(MimicWeapon))
                {
                    projectileCollider.includeLayers = LayerMask.GetMask("Ground", "Flat");
                    projectileCollider.layerOverridePriority = 1;
                }
                root.AddComponent<T>();
                return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<T>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static Transform Marker(string name, Transform parent, Vector3 position)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go.transform;
        }

        private static LineRenderer MakeLine(string name, Transform parent, Material material, float width, Color color)
        {
            LineRenderer line = Undo.AddComponent<LineRenderer>(Marker(name, parent, Vector3.zero).gameObject);
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            line.startColor = line.endColor = color;
            line.sortingLayerName = "Weapon";
            line.sortingOrder = 20;
            line.positionCount = 0;
            line.enabled = false;
            return line;
        }
    }
}
