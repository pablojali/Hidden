using System.Collections.Generic;
using UnityEngine;

namespace Hidden.World
{
    // A small tapered tube that leans slightly to one side, has a
    // jittered (non-circular) cross-section, and a jittered radius per
    // ring -- taper, lean, and irregularity read as "trunk" rather than
    // "cylinder". Built once in Awake() from a seeded PRNG, capped on top
    // so it isn't hollow where a canopy doesn't fully cover it.
    //
    // Reference-image visual correction: vertices are shared between
    // adjacent rings/sides (one vertex per ring/side pair, not duplicated
    // per triangle) and shading comes from Mesh.RecalculateNormals(), so
    // the trunk reads as a smoothly rounded tube instead of a faceted
    // polygon column.
    // ExecuteAlways: without it, Awake() never runs outside Play Mode, so
    // the mesh would only appear once you press Play (or in a build),
    // leaving the Scene view empty while editing/browsing the level.
    [ExecuteAlways]
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
            Rebuild();
        }

        // Unity does not call Awake() for a plain (non-ExecuteAlways)
        // MonoBehaviour outside Play Mode, so EditMode tests can't rely on
        // AddComponent triggering it (same reason CharacterMover exposes
        // Initialize()). Exposed publicly so tests can drive it directly.
        public void Rebuild()
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

            var vertices = new List<Vector3>();
            var vertexIndex = new int[ringCount, sides];
            for (var r = 0; r < ringCount; r++)
            {
                for (var s = 0; s < sides; s++)
                {
                    var angle = (float)s / sides * Mathf.PI * 2f;
                    var radius = ringRadii[r] * sideJitter[s];
                    var local = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    vertexIndex[r, s] = vertices.Count;
                    vertices.Add(ringCenters[r] + local);
                }
            }

            var capCenterIndex = vertices.Count;
            vertices.Add(ringCenters[heightSegments]);

            var sideTriangles = new List<int>();
            for (var r = 0; r < heightSegments; r++)
            {
                for (var s = 0; s < sides; s++)
                {
                    var sNext = (s + 1) % sides;
                    var a = vertexIndex[r, s];
                    var b = vertexIndex[r, sNext];
                    var c = vertexIndex[r + 1, s];
                    var d = vertexIndex[r + 1, sNext];
                    sideTriangles.Add(a);
                    sideTriangles.Add(c);
                    sideTriangles.Add(b);
                    sideTriangles.Add(b);
                    sideTriangles.Add(c);
                    sideTriangles.Add(d);
                }
            }

            var capTriangles = new List<int>();
            for (var s = 0; s < sides; s++)
            {
                var sNext = (s + 1) % sides;
                capTriangles.Add(vertexIndex[heightSegments, s]);
                capTriangles.Add(vertexIndex[heightSegments, sNext]);
                capTriangles.Add(capCenterIndex);
            }

            // Side and cap faces have different "correct outward"
            // directions (radial vs. upward), so each is verified and
            // corrected independently rather than with one combined
            // check -- a single global vote can't guarantee both at once.
            CorrectRadialWinding(vertices, sideTriangles, ringCenters[0], ringCenters[heightSegments]);
            CorrectUpwardWinding(vertices, capTriangles);

            var triangles = new List<int>(sideTriangles.Count + capTriangles.Count);
            triangles.AddRange(sideTriangles);
            triangles.AddRange(capTriangles);

            var mesh = new Mesh { name = $"ProceduralTrunk_{seed}" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // The tube's side topology is consistent throughout (either every
        // face is backwards or none are), so winding is corrected once
        // globally: each face votes on whether its normal points away
        // from the trunk's own central axis at that height (approximated
        // by the segment between the base and top ring centers); a
        // negative vote flips every side triangle.
        private static void CorrectRadialWinding(List<Vector3> vertices, List<int> triangles,
            Vector3 axisBase, Vector3 axisTop)
        {
            var voteSum = 0f;
            for (var i = 0; i < triangles.Count; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                var normal = Vector3.Cross(b - a, c - a);
                var faceCenter = (a + b + c) / 3f;
                var t = axisTop.y > axisBase.y
                    ? Mathf.Clamp01((faceCenter.y - axisBase.y) / (axisTop.y - axisBase.y))
                    : 0f;
                var axisPoint = Vector3.Lerp(axisBase, axisTop, t);
                var outward = faceCenter - axisPoint;
                outward.y = 0f;
                voteSum += Vector3.Dot(normal, outward);
            }

            if (voteSum < 0f)
            {
                FlipAll(triangles);
            }
        }

        // The cap fan's only sensible outward direction is straight up.
        private static void CorrectUpwardWinding(List<Vector3> vertices, List<int> triangles)
        {
            var voteSum = 0f;
            for (var i = 0; i < triangles.Count; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                var normal = Vector3.Cross(b - a, c - a);
                voteSum += normal.y;
            }

            if (voteSum < 0f)
            {
                FlipAll(triangles);
            }
        }

        private static void FlipAll(List<int> triangles)
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
