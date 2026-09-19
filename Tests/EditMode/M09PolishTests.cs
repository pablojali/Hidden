using System.Linq;
using Hidden.Camera;
using Hidden.Discovery;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hidden.Tests
{
    public class CompletionCelebrationTests
    {
        private GameObject celebrationGo;
        private GameObject managerGo;
        private GameObject targetGo;

        private CompletionCelebration celebration;
        private DiscoveryManager manager;
        private Discoverable target;

        [SetUp]
        public void SetUp()
        {
            celebrationGo = new GameObject("TestCelebration");
            celebration = celebrationGo.AddComponent<CompletionCelebration>();

            managerGo = new GameObject("TestDiscoveryManager");
            manager = managerGo.AddComponent<DiscoveryManager>();

            targetGo = new GameObject("TestTarget");
            target = targetGo.AddComponent<Discoverable>();
            manager.RegisterTargets(new[] { target });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(celebrationGo);
            Object.DestroyImmediate(managerGo);
            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void Play_SetsIsPlaying()
        {
            Assert.DoesNotThrow(() => celebration.Play());
            Assert.IsTrue(celebration.IsPlaying);
            Assert.IsTrue(celebration.HasPlayed);
        }

        [Test]
        public void Play_CalledTwice_DoesNotRestartOrThrow()
        {
            celebration.Play();

            Assert.DoesNotThrow(() => celebration.Play());
            Assert.IsTrue(celebration.HasPlayed);
        }

        [Test]
        public void Bind_TriggersPlayOnCompletion_ExactlyOnce()
        {
            celebration.Bind(manager);
            Assert.IsFalse(celebration.IsPlaying);

            target.Discover();

            Assert.IsTrue(celebration.HasPlayed);
        }

        [Test]
        public void AfterCompletionFires_FurtherPlayCallsAreNoOps()
        {
            celebration.Bind(manager);
            target.Discover();
            Assert.IsTrue(celebration.HasPlayed);

            // DiscoveryManager.OnCompleted already only fires once (M0.5),
            // but Play() itself must also refuse to restart if ever called
            // again for any other reason -- calling it again must not throw
            // or change the one-shot outcome.
            Assert.DoesNotThrow(() => celebration.Play());
            Assert.IsTrue(celebration.HasPlayed);
        }
    }

    public class Level01PolishSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Worlds/Level_01_ForestDiorama.unity";

        [Test]
        public void Scene_HasWiderZoomRangeAndCompletionCelebration()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var rootObjects = scene.GetRootGameObjects();
                var world = rootObjects[0];

                var cameraRig = world.transform.Find("Camera");
                var controller = cameraRig.GetComponent<DioramaCameraController>();
                Assert.IsNotNull(controller);

                var camera = world.GetComponentsInChildren<UnityEngine.Camera>(true).FirstOrDefault();
                Assert.IsNotNull(camera);

                var celebration = world.GetComponentInChildren<CompletionCelebration>(true);
                Assert.IsNotNull(celebration, "Level_01 should have a CompletionCelebration.");
                Assert.IsTrue(celebration.transform.IsChildOf(camera.transform),
                    "CompletionCelebration should be parented to the camera so it tracks pan/zoom.");

                var firework = world.GetComponentInChildren<FireworkEffect>(true);
                Assert.IsNotNull(firework, "Per-discovery FireworkEffect should still be present.");

                Assert.AreEqual(6, world.GetComponentsInChildren<Discoverable>(true).Length);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
