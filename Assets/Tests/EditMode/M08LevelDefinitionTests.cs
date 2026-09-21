using System.Linq;
using System.Reflection;
using Hidden.Camera;
using Hidden.Characters;
using Hidden.Discovery;
using Hidden.Levels;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hidden.Tests
{
    public class LevelDefinitionTests
    {
        [Test]
        public void Defaults_AreValid()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();

            try
            {
                Assert.IsTrue(level.IsValid);
                Assert.IsFalse(string.IsNullOrWhiteSpace(level.LevelId));
                Assert.IsFalse(string.IsNullOrWhiteSpace(level.DisplayName));
                Assert.Greater(level.ExpectedTargetCount, 0);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void EmptyDisplayName_IsInvalid()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();

            try
            {
                SetPrivateField(level, "displayName", "");
                Assert.IsFalse(level.IsValid);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void ZeroTargetCount_IsInvalid()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();

            try
            {
                SetPrivateField(level, "expectedTargetCount", 0);
                Assert.IsFalse(level.IsValid);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(target, value);
        }
    }

    public class LevelInfoTests
    {
        [Test]
        public void Bind_MatchingCount_ReportsMatch()
        {
            var levelGo = new GameObject("TestLevelInfo");
            var managerGo = new GameObject("TestDiscoveryManager");
            var targetGo = new GameObject("TestTarget");
            var level = ScriptableObject.CreateInstance<LevelDefinition>();

            try
            {
                var info = levelGo.AddComponent<LevelInfo>();
                var manager = managerGo.AddComponent<DiscoveryManager>();
                var target = targetGo.AddComponent<Discoverable>();
                manager.RegisterTargets(new[] { target });
                SetPrivateField(level, "expectedTargetCount", 1);

                info.Bind(level, manager);

                Assert.IsTrue(info.MatchesExpectedTargetCount);
            }
            finally
            {
                Object.DestroyImmediate(levelGo);
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(targetGo);
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void Bind_MismatchedCount_ReportsMismatch()
        {
            var levelGo = new GameObject("TestLevelInfo");
            var managerGo = new GameObject("TestDiscoveryManager");
            var level = ScriptableObject.CreateInstance<LevelDefinition>();

            try
            {
                var info = levelGo.AddComponent<LevelInfo>();
                var manager = managerGo.AddComponent<DiscoveryManager>();
                SetPrivateField(level, "expectedTargetCount", 6);

                info.Bind(level, manager);

                Assert.IsFalse(info.MatchesExpectedTargetCount);
            }
            finally
            {
                Object.DestroyImmediate(levelGo);
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(level);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(target, value);
        }
    }

    public class Level01SceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Worlds/Level_01_ForestDiorama.unity";

        [Test]
        public void Scene_LoadsAndContainsExpectedHierarchy()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                Assert.IsTrue(scene.IsValid());
                Assert.IsTrue(scene.isLoaded);

                var rootObjects = scene.GetRootGameObjects();
                Assert.AreEqual(1, rootObjects.Length, "Level_01 should have a single 'World' root.");

                var world = rootObjects[0];
                Assert.AreEqual("World", world.name);

                var groupNames = new[]
                {
                    "Environment", "Structures", "Props", "Paths", "Discoverables", "Camera", "GameSystems", "UI",
                };
                foreach (var groupName in groupNames)
                {
                    Assert.IsNotNull(world.transform.Find(groupName), $"World should contain a '{groupName}' child.");
                }

                var discoverables = world.GetComponentsInChildren<Discoverable>(true);
                Assert.AreEqual(6, discoverables.Length, "Level_01 should have exactly 6 Discoverable targets.");

                var movers = world.GetComponentsInChildren<CharacterMover>(true);
                Assert.AreEqual(2, movers.Length, "Level_01 should have exactly 2 moving targets.");

                var camera = world.GetComponentsInChildren<UnityEngine.Camera>(true).FirstOrDefault();
                Assert.IsNotNull(camera, "Scene should contain a Camera.");
                Assert.IsFalse(camera.orthographic, "Diorama camera must be perspective.");

                var cameraRig = world.transform.Find("Camera");
                Assert.IsNotNull(cameraRig.GetComponent<DioramaCameraController>());
                Assert.IsNotNull(cameraRig.GetComponent<CameraInput>());

                Assert.IsNotNull(world.GetComponentInChildren<DiscoveryManager>(true));
                Assert.IsNotNull(world.GetComponentInChildren<LevelInfo>(true));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
