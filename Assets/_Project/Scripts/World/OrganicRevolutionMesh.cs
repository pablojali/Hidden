using System.Collections.Generic;
using UnityEngine;

namespace Hidden.World
{
    // Reference-image visual correction: replaces the earlier flat-shaded,
    // high-frequency-jittered lobe clusters (ProceduralClusterMesh) with a
    // single smooth, shared-vertex mesh built by revolving a designer-
    // authored profile curve (height/radius control points) around the Y
    // axis. One component, many purpose-built organic shapes -- tree
    // canopies, bush clumps, mushroom caps, flower blooms -- depending
    // only on which profile is supplied.
    //
    // The single highest-leverage change here is smooth shading: vertices
    // are shared between adjacent faces and Mesh.RecalculateNormals()
    // averages them, so the result reads as a rounded organic mass
    // instead of a "collection of triangles." Asymmetry comes from one
    // low-frequency lean/bulge direction for the whole shape (like
    // ProceduralTrunkMesh's lean), not per-vertex noise -- high-frequency
    // per-vertex jitter is exactly what made the earlier shapes look
    // faceted/crystalline.
    // ExecuteAlways: without it, Awake() never runs outside Play Mode, so
    // the mesh would only appear once you press Play (or in a build),
    // leaving the Scene view empty while editing/browsing the level.
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    public class OrganicRevolutionMesh : MonoBehaviour
    {
        [SerializeField] private int seed;
        [SerializeField] private int sides = 10;
        [SerializeField] private float[] profileHeights = { 0f, 0.5f, 1f };
        [SerializeField] private float[] profileRadii = { 0f, 0.5f, 0f };
        [SerializeField] private float asymmetry = 0.12f;
        [SerializeField] private float radiusJitter = 0.05f;

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
                Build(seed, sides, profileHeights, profileRadii, asymmetry, radiusJitter);
        }

        public static Mesh Build(int seed, int sides, float[] profileHeights, float[] profileRadii,
            float asymmetry, float radiusJitter)
        {
            var rng = new System.Random(seed);
            var ringCount = profileHeights.Length;
            var minH = profileHeights[0];
            var maxH = profileHeights[ringCount - 1];
            var span = Mathf.Max(0.0001f, maxH - minH);

            // One low-frequency lean/bulge direction for the whole shape,
            // eased to zero at the poles (where radius is already ~0) so
            // the cap doesn't visibly detach from the body.
            var leanAngle = (float)(rng.NextDouble() * System.Math.PI * 2.0);
            var leanDir = new Vector3(Mathf.Cos(leanAngle), 0f, Mathf.Sin(leanAngle));

            var ringCenters = new Vector3[ringCount];
            var ringRadii = new float[ringCount];
            for (var r = 0; r < ringCount; r++)
            {
                var t = (profileHeights[r] - minH) / span;
                var bulge = Mathf.Sin(t * Mathf.PI) * asymmetry;
                ringCenters[r] = new Vector3(leanDir.x * bulge, profileHeights[r], leanDir.z * bulge);
                ringRadii[r] = Mathf.Max(0f,
                    profileRadii[r] * (1f + (float)(rng.NextDouble() * 2.0 - 1.0) * radiusJitter));
            }

            var vertices = new List<Vector3>();
            var ringVertexStart = new int[ringCount];
            for (var r = 0; r < ringCount; r++)
            {
                ringVertexStart[r] = vertices.Count;
                if (ringRadii[r] <= 0.0001f)
                {
                    // A pole (top/bottom tip): one shared vertex rather
                    // than a degenerate ring of coincident vertices.
                    vertices.Add(ringCenters[r]);
                }
                else
                {
                    for (var s = 0; s < sides; s++)
                    {
                        var angle = (float)s / sides * Mathf.PI * 2f;
                        var local = new Vector3(Mathf.Cos(angle) * ringRadii[r], 0f,
                            Mathf.Sin(angle) * ringRadii[r]);
                        vertices.Add(ringCenters[r] + local);
                    }
                }
            }

            var triangles = new List<int>();
            for (var r = 0; r < ringCount - 1; r++)
            {
                var thisIsPole = ringRadii[r] <= 0.0001f;
                var nextIsPole = ringRadii[r + 1] <= 0.0001f;

                if (thisIsPole)
                {
                    var pole = ringVertexStart[r];
                    for (var s = 0; s < sides; s++)
                    {
                        var sNext = (s + 1) % sides;
                        triangles.Add(pole);
                        triangles.Add(ringVertexStart[r + 1] + s);
                        triangles.Add(ringVertexStart[r + 1] + sNext);
                    }
                }
                else if (nextIsPole)
                {
                    var pole = ringVertexStart[r + 1];
                    for (var s = 0; s < sides; s++)
                    {
                        var sNext = (s + 1) % sides;
                        triangles.Add(ringVertexStart[r] + s);
                        triangles.Add(pole);
                        triangles.Add(ringVertexStart[r] + sNext);
                    }
                }
                else
                {
                    for (var s = 0; s < sides; s++)
                    {
                        var sNext = (s + 1) % sides;
                        var a = ringVertexStart[r] + s;
                        var b = ringVertexStart[r] + sNext;
                        var c = ringVertexStart[r + 1] + s;
                        var d = ringVertexStart[r + 1] + sNext;
                        triangles.Add(a);
                        triangles.Add(c);
                        triangles.Add(b);
                        triangles.Add(b);
                        triangles.Add(c);
                        triangles.Add(d);
                    }
                }
            }

            CorrectGlobalWinding(vertices, triangles);

            var mesh = new Mesh { name = $"OrganicRevolution_{seed}" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // A revolved shape's topology is consistent throughout (either
        // every face is backwards or none are), so winding is corrected
        // once globally rather than per-face: sum each face's (normal .
        // radially-outward-from-axis) across the whole mesh and flip all
        // triangles if the vote is negative. Summing avoids picking one
        // possibly-degenerate pole triangle as the sole sample.
        private static void CorrectGlobalWinding(List<Vector3> vertices, List<int> triangles)
        {
            var voteSum = 0f;
            for (var i = 0; i < triangles.Count; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                var normal = Vector3.Cross(b - a, c - a);
                var faceCenter = (a + b + c) / 3f;
                var outward = new Vector3(faceCenter.x, 0f, faceCenter.z);
                voteSum += Vector3.Dot(normal, outward);
            }

            if (voteSum < 0f)
            {
                for (var i = 0; i < triangles.Count; i += 3)
                {
                    var tmp = triangles[i + 1];
                    triangles[i + 1] = triangles[i + 2];
                    triangles[i + 2] = tmp;
                }
            }
        }
    }
}
