"""Generates Level_01_ForestDiorama.unity from scratch, reusing verified
mesh/material/script GUIDs extracted from M02_DioramaPrototype.unity.
"""
import math
import random

OUT_PATH = "/home/user/Hidden/Assets/_Project/Scenes/Worlds/Level_01_ForestDiorama.unity"

E_GUID = "0000000000000000e000000000000000"
F_GUID = "0000000000000000f000000000000000"
MESH_CUBE = 10202
MESH_CYLINDER = 10206
MESH_SPHERE = 10207
MESH_CAPSULE = 10208
FONT_ARIAL = 10102

# --- verified script guids (from M02 / M0.7 work) -------------------------
SCRIPT_GAME_BOOTSTRAP = "fe9f55189695420ab01ba23034bacc3d"
SCRIPT_CAMERA_INPUT = "c1e343e84d014ed09efe2782d684f780"
SCRIPT_CAMERA_CONTROLLER = "756bbdcafaa74a25abf020c4bd9f8706"
SCRIPT_URP_CAMERA_DATA = "a79441f348de89743a2939f4d699eac1"
SCRIPT_DISCOVERY_SYSTEM = "69d7a4a1d9b840d3882db59823413660"
SCRIPT_CHARACTER_MOVER = "87c9b821c01942f2b9339e3e97e069f9"
SCRIPT_CHARACTER_VISUAL = "37a1a0796c484de199ca626fdc302ab8"
SCRIPT_DISCOVERABLE = "49cb50de3a0241c8863593e82b98b653"
SCRIPT_CHARACTER_PATH = "2ead7526219646a2bca27cfbf710db46"
SCRIPT_PULSE_FEEDBACK = "ebda8ccf83034076bf7085e691dd8f34"
SCRIPT_DISCOVERY_MANAGER = "c449e021d68944c8ae167b8ac2209387"
SCRIPT_COMPLETION_FEEDBACK = "0da7f031f52d4b7caad09e9df28fb751"
SCRIPT_DISCOVERY_UI = "bded1f83763f4ac483d11beb880d0436"
SCRIPT_FIREWORK = "f38b8aedae1741abaae047fdc20799f2"
SCRIPT_COMPLETION_CELEBRATION = "44ddd443113c45fb944bc499523150d6"
SCRIPT_CANVAS_SCALER = "0cd44c1031e13a943bb63640046fad76"
SCRIPT_UI_TEXT = "5f7201a12d95ffc409449d95f23cf332"
SCRIPT_UI_SHADOW = "cfabb0440166ab443bba8876756fdfa9"
SCRIPT_LEVEL_DEFINITION = "e75e95b5b1be433abaf5f57ff649c80e"
SCRIPT_LEVEL_INFO = "17c591b3da674f9fb6de67f566d4f094"
LEVEL_ASSET_GUID = "6f5651b92cfc40208c81cf00fb44c09a"
SCRIPT_BLOB = "139a93803d8f4ecaadefafbbc5581ca2"
SCRIPT_CONE = "808e3495d4824ea68dabfd80e76b9dac"
SCRIPT_CLUSTER = "a62c9f1303f34d8c8381144342d57081"  # superseded, unused below
SCRIPT_TRUNK = "397c233293c94eddaa68360b7dbef97b"
SCRIPT_REVOLUTION = "0be220c53e554fafbb4c716c61216199"
SCRIPT_ORGANIC_ROCK = "ed9ef5ec8ada431ba34a8de4ba28a3c7"

# --- verified material guids (from M02, plus M0.9-added M_Ground/M_Moss) ---
MAT_GROUND = "198ce29a326b498ba7b12a79d2a5f725"  # M_Terrain -- hill only now
MAT_GROUND_MAIN = "e156a06b9ddc49438d178136d179f9fb"  # M_Ground -- main ground
MAT_MOSS = "20a4f329a8864cf9ad8633b822f08903"  # M_Moss -- recessed hollow
MAT_TERRAIN_SIDE = "9b5255ca240a42809e32726461f5870a"  # M_TerrainSide -- chunk sides
MAT_WALLS = "a76b26242729431195eb31b640069313"
MAT_ROOF = "c7c2babef51149da942fd2cca61d2ea0"
MAT_TRUNK = "22b92240f2b244529d3ef1ca9f405607"
MAT_FOLIAGE_A = "5852ed4b7688430d8367676f7dad7326"
MAT_FOLIAGE_B = "4415fb1428794a8ebd9be25fd62b1dc3"
MAT_ROCK = "33ed6b333be64f5c8aef9122cd25ea63"
MAT_PATH = "80cfe283619f44c48b70e8b87192d21b"
MAT_CHAR_BODY_A = "1fe4cbcbd6734e84a25ded924b4d4bca"
MAT_CHAR_BODY_B = "803b546ca894493fb1c89dd3af7c2411"
MAT_CHAR_HEAD = "d61828cc441a4c1f9187a65d8ab4654c"
MAT_GEM = "15ff5e47c47f4adaad9159e815df8458"
MAT_WATER = "75e5e42e99de44ae983a8be428dd085f"  # M_Water -- river/waterfall
MAT_DIRT = "028d0fb539ca4c75852a316d359687ba"  # M_Dirt -- dirt roads
MAT_FLOWER = "1edebf37afea4ff9849432ca0d174105"  # M_Flower -- small accent blooms
MAT_MUSHROOM = "1f1106bf61244dc1b198df6c9d8f8e88"  # M_Mushroom -- small accent caps
MAT_SKYBOX_FID = 10304
MAT_SPOTCOOKIE_FID = 10001

_next_id = [1000000]


def alloc():
    _next_id[0] += 1
    return _next_id[0]


def alloc_n(n):
    return [alloc() for _ in range(n)]


def f3(v):
    return f"{{x: {v[0]}, y: {v[1]}, z: {v[2]}}}"


def f4(v):
    return f"{{x: {v[0]}, y: {v[1]}, z: {v[2]}, w: {v[3]}}}"


IDENTITY_ROT = (0, 0, 0, 1)


def go_block(fid, name, comp_fids, layer=0, tag="Untagged"):
    comp_lines = "\n".join(f"  - component: {{fileID: {c}}}" for c in comp_fids)
    return f"""--- !u!1 &{fid}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comp_lines}
  m_Layer: {layer}
  m_Name: {name}
  m_TagString: {tag}
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
"""


def transform_block(fid, go_fid, pos, rot=IDENTITY_ROT, scale=(1, 1, 1), father=0, children=None):
    children = children or []
    children_lines = " []\n" if not children else "\n" + "\n".join(f"  - {{fileID: {c}}}" for c in children) + "\n"
    return f"""--- !u!4 &{fid}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  serializedVersion: 2
  m_LocalRotation: {f4(rot)}
  m_LocalPosition: {f3(pos)}
  m_LocalScale: {f3(scale)}
  m_ConstrainProportionsScale: 0
  m_Children:{children_lines}  m_Father: {{fileID: {father}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
"""


def meshfilter_block(fid, go_fid, mesh_fid):
    return f"""--- !u!33 &{fid}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  m_Mesh: {{fileID: {mesh_fid}, guid: {E_GUID}, type: 0}}
"""


def meshrenderer_block(fid, go_fid, material_guid, cast_shadows=1, receive_shadows=1, light_probe=1, reflection_probe=1):
    return f"""--- !u!23 &{fid}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  m_Enabled: 1
  m_CastShadows: {cast_shadows}
  m_ReceiveShadows: {receive_shadows}
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: {light_probe}
  m_ReflectionProbeUsage: {reflection_probe}
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 2100000, guid: {material_guid}, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
"""


def mono_block(fid, go_fid, script_guid, fields, editor_class_identifier=""):
    field_lines = "\n".join(f"  {line}" for line in fields.splitlines()) if fields else ""
    return f"""--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: {editor_class_identifier}
{field_lines}
"""


parts = []
group_children = {
    "Environment": [],
    "Structures": [],
    "Props": [],
    "Paths": [],
    "Discoverables": [],
    "GameSystems": [],
    "UI": [],
}

GROUP_NAMES = ["Environment", "Structures", "Props", "Paths", "Discoverables", "GameSystems", "UI"]

# Pre-allocate World + every group's GameObject/Transform fileIDs up front,
# so every entity below can set its own m_Father correctly the first time
# it's written, instead of being patched in later.
world_go, world_t = alloc_n(2)
group_fids = {}
for _name in GROUP_NAMES:
    g_go, g_t = alloc_n(2)
    group_fids[_name] = (g_go, g_t)

# CompletionCelebration's own ids are also pre-allocated: its rig is a
# child of Main Camera, and Main Camera's transform_block (written before
# the rig's own content further below) needs cel_t in its m_Children list.
cel_go, cel_t, cel_comp = alloc_n(3)

# Footprints of every hand-placed tree/bush/rock/landmark (x, z, radius),
# recorded as each is emitted below -- the later dense environmental
# scatter pass uses this to keep new density from stacking directly on
# top of what's already here, while still allowing it to sit naturally
# close/adjacent (real undergrowth crowds around trees and rocks).
PLACED_FOOTPRINTS = []


def emit_simple_prop(name, mesh_fid, pos, scale, material_guid, rot=IDENTITY_ROT, group="Props",
                      cast_shadows=1, receive_shadows=1, light_probe=1, reflection_probe=1):
    go, t, mf, mr = alloc_n(4)
    parts.append(go_block(go, name, [t, mf, mr]))
    parts.append(transform_block(t, go, pos, rot, scale, father=group_fids[group][1]))
    parts.append(meshfilter_block(mf, go, mesh_fid))
    parts.append(meshrenderer_block(mr, go, material_guid, cast_shadows, receive_shadows, light_probe, reflection_probe))
    group_children[group].append(t)
    return go, t


_blob_seed = [1000]


def next_blob_seed():
    _blob_seed[0] += 1
    return _blob_seed[0]


def emit_blob_prop(name, pos, radius, material_guid, jitter, vertical_squash, rot=IDENTITY_ROT, group="Props",
                    cast_shadows=1, receive_shadows=1, light_probe=1, reflection_probe=1, seed=None, father=None):
    # Same shape (MeshFilter starting with no mesh, ProceduralBlobMesh
    # fills it in at Awake()) used for every organic prop -- rocks, tree
    # canopies, bushes, grass tufts -- just different jitter/squash/scale.
    go, t, mf, mr, blob = alloc_n(5)
    father_t = father if father is not None else group_fids[group][1]
    parts.append(go_block(go, name, [t, mf, mr, blob]))
    parts.append(transform_block(t, go, pos, rot, (radius, radius, radius), father=father_t))
    parts.append(f"""--- !u!33 &{mf}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Mesh: {{fileID: 0}}
""")
    parts.append(meshrenderer_block(mr, go, material_guid, cast_shadows, receive_shadows, light_probe, reflection_probe))
    parts.append(mono_block(blob, go, SCRIPT_BLOB, f"""seed: {seed if seed is not None else next_blob_seed()}
jitter: {jitter}
verticalSquash: {vertical_squash}"""))
    if father is None:
        group_children[group].append(t)
    return go, t


def emit_cone_prop(name, pos, radius, height, material_guid, sides=8, radius_jitter=0.08,
                    rot=IDENTITY_ROT, group="Props", seed=None, father=None):
    # Same pattern as emit_blob_prop (empty MeshFilter, ProceduralConeMesh
    # fills it in at Awake()) but for the low-poly pine-tree silhouette --
    # a cone rather than a rounder blob. radius/height scale the unit cone
    # (base radius 0.5, height 1) independently via transform scale.
    go, t, mf, mr, cone = alloc_n(5)
    father_t = father if father is not None else group_fids[group][1]
    # Scale convention matches emit_blob_prop: the unit shape already has
    # base radius 0.5, so "radius" here is the scale factor (effective
    # world radius = radius * 0.5), not a literal world-unit radius.
    parts.append(go_block(go, name, [t, mf, mr, cone]))
    parts.append(transform_block(t, go, pos, rot, (radius, height, radius), father=father_t))
    parts.append(f"""--- !u!33 &{mf}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Mesh: {{fileID: 0}}
""")
    parts.append(meshrenderer_block(mr, go, material_guid))
    parts.append(mono_block(cone, go, SCRIPT_CONE, f"""seed: {seed if seed is not None else next_blob_seed()}
sides: {sides}
radiusJitter: {radius_jitter}"""))
    if father is None:
        group_children[group].append(t)
    return go, t


