using System.Collections.Generic;
using UnityEngine;

namespace Hidden.World
{
    // M0.10 organic-density pass: a reusable generator that merges several
    // jittered icosahedron "lobes" (the same base shape as
    // ProceduralBlobMesh) into ONE mesh, at varied local offsets, radii,
    // and heights. One component, three visual families depending on
    // parameters: a tree canopy (lobes stacked vertically for layered
    // depth), a bush (lobes spread low and wide), or a rock formation
    // (fewer, more angular lobes with little vertical stacking). The
    // result reads as an irregular compound mass with a non-symmetrical
    // silhouette instead of a single smooth/faceted primitive, while
    // staying cheap: a handful of 20-triangle lobes merged into one draw
    // call, built once in Awake(), never rebuilt.
    // ExecuteAlways: without it, Awake() never runs outside Play Mode, so
    // the mesh would only appear once you press Play (or in a build),
    // leaving the Scene view empty while editing/browsing the level.
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    public class ProceduralClusterMesh : MonoBehaviour
    {
        [SerializeField] private int seed;
        [SerializeField] private int lobeCount = 4;
        [SerializeField] private float baseRadius = 0.5f;
        [SerializeField] private float radiusVariance = 0.35f;
        [SerializeField] private float jitter = 0.22f;
        [SerializeField] private float verticalSquash = 0.8f;
        [SerializeField] private float spreadRadius = 0.35f;
        [SerializeField] private float verticalSpread = 0.3f;

        private void Awake()
        {
            Rebuild();
        }

        // Unity does not call Awake() for a plain (non-ExecuteAlways)
        // MonoBehaviour outside Play Mode, so EditMode tests can't rely on
        // AddComponent triggering it (same reason CharacterMover exposes
        // Initialize()). Exposed publicly so tests can drive it directly.
        public void Rebuild()
        {
            GetComponent<MeshFilter>().sharedMesh = Build(
                seed, lobeCount, baseRadius, radiusVariance, jitter, verticalSquash, spreadRadius, verticalSpread);
        }

        public static Mesh Build(int seed, int lobeCount, float baseRadius, float radiusVariance,
            float jitter, float verticalSquash, float spreadRadius, float verticalSpread)
        {
            var rng = new System.Random(seed);
            var faces = IcosahedronFaces();
            var unitVertices = IcosahedronVertices();

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            for (var lobe = 0; lobe < lobeCount; lobe++)
            {
                // Lobes are biased to stack upward with index, so the
                // cluster reads as layered depth (lower/outer masses vs.
                // upper/inner ones) rather than a flat ring of blobs.
                var heightT = lobeCount <= 1 ? 0f : (float)lobe / (lobeCount - 1);
                var angle = (float)(rng.NextDouble() * System.Math.PI * 2.0);
                var radialDist = (float)rng.NextDouble() * spreadRadius;
                var offset = new Vector3(
                    Mathf.Cos(angle) * radialDist,
                    heightT * verticalSpread + (float)(rng.NextDouble() - 0.5) * verticalSpread * 0.3f,
                    Mathf.Sin(angle) * radialDist);

                var lobeRadius = baseRadius * (1f + (float)(rng.NextDouble() * 2.0 - 1.0) * radiusVariance);
                var lobeRng = new System.Random(rng.Next());

                var jitteredUnit = new Vector3[unitVertices.Length];
                for (var i = 0; i < unitVertices.Length; i++)
                {
                    var radiusScale = 1f + (float)(lobeRng.NextDouble() * 2.0 - 1.0) * jitter;
                    // Unit vertices have magnitude 0.5, so doubling here
                    // makes the final lobe's own radius equal lobeRadius,
                    // matching the "radius = final world size" convention
                    // ProceduralBlobMesh/emit_blob_prop already use.
                    var vertex = unitVertices[i] * radiusScale * lobeRadius * 2f;
                    vertex.y *= verticalSquash;
                    jitteredUnit[i] = vertex;
                }

                for (var f = 0; f < faces.Length; f++)
                {
                    var face = faces[f];
                    var localA = jitteredUnit[face.Item1];
                    var localB = jitteredUnit[face.Item2];
                    var localC = jitteredUnit[face.Item3];

                    var normal = Vector3.Cross(localB - localA, localC - localA).normalized;
                    var localCenter = (localA + localB + localC) / 3f;

                    var a = localA + offset;
                    var b = localB + offset;
                    var c = localC + offset;

                    // Winding is verified in the lobe's own local space
                    // (before the cluster offset is applied): an outward
                    // face must point away from that lobe's own center,
                    // same reasoning ProceduralBlobMesh uses per-shape.
                    if (Vector3.Dot(normal, localCenter) < 0f)
                    {
                        (b, c) = (c, b);
                        normal = -normal;
                    }

                    var baseIndex = vertices.Count;
                    vertices.Add(a);
                    vertices.Add(b);
                    vertices.Add(c);
                    normals.Add(normal);
                    normals.Add(normal);
                    normals.Add(normal);
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 2);
                }
            }

            var mesh = new Mesh { name = $"ProceduralCluster_{seed}" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Same unit icosahedron as ProceduralBlobMesh (12 vertices,
        // radius-normalized to 0.5) -- each lobe is built from it.
        private static Vector3[] IcosahedronVertices()
        {
            var t = (1f + Mathf.Sqrt(5f)) / 2f;
            var vertices = new[]
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };

            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i] = vertices[i].normalized * 0.5f;
            }

            return vertices;
        }

        private static (int, int, int)[] IcosahedronFaces()
        {
            return new[]
            {
                (0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11),
                (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6), (7, 1, 8),
                (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9),
                (4, 9, 5), (2, 4, 11), (6, 2, 10), (8, 6, 7), (9, 8, 1),
            };
        }
    }
}
