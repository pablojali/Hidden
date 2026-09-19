using Hidden.Discovery;
using NUnit.Framework;
using UnityEngine;

namespace Hidden.Tests
{
    public class DiscoveryManagerTests
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
        public void Manager_StartsWithZeroDiscovered()
        {
            Assert.AreEqual(0, manager.DiscoveredCount);
        }

        [Test]
        public void RegisteringThreeTargets_SetsTotalTargetsToThree()
        {
            manager.RegisterTargets(new[] { targetA, targetB, targetC });

            Assert.AreEqual(3, manager.TotalTargets);
        }

        [Test]
        public void DiscoveringOneTarget_SetsDiscoveredCountToOne()
        {
            manager.RegisterTargets(new[] { targetA, targetB, targetC });

            targetA.Discover();

            Assert.AreEqual(1, manager.DiscoveredCount);
        }

        [Test]
        public void DiscoveringTwoTargets_SetsDiscoveredCountToTwo()
        {
            manager.RegisterTargets(new[] { targetA, targetB, targetC });

            targetA.Discover();
            targetB.Discover();

            Assert.AreEqual(2, manager.DiscoveredCount);
        }

        [Test]
        public void DiscoveringAllThreeTargets_SetsDiscoveredCountToThree()
        {
            manager.RegisterTargets(new[] { targetA, targetB, targetC });

            targetA.Discover();
            targetB.Discover();
            targetC.Discover();

            Assert.AreEqual(3, manager.DiscoveredCount);
        }

        [Test]
        public void IsComplete_OnlyTrueAfterFinalTarget()
        {
            manager.RegisterTargets(new[] { targetA, targetB, targetC });

            targetA.Discover();
            Assert.IsFalse(manager.IsComplete);

            targetB.Discover();
            Assert.IsFalse(manager.IsComplete);

            targetC.Discover();
            Assert.IsTrue(manager.IsComplete);
        }

        [Test]
        public void DiscoveringSameTargetTwice_DoesNotIncrementProgressTwice()
        {
            manager.RegisterTargets(new[] { targetA, targetB, targetC });

            targetA.Discover();
            targetA.Discover();
            targetA.Discover();

            Assert.AreEqual(1, manager.DiscoveredCount);
        }

        [Test]
        public void OnDiscovery_FiresExactlyOncePerTarget()
        {
            manager.RegisterTargets(new[] { targetA, targetB, targetC });

            var fireCount = 0;
            manager.OnDiscovery += _ => fireCount++;

            targetA.Discover();
            targetA.Discover();
            targetB.Discover();

            Assert.AreEqual(2, fireCount);
        }

        [Test]
        public void OnCompleted_FiresExactlyOnce()
        {
            manager.RegisterTargets(new[] { targetA, targetB, targetC });

            var completedCount = 0;
            manager.OnCompleted += () => completedCount++;

            targetA.Discover();
            targetB.Discover();
            targetC.Discover();
            // Redundant re-discovery attempts must not re-fire completion.
            targetA.Discover();
            targetB.Discover();

            Assert.AreEqual(1, completedCount);
        }

        [Test]
        public void ZeroTargetManager_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => manager.RegisterTargets(System.Array.Empty<Discoverable>()));
            Assert.AreEqual(0, manager.TotalTargets);
            Assert.IsFalse(manager.IsComplete);
        }
    }
}
