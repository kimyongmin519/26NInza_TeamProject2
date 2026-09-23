#if UNITY_INCLUDE_TESTS
using System.Reflection;
using Member.YKJ.Bosses;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Member.YKJ.Tests
{
    public sealed class MimicCoinRainTests
    {
        [Test]
        public void CoinPrefabHasFallingOnlyDamageAndTriggerCollider()
        {
            var coin = AssetDatabase.LoadAssetAtPath<MimicHazard>(
                "Assets/Member/YKJ/MimicTestAssets/MimicCoin.prefab");
            Assert.That(coin, Is.Not.Null);
            Assert.That(coin.GetComponent<Collider2D>().isTrigger, Is.True);
            Assert.That(coin.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(coin.GetComponentInChildren<SpriteRenderer>().sprite, Is.Not.Null);
            Assert.That(new SerializedObject(coin).FindProperty("descendingOnly").boolValue, Is.True);
        }

        [Test]
        public void CoinPatternOpensChestAndRestoresItWhenCancelled()
        {
            var root = new GameObject("Coin pattern test");
            root.SetActive(false);
            try
            {
                MimicBoss boss = root.AddComponent<MimicBoss>();
                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform);
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                Sprite closed = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath("c63f875e980ac814ea9040291818fbc2"));
                Sprite opened = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath("b4f1c0a0e5e9c2f4b95a141d9fc5354a"));
                Assert.That(closed, Is.Not.Null);
                Assert.That(opened, Is.Not.Null);
                renderer.sprite = closed;
                MimicCoinRainPattern pattern = root.AddComponent<MimicCoinRainPattern>();
                pattern.Initialize(boss);
                Set(pattern, "chestRenderer", renderer);
                Set(pattern, "openChestSprite", opened);
                pattern.OnStart();
                Assert.That(renderer.sprite, Is.SameAs(opened));
                Assert.That(pattern.EmittedCount, Is.Zero);
                pattern.OnDie();
                pattern.OnEnd();
                Assert.That(renderer.sprite, Is.SameAs(closed));
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
#endif
