using System.Collections.Generic;
using System.Linq;
using Hidden.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hidden.EditorTools
{
    // One-off level-layout tool (not shipped) for the "cover the whole
    // plane, mountains along one edge, distinct zones" pass requested
    // after the reference-style correction round. There is no
    // Tools/SceneGeneration/gen_level01.py in this repository (see
    // ARCHITECTURE.md's M0.10.2 note) so this operates directly on the
    // compiled scene, the same way ReferenceStyleUpdater did: it clones
    // existing, already-verified prop hierarchies (ScatterTree_01,
    // ScatterBush_01, ScatterRock_01, the Mountain cluster) rather than
    // re-deriving their mesh-generator wiring from scratch, and reseeds
    // each clone's procedural mesh components for variety.
    public static class WorldExpansionBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Worlds/Level_01_ForestDiorama.unity";

        private const float BoundsMin = -22f;
        private const float BoundsMax = 22f;
        // Mountain range hugs this edge (+X), matching the existing
        // Mountain's side. Kept out of the general scatter pass.
        private const float MountainEdgeXMin = 13f;

        private static System.Random _rng;
        private static readonly List<Footprint> Footprints = new();

        private struct Footprint
        {
            public float X;
            public float Z;
            public float Radius;
        }

        public static void Apply()
        {
            _rng = new System.Random(20260921);
            Footprints.Clear();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RemovePreviousRun();
            RegisterFixedFootprints();
            RegisterExistingPropFootprints();

            var mountainCount = ExtendMountainRange();
            var campCount = BuildCampingZone();
            var forestCount = DensifyForest();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"DIAGNOSTIC_WORLD_EXPANSION_DONE mountain={mountainCount} camp={campCount} forest={forestCount} " +
                      $"footprints={Footprints.Count}");
        }

        // Makes the tool safely re-runnable: deletes everything a
        // previous Apply() call added (by name prefix) before generating
        // again, so tuning parameters and re-running never duplicates or
        // compounds content.
        private static void RemovePreviousRun()
        {
            var toRemove = new List<GameObject>();
            var propsGroup = GameObject.Find("Props");
            var environmentGroup = GameObject.Find("Environment");

            foreach (var group in new[] { propsGroup, environmentGroup })
            {
                if (group == null)
                {
                    continue;
                }

                foreach (Transform child in group.transform)
                {
                    if (child.name.StartsWith("Densify_") ||
                        child.name.StartsWith("MountainRange_") ||
                        child.name.StartsWith("MountainTerrace_") ||
                        child.name == "CampingZone")
                    {
                        toRemove.Add(child.gameObject);
                    }
                }
            }

            foreach (var go in toRemove)
            {
                Object.DestroyImmediate(go);
            }
        }

        private static float NextFloat(float min, float max) => (float)(min + _rng.NextDouble() * (max - min));

        private static bool TryFindOpenSpot(float radius, float minX, float maxX, float minZ, float maxZ,
            int attempts, out Vector3 spot)
        {
            for (var i = 0; i < attempts; i++)
            {
                var x = NextFloat(minX, maxX);
                var z = NextFloat(minZ, maxZ);
                if (IsClear(x, z, radius))
                {
                    spot = new Vector3(x, 0f, z);
                    return true;
                }
            }

            spot = Vector3.zero;
            return false;
        }

        private static bool IsClear(float x, float z, float radius)
        {
            foreach (var fp in Footprints)
            {
                var dx = x - fp.X;
                var dz = z - fp.Z;
                var minDist = radius + fp.Radius;
                if (dx * dx + dz * dz < minDist * minDist)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Register(float x, float z, float radius)
        {
            Footprints.Add(new Footprint { X = x, Z = z, Radius = radius });
        }

        // Targets and the house keep generous clearings; discovery must
        // stay exactly as tuned (distance/viewport only) regardless of how
        // dense the surrounding forest gets.
        private static void RegisterFixedFootprints()
        {
            foreach (var name in new[] { "Target_01", "Target_02", "Target_03", "Target_04", "Target_05", "Target_06" })
            {
                var go = GameObject.Find(name);
                if (go != null)
                {
                    Register(go.transform.position.x, go.transform.position.z, 3f);
                }
            }

            var house = GameObject.Find("House");
            if (house != null)
            {
                Register(house.transform.position.x, house.transform.position.z, 7f);
            }
        }

        // Every existing prop already in Props/Environment gets a
        // footprint from its own renderer bounds, so the new scatter pass
        // never overlaps anything already placed (trees, rocks, the
        // river/roads, terrain knolls, the dock/boat, etc).
        private static void RegisterExistingPropFootprints()
        {
            var groups = new[] { "Props", "Environment", "Paths" }
                .Select(GameObject.Find)
                .Where(g => g != null);

            foreach (var group in groups)
            {
                foreach (Transform child in group.transform)
                {
                    var renderers = child.GetComponentsInChildren<Renderer>();
                    if (renderers.Length == 0)
                    {
                        continue;
                    }

                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers)
                    {
                        bounds.Encapsulate(r.bounds);
                    }

                    var radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.3f;
                    // The ground/terrain slabs themselves would otherwise
                    // register a footprint covering the whole map.
                    if (radius > 15f)
                    {
                        continue;
                    }

                    Register(child.position.x, child.position.z, radius);
                }
            }
        }

        // Clones `templateName`, reseeds every procedural mesh component
        // found on it (fresh geometry, not just a repositioned duplicate
        // of the same shape), and parents it under `parent`.
        private static GameObject CloneReseeded(string templateName, Transform parent, Vector3 position,
            float rotationY, float uniformScale)
        {
            var template = GameObject.Find(templateName);
            if (template == null)
            {
                Debug.LogError($"DIAGNOSTIC_WORLD_EXPANSION_MISSING_TEMPLATE {templateName}");
                return null;
            }

            var clone = Object.Instantiate(template, parent);
            clone.name = templateName;
            clone.transform.position = position;
            clone.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            clone.transform.localScale = Vector3.one * uniformScale;

            foreach (var mesh in clone.GetComponentsInChildren<OrganicRevolutionMesh>())
            {
                Reseed(mesh);
            }

            foreach (var mesh in clone.GetComponentsInChildren<OrganicRockMesh>())
            {
                Reseed(mesh);
            }

            foreach (var mesh in clone.GetComponentsInChildren<ProceduralTrunkMesh>())
            {
                Reseed(mesh);
            }

            return clone;
        }

        private static void Reseed(Component component)
        {
            var so = new SerializedObject(component);
            var seedProp = so.FindProperty("seed");
            if (seedProp != null)
            {
                seedProp.intValue = _rng.Next(1, 1_000_000);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            switch (component)
            {
                case OrganicRevolutionMesh rev:
                    rev.Rebuild();
                    break;
                case OrganicRockMesh rock:
                    rock.Rebuild();
                    break;
                case ProceduralTrunkMesh trunk:
                    trunk.Rebuild();
                    break;
            }
        }

        // Duplicates the existing Mountain (Peak + 4 BaseRocks, already
        // hand-tuned) at several points along the whole east edge instead
        // of leaving one cluster near a single corner, plus a matching
        // MountainTerrace step under each. The original Mountain (and its
        // Waterfall/river attachment) is left exactly where it was.
        private static int ExtendMountainRange()
        {
            var count = 0;
            var propsGroup = GameObject.Find("Props");
            var environmentGroup = GameObject.Find("Environment");
            var parent = propsGroup != null ? propsGroup.transform : null;
            var terraceParent = environmentGroup != null ? environmentGroup.transform : parent;

            var originalMountain = GameObject.Find("Mountain");
            var originalTerrace = GameObject.Find("MountainTerrace");
            if (originalMountain == null)
            {
                return 0;
            }

            // The existing Mountain sits at z=6; extend north and south
            // along the same edge via rejection sampling (like the forest
            // scatter below) so clusters actually spread the length of the
            // edge instead of clumping together or silently failing.
            const int desiredClusters = 7;
            const float minSeparation = 2.5f;
            for (var attempt = 0; attempt < desiredClusters * 150 && count < desiredClusters; attempt++)
            {
                var x = NextFloat(11f, 21f);
                var z = NextFloat(BoundsMin, BoundsMax);
                if (!IsClear(x, z, minSeparation))
                {
                    continue;
                }

                var clone = Object.Instantiate(originalMountain, parent);
                clone.name = "MountainRange_" + count;
                clone.transform.position = new Vector3(x, 0f, z);
                var scale = NextFloat(0.7f, 1.15f);
                clone.transform.localScale = Vector3.one * scale;

                foreach (var mesh in clone.GetComponentsInChildren<OrganicRockMesh>())
                {
                    Reseed(mesh);
                }

                if (originalTerrace != null)
                {
                    var terraceClone = Object.Instantiate(originalTerrace, terraceParent);
                    terraceClone.name = "MountainTerrace_" + count;
                    var terraceOffset = originalTerrace.transform.position - originalMountain.transform.position;
                    terraceClone.transform.position = new Vector3(x, 0f, z) + terraceOffset * scale;
                    terraceClone.transform.localScale = originalTerrace.transform.localScale;
                }

                Register(x, z, 6f * scale);
                count++;
            }

            return count;
        }

        // A small forest clearing on the opposite (west) side from the
        // mountain range: two simple flat-shaded tent wedges, a campfire
        // (a tight ring of small rocks plus crossed trunk "logs"), and a
        // log to sit on -- a second distinct, human-made zone alongside
        // the house, per the "zonas como de casa o de camping" request.
        private static int BuildCampingZone()
        {
            const float clearingRadius = 5f;

            // Search the west side (away from the mountain range's east
            // edge) for a spot clear of every already-registered footprint
            // -- existing trees/rocks, the house, targets -- rather than a
            // fixed coordinate that might land inside an existing cluster.
            if (!TryFindOpenSpot(clearingRadius, BoundsMin, -2f, BoundsMin, BoundsMax, 200, out var center))
            {
                Debug.LogWarning("DIAGNOSTIC_WORLD_EXPANSION_NO_CAMP_SPOT");
                return 0;
            }

            var centerX = center.x;
            var centerZ = center.z;

            var propsGroup = GameObject.Find("Props");
            var parent = propsGroup != null ? propsGroup.transform : null;
            var zone = new GameObject("CampingZone");
            zone.transform.SetParent(parent, false);
            zone.transform.position = new Vector3(centerX, 0f, centerZ);

            var tentMesh = BuildTentMesh();
            var tentMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Tent.mat");

            CreateTent(zone.transform, tentMesh, tentMaterial, new Vector3(-2.2f, 0f, 1.3f), 20f);
            CreateTent(zone.transform, tentMesh, tentMaterial, new Vector3(2.6f, 0f, -0.8f), -35f);

            BuildCampfire(zone.transform, Vector3.zero);

            var rockTemplate = GameObject.Find("Rock_01");
            var trunkTemplate = GameObject.Find("ScatterTree_01");
            if (trunkTemplate != null)
            {
                var logSeat = Object.Instantiate(
                    trunkTemplate.transform.Find("Trunk").gameObject, zone.transform);
                logSeat.name = "LogSeat";
                logSeat.transform.position = new Vector3(centerX - 0.5f, 0.15f, centerZ + 2.8f);
                logSeat.transform.rotation = Quaternion.Euler(90f, 30f, 0f);
                logSeat.transform.localScale = Vector3.one * 1.6f;
                foreach (var mesh in logSeat.GetComponentsInChildren<ProceduralTrunkMesh>())
                {
                    Reseed(mesh);
                }
            }

            Register(centerX, centerZ, clearingRadius);
            return 1;
        }

        private static void CreateTent(Transform parent, Mesh mesh, Material material, Vector3 localPos, float rotY)
        {
            var go = new GameObject("Tent");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            go.transform.localScale = new Vector3(1.4f, 1f, 2.2f);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
        }

        // A simple triangular-prism "pup tent" wedge: two triangular end
        // caps plus three rectangular faces (floor omitted, never seen).
        // Built once and shared by every tent instance.
        private static Mesh BuildTentMesh()
        {
            var halfW = 0.5f;
            var halfL = 0.5f;
            var height = 0.6f;

            Vector3 FrontLeft = new(-halfW, 0f, -halfL);
            Vector3 FrontRight = new(halfW, 0f, -halfL);
            Vector3 FrontPeak = new(0f, height, -halfL);
            Vector3 BackLeft = new(-halfW, 0f, halfL);
            Vector3 BackRight = new(halfW, 0f, halfL);
            Vector3 BackPeak = new(0f, height, halfL);

            var vertices = new List<Vector3>
            {
                // Left slope
                FrontLeft, BackLeft, BackPeak, FrontLeft, BackPeak, FrontPeak,
                // Right slope
                FrontRight, FrontPeak, BackPeak, FrontRight, BackPeak, BackRight,
                // Front triangle
                FrontLeft, FrontPeak, FrontRight,
                // Back triangle
                BackLeft, BackRight, BackPeak,
            };

            var triangles = new int[vertices.Count];
            for (var i = 0; i < triangles.Length; i++)
            {
                triangles[i] = i;
            }

            var mesh = new Mesh { name = "TentWedge" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Self-correct winding the same way the procedural generators
            // do: no Editor was available to check it visually up front,
            // so verify the front-triangle face normal points -Z (outward)
            // and flip every face if it doesn't.
            var normals = mesh.normals;
            if (normals.Length > 0 && Vector3.Dot(normals[12], Vector3.back) < 0f)
            {
                for (var i = 0; i < triangles.Length; i += 3)
                {
                    (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
                }

                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
            }

            return mesh;
        }

        private static void BuildCampfire(Transform parent, Vector3 localCenter)
        {
            var rockTemplate = GameObject.Find("Rock_01");
            var trunkTemplate = GameObject.Find("ScatterTree_01")?.transform.Find("Trunk").gameObject;

            var fire = new GameObject("Campfire");
            fire.transform.SetParent(parent, false);
            fire.transform.localPosition = localCenter;

            if (rockTemplate != null)
            {
                const int ringCount = 6;
                for (var i = 0; i < ringCount; i++)
                {
                    var angle = i / (float)ringCount * Mathf.PI * 2f;
                    var pos = new Vector3(Mathf.Cos(angle) * 0.7f, 0f, Mathf.Sin(angle) * 0.7f);
                    var rock = Object.Instantiate(rockTemplate, fire.transform);
                    rock.name = "FireRing_" + i;
                    rock.transform.localPosition = pos;
                    rock.transform.localScale = Vector3.one * 0.22f;
                    foreach (var mesh in rock.GetComponentsInChildren<OrganicRockMesh>())
                    {
                        Reseed(mesh);
                    }
                }
            }

            if (trunkTemplate != null)
            {
                for (var i = 0; i < 2; i++)
                {
                    var log = Object.Instantiate(trunkTemplate, fire.transform);
                    log.name = "FireLog_" + i;
                    log.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                    log.transform.localRotation = Quaternion.Euler(90f, i * 70f, 0f);
                    log.transform.localScale = Vector3.one * 0.55f;
                    foreach (var mesh in log.GetComponentsInChildren<ProceduralTrunkMesh>())
                    {
                        Reseed(mesh);
                    }
                }
            }
        }

        // Fills the rest of the ~44x44 playable footprint with trees,
        // bushes, and rocks via rejection sampling against every footprint
        // registered so far (targets, house, existing props, the new
        // mountain range, the camping clearing) -- the same technique
        // Docs/ARCHITECTURE.md describes for the original scatter pass,
        // run again at a much higher target count so the ground reads as
        // fully forested rather than having open gaps.
        private static int DensifyForest()
        {
            var propsGroup = GameObject.Find("Props");
            var parent = propsGroup != null ? propsGroup.transform : null;
            var placed = 0;

            placed += ScatterKind("ScatterTree_01", parent, 320, 1.05f, 0.85f, 1.25f, 40);
            placed += ScatterKind("ScatterBush_01", parent, 180, 0.65f, 0.8f, 1.3f, 40);
            placed += ScatterKind("ScatterRock_01", parent, 140, 0.55f, 0.7f, 1.4f, 40);
            // Small ground-detail filler pass: much smaller footprint, so
            // it settles into whatever gaps remain between the trees/
            // bushes/rocks above instead of competing with them for space.
            placed += ScatterKind("ScatterPebble_01", parent, 150, 0.3f, 0.8f, 1.3f, 30);
            placed += ScatterKind("ScatterGrass_01", parent, 150, 0.3f, 0.8f, 1.3f, 30);
            placed += ScatterKind("ScatterFlowers_01", parent, 60, 0.35f, 0.8f, 1.2f, 30);

            return placed;
        }

        private static int ScatterKind(string templateName, Transform parent, int targetCount,
            float footprintRadius, float minScale, float maxScale, int attemptsPerItem)
        {
            var placed = 0;

            for (var i = 0; i < targetCount; i++)
            {
                var minX = BoundsMin;
                var maxX = MountainEdgeXMin; // keep the general scatter off the mountain edge strip
                if (!TryFindOpenSpot(footprintRadius, minX, maxX, BoundsMin, BoundsMax, attemptsPerItem, out var spot))
                {
                    continue;
                }

                var scale = NextFloat(minScale, maxScale);
                var rotY = NextFloat(0f, 360f);
                var clone = CloneReseeded(templateName, parent, spot, rotY, scale);
                if (clone == null)
                {
                    return placed;
                }

                clone.name = "Densify_" + templateName + "_" + i;
                Register(spot.x, spot.z, footprintRadius * scale);
                placed++;
            }

            return placed;
        }
    }
}
