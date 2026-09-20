#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using Member.YKJ.Bosses;
using NUnit.Framework;
using UnityEngine;

namespace Member.YKJ.Tests
{
    public sealed class MimicPatternTests
    {
        private sealed class Probe : MimicPattern
        {
            public int Starts, Updates, Pauses, Resumes, Ends, Deaths;
            public bool Allowed = true;
            public Action<float> TickAction;
            public override bool CanStart() => Allowed;
            public override void OnStart() => Starts++;
            public override void OnUpdate(float deltaTime) { Updates++; TickAction?.Invoke(deltaTime); }
            public override void OnPause() => Pauses++;
            public override void OnResume() => Resumes++;
            public override void OnEnd() => Ends++;
            public override void OnDie() => Deaths++;
        }

        [Test]
        public void InterruptResumesWithoutRestartingParent()
        {
            var runner = new MimicPatternRunner();
            var treasure = new Probe();
            var tongue = new Probe();
            Assert.That(runner.Start(treasure), Is.True);
            Assert.That(runner.Interrupt(treasure, tongue), Is.True);
            runner.Tick(0.5f);
            Assert.That(treasure.Updates, Is.Zero);
            Assert.That(tongue.Updates, Is.EqualTo(1));
            Assert.That(runner.Complete(tongue), Is.True);
            Assert.That(runner.Current, Is.SameAs(treasure));
            Assert.That(treasure.Starts, Is.EqualTo(1));
            Assert.That(treasure.Resumes, Is.EqualTo(1));
            Assert.That(treasure.Ends, Is.Zero);
            Assert.That(tongue.Ends, Is.EqualTo(1));
        }

        [Test]
        public void StaleCallbacksCannotFinishOrInterruptCurrentPattern()
        {
            var runner = new MimicPatternRunner();
            var parent = new Probe();
            var child = new Probe();
            runner.Start(parent);
            runner.Interrupt(parent, child);
            Assert.That(runner.Complete(parent), Is.False);
            Assert.That(runner.Interrupt(parent, new Probe()), Is.False);
            Assert.That(runner.Current, Is.SameAs(child));
        }

        [Test]
        public void NestedInterruptsResumeInReverseOrder()
        {
            var runner = new MimicPatternRunner();
            var first = new Probe();
            var second = new Probe();
            var third = new Probe();
            runner.Start(first);
            runner.Interrupt(first, second);
            runner.Interrupt(second, third);
            Assert.That(runner.Interrupt(third, first), Is.False);
            runner.Complete(third);
            Assert.That(runner.Current, Is.SameAs(second));
            runner.Complete(second);
            Assert.That(runner.Current, Is.SameAs(first));
            runner.Complete(first);
            Assert.That(runner.IsRunning, Is.False);
            Assert.That(runner.SuspendedCount, Is.Zero);
        }

        [Test]
        public void DeathCancelsActiveAndSuspendedPatternsExactlyOnce()
        {
            var runner = new MimicPatternRunner();
            var parent = new Probe();
            var child = new Probe();
            runner.Start(parent);
            runner.Interrupt(parent, child);
            runner.Cancel(true);
            runner.Cancel(true);
            Assert.That(parent.Deaths, Is.EqualTo(1));
            Assert.That(child.Deaths, Is.EqualTo(1));
            Assert.That(parent.Ends, Is.EqualTo(1));
            Assert.That(child.Ends, Is.EqualTo(1));
            Assert.That(parent.Resumes, Is.Zero);
            Assert.That(runner.IsRunning, Is.False);
            Assert.That(runner.SuspendedCount, Is.Zero);
        }

        [Test]
        public void CancelledRunnerCanStartANewEncounter()
        {
            var runner = new MimicPatternRunner();
            var pattern = new Probe();
            runner.Start(pattern);
            runner.Cancel();
            Assert.That(runner.Start(pattern), Is.True);
            Assert.That(pattern.Starts, Is.EqualTo(2));
            Assert.That(pattern.Deaths, Is.Zero);
        }

