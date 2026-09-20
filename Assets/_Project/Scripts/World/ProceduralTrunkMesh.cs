using System.Collections.Generic;
using UnityEngine;

namespace Hidden.World
{
    // M0.10 organic-density pass: replaces a plain MESH_CYLINDER trunk
    // with a small tapered tube that leans slightly to one side, has a
    // jittered (non-circular) cross-section, and a jittered radius per
    // ring -- the cheapest possible set of cues that read as "trunk"
    // rather than "cylinder": taper, lean, and irregularity. Built once
    // in Awake() from a seeded PRNG, capped on top so it isn't hollow
    // where a canopy doesn't fully cover it.
    [RequireComponent(typeof(MeshFilter))]
    public class ProceduralTrunkMesh : MonoBehaviour
    {
        [SerializeField] private int seed;
        [SerializeField] private int sides = 6;
        [SerializeField] private int heightSegments = 4;
        [SerializeField] private float baseRadius = 0.15f;
        [SerializeField] private float topRadius = 0.07f;
        [SerializeField] private float height = 1.4f;
        [SerializeField] private float radiusJitter = 0.15f;
        [SerializeField] private float leanAmount = 0.12f;

        private void Awake()
        {
            GetComponent<MeshFilter>().sharedMesh = Build(
                seed, sides, heightSegments, baseRadius, topRadius, height, radiusJitter, leanAmount);
        }

        public static Mesh Build(int seed, int sides, int heightSegments, float baseRadius, float topRadius,
            float height, float radiusJitter, float leanAmount)
        {
            var rng = new System.Random(seed);
            var ringCount = heightSegments + 1;

            // One random lean direction for the whole trunk, strongest
            // partway up (eased with a sine) -- a real trunk rarely grows
            // perfectly straight, and this single cue does more for the
            // "trunk, not cylinder" read than any amount of surface noise.
            var leanAngle = (float)(rng.NextDouble() * System.Math.PI * 2.0);
            var leanDir = new Vector3(Mathf.Cos(leanAngle), 0f, Mathf.Sin(leanAngle));

            var ringCenters = new Vector3[ringCount];
            var ringRadii = new float[ringCount];
            for (var r = 0; r < ringCount; r++)
            {
                var t = (float)r / heightSegments;
                var bend = leanDir * (Mathf.Sin(t * Mathf.PI * 0.5f) * leanAmount);
                ringCenters[r] = new Vector3(bend.x, t * height, bend.z);
                var lerpedRadius = Mathf.Lerp(baseRadius, topRadius, t);
                ringRadii[r] = lerpedRadius * (1f + (float)(rng.NextDouble() * 2.0 - 1.0) * radiusJitter);
            }

            // Per-side angular jitter kept constant up the whole trunk so
            // the cross-section itself reads as irregular bark rather
            // than a perfect circle, without the silhouette twisting.
            var sideJitter = new float[sides];
            for (var s = 0; s < sides; s++)
            {
                sideJitter[s] = 1f + (float)(rng.NextDouble() * 2.0 - 1.0) * radiusJitter * 0.5f;
            }

            var ring = new Vector3[ringCount, sides];
            for (var r = 0; r < ringCount; r++)
            {
                for (var s = 0; s < sides; s++)
                {
                    var angle = (float)s / sides * Mathf.PI * 2f;
                    var radius = ringRadii[r] * sideJitter[s];
                    var local = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    ring[r, s] = ringCenters[r] + local;
                }
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            for (var r = 0; r < heightSegments; r++)
            {
                var axisPoint = (ringCenters[r] + ringCenters[r + 1]) / 2f;
                for (var s = 0; s < sides; s++)
                {
                    var sNext = (s + 1) % sides;
                    var a = ring[r, s];
                    var b = ring[r, sNext];
                    var c = ring[r + 1, s];
                    var d = ring[r + 1, sNext];

                    AddTriangle(vertices, normals, triangles, a, b, c, axisPoint);
                    AddTriangle(vertices, normals, triangles, b, d, c, axisPoint);
                }
            }

            AddCap(vertices, normals, triangles, ring, heightSegments, sides, ringCenters[heightSegments]);

            var mesh = new Mesh { name = $"ProceduralTrunk_{seed}" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // A side face's correct outward normal points away from the
        // trunk's own central axis at that height (approximated by the
        // midpoint between the two rings the face spans), not from the
        // world origin -- so the lean doesn't throw off the winding check.
        private static void AddTriangle(List<Vector3> vertices, List<Vector3> normals, List<int> triangles,
            Vector3 a, Vector3 b, Vector3 c, Vector3 axisPoint)
        {
            var normal = Vector3.Cross(b - a, c - a).normalized;
            var faceCenter = (a + b + c) / 3f;
            var outward = faceCenter - axisPoint;
            outward.y = 0f;

            if (Vector3.Dot(normal, outward) < 0f)
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

        // A simple fan cap over the top ring, self-corrected to face
        // upward (a cap's only sensible outward direction) rather than
        // radially.
        private static void AddCap(List<Vector3> vertices, List<Vector3> normals, List<int> triangles,
            Vector3[,] ring, int topRing, int sides, Vector3 center)
        {
            for (var s = 0; s < sides; s++)
            {
                var sNext = (s + 1) % sides;
                var a = ring[topRing, s];
                var b = ring[topRing, sNext];
                var normal = Vector3.Cross(b - a, center - a).normalized;

                if (normal.y < 0f)
                {
                    (a, b) = (b, a);
                    normal = -normal;
                }

                var baseIndex = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(center);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
            }
        }
    }
}