def emit_cluster_prop(name, pos, lobe_count, base_radius, radius_variance, jitter, vertical_squash,
                       spread_radius, vertical_spread, material_guid, rot=IDENTITY_ROT, group="Props",
                       seed=None, father=None):
    # Reference-image visual correction pass: replaces a single primitive
    # (one blob/cone/cube) with ProceduralClusterMesh -- several jittered
    # icosahedron lobes merged into ONE mesh at varied offsets/sizes, so
    # the result reads as an irregular compound mass (a canopy, a bush, a
    # rock formation) instead of one smooth/faceted shape. All sizing is
    # baked directly into the mesh's own vertex data (matching
    # emit_blob_prop's "radius = final world size" convention), so the
    # transform itself stays at identity scale unless the caller wants an
    # extra uniform multiplier.
    go, t, mf, mr, cluster = alloc_n(5)
    father_t = father if father is not None else group_fids[group][1]
    parts.append(go_block(go, name, [t, mf, mr, cluster]))
    parts.append(transform_block(t, go, pos, rot, (1, 1, 1), father=father_t))
    parts.append(f"""--- !u!33 &{mf}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Mesh: {{fileID: 0}}
""")
    parts.append(meshrenderer_block(mr, go, material_guid))
    parts.append(mono_block(cluster, go, SCRIPT_CLUSTER, f"""seed: {seed if seed is not None else next_blob_seed()}
lobeCount: {lobe_count}
baseRadius: {base_radius}
radiusVariance: {radius_variance}
jitter: {jitter}
verticalSquash: {vertical_squash}
spreadRadius: {spread_radius}
verticalSpread: {vertical_spread}"""))
    if father is None:
        group_children[group].append(t)
    return go, t


def emit_trunk_prop(name, pos, sides, height_segments, base_radius, top_radius, height, radius_jitter,
                     lean_amount, material_guid, rot=IDENTITY_ROT, group="Props", seed=None, father=None):
    # ProceduralTrunkMesh: a small tapered, leaning, jittered-cross-section
    # tube replacing a plain MESH_CYLINDER trunk -- taper + lean +
    # irregularity read as "trunk" instead of "cylinder" without needing
    # bark detail or a hand-authored mesh asset.
    go, t, mf, mr, trunk = alloc_n(5)
    father_t = father if father is not None else group_fids[group][1]
    parts.append(go_block(go, name, [t, mf, mr, trunk]))
    parts.append(transform_block(t, go, pos, rot, (1, 1, 1), father=father_t))
    parts.append(f"""--- !u!33 &{mf}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Mesh: {{fileID: 0}}
""")
    parts.append(meshrenderer_block(mr, go, material_guid))
    parts.append(mono_block(trunk, go, SCRIPT_TRUNK, f"""seed: {seed if seed is not None else next_blob_seed()}
sides: {sides}
heightSegments: {height_segments}
baseRadius: {base_radius}
topRadius: {top_radius}
height: {height}
radiusJitter: {radius_jitter}
leanAmount: {lean_amount}"""))
    if father is None:
        group_children[group].append(t)
    return go, t


def emit_revolution_prop(name, pos, sides, profile_heights, profile_radii, material_guid,
                          asymmetry=0.12, radius_jitter=0.05, rot=IDENTITY_ROT, group="Props",
                          seed=None, father=None):
    # Reference-image visual correction pass 2: OrganicRevolutionMesh --
    # ONE smooth, shared-vertex mesh (shading via RecalculateNormals(),
    # not per-face flat normals) revolved from a hand-authored profile
    # curve. Replaces cones/merged-lobe clusters as the final shape for
    # tree canopies, bush clumps, mushroom caps, and flower blooms --
    # different profile presets, same component.
    go, t, mf, mr, comp = alloc_n(5)
    father_t = father if father is not None else group_fids[group][1]
    parts.append(go_block(go, name, [t, mf, mr, comp]))
    parts.append(transform_block(t, go, pos, rot, (1, 1, 1), father=father_t))
    parts.append(f"""--- !u!33 &{mf}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Mesh: {{fileID: 0}}
""")
    parts.append(meshrenderer_block(mr, go, material_guid))
    heights_field = "\n".join(f"- {h}" for h in profile_heights)
    radii_field = "\n".join(f"- {r}" for r in profile_radii)
    parts.append(mono_block(comp, go, SCRIPT_REVOLUTION, f"""seed: {seed if seed is not None else next_blob_seed()}
sides: {sides}
profileHeights:
{heights_field}
profileRadii:
{radii_field}
asymmetry: {asymmetry}
radiusJitter: {radius_jitter}"""))
    if father is None:
        group_children[group].append(t)
    return go, t


def emit_rock_prop(name, pos, bump_count, bump_strength, bump_sharpness, material_guid,
                    scale=(1, 1, 1), rot=IDENTITY_ROT, group="Props", seed=None, father=None):
    # Reference-image visual correction pass 2: OrganicRockMesh -- a
    # smooth, shared-vertex icosphere displaced by a handful of large,
    # low-frequency bumps (never per-vertex noise), shaded via
    # RecalculateNormals(). Replaces merged-icosahedron-lobe rocks as the
    # final shape for every boulder-scale rock and pebble.
    go, t, mf, mr, comp = alloc_n(5)
    father_t = father if father is not None else group_fids[group][1]
    parts.append(go_block(go, name, [t, mf, mr, comp]))
    parts.append(transform_block(t, go, pos, rot, scale, father=father_t))
    parts.append(f"""--- !u!33 &{mf}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Mesh: {{fileID: 0}}
""")
    parts.append(meshrenderer_block(mr, go, material_guid))
    parts.append(mono_block(comp, go, SCRIPT_ORGANIC_ROCK, f"""seed: {seed if seed is not None else next_blob_seed()}
bumpCount: {bump_count}
bumpStrength: {bump_strength}
bumpSharpness: {bump_sharpness}"""))
    if father is None:
        group_children[group].append(t)
    return go, t


def emit_ribbon_segment(name, p1, p2, width, height, material_guid, group="Environment",
                         cast_shadows=0, receive_shadows=1):
    # One straight flattened box between two XZ points -- the building
    # block for both the river (chained with emit_water_path) and the
    # dirt roads (chained with emit_dirt_road): a simple, cheap polyline,
    # not a spline or a custom mesh.
    x1, z1 = p1
    x2, z2 = p2
    dx, dz = x2 - x1, z2 - z1
    length = math.sqrt(dx * dx + dz * dz)
    mid = ((x1 + x2) / 2, height / 2, (z1 + z2) / 2)
    angle = math.atan2(dx, dz)
    rot = (0, round(math.sin(angle / 2), 7), 0, round(math.cos(angle / 2), 7))
    return emit_simple_prop(name, MESH_CUBE, mid, (width, height, length), material_guid, rot=rot,
                             group=group, cast_shadows=cast_shadows, receive_shadows=receive_shadows,
                             light_probe=0, reflection_probe=0)


def _catmull_rom_point(p0, p1, p2, p3, t):
    t2 = t * t
    t3 = t2 * t
    x = 0.5 * ((2 * p1[0]) + (-p0[0] + p2[0]) * t + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * t2
               + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * t3)
    z = 0.5 * ((2 * p1[1]) + (-p0[1] + p2[1]) * t + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * t2
               + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * t3)
    return (x, z)


def _resample_smooth(waypoints, segments_per_gap=4):
    # Photo comparison against the reference found the river/road reading
    # as "rectangular pieces placed on the floor" -- straight segments
    # meeting at sharp mitered corners. Catmull-Rom-interpolating each
    # original waypoint gap into several shorter segments approximates a
    # continuous curve instead, without needing a real spline mesh.
    padded = [waypoints[0]] + list(waypoints) + [waypoints[-1]]
    result = []
    for i in range(1, len(padded) - 2):
        p0, p1, p2, p3 = padded[i - 1], padded[i], padded[i + 1], padded[i + 2]
        for s in range(segments_per_gap):
            result.append(_catmull_rom_point(p0, p1, p2, p3, s / segments_per_gap))
    result.append(waypoints[-1])
    return result


def emit_water_path(name_prefix, waypoints, width=1.7, height=0.12):
    smooth_pts = _resample_smooth(waypoints)
    for i in range(len(smooth_pts) - 1):
        emit_ribbon_segment(f"{name_prefix}_{i + 1:02d}", smooth_pts[i], smooth_pts[i + 1],
                             width, height, MAT_WATER)


def emit_dirt_road(name_prefix, waypoints, width=1.5, height=0.06):
    smooth_pts = _resample_smooth(waypoints)
    for i in range(len(smooth_pts) - 1):
        emit_ribbon_segment(f"{name_prefix}_{i + 1:02d}", smooth_pts[i], smooth_pts[i + 1],
                             width, height, MAT_DIRT, group="Props")


# Reference-image visual correction pass 2: three hand-authored canopy
# profile curves (sides, profile heights, profile radii), revolved by
# OrganicRevolutionMesh into ONE smooth, shared-vertex mesh per tree --
# not merged flat-shaded lobes, not a cone. 0/1 stay the two pine
# silhouettes (tall/narrow vs. shorter/fuller teardrop); 2 is a rounder,
# more symmetric deciduous canopy.
# Photo comparison against the reference found these canopies rounding off
# into a ball/pom-pom instead of coming to a point -- the earlier profiles
# tapered too gradually near the top. All three now end in a short, steep
# final segment (a real spike, not a gentle round-off) so the silhouette
# reads as an actual pine cone under smooth shading. All three are pine
# variants now (narrow/tall, full/short, and a third smaller size) rather
# than mixing in a round "deciduous" shape the reference doesn't show.
CANOPY_VARIANTS = [
    (10, [0.0, 0.1, 0.4, 0.75, 1.1, 1.35, 1.5], [0.12, 0.48, 0.56, 0.42, 0.24, 0.08, 0.0]),
    (10, [0.0, 0.12, 0.45, 0.8, 1.0, 1.15], [0.18, 0.62, 0.72, 0.5, 0.22, 0.0]),
    (10, [0.0, 0.15, 0.5, 0.8, 0.95, 1.05], [0.15, 0.55, 0.65, 0.4, 0.15, 0.0]),
]
TRUNK_VARIANTS = [
    (1.6, 0.16, 0.06),
    (1.25, 0.18, 0.08),
    (1.05, 0.17, 0.09),
]


