using System.Linq;
using Hidden.Characters;
using Hidden.Discovery;
using Hidden.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hidden.Tests
{
    public class ProceduralBlobMeshTests
    {
        [Test]
        public void Build_SameSeed_IsDeterministic()
        {
            var meshA = ProceduralBlobMesh.Build(seed: 42, jitter: 0.2f, verticalSquash: 0.8f);
            var meshB = ProceduralBlobMesh.Build(seed: 42, jitter: 0.2f, verticalSquash: 0.8f);

            CollectionAssert.AreEqual(meshA.vertices, meshB.vertices);
        }

        [Test]
        public void Build_DifferentSeed_ProducesDifferentShape()
        {
            var meshA = ProceduralBlobMesh.Build(seed: 1, jitter: 0.3f, verticalSquash: 1f);
            var meshB = ProceduralBlobMesh.Build(seed: 2, jitter: 0.3f, verticalSquash: 1f);

            CollectionAssert.AreNotEqual(meshA.vertices, meshB.vertices);
        }

        [Test]
        public void Build_ProducesExpectedTriangleCount()
        {
            var mesh = ProceduralBlobMesh.Build(seed: 7, jitter: 0.18f, verticalSquash: 1f);

            // 20 icosahedron faces, unshared vertices for flat shading.
            Assert.AreEqual(60, mesh.vertexCount);
            Assert.AreEqual(60, mesh.triangles.Length);
            Assert.AreEqual(20, mesh.triangles.Length / 3);
        }

        [Test]
        public void Build_AllFaceNormalsPointOutwardFromCenter()
        {
            var mesh = ProceduralBlobMesh.Build(seed: 99, jitter: 0.3f, verticalSquash: 0.7f);
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var triangles = mesh.triangles;

            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                var faceCenter = (a + b + c) / 3f;
                var normal = normals[triangles[i]];

                Assert.Greater(Vector3.Dot(normal, faceCenter), 0f,
                    "Every face normal must point away from the blob's own center.");
            }
        }

        [Test]
        public void Awake_AssignsMeshToSiblingMeshFilter()
        {
            var go = new GameObject("TestBlob");

            try
            {
                var blob = go.AddComponent<ProceduralBlobMesh>();
                var filter = go.GetComponent<MeshFilter>();

                Assert.IsNotNull(filter, "ProceduralBlobMesh requires a MeshFilter.");
                Assert.IsNotNull(filter.sharedMesh, "Awake() should have assigned a generated mesh.");
                Assert.IsTrue(blob.enabled);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    public class Level01VisualSliceSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Worlds/Level_01_ForestDiorama.unity";

        [Test]
        public void Scene_HasOrganicPropsAndIntactDiscoverySystems()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var world = scene.GetRootGameObjects()[0];

                // Trees, bushes, rocks, and grass tufts all use the
                // procedural blob generator now -- rocks (0), tree
                // canopies (13), bushes (4), grass tufts (10) = 27, at
                // minimum.
                var blobs = world.GetComponentsInChildren<ProceduralBlobMesh>(true);
                Assert.GreaterOrEqual(blobs.Length, 27,
                    "Expected rocks/canopies/bushes/grass tufts to all use ProceduralBlobMesh.");

                // The discovery chain itself must be completely untouched
                // by the visual pass.
                Assert.AreEqual(6, world.GetComponentsInChildren<Discoverable>(true).Length);
                Assert.AreEqual(2, world.GetComponentsInChildren<CharacterMover>(true).Length);
                Assert.IsNotNull(world.GetComponentInChildren<DiscoveryManager>(true));
                Assert.IsNotNull(world.GetComponentInChildren<DiscoverySystem>(true));
                Assert.IsNotNull(world.GetComponentInChildren<FireworkEffect>(true));
                Assert.IsNotNull(world.GetComponentInChildren<CompletionCelebration>(true));

                // Discoverable targets are still plain (unjittered) spheres,
                // deliberately kept visually distinct from the organic
                // blob props around them.
                var targetMeshFilters = world.GetComponentsInChildren<Discoverable>(true)
                    .Select(d => d.GetComponent<MeshFilter>())
                    .Where(f => f != null);
                foreach (var filter in targetMeshFilters)
                {
                    Assert.IsNull(filter.GetComponent<ProceduralBlobMesh>());
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void Scene_CharactersHaveArmsAndBackpack()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var world = scene.GetRootGameObjects()[0];
                var movers = world.GetComponentsInChildren<CharacterMover>(true);
                Assert.AreEqual(2, movers.Length);

                foreach (var mover in movers)
                {
                    var model = mover.transform.Find("Model");
                    Assert.IsNotNull(model, $"{mover.name} should have a Model child.");
                    Assert.IsNotNull(model.Find("ArmLeft"));
                    Assert.IsNotNull(model.Find("ArmRight"));
                    Assert.IsNotNull(model.Find("Backpack"));
                    Assert.IsNotNull(model.Find("Body"));
                    Assert.IsNotNull(model.Find("Head"));
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
