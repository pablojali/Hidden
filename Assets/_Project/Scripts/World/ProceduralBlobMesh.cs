using UnityEngine;

namespace Hidden.World
{
    // M0.9 visual pass: builds a small, flat-shaded, low-poly "blob" mesh
    // (a jittered icosahedron, 20 triangles) once at startup and assigns
    // it to the sibling MeshFilter. One shared shape drives rocks, tree
    // canopies, bushes, grass tufts, and small character accessories --
    // each just a different seed/jitter/squash/scale/material combination
    // of the same generator -- so the diorama reads as organic, faceted
    // geometry instead of a collection of perfect Unity primitives,
    // without any external mesh asset, texture, or custom shader.
    //
    // Built once in Awake() from a seeded PRNG (deterministic per seed)
    // and never rebuilt at runtime -- no per-frame cost, and the vertex
    // count (20 triangles = 60 verts, unshared for flat shading) stays
    // small enough that dozens of instances remain mobile-cheap.
    // ExecuteAlways: without it, Awake() never runs outside Play Mode, so
    // the mesh would only appear once you press Play (or in a build),
    // leaving the Scene view empty while editing/browsing the level.
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    public class ProceduralBlobMesh : MonoBehaviour
    {
        [SerializeField] private int seed;
        [SerializeField] private float jitter = 0.18f;
        [SerializeField] private float verticalSquash = 1f;

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
            GetComponent<MeshFilter>().sharedMesh = Build(seed, jitter, verticalSquash);
        }

        public static Mesh Build(int seed, float jitter, float verticalSquash)
        {
            var rng = new System.Random(seed);
            var baseVertices = IcosahedronVertices();
            var faces = IcosahedronFaces();

            var jittered = new Vector3[baseVertices.Length];
            for (var i = 0; i < baseVertices.Length; i++)
            {
                var radiusScale = 1f + (float)(rng.NextDouble() * 2.0 - 1.0) * jitter;
                var vertex = baseVertices[i] * radiusScale;
                vertex.y *= verticalSquash;
                jittered[i] = vertex;
            }

            var vertexCount = faces.Length * 3;
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var triangles = new int[vertexCount];

            for (var f = 0; f < faces.Length; f++)
            {
                var face = faces[f];
                var a = jittered[face.Item1];
                var b = jittered[face.Item2];
                var c = jittered[face.Item3];

                var normal = Vector3.Cross(b - a, c - a).normalized;
                var center = (a + b + c) / 3f;

                // The reference face list isn't guaranteed to wind the
                // same way Unity expects for outward-facing/front-facing
                // triangles (there's no Editor here to check visually),
                // so self-correct from geometry: a properly outward face
                // normal must point away from the shape's own center.
                if (Vector3.Dot(normal, center) < 0f)
                {
                    (b, c) = (c, b);
                    normal = -normal;
                }

                var baseIndex = f * 3;
                vertices[baseIndex] = a;
                vertices[baseIndex + 1] = b;
                vertices[baseIndex + 2] = c;
                normals[baseIndex] = normal;
                normals[baseIndex + 1] = normal;
                normals[baseIndex + 2] = normal;
                triangles[baseIndex] = baseIndex;
                triangles[baseIndex + 1] = baseIndex + 1;
                triangles[baseIndex + 2] = baseIndex + 2;
            }

            var mesh = new Mesh { name = $"ProceduralBlob_{seed}" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Standard unit icosahedron (12 vertices from golden-ratio
        // permutations), radius-normalized to 0.5 so a default-scaled
        // instance reads as roughly a 1-unit blob, matching this
        // project's other primitive-based props.
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
