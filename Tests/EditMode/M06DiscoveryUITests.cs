using Hidden.Discovery;
using Hidden.UI;
using NUnit.Framework;
using UnityEngine;

namespace Hidden.Tests
{
    // DiscoveryUI's scope narrowed in M0.7: per-discovery/completion
    // feedback moved to FireworkEffect/CompletionFeedback (visual, no
    // text), so only the progress-readout behavior is tested here now.
    public class DiscoveryUITests
    {
        private GameObject managerGo;
        private GameObject uiGo;
        private GameObject targetAGo;
        private GameObject targetBGo;
        private GameObject targetCGo;

        private DiscoveryManager manager;
        private DiscoveryUI ui;
        private Discoverable targetA;
        private Discoverable targetB;
        private Discoverable targetC;

        [SetUp]
        public void SetUp()
        {
            managerGo = new GameObject("TestDiscoveryManager");
            manager = managerGo.AddComponent<DiscoveryManager>();

            uiGo = new GameObject("TestDiscoveryUI");
            ui = uiGo.AddComponent<DiscoveryUI>();

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
            Object.DestroyImmediate(uiGo);
            Object.DestroyImmediate(targetAGo);
            Object.DestroyImmediate(targetBGo);
            Object.DestroyImmediate(targetCGo);
        }

        [Test]
        public void Bind_ReflectsInitialZeroOfTotal()
        {
            ui.Bind(manager);

            Assert.AreEqual("0 / 3", ui.CurrentProgressText);
        }

        [Test]
        public void DiscoveryEvent_UpdatesDisplayedProgress()
        {
            ui.Bind(manager);

            targetA.Discover();
            Assert.AreEqual("1 / 3", ui.CurrentProgressText);

            targetB.Discover();
            Assert.AreEqual("2 / 3", ui.CurrentProgressText);

            targetC.Discover();
            Assert.AreEqual("3 / 3", ui.CurrentProgressText);
        }

        [Test]
        public void UI_DoesNotAlterDiscoveryManagerState()
        {
            ui.Bind(manager);

            targetA.Discover();
            targetB.Discover();

            // The UI only reads TotalTargets/DiscoveredCount and subscribes
            // to OnDiscovery -- it never calls a manager method that could
            // mutate state, so the manager's own counters must reflect
            // exactly what the two Discover() calls produced, nothing more.
            Assert.AreEqual(3, manager.TotalTargets);
            Assert.AreEqual(2, manager.DiscoveredCount);
            Assert.IsFalse(manager.IsComplete);
        }
    }
}