def emit_tree(name, pos, canopy_material_guid, species=0):
    # Trunk stays ProceduralTrunkMesh (now smooth-shaded); the canopy is a
    # single smooth OrganicRevolutionMesh built from one of the three
    # profiles above. Taller pine species also get one visible branch
    # breaking the trunk/canopy line.
    species = species % 3
    sides, heights, radii = CANOPY_VARIANTS[species]
    trunk_height, trunk_base_r, trunk_top_r = TRUNK_VARIANTS[species]
    with_branch = species != 2

    root_go, root_t = alloc_n(2)
    parts.append(go_block(root_go, name, [root_t]))

    _, trunk_t = emit_trunk_prop("Trunk", (0, 0, 0), 6, 4, trunk_base_r, trunk_top_r, trunk_height,
                                  0.14, 0.14, MAT_TRUNK, father=root_t)
    canopy_base_y = trunk_height * 0.62
    _, canopy_t = emit_revolution_prop("Canopy", (0, canopy_base_y, 0), sides, heights, radii,
                                        canopy_material_guid, asymmetry=0.1, radius_jitter=0.05, father=root_t)
    children = [trunk_t, canopy_t]

    if with_branch:
        # A single short branch capsule breaking the trunk/canopy line --
        # still a primitive, but only an accent detail riding on an
        # already-organic trunk+canopy, not the tree's own silhouette.
        # Side alternates deterministically from the tree's own position
        # so neighboring trees don't all lean the same way.
        branch_side = 1 if int(round(pos[0] * 10)) % 2 == 0 else -1
        branch_go, branch_t, branch_mf, branch_mr = alloc_n(4)
        parts.append(go_block(branch_go, "Branch", [branch_t, branch_mf, branch_mr]))
        branch_y = trunk_height * 0.62
        branch_rot = (0, 0, 0.30071 * branch_side, 0.95372)
        parts.append(transform_block(branch_t, branch_go, (0.05 * branch_side, branch_y, 0), rot=branch_rot,
                                      scale=(0.07, 0.4, 0.07), father=root_t))
        parts.append(meshfilter_block(branch_mf, branch_go, MESH_CAPSULE))
        parts.append(meshrenderer_block(branch_mr, branch_go, MAT_TRUNK))
        children.append(branch_t)

    parts.append(transform_block(root_t, root_go, pos, children=children, father=group_fids["Props"][1]))
    group_children["Props"].append(root_t)
    return root_go


def emit_bare_tree(name, pos, height_scale=1.0):
    # Reference-image visual correction pass: the trunk is now a tapered,
    # leaning ProceduralTrunkMesh instead of a plain cylinder -- with no
    # canopy to distract from it, a bare tree is the case where "cylinder
    # as trunk" reads worst, so it gets the same irregular-trunk treatment
    # as full trees. Three angled branch capsules (was two) for a fuller,
    # less symmetrical silhouette.
    root_go, root_t = alloc_n(2)
    b1_go, b1_t, b1_mf, b1_mr = alloc_n(4)
    b2_go, b2_t, b2_mf, b2_mr = alloc_n(4)
    b3_go, b3_t, b3_mf, b3_mr = alloc_n(4)

    parts.append(go_block(root_go, name, [root_t]))
    _, trunk_t = emit_trunk_prop("Trunk", (0, 0, 0), 6, 3, 0.13 * height_scale, 0.05 * height_scale,
                                  1.8 * height_scale, 0.18, 0.16, MAT_TRUNK, father=root_t)
    parts.append(go_block(b1_go, "Branch_01", [b1_t, b1_mf, b1_mr]))
    parts.append(transform_block(b1_t, b1_go, (0.2, 1.55 * height_scale, 0), rot=(0, 0, 0.35837, 0.93358),
                                  scale=(0.07, 0.55 * height_scale, 0.07), father=root_t))
    parts.append(meshfilter_block(b1_mf, b1_go, MESH_CYLINDER))
    parts.append(meshrenderer_block(b1_mr, b1_go, MAT_TRUNK))
    parts.append(go_block(b2_go, "Branch_02", [b2_t, b2_mf, b2_mr]))
    parts.append(transform_block(b2_t, b2_go, (-0.18, 1.7 * height_scale, 0.1), rot=(0.31048, 0, -0.31048, 0.89767),
                                  scale=(0.06, 0.45 * height_scale, 0.06), father=root_t))
    parts.append(meshfilter_block(b2_mf, b2_go, MESH_CYLINDER))
    parts.append(meshrenderer_block(b2_mr, b2_go, MAT_TRUNK))
    parts.append(go_block(b3_go, "Branch_03", [b3_t, b3_mf, b3_mr]))
    parts.append(transform_block(b3_t, b3_go, (0.05, 1.3 * height_scale, -0.15), rot=(-0.27060, 0, 0.15038, 0.95106),
                                  scale=(0.055, 0.4 * height_scale, 0.055), father=root_t))
    parts.append(meshfilter_block(b3_mf, b3_go, MESH_CYLINDER))
    parts.append(meshrenderer_block(b3_mr, b3_go, MAT_TRUNK))
    parts.append(transform_block(root_t, root_go, pos, children=[trunk_t, b1_t, b2_t, b3_t],
                                  father=group_fids["Props"][1]))

    group_children["Props"].append(root_t)
    return root_go


# Reference-image visual correction pass 2: three hand-authored rock
# "recipes" (bump count/strength/sharpness, plus a proportions multiplier
# applied via transform scale) reused across every rock/pebble/knoll
# placement -- variation in size/proportion comes from scale, not from
# generating a structurally unique mesh every time.
ROCK_VARIANTS = [
    (3, 0.3, 3.0, (1.0, 0.75, 0.9)),
    (4, 0.4, 2.5, (0.85, 0.95, 1.15)),
    (5, 0.25, 4.0, (1.1, 0.65, 1.0)),
]
BUSH_VARIANTS = [
    (9, [0.0, 0.15, 0.35, 0.5], [0.3, 0.55, 0.4, 0.0]),
    (9, [0.0, 0.2, 0.4, 0.55], [0.35, 0.6, 0.45, 0.0]),
]


def emit_bush(name, pos, radius_scale, material_guid, seed=None):
    # Reference-image visual correction pass 2: a bush is two overlapping
    # smooth OrganicRevolutionMesh clumps (a purpose-built mesh, several
    # overlapping volumes) instead of merged flat-shaded lobes -- avoids
    # reading as an "obvious spherical primitive."
    root_go, root_t = alloc_n(2)
    parts.append(go_block(root_go, name, [root_t]))
    children = []
    offsets = [(0.14, 0.0, 0.09), (-0.12, 0.02, -0.08)]
    for i, (sides, heights, radii) in enumerate(BUSH_VARIANTS):
        scaled_heights = [h * radius_scale for h in heights]
        scaled_radii = [r * radius_scale for r in radii]
        clump_seed = None if seed is None else seed + i
        _, clump_t = emit_revolution_prop(f"Clump_{i + 1:02d}", offsets[i], sides, scaled_heights, scaled_radii,
                                           material_guid, asymmetry=0.14, radius_jitter=0.06,
                                           seed=clump_seed, father=root_t)
        children.append(clump_t)
    parts.append(transform_block(root_t, root_go, pos, children=children, father=group_fids["Props"][1]))
    group_children["Props"].append(root_t)
    return root_go


def emit_rock_formation(name, pos, radius, material_guid=MAT_ROCK, variant=0, seed=None, father=None,
                         group="Props"):
    # Reference-image visual correction pass 2: a rock is now ONE smooth,
    # shared-vertex OrganicRockMesh (an icosphere with a few large,
    # low-frequency bumps) instead of several merged flat-shaded angular
    # lobes -- a rounded, irregular "eroded boulder," not a faceted gem
    # cluster. 3 reused bump/proportion presets give shape variety without
    # every rock being a structurally unique mesh.
    bump_count, bump_strength, bump_sharpness, shape = ROCK_VARIANTS[variant % len(ROCK_VARIANTS)]
    scale = (radius * shape[0], radius * shape[1], radius * shape[2])
    return emit_rock_prop(name, pos, bump_count, bump_strength, bump_sharpness, material_guid,
                           scale=scale, seed=seed, father=father, group=group)


def emit_pebble(name, pos, radius, material_guid=MAT_ROCK, variant=0, seed=None, father=None, group="Props"):
    # Small-tier ground detail: the same OrganicRockMesh technique as
    # emit_rock_formation, just at loose-stone scale -- the "small
    # elements" tier of the density hierarchy, not the "rocks" tier.
    return emit_rock_formation(name, pos, radius, material_guid, variant=variant, seed=seed, father=father,
                                group=group)


def emit_flower_cluster(name, pos, seed=None, father=None, group="Props"):
    # A thin stem (a straight primitive cylinder is a fair simplification
    # of a real stem at this scale) plus a tiny smooth OrganicRevolutionMesh
    # bloom in the accent M_Flower color, distinct from the foliage
    # palette so it reads as a flower, not another leaf.
    root_go, root_t, stem_go, stem_t, stem_mf, stem_mr = alloc_n(6)
    father_t = father if father is not None else group_fids[group][1]
    parts.append(go_block(root_go, name, [root_t]))
    parts.append(go_block(stem_go, "Stem", [stem_t, stem_mf, stem_mr]))
    parts.append(transform_block(stem_t, stem_go, (0, 0.05, 0), scale=(0.015, 0.08, 0.015), father=root_t))
    parts.append(meshfilter_block(stem_mf, stem_go, MESH_CYLINDER))
    parts.append(meshrenderer_block(stem_mr, stem_go, MAT_TRUNK))
    _, bloom_t = emit_revolution_prop("Bloom", (0, 0.11, 0), 8, [0.0, 0.02, 0.05, 0.07], [0.0, 0.06, 0.07, 0.0],
                                       MAT_FLOWER, asymmetry=0.15, radius_jitter=0.08, seed=seed, father=root_t)
    parts.append(transform_block(root_t, root_go, pos, children=[stem_t, bloom_t], father=father_t))
    if father is None:
        group_children[group].append(root_t)
    return root_go


def emit_mushroom(name, pos, seed=None, father=None, group="Props"):
    # A thin stem plus a small smooth OrganicRevolutionMesh cap (a
    # flattened dome, wider than tall) in the accent M_Mushroom color --
    # one of the small ground-detail variants the brief calls out
    # explicitly.
    root_go, root_t, stem_go, stem_t, stem_mf, stem_mr = alloc_n(6)
    father_t = father if father is not None else group_fids[group][1]
    parts.append(go_block(root_go, name, [root_t]))
    parts.append(go_block(stem_go, "Stem", [stem_t, stem_mf, stem_mr]))
    parts.append(transform_block(stem_t, stem_go, (0, 0.045, 0), scale=(0.022, 0.09, 0.022), father=root_t))
    parts.append(meshfilter_block(stem_mf, stem_go, MESH_CYLINDER))
    parts.append(meshrenderer_block(stem_mr, stem_go, MAT_CHAR_HEAD))
    _, cap_t = emit_revolution_prop("Cap", (0, 0.085, 0), 9, [0.0, 0.02, 0.05], [0.0, 0.09, 0.0],
                                     MAT_MUSHROOM, asymmetry=0.1, radius_jitter=0.06, seed=seed, father=root_t)
    parts.append(transform_block(root_t, root_go, pos, children=[stem_t, cap_t], father=father_t))
    if father is None:
        group_children[group].append(root_t)
    return root_go


def emit_log(name, pos, rot=IDENTITY_ROT, length_scale=1.0, seed=None, father=None, group="Props"):
    # Reference-image visual correction pass 2: a fallen log/branch is now
    # the same smooth ProceduralTrunkMesh used for standing trunks, laid
    # on its side -- tapered and slightly irregular, not a plain cylinder.
    return emit_trunk_prop(name, pos, 6, 3, 0.14 * length_scale, 0.07 * length_scale, 1.3 * length_scale,
                            0.15, 0.1, MAT_TRUNK, rot=rot, seed=seed, father=father, group=group)


def emit_mountain(name, pos):
    # A single "costado" landmark, deliberately bigger and rockier than
    # Hill_01 (M0.8). Reference-image visual correction pass 2: the peak
    # is now a large smooth OrganicRockMesh mass (an icosphere with a few
    # big low-frequency bumps, not merged flat-shaded lobes) and every
    # base rock is emit_rock_formation, same as every other named rock.
    root_go, root_t = alloc_n(2)
    parts.append(go_block(root_go, name, [root_t]))
    _, peak_t = emit_rock_prop("Peak", (0, 3.2, 0), 6, 0.45, 2.2, MAT_ROCK,
                                scale=(6.5, 6.0, 6.5), father=root_t)
    children = [peak_t]
    base_rocks = [
        (-4.5, 0, 3.5, 1.6), (4, 0, 4.5, 1.8), (-2, 0, -2.5, 1.3), (3.5, 0, -1.5, 1.5),
        (-3, 0, 6, 1.1), (2, 0, 6.5, 1.2),
    ]
    for i, (dx, dy, dz, radius) in enumerate(base_rocks, start=1):
        _, rock_t = emit_rock_formation(f"BaseRock_{i:02d}", (dx, dy + 0.3, dz), radius, variant=i % 3,
                                         father=root_t)
        children.append(rock_t)
    parts.append(transform_block(root_t, root_go, pos, children=children, father=group_fids["Environment"][1]))
    group_children["Environment"].append(root_t)
    return root_go


