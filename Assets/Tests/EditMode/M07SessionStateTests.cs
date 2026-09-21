using Hidden.Discovery;
using NUnit.Framework;
using UnityEngine;

namespace Hidden.Tests
{
    // DiscoveryManager.SessionState is a thin, read-only view over state
    // DiscoveryManager already tracked since M0.5 (IsComplete/completedFired),
    // not a new framework -- these tests cover that view plus the "no
    // further changes after completion" guarantee the M0.7 spec calls out
    // explicitly. All existing M0.5 behavior (M05DiscoveryManagerTests.cs)
    // is unchanged and still applies; DiscoveryManager's core API was only
    // added to, nothing was removed or altered.
    public class SessionStateTests
    {
        private GameObject managerGo;
        private GameObject targetAGo;
        private GameObject targetBGo;
        private GameObject targetCGo;

        private DiscoveryManager manager;
        private Discoverable targetA;
        private Discoverable targetB;
        private Discoverable targetC;

        [SetUp]
        public void SetUp()
        {
            managerGo = new GameObject("TestDiscoveryManager");
            manager = managerGo.AddComponent<DiscoveryManager>();

            targetAGo = new GameObject("TargetA");
            targetBGo = new GameObject("TargetB");
            targetCGo = new GameObject("TargetC");
            targetA = targetAGo.AddComponent<Discoverable>();
            targetB = targetBGo.AddComponent<Discoverable>();
            targetC = targetCGo.AddComponent<Discoverable>();

            manager.RegisterTargets(new[] { targetA, targetB, targetC });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGo);
            Object.DestroyImmediate(targetAGo);
            Object.DestroyImmediate(targetBGo);
            Object.DestroyImmediate(targetCGo);
        }

        [Test]
        public void NewSession_StartsInPlayingState()
        {
            Assert.AreEqual(DiscoveryManager.SessionState.Playing, manager.State);
        }

        [Test]
        public void NewSession_DiscoveryProgressStartsAtZero()
        {
            Assert.AreEqual(0, manager.DiscoveredCount);
            Assert.AreEqual(3, manager.TotalTargets);
        }

        [Test]
        public void DiscoveringAllTargets_TransitionsToCompleted()
        {
            targetA.Discover();
            Assert.AreEqual(DiscoveryManager.SessionState.Playing, manager.State);

            targetB.Discover();
            Assert.AreEqual(DiscoveryManager.SessionState.Playing, manager.State);

            targetC.Discover();
            Assert.AreEqual(DiscoveryManager.SessionState.Completed, manager.State);
        }

        [Test]
        public void Completion_HappensExactlyOnce()
        {
            var completedCount = 0;
            manager.OnCompleted += () => completedCount++;

            targetA.Discover();
            targetB.Discover();
            targetC.Discover();

            Assert.AreEqual(1, completedCount);
        }

        [Test]
        public void AfterCompletion_AdditionalDiscoveryAttemptsDoNotModifyProgress()
        {
            targetA.Discover();
            targetB.Discover();
            targetC.Discover();

            Assert.AreEqual(3, manager.DiscoveredCount);
            Assert.AreEqual(DiscoveryManager.SessionState.Completed, manager.State);

            // Re-discovering already-discovered targets (Discoverable is
            // one-shot from M0.4) must not change count or re-fire events.
            var completedCount = 0;
            manager.OnCompleted += () => completedCount++;
            var discoveryCount = 0;
            manager.OnDiscovery += _ => discoveryCount++;

            targetA.Discover();
            targetB.Discover();
            targetC.Discover();

            Assert.AreEqual(3, manager.DiscoveredCount);
            Assert.AreEqual(DiscoveryManager.SessionState.Completed, manager.State);
            Assert.AreEqual(0, completedCount);
            Assert.AreEqual(0, discoveryCount);
        }
    }

    public class FireworkEffectTests
    {
        [Test]
        public void Play_SetsIsPlayingAndMovesToTargetPosition()
        {
            var go = new GameObject("TestFirework");

            try
            {
                var firework = go.AddComponent<FireworkEffect>();
                var targetPosition = new Vector3(3f, 0f, 5f);

                Assert.DoesNotThrow(() => firework.Play(targetPosition));

                Assert.IsTrue(firework.IsPlaying);
                Assert.Greater(firework.transform.position.y, targetPosition.y);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Bind_TriggersPlayOnDiscovery()
        {
            var fireworkGo = new GameObject("TestFirework");
            var targetGo = new GameObject("TestTarget");
            var managerGo = new GameObject("TestManager");

            try
            {
                var firework = fireworkGo.AddComponent<FireworkEffect>();
                var target = targetGo.AddComponent<Discoverable>();
                targetGo.transform.position = new Vector3(1f, 0f, 2f);
                var manager = managerGo.AddComponent<DiscoveryManager>();
                manager.RegisterTargets(new[] { target });

                firework.Bind(manager);

                Assert.IsFalse(firework.IsPlaying);

                target.Discover();

                Assert.IsTrue(firework.IsPlaying);
            }
            finally
            {
                Object.DestroyImmediate(fireworkGo);
                Object.DestroyImmediate(targetGo);
                Object.DestroyImmediate(managerGo);
            }
        }
    }
}
