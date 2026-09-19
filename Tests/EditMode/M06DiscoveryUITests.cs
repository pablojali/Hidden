using Hidden.Discovery;
using Hidden.UI;
using NUnit.Framework;
using UnityEngine;

namespace Hidden.Tests
{
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
        }

        [Test]
        public void ConfirmationFeedback_TriggeredOncePerDiscovery()
        {
            ui.Bind(manager);

            targetA.Discover();
            Assert.AreEqual(1, ui.DiscoveryMessageCount);
            Assert.IsTrue(ui.IsShowingMessage);

            targetB.Discover();
            Assert.AreEqual(2, ui.DiscoveryMessageCount);

            // Re-discovering (already-discovered) targetA must not re-trigger --
            // Discoverable itself is one-shot, from M0.4.
            targetA.Discover();
            Assert.AreEqual(2, ui.DiscoveryMessageCount);
        }

        [Test]
        public void CompletionFeedback_TriggeredWhenManagerReportsCompletion()
        {
            ui.Bind(manager);

            targetA.Discover();
            targetB.Discover();
            Assert.AreEqual(0, ui.CompletionMessageCount);

            targetC.Discover();
            Assert.AreEqual(1, ui.CompletionMessageCount);
            Assert.IsTrue(manager.IsComplete);
        }

        [Test]
        public void ConfigurableConfirmationText_IsUsedInsteadOfAHardcodedString()
        {
            ui.Bind(manager);
            ui.Configure(new[] { "Custom found message" }, "Custom completion message");

            targetA.Discover();

            Assert.AreEqual("Custom found message", ui.CurrentMessage);
        }

        [Test]
        public void ConfigurableCompletionText_IsUsed()
        {
            ui.Bind(manager);
            ui.Configure(new[] { "Custom found message" }, "Custom completion message");

            targetA.Discover();
            targetB.Discover();
            targetC.Discover();

            Assert.AreEqual("Custom completion message", ui.CurrentMessage);
        }

        [Test]
        public void UI_DoesNotAlterDiscoveryManagerState()
        {
            ui.Bind(manager);

            targetA.Discover();
            targetB.Discover();

            // The UI only reads TotalTargets/DiscoveredCount and subscribes
            // to events -- it never calls a manager method that could
            // mutate state, so the manager's own counters must reflect
            // exactly what the two Discover() calls produced, nothing more.
            Assert.AreEqual(3, manager.TotalTargets);
            Assert.AreEqual(2, manager.DiscoveredCount);
            Assert.IsFalse(manager.IsComplete);
        }
    }
}
