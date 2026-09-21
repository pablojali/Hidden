using System.Linq;
using Hidden.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hidden.EditorTools
{
    // One-off content-editing tool for the reference-image comparison
    // round after the project's first real Editor open: switches every
    // OrganicRevolutionMesh/OrganicRockMesh instance in Level_01_ForestDiorama
    // to flat shading (matching the reference's clean, low-facet-count look
    // -- the earlier move to smooth shading fixed a different problem,
    // high-frequency per-vertex jitter reading as "broken glass", not
    // faceting itself), and gives the dirt roads their own asphalt-style
    // material plus a dashed centerline, distinct from MountainTerrace and
    // GroundHollow_ (which also use M_Dirt and must stay untouched). Not
    // part of the shipped game.
    public static class ReferenceStyleUpdater
    {
        private const string ScenePath = "Assets/_Project/Scenes/Worlds/Level_01_ForestDiorama.unity";
        private const string RoadMaterialPath = "Assets/_Project/Materials/M_Road.mat";
        private const string RoadLineMaterialPath = "Assets/_Project/Materials/M_RoadLine.mat";

        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var revolutionCount = FlattenRevolutionMeshes();
            var rockCount = FlattenRockMeshes();
            var roadCount = RestyleRoads();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"DIAGNOSTIC_STYLE_UPDATE_DONE revolution={revolutionCount} rock={rockCount} road={roadCount}");
        }

        private static int FlattenRevolutionMeshes()
        {
            var count = 0;
            foreach (var component in Object.FindObjectsByType<OrganicRevolutionMesh>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(component);
                so.FindProperty("flatShaded").boolValue = true;
                // Dial down (not zero) the lean/jitter that made canopies
                // read as "leaning organic blobs" -- the reference's trees
                // are clean, close-to-symmetrical cones/clumps.
                so.FindProperty("asymmetry").floatValue =
                    Mathf.Min(so.FindProperty("asymmetry").floatValue, 0.04f);
                so.FindProperty("radiusJitter").floatValue =
                    Mathf.Min(so.FindProperty("radiusJitter").floatValue, 0.03f);
                so.ApplyModifiedPropertiesWithoutUndo();
                component.Rebuild();
                count++;
            }

            return count;
        }

        private static int FlattenRockMeshes()
        {
            var count = 0;
            foreach (var component in Object.FindObjectsByType<OrganicRockMesh>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(component);
                so.FindProperty("flatShaded").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                component.Rebuild();
                count++;
            }

            return count;
        }

        private static int RestyleRoads()
        {
            var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMaterialPath);
            var lineMat = AssetDatabase.LoadAssetAtPath<Material>(RoadLineMaterialPath);
            if (roadMat == null || lineMat == null)
            {
                Debug.LogError("DIAGNOSTIC_STYLE_UPDATE_MISSING_MATERIAL");
                return 0;
            }

            var segments = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r => r.gameObject.name.StartsWith("RoadToMountain_") ||
                            r.gameObject.name.StartsWith("RoadToDock_"))
                .OrderBy(r => r.gameObject.name)
                .ToArray();

            var index = 0;
            foreach (var renderer in segments)
            {
                renderer.sharedMaterial = roadMat;

                // Dashed centerline: skip every other segment so gaps read
                // as a real dashed road line rather than a solid stripe.
                if (index % 2 == 0)
                {
                    var seg = renderer.transform;
                    var topY = renderer.bounds.max.y;

                    var dash = new GameObject(seg.name + "_Line");
                    dash.transform.SetParent(seg.parent, false);
                    dash.transform.position = new Vector3(seg.position.x, topY + 0.006f, seg.position.z);
                    dash.transform.rotation = seg.rotation;
                    dash.transform.localScale = new Vector3(seg.lossyScale.x * 0.55f, 0.02f, 0.12f);

                    var mf = dash.AddComponent<MeshFilter>();
                    mf.sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    var mr = dash.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = lineMat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                index++;
            }

            return segments.Length;
        }
    }
}
