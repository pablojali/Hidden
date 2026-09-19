using UnityEngine;

namespace Hidden.World
{
    // Companion to ProceduralBlobMesh: builds a small flat-shaded low-poly
    // cone (a triangle fan from an apex down to a gently jittered base rim,
    // unshared vertices for flat shading) once at startup and assigns it to
    // the sibling MeshFilter. This is the classic low-poly conifer
    // silhouette requested directly against a reference image, used for
    // tree canopies in place of the rounder blob shape.
    [RequireComponent(typeof(MeshFilter))]
    public class ProceduralConeMesh : MonoBehaviour
    {
        [SerializeField] private int seed;
        [SerializeField] private int sides = 8;
        [SerializeField] private float radiusJitter = 0.08f;

        private void Awake()
        {
            GetComponent<MeshFilter>().sharedMesh = Build(seed, sides, radiusJitter);
        }

        public static Mesh Build(int seed, int sides, float radiusJitter)
        {
            sides = Mathf.Max(sides, 3);
            var rng = new System.Random(seed);

            var apex = new Vector3(0f, 1f, 0f);
            var rim = new Vector3[sides];
            for (var i = 0; i < sides; i++)
            {
                var angle = i * Mathf.PI * 2f / sides;
                var radius = 0.5f * (1f + (float)(rng.NextDouble() * 2.0 - 1.0) * radiusJitter);
                rim[i] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }

            // Side faces only -- the base is never seen (a trunk sits under
            // it), so it isn't built, keeping the mesh a bit cheaper.
            var vertexCount = sides * 3;
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var triangles = new int[vertexCount];

            for (var i = 0; i < sides; i++)
            {
                var a = apex;
                var b = rim[i];
                var c = rim[(i + 1) % sides];

                var normal = Vector3.Cross(b - a, c - a).normalized;
                var faceCenter = (a + b + c) / 3f;
                // A cone's lateral surface always points radially outward
                // from its own central (Y) axis, not away from a single
                // center point -- so the outward check here is against the
                // face center's horizontal (XZ) direction only, unlike
                // ProceduralBlobMesh's whole-shape center check. Same
                // reasoning as that script: no Editor available to confirm
                // winding visually, so it's derived from geometry instead.
                var outward = new Vector3(faceCenter.x, 0f, faceCenter.z);
                if (outward.sqrMagnitude > 0.0001f && Vector3.Dot(normal, outward) < 0f)
                {
                    (b, c) = (c, b);
                    normal = -normal;
                }

                var baseIndex = i * 3;
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

            var mesh = new Mesh { name = $"ProceduralCone_{seed}" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