        [Test]
        public void RejectsInvalidStartsAndInterruptsWithoutChangingParent()
        {
            var runner = new MimicPatternRunner();
            var parent = new Probe();
            Assert.That(runner.Start(null), Is.False);
            Assert.That(runner.Start(new Probe { Allowed = false }), Is.False);
            runner.Start(parent);
            Assert.That(runner.Start(new Probe()), Is.False);
            Assert.That(runner.Interrupt(parent, parent), Is.False);
            Assert.That(runner.Interrupt(parent, new Probe { Allowed = false }), Is.False);
            Assert.That(parent.Pauses, Is.Zero);
        }

        [Test]
        public void CompletionInsideTickDoesNotUpdateTheResumedPatternInTheSameTick()
        {
            var runner = new MimicPatternRunner();
            var parent = new Probe();
            var child = new Probe();
            child.TickAction = _ => runner.Complete(child);
            runner.Start(parent);
            runner.Interrupt(parent, child);
            runner.Tick(1f);
            Assert.That(parent.Updates, Is.Zero);
            runner.Tick(1f);
            Assert.That(parent.Updates, Is.EqualTo(1));
        }

        [Test]
        public void FifteenWeaponsProduceExactlyTwoTongueCheckpoints()
        {
            var progress = new MimicEmissionProgress(15, 0.5f);
            var checkpoints = new List<int>();
            for (int i = 0; i < 15; i++)
            {
                Assert.That(progress.Advance(0.25f), Is.False);
                Assert.That(progress.Advance(0.25f), Is.True);
                if (progress.IsTongueCheckpoint)
                    checkpoints.Add(progress.Count);
            }
            CollectionAssert.AreEqual(new[] { 5, 10 }, checkpoints);
            Assert.That(progress.Finished, Is.True);
            Assert.That(progress.Advance(5f), Is.False);
            Assert.That(progress.Count, Is.EqualTo(15));
        }

        [Test]
        public void PausedEmissionKeepsCountAndDoesNotAccumulateTongueTime()
        {
            var runner = new MimicPatternRunner();
            var progress = new MimicEmissionProgress(15, 0.5f);
            var parent = new Probe();
            var tongue = new Probe();
            parent.TickAction = delta =>
            {
                if (progress.Advance(delta) && progress.IsTongueCheckpoint)
                    runner.Interrupt(parent, tongue);
            };
            runner.Start(parent);
            for (int i = 0; i < 5; i++) runner.Tick(0.5f);
            runner.Tick(100f);
            Assert.That(progress.Count, Is.EqualTo(5));
            runner.Complete(tongue);
            runner.Tick(0.25f);
            Assert.That(progress.Count, Is.EqualTo(5));
            runner.Tick(0.25f);
            Assert.That(progress.Count, Is.EqualTo(6));
        }

        [Test]
        public void LargeFrameDoesNotBurstWeaponsOrSkipCheckpoints()
        {
            var progress = new MimicEmissionProgress(15, 0.5f);
            Assert.That(progress.Advance(5f), Is.True);
            Assert.That(progress.Count, Is.EqualTo(1));
            Assert.That(progress.Advance(0.01f), Is.False);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidDeltaDoesNotAdvancePatterns(float delta)
        {
            var runner = new MimicPatternRunner();
            var pattern = new Probe();
            runner.Start(pattern);
            runner.Tick(delta);
            Assert.That(pattern.Updates, Is.Zero);
            Assert.That(new MimicEmissionProgress(15, 0.5f).Advance(delta), Is.False);
        }

        [Test]
        public void InvalidEmissionSettingsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MimicEmissionProgress(0, 0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MimicEmissionProgress(15, 0f));
        }

        [TestCase(1.2f)]
        [TestCase(0.5f)]
        [TestCase(2f)]
        public void BallisticVelocityReachesRequestedDestination(float time)
        {
            var origin = new Vector2(2f, 3f);
            var destination = new Vector2(-5f, 1f);
            var gravity = new Vector2(0f, -19.62f);
            Vector2 velocity = MimicWeapon.CalculateLaunchVelocity(origin, destination, gravity, time);
            Vector2 actual = origin + velocity * time + gravity * (0.5f * time * time);
            Assert.That(Vector2.Distance(actual, destination), Is.LessThan(0.0001f));
        }