def emit_dock(name, pos, length=5.5):
    # A short line of overlapping wooden planks (boxes) from "shore" out
    # over the water, plus two simple post-like supports -- primitive
    # geometry only.
    root_go, root_t = alloc_n(2)
    parts.append(go_block(root_go, name, [root_t]))
    plank_go, plank_t, plank_mf, plank_mr = alloc_n(4)
    parts.append(go_block(plank_go, "Planks", [plank_t, plank_mf, plank_mr]))
    parts.append(transform_block(plank_t, plank_go, (0, 0.12, length / 2), scale=(1.3, 0.12, length), father=root_t))
    parts.append(meshfilter_block(plank_mf, plank_go, MESH_CUBE))
    parts.append(meshrenderer_block(plank_mr, plank_go, MAT_TRUNK))
    children = [plank_t]
    for i, dz in enumerate((0.8, length - 0.8), start=1):
        post_go, post_t, post_mf, post_mr = alloc_n(4)
        parts.append(go_block(post_go, f"Post_{i:02d}", [post_t, post_mf, post_mr]))
        parts.append(transform_block(post_t, post_go, (0, -0.35, dz), scale=(0.12, 0.9, 0.12), father=root_t))
        parts.append(meshfilter_block(post_mf, post_go, MESH_CYLINDER))
        parts.append(meshrenderer_block(post_mr, post_go, MAT_TRUNK))
        children.append(post_t)
    parts.append(transform_block(root_t, root_go, pos, children=children, father=group_fids["Structures"][1]))
    group_children["Structures"].append(root_t)
    return root_go


def emit_boat(name, pos, rot=IDENTITY_ROT):
    # A low, elongated flattened blob for the hull (strong non-uniform
    # transform scale on top of the blob's own vertical squash) plus a
    # small mast -- a simple, readable "rowboat" silhouette rather than a
    # detailed model.
    root_go, root_t = alloc_n(2)
    parts.append(go_block(root_go, name, [root_t]))
    hull_go, hull_t, hull_mf, hull_mr, hull_blob = alloc_n(5)
    parts.append(go_block(hull_go, "Hull", [hull_t, hull_mf, hull_mr, hull_blob]))
    parts.append(transform_block(hull_t, hull_go, (0, 0.18, 0), scale=(0.55, 0.4, 1.5), father=root_t))
    parts.append(f"""--- !u!33 &{hull_mf}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {hull_go}}}
  m_Mesh: {{fileID: 0}}
""")
    parts.append(meshrenderer_block(hull_mr, hull_go, MAT_ROOF))
    parts.append(mono_block(hull_blob, hull_go, SCRIPT_BLOB, f"""seed: {next_blob_seed()}
jitter: 0.1
verticalSquash: 0.5"""))
    mast_go, mast_t, mast_mf, mast_mr = alloc_n(4)
    parts.append(go_block(mast_go, "Mast", [mast_t, mast_mf, mast_mr]))
    parts.append(transform_block(mast_t, mast_go, (0, 0.55, -0.1), scale=(0.06, 0.7, 0.06), father=root_t))
    parts.append(meshfilter_block(mast_mf, mast_go, MESH_CYLINDER))
    parts.append(meshrenderer_block(mast_mr, mast_go, MAT_TRUNK))
    parts.append(transform_block(root_t, root_go, pos, rot=rot, children=[hull_t, mast_t],
                                  father=group_fids["Props"][1]))
    group_children["Props"].append(root_t)
    return root_go


def emit_house(name, pos):
    # Photo comparison against the reference: the single diamond-rotated
    # roof box read as a flat wedge rather than an actual sloped roof, and
    # the walls sat directly on the grass with no sense of a grounded
    # structure. Rebuilt with a real two-slope gable roof (two thin slabs
    # meeting at a ridge, plus a ridge cap covering the seam) and a stone
    # foundation course under the walls.
    root_go, root_t = alloc_n(2)
    found_go, found_t, found_mf, found_mr = alloc_n(4)
    walls_go, walls_t, walls_mf, walls_mr = alloc_n(4)
    roof_r_go, roof_r_t, roof_r_mf, roof_r_mr = alloc_n(4)
    roof_l_go, roof_l_t, roof_l_mf, roof_l_mr = alloc_n(4)
    ridge_go, ridge_t, ridge_mf, ridge_mr = alloc_n(4)
    chimney_go, chimney_t, chimney_mf, chimney_mr = alloc_n(4)
    door_go, door_t, door_mf, door_mr = alloc_n(4)
    window_go, window_t, window_mf, window_mr = alloc_n(4)

    parts.append(go_block(root_go, name, [root_t]))

    parts.append(go_block(found_go, "Foundation", [found_t, found_mf, found_mr]))
    parts.append(transform_block(found_t, found_go, (0, 0.1, 0), scale=(1.75, 0.2, 1.75), father=root_t))
    parts.append(meshfilter_block(found_mf, found_go, MESH_CUBE))
    parts.append(meshrenderer_block(found_mr, found_go, MAT_ROCK))

    parts.append(go_block(walls_go, "Walls", [walls_t, walls_mf, walls_mr]))
    parts.append(transform_block(walls_t, walls_go, (0, 0.9, 0), scale=(1.6, 1.4, 1.6), father=root_t))
    parts.append(meshfilter_block(walls_mf, walls_go, MESH_CUBE))
    parts.append(meshrenderer_block(walls_mr, walls_go, MAT_WALLS))

    # Wall top sits at y=1.6 (half-width 0.8 on X); the roof rises from an
    # overhanging eave (x=+-1.05, y=1.55) to a ridge at (x=0, y=2.2).
    # Right slab: local +X (outward, toward the eave) rotated down toward
    # -Y, local -X (toward the ridge) up toward +Y -- a negative Z rotation
    # by the pitch angle achieves that; the left slab is its mirror image
    # (same pitch magnitude, opposite sign, mirrored X position).
    parts.append(go_block(roof_r_go, "RoofRight", [roof_r_t, roof_r_mf, roof_r_mr]))
    parts.append(transform_block(roof_r_t, roof_r_go, (0.525, 1.875, 0), rot=(0, 0, -0.2736191, 0.9618381),
                                  scale=(1.235, 0.08, 2.2), father=root_t))
    parts.append(meshfilter_block(roof_r_mf, roof_r_go, MESH_CUBE))
    parts.append(meshrenderer_block(roof_r_mr, roof_r_go, MAT_ROOF))

    parts.append(go_block(roof_l_go, "RoofLeft", [roof_l_t, roof_l_mf, roof_l_mr]))
    parts.append(transform_block(roof_l_t, roof_l_go, (-0.525, 1.875, 0), rot=(0, 0, 0.2736191, 0.9618381),
                                  scale=(1.235, 0.08, 2.2), father=root_t))
    parts.append(meshfilter_block(roof_l_mf, roof_l_go, MESH_CUBE))
    parts.append(meshrenderer_block(roof_l_mr, roof_l_go, MAT_ROOF))

    parts.append(go_block(ridge_go, "RoofRidge", [ridge_t, ridge_mf, ridge_mr]))
    parts.append(transform_block(ridge_t, ridge_go, (0, 2.18, 0), scale=(0.18, 0.12, 2.25), father=root_t))
    parts.append(meshfilter_block(ridge_mf, ridge_go, MESH_CUBE))
    parts.append(meshrenderer_block(ridge_mr, ridge_go, MAT_ROOF))

    parts.append(go_block(chimney_go, "Chimney", [chimney_t, chimney_mf, chimney_mr]))
    parts.append(transform_block(chimney_t, chimney_go, (0.45, 2.5, 0.3), scale=(0.22, 0.9, 0.22), father=root_t))
    parts.append(meshfilter_block(chimney_mf, chimney_go, MESH_CUBE))
    parts.append(meshrenderer_block(chimney_mr, chimney_go, MAT_ROOF))

    parts.append(go_block(door_go, "Door", [door_t, door_mf, door_mr]))
    parts.append(transform_block(door_t, door_go, (0, 0.62, 0.81), scale=(0.42, 0.82, 0.06), father=root_t))
    parts.append(meshfilter_block(door_mf, door_go, MESH_CUBE))
    parts.append(meshrenderer_block(door_mr, door_go, MAT_TRUNK))

    parts.append(go_block(window_go, "Window", [window_t, window_mf, window_mr]))
    parts.append(transform_block(window_t, window_go, (-0.55, 1.15, 0.81), scale=(0.28, 0.28, 0.06), father=root_t))
    parts.append(meshfilter_block(window_mf, window_go, MESH_CUBE))
    parts.append(meshrenderer_block(window_mr, window_go, MAT_CHAR_HEAD))

    parts.append(transform_block(root_t, root_go, pos,
                                  children=[found_t, walls_t, roof_r_t, roof_l_t, ridge_t, chimney_t, door_t,
                                            window_t],
                                  father=group_fids["Structures"][1]))

    group_children["Structures"].append(root_t)
    return root_go


def emit_character_path(name, waypoints):
    root_go, root_t, path_comp = alloc_n(3)
    wp_fids = []
    for i, wp_pos in enumerate(waypoints, start=1):
        wp_go, wp_t = alloc_n(2)
        parts.append(go_block(wp_go, f"Waypoint_{i:02d}", [wp_t]))
        parts.append(transform_block(wp_t, wp_go, wp_pos, father=root_t))
        wp_fids.append(wp_t)

    parts.append(go_block(root_go, name, [root_t, path_comp]))
    parts.append(transform_block(root_t, root_go, (0, 0, 0), children=wp_fids, father=group_fids["Paths"][1]))
    parts.append(mono_block(path_comp, root_go, SCRIPT_CHARACTER_PATH, ""))

    group_children["Paths"].append(root_t)
    return path_comp


