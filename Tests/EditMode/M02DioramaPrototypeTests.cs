using System.Linq;
using Hidden.Camera;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hidden.Tests
{
    public class M02DioramaPrototypeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Worlds/M02_DioramaPrototype.unity";

        [Test]
        public void Scene_LoadsAndContainsExpectedEnvironment()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                Assert.IsTrue(scene.IsValid());
                Assert.IsTrue(scene.isLoaded);

                var rootObjects = scene.GetRootGameObjects();
                var rootNames = rootObjects.Select(go => go.name).ToArray();

                Assert.Contains("CameraRig", rootNames);
                Assert.Contains("DioramaBase", rootNames);
                Assert.Contains("House", rootNames);
                Assert.Contains("LargeTree", rootNames);

                var camera = rootObjects
                    .SelectMany(go => go.GetComponentsInChildren<UnityEngine.Camera>(true))
                    .FirstOrDefault();

                Assert.IsNotNull(camera, "Scene should contain a Camera.");
                Assert.IsFalse(camera.orthographic, "Diorama camera must be perspective.");

                var rig = rootObjects.First(go => go.name == "CameraRig");
                Assert.IsNotNull(rig.GetComponent<DioramaCameraController>());
                Assert.IsNotNull(rig.GetComponent<CameraInput>());
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    public class DioramaCameraControllerTests
    {
        [Test]
        public void AddingControllerComponents_DoesNotThrow()
        {
            var go = new GameObject("CameraRigUnderTest");

            try
            {
                Assert.DoesNotThrow(() =>
                {
                    go.AddComponent<CameraInput>();
                    go.AddComponent<DioramaCameraController>();
                });
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
