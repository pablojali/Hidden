using System.Collections.Generic;
using UnityEngine;

namespace Hidden.World
{
    // Reference-image visual correction: replaces jittered-icosahedron
    // "faceted gem" rocks with a smooth, shared-vertex icosphere (one
    // subdivision: 42 vertices, 80 triangles) displaced by a handful of
    // large, low-frequency bumps -- not per-vertex random noise, which is
    // exactly what made earlier rocks look like broken glass. A bump
    // pushes vertices near one random direction outward/inward with a
    // smooth angular falloff, so the result is a rounded, irregular,
    // non-radially-symmetric "eroded boulder" silhouette. Smooth-shaded
    // via Mesh.RecalculateNormals(). Different proportions/sizes between
    // instances come from the caller's own non-uniform Transform scale,
    // not from the mesh itself.
    // ExecuteAlways: without it, Awake() never runs outside Play Mode, so
    // the mesh would only appear once you press Play (or in a build),
    // leaving the Scene view empty while editing/browsing the level.
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    public class OrganicRockMesh : MonoBehaviour
    {
        [SerializeField] private int seed;
        [SerializeField] private int bumpCount = 4;
        [SerializeField] private float bumpStrength = 0.35f;
        [SerializeField] private float bumpSharpness = 3f;
        // Reference-image correction: see OrganicRevolutionMesh's flatShaded
        // for why -- false by default so every existing caller/test keeps
        // its original shared-vertex/smooth topology unless opted in.
        [SerializeField] private bool flatShaded;

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
            GetComponent<MeshFilter>().sharedMesh =
                Build(seed, bumpCount, bumpStrength, bumpSharpness, flatShaded);
        }

        public static Mesh Build(int seed, int bumpCount, float bumpStrength, float bumpSharpness,
            bool flatShaded = false)
        {
            var rng = new System.Random(seed);
            var (vertices, triangles) = BuildIcosphere();

            var bumpDirs = new Vector3[bumpCount];
            var bumpStrengths = new float[bumpCount];
            for (var i = 0; i < bumpCount; i++)
            {
                bumpDirs[i] = RandomUnitVector(rng);
                // Bumps are mostly outward protrusions (a real eroded
                // boulder reads as lumpy, not dented) with only mild
                // inward dents, and the final radius is clamped well
                // above zero so a stack of negative bumps can never
                // invert the surface locally.
                var isDent = rng.NextDouble() < 0.25;
                var magnitude = bumpStrength * (0.6f + (float)rng.NextDouble() * 0.8f);
                bumpStrengths[i] = isDent ? -magnitude * 0.5f : magnitude;
            }

            for (var i = 0; i < vertices.Count; i++)
            {
                var dir = vertices[i].normalized;
                var displacement = 0f;
                for (var b = 0; b < bumpCount; b++)
                {
                    var similarity = Mathf.Max(0f, Vector3.Dot(dir, bumpDirs[b]));
                    displacement += bumpStrengths[b] * Mathf.Pow(similarity, bumpSharpness);
                }
                var radius = Mathf.Max(0.25f, 0.5f + displacement);
                vertices[i] = dir * radius;
            }

            if (flatShaded)
            {
                Unshare(vertices, triangles);
            }

            var mesh = new Mesh { name = $"OrganicRock_{seed}" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Duplicates one vertex per triangle corner so no vertex is shared
        // between adjacent faces -- RecalculateNormals() then produces one
        // flat normal per face instead of an averaged smooth one.
        private static void Unshare(List<Vector3> vertices, List<int> triangles)
        {
            var unshared = new List<Vector3>(triangles.Count);
            for (var i = 0; i < triangles.Count; i++)
            {
                unshared.Add(vertices[triangles[i]]);
                triangles[i] = i;
            }

            vertices.Clear();
            vertices.AddRange(unshared);
        }

        private static Vector3 RandomUnitVector(System.Random rng)
        {
            // Uniform point on the unit sphere via the standard
            // rejection-free z/theta method.
            var z = (float)(rng.NextDouble() * 2.0 - 1.0);
            var theta = (float)(rng.NextDouble() * System.Math.PI * 2.0);
            var r = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
            return new Vector3(r * Mathf.Cos(theta), z, r * Mathf.Sin(theta));
        }

        // One-subdivision icosphere: the standard 12-vertex/20-face
        // icosahedron with each face split into 4 by inserting a shared
        // vertex at each edge's midpoint (cached per edge so adjacent
        // faces reuse the same new vertex), then re-normalized to the
        // unit sphere. Shared vertices throughout -- unlike
        // ProceduralBlobMesh's unshared, flat-shaded 20-triangle blob --
        // are what make smooth shading possible here.
        private static (List<Vector3>, List<int>) BuildIcosphere()
        {
            var t = (1f + Mathf.Sqrt(5f)) / 2f;
            var vertices = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            for (var i = 0; i < vertices.Count; i++)
            {
                vertices[i] = vertices[i].normalized * 0.5f;
            }

            var faces = new List<(int, int, int)>
            {
                (0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11),
                (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6), (7, 1, 8),
                (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9),
                (4, 9, 5), (2, 4, 11), (6, 2, 10), (8, 6, 7), (9, 8, 1),
            };

            // Correct each base face's winding before subdividing (same
            // "normal must point away from the shape's own center" check
            // ProceduralBlobMesh uses): subdivision preserves a parent
            // face's winding in all 4 of its children, so fixing the 20
            // base faces here is sufficient for the whole sphere.
            for (var f = 0; f < faces.Count; f++)
            {
                var (a, b, c) = faces[f];
                var normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                var center = (vertices[a] + vertices[b] + vertices[c]) / 3f;
                if (Vector3.Dot(normal, center) < 0f)
                {
                    faces[f] = (a, c, b);
                }
            }

            var midpointCache = new Dictionary<long, int>();

            int GetMidpoint(int a, int b)
            {
                var key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (midpointCache.TryGetValue(key, out var existing))
                {
                    return existing;
                }

                var mid = ((vertices[a] + vertices[b]) * 0.5f).normalized * 0.5f;
                vertices.Add(mid);
                var index = vertices.Count - 1;
                midpointCache[key] = index;
                return index;
            }

            var subdivided = new List<(int, int, int)>();
            foreach (var (a, b, c) in faces)
            {
                var ab = GetMidpoint(a, b);
                var bc = GetMidpoint(b, c);
                var ca = GetMidpoint(c, a);
                subdivided.Add((a, ab, ca));
                subdivided.Add((b, bc, ab));
                subdivided.Add((c, ca, bc));
                subdivided.Add((ab, bc, ca));
            }

            var triangles = new List<int>(subdivided.Count * 3);
            foreach (var (a, b, c) in subdivided)
            {
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }

            return (vertices, triangles);
        }
    }
}
