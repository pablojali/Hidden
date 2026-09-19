using System.Linq;
using Hidden.Characters;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hidden.Tests
{
    public class M03LivingCharacterTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Worlds/M02_DioramaPrototype.unity";

        [Test]
        public void Scene_ContainsCharacterAndPathWithValidReferences()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var rootObjects = scene.GetRootGameObjects();

                var character = rootObjects.FirstOrDefault(go => go.name == "Character");
                Assert.IsNotNull(character, "Scene should contain a 'Character' root object.");

                var mover = character.GetComponent<CharacterMover>();
                var visual = character.GetComponent<CharacterVisual>();
                Assert.IsNotNull(mover, "Character should have a CharacterMover.");
                Assert.IsNotNull(visual, "Character should have a CharacterVisual.");

                Assert.Greater(mover.MoveSpeed, 0f);
                Assert.GreaterOrEqual(mover.PauseDuration, 0f);
                Assert.Greater(mover.ArrivalThreshold, 0f);

                var characterPathObject = rootObjects.FirstOrDefault(go => go.name == "CharacterPath");
                Assert.IsNotNull(characterPathObject, "Scene should contain a 'CharacterPath' root object.");

                var path = characterPathObject.GetComponent<CharacterPath>();
                Assert.IsNotNull(path);
                Assert.GreaterOrEqual(path.WaypointCount, 3, "Path should cross multiple areas, not just two points.");

                for (var i = 0; i < path.WaypointCount; i++)
                {
                    Assert.DoesNotThrow(() => path.GetWaypointPosition(i));
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    public class CharacterMoverTests
    {
        [Test]
        public void Initialize_WithAssignedPath_PlacesCharacterAtFirstWaypointAndEntersIdle()
        {
            var pathGo = new GameObject("TestPath");
            var characterGo = new GameObject("TestCharacter");

            try
            {
                var path = pathGo.AddComponent<CharacterPath>();
                CreateWaypointChild(pathGo.transform, new Vector3(0f, 0f, 0f));
                CreateWaypointChild(pathGo.transform, new Vector3(5f, 0f, 0f));
                CreateWaypointChild(pathGo.transform, new Vector3(5f, 0f, 5f));

                var mover = characterGo.AddComponent<CharacterMover>();
                mover.SetPath(path);

                Assert.DoesNotThrow(() => mover.Initialize());

                Assert.AreEqual(CharacterMover.State.Idle, mover.CurrentState);
                Assert.AreEqual(path.GetWaypointPosition(0), characterGo.transform.position);
            }
            finally
            {
                Object.DestroyImmediate(characterGo);
                Object.DestroyImmediate(pathGo);
            }
        }

        [Test]
        public void Initialize_WithoutPath_DisablesSelfInsteadOfThrowing()
        {
            var characterGo = new GameObject("TestCharacterNoPath");

            try
            {
                var mover = characterGo.AddComponent<CharacterMover>();

                Assert.DoesNotThrow(() => mover.Initialize());
                Assert.IsFalse(mover.enabled);
            }
            finally
            {
                Object.DestroyImmediate(characterGo);
            }
        }

        private static void CreateWaypointChild(Transform parent, Vector3 position)
        {
            var waypoint = new GameObject("Waypoint");
            waypoint.transform.SetParent(parent);
            waypoint.transform.position = position;
        }
    }
}