def emit_moving_target(name, pos, path_comp_fid, body_material_guid, move_speed, rotation_speed, pause_duration,
                        accessory_material_guid):
    # M0.9 visual pass: Body+Head from M0.3 are joined by two small Arms
    # and a Backpack accessory, all static children of the same "Model"
    # transform CharacterVisual already bobs/sways as one piece -- so the
    # richer silhouette rides along with the existing procedural animation
    # for free, with no change to CharacterMover/CharacterPath/
    # CharacterVisual themselves.
    root_go, root_t, mover, visual, discoverable = alloc_n(5)
    model_go, model_t = alloc_n(2)
    body_go, body_t, body_mf, body_mr = alloc_n(4)
    arm_l_go, arm_l_t, arm_l_mf, arm_l_mr = alloc_n(4)
    arm_r_go, arm_r_t, arm_r_mf, arm_r_mr = alloc_n(4)
    pack_go, pack_t, pack_mf, pack_mr = alloc_n(4)

    parts.append(go_block(root_go, name, [root_t, mover, visual, discoverable]))
    parts.append(mono_block(mover, root_go, SCRIPT_CHARACTER_MOVER, f"""path: {{fileID: {path_comp_fid}}}
moveSpeed: {move_speed}
rotationSpeed: {rotation_speed}
pauseDuration: {pause_duration}
arrivalThreshold: 0.15
turnThresholdDegrees: 5
loop: 1"""))
    parts.append(mono_block(visual, root_go, SCRIPT_CHARACTER_VISUAL, f"""model: {{fileID: {model_t}}}
walkBobHeight: 0.06
walkBobSpeed: 6
idleSwaySpeed: 1.2
idleSwayAngle: 3
discoveryReactionDuration: 0.7
discoveryPulseScale: 0.25"""))
    parts.append(mono_block(discoverable, root_go, SCRIPT_DISCOVERABLE, "isDiscovered: 0"))

    parts.append(go_block(model_go, "Model", [model_t]))
    parts.append(go_block(body_go, "Body", [body_t, body_mf, body_mr]))
    parts.append(transform_block(body_t, body_go, (0, 0.35, 0), scale=(0.5, 0.35, 0.5), father=model_t))
    parts.append(meshfilter_block(body_mf, body_go, MESH_CAPSULE))
    parts.append(meshrenderer_block(body_mr, body_go, body_material_guid))
    # Faceted head (reference-image "faceted geometry" language): a
    # low-jitter ProceduralBlobMesh instead of a perfectly smooth sphere --
    # jitter kept small (0.12) and vertical_squash near 1 so it still
    # reads clearly as a head, just with subtle polygonal facets.
    _, head_t = emit_blob_prop("Head", (0, 0.82, 0), 0.28, MAT_CHAR_HEAD,
                                jitter=0.12, vertical_squash=0.9, father=model_t)

    parts.append(go_block(arm_l_go, "ArmLeft", [arm_l_t, arm_l_mf, arm_l_mr]))
    parts.append(transform_block(arm_l_t, arm_l_go, (-0.26, 0.4, 0), rot=(0, 0, 0.17365, 0.98481),
                                  scale=(0.13, 0.32, 0.13), father=model_t))
    parts.append(meshfilter_block(arm_l_mf, arm_l_go, MESH_CAPSULE))
    parts.append(meshrenderer_block(arm_l_mr, arm_l_go, body_material_guid))
    parts.append(go_block(arm_r_go, "ArmRight", [arm_r_t, arm_r_mf, arm_r_mr]))
    parts.append(transform_block(arm_r_t, arm_r_go, (0.26, 0.4, 0), rot=(0, 0, -0.17365, 0.98481),
                                  scale=(0.13, 0.32, 0.13), father=model_t))
    parts.append(meshfilter_block(arm_r_mf, arm_r_go, MESH_CAPSULE))
    parts.append(meshrenderer_block(arm_r_mr, arm_r_go, body_material_guid))

    parts.append(go_block(pack_go, "Backpack", [pack_t, pack_mf, pack_mr]))
    parts.append(transform_block(pack_t, pack_go, (0, 0.42, -0.22), scale=(0.26, 0.3, 0.18), father=model_t))
    parts.append(meshfilter_block(pack_mf, pack_go, MESH_CUBE))
    parts.append(meshrenderer_block(pack_mr, pack_go, accessory_material_guid))

    parts.append(transform_block(model_t, model_go, (0, 0, 0),
                                  children=[body_t, head_t, arm_l_t, arm_r_t, pack_t], father=root_t))
    parts.append(transform_block(root_t, root_go, pos, children=[model_t], father=group_fids["Discoverables"][1]))

    group_children["Discoverables"].append(root_t)
    return discoverable


def emit_static_target(name, pos, scale, pulse_duration=0.7, pulse_scale=0.3):
    go, t, mf, mr, discoverable, pulse = alloc_n(6)
    parts.append(go_block(go, name, [t, mf, mr, discoverable, pulse]))
    parts.append(transform_block(t, go, pos, scale=scale, father=group_fids["Discoverables"][1]))
    parts.append(meshfilter_block(mf, go, MESH_SPHERE))
    parts.append(meshrenderer_block(mr, go, MAT_GEM))
    parts.append(mono_block(discoverable, go, SCRIPT_DISCOVERABLE, "isDiscovered: 0"))
    parts.append(mono_block(pulse, go, SCRIPT_PULSE_FEEDBACK, f"""model: {{fileID: {t}}}
pulseDuration: {pulse_duration}
pulseScale: {pulse_scale}"""))

    group_children["Discoverables"].append(t)
    return discoverable


# ---------------------------------------------------------------------------
# Environment: Directional Light, ground, hill
# ---------------------------------------------------------------------------
light_go, light_t, light_comp = alloc_n(3)
parts.append(go_block(light_go, "Directional Light", [light_t, light_comp]))
parts.append(transform_block(light_t, light_go, (0, 3, 0),
                              rot=(0.4082179, -0.2345697, 0.1093816, 0.8754261),
                              father=group_fids["Environment"][1]))
parts.append(f"""--- !u!108 &{light_comp}
Light:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {light_go}}}
  m_Enabled: 1
  serializedVersion: 12
  m_Type: 1
  m_Color: {{r: 1, g: 0.95686275, b: 0.8392157, a: 1}}
  m_Intensity: 1.1
  m_Range: 10
  m_SpotAngle: 30
  m_InnerSpotAngle: 21.802082
  m_CookieSize2D: {{x: 10, y: 10}}
  m_Shadows:
    m_Type: 2
    m_Resolution: -1
    m_CustomResolution: -1
    m_Strength: 0.85
    m_Bias: 0.05
    m_NormalBias: 0.4
    m_NearPlane: 0.2
    m_CullingMatrixOverride:
      e00: 1
      e01: 0
      e02: 0
      e03: 0
      e10: 0
      e11: 1
      e12: 0
      e13: 0
      e20: 0
      e21: 0
      e22: 1
      e23: 0
      e30: 0
      e31: 0
      e32: 0
      e33: 1
    m_UseCullingMatrixOverride: 0
  m_Cookie: {{fileID: 0}}
  m_DrawHalo: 0
  m_Flare: {{fileID: 0}}
  m_RenderMode: 0
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingLayerMask: 1
  m_Lightmapping: 4
  m_LightShadowCasterMode: 0
  m_AreaSize: {{x: 1, y: 1}}
  m_BounceIntensity: 1
  m_ColorTemperature: 6570
  m_UseColorTemperature: 0
  m_BoundingSphereOverride: {{x: 0, y: 0, z: 0, w: 0}}
  m_UseBoundingSphereOverride: 0
  m_UseViewFrustumForShadowCasterCull: 1
  m_ForceVisible: 0
  m_ShadowRadius: 0
  m_ShadowAngle: 0
  m_LightUnit: 1
  m_LuxAtDistance: 1
  m_EnableSpotReflector: 1
""")
group_children["Environment"].append(light_t)

# Map enlarged ~50% this pass (pan half 13 -> 20, ground scaled to match).
# Thick two-tone terrain chunk (matched against a reference low-poly
# diorama): a tall warm tan/gold "TerrainBase" block for the visible
# sides, with the thin green "DioramaBase" grass slab sitting directly on
# top of it. The grass slab's TOP surface stays at exactly y=0 (its size
# and position are unchanged from before) since every prop in the scene
# is placed assuming that surface -- only the new tan block underneath is
# new, seamlessly joining at y=-1 where the grass slab's underside is.
emit_simple_prop("TerrainBase", MESH_CUBE, (0, -2.75, 0), (106, 3.5, 106), MAT_TERRAIN_SIDE, group="Environment")
emit_simple_prop("DioramaBase", MESH_CUBE, (0, -0.5, 0), (106, 1, 106), MAT_GROUND_MAIN, group="Environment")
emit_simple_prop("Hill_01", MESH_SPHERE, (-9, -1, 11), (5, 2, 5), MAT_GROUND, group="Environment")
PLACED_FOOTPRINTS.append((-9, 11, 5.5))

# Shallow recessed hollow near the hill/rock nook -- a flattened disc just
# below the main ground's surface (y=0), in a cooler mossy tone, so that
# corner reads as a small sunken clearing rather than flat ground with
# rocks scattered on it. Simple primitive geometry (a squashed cylinder),
# not terrain deformation.
emit_simple_prop("MossyHollow", MESH_CYLINDER, (-8, -0.08, 10.5), (2.3, 0.05, 2.3), MAT_MOSS, group="Environment")

# Reference-image visual correction pass 2: the terrain itself needed more
# visual interest than one flat slab. Gentle elevation knolls -- a smooth
# OrganicRockMesh heavily flattened via transform scale -- scattered
# across the open ground, plus more recessed hollow patches and a couple
# of loose ground-texture patches (dirt/moss) away from the hill -- none
# of them touching the y=0 surface elsewhere in the scene.
terrain_knolls = [
    (-3, -12, 3.2), (6, -2.5, 2.6), (-14, -4, 3.0), (11, -9, 2.8),
    (-2, 6.5, 2.4), (13, 10, 3.0),
]
for i, (x, z, radius) in enumerate(terrain_knolls, start=1):
    emit_rock_prop(f"TerrainKnoll_{i:02d}", (x, -0.15, z), 4, 0.2, 2.5, MAT_GROUND,
                    scale=(radius, radius * 0.22, radius), group="Environment")
    PLACED_FOOTPRINTS.append((x, z, radius))

terrain_hollows = [
    (5, -0.08, -13, 2.0, MAT_MOSS), (-11, -0.08, -8, 1.8, MAT_MOSS),
    (16, -0.08, -3, 1.6, MAT_DIRT), (-4, -0.08, 4, 1.7, MAT_MOSS),
]
for i, (x, y, z, radius, mat) in enumerate(terrain_hollows, start=1):
    emit_simple_prop(f"GroundHollow_{i:02d}", MESH_CYLINDER, (x, y, z), (radius, 0.05, radius), mat,
                      group="Environment")
    PLACED_FOOTPRINTS.append((x, z, radius))

ground_patches = [
    (-6, 0.02, -11, 2.4, 2.0, MAT_DIRT), (9, 0.02, 9, 2.2, 1.9, MAT_MOSS),
]
for i, (x, y, z, sx, sz, mat) in enumerate(ground_patches, start=1):
    emit_simple_prop(f"GroundPatch_{i:02d}", MESH_CYLINDER, (x, y, z), (sx, 0.03, sz), mat, group="Environment")

# ---------------------------------------------------------------------------
# Structures: House
# ---------------------------------------------------------------------------
emit_house("House", (3, 0, 9))
PLACED_FOOTPRINTS.append((3, 9, 2.2))

# ---------------------------------------------------------------------------
# Props: trees, bushes, rocks, path stones, stump
# ---------------------------------------------------------------------------
left_cluster = [(-11, -5), (-8, -2), (-12, 2), (-9, 6), (-11.5, 9), (-7.5, 10.5)]
right_cluster = [(6, 3), (9, 2), (11, 5), (7, 7), (10, 8.5), (6.5, 11), (9.5, 12)]

for i, (x, z) in enumerate(left_cluster, start=1):
    mat = MAT_FOLIAGE_A if i % 2 else MAT_FOLIAGE_B
    # species cycles 0/1/2 (pine-tall, pine-full, deciduous) so a cluster
    # reads as mixed forest rather than one repeated silhouette.
    emit_tree(f"Tree_{i:02d}", (x, 0, z), mat, species=i % 3)
    PLACED_FOOTPRINTS.append((x, z, 1.1))

for j, (x, z) in enumerate(right_cluster, start=len(left_cluster) + 1):
    mat = MAT_FOLIAGE_A if j % 2 else MAT_FOLIAGE_B
    emit_tree(f"Tree_{j:02d}", (x, 0, z), mat, species=j % 3)
    PLACED_FOOTPRINTS.append((x, z, 1.1))

# Bushes: two overlapping smooth OrganicRevolutionMesh clumps each (see
# emit_bush), same two foliage materials as the tree canopies for a
# cohesive palette.
bushes = [
    ("Bush_01", (-5, 0.35, -4), 0.85, MAT_FOLIAGE_B),
    ("Bush_02", (2.5, 0.3, -3), 0.7, MAT_FOLIAGE_A),
    ("Bush_03", (1.5, 0.3, 8), 0.72, MAT_FOLIAGE_B),
    ("Bush_04", (-6.5, 0.3, 9.5), 0.8, MAT_FOLIAGE_A),
]
for name, pos, radius, mat in bushes:
    emit_bush(name, pos, radius, mat)
    PLACED_FOOTPRINTS.append((pos[0], pos[2], radius * 0.8))

