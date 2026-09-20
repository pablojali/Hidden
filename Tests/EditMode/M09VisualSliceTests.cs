using System.Linq;
using System.Reflection;
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

    // ProceduralConeMesh and ProceduralClusterMesh are no longer used by
    // Level_01_ForestDiorama: cones were replaced by OrganicRevolutionMesh
    // canopies (M0.10 round 2), and the merged-lobe cluster approach that
    // replaced them in between was itself replaced by OrganicRevolutionMesh
    // (canopies/bushes) and OrganicRockMesh (rocks) after explicit
    // feedback that flat-shaded, high-frequency-jittered lobes still read
    // as "faceted primitives," not organic shapes. Both scripts are kept,
    // still correct and still unit-tested, in case a future shape wants
    // their specific silhouette.
    public class ProceduralConeMeshTests
    {
        [Test]
        public void Build_SameSeed_IsDeterministic()
        {
            var meshA = ProceduralConeMesh.Build(seed: 5, sides: 8, radiusJitter: 0.1f);
            var meshB = ProceduralConeMesh.Build(seed: 5, sides: 8, radiusJitter: 0.1f);

            CollectionAssert.AreEqual(meshA.vertices, meshB.vertices);
        }

        [Test]
        public void Build_ProducesExpectedTriangleCount()
        {
            var mesh = ProceduralConeMesh.Build(seed: 3, sides: 8, radiusJitter: 0.08f);

            // One triangle per side face, unshared vertices for flat
            // shading, no base cap (hidden by the trunk).
            Assert.AreEqual(24, mesh.vertexCount);
            Assert.AreEqual(8, mesh.triangles.Length / 3);
        }

        [Test]
        public void Build_AllFaceNormalsPointRadiallyOutward()
        {
            var mesh = ProceduralConeMesh.Build(seed: 11, sides: 9, radiusJitter: 0.15f);
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var triangles = mesh.triangles;

            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                var faceCenter = (a + b + c) / 3f;
                var outward = new Vector3(faceCenter.x, 0f, faceCenter.z);
                var normal = normals[triangles[i]];

                Assert.Greater(Vector3.Dot(normal, outward), 0f,
                    "Every side face normal must point away from the cone's central (Y) axis.");
            }
        }

        [Test]
        public void Awake_AssignsMeshToSiblingMeshFilter()
        {
            var go = new GameObject("TestCone");

            try
            {
                go.AddComponent<ProceduralConeMesh>();
                var filter = go.GetComponent<MeshFilter>();

                Assert.IsNotNull(filter.sharedMesh, "Awake() should have assigned a generated cone mesh.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    public class ProceduralClusterMeshTests
    {
        [Test]
        public void Build_SameSeed_IsDeterministic()
        {
            var meshA = ProceduralClusterMesh.Build(12, 4, 0.5f, 0.3f, 0.2f, 0.8f, 0.4f, 0.3f);
            var meshB = ProceduralClusterMesh.Build(12, 4, 0.5f, 0.3f, 0.2f, 0.8f, 0.4f, 0.3f);

            CollectionAssert.AreEqual(meshA.vertices, meshB.vertices);
        }

        [Test]
        public void Build_TriangleAndVertexCountScaleWithLobeCount()
        {
            var mesh = ProceduralClusterMesh.Build(3, 5, 0.5f, 0.3f, 0.2f, 0.8f, 0.4f, 0.3f);

            Assert.AreEqual(5 * 20, mesh.triangles.Length / 3);
            Assert.AreEqual(5 * 60, mesh.vertexCount);
        }

        [Test]
        public void Awake_AssignsMeshToSiblingMeshFilter()
        {
            var go = new GameObject("TestCluster");

            try
            {
                go.AddComponent<ProceduralClusterMesh>();
                var filter = go.GetComponent<MeshFilter>();

                Assert.IsNotNull(filter.sharedMesh, "Awake() should have assigned a generated cluster mesh.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    // ProceduralTrunkMesh: replaces a plain MESH_CYLINDER trunk with a
    // tapered, leaning, jittered-cross-section tube. Build()'s signature
    // and triangle/vertex counts are unchanged from earlier rounds; the
    // implementation now shares vertices between adjacent rings/sides and
    // shades via Mesh.RecalculateNormals() instead of flat per-face
    // normals, so it reads as a smoothly rounded tube.
    public class ProceduralTrunkMeshTests
    {
        [Test]
        public void Build_SameSeed_IsDeterministic()
        {
            var meshA = ProceduralTrunkMesh.Build(4, 6, 4, 0.16f, 0.06f, 1.6f, 0.16f, 0.12f);
            var meshB = ProceduralTrunkMesh.Build(4, 6, 4, 0.16f, 0.06f, 1.6f, 0.16f, 0.12f);

            CollectionAssert.AreEqual(meshA.vertices, meshB.vertices);
        }

        [Test]
        public void Build_ProducesExpectedTriangleCount()
        {
            var mesh = ProceduralTrunkMesh.Build(4, 6, 4, 0.16f, 0.06f, 1.6f, 0.16f, 0.12f);

            // 2 triangles per side quad (sides * heightSegments quads) plus
            // one fan triangle per side for the top cap.
            const int sides = 6;
            const int heightSegments = 4;
            var expectedTriangles = sides * heightSegments * 2 + sides;
            Assert.AreEqual(expectedTriangles, mesh.triangles.Length / 3);
        }

        [Test]
        public void Build_TopRingSitsNearConfiguredHeight()
        {
            const float height = 1.6f;
            var mesh = ProceduralTrunkMesh.Build(7, 6, 4, 0.16f, 0.06f, height, 0.1f, 0.1f);

            var maxY = mesh.vertices.Max(v => v.y);
            Assert.AreEqual(height, maxY, 0.01f);
        }

        [Test]
        public void Build_NormalsAreUnitLength()
        {
            var mesh = ProceduralTrunkMesh.Build(2, 6, 4, 0.16f, 0.06f, 1.6f, 0.16f, 0.12f);

            foreach (var normal in mesh.normals)
            {
                Assert.AreEqual(1f, normal.magnitude, 0.01f, "Smooth-shaded normals must be unit length.");
            }
        }

        [Test]
        public void Awake_AssignsMeshToSiblingMeshFilter()
        {
            var go = new GameObject("TestTrunk");

            try
            {
                go.AddComponent<ProceduralTrunkMesh>();
                var filter = go.GetComponent<MeshFilter>();

                Assert.IsNotNull(filter.sharedMesh, "Awake() should have assigned a generated trunk mesh.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    // OrganicRevolutionMesh: the core of the second reference-image visual
    // correction round. A single smooth, shared-vertex mesh revolved from
    // a hand-authored profile curve -- replaces cones and merged jittered
    // lobes as the shape for tree canopies, bush clumps, mushroom caps,
    // and flower blooms. Shading comes from Mesh.RecalculateNormals()
    // over shared vertices, not per-face flat normals.
    public class OrganicRevolutionMeshTests
    {
        private static readonly float[] Heights = { 0f, 0.2f, 0.5f, 0.8f, 1.2f };
        private static readonly float[] Radii = { 0f, 0.4f, 0.55f, 0.35f, 0f };

        [Test]
        public void Build_SameSeed_IsDeterministic()
        {
            var meshA = OrganicRevolutionMesh.Build(8, 10, Heights, Radii, 0.1f, 0.05f);
            var meshB = OrganicRevolutionMesh.Build(8, 10, Heights, Radii, 0.1f, 0.05f);

            CollectionAssert.AreEqual(meshA.vertices, meshB.vertices);
        }

        [Test]
        public void Build_DifferentSeed_ProducesDifferentShape()
        {
            var meshA = OrganicRevolutionMesh.Build(1, 10, Heights, Radii, 0.15f, 0.08f);
            var meshB = OrganicRevolutionMesh.Build(2, 10, Heights, Radii, 0.15f, 0.08f);

            CollectionAssert.AreNotEqual(meshA.vertices, meshB.vertices);
        }

        [Test]
        public void Build_PolesShareASingleVertexNotAFullRing()
        {
            // Heights/Radii both start and end at radius 0 (the poles),
            // so vertex count = (ringCount - 2) full rings * sides + 2
            // pole vertices, not ringCount * sides.
            const int sides = 10;
            var mesh = OrganicRevolutionMesh.Build(4, sides, Heights, Radii, 0.1f, 0.05f);

            var expectedVertices = (Heights.Length - 2) * sides + 2;
            Assert.AreEqual(expectedVertices, mesh.vertexCount);
        }

        [Test]
        public void Build_NormalsAreUnitLength()
        {
            var mesh = OrganicRevolutionMesh.Build(6, 9, Heights, Radii, 0.12f, 0.06f);

            foreach (var normal in mesh.normals)
            {
                Assert.AreEqual(1f, normal.magnitude, 0.01f, "Smooth-shaded normals must be unit length.");
            }
        }

        [Test]
        public void Awake_AssignsMeshToSiblingMeshFilter()
        {
            var go = new GameObject("TestRevolution");

            try
            {
                go.AddComponent<OrganicRevolutionMesh>();
                var filter = go.GetComponent<MeshFilter>();

                Assert.IsNotNull(filter.sharedMesh, "Awake() should have assigned a generated revolution mesh.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    // OrganicRockMesh: a smooth, shared-vertex icosphere (one subdivision)
    // displaced by a handful of large, low-frequency bumps -- not
    // per-vertex noise -- so it reads as a rounded, irregular "eroded
    // boulder" instead of a faceted gem cluster.
    public class OrganicRockMeshTests
    {
        [Test]
        public void Build_SameSeed_IsDeterministic()
        {
            var meshA = OrganicRockMesh.Build(3, 4, 0.35f, 3f);
            var meshB = OrganicRockMesh.Build(3, 4, 0.35f, 3f);

            CollectionAssert.AreEqual(meshA.vertices, meshB.vertices);
        }

        [Test]
        public void Build_DifferentSeed_ProducesDifferentShape()
        {
            var meshA = OrganicRockMesh.Build(1, 4, 0.35f, 3f);
            var meshB = OrganicRockMesh.Build(2, 4, 0.35f, 3f);

            CollectionAssert.AreNotEqual(meshA.vertices, meshB.vertices);
        }

        [Test]
        public void Build_ProducesOneSubdivisionIcosphereTopology()
        {
            // 12 base vertices + 30 unique edge midpoints = 42 vertices,
            // 20 base faces * 4 = 80 triangles -- fixed regardless of bump
            // parameters, since bumps only displace existing vertices.
            var mesh = OrganicRockMesh.Build(5, 3, 0.2f, 2f);

            Assert.AreEqual(42, mesh.vertexCount);
            Assert.AreEqual(80, mesh.triangles.Length / 3);
        }

        [Test]
        public void Build_AllFaceNormalsPointOutwardFromCenter()
        {
            var mesh = OrganicRockMesh.Build(11, 5, 0.3f, 2.5f);
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
                    "Every face normal must point away from the rock's own center, bumps included.");
            }
        }

        [Test]
        public void Build_NeverProducesADegenerateOrInvertedRadius()
        {
            // Several negative (dent) bumps stacking on the same vertex
            // must never push the local radius to zero or below.
            var mesh = OrganicRockMesh.Build(42, 6, 0.5f, 1.5f);

            foreach (var vertex in mesh.vertices)
            {
                Assert.Greater(vertex.magnitude, 0.2f,
                    "No vertex should collapse toward the center regardless of bump stacking.");
            }
        }

        [Test]
        public void Awake_AssignsMeshToSiblingMeshFilter()
        {
            var go = new GameObject("TestRock");

            try
            {
                go.AddComponent<OrganicRockMesh>();
                var filter = go.GetComponent<MeshFilter>();

                Assert.IsNotNull(filter.sharedMesh, "Awake() should have assigned a generated rock mesh.");
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
        public void Scene_TreesUseSmoothTrunkAndRevolutionCanopyNotPrimitives()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var world = scene.GetRootGameObjects()[0];

                // Bare trees (no canopy, by design) also have a "Trunk"
                // child, so they're excluded from the trunk/canopy 1:1
                // comparison below but still checked for the mesh type.
                var allTrunks = world.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name == "Trunk" && t.parent != null && t.parent.parent != null
                                && t.parent.parent.name == "Props")
                    .ToList();
                Assert.GreaterOrEqual(allTrunks.Count, 25, "Expected many tree trunks in the Props group.");
                foreach (var trunk in allTrunks)
                {
                    Assert.IsNotNull(trunk.GetComponent<ProceduralTrunkMesh>(),
                        $"{trunk.parent.name}'s Trunk should use ProceduralTrunkMesh, not a plain cylinder.");
                }

                var canopiedTrunks = allTrunks.Where(t => !t.parent.name.StartsWith("BareTree_")).ToList();
                var canopies = world.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name == "Canopy" && t.parent != null && t.parent.parent != null
                                && t.parent.parent.name == "Props")
                    .ToList();
                Assert.AreEqual(canopiedTrunks.Count, canopies.Count,
                    "Every non-bare tree should have exactly one canopy.");
                foreach (var canopy in canopies)
                {
                    Assert.IsNotNull(canopy.GetComponent<OrganicRevolutionMesh>(),
                        $"{canopy.parent.name}'s Canopy should use OrganicRevolutionMesh, not a cone or merged lobes.");
                    Assert.IsNull(canopy.GetComponent<ProceduralConeMesh>());
                    Assert.IsNull(canopy.GetComponent<ProceduralClusterMesh>());
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void Scene_RocksAreOrganicRockMeshAndBushesAreOverlappingClumps()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var world = scene.GetRootGameObjects()[0];

                // Every named "Rock*"/"ScatterRock*" object is ONE smooth
                // OrganicRockMesh (an icosphere with a few large bumps),
                // not several merged flat-shaded lobes and not a single
                // faceted blob.
                var rocks = world.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name.StartsWith("Rock_") || t.name.StartsWith("ScatterRock_"))
                    .ToList();
                Assert.GreaterOrEqual(rocks.Count, 25, "Expected many named rock formations across the map.");
                foreach (var rock in rocks)
                {
                    Assert.IsNotNull(rock.GetComponent<OrganicRockMesh>(),
                        $"{rock.name} should be a single OrganicRockMesh formation.");
                    Assert.IsNull(rock.GetComponent<ProceduralClusterMesh>());
                }

                // Every named "Bush*"/"ScatterBush*" object is a root with
                // two "Clump_*" children, each an OrganicRevolutionMesh --
                // several overlapping volumes, not one spherical primitive.
                var bushes = world.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name.StartsWith("Bush_") || t.name.StartsWith("ScatterBush_"))
                    .ToList();
                Assert.GreaterOrEqual(bushes.Count, 25, "Expected many named bushes across the map.");
                foreach (var bush in bushes)
                {
                    var clumps = bush.Cast<Transform>().Where(c => c.name.StartsWith("Clump_")).ToList();
                    Assert.AreEqual(2, clumps.Count, $"{bush.name} should have exactly 2 foliage clumps.");
                    foreach (var clump in clumps)
                    {
                        Assert.IsNotNull(clump.GetComponent<OrganicRevolutionMesh>(),
                            $"{bush.name}'s {clump.name} should use OrganicRevolutionMesh.");
                    }
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void Scene_HasSmallVegetationVariantsAndDenseScatterAcrossFullFootprint()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var world = scene.GetRootGameObjects()[0];
                var allTransforms = world.GetComponentsInChildren<Transform>(true);

                // Density hierarchy: small-tier ground detail (pebbles,
                // grass, flower clusters, mushrooms) should vastly
                // outnumber the medium/large tiers.
                var pebbles = allTransforms.Count(t => t.name.Contains("Pebble"));
                var grass = allTransforms.Count(t => t.name.Contains("Grass"));
                var flowers = allTransforms.Count(t => t.name.StartsWith("Flowers_")
                    || t.name.StartsWith("ScatterFlowers_"));
                var mushrooms = allTransforms.Count(t => t.name.StartsWith("Mushroom_")
                    || t.name.StartsWith("ScatterMushroom_"));
                Assert.GreaterOrEqual(pebbles, 40, "Expected dense small-stone ground detail.");
                Assert.GreaterOrEqual(grass, 40, "Expected dense grass-tuft ground detail.");
                Assert.GreaterOrEqual(flowers, 15, "Expected flower-cluster ground detail.");
                Assert.GreaterOrEqual(mushrooms, 5, "Expected mushroom ground detail (a named small-vegetation variant).");

                // The world should read as populated edge to edge, not just
                // around the center: at least one Props-group object sits
                // well into each of the four outer directions.
                var propsGroup = world.transform.Find("Props");
                Assert.IsNotNull(propsGroup);
                var propPositions = propsGroup.GetComponentsInChildren<Transform>(true)
                    .Select(t => t.position)
                    .ToList();
                Assert.IsTrue(propPositions.Any(p => p.x < -15f), "Expected content reaching the west edge.");
                Assert.IsTrue(propPositions.Any(p => p.x > 15f), "Expected content reaching the east edge.");
                Assert.IsTrue(propPositions.Any(p => p.z < -15f), "Expected content reaching the south edge.");
                Assert.IsTrue(propPositions.Any(p => p.z > 10f), "Expected content reaching the north edge.");

                // The discovery chain must still be completely untouched by
                // the density increase.
                Assert.AreEqual(6, world.GetComponentsInChildren<Discoverable>(true).Length);
                Assert.AreEqual(2, world.GetComponentsInChildren<CharacterMover>(true).Length);
                Assert.IsNotNull(world.GetComponentInChildren<DiscoveryManager>(true));
                Assert.IsNotNull(world.GetComponentInChildren<DiscoverySystem>(true));
                Assert.IsNotNull(world.GetComponentInChildren<FireworkEffect>(true));
                Assert.IsNotNull(world.GetComponentInChildren<CompletionCelebration>(true));

                // Discoverable targets are still plain (unjittered) spheres,
                // deliberately kept visually distinct from every organic
                // prop around them.
                var targetMeshFilters = world.GetComponentsInChildren<Discoverable>(true)
                    .Select(d => d.GetComponent<MeshFilter>())
                    .Where(f => f != null);
                foreach (var filter in targetMeshFilters)
                {
                    Assert.IsNull(filter.GetComponent<ProceduralBlobMesh>());
                    Assert.IsNull(filter.GetComponent<OrganicRockMesh>());
                    Assert.IsNull(filter.GetComponent<OrganicRevolutionMesh>());
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
                    var head = model.Find("Head");
                    Assert.IsNotNull(head);

                    Assert.IsNotNull(head.GetComponent<ProceduralBlobMesh>(),
                        $"{mover.name}'s Head should use ProceduralBlobMesh for a faceted look.");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void Scene_HasExpandedMapWithMountainRiverDockAndBoat()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var world = scene.GetRootGameObjects()[0];

                var mountain = world.transform.Find("Environment/Mountain");
                Assert.IsNotNull(mountain);
                var peak = mountain.Find("Peak");
                Assert.IsNotNull(peak);
                Assert.IsNotNull(peak.GetComponent<OrganicRockMesh>(),
                    "The mountain's Peak should be a smooth OrganicRockMesh mass, not merged lobes.");

                Assert.IsNotNull(world.transform.Find("Environment/Waterfall"));
                Assert.IsNotNull(world.transform.Find("Environment/MountainTerrace"));
                Assert.IsNotNull(world.transform.Find("Structures/Dock"));
                Assert.IsNotNull(world.transform.Find("Props/Boat"));

                // Thick two-tone terrain block (reference-image "visible
                // sides" requirement): a tall TerrainBase under the thin
                // green DioramaBase slab, whose top surface must stay at
                // exactly y=0 since every prop assumes that ground level.
                var terrainBase = world.transform.Find("Environment/TerrainBase");
                var dioramaBase = world.transform.Find("Environment/DioramaBase");
                Assert.IsNotNull(terrainBase, "Expected a TerrainBase block for the visible terrain sides.");
                Assert.IsNotNull(dioramaBase);
                Assert.AreEqual(0f, dioramaBase.position.y + dioramaBase.localScale.y / 2f, 0.0001f,
                    "DioramaBase's top surface must stay at world y=0.");

                // Gentle elevation variation: a handful of low, smooth
                // terrain knolls (OrganicRockMesh, heavily flattened).
                var knolls = world.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name.StartsWith("TerrainKnoll_"))
                    .ToList();
                Assert.GreaterOrEqual(knolls.Count, 4, "Expected several gentle terrain elevation knolls.");
                foreach (var knoll in knolls)
                {
                    Assert.IsNotNull(knoll.GetComponent<OrganicRockMesh>());
                }

                var riverSegments = world.GetComponentsInChildren<Transform>(true)
                    .Count(t => t.name.StartsWith("River_"));
                Assert.GreaterOrEqual(riverSegments, 5, "River should be built from several chained segments.");

                var cameraRig = world.transform.Find("Camera");
                var controller = cameraRig.GetComponent<Hidden.Camera.DioramaCameraController>();
                var maxZoomField = typeof(Hidden.Camera.DioramaCameraController)
                    .GetField("maxZoomDistance", BindingFlags.NonPublic | BindingFlags.Instance);
                var maxZoom = (float)maxZoomField.GetValue(controller);
                Assert.Greater(maxZoom, 65f, "maxZoomDistance should have grown along with the enlarged map.");

                // The discovery chain must still be exactly as before: the
                // map got bigger and busier, the loop did not change.
                Assert.AreEqual(6, world.GetComponentsInChildren<Discoverable>(true).Length);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
