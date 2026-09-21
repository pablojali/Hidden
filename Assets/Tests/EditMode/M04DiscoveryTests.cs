using Hidden.Discovery;
using NUnit.Framework;
using UnityEngine;

namespace Hidden.Tests
{
    public class DiscoverableTests
    {
        [Test]
        public void Discoverable_StartsUndiscovered()
        {
            var go = new GameObject("TestDiscoverable");

            try
            {
                var discoverable = go.AddComponent<Discoverable>();
                Assert.IsFalse(discoverable.IsDiscovered);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Discover_SetsIsDiscoveredTrue()
        {
            var go = new GameObject("TestDiscoverable");

            try
            {
                var discoverable = go.AddComponent<Discoverable>();
                discoverable.Discover();

                Assert.IsTrue(discoverable.IsDiscovered);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Discover_CalledMultipleTimes_OnlyFiresEventOnce()
        {
            var go = new GameObject("TestDiscoverable");

            try
            {
                var discoverable = go.AddComponent<Discoverable>();
                var callCount = 0;
                discoverable.Discovered += () => callCount++;

                discoverable.Discover();
                discoverable.Discover();
                discoverable.Discover();

                Assert.AreEqual(1, callCount);
                Assert.IsTrue(discoverable.IsDiscovered);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // Discoverable has no reference to, and no dependency on,
        // CharacterMover: a bare GameObject with only this component (no
        // Character, no CharacterMover anywhere in the scene) works fully
        // on its own.
        [Test]
        public void Discoverable_WorksWithoutAnyCharacterMoverPresent()
        {
            var go = new GameObject("StandaloneDiscoverable");

            try
            {
                var discoverable = go.AddComponent<Discoverable>();

                Assert.DoesNotThrow(() => discoverable.Discover());
                Assert.IsTrue(discoverable.IsDiscovered);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    public class DiscoverySystemTests
    {
        private GameObject cameraGo;
        private GameObject targetGo;
        private GameObject systemGo;

        [TearDown]
        public void TearDown()
        {
            if (cameraGo != null) Object.DestroyImmediate(cameraGo);
            if (targetGo != null) Object.DestroyImmediate(targetGo);
            if (systemGo != null) Object.DestroyImmediate(systemGo);
        }

        [Test]
        public void CanDiscover_TargetInRangeAndInView_ReturnsTrue()
        {
            var (system, target) = BuildSystemAndTarget(new Vector3(0f, 0f, 5f));

            Assert.DoesNotThrow(() => system.CanDiscover(target));
            Assert.IsTrue(system.CanDiscover(target));
        }

        [Test]
        public void CanDiscover_TargetBeyondDiscoveryRange_ReturnsFalse()
        {
            // Same viewport position as the in-range case (directly ahead
            // of the camera), just much farther away than the default
            // 15-unit discovery range.
            var (system, target) = BuildSystemAndTarget(new Vector3(0f, 0f, 50f));

            Assert.IsFalse(system.CanDiscover(target));
        }

        private (DiscoverySystem system, Discoverable target) BuildSystemAndTarget(Vector3 targetPosition)
        {
            cameraGo = new GameObject("TestCamera");
            var camera = cameraGo.AddComponent<UnityEngine.Camera>();

            targetGo = new GameObject("TestTarget");
            targetGo.transform.position = targetPosition;
            var target = targetGo.AddComponent<Discoverable>();

            systemGo = new GameObject("TestDiscoverySystem");
            var system = systemGo.AddComponent<DiscoverySystem>();
            system.SetCamera(camera);
            system.SetTargets(new[] { target });

            return (system, target);
        }
    }
}
