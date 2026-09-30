using System.Collections.Generic;
using System.IO;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Environment;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Member.ODK.Scripts.EditorTools
{
    [InitializeOnLoad]
    public static class ODKAmbienceBaker
    {
        private const string TextureFolder = "Assets/Member/ODK/04_Texture/Ambience";
        private const string MaterialFolder = "Assets/Member/ODK/06_Mat/Ambience";

        static ODKAmbienceBaker()
        {
            EditorSceneManager.sceneOpened += HandleSceneOpened;
            EditorApplication.delayCall += BakeLoadedScenes;
        }

        private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
        {
            EditorApplication.delayCall += () => BakeScene(scene);
        }

        [MenuItem("ODK/Ambience/Bake Particles In Open Scenes")]
        private static void BakeLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                BakeScene(SceneManager.GetSceneAt(i));
        }

        private static void BakeScene(Scene scene)
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!scene.IsValid() || !scene.isLoaded) return;

            List<ODKArenaAmbience> targets = new List<ODKArenaAmbience>();
            foreach (GameObject rootObject in scene.GetRootGameObjects())
                targets.AddRange(rootObject.GetComponentsInChildren<ODKArenaAmbience>(true));
            if (targets.Count == 0) return;

            bool wasDirty = scene.isDirty;
            foreach (ODKArenaAmbience ambience in targets)
                Bake(scene, ambience);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!wasDirty && !string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ODK] {scene.name} 씬에 앰비언스 파티클 {targets.Count}세트를 배치했어.");
        }

        private static void Bake(Scene scene, ODKArenaAmbience ambience)
        {
            if (ambience == null) return;
            ResolveArea(scene, out Vector2 center, out Vector2 halfSize);

            Transform parent = new GameObject("Ambience Particles").transform;
            parent.SetParent(ambience.transform, false);
            parent.position = new Vector3(center.x, center.y, ambience.transform.position.z);

            List<ParticleSystem> systems = ambience.BakeSystems(parent, halfSize, CreateMaterialAsset);
            foreach (ParticleSystem system in systems)
                if (system != null) EditorUtility.SetDirty(system);

            RegisterWithArenaAmbience(scene, systems);
            Object.DestroyImmediate(ambience);
        }

        private static void ResolveArea(Scene scene, out Vector2 center, out Vector2 halfSize)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                foreach (CinemachineConfiner2D confiner in rootObject.GetComponentsInChildren<CinemachineConfiner2D>(true))
                {
                    if (confiner == null || confiner.BoundingShape2D == null) continue;
                    Bounds bounds = confiner.BoundingShape2D.bounds;
                    center = bounds.center;
                    halfSize = new Vector2(Mathf.Max(4f, bounds.extents.x), Mathf.Max(3f, bounds.extents.y));
                    return;
                }
            }

            Camera camera = Camera.main;
            if (camera != null && camera.orthographic)
            {
                center = camera.transform.position;
                halfSize = new Vector2(camera.orthographicSize * camera.aspect, camera.orthographicSize);
                return;
            }

            center = new Vector2(0f, 3f);
            halfSize = new Vector2(12f, 7f);
        }

        private static void RegisterWithArenaAmbience(Scene scene, List<ParticleSystem> systems)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                foreach (BossArenaAmbience arenaAmbience in rootObject.GetComponentsInChildren<BossArenaAmbience>(true))
                {
                    SerializedObject serialized = new SerializedObject(arenaAmbience);
                    SerializedProperty list = serialized.FindProperty("ambientParticles");
                    if (list == null) continue;
                    foreach (ParticleSystem system in systems)
                    {
                        if (system == null) continue;
                        int index = list.arraySize;
                        list.InsertArrayElementAtIndex(index);
                        list.GetArrayElementAtIndex(index).objectReferenceValue = system;
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            foreach (ParticleSystem system in systems)
            {
                if (system == null) continue;
                ParticleSystem.MainModule main = system.main;
                main.playOnAwake = true;
            }
        }

        private static Material CreateMaterialAsset(string materialName, bool additive, Texture2D texture)
        {
            EnsureFolder(TextureFolder);
            EnsureFolder(MaterialFolder);

            string textureName = texture != null ? texture.name.Replace("ODK Ambience ", string.Empty) : "Dot";
            string texturePath = $"{TextureFolder}/Ambience_{textureName}.png";
            if (texture != null && !File.Exists(texturePath))
            {
                File.WriteAllBytes(texturePath, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
                if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
            Texture2D textureAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            string materialPath = $"{MaterialFolder}/Ambience_{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            bool created = false;
            if (material == null)
            {
                material = ODKArenaAmbience.CreateBakeMaterial(additive);
                if (material == null) return null;
                material.hideFlags = HideFlags.None;
                material.name = Path.GetFileNameWithoutExtension(materialPath);
                created = true;
            }

            if (textureAsset != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", textureAsset);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", textureAsset);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);

            if (created) AssetDatabase.CreateAsset(material, materialPath);
            else EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
