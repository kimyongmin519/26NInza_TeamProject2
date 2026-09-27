#if UNITY_INCLUDE_TESTS
using System.Linq;
using Member.YKJ.Bosses;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Member.YKJ.Tests
{
    public sealed class MimicBossSceneTests
    {
        [Test]
        public void SavedBossTestKOUIIsBoundToMimicAndHiddenUntilDeath()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Member/YKJ/Scene/BossTest.unity");
            try
            {
                var roots = scene.GetRootGameObjects();
                var boss = roots.SelectMany(root => root.GetComponentsInChildren<MimicBoss>(true)).Single();
                var ko = roots.SelectMany(root => root.GetComponentsInChildren<Member.KYM.Scripts.UI.BossKOUI>(true)).Single();
                var settings = new SerializedObject(ko);
                Assert.That(ko.isActiveAndEnabled, Is.True, "The controller must stay active to receive death events.");
                Assert.That(settings.FindProperty("healthModule").objectReferenceValue,
                    Is.SameAs(boss.GetComponent<Member.ODK.Scripts.HealthModule>()));
                RequireReferences(ko, "overlayRoot", "darkBackground", "koImage");
                var overlay = (GameObject)settings.FindProperty("overlayRoot").objectReferenceValue;
                Assert.That(overlay, Is.Not.SameAs(ko.gameObject));
                Assert.That(overlay.activeSelf, Is.False);
                var image = (UnityEngine.UI.Image)settings.FindProperty("koImage").objectReferenceValue;
                Assert.That(image.sprite, Is.Not.Null);
                Assert.That(image.type, Is.EqualTo(UnityEngine.UI.Image.Type.Filled));
                Assert.That(ko.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(ko.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void SavedBossTestHasStartableTransitionAndPhaseTwoWithPolishConnected()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Member/YKJ/Scene/BossTest.unity");
            try
            {
                var roots = scene.GetRootGameObjects();
                MimicBoss boss = roots.SelectMany(root => root.GetComponentsInChildren<MimicBoss>(true)).Single();
                Assert.That(boss.InitializePatternDictionary(), Is.True);
                var settings = new SerializedObject(boss);
                int coinId = settings.FindProperty("coinRainSkillId").intValue;
                Assert.That(boss.GetPattern(coinId), Is.TypeOf<MimicCoinRainPattern>());
                Assert.That(boss.GetPattern(coinId).CanStart(), Is.True, "Coin transition references are missing.");
                SerializedProperty phaseTwo = settings.FindProperty("phaseTwoSkillIds");
                Assert.That(phaseTwo.arraySize, Is.GreaterThan(0));
                for (int i = 0; i < phaseTwo.arraySize; i++)
                {
                    int id = phaseTwo.GetArrayElementAtIndex(i).intValue;
                    MimicPattern pattern = boss.GetPattern(id);
                    Assert.That(pattern, Is.Not.Null, $"Phase two skill {id} is not registered.");
                    Assert.That(pattern.CanStart(), Is.True, $"Phase two skill {id} ({pattern.name}) is not configured.");
                }

                Assert.That(boss.BodyAnimator, Is.Not.Null);
                Assert.That(boss.CombatVfx, Is.Not.Null);
                Assert.That(boss.CombatVfx.enabled, Is.True);
                RequireReferences(boss.CombatVfx, "boss", "chestRenderer", "bodyCollider", "glowMaterial", "cameraChannel");
                var particles = boss.GetComponent<MimicParticleFeedback>();
                Assert.That(particles, Is.Not.Null);
                Assert.That(particles.enabled, Is.True);
                RequireReferences(particles, "boss", "bodyCollider", "feedbackChannel", "spitFeedback",
                    "landingFeedback", "dustPrefab", "sparkMaterial", "cloudMaterial");
                var jump = boss.GetComponentsInChildren<MimicJumpPattern>(true).Single();
                RequireReferences(jump, "afterimage");
                var laser = boss.GetComponentsInChildren<MimicLaserPattern>(true).Single();
                RequireReferences(laser, "chestRenderer", "openChestSprite", "laserMaterial");
                var tongue = new SerializedObject(boss.Tongue).FindProperty("tongueLine").objectReferenceValue as LineRenderer;
                Assert.That(new SerializedObject(laser).FindProperty("laserMaterial").objectReferenceValue,
                    Is.SameAs(tongue.sharedMaterial), "Both phases should share the red laser material.");
                var treasure = boss.GetComponentsInChildren<MimicTreasurePattern>(true).Single();
                var feedback = new SerializedObject(particles);
                Assert.That(new SerializedObject(treasure).FindProperty("feedbackChannel").objectReferenceValue,
                    Is.SameAs(feedback.FindProperty("feedbackChannel").objectReferenceValue));
                Assert.That(new SerializedObject(jump).FindProperty("feedbackChannel").objectReferenceValue,
                    Is.SameAs(feedback.FindProperty("feedbackChannel").objectReferenceValue));

                Camera camera = settings.FindProperty("boomCamera").objectReferenceValue as Camera;
                Assert.That(camera, Is.Not.Null);
                Assert.That(camera.allowHDR, Is.True);
                Assert.That(camera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing, Is.True);
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<Volume>(true)).Any(volume =>
                    volume.enabled && volume.isGlobal && volume.weight > 0f && volume.sharedProfile != null &&
                    volume.sharedProfile.TryGet(out Bloom bloom) && bloom.active && bloom.intensity.overrideState &&
                    bloom.intensity.value > 0f), Is.True, "An active Bloom profile is required for the glow effects.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static void RequireReferences(Object target, params string[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach (string field in fields)
                Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null,
                    $"{target.GetType().Name}.{field} is not connected in BossTest.");
        }
    }
}
#endif