# Rocks: one smooth OrganicRockMesh formation each (see emit_rock_formation),
# so they read as rounded eroded boulders rather than foliage or a faceted
# primitive.
rocks = [
    ("Rock_01", (-8, 0.25, 9.5), 0.62),
    ("Rock_02", (-7, 0.22, 11.5), 0.52),
    ("Rock_03", (-9.5, 0.28, 12), 0.68),
    ("Rock_04", (4, 0.25, -8), 0.55),
    ("Rock_05", (-3, 0.22, -9), 0.46),
    ("Rock_06", (2.2, 0.2, 0), 0.4),
]
for i, (name, pos, radius) in enumerate(rocks):
    emit_rock_formation(name, pos, radius, variant=i % 3)
    PLACED_FOOTPRINTS.append((pos[0], pos[2], radius * 0.9))

# Small-tier ground detail near the core clearing: loose pebbles, flower
# clusters, and a mushroom (see emit_pebble/emit_flower_cluster/emit_mushroom).
core_pebbles = [
    (-6.5, -3.5), (3.2, 8.5), (-2, -8.5), (0.8, 2.5), (5, 6.5), (-8.5, 6),
]
for i, (x, z) in enumerate(core_pebbles, start=1):
    emit_pebble(f"Pebble_{i:02d}", (x, 0.08, z), 0.16 + 0.06 * (i % 3), variant=i % 3)

core_flowers = [(-4.5, -1), (2, 7), (-7, 8.5)]
for i, (x, z) in enumerate(core_flowers, start=1):
    emit_flower_cluster(f"Flowers_{i:02d}", (x, 0.05, z))

emit_mushroom("Mushroom_01", (-3.2, 0.1, -5.5))
emit_mushroom("Mushroom_02", (6.2, 0.1, 9))

path_positions = [(0.5, -9), (0.8, -6.5), (1.2, -4), (1.5, -1.5), (1.8, 1), (2.2, 3.5), (2.5, 6)]
for i, (x, z) in enumerate(path_positions, start=1):
    emit_simple_prop(f"PathStone_{i:02d}", MESH_CUBE, (x, 0.05, z), (0.8, 0.1, 0.8), MAT_PATH)

emit_simple_prop("Stump", MESH_CYLINDER, (3.5, 0.3, -7), (0.6, 0.5, 0.6), MAT_TRUNK)

# A fallen log and a small crate: a log is now the smooth tapered trunk
# mesh laid on its side (see emit_log); the crate stays a plain primitive
# box (a crate genuinely IS a box, not something the brief flags).
emit_log("FallenLog", (-2.5, 0.18, -6.5), rot=(0.70711, 0, 0, 0.70711))
emit_simple_prop("Crate", MESH_CUBE, (4.2, 0.2, -4.5), (0.4, 0.4, 0.4), MAT_TRUNK)

# Grass tufts: small, flattened, low-jitter blobs scattered near the
# clearing and path edges purely for ground texture -- kept few and cheap
# (20 triangles each) rather than a dense field, per the mobile/object-
# count constraints.
grass_tufts = [
    (-2, -7.5), (0.5, -3), (3, -1), (-4, -1.5), (2.8, 5),
    (-1, 4), (5.5, -2), (-9, 4), (0, 9.5), (-3.5, 7.5),
]
for i, (x, z) in enumerate(grass_tufts, start=1):
    mat = MAT_FOLIAGE_A if i % 2 else MAT_FOLIAGE_B
    emit_blob_prop(f"GrassTuft_{i:02d}", (x, 0.12, z), 0.22, mat, jitter=0.28, vertical_squash=0.45)

# ---------------------------------------------------------------------------
# Map expansion (+50%): a mountain "a un costado" (right side) with a
# waterfall feeding a river that runs across the newly expanded southern
# margin to a dock and boat, plus dirt roads and extra density. Everything
# here sits outside the original ~26x26 core (clearing/path/house/tree
# clusters/targets), which is untouched, so none of it disturbs the
# existing discovery layout.
# ---------------------------------------------------------------------------
emit_mountain("Mountain", (17, 0, 6))
PLACED_FOOTPRINTS.append((17, 6, 9.5))

# A low foothill terrace ringing the mountain's base -- a second,
# decorative elevation step (a flat dirt-toned slab) giving a stepped
# transition from flat ground up to the mountain, entirely in the
# target-free margin, purely for "layered terrain elevation."
emit_simple_prop("MountainTerrace", MESH_CUBE, (15, 0.1, 8), (7, 0.2, 6), MAT_DIRT, group="Environment")

# Waterfall: a single tilted slab leaning against the mountain's near
# face, cascading down to the pool that starts the river.
emit_simple_prop("Waterfall", MESH_CUBE, (16, 2, 4.3), (1.0, 4.6, 0.4), MAT_WATER,
                  rot=(0.08716, 0, 0, 0.99619), group="Environment")

river_waypoints = [
    (15, 3), (14.3, -3), (12.8, -9), (8.5, -14), (1, -16.5),
    (-7, -17), (-13, -15.5), (-16, -13),
]
emit_water_path("River", river_waypoints)

# Dirt roads: wider, warmer-brown, gently wavy strips -- visually distinct
# from the neat grey-tan PathStone walkway near the house. Named
# variables (not inline literals) so the dense scatter pass further below
# can also use them to keep new density from straddling the roads.
road_to_mountain_wp = [(2, 4), (6, 1.5), (10, 0.5), (13.5, 2.5), (15, 4.5)]
road_to_dock_wp = [(1, -9), (-3, -11), (-8, -12.5), (-13, -13), (-14.5, -13)]
emit_dirt_road("RoadToMountain", road_to_mountain_wp)
emit_dirt_road("RoadToDock", road_to_dock_wp)

emit_dock("Dock", (-15, 0, -13.5))
PLACED_FOOTPRINTS.append((-15, -13.5, 3.0))
emit_boat("Boat", (-17.5, 0.15, -12.5), rot=(0, 0.25882, 0, 0.96593))
PLACED_FOOTPRINTS.append((-17.5, -12.5, 1.8))

# Extra density in the expanded margin: more trees/rocks/bushes/grass, plus
# a handful of bare trees for variety, at roughly the same medium density
# as the original core rather than packing every open space.
margin_trees = [
    (14, 9), (16, 12), (-15, 5), (-17, 8), (-4, -13), (5, -12),
    (-16, -6), (13, -4), (-2, 12.5), (17, -2),
]
for i, (x, z) in enumerate(margin_trees, start=len(left_cluster) + len(right_cluster) + 1):
    mat = MAT_FOLIAGE_A if i % 2 else MAT_FOLIAGE_B
    emit_tree(f"Tree_{i:02d}", (x, 0, z), mat, species=i % 3)
    PLACED_FOOTPRINTS.append((x, z, 1.1))

bare_trees = [
    (12, 6), (15, 8.5), (-6, -14), (9, -13), (-15, -3), (18, 3),
]
for i, (x, z) in enumerate(bare_trees, start=1):
    emit_bare_tree(f"BareTree_{i:02d}", (x, 0, z), height_scale=0.9 + 0.2 * (i % 2))
    PLACED_FOOTPRINTS.append((x, z, 0.7))

margin_rocks = [
    (10, -6), (-14, 1), (2, -14), (-10, -14.5), (14, -5.5),
    (-18, 9), (7, 11.5), (-3, 13.5),
]
for i, (x, z) in enumerate(margin_rocks, start=len(rocks) + 1):
    emit_rock_formation(f"Rock_{i:02d}", (x, 0.22, z), 0.5, variant=i % 3)
    PLACED_FOOTPRINTS.append((x, z, 0.6))

margin_bushes = [
    (-13, -12), (4, -13.5), (-16, 7), (12, -3), (-1, -14.5),
]
for i, (x, z) in enumerate(margin_bushes, start=len(bushes) + 1):
    mat = MAT_FOLIAGE_A if i % 2 else MAT_FOLIAGE_B
    emit_bush(f"Bush_{i:02d}", (x, 0.3, z), 0.75, mat)
    PLACED_FOOTPRINTS.append((x, z, 0.7))

margin_grass = [
    (-12, -10), (3, -11), (9, -8), (-6, 9), (14, 0),
    (-18, -1), (7, -15.5), (-9, 13), (16, -8), (0, -18),
]
for i, (x, z) in enumerate(margin_grass, start=len(grass_tufts) + 1):
    mat = MAT_FOLIAGE_A if i % 2 else MAT_FOLIAGE_B
    emit_blob_prop(f"GrassTuft_{i:02d}", (x, 0.12, z), 0.22, mat, jitter=0.28, vertical_squash=0.45)

# ---------------------------------------------------------------------------
# Dense environmental scatter (reference-image visual correction pass):
# every hand-placed cluster above concentrates content near the map's
# center. This pass fills the rest of the ~40x40 playable footprint --
# edges, corners, riverbanks, road/path edges, the rocky mountain base --
# with more trees/bushes/rock formations/small ground detail. A fixed
# seed makes it fully deterministic (regenerating the scene always
# reproduces the same layout); rejection sampling keeps every new point
# clear of the 6 targets, the river/road/path centerlines, and anything
# already placed (big-tier families only -- small ground detail is
# allowed to sit close to/under bigger elements, which is how real
# undergrowth actually clusters).
# ---------------------------------------------------------------------------
_scatter_rng = random.Random(20260920)

# Mirrors the literal positions in the "Discoverable targets" section
# below -- never change one without the other. Only X/Z matter here.
_TARGET_XZ = [(1.8, -5), (1.2, 10.3), (-5.6, -3.6), (8, 4), (-8.3, 10.8), (-10, 0)]
_TARGET_CLEARANCE = 1.4

_POLYLINE_EXCLUSIONS = [
    (river_waypoints, 2.2),
    (road_to_mountain_wp, 1.6),
    (road_to_dock_wp, 1.6),
    (path_positions, 1.3),
]

_SCATTER_X_RANGE = (-19, 19)
_SCATTER_Z_RANGE = (-19, 15)


def _point_segment_distance(px, pz, x1, z1, x2, z2):
    dx, dz = x2 - x1, z2 - z1
    length_sq = dx * dx + dz * dz
    if length_sq == 0:
        return math.hypot(px - x1, pz - z1)
    t = max(0.0, min(1.0, ((px - x1) * dx + (pz - z1) * dz) / length_sq))
    return math.hypot(px - (x1 + t * dx), pz - (z1 + t * dz))


def _point_polyline_distance(px, pz, waypoints):
    if len(waypoints) < 2:
        x1, z1 = waypoints[0]
        return math.hypot(px - x1, pz - z1)
    return min(_point_segment_distance(px, pz, x1, z1, x2, z2)
               for (x1, z1), (x2, z2) in zip(waypoints, waypoints[1:]))


def _is_excluded(x, z):
    for tx, tz in _TARGET_XZ:
        if math.hypot(x - tx, z - tz) < _TARGET_CLEARANCE:
            return True
    for waypoints, clearance in _POLYLINE_EXCLUSIONS:
        if _point_polyline_distance(x, z, waypoints) < clearance:
            return True
    return False


def _too_close_to_footprint(x, z, min_gap):
    return any(math.hypot(x - fx, z - fz) < fr + min_gap for fx, fz, fr in PLACED_FOOTPRINTS)


def _scatter_family(count, min_spacing, avoid_big=True, attempts_per_point=120):
    placed = []
    attempts = 0
    while len(placed) < count and attempts < count * attempts_per_point:
        attempts += 1
        x = _scatter_rng.uniform(*_SCATTER_X_RANGE)
        z = _scatter_rng.uniform(*_SCATTER_Z_RANGE)
        if _is_excluded(x, z):
            continue
        if any(math.hypot(x - px, z - pz) < min_spacing for px, pz in placed):
            continue
        if avoid_big and _too_close_to_footprint(x, z, min_spacing):
            continue
        placed.append((x, z))
    return placed


