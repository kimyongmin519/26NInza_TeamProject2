#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Players.RobotArm;
using Member.YKJ.Bosses;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Member.YKJ.Tests
{
    public sealed class MimicWeaponPlayModeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private SimulationMode2D _simulationMode;

        [UnitySetUp]
        public IEnumerator EnterPlayMode()
        {
            yield return new EnterPlayMode();
            _simulationMode = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        [UnityTearDown]
        public IEnumerator ExitPlayMode()
        {
            foreach (GameObject item in _objects)
                if (item != null) Object.Destroy(item);
            _objects.Clear();
            Physics2D.simulationMode = _simulationMode;
            yield return null;
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator TreasureSurvivesLandingAndRemainsGrabbable()
        {
            GameObject ground = Create("Ground", new Vector2(0f, -1f));
            ground.layer = LayerMask.NameToLayer("Ground");
            ground.AddComponent<BoxCollider2D>().size = new Vector2(30f, 1f);
            MimicWeapon weapon = CreateWeapon(new Vector2(-4f, 3f));
            weapon.LaunchFromBoss(null, new Vector2(0f, 0f), 0.75f, false);
            Physics2D.SyncTransforms();
            for (int i = 0; i < 150; i++) Physics2D.Simulate(0.02f);
            Assert.That(weapon.gameObject.activeSelf, Is.True);
            Assert.That(weapon.State, Is.EqualTo(MimicWeapon.WeaponState.Grounded));
            Assert.That(((IGrabbable)weapon).CanBeGrabbed, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TreasurePassesUpThroughFlatThenRestsOnItsTop()
        {
            GameObject platform = Create("One Way Platform", new Vector2(0f, 2f));
            platform.layer = LayerMask.NameToLayer("Flat");
            var surface = platform.AddComponent<BoxCollider2D>();
            surface.size = new Vector2(6f, 0.2f);
            surface.usedByEffector = true;
            var effector = platform.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useColliderMask = false;
            MimicWeapon weapon = CreateWeapon(Vector2.zero);
            weapon.GetComponent<Rigidbody2D>().gravityScale = 1.5f;
            bool ignoredGlobally = Physics2D.GetIgnoreLayerCollision(weapon.gameObject.layer, platform.layer);
            weapon.LaunchToSurfaceFromBoss(null, new Vector2(2f, 2.22f), 2f, false);
            Physics2D.SyncTransforms();
            float peak = 0f;
            for (int i = 0; i < 200; i++)
            {
                Physics2D.Simulate(0.02f);
                peak = Mathf.Max(peak, weapon.transform.position.y);
            }
            Assert.That(peak, Is.GreaterThan(4f));
            Assert.That(weapon.State, Is.EqualTo(MimicWeapon.WeaponState.Grounded));
            Assert.That(weapon.transform.position.y, Is.InRange(2.15f, 2.3f));
            Assert.That(weapon.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(weapon.CanBeGrabbed, Is.True);
            Assert.That(Physics2D.GetIgnoreLayerCollision(weapon.gameObject.layer, platform.layer), Is.EqualTo(ignoredGlobally));
            yield return null;
        }

        [UnityTest]
        public IEnumerator GrabReleaseAndThrowDamagesBossExactlyOnce()
        {
            MimicWeapon weapon = CreateWeapon(Vector2.zero);
            var owner = Create("Throw Owner", new Vector2(-3f, 0f)).AddComponent<Agent>();
            Transform hold = Create("Hand", Vector2.zero).transform;
            MimicBoss boss = CreateBoss(new Vector2(4f, 0f), owner.transform, weapon);
            boss.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;
            boss.BeginEncounter();

            weapon.Grab(hold, owner.gameObject);
            Assert.That(weapon.IsHeld, Is.True);
            weapon.Release();
            Assert.That(weapon.State, Is.EqualTo(MimicWeapon.WeaponState.Grounded));
            Assert.That(weapon.gameObject.activeSelf, Is.True);
            weapon.Grab(hold, owner.gameObject);
            weapon.GetComponent<Rigidbody2D>().gravityScale = 0f;
            weapon.Throw(new ThrowData(Vector2.right, owner, 20f));
            // RestorePhysicsState restores the pre-grab gravity; disable gravity for this horizontal test.
            weapon.GetComponent<Rigidbody2D>().gravityScale = 0f;
            float before = boss.HealthModule.CurrentHealth;
            Physics2D.SyncTransforms();
            for (int i = 0; i < 30; i++) Physics2D.Simulate(0.02f);
            Assert.That(before - boss.HealthModule.CurrentHealth, Is.EqualTo(80f));
            Assert.That(weapon.State, Is.EqualTo(MimicWeapon.WeaponState.Spent));
            Assert.That(weapon.gameObject.activeSelf, Is.False);
            boss.StopEncounter();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RetiringHeldWeaponDoesNotInvalidateRobotArmReference()
        {
            MimicWeapon weapon = CreateWeapon(Vector2.zero);
            Transform hold = Create("Hand", Vector2.zero).transform;
            weapon.Grab(hold, null);
            weapon.Retire();
            Assert.That(weapon.IsHeld, Is.True);
            Assert.That(weapon.gameObject.activeSelf, Is.True);
            weapon.Release();
            Assert.That(weapon.gameObject.activeSelf, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CancellingTongueDropsCapturedWeaponsAndHidesLine()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(2f, 0f));
            Transform target = Create("Target", new Vector2(4f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            Assert.That(boss.Patterns.Start(boss.Tongue), Is.True);
            Physics2D.SyncTransforms();
            boss.Patterns.Tick(0.5f);
            boss.Patterns.Tick(0.2f);
            Assert.That(weapon.IsHeld, Is.True);
            boss.Patterns.Cancel();
            Assert.That(weapon.IsHeld, Is.False);
            Assert.That(weapon.gameObject.activeSelf, Is.True);
            Assert.That(boss.GetComponentInChildren<LineRenderer>().enabled, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompletedTongueConsumesCapturedWeapon()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(2f, 0f));
            Transform target = Create("Target", new Vector2(4f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            boss.Patterns.Start(boss.Tongue);
            Physics2D.SyncTransforms();
            boss.Patterns.Tick(0.5f);
            boss.Patterns.Tick(0.2f);
            boss.Patterns.Tick(0.35f);
            Assert.That(weapon.IsHeld, Is.False);
            Assert.That(weapon.gameObject.activeSelf, Is.False);
            Assert.That(boss.Patterns.IsRunning, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TenJumpsSpawnThirtyRocksAndResumeAfterOneTongue()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-20f, 0f));
            Transform target = Create("Target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            MimicJumpPattern jump = ConfigureJump(boss);
            int interrupts = 0;
            Assert.That(boss.Patterns.Start(jump), Is.True);
            for (int i = 0; i < 100 && boss.Patterns.IsRunning; i++)
            {
                if (boss.Patterns.Current == boss.Tongue)
                {
                    interrupts++;
                    Assert.That(jump.LandedCount, Is.EqualTo(5));
                    boss.Patterns.Tick(0.5f);
                    boss.Patterns.Tick(0.2f);
                    boss.Patterns.Tick(0.35f);
                    Assert.That(boss.Patterns.Current, Is.SameAs(jump));
                    Assert.That(jump.LandedCount, Is.EqualTo(5));
                }
                else boss.Patterns.Tick(1f);
            }
            Assert.That(jump.LandedCount, Is.EqualTo(10));
            Assert.That(interrupts, Is.EqualTo(1));
            Assert.That(boss.Patterns.IsRunning, Is.False);
            Assert.That(Object.FindObjectsByType<MimicHazard>(FindObjectsSortMode.None).Length, Is.EqualTo(31));
            boss.StopEncounter();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CancellingMidJumpRestoresStablePositionAndHidesWarning()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-20f, 0f));
            Transform target = Create("Target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            MimicJumpPattern jump = ConfigureJump(boss);
            boss.Patterns.Start(jump);
            boss.Patterns.Tick(0.35f);
            boss.Patterns.Tick(0.2f);
            Assert.That(boss.transform.position.y, Is.GreaterThan(0f));
            boss.Patterns.Cancel();
            Assert.That(boss.transform.position, Is.EqualTo(Vector3.zero));
            foreach (LineRenderer line in boss.GetComponentsInChildren<LineRenderer>())
                Assert.That(line.enabled, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FallingRockBreaksOnGround()
        {
            GameObject ground = Create("Ground", new Vector2(0f, -1f));
            ground.layer = LayerMask.NameToLayer("Ground");
            ground.AddComponent<BoxCollider2D>().size = new Vector2(10f, 1f);
            GameObject rock = Create("Rock", new Vector2(0f, 2f));
            rock.AddComponent<CircleCollider2D>().radius = 0.2f;
            var hazard = rock.AddComponent<MimicHazard>();
            hazard.Launch(null, Vector2.zero, 1.5f, 10f, 8f);
            Physics2D.SyncTransforms();
            for (int i = 0; i < 100; i++) Physics2D.Simulate(0.02f);
            Assert.That(rock.activeSelf, Is.False);
            yield return null;
        }

        private MimicJumpPattern ConfigureJump(MimicBoss boss)
        {
            MimicArena arena = Create("Arena", Vector2.zero).AddComponent<MimicArena>();
            var zones = new MimicArena.Zone[3];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = new MimicArena.Zone
                {
                    LandingPoint = Create("Landing", new Vector2((i - 1) * 5f, 0f)).transform,
                    RockLeft = Create("Rock Left", new Vector2((i - 1) * 5f - 2f, 10f)).transform,
                    RockRight = Create("Rock Right", new Vector2((i - 1) * 5f + 2f, 10f)).transform
                };
            }
            SetField(arena, "zones", zones);
            GameObject rock = Create("Rock Prefab", new Vector2(-30f, 0f));
            rock.AddComponent<CircleCollider2D>();
            var hazard = rock.AddComponent<MimicHazard>();
            SetField(boss, "arena", arena);
            SetField(boss, "rockPrefab", hazard);
            var jump = new MimicJumpPattern();
            jump.Initialize(boss);
            LineRenderer warning = Create("Landing Warning", Vector2.zero).AddComponent<LineRenderer>();
            warning.transform.SetParent(boss.transform);
            warning.enabled = false;
            SetField(jump, "landingWarning", warning);
            return jump;
        }

        [UnityTest]
        public IEnumerator DamageCallbackCancelsLandingBeforeMoreRocksSpawn()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-20f, 0f));
            Transform target = Create("Target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            MimicBoss receiver = CreateBoss(Vector2.zero, target, weapon);
            receiver.gameObject.layer = LayerMask.NameToLayer("Player");
            receiver.gameObject.AddComponent<BoxCollider2D>();
            receiver.BeginEncounter();
            receiver.HealthModule.OnHealthChanged += (_, __) => boss.StopEncounter();
            MimicJumpPattern jump = ConfigureJump(boss);
            var zones = (MimicArena.Zone[])typeof(MimicArena).GetField("zones",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boss.Arena);
            foreach (MimicArena.Zone zone in zones) zone.LandingPoint.position = Vector3.zero;
            Physics2D.SyncTransforms();
            boss.Patterns.Start(jump);
            boss.Patterns.Tick(0.35f);
            boss.Patterns.Tick(0.75f);
            Assert.That(receiver.HealthModule.CurrentHealth, Is.LessThan(receiver.HealthModule.MaxHealth));
            Assert.That(boss.Patterns.IsRunning, Is.False);
            Assert.That(Object.FindObjectsByType<MimicHazard>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            receiver.StopEncounter();
            yield return null;
        }

        private MimicBoss CreateBoss(Vector2 position, Transform target, MimicWeapon weapon)
        {
            MimicBoss boss = Create("Mimic", position).AddComponent<MimicBoss>();
            Transform mouth = Create("Mouth", position).transform;
            mouth.SetParent(boss.transform);
            LineRenderer line = Create("Tongue", position).AddComponent<LineRenderer>();
            line.transform.SetParent(boss.transform);
            line.enabled = false;
            SetField(boss, "target", target);
            SetField(boss, "mouth", mouth);
            SetField(boss, "landingLeft", target);
            SetField(boss, "landingRight", target);
            Collider2D lower = Create("Lower Platform", new Vector2(-50f, 2f)).AddComponent<BoxCollider2D>();
            Collider2D upper = Create("Upper Platform", new Vector2(-50f, 5f)).AddComponent<BoxCollider2D>();
            SetField(boss, "firstPlatforms", new[] { lower });
            SetField(boss, "secondPlatforms", new[] { upper });
            SetField(boss, "weaponPrefabs", new[] { weapon });
            SetField(boss.Tongue, "tongueLine", line);
            return boss;
        }

        private MimicWeapon CreateWeapon(Vector2 position)
        {
            GameObject item = Create("Weapon", position);
            item.AddComponent<Rigidbody2D>();
            item.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 0.2f);
            return item.AddComponent<MimicWeapon>();
        }

        private GameObject Create(string name, Vector2 position)
        {
            var item = new GameObject(name);
            item.transform.position = position;
            _objects.Add(item);
            return item;
        }

        private static void SetField(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
#endif
