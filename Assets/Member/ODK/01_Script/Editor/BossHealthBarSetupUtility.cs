#if UNITY_EDITOR
using Member.ODK.Scripts;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Member.ODK.Editor
{
    public static class BossHealthBarSetupUtility
    {
        private const string HealthBarPrefabPath =
            "Assets/Member/KYM/03.GameAssets/UI/BossHealthBar.prefab";
        private const string HudFolder = "Assets/Member/ODK/02_Prefab/UI";
        private const string HudPrefabPath = HudFolder + "/BossHealthHUD.prefab";
        private const string HudRootName = "BossHealthHUD";
        private const string TestScenePath =
            "Assets/Member/ODK/08_Scene/ODK_TestBoss1.unity";

        private static readonly string[] BossPrefabPaths =
        {
            "Assets/Member/ODK/02_Prefab/Moon.prefab",
            "Assets/Member/ODK/02_Prefab/Swordmaster.prefab",
            "Assets/Member/ODK/02_Prefab/Boss/LostSoul/LostSoul.prefab"
        };

        [MenuItem("Tools/ODK/Connect Boss Health Bars")]
        public static void ConnectBossHealthBars()
        {
            foreach (string bossPrefabPath in BossPrefabPaths)
                AddBinding(bossPrefabPath);

            GameObject hudPrefab = CreateHudPrefab();
            PlaceHudInTestScene(hudPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Moon, Swordmaster, Lost Soul health bars are connected.");
        }

        private static void AddBinding(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                BossHealthBarBinding binding =
                    root.GetComponent<BossHealthBarBinding>();
                if (binding == null)
                    binding = root.AddComponent<BossHealthBarBinding>();

                HealthModule health = root.GetComponent<HealthModule>();
                if (health == null)
                    health = root.GetComponentInChildren<HealthModule>(true);

                SerializedObject bindingObject = new SerializedObject(binding);
                bindingObject.FindProperty("healthModule").objectReferenceValue = health;
                bindingObject.ApplyModifiedPropertiesWithoutUndo();

                PhasedBossController controller =
                    root.GetComponent<PhasedBossController>();
                if (controller != null)
                {
                    SerializedObject controllerObject = new SerializedObject(controller);
                    controllerObject.FindProperty("healthBarBinding").objectReferenceValue =
                        binding;
                    controllerObject.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject CreateHudPrefab()
        {
            EnsureFolder(HudFolder);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            if (existing != null)
                return existing;

            GameObject sourceBar = AssetDatabase.LoadAssetAtPath<GameObject>(
                HealthBarPrefabPath
            );
            if (sourceBar == null)
                throw new MissingReferenceException(HealthBarPrefabPath);

            GameObject root = new GameObject(
                HudRootName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

            try
            {
                int uiLayer = LayerMask.NameToLayer("UI");
                if (uiLayer >= 0)
                    root.layer = uiLayer;

                Canvas canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;

                CanvasScaler scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                GameObject bar = (GameObject)PrefabUtility.InstantiatePrefab(
                    sourceBar,
                    root.transform
                );
                bar.name = "BossHealthBar";

                RectTransform rect = bar.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 40f);
                rect.sizeDelta = new Vector2(1075f, 150f);

                return PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void PlaceHudInTestScene(GameObject hudPrefab)
        {
            Scene scene = SceneManager.GetSceneByPath(TestScenePath);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(
                    TestScenePath,
                    OpenSceneMode.Additive
                );
            }

            bool hasHud = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != HudRootName)
                    continue;

                hasHud = true;
                break;
            }

            if (!hasHud)
                PrefabUtility.InstantiatePrefab(hudPrefab, scene);

            EditorSceneManager.SaveScene(scene);
            if (!wasLoaded)
                EditorSceneManager.CloseScene(scene, true);
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