# Medium/large tier: extra trees, bushes, and rock formations, each
# registering its own footprint so later families (and each other) don't
# stack directly on top of them.
for i, (x, z) in enumerate(_scatter_family(14, 2.0), start=1):
    species = _scatter_rng.randrange(3)
    mat = MAT_FOLIAGE_A if _scatter_rng.random() < 0.5 else MAT_FOLIAGE_B
    emit_tree(f"ScatterTree_{i:02d}", (x, 0, z), mat, species=species)
    PLACED_FOOTPRINTS.append((x, z, 1.1))

for i, (x, z) in enumerate(_scatter_family(20, 1.0), start=1):
    mat = MAT_FOLIAGE_A if _scatter_rng.random() < 0.5 else MAT_FOLIAGE_B
    radius = 0.5 + _scatter_rng.random() * 0.35
    emit_bush(f"ScatterBush_{i:02d}", (x, 0.3, z), radius, mat)
    PLACED_FOOTPRINTS.append((x, z, radius * 0.8))

for i, (x, z) in enumerate(_scatter_family(16, 1.1), start=1):
    radius = 0.35 + _scatter_rng.random() * 0.35
    emit_rock_formation(f"ScatterRock_{i:02d}", (x, 0.2, z), radius, variant=i % 3)
    PLACED_FOOTPRINTS.append((x, z, radius * 0.9))

# Fallen branches: the same smooth tapered trunk mesh as standing trees,
# laid on its side via one of two simple 90-degree-lay orientations
# (matching FallenLog's own convention), alternated for variety -- no
# quaternion composition needed.
_lay_rotations = [(0.70711, 0, 0, 0.70711), (0, 0, 0.70711, 0.70711)]
for i, (x, z) in enumerate(_scatter_family(10, 1.1), start=1):
    emit_log(f"ScatterBranch_{i:02d}", (x, 0.15, z), rot=_lay_rotations[i % 2], length_scale=0.6)
    PLACED_FOOTPRINTS.append((x, z, 1.0))

# Small tier: pebbles, grass, flower clusters, and mushrooms -- allowed to
# sit close to or under bigger elements (avoid_big=False), the way real
# ground detail actually clusters around trees and rocks.
for i, (x, z) in enumerate(_scatter_family(45, 0.45, avoid_big=False), start=1):
    radius = 0.12 + _scatter_rng.random() * 0.14
    emit_pebble(f"ScatterPebble_{i:02d}", (x, 0.06, z), radius, variant=i % 3)

for i, (x, z) in enumerate(_scatter_family(40, 0.4, avoid_big=False), start=1):
    mat = MAT_FOLIAGE_A if _scatter_rng.random() < 0.5 else MAT_FOLIAGE_B
    emit_blob_prop(f"ScatterGrass_{i:02d}", (x, 0.12, z), 0.2 + _scatter_rng.random() * 0.08, mat,
                    jitter=0.28, vertical_squash=0.45)

for i, (x, z) in enumerate(_scatter_family(18, 0.6, avoid_big=False), start=1):
    emit_flower_cluster(f"ScatterFlowers_{i:02d}", (x, 0.05, z))

for i, (x, z) in enumerate(_scatter_family(8, 0.7, avoid_big=False), start=1):
    emit_mushroom(f"ScatterMushroom_{i:02d}", (x, 0.05, z))

# ---------------------------------------------------------------------------
# Paths + moving Discoverable targets (Target_04, Target_06)
# ---------------------------------------------------------------------------
target04_path = emit_character_path("Target_04_Path", [(8, 0, 4), (10, 0, 6.5), (7, 0, 9), (9.5, 0, 10.5)])
target06_path = emit_character_path("Target_06_Path", [(-10, 0, 0), (-7, 0, 3), (-11, 0, 6), (-8.5, 0, 8.5)])

# ---------------------------------------------------------------------------
# Discoverable targets, in order 01..06
# ---------------------------------------------------------------------------
target_discoverables = []
target_discoverables.append(emit_static_target("Target_01", (1.8, 0.15, -5), (0.35, 0.35, 0.35)))
target_discoverables.append(emit_static_target("Target_02", (1.2, 0.15, 10.3), (0.35, 0.35, 0.35)))
target_discoverables.append(emit_static_target("Target_03", (-5.6, 0.15, -3.6), (0.32, 0.32, 0.32)))
target_discoverables.append(emit_moving_target("Target_04", (8, 0, 4), target04_path, MAT_CHAR_BODY_A,
                                                move_speed=3.6, rotation_speed=110, pause_duration=1.6,
                                                accessory_material_guid=MAT_TRUNK))
target_discoverables.append(emit_static_target("Target_05", (-8.3, 0.15, 10.8), (0.28, 0.28, 0.28)))
target_discoverables.append(emit_moving_target("Target_06", (-10, 0, 0), target06_path, MAT_CHAR_BODY_B,
                                                move_speed=3.8, rotation_speed=110, pause_duration=1.8,
                                                accessory_material_guid=MAT_FOLIAGE_A))

# ---------------------------------------------------------------------------
# Camera rig (named "Camera" per the M0.8 hierarchy) -- direct child of World
# ---------------------------------------------------------------------------
CAMERA_ANGLE_DEG = 50
# Map enlarged ~50% this pass: pan half and both zoom distances scaled by
# the same ~1.5x so the wider world still frames the same way relative to
# its own size (M0.9's ratios preserved, not redesigned). minZoomDistance
# is left at 6 -- close-in inspection is unaffected by the map's overall
# size.
START_DISTANCE = 78
MIN_ZOOM = 6
MAX_ZOOM = 98
PAN_HALF = 20

angle_rad = math.radians(CAMERA_ANGLE_DEG)
cam_local_y = START_DISTANCE * math.sin(angle_rad)
cam_local_z = -START_DISTANCE * math.cos(angle_rad)
half_pitch = math.radians(CAMERA_ANGLE_DEG / 2)
cam_rot = (math.sin(half_pitch), 0, 0, math.cos(half_pitch))

rig_go, rig_t, cam_input, cam_controller = alloc_n(4)
maincam_go, maincam_t, cam_comp, audio_listener, urp_data, discovery_system = alloc_n(6)

parts.append(go_block(rig_go, "Camera", [rig_t, cam_input, cam_controller]))
parts.append(mono_block(cam_input, rig_go, SCRIPT_CAMERA_INPUT, ""))
parts.append(mono_block(cam_controller, rig_go, SCRIPT_CAMERA_CONTROLLER, f"""cameraTransform: {{fileID: {maincam_t}}}
cameraAngle: {CAMERA_ANGLE_DEG}
panSpeed: 0.015
panSmoothing: 8
panBounds:
  serializedVersion: 2
  x: {-PAN_HALF}
  y: {-PAN_HALF}
  width: {PAN_HALF * 2}
  height: {PAN_HALF * 2}
zoomSpeed: 1
zoomSmoothing: 10
minZoomDistance: {MIN_ZOOM}
maxZoomDistance: {MAX_ZOOM}"""))

parts.append(go_block(maincam_go, "Main Camera", [maincam_t, cam_comp, audio_listener, urp_data, discovery_system], tag="MainCamera"))
parts.append(transform_block(maincam_t, maincam_go, (0, round(cam_local_y, 3), round(cam_local_z, 3)),
                              rot=tuple(round(v, 7) for v in cam_rot), father=rig_t, children=[cel_t]))
parts.append(f"""--- !u!20 &{cam_comp}
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {maincam_go}}}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 2
  m_BackGroundColor: {{r: 0.6509434, g: 0.7686275, b: 0.8509434, a: 1}}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_Iso: 200
  m_ShutterSpeed: 0.005
  m_Aperture: 16
  m_FocusDistance: 10
  m_FocalLength: 50
  m_BladeCount: 5
  m_Curvature: {{x: 2, y: 11}}
  m_BarrelClipping: 0.25
  m_Anamorphism: 0
  m_SensorSize: {{x: 36, y: 24}}
  m_LensShift: {{x: 0, y: 0}}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 1000
  field of view: 45
  orthographic: 0
  orthographic size: 5
  m_Depth: 0
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {{fileID: 0}}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 0
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
""")
parts.append(f"""--- !u!81 &{audio_listener}
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {maincam_go}}}
  m_Enabled: 1
""")
parts.append(mono_block(urp_data, maincam_go, SCRIPT_URP_CAMERA_DATA, """m_RenderShadows: 1
m_RequiresDepthTextureOption: 2
m_RequiresOpaqueTextureOption: 2
m_CameraType: 0
m_Cameras: []
m_RendererIndex: -1
m_VolumeLayerMask:
  serializedVersion: 2
  m_Bits: 1
m_VolumeTrigger: {fileID: 0}
m_VolumeFrameworkUpdateModeOption: 2
m_RenderPostProcessing: 0
m_Antialiasing: 0
m_AntialiasingQuality: 2
m_StopNaN: 0
m_Dithering: 0
m_ClearDepth: 1
m_AllowXRRendering: 1
m_AllowHDROutput: 0
m_UseScreenCoordOverride: 0
m_ScreenSizeOverride: {x: 0, y: 0, z: 0, w: 0}
m_ScreenCoordScaleBias: {x: 0, y: 0, z: 0, w: 0}
m_RequiresDepthTexture: 0
m_RequiresColorTexture: 0
m_TaaSettings:
  m_Quality: 3
  m_FrameInfluence: 0.1
  m_JitterScale: 1
  m_MipBias: 0
  m_VarianceClampScale: 0.9
  m_ContrastAdaptiveSharpening: 0
m_Version: 2""", editor_class_identifier="Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.UniversalAdditionalCameraData"))

discoverables_field = "\n".join(f"- {{fileID: {d}}}" for d in target_discoverables)
parts.append(mono_block(discovery_system, maincam_go, SCRIPT_DISCOVERY_SYSTEM, f"""observerCamera: {{fileID: {cam_comp}}}
discoverables:
{discoverables_field}
discoveryRange: 10
requireLineOfSight: 0
lineOfSightMask:
  serializedVersion: 2
  m_Bits: 4294967295
viewportMargin: 0.05"""))

parts.append(transform_block(rig_t, rig_go, (0, 0, -6), children=[maincam_t], father=world_t))

# ---------------------------------------------------------------------------
# GameSystems: GameBootstrap, DiscoveryManager, CompletionFeedback,
# FireworkEffect rig, LevelInfo
# ---------------------------------------------------------------------------
boot_go, boot_t, boot_comp = alloc_n(3)
parts.append(go_block(boot_go, "GameBootstrap", [boot_t, boot_comp]))
parts.append(transform_block(boot_t, boot_go, (0, 0, 0), father=group_fids["GameSystems"][1]))
parts.append(mono_block(boot_comp, boot_go, SCRIPT_GAME_BOOTSTRAP, "targetFrameRate: 60"))
group_children["GameSystems"].append(boot_t)

dm_go, dm_t, dm_comp, completion_comp = alloc_n(4)
parts.append(go_block(dm_go, "DiscoveryManager", [dm_t, dm_comp, completion_comp]))
parts.append(transform_block(dm_t, dm_go, (0, 0, 0), father=group_fids["GameSystems"][1]))
targets_field = "\n".join(f"- {{fileID: {d}}}" for d in target_discoverables)
parts.append(mono_block(dm_comp, dm_go, SCRIPT_DISCOVERY_MANAGER, f"targets:\n{targets_field}"))
parts.append(mono_block(completion_comp, dm_go, SCRIPT_COMPLETION_FEEDBACK, f"""manager: {{fileID: {dm_comp}}}
keyLight: {{fileID: {light_comp}}}
pulseDuration: 1
intensityBoost: 0.6"""))
group_children["GameSystems"].append(dm_t)

# FireworkEffect rig: 8 sparks, cycling through the same 4 materials used
# in M0.7's rig.
spark_materials = [MAT_GEM, MAT_CHAR_BODY_A, MAT_FOLIAGE_A, MAT_CHAR_BODY_B]
fx_go, fx_t, fx_comp = alloc_n(3)
spark_t_fids = []
for i in range(8):
    s_go, s_t, s_mf, s_mr = alloc_n(4)
    parts.append(go_block(s_go, f"Spark_{i + 1:02d}", [s_t, s_mf, s_mr]))
    parts.append(transform_block(s_t, s_go, (0, 0, 0), scale=(0, 0, 0), father=fx_t))
    parts.append(meshfilter_block(s_mf, s_go, MESH_SPHERE))
    parts.append(meshrenderer_block(s_mr, s_go, spark_materials[i % len(spark_materials)],
                                     cast_shadows=0, receive_shadows=0, light_probe=0, reflection_probe=0))
    spark_t_fids.append(s_t)