        [TestCase(-1.6f, -8f)]
        [TestCase(1.435f, 6.5f)]
        [TestCase(4.935f, -6.5f)]
        [TestCase(4.935f, 0f)]
        public void ArcReachesSelectedHeightOnDescent(float y, float x)
        {
            var origin = new Vector2(0f, -0.4f);
            var destination = new Vector2(x, y);
            var gravity = new Vector2(0f, -14.715f);
            Vector2 velocity = MimicWeapon.CalculateArcVelocity(origin, destination, gravity, 0.75f);
            float peak = Mathf.Max(origin.y, destination.y) + 0.75f;
            float time = velocity.y / -gravity.y + Mathf.Sqrt(2f * (peak - destination.y) / -gravity.y);
            Vector2 actual = origin + velocity * time + gravity * (0.5f * time * time);
            Assert.That(Vector2.Distance(actual, destination), Is.LessThan(0.0001f));
            Assert.That(velocity.y + gravity.y * time, Is.LessThan(0f));
            Assert.That(origin.y + velocity.y * velocity.y / (2f * -gravity.y), Is.EqualTo(peak).Within(0.0001f));
        }

        [Test]
        public void FifteenEmissionsSelectEachHeightFiveTimes()
        {
            UnityEngine.Random.State previous = UnityEngine.Random.state;
            try
            {
                var heights = new MimicTreasureHeightCycle();
                var counts = new int[3];
                for (int batch = 0; batch < 5; batch++)
                {
                    var seen = new HashSet<MimicTreasureHeight>();
                    for (int shot = 0; shot < 3; shot++)
                    {
                        MimicTreasureHeight height = heights.Next();
                        Assert.That(seen.Add(height), Is.True);
                        counts[(int)height]++;
                    }
                }
                CollectionAssert.AreEqual(new[] { 5, 5, 5 }, counts);
            }
            finally { UnityEngine.Random.state = previous; }
        }

        [TestCase(-6.5f)]
        [TestCase(6.5f)]
        public void PlatformLandingSamplesOnlyItsOwnWidthAtOneHeight(float platformX)
        {
            var surface = new Bounds(new Vector3(platformX, 4f), new Vector3(4.55f, 1.08f));
            for (int i = 0; i <= 10; i++)
            {
                Vector2 point = MimicBoss.SamplePlatformLanding(surface, new Vector2(0.275f, 0.375f), 0.65f, i / 10f);
                Assert.That(point.x, Is.InRange(surface.min.x + 0.925f, surface.max.x - 0.925f));
                Assert.That(point.y, Is.EqualTo(4.935f).Within(0.0001f));
                Assert.That(Mathf.Sign(point.x), Is.EqualTo(Mathf.Sign(platformX)));
            }
        }

        [TestCase(-1.6f, 1.04f)]
        [TestCase(1.435f, 4.54f)]
        public void LowerHeightArcCannotLandOnTheNextPlatformLevel(float landingY, float nextSurfaceY)
        {
            Vector2 velocity = MimicWeapon.CalculateArcVelocity(new Vector2(0f, -0.4f),
                new Vector2(6.5f, landingY), new Vector2(0f, -14.715f), 0.75f);
            float peakBottom = -0.4f + velocity.y * velocity.y / (2f * 14.715f) - 0.375f;
            Assert.That(peakBottom, Is.LessThan(nextSurfaceY));
        }

        [TestCase(0f)]
        [TestCase(9.81f)]
        public void ArcRejectsGravityThatDoesNotPullDown(float gravityY)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MimicWeapon.CalculateArcVelocity(
                Vector2.zero, Vector2.right, new Vector2(0f, gravityY), 0.75f));
        }
    }
}
#endif
