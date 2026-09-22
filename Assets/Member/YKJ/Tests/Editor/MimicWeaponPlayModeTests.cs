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
        public IEnumerator TenJumpsSpawnNoRocksAndResumeAfterOneTongue()
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
            Assert.That(Object.FindObjectsByType<MimicHazard>(FindObjectsSortMode.None).Length, Is.Zero);
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
            SetField(boss, "arena", arena);
            var jump = boss.GetComponentInChildren<MimicJumpPattern>();
            jump.Initialize(boss);
            LineRenderer warning = Create("Landing Warning", Vector2.zero).AddComponent<LineRenderer>();
            warning.transform.SetParent(boss.transform);
            warning.enabled = false;
            SetField(jump, "landingWarning", warning);
            return jump;
        }

        [UnityTest]
        public IEnumerator LandingRaisesFeedbackOnceAndNotWhenCancelledInFlight()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-20f, 0f));
            Transform target = Create("Target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            MimicJumpPattern jump = ConfigureJump(boss);
            var channel = ScriptableObject.CreateInstance<KimLIb.EventSystem.EventChannelSO>();
            var feedback = ScriptableObject.CreateInstance<YKJ_Script.Feedbacks.FeedbackSO>();
            feedback.FeedBackId = 123;
            SetField(jump, "feedbackChannel", channel);
            SetField(jump, "landingFeedback", feedback);
            int count = 0;
            channel.AddListener<YKJ_Script.Feedbacks.PlayFeedBack>(evt =>
            {
                Assert.That(evt.FeedbackId, Is.EqualTo(123));
                count++;
            });
            try
            {
                Assert.That(boss.Patterns.Start(jump), Is.True);
                boss.Patterns.Tick(0.35f);
                Assert.That(count, Is.Zero);
                boss.Patterns.Tick(0.75f);
                Assert.That(count, Is.EqualTo(1));
                boss.Patterns.Tick(0.1f);
                Assert.That(count, Is.EqualTo(1));
                boss.Patterns.Cancel();
                Assert.That(boss.Patterns.Start(jump), Is.True);
                boss.Patterns.Tick(0.35f);
                boss.Patterns.Tick(0.1f);
                boss.Patterns.Cancel();
                Assert.That(count, Is.EqualTo(1));
            }
            finally
            {
                channel.Clear();
                Object.Destroy(channel);
                Object.Destroy(feedback);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TongueRestoresChestSpriteAfterCompletionAndCancellation()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-20f, 0f));
            Transform target = Create("Target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            SpriteRenderer visual = Create("Chest", Vector2.zero).AddComponent<SpriteRenderer>();
            visual.transform.SetParent(boss.transform);
            Sprite closed = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            Sprite open = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            try
            {
                visual.sprite = closed;
                SetField(boss.Tongue, "chestRenderer", visual);
                SetField(boss.Tongue, "openChestSprite", open);
                Assert.That(boss.Patterns.Start(boss.Tongue), Is.True);
                Assert.That(visual.sprite, Is.SameAs(open));
                boss.Patterns.Tick(0.5f);
                boss.Patterns.Tick(0.2f);
                boss.Patterns.Tick(0.35f);
                Assert.That(visual.sprite, Is.SameAs(closed));
                Assert.That(boss.Patterns.Start(boss.Tongue), Is.True);
                Assert.That(visual.sprite, Is.SameAs(open));
                boss.Patterns.Cancel();
                Assert.That(visual.sprite, Is.SameAs(closed));
                Assert.That(boss.Patterns.Start(boss.Tongue), Is.True);
                boss.Tongue.enabled = false;
                Assert.That(visual.sprite, Is.SameAs(closed));
                boss.Patterns.Cancel();
                boss.Tongue.enabled = true;
                var treasure = boss.GetComponentInChildren<MimicTreasurePattern>();
                SetField(treasure, "chestRenderer", visual);
                SetField(treasure, "openChestSprite", open);
                Assert.That(boss.Patterns.Start(treasure), Is.True);
                Assert.That(visual.sprite, Is.SameAs(open));
                Assert.That(boss.Patterns.Interrupt(treasure, boss.Tongue), Is.True);
                Assert.That(visual.sprite, Is.SameAs(open));
                boss.Patterns.Complete(boss.Tongue);
                Assert.That(visual.sprite, Is.SameAs(open));
                boss.Patterns.Complete(treasure);
                Assert.That(visual.sprite, Is.SameAs(closed));
                boss.Patterns.Start(treasure);
                boss.Patterns.Interrupt(treasure, boss.Tongue);
                boss.Patterns.Cancel();
                Assert.That(visual.sprite, Is.SameAs(closed));
            }
            finally
            {
                Object.Destroy(closed);
                Object.Destroy(open);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator JumpAlignsColliderBottomWithGroundInsteadOfMarkerHeight()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-20f, 0f));
            Transform target = Create("Target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            var body = boss.gameObject.AddComponent<BoxCollider2D>();
            body.size = new Vector2(2f, 2f);
            body.offset = new Vector2(0f, 0.25f);
            var ground = Create("Landing Ground", new Vector2(0f, -2f)).AddComponent<BoxCollider2D>();
            ground.size = new Vector2(40f, 1f);
            MimicJumpPattern jump = ConfigureJump(boss);
            SetField(jump, "groundSurface", ground);
            SetField(jump, "bossCollider", body);
            Physics2D.SyncTransforms();
            Assert.That(boss.Patterns.Start(jump), Is.True);
            boss.Patterns.Tick(0.35f);
            boss.Patterns.Tick(0.75f);
            Physics2D.SyncTransforms();
            Assert.That(body.bounds.min.y, Is.EqualTo(ground.bounds.max.y).Within(0.001f));
            boss.Patterns.Cancel();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BodyAnticipationKeepsFeetAndColliderFixedAndResetsOnDisable()
        {
            GameObject root = Create("Animated Mimic", new Vector2(2f, 3f));
            var collider = root.AddComponent<BoxCollider2D>();
            GameObject visual = Create("Visual", Vector2.zero);
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            Sprite sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            renderer.sprite = sprite;
            var animator = root.AddComponent<MimicBodyAnimator>();
            SetField(animator, "chestRenderer", renderer);
            try
            {
                Physics2D.SyncTransforms();
                Bounds originalBounds = collider.bounds;
                float originalBottom = renderer.bounds.min.y;
                animator.PrepareJump(0.35f);
                var pose = (DG.Tweening.Sequence)typeof(MimicBodyAnimator)
                    .GetField("_pose", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(animator);
                DG.Tweening.TweenExtensions.Complete(pose, true);
                Physics2D.SyncTransforms();
                Assert.That(visual.transform.localScale.y, Is.EqualTo(0.72f).Within(0.001f));
                Assert.That(renderer.bounds.min.y, Is.EqualTo(originalBottom).Within(0.001f));
                Assert.That(collider.bounds, Is.EqualTo(originalBounds));
                Assert.That(root.transform.position, Is.EqualTo(new Vector3(2f, 3f, 0f)));
                animator.enabled = false;
                Assert.That(visual.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(visual.transform.localPosition, Is.EqualTo(Vector3.zero));
                animator.enabled = true;
                animator.PrepareTreasure(0.5f);
                animator.ResetPose();
                Assert.That(visual.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally { Object.Destroy(sprite); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DamageCallbackCancelsLandingWithoutContinuingPattern()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-20f, 0f));
            Transform target = Create("Target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            MimicBoss receiver = CreateBoss(Vector2.zero, target, weapon);
            receiver.gameObject.layer = LayerMask.NameToLayer("Player");
            receiver.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(30f, 1f);
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
            Assert.That(Object.FindObjectsByType<MimicHazard>(FindObjectsSortMode.None).Length, Is.Zero);
            receiver.StopEncounter();
            yield return null;
        }

        [UnityTest]
        public IEnumerator LaserAccumulatesFiveBeamsThenRotatesOnceAndCleansUp()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-20f, 0f));
            Transform target = Create("Laser target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            MimicBoss receiver = CreateBoss(new Vector2(4f, 0f), target, weapon);
            receiver.gameObject.layer = LayerMask.NameToLayer("Player");
            receiver.gameObject.AddComponent<BoxCollider2D>();
            receiver.BeginEncounter();
            var laser = CreatePattern<MimicLaserPattern>(boss, 6);
            SetField(laser, "laserMaterial", UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Member/YKJ/MimicTestAssets/MimicUnlit.mat"));
            SetField(laser, "angleRange", Vector2.zero);
            boss.InitializePatternDictionary();
            Physics2D.SyncTransforms();
            float before = receiver.HealthModule.CurrentHealth;
            Assert.That(boss.Patterns.Start(laser), Is.True);
            LineRenderer[] lines = laser.GetComponentsInChildren<LineRenderer>();
            Assert.That(lines.Length, Is.EqualTo(5));
            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 direction = lines[i].GetPosition(1) - lines[i].GetPosition(0);
                Vector2 next = lines[(i + 1) % lines.Length].GetPosition(1) - lines[(i + 1) % lines.Length].GetPosition(0);
                Assert.That(Vector2.SignedAngle(direction, next), Is.EqualTo(72f).Within(0.001f));
            }
            for (int i = 0; i < 5; i++)
            {
                Assert.That(laser.FiredCount, Is.EqualTo(i));
                boss.Patterns.Tick(i == 0 ? 0.6f : 0.12f);
                Assert.That(laser.FiredCount, Is.EqualTo(i + 1));
                for (int beam = 0; beam <= i; beam++)
                {
                    Assert.That(lines[beam].enabled, Is.True);
                    Assert.That(lines[beam].startWidth, Is.EqualTo(0.65f).Within(0.001f));
                }
                Assert.That(before - receiver.HealthModule.CurrentHealth, Is.EqualTo(15f));
                Assert.That(laser.RotationDegrees, Is.Zero);
            }
            boss.Patterns.Tick(0.3f);
            Assert.That(laser.RotationDegrees, Is.Zero);
            Assert.That(boss.Patterns.IsRunning, Is.True);
            foreach (LineRenderer line in lines) Assert.That(line.enabled, Is.True);
            boss.Patterns.Tick(0.3f);
            Assert.That(laser.RotationDegrees, Is.Zero);
            boss.Patterns.Tick(1.5f);
            Assert.That(laser.RotationDegrees, Is.EqualTo(-180f).Within(0.001f));
            foreach (LineRenderer line in lines) Assert.That(line.enabled, Is.True);
            boss.Patterns.Tick(1.5f);
            Assert.That(laser.RotationDegrees, Is.EqualTo(-360f).Within(0.001f));
            Assert.That(boss.Patterns.IsRunning, Is.False);
            Assert.That(before - receiver.HealthModule.CurrentHealth, Is.EqualTo(75f));
            foreach (LineRenderer line in lines) Assert.That(line.enabled, Is.False);
            boss.Patterns.Start(laser);
            boss.Patterns.Cancel();
            foreach (LineRenderer line in lines) Assert.That(line.enabled, Is.False);
            receiver.StopEncounter();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossWeaponExcludesPlayerButStillCanBeGrabbed()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-3f, 0f));
            var player = Create("Player body", Vector2.zero);
            player.layer = LayerMask.NameToLayer("Player");
            Collider2D playerCollider = player.AddComponent<BoxCollider2D>();
            MimicBoss boss = CreateBoss(new Vector2(-6f, 0f), player.transform, weapon);
            weapon.GetComponent<Rigidbody2D>().gravityScale = 0f;
            weapon.LaunchFromBoss(boss, new Vector2(3f, 0f), 1f, true);
            Collider2D collider = weapon.GetComponent<Collider2D>();
            Assert.That(collider.excludeLayers.value & LayerMask.GetMask("Player"), Is.Not.Zero);
            Assert.That(collider.includeLayers.value & LayerMask.GetMask("Player"), Is.Zero);
            Assert.That(weapon.gameObject.layer, Is.EqualTo(GrabbableLayer.Index));
            Assert.That(Physics2D.GetIgnoreCollision(collider, playerCollider), Is.True);
            Physics2D.SyncTransforms();
            for (int i = 0; i < 50; i++) Physics2D.Simulate(0.02f);
            Assert.That(weapon.transform.position.x, Is.GreaterThan(2.5f));
            Assert.That(weapon.State, Is.EqualTo(MimicWeapon.WeaponState.BossFlight));
            Assert.That(weapon.CanBeGrabbed, Is.True);
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
            CreatePattern<MimicTreasurePattern>(boss, 1);
            CreatePattern<MimicJumpPattern>(boss, 2);
            MimicTonguePattern tongue = CreatePattern<MimicTonguePattern>(boss, 3);
            SetField(tongue, "tongueLine", line);
            boss.InitializePatternDictionary();
            return boss;
        }

        private T CreatePattern<T>(MimicBoss boss, int id) where T : MimicPattern
        {
            GameObject child = Create(typeof(T).Name, boss.transform.position);
            child.transform.SetParent(boss.transform);
            T pattern = child.AddComponent<T>();
            typeof(MimicPattern).GetField("skillId", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(pattern, id);
            return pattern;
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