parts.append(go_block(fx_go, "FireworkEffect", [fx_t, fx_comp]))
sparks_field = "\n".join(f"- {{fileID: {t}}}" for t in spark_t_fids)
parts.append(mono_block(fx_comp, fx_go, SCRIPT_FIREWORK, f"""manager: {{fileID: {dm_comp}}}
sparks:
{sparks_field}
burstDuration: 0.6
burstRadius: 0.9
startHeight: 1.1
sparkSize: 0.16"""))
parts.append(transform_block(fx_t, fx_go, (0, 0, 0), children=spark_t_fids, father=group_fids["GameSystems"][1]))
group_children["GameSystems"].append(fx_t)

# M0.9: CompletionCelebration rig -- a bigger, longer, screen-covering
# version of the same pre-placed-sparks idea, parented to Main Camera (in
# camera-local space) instead of the GameSystems group, so it always reads
# as covering the screen regardless of the camera's current pan/zoom. 16
# sparks (double FireworkEffect's 8), same 4-material cycle.
CELEBRATION_SPARK_COUNT = 16
cel_spark_t_fids = []
for i in range(CELEBRATION_SPARK_COUNT):
    s_go, s_t, s_mf, s_mr = alloc_n(4)
    parts.append(go_block(s_go, f"CelebrationSpark_{i + 1:02d}", [s_t, s_mf, s_mr]))
    parts.append(transform_block(s_t, s_go, (0, 0, 0), scale=(0, 0, 0), father=cel_t))
    parts.append(meshfilter_block(s_mf, s_go, MESH_SPHERE))
    parts.append(meshrenderer_block(s_mr, s_go, spark_materials[i % len(spark_materials)],
                                     cast_shadows=0, receive_shadows=0, light_probe=0, reflection_probe=0))
    cel_spark_t_fids.append(s_t)

parts.append(go_block(cel_go, "CompletionCelebration", [cel_t, cel_comp]))
cel_sparks_field = "\n".join(f"- {{fileID: {t}}}" for t in cel_spark_t_fids)
parts.append(mono_block(cel_comp, cel_go, SCRIPT_COMPLETION_CELEBRATION, f"""manager: {{fileID: {dm_comp}}}
sparks:
{cel_sparks_field}
burstDuration: 1.1
burstRadius: 3.5
sparkSize: 0.5
horizontalSpread: 0.6"""))
# Local position (0, 0, 10): 10 units along the camera's own forward axis,
# so the burst sits centered in view no matter where the rig (and camera)
# have panned/zoomed to.
parts.append(transform_block(cel_t, cel_go, (0, 0, 10), children=cel_spark_t_fids, father=maincam_t))

levelinfo_go, levelinfo_t, levelinfo_comp = alloc_n(3)
parts.append(go_block(levelinfo_go, "LevelInfo", [levelinfo_t, levelinfo_comp]))
parts.append(transform_block(levelinfo_t, levelinfo_go, (0, 0, 0), father=group_fids["GameSystems"][1]))
parts.append(mono_block(levelinfo_comp, levelinfo_go, SCRIPT_LEVEL_INFO, f"""level: {{fileID: 11400000, guid: {LEVEL_ASSET_GUID}, type: 2}}
manager: {{fileID: {dm_comp}}}"""))
group_children["GameSystems"].append(levelinfo_t)

# ---------------------------------------------------------------------------
# UI: DiscoveryCanvas + ProgressText
# ---------------------------------------------------------------------------
canvas_go, canvas_rt, canvas_comp, scaler_comp, ui_comp = alloc_n(5)
text_go, text_rt, text_cr, text_comp, shadow_comp = alloc_n(5)

parts.append(go_block(text_go, "ProgressText", [text_rt, text_cr, text_comp, shadow_comp], layer=5))
parts.append(f"""--- !u!224 &{text_rt}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {text_go}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {canvas_rt}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 1}}
  m_AnchorMax: {{x: 0, y: 1}}
  m_AnchoredPosition: {{x: 48, y: -56}}
  m_SizeDelta: {{x: 360, y: 90}}
  m_Pivot: {{x: 0, y: 1}}
""")
parts.append(f"""--- !u!222 &{text_cr}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {text_go}}}
  m_CullTransparentMesh: 1
""")
parts.append(mono_block(text_comp, text_go, SCRIPT_UI_TEXT, """m_Material: {fileID: 0}
m_Color: {r: 1, g: 1, b: 1, a: 0.85}
m_RaycastTarget: 0
m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
m_Maskable: 1
m_OnCullStateChanged:
  m_PersistentCalls:
    m_Calls: []
m_FontData:
  m_Font: {fileID: 10102, guid: 0000000000000000e000000000000000, type: 0}
  m_FontSize: 46
  m_FontStyle: 0
  m_BestFit: 0
  m_MinSize: 10
  m_MaxSize: 300
  m_Alignment: 0
  m_AlignByGeometry: 0
  m_RichText: 1
  m_HorizontalOverflow: 0
  m_VerticalOverflow: 0
  m_LineSpacing: 1
m_Text: 0 / 6"""))
parts.append(mono_block(shadow_comp, text_go, SCRIPT_UI_SHADOW, """m_EffectColor: {r: 0.15, g: 0.12, b: 0.08, a: 0.55}
m_EffectDistance: {x: 1.5, y: -1.5}
m_UseGraphicAlpha: 1"""))

parts.append(go_block(canvas_go, "DiscoveryCanvas", [canvas_rt, canvas_comp, scaler_comp, ui_comp], layer=5))
parts.append(f"""--- !u!224 &{canvas_rt}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {canvas_go}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: {text_rt}}}
  m_Father: {{fileID: {group_fids["UI"][1]}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 0}}
  m_AnchorMax: {{x: 1, y: 1}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 0, y: 0}}
  m_Pivot: {{x: 0.5, y: 0.5}}
""")
parts.append(f"""--- !u!223 &{canvas_comp}
Canvas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {canvas_go}}}
  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 0
  m_Camera: {{fileID: 0}}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 1
  m_OverrideSorting: 0
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_VertexColorAlwaysGammaSpace: 0
  m_AdditionalShaderChannelsFlag: 25
  m_UpdateRectTransformForStandalone: 0
  m_SortingLayerID: 0
  m_SortingOrder: 0
  m_TargetDisplay: 0
""")
parts.append(mono_block(scaler_comp, canvas_go, SCRIPT_CANVAS_SCALER, """m_UiScaleMode: 1
m_ReferencePixelsPerUnit: 100
m_ScaleFactor: 1
m_ReferenceResolution: {x: 1080, y: 1920}
m_ScreenMatchMode: 0
m_MatchWidthOrHeight: 0.5
m_PhysicalUnit: 3
m_FallbackScreenDPI: 96
m_DefaultSpriteDPI: 96
m_DynamicPixelsPerUnit: 1"""))
parts.append(mono_block(ui_comp, canvas_go, SCRIPT_DISCOVERY_UI, f"""manager: {{fileID: {dm_comp}}}
progressText: {{fileID: {text_comp}}}"""))
group_children["UI"].append(canvas_rt)

# ---------------------------------------------------------------------------
# Emit the group wrapper GameObjects/Transforms (fileIDs were pre-allocated)
# and finally the World root.
# ---------------------------------------------------------------------------
for _name in GROUP_NAMES:
    g_go, g_t = group_fids[_name]
    parts.append(go_block(g_go, _name, [g_t]))
    parts.append(transform_block(g_t, g_go, (0, 0, 0), children=group_children[_name], father=world_t))

world_children = [group_fids[n][1] for n in ["Environment", "Structures", "Props", "Paths", "Discoverables"]]
world_children.append(rig_t)
world_children.append(group_fids["GameSystems"][1])
world_children.append(group_fids["UI"][1])

parts.append(go_block(world_go, "World", [world_t]))
parts.append(transform_block(world_t, world_go, (0, 0, 0), children=world_children, father=0))

header = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {fileID: 0}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 10
  m_Fog: 0
  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {r: 0.65, g: 0.72, b: 0.78, a: 1}
  m_AmbientEquatorColor: {r: 0.55, g: 0.58, b: 0.55, a: 1}
  m_AmbientGroundColor: {r: 0.35, g: 0.32, b: 0.28, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 0
  m_SubtractiveShadowColor: {r: 0.42, g: 0.478, b: 0.627, a: 1}
""" + f"  m_SkyboxMaterial: {{fileID: {MAT_SKYBOX_FID}, guid: {F_GUID}, type: 0}}\n" + """  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {fileID: 0}
""" + f"  m_SpotCookie: {{fileID: {MAT_SPOTCOOKIE_FID}, guid: {E_GUID}, type: 0}}\n" + """  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {fileID: 0}
""" + f"  m_Sun: {{fileID: {light_comp}}}\n" + """  m_UseRadianceAmbientProbe: 0
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 13
  m_BakeOnSceneLoad: 0
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 0
    m_EnableRealtimeLightmaps: 0
  m_LightmapEditorSettings:
    serializedVersion: 12
    m_Resolution: 2
    m_BakeResolution: 40
    m_AtlasSize: 1024
    m_AO: 0
    m_AOMaxDistance: 1
    m_CompAOExponent: 1
    m_CompAOExponentDirect: 0
    m_ExtractAmbientOcclusion: 0
    m_Padding: 2
    m_LightmapParameters: {fileID: 0}
    m_LightmapsBakeMode: 1
    m_TextureCompression: 1
    m_ReflectionCompression: 2
    m_MixedBakeMode: 2
    m_BakeBackend: 1
    m_PVRSampling: 1
    m_PVRDirectSampleCount: 32
    m_PVRSampleCount: 512
    m_PVRBounces: 2
    m_PVREnvironmentSampleCount: 256
    m_PVREnvironmentReferencePointCount: 2048
    m_PVRFilteringMode: 1
    m_PVRDenoiserTypeDirect: 1
    m_PVRDenoiserTypeIndirect: 1
    m_PVRDenoiserTypeAO: 1
    m_PVRFilterTypeDirect: 0
    m_PVRFilterTypeIndirect: 0
    m_PVRFilterTypeAO: 0
    m_PVREnvironmentMIS: 1
    m_PVRCulling: 1
    m_PVRFilteringGaussRadiusDirect: 1
    m_PVRFilteringGaussRadiusIndirect: 5
    m_PVRFilteringGaussRadiusAO: 2
    m_PVRFilteringAtrousPositionSigmaDirect: 0.5
    m_PVRFilteringAtrousPositionSigmaIndirect: 2
    m_PVRFilteringAtrousPositionSigmaAO: 1
    m_ExportTrainingData: 0
    m_TrainingDataDestination: TrainingData
    m_LightProbeSampleCountMultiplier: 4
  m_LightingDataAsset: {fileID: 0}
  m_LightingSettings: {fileID: 0}
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 3
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    buildHeightMesh: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {fileID: 0}
"""

scene_roots_block = f"""--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {{fileID: {world_t}}}
"""

full_text = header + "".join(parts) + scene_roots_block

with open(OUT_PATH, "w") as f:
    f.write(full_text)

print("Wrote", OUT_PATH)
print("Total blocks:", full_text.count("\n--- !u!") + (1 if full_text.startswith("--- !u!") else 0))
print("World root transform fileID:", world_t)
print("Camera rig transform fileID (rig_t):", rig_t)
print("DiscoveryManager component fileID:", dm_comp)
print("6 target discoverable fileIDs:", target_discoverables)
print("Camera start local pos:", (0, round(cam_local_y, 3), round(cam_local_z, 3)))
