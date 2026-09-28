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
                Assert.That(visual.sprite, Is.SameAs(closed));
                Assert.That(boss.Patterns.Interrupt(treasure, boss.Tongue), Is.True);
                Assert.That(visual.sprite, Is.SameAs(open));
                boss.Patterns.Complete(boss.Tongue);
                Assert.That(visual.sprite, Is.SameAs(closed));
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
        public IEnumerator SpitPresentationDoesNotChangeBallisticsAndResetsWhenGrabbed()
        {
            MimicWeapon prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<MimicWeapon>(
                "Assets/Member/YKJ/MimicTestAssets/MimicTreasureWeapon.prefab");
            MimicWeapon weapon = Object.Instantiate(prefab);
            _objects.Add(weapon.gameObject);
            var presentation = weapon.GetComponent<MimicSpitPresentation>();
            var body = weapon.GetComponent<Rigidbody2D>();
            var collider = weapon.GetComponent<BoxCollider2D>();
            var visual = weapon.GetComponentInChildren<SpriteRenderer>();
            Material material = visual.sharedMaterial;
            Vector3 visualScale = visual.transform.localScale;
            Quaternion visualRotation = visual.transform.localRotation;
            weapon.LaunchToSurfaceFromBoss(null, new Vector2(5f, 2f), 1f, false);
            Vector3 position = weapon.transform.position;
            Vector3 scale = weapon.transform.localScale;
            Vector2 velocity = body.linearVelocity;
            Vector2 size = collider.size;
            presentation.Play(position + Vector3.up * 0.1f);
            Assert.That(body.linearVelocity, Is.EqualTo(velocity));
            Assert.That(weapon.transform.position, Is.EqualTo(position));
            Assert.That(weapon.transform.localScale, Is.EqualTo(scale));
            Assert.That(collider.size, Is.EqualTo(size));
            Assert.That(visual.sharedMaterial, Is.Not.SameAs(material));
            Assert.That(visual.transform.localScale, Is.Not.EqualTo(visualScale));
            var sequence = (DG.Tweening.Sequence)typeof(MimicSpitPresentation)
                .GetField("_reveal", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(presentation);
            DG.Tweening.TweenExtensions.Complete(sequence, true);
            Assert.That(visual.transform.localScale, Is.EqualTo(visualScale));
            weapon.Grab(Create("Hand", Vector2.zero).transform, null);
            typeof(MimicSpitPresentation).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(presentation, null);
            Assert.That(visual.sharedMaterial, Is.SameAs(material));
            Assert.That(visual.transform.localScale, Is.EqualTo(visualScale));
            Assert.That(visual.transform.localRotation, Is.EqualTo(visualRotation));
            weapon.Release();
            weapon.Retire();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TreasureOpensForEachShotAndFinishesRecoilBeforeTongueInterrupt()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-20f, 0f));
            Transform target = Create("Target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            var treasure = boss.GetComponentInChildren<MimicTreasurePattern>();
            SpriteRenderer visual = Create("Chest", Vector2.zero).AddComponent<SpriteRenderer>();
            visual.transform.SetParent(boss.transform, false);
            Sprite closed = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            Sprite open = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            visual.sprite = closed;
            SetField(treasure, "chestRenderer", visual);
            SetField(treasure, "openChestSprite", open);
            try
            {
                Assert.That(boss.Patterns.Start(treasure), Is.True);
                Assert.That(visual.sprite, Is.SameAs(closed));
                boss.Patterns.Tick(0.36f);
                Assert.That(treasure.EmittedCount, Is.Zero);
                boss.Patterns.Tick(0.14f);
                Assert.That(treasure.EmittedCount, Is.EqualTo(1));
                Assert.That(visual.sprite, Is.SameAs(open));
                boss.Patterns.Tick(0.2f);
                Assert.That(visual.sprite, Is.SameAs(closed));
                boss.Patterns.Tick(0.3f);
                Assert.That(treasure.EmittedCount, Is.EqualTo(2));
                for (int i = 0; i < 3; i++) boss.Patterns.Tick(0.5f);
                Assert.That(treasure.EmittedCount, Is.EqualTo(5));
                Assert.That(boss.Patterns.Current, Is.SameAs(treasure));
                boss.Patterns.Tick(0.2f);
                Assert.That(boss.Patterns.Current, Is.SameAs(boss.Tongue));
                boss.Patterns.Complete(boss.Tongue);
                Assert.That(visual.sprite, Is.SameAs(closed));
                boss.Patterns.Tick(0.5f);
                Assert.That(treasure.EmittedCount, Is.EqualTo(6));
                boss.Patterns.Cancel();
                Assert.That(visual.sprite, Is.SameAs(closed));
            }
            finally
            {
                boss.StopEncounter();
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
                boss.Patterns.Tick(i == 0 ? 1f : 0.12f);
                Assert.That(laser.FiredCount, Is.EqualTo(i + 1));
                for (int beam = 0; beam <= i; beam++)
                {
                    Assert.That(lines[beam].enabled, Is.True);
                    Assert.That(lines[beam].startWidth, Is.EqualTo(0.35f).Within(0.001f));
                }
                Assert.That(receiver.HealthModule.CurrentHealth, Is.EqualTo(before));
                Assert.That(laser.IsDamaging, Is.False);
                Assert.That(laser.RotationDegrees, Is.Zero);
            }
            boss.Patterns.Tick(0.6f);
            Assert.That(laser.RotationDegrees, Is.Zero);
            Assert.That(laser.IsDamaging, Is.False);
            Assert.That(receiver.HealthModule.CurrentHealth, Is.EqualTo(before));
            Assert.That(boss.Patterns.IsRunning, Is.True);
            foreach (LineRenderer line in lines) Assert.That(line.enabled, Is.True);
            boss.Patterns.Tick(0.6f);
            Assert.That(laser.RotationDegrees, Is.Zero);
            Assert.That(laser.IsDamaging, Is.True);
            Assert.That(before - receiver.HealthModule.CurrentHealth, Is.EqualTo(1f));
            boss.Patterns.Tick(7f);
            Assert.That(laser.RotationDegrees, Is.EqualTo(-180f).Within(0.001f));
            foreach (LineRenderer line in lines) Assert.That(line.enabled, Is.True);
            boss.Patterns.Tick(7f);
            Assert.That(laser.RotationDegrees, Is.EqualTo(-360f).Within(0.001f));
            Assert.That(boss.Patterns.IsRunning, Is.False);
            Assert.That(before - receiver.HealthModule.CurrentHealth, Is.EqualTo(5f));
            Assert.That(laser.IsDamaging, Is.False);
            foreach (LineRenderer line in lines) Assert.That(line.enabled, Is.False);
            boss.Patterns.Start(laser);
            boss.Patterns.Cancel();
            foreach (LineRenderer line in lines) Assert.That(line.enabled, Is.False);
            receiver.StopEncounter();
            yield return null;
        }

        [UnityTest]
        public IEnumerator LaserOpensChestAnimatesOnlyVisualAndRestoresAfterAllExitPaths()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-20f, 0f));
            Transform target = Create("Laser target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            var collider = boss.gameObject.AddComponent<BoxCollider2D>();
            SpriteRenderer visual = Create("Laser chest", Vector2.zero).AddComponent<SpriteRenderer>();
            visual.transform.SetParent(boss.transform, false);
            Sprite closed = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            Sprite open = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 0.75f);
            visual.sprite = closed;
            var animator = boss.gameObject.AddComponent<MimicBodyAnimator>();
            SetField(animator, "chestRenderer", visual);
            SetField(boss, "bodyAnimator", animator);
            var laser = CreatePattern<MimicLaserPattern>(boss, 6);
            SetField(laser, "laserMaterial", UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Member/YKJ/MimicTestAssets/MimicPhaseOneLaser.mat"));
            SetField(laser, "chestRenderer", visual);
            SetField(laser, "openChestSprite", open);
            boss.InitializePatternDictionary();
            Physics2D.SyncTransforms();
            Bounds bodyBounds = collider.bounds;
            float feet = visual.bounds.min.y;
            try
            {
                for (int exit = 0; exit < 4; exit++)
                {
                    laser.enabled = true;
                    Assert.That(boss.Patterns.Start(laser), Is.True);
                    Assert.That(visual.sprite, Is.SameAs(closed));
                    boss.Patterns.Tick(1f);
                    Assert.That(visual.sprite, Is.SameAs(open));
                    var recoil = (DG.Tweening.Sequence)typeof(MimicBodyAnimator)
                        .GetField("_pose", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(animator);
                    DG.Tweening.TweenExtensions.Complete(recoil, true);
                    Assert.That(visual.transform.localScale.x, Is.EqualTo(0.88f).Within(0.001f));
                    Assert.That(visual.transform.localScale.y, Is.EqualTo(1.2f).Within(0.001f));
                    Assert.That(visual.bounds.min.y, Is.EqualTo(feet).Within(0.001f));
                    Physics2D.SyncTransforms();
                    Assert.That(collider.bounds, Is.EqualTo(bodyBounds));
                    for (int i = 1; i < 5; i++) boss.Patterns.Tick(0.12f);
                    boss.Patterns.Tick(1.2f);
                    var hold = (DG.Tweening.Sequence)typeof(MimicBodyAnimator)
                        .GetField("_pose", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(animator);
                    Assert.That(DG.Tweening.TweenExtensions.IsActive(hold), Is.True);
                    Assert.That(visual.sprite, Is.SameAs(open));

                    if (exit == 0) boss.Patterns.Tick(14f);
                    else if (exit == 1) boss.Patterns.Cancel();
                    else if (exit == 2) laser.enabled = false;
                    else boss.Patterns.Cancel(true);

                    Assert.That(visual.sprite, Is.SameAs(closed));
                    Assert.That(visual.transform.localScale, Is.EqualTo(Vector3.one));
                    Assert.That(visual.transform.localPosition, Is.EqualTo(Vector3.zero));
                    Assert.That(DG.Tweening.TweenExtensions.IsActive(hold), Is.False);
                    foreach (LineRenderer line in laser.GetComponentsInChildren<LineRenderer>())
                        Assert.That(line.enabled, Is.False);
                    boss.Patterns.Cancel();
                }
            }
            finally
            {
                boss.StopEncounter();
                Object.Destroy(closed);
                Object.Destroy(open);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator FallingWeaponsWarnOnCorrectScreenEdgeBeforeEveryShotAndCleanUp()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-30f, 0f));
            Transform target = Create("Target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            Camera view = Create("Warning camera", Vector2.zero).AddComponent<Camera>();
            view.transform.position = new Vector3(0f, 0f, -10f);
            view.orthographic = true;
            view.orthographicSize = 8f;
            view.aspect = 16f / 9f;
            var platforms = new Collider2D[4];
            Vector2[] positions = { new Vector2(-6f, -2f), new Vector2(-6f, 2f),
                new Vector2(6f, 2f), new Vector2(6f, -2f) };
            for (int i = 0; i < platforms.Length; i++)
            {
                var platform = Create("Warning platform " + i, positions[i]).AddComponent<BoxCollider2D>();
                platform.size = new Vector2(4f, 0.2f);
                platforms[i] = platform;
            }
            var falling = CreatePattern<MimicFallingWeaponsPattern>(boss, 5);
            SetField(falling, "platforms", platforms);
            SetField(falling, "flightCamera", view);
            SetField(falling, "warningMaterial", UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Member/YKJ/MimicTestAssets/MimicUnlit.mat"));
            boss.InitializePatternDictionary();
            MimicJumpPattern concurrentJump = ConfigureJump(boss);
            Physics2D.SyncTransforms();
            try
            {
                foreach (bool reverse in new[] { false, true })
                {
                    SetField(falling, "jumpPattern", reverse ? concurrentJump : null);
                    Assert.That(boss.Patterns.Start(falling), Is.True);
                    Assert.That(concurrentJump.IsRunningAlongside(falling), Is.EqualTo(reverse));
                    SetField(falling, "_reverse", reverse);
                    var line = falling.GetComponentInChildren<LineRenderer>();
                    Assert.That(line.enabled, Is.True);
                    for (int shot = 0; shot < 4; shot++)
                    {
                        if (shot > 0) boss.Patterns.Tick(0.2f);
                        boss.Patterns.Tick(0.01f);
                        Assert.That(falling.IsWarning, Is.True);
                        Assert.That(falling.EmittedCount, Is.EqualTo(shot));
                        int index = MimicFallingWeaponsPattern.PlatformIndex(shot, reverse);
                        bool left = index < 2;
                        Vector3 tip = view.WorldToViewportPoint(line.GetPosition(1));
                        Assert.That(tip.x, left ? Is.LessThan(0.2f) : Is.GreaterThan(0.8f));
                        Assert.That((line.GetPosition(1).x - line.GetPosition(0).x) * (left ? 1f : -1f), Is.GreaterThan(0f));
                        Assert.That(line.startColor.r, Is.EqualTo(1f));
                        Assert.That(line.startColor.g, Is.LessThan(0.1f));
                        view.transform.position += Vector3.right;
                        boss.Patterns.Tick(0.3f);
                        Vector3 movedTip = view.WorldToViewportPoint(line.GetPosition(1));
                        Assert.That(movedTip.x, Is.EqualTo(tip.x).Within(0.001f));
                        boss.Patterns.Tick(0.31f);
                        Assert.That(falling.EmittedCount, Is.EqualTo(shot));
                        boss.Patterns.Tick(0.04f);
                        Assert.That(falling.EmittedCount, Is.EqualTo(shot + 1));
                        Assert.That(falling.IsWarning, Is.False);
                        var wave = (MimicWeapon[])typeof(MimicFallingWeaponsPattern)
                            .GetField("_wave", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(falling);
                        Assert.That(wave[shot].transform.position.y, Is.EqualTo(platforms[index].bounds.max.y + 1f).Within(0.001f));
                        float spawnX = view.WorldToViewportPoint(wave[shot].transform.position).x;
                        Assert.That(spawnX, left ? Is.LessThan(0f) : Is.GreaterThan(1f));
                        wave[shot].Retire();
                    }
                    boss.Patterns.Tick(0.1f);
                    if (reverse)
                    {
                        Assert.That(concurrentJump.LandedCount, Is.GreaterThan(0));
                        for (int tick = 0; tick < 60 && boss.Patterns.IsRunning; tick++)
                        {
                            Assert.That(boss.Patterns.Current, Is.SameAs(falling), "Concurrent jumping must not interrupt with tongue.");
                            boss.Patterns.Tick(0.05f);
                        }
                        Assert.That(concurrentJump.IsRunningAlongside(falling), Is.False);
                    }
                    Assert.That(boss.Patterns.IsRunning, Is.False);
                    Assert.That(line.enabled, Is.False);
                    Assert.That(boss.Patterns.Start(falling), Is.True);
                    Assert.That(falling.IsWarning, Is.True);
                    if (reverse)
                    {
                        boss.Patterns.Tick(0.35f);
                        boss.Patterns.Tick(0.1f);
                        Assert.That(concurrentJump.IsRunningAlongside(falling), Is.True);
                    }
                    boss.Patterns.Cancel(true);
                    Assert.That(concurrentJump.IsRunningAlongside(falling), Is.False);
                    Assert.That(line.enabled, Is.False);
                    Assert.That(boss.Patterns.Start(falling), Is.True);
                    falling.enabled = false;
                    Assert.That(line.enabled, Is.False);
                    boss.Patterns.Cancel();
                    falling.enabled = true;
                    Assert.That(falling.GetComponentsInChildren<LineRenderer>().Length, Is.EqualTo(1));
                }
            }
            finally { boss.StopEncounter(); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CombatPolishIsCosmeticBoundedReusableAndCleansUp()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-20f, 0f));
            Transform target = Create("Target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            MimicCombatVfx vfx = ConfigureCombatVfx(boss);
            SetField(vfx, "maxStrokes", 8);
            float health = boss.HealthModule.CurrentHealth;
            float timeScale = Time.timeScale;
            Random.State random = Random.state;
            vfx.SetCharge(0.5f);
            for (int i = 0; i < 20; i++)
            {
                vfx.LaserShot();
                vfx.Spit();
                vfx.Land();
                vfx.Catch(Vector3.zero);
                vfx.Hit(Vector3.zero);
            }
            vfx.JumpAfterimage();
            vfx.PhaseBreak();
            vfx.DeathBurst();
            Assert.That(Random.state, Is.EqualTo(random));
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
            Assert.That(boss.HealthModule.CurrentHealth, Is.EqualTo(health));
            Transform root = (Transform)typeof(MimicCombatVfx)
                .GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(vfx);
            Assert.That(root.GetComponentsInChildren<LineRenderer>().Length, Is.LessThanOrEqualTo(10));
            Assert.That(root.GetComponentsInChildren<Collider2D>().Length, Is.Zero);
            int count = root.childCount;
            vfx.Clear();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                Assert.That(renderer.enabled, Is.False);
            vfx.LaserShot();
            Assert.That(root.childCount, Is.EqualTo(count));
            vfx.enabled = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                Assert.That(renderer.enabled, Is.False);
            Object.Destroy(vfx);
            yield return null;
            Assert.That(root == null, Is.True);
        }

        [UnityTest]
        public IEnumerator BossHitPolishOnlyRunsForAcceptedDamage()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-20f, 0f));
            Transform target = Create("Target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            MimicCombatVfx vfx = ConfigureCombatVfx(boss);
            var channel = ScriptableObject.CreateInstance<KimLIb.EventSystem.EventChannelSO>();
            SetField(vfx, "cameraChannel", channel);
            int count = 0;
            channel.AddListener<Member.KYM.Scripts.CoreSystems.Events.CameraShakeEvent>(_ => count++);
            try
            {
                boss.BeginEncounter();
                var damage = new DamageData(10f, Member.ODK.Scripts.DamageType.Projectile);
                boss.TakeWeaponDamage(damage, Vector3.right);
                Assert.That(count, Is.EqualTo(1));
                boss.HealthModule.SettingInvisibleTime(1f);
                boss.TakeWeaponDamage(damage, Vector3.right);
                Assert.That(count, Is.EqualTo(1));
                boss.StopEncounter();
                boss.HealthModule.SettingInvisibleTime(0f);
                boss.TakeDamage(damage);
                Assert.That(count, Is.EqualTo(1));
            }
            finally
            {
                boss.StopEncounter();
                channel.Clear();
                Object.Destroy(channel);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossCapturingItsOwnWeaponDoesNotPlayPlayerCatchEffect()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-2f, 0f));
            Transform target = Create("Target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            MimicCombatVfx vfx = ConfigureCombatVfx(boss);
            FieldInfo effectsRoot = typeof(MimicCombatVfx).GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic);
            weapon.LaunchFromBoss(boss, Vector2.right * 2f, 0.8f, false);
            weapon.Grab(boss.transform, boss.gameObject);
            Assert.That(effectsRoot.GetValue(vfx), Is.Null);
            weapon.Release();
            weapon.LaunchFromBoss(boss, Vector2.right * 2f, 0.8f, false);
            weapon.Grab(target, target.gameObject);
            Assert.That(effectsRoot.GetValue(vfx), Is.Not.Null);
            weapon.Release();
            yield return null;
        }

        [UnityTest]
        public IEnumerator WeaponTrailChangesForThrowAndClearsWhenHeldOrRetired()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<MimicWeapon>(
                "Assets/Member/YKJ/MimicTestAssets/MimicTreasureWeapon.prefab");
            MimicWeapon weapon = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            _objects.Add(weapon.gameObject);
            TrailRenderer trail = weapon.GetComponentInChildren<TrailRenderer>();
            Assert.That(trail, Is.Not.Null);
            Vector3 originalScale = weapon.transform.localScale;
            float mass = weapon.GetComponent<Rigidbody2D>().mass;
            weapon.LaunchFromBoss(null, new Vector2(2f, 0f), 0.8f, false);
            Assert.That(trail.emitting, Is.True);
            Assert.That(trail.startColor.r, Is.GreaterThan(trail.startColor.b));
            Transform hand = Create("Hand", Vector2.zero).transform;
            var owner = Create("Owner", Vector2.zero).AddComponent<Agent>();
            weapon.Grab(hand, owner.gameObject);
            Assert.That(trail.emitting, Is.False);
            Assert.That(trail.positionCount, Is.Zero);
            weapon.Throw(new ThrowData(Vector2.right, owner, 20f));
            Assert.That(trail.emitting, Is.True);
            Assert.That(trail.startColor.b, Is.GreaterThan(trail.startColor.r));
            Assert.That(weapon.transform.localScale, Is.EqualTo(originalScale));
            Assert.That(weapon.GetComponent<Rigidbody2D>().mass, Is.EqualTo(mass));
            weapon.Retire();
            Assert.That(trail.emitting, Is.False);
            Assert.That(trail.positionCount, Is.Zero);
            yield return null;
        }

        private MimicCombatVfx ConfigureCombatVfx(MimicBoss boss)
        {
            SpriteRenderer visual = Create("Polished chest", Vector2.zero).AddComponent<SpriteRenderer>();
            visual.transform.SetParent(boss.transform, false);
            var collider = boss.gameObject.AddComponent<BoxCollider2D>();
            var vfx = boss.gameObject.AddComponent<MimicCombatVfx>();
            SetField(vfx, "boss", boss);
            SetField(vfx, "chestRenderer", visual);
            SetField(vfx, "bodyCollider", collider);
            SetField(vfx, "glowMaterial", UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Member/YKJ/MimicTestAssets/MimicCombatGlow.mat"));
            SetField(boss, "combatVfx", vfx);
            return vfx;
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

        [UnityTest]
        public IEnumerator JumpAfterimagesStayInWorldSpaceFadeOnLandingAndClearOnCancel()
        {
            MimicWeapon weapon = CreateWeapon(new Vector2(-20f, 0f));
            Transform target = Create("Target", new Vector2(12f, 0f)).transform;
            MimicBoss boss = CreateBoss(Vector2.zero, target, weapon);
            MimicJumpPattern jump = ConfigureJump(boss);
            var source = boss.gameObject.AddComponent<SpriteRenderer>();
            Sprite sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            source.sprite = sprite;
            var afterimage = boss.gameObject.AddComponent<MimicJumpAfterimage>();
            SetField(afterimage, "sourceRenderer", source);
            SetField(jump, "afterimage", afterimage);
            MethodInfo tick = typeof(MimicJumpAfterimage).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo imagesField = typeof(MimicJumpAfterimage).GetField("_images", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                boss.Patterns.Start(jump);
                tick.Invoke(afterimage, new object[] { 0.1f });
                Assert.That(imagesField.GetValue(afterimage), Is.Null, "No afterimages during anticipation.");
                boss.Patterns.Tick(0.35f);
                boss.Patterns.Tick(0.2f);
                Vector3 airbornePosition = boss.transform.position;
                tick.Invoke(afterimage, new object[] { 0.05f });
                var images = (SpriteRenderer[])imagesField.GetValue(afterimage);
                Assert.That(images.Length, Is.EqualTo(8));
                Assert.That(images[0].enabled, Is.True);
                Assert.That(images[0].sprite, Is.SameAs(sprite));
                Assert.That(images[0].transform.position, Is.EqualTo(airbornePosition));
                Assert.That(boss.transform.position, Is.EqualTo(airbornePosition));
                boss.Patterns.Tick(0.55f);
                Assert.That(jump.LandedCount, Is.EqualTo(1));
                Assert.That(images[0].transform.position, Is.EqualTo(airbornePosition));
                Assert.That(images[0].enabled, Is.True, "Landing allows the last snapshots to fade.");
                tick.Invoke(afterimage, new object[] { 0.3f });
                foreach (SpriteRenderer image in images) Assert.That(image.enabled, Is.False);

                boss.Patterns.Cancel();
                boss.Patterns.Start(jump);
                boss.Patterns.Tick(0.35f);
                for (int i = 0; i < 12; i++) tick.Invoke(afterimage, new object[] { 0.05f });
                Assert.That(imagesField.GetValue(afterimage), Is.SameAs(images), "The fixed pool must be reused.");
                boss.Patterns.Cancel();
                foreach (SpriteRenderer image in images) Assert.That(image.enabled, Is.False);
                afterimage.Begin();
                tick.Invoke(afterimage, new object[] { 0.05f });
                afterimage.enabled = false;
                foreach (SpriteRenderer image in images) Assert.That(image.enabled, Is.False);
            }
            finally { Object.Destroy(sprite); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoinTransitionRejectsBothNormalAndThrownWeaponDamage()
        {
            var weapon = CreateWeapon(Vector2.left * 20f);
            var boss = CreateBoss(Vector2.zero, Create("Target", Vector2.right * 12f).transform, weapon);
            boss.BeginEncounter();
            boss.TakeDamage(new DamageData(boss.HealthModule.MaxHealth * 0.5f, Member.ODK.Scripts.DamageType.Special));
            Assert.That(boss.Phase, Is.EqualTo(MimicBoss.EncounterPhase.Transition));
            float health = boss.HealthModule.CurrentHealth;
            boss.TakeDamage(new DamageData(100f, Member.ODK.Scripts.DamageType.Special));
            boss.TakeWeaponDamage(new DamageData(100f, Member.ODK.Scripts.DamageType.Projectile), Vector3.zero);
            Assert.That(boss.HealthModule.CurrentHealth, Is.EqualTo(health));
            boss.StopEncounter();
            yield return null;
        }

        [UnityTest]
        public IEnumerator DeadlineWaitsSixtySecondsThenChargesAndDealsExactlyOneHundredThroughInvulnerability()
        {
            var weapon = CreateWeapon(Vector2.left * 20f);
            var target = Create("Target", Vector2.right * 12f);
            var health = target.AddComponent<Member.ODK.Scripts.HealthModule>();
            health.SetMaxHealth(250f);
            health.SettingInvisibleTime(float.PositiveInfinity);
            int deaths = 0;
            health.OnDeath += () => deaths++;
            var boss = CreateBoss(Vector2.zero, target.transform, weapon);
            boss.BeginEncounter();
            MethodInfo tick = typeof(MimicBoss).GetMethod("TickDeadline", BindingFlags.Instance | BindingFlags.NonPublic);
            tick.Invoke(boss, new object[] { 59f });
            Assert.That(boss.IsTimeoutSequenceActive, Is.False);
            Assert.That(boss.EncounterTimeRemaining, Is.EqualTo(1f));
            tick.Invoke(boss, new object[] { 1f });
            Assert.That(boss.IsTimeoutSequenceActive, Is.True);
            Assert.That(boss.TryStartSkill(1), Is.False);
            tick.Invoke(boss, new object[] { 2.9f });
            Assert.That(health.CurrentHealth, Is.EqualTo(250f));
            tick.Invoke(boss, new object[] { 0.2f });
            Assert.That(health.CurrentHealth, Is.EqualTo(150f));
            Assert.That(health.IsDead, Is.True);
            tick.Invoke(boss, new object[] { 10f });
            Assert.That(health.CurrentHealth, Is.EqualTo(150f));
            Assert.That(deaths, Is.EqualTo(1));
            boss.StopEncounter();
            yield return null;
        }

        [UnityTest]
        public IEnumerator KillingBossBeforeDeadlinePreventsTimeoutDamage()
        {
            var weapon = CreateWeapon(Vector2.left * 20f);
            var target = Create("Target", Vector2.right * 12f);
            var health = target.AddComponent<Member.ODK.Scripts.HealthModule>();
            health.SetMaxHealth(100f);
            var boss = CreateBoss(Vector2.zero, target.transform, weapon);
            boss.BeginEncounter();
            boss.TakeDamage(new DamageData(boss.HealthModule.MaxHealth, Member.ODK.Scripts.DamageType.Special));
            typeof(MimicBoss).GetMethod("TickDeadline", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(boss, new object[] { 70f });
            Assert.That(boss.IsEncounterActive, Is.False);
            Assert.That(boss.IsTimeoutSequenceActive, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(100f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DeadlineVignetteRampsAndChestChargeRestoresOnCancel()
        {
            var weapon = CreateWeapon(Vector2.left * 20f);
            var boss = CreateBoss(Vector2.zero, Create("Target", Vector2.right * 12f).transform, weapon);
            var camera = Create("Deadline Camera", Vector2.zero).AddComponent<Camera>();
            var cameraData = camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.volumeLayerMask = 1;
            SetField(boss, "boomCamera", camera);
            var visual = Create("Chest", Vector2.zero).AddComponent<SpriteRenderer>();
            visual.transform.SetParent(boss.transform, false);
            visual.transform.localScale = Vector3.one * 2f;
            var animator = boss.gameObject.AddComponent<MimicBodyAnimator>();
            SetField(animator, "chestRenderer", visual);
            SetField(boss, "bodyAnimator", animator);
            boss.BeginEncounter();
            MethodInfo tick = typeof(MimicBoss).GetMethod("TickDeadline", BindingFlags.Instance | BindingFlags.NonPublic);
            var volume = boss.GetComponentInChildren<UnityEngine.Rendering.Volume>();
            Assert.That(volume, Is.Not.Null);
            Assert.That(volume.weight, Is.Zero);
            tick.Invoke(boss, new object[] { 30f });
            Assert.That(volume.weight, Is.EqualTo(0.5f).Within(0.001f));
            tick.Invoke(boss, new object[] { 30f });
            tick.Invoke(boss, new object[] { 1.5f });
            Assert.That(visual.transform.localScale.x, Is.GreaterThan(2f));
            Assert.That(visual.color.g, Is.LessThan(1f));
            boss.StopEncounter();
            Assert.That(visual.transform.localScale, Is.EqualTo(Vector3.one * 2f));
            Assert.That(visual.color, Is.EqualTo(Color.white));
            Assert.That(volume.enabled, Is.False);
            Assert.That(cameraData.renderPostProcessing, Is.False);
            yield return null;
            Assert.That(volume == null, Is.True);
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
