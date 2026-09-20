# Architecture

## Project structure

```
Assets/_Project/
  Scenes/
    Bootstrap.unity            M0.1 entry-point scene
    Worlds/
      M02_DioramaPrototype.unity      M0.2 prototype/test scene (unchanged since M0.7)
      Level_01_ForestDiorama.unity    M0.8 first real, designed level
  Scripts/
    Core/         GameBootstrap (target frame rate, entry logging)
    Camera/       CameraInput, DioramaCameraController (see below)
    Characters/   CharacterMover, CharacterPath, CharacterVisual (see below)
    Discovery/    DiscoverySystem, Discoverable, DiscoveryManager,
                  DiscoveryPulseFeedback, CompletionFeedback,
                  FireworkEffect, CompletionCelebration (see below)
    Levels/       LevelDefinition, LevelInfo (see below)
    UI/           DiscoveryUI (see below)
    World/        ProceduralBlobMesh, ProceduralTrunkMesh,
                  OrganicRevolutionMesh, OrganicRockMesh (see below);
                  ProceduralConeMesh and ProceduralClusterMesh kept but
                  unused in the current scene; Interaction/, Learning/,
                  Localization/ still empty placeholders for future work
    Editor/       Editor-only tooling (URP asset bootstrap, shader stripping)
  Data/
    Levels/       LevelDefinition ScriptableObject assets, one per level
  Materials/      Simple URP/Lit materials, flat colors only (M_Moss,
                  M_Water, M_Dirt, M_TerrainSide, M_Flower, M_Mushroom
                  added in M0.10)
  Settings/       URP pipeline/renderer assets
Tests/EditMode/    Structural smoke tests (scene loads, components present)
```

Each scene is self-contained: opening it and pressing Play should work with
no manual setup.

## Camera architecture (M0.2)

```
CameraRig (GameObject, pans in the XZ plane)
  - CameraInput            reads input, exposes ICameraInputSource
  - DioramaCameraController  consumes ICameraInputSource, moves the rig,
                              dollies the camera child
  Main Camera (child, fixed pitch, offset along a dolly axis)
```

- **`ICameraInputSource`** (`PanDelta`, `ZoomDelta`) is the only thing
  `DioramaCameraController` depends on for input. `CameraInput` reads both
  the new Input System's `Mouse` (left-drag → pan, scroll → zoom) and
  `Touchscreen` (one-finger drag → pan, two-finger pinch → zoom) in the
  same `Update()` and combines them into one `PanDelta`/`ZoomDelta` pair,
  so the same component works unchanged in the Editor and on a touch
  device — no swapping, no controller changes.
- **Pan** moves the rig's own `Transform` in the XZ plane, clamped to a
  `Rect` (`panBounds`, Inspector-exposed) so the camera can't leave the
  world.
- **Zoom** dollies the child camera along a fixed offset direction derived
  from `cameraAngle` (distance clamped between `minZoomDistance` and
  `maxZoomDistance`), rather than changing field of view — keeps the
  diorama's perspective consistent at any zoom level and can't clip
  through the terrain or fly off into space.
- Both pan and zoom are smoothed with a simple `Lerp` toward a target
  value (`panSmoothing`, `zoomSmoothing`).
- The camera's fixed pitch (`cameraAngle`, default 50°) intentionally has
  no rotation control yet — out of scope for M0.2.

## Character architecture (M0.3)

```
Character (GameObject, root transform used for navigation)
  - CharacterMover    autonomous waypoint follower, reads CharacterPath
  - CharacterVisual   cosmetic-only, reads CharacterMover.CurrentState
  Model (child, the only transform CharacterVisual ever touches)
    Body, Head        placeholder primitives

CharacterPath (GameObject)
  Waypoint_01 .. Waypoint_05   child transforms = the route, in order
```

- **`CharacterPath`** has no behavior beyond exposing its children's
  positions in order (`WaypointCount`, `GetWaypointPosition`) and drawing
  them as Editor gizmos. Changing the route means adding/removing/moving
  child transforms — no code changes.
- **`CharacterMover`** is a small explicit state machine (`Idle` → `Turning`
  → `Walking` → back to `Idle` at the next waypoint, looping by default).
  It rotates smoothly toward its target with `Quaternion.RotateTowards`
  (never snaps), pauses at each waypoint (`pauseDuration`), and only starts
  walking once it's turned to face the next waypoint within
  `turnThresholdDegrees`. It knows nothing about input, camera, discovery,
  learning, rewards, or saving — it only reads a `CharacterPath` and moves
  its own transform. Not player-controlled, so it behaves identically on
  PC, Android, and iOS. A `SetPath`/`Initialize` pair lets tests drive it
  deterministically outside Play Mode, since Unity doesn't call `Start()`
  in the Editor.
- **`CharacterVisual`** never touches the root transform `CharacterMover`
  navigates with — it only animates a child `Model` transform (vertical
  bob while `Walking`, a slow sway while `Idle`/`Turning`), so a real
  rig/animation controller can replace it later without any change to
  movement logic.
- No prefab was created for the character (see M0.3 report): hand-authoring
  Unity's `PrefabInstance` YAML format outside the Editor carries
  meaningfully more risk than a plain scene `GameObject`, for no behavior
  difference at this stage. Converting it to a prefab in the Editor later
  is a trivial drag-and-drop.

## Discovery architecture (M0.4)

```
Main Camera
  - DiscoverySystem   evaluates range + in-view (+ optional line-of-sight)
                       for a list of Discoverable targets, from this
                       camera's point of view

Character
  - Discoverable      one-shot discovered flag + Discovered event
  - CharacterVisual    (already existed) optionally subscribes to
                       Discovered on the same object -> one-shot scale pulse
```

- **`Discoverable`** is a tiny, self-contained one-shot flag
  (`IsDiscovered`, `Discover()`, a `Discovered` event). It has no reference
  to `DiscoverySystem`, `CharacterMover`, or anything else — a bare
  `GameObject` with only this component works on its own (see
  `M04DiscoveryTests.cs`). `Discover()` is idempotent: calling it again
  after the first time is a no-op and does not re-fire the event.
- **`DiscoverySystem`** holds an explicit, Inspector-assigned list of
  `Discoverable` targets (3, as of M0.5) and a camera reference (defaults
  to the `Camera` on the same object, then `Camera.main`). Each `Update()`
  it checks, per undiscovered target: distance ≤ `discoveryRange`, inside
  the camera's viewport (`WorldToViewportPoint`, expanded by
  `viewportMargin`) and in front of it, and — only if `requireLineOfSight`
  is enabled — an unobstructed `Physics.Linecast` against
  `lineOfSightMask`. It never reaches into `CharacterMover`, and it never
  tracks progress across targets — it only calls `target.Discover()` on
  each one individually; that's `DiscoveryManager`'s job (M0.5, below).
  `discoveryRange` is scene-tuned rather than fixed and has been tightened
  several times during playtesting (`M02_DioramaPrototype.unity` is
  currently at 25, against a `maxZoomDistance` of 95), so discovery
  requires deliberately zooming in from the fully-zoomed-out starting view,
  not just spotting something from far away.
- **Line-of-sight note**: no world geometry in `M02_DioramaPrototype.unity`
  has a `Collider` yet (M0.2 deliberately skipped colliders — nothing to
  occlude against, and none needed for camera-only exploration). With
  `requireLineOfSight` on, the linecast currently always passes trivially.
  It's implemented and wired correctly for when obstacles get colliders
  later; `requireLineOfSight` defaults to **off** so today's behavior
  doesn't silently imply occlusion that isn't actually happening yet.
- **Feedback** lives entirely in `CharacterVisual` (already the sole owner
  of the character's cosmetic animation): it looks up a `Discoverable` on
  the same `GameObject` in `Awake` (nullable — works fine without one) and
  subscribes to `Discovered`; the handler starts a short timer that layers
  a sine-curve scale pulse (`discoveryPulseScale`, ~0.5–1s via
  `discoveryReactionDuration`) on top of whatever bob/sway is already
  running, then it fades back to normal. Neither `Discoverable` nor
  `DiscoverySystem` know this reaction exists.
- **`CanDiscover(Discoverable)`/`SetTargets(...)`/`SetCamera(...)`** are
  public specifically so tests (and later systems) can drive
  `DiscoverySystem` deterministically without waiting on `Update()`, the
  same pattern `CharacterMover.Initialize()` established in M0.3.

## Discovery progress architecture (M0.5)

```
Camera
  - DiscoverySystem     detects individual targets only (unchanged from M0.4)

DiscoveryManager (GameObject)
  - DiscoveryManager     owns global progress across a fixed target list
  - CompletionFeedback   subscribes to DiscoveryManager.OnCompleted

3 Discoverable targets:
  Character   (M0.3 mover, CharacterVisual pulse)
  Character2  (same mover/path/visual components, separate route)
  HiddenGem   (static, DiscoveryPulseFeedback pulse)
```

- **`DiscoveryManager`** is the single owner of cross-target progress
  (`TotalTargets`, `DiscoveredCount`, `IsComplete`, `OnDiscovery`,
  `OnCompleted`). It does this by subscribing to each registered target's
  own `Discovered` event (unchanged from M0.4) — `Discoverable` never
  references `DiscoveryManager` back, so it stays exactly as independent
  and reusable as M0.4 left it. `RegisterTargets(...)` (mirroring
  `CharacterMover.Initialize()`/`DiscoverySystem.SetTargets()`) lets both
  `Awake()` and tests wire it deterministically. Re-discovering a target,
  or the manager holding zero targets, cannot throw or double-count:
  `IsComplete` is `false` whenever `TotalTargets == 0`, and a `HashSet`
  plus a `completedFired` flag make `OnDiscovery`/`OnCompleted` strictly
  one-shot per target/manager.
- **`DiscoverySystem` still only detects** — it has no concept of "global
  progress" and was not changed to track it; that separation is
  deliberate per this milestone's spec.
- **Target 2 (`Character2`)** reuses `CharacterMover`, `CharacterPath`, and
  `CharacterVisual` completely unchanged on a second GameObject with its
  own 4-waypoint `Character2Path` on the opposite side of the diorama —
  no new movement code, per the milestone's explicit instruction not to
  build a second movement system.
- **Target 3 (`HiddenGem`)** is a static prop (no `CharacterMover`) tucked
  next to an existing bush. Its discovery feedback is
  **`DiscoveryPulseFeedback`**, a new minimal component (scale pulse only,
  no bob/sway) so a non-moving target doesn't need `CharacterVisual`'s
  `CharacterMover` dependency — `CharacterVisual` itself is untouched and
  still only used by the two moving targets.
- **`CompletionFeedback`** is the milestone's one required "all 3 found"
  cue: on `DiscoveryManager.OnCompleted` it briefly boosts the scene's
  directional light intensity, then restores it. No UI, no audio, no
  particles, nothing on a per-frame budget beyond a single `Light` field
  write while the pulse is active.
- `DiscoveryManager`'s public progress API
  (`TotalTargets`/`DiscoveredCount`/`IsComplete`/events) exists specifically
  so a UI milestone could read/subscribe to it without any change to this
  layer — M0.6 (below) is that milestone.

## Discovery UI architecture (M0.6, revised M0.7)

```
DiscoveryCanvas (Screen Space - Overlay, CanvasScaler 1080x1920)
  - DiscoveryUI       subscribes to DiscoveryManager.OnDiscovery
  ProgressText        top-left corner, "{DiscoveredCount} / {TotalTargets}"
```

- **`DiscoveryUI`** depends on `DiscoveryManager` in one direction only:
  it holds a reference to read `TotalTargets`/`DiscoveredCount` and
  subscribes to `OnDiscovery`. `DiscoveryManager` has no field, event, or
  method that knows `DiscoveryUI` (or any UI) exists — same shape as
  `CharacterVisual` → `Discoverable` and `DiscoveryPulseFeedback` →
  `Discoverable`. `DiscoveryUI` never calls a mutating method on
  `DiscoveryManager`, only reads it, so gameplay state can't be affected
  by whether a UI is even present. No UI singleton: one `DiscoveryUI` on
  one `Canvas`, wired via a normal Inspector reference, is all this
  milestone needs.
- **Progress** (`ProgressText`) updates the moment `OnDiscovery` fires —
  no animation, no delay, always current. It's plain white digits with a
  soft drop shadow for legibility against the diorama, no background
  panel — reads as *discreet* rather than a HUD.
- **No per-discovery or completion text** — see "First playable / game
  feel (M0.7)" below for why and what replaced it.
- **No victory screen, counter beyond the corner readout, stars, score,
  timer, hints, sound, particles, or localization system** — explicitly
  out of scope.

## First playable / game feel (M0.7)

```
DiscoveryManager
  - State (SessionState: Playing | Completed)   computed from IsComplete

FireworkEffect (GameObject, persistent, never instantiated/destroyed)
  - FireworkEffect    subscribes to DiscoveryManager.OnDiscovery
  8x Spark_XX          pre-placed child Transforms, scaled to 0 at rest
```

- **`DiscoveryManager.SessionState`** (`Playing`/`Completed`) is a
  read-only computed property (`IsComplete ? Completed : Playing`) over
  state `DiscoveryManager` has tracked since M0.5 — not a new state
  framework, no field added, nothing else needed to observe it beyond
  reading the property or the existing `OnCompleted` event it's derived
  from. Because `IsComplete`/`completedFired` were already strictly
  one-shot (M0.5), `State` flipping to `Completed` and staying there, and
  further discoveries after completion not changing anything, both fall
  out of the existing guarantees rather than needing new code.
- **`FireworkEffect`** replaced M0.6's per-discovery/completion text
  (`ConfirmationText`) after device testing showed the text competing
  with the diorama for attention and needing future translation for no
  real benefit. It's a single rig of 8 primitive "spark" child
  `Transform`s, placed once in the scene and never instantiated or
  destroyed at runtime — `Play(position)` just repositions the rig and
  resets a timer; `Update()` expands and fades all 8 sparks along one
  shared sine curve read from that timer. No `Instantiate`/`Destroy`
  calls, no per-frame allocation, no particle system — satisfies the
  milestone's performance constraints while giving a clearer, faster,
  translation-free acknowledgment than the text it replaced. It
  subscribes to `DiscoveryManager.OnDiscovery` the same one-directional
  way `DiscoveryUI` does; `DiscoveryManager` has no knowledge it exists.
- **Progress counter moved top-left.** On the test device (Galaxy S10e)
  the front-camera cutout sits top-right, obstructing the M0.6 placement.
  `ProgressText`'s anchors/pivot were flipped to top-left; no other
  behavior changed. It was already background-free (`Text` + `Shadow`
  only), so the "no purple/no background, just the numbers" request was
  a rendering fix (below), not a layout one.
- **Purple/magenta UI fix**: `ProjectSettings/GraphicsSettings.asset` had
  an empty `m_AlwaysIncludedShaders` list. Legacy `Text` (and its
  `Shadow`) use the `UI/Default` shader implicitly (`m_Material:
  {fileID: 0}`), with no on-disk `Material` asset anywhere in the project
  referencing it — a known way for a shader to get stripped from a build
  and fall back to Unity's solid-magenta "missing shader" rendering.
  Fixed by explicitly listing the built-in `UI/Default` shader
  (`{fileID: 10770, guid: 0000000000000000f000000000000000, type: 0}`)
  in `m_AlwaysIncludedShaders`, guaranteeing it's always included.
- **Game feel review**: character speed/pause/turn timing, discovery
  range, feedback timing, and target placement (from M0.3–M0.5) were
  re-checked against this milestone's checklist and left unchanged —
  they were already tuned through several rounds of direct device
  feedback (see M0.4's roadmap entry) and still hold up as one coherent
  session. The camera (M0.2) was reviewed only for fit with the above and
  was not redesigned.
- **No player movement, joystick, combat, inventory, hints, timer,
  score, stars, lives, monetization, ads, sound/music, save data,
  multiple levels, level select, localization, analytics, or backend
  integration** — explicitly out of scope for this milestone.

## First real level (M0.8)

```
Assets/_Project/Scenes/Worlds/Level_01_ForestDiorama.unity

World
  Environment     ground, hill, Directional Light
  Structures      House
  Props           13 trees, 4 bushes, 6 rocks, 7 path stones, 1 stump
  Paths           Target_04_Path, Target_06_Path (CharacterPath waypoint routes)
  Discoverables   Target_01 .. Target_06
  Camera          CameraInput, DioramaCameraController, Main Camera
                   (DiscoverySystem, CompletionCelebration rig)
  GameSystems     GameBootstrap, DiscoveryManager, CompletionFeedback,
                   FireworkEffect rig, LevelInfo
  UI              DiscoveryCanvas, ProgressText
```

- **`M02_DioramaPrototype.unity` was left untouched** and remains the
  prototype/test scene (the milestone's option B) — every object, GUID,
  and tuned value from M0.1–M0.7 in it is exactly as M0.7 left it.
  `Level_01_ForestDiorama.unity` is a new, independent scene built by
  reusing the same verified mesh/material/script GUIDs (trees, rocks,
  bushes, the house, the character rig, the camera rig, the UI, the
  firework rig), not by copying and editing M02's file in place — that
  keeps both scenes internally consistent without one's edits risking the
  other.
- **Every system on this scene is the exact same component as M0.1–M0.7**,
  wired the same one-directional way: `DiscoverySystem` (on `Main Camera`)
  still only detects; `DiscoveryManager` still only tracks progress by
  subscribing to each target's `Discovered` event; `Discoverable`,
  `DiscoveryPulseFeedback`, `CharacterMover`/`CharacterPath`/
  `CharacterVisual`, `FireworkEffect`, and `DiscoveryUI` are all
  unmodified. Nothing new was built for movement, detection, feedback, or
  UI — M0.8 is a level-design milestone, not a systems milestone.
- **6 `Discoverable` targets**, distributed per the milestone brief (2
  moving, 2 partially hidden static, 1 easy, 1 requiring closer look):
  - `Target_01` — static, near the entrance path; deliberately the easiest
    find, so a first-time player confirms quickly that things here *can*
    be found.
  - `Target_02` — static, tucked just behind the house's back-left corner;
    screened from the starting camera position by the house itself, only
    resolved once the camera pans to open a sightline past it.
  - `Target_03` — static, nestled against `Bush_01` at the clearing's
    left edge; partially occluded by the bush's silhouette rather than
    hidden outright.
  - `Target_04` — moving (`CharacterMover` + its own `Target_04_Path`),
    weaving through the denser right-hand tree stand; found by noticing
    motion between the trunks, not by spotting a static shape.
  - `Target_05` — static, smaller-scaled and set into the rock/hill nook
    at the back-left; the milestone's "requires careful observation"
    target — found by zooming into that specific pocket of the diorama,
    not by anything being invisible.
  - `Target_06` — moving (`CharacterMover` + `Target_06_Path`), patrolling
    the left-hand tree stand on a different route/pace than `Target_04` so
    the two moving targets don't read as the same thing twice.
  - All 6 use the same visual language the player learns from `Target_01`
    (a small gold sphere for static targets, the established character rig
    for moving ones) — difficulty comes from placement and occlusion, not
    from disguising what a target looks like.
- **Starting camera** (widened in M0.9 — see below): the rig starts offset
  toward the entrance (`(0, 0, -6)`) at a zoom distance of 52, between
  `minZoomDistance` (6, unchanged) and `maxZoomDistance` (65, widened in
  M0.9) — enough to read the whole diorama as one small world at a glance,
  not enough to resolve `Target_02`, `Target_04`, `Target_05`, or
  `Target_06` from that view alone. `panBounds` was shrunk to match this
  smaller, ~26×26 world (down from M02's ~45×45); `discoveryRange` was
  retuned to 10 for the same reason `DiscoverySystem`'s range has always
  been scene-tuned rather than fixed (M0.4). `cameraAngle` (50°), pan/zoom
  speed and smoothing are untouched from M0.2 — the camera itself was
  reviewed only for fit, not redesigned, per this milestone's explicit
  scope.
- **`LevelDefinition`** (`Scripts/Levels/LevelDefinition.cs`) is a small
  `ScriptableObject` — `levelId`, `displayName`, `expectedTargetCount`,
  and a computed `IsValid` — with one asset,
  `Data/Levels/Level_01_ForestDiorama.asset`, for this level.
  **`LevelInfo`** (`Scripts/Levels/LevelInfo.cs`) sits in `GameSystems`,
  holding a reference to that asset and to the scene's `DiscoveryManager`,
  and exposes `MatchesExpectedTargetCount`. Neither type owns, duplicates,
  or reaches into discovery logic — `DiscoveryManager` remains the single
  owner of progress, exactly as after M0.5; `LevelInfo` only *reads*
  `DiscoveryManager.TotalTargets` to flag a mismatch. This is the entire
  "level data" layer this milestone introduces — no level list, no level
  loader, no generalized pipeline.
- **Mobile/performance**: the scene has 126 `GameObject`s total (vs.
  M02's much larger prototype footprint), no new shaders, no textures, no
  particle systems, and no new physics — `DiscoverySystem`'s existing
  range/viewport checks are the only per-frame discovery cost, unchanged
  from M0.4.

## Polish: camera framing & completion celebration (M0.9)

Applied to `Level_01_ForestDiorama.unity` only — `M02_DioramaPrototype.unity`
was left untouched per explicit direction this round.

- **Wider starting camera**: `DioramaCameraController.maxZoomDistance` went
  from 40 to 65 and the scene's starting zoom distance from 24 to 52 (80%
  of the new max) — the Main Camera child's initial local position/rotation
  were recomputed from the same `cameraAngle`/distance trigonometry M0.8
  used, nothing structural changed. `minZoomDistance` (6) is untouched, so
  zooming in close enough to inspect a target is unaffected. `panBounds`,
  `cameraAngle`, `panSpeed`/`panSmoothing`, and `zoomSpeed`/`zoomSmoothing`
  are all untouched — only the two distance limits and the starting
  distance changed, per this milestone's "adjust defaults/limits, don't
  redesign" scope. `DioramaBase`'s scale grew from 41 to 68, proportional
  to the zoom increase, purely so the ground still fills the frame at the
  new starting distance instead of showing the flat background color past
  its edge.
- **`CompletionCelebration`** (`Scripts/Discovery/CompletionCelebration.cs`)
  is architecturally a sibling of `FireworkEffect`: a persistent rig of
  pre-placed spark `Transform`s (16, vs. `FireworkEffect`'s 8), animated by
  the same shape of sine-curve expand-then-fade in `Update()`, no
  `Instantiate`/`Destroy`, no per-frame allocation. The one structural
  difference is where the rig lives: `FireworkEffect`'s rig sits under
  `GameSystems` and is repositioned to a world point per discovery;
  `CompletionCelebration`'s rig is parented directly to `Main Camera`'s
  `Transform` at local `(0, 0, 10)` (10 units along the camera's own
  forward axis) and never repositioned, so it rides along with every pan
  and zoom automatically — no camera-tracking code needed — and always
  reads as centered in view. `burstDuration` (1.1s vs. 0.6s) and
  `burstRadius` (3.5 vs. 0.9) are both larger, and a `horizontalSpread`
  factor (0.6) keeps the spark pattern proportioned to the portrait screen
  rather than a perfect circle. Because the sparks are small spheres, not
  a full-screen quad, the diorama stays visible through and around the
  burst rather than being covered by it.
- **Wiring**: `CompletionCelebration` subscribes to
  `DiscoveryManager.OnCompleted` the same one-directional way
  `FireworkEffect` subscribes to `OnDiscovery` — `DiscoveryManager` still
  has no reference to either. `DiscoveryManager.OnCompleted` already fires
  at most once per manager (M0.5's `completedFired` flag);
  `CompletionCelebration.Play()` adds a second, independent `HasPlayed`
  guard so the celebration itself cannot restart even if `Play()` were
  ever called more than once for any other reason. `CompletionFeedback`
  (the M0.5 light-pulse cue) is untouched and still runs alongside it —
  this milestone added a second completion cue, it didn't replace the
  first one.
- **No new screens or systems**: no "LEVEL COMPLETED" screen, no buttons,
  no menus, no grayscale transition, no scoring — explicitly out of scope
  for this milestone.

## Visual vertical slice (M0.10)

Requested as "M0.9 — Visual Vertical Slice"; numbered M0.10 here since M0.9
was already the camera/completion-celebration polish above (the request's
"M0.8.1"). Purely a visual pass on `Level_01_ForestDiorama.unity` —
`M02_DioramaPrototype.unity` untouched, discovery/camera/UI systems
untouched. This one milestone covers several rounds of the same visual
pass (the procedural mesh generators, a world-expansion round, then a
tree-shape revision) rather than incrementing per round, per explicit
direction partway through.

```
Assets/_Project/Scripts/World/ProceduralBlobMesh.cs
Assets/_Project/Scripts/World/ProceduralConeMesh.cs
```

- **`ProceduralBlobMesh`** is the piece of code driving most of this
  milestone's visual change: a `[RequireComponent(typeof(MeshFilter))]`
  `MonoBehaviour` that builds a small flat-shaded "blob" mesh — a unit
  icosahedron (12 vertices, 20 faces) with each of its 12 vertices jittered
  by a seeded `System.Random` and squashed vertically by a configurable
  factor — once in `Awake()`, and assigns it to the sibling `MeshFilter`'s
  `sharedMesh`. Faces are rendered unshared (60 vertices for 20 triangles)
  with per-face flat normals, so the shape reads as faceted/low-poly rather
  than smoothly rounded, without any custom shader — the existing flat-
  color URP/Lit materials render it correctly as-is. Because there's no
  Unity Editor available to check the icosahedron reference face list's
  winding visually, each face's normal is verified against its own
  geometric center (must point outward) and the triangle's winding is
  flipped if it doesn't — this makes the shape correct regardless of which
  winding convention the source face list happened to use, rather than
  relying on getting Unity's convention right from memory. Drives every
  rock (jitter 0.34, most angular), bush (jitter 0.22), grass tuft (jitter
  0.28, small and flat), the mountain's peak and base rocks (see below),
  and the boat's hull — different `seed`/`jitter`/`verticalSquash`/scale
  per instance, same component.
- **`ProceduralConeMesh`** is a second, equally small generator added
  after seeing the blob-canopy trees next to a reference image of a
  low-poly diorama — a round blob read as too soft for a conifer. It's a
  flat-shaded triangle fan from a fixed apex down to a gently jittered
  base rim (one triangle per side, `sides` configurable, no base cap since
  a trunk always sits under it), same "build once in `Awake()`, self-
  correct winding from geometry" approach as the blob generator. Most tree
  canopies use it (two pine "species" — taller/narrower vs. shorter/
  fuller); roughly a third of trees now cycle to a third species that
  reuses `ProceduralBlobMesh` instead for a rounder "deciduous" canopy
  (`emit_tree`'s `species` parameter, `species % 3` at each call site), so
  a cluster reads as mixed forest rather than one repeated silhouette.
  Rocks/bushes/grass/mountain/boat/character-heads (see below) stay on the
  rounder blob shape, which reads better for those. `Discoverable` targets
  deliberately keep their
  plain, unjittered sphere mesh (`M_Hidden`, gold), used nowhere else —
  the contrast between "perfect geometric shape" (something to find) and
  "organic faceted prop" (environment) is a readability choice, not an
  oversight; `Level01VisualSliceSceneTests` in
  `Tests/EditMode/M09VisualSliceTests.cs` asserts a target's own mesh
  never has a `ProceduralBlobMesh` on it.
- **House**: the M0.2/M0.8 wall-box-plus-diamond-roof gained a `Chimney`
  (a small offset box, `M_Roof`), a `Door` (a thin box, `M_Bark`), and a
  `Window` (a thin box, `M_CharacterHead`'s warm cream tone) — all static
  primitive children of the same house root, no new mesh work.
- **Characters**: `Target_04`/`Target_06`'s `Model` hierarchy (from M0.3)
  gained `ArmLeft`/`ArmRight` (small angled capsules) and a `Backpack`
  (a small box) alongside the existing `Body`/`Head`. These are static
  children of `Model`, the same transform `CharacterVisual` already
  bobs/sways as one piece — `CharacterMover`, `CharacterPath`, and
  `CharacterVisual` were not touched, so the existing procedural
  walk/idle animation now moves a richer silhouette for free. `Head` later
  switched from a plain `MESH_SPHERE` to a low-jitter (0.12)
  `ProceduralBlobMesh` — `verticalSquash` kept near 1 (0.9) so it still
  clearly reads as a head, with only subtle facets — matching the
  reference image's "faceted geometry" language for characters.
  `Body`/`ArmLeft`/`ArmRight` stay on plain capsules; only the head
  changed.
- **Ground variation**: `DioramaBase` switched from `M_Terrain` to the
  previously-unused `M_Ground` (a richer grass green), while `Hill_01`
  keeps `M_Terrain`, giving the flat ground and the hill distinct tones. A
  material, `M_Moss` (a cooler, deeper green), is used once for
  `MossyHollow` — a shallow, flattened disc sitting just below the main
  ground's surface near the hill/rock nook — a cheap primitive-only way to
  read as a recessed pocket without any terrain deformation. A `FallenLog`
  (rotated cylinder) and a `Crate` (small cube) add minor prop variety
  near the clearing.
- **Thick terrain block**: to match the reference image's visible-sides
  diorama block, the ground is now two stacked `MESH_CUBE`s instead of one
  flat slab — a new, tall `TerrainBase` (new `M_TerrainSide` material, warm
  tan/gold) at `(0, -2.75, 0)` scale `(106, 3.5, 106)`, with the original
  `DioramaBase` green slab sitting directly on top at its exact original
  position/scale. `DioramaBase`'s top surface is unchanged at world y=0 —
  the invariant every prop in the scene assumes — so this is purely
  additive underneath it. A `MountainTerrace` (`MESH_CUBE`, `M_Dirt`, a
  flat dirt-toned step near the mountain's base) gives a second, layered
  elevation change, entirely in the target-free margin. `M_Rock` and
  `M_Ground` were also both warmed/saturated slightly to match the
  reference's palette (mauve-brown rock, more vivid green ground).
- **Map enlarged ~50%**, again directly against the reference image:
  `PAN_HALF` 13→20 (`panBounds` now 40×40), `DioramaBase` scale 68→106,
  `maxZoomDistance` 65→98, starting zoom distance 52→78 — the same ratios
  M0.9 established, scaled up together rather than redesigned.
  `minZoomDistance` (6) untouched.
- **`Mountain`** ("a un costado" per the request) reuses `ProceduralBlobMesh`
  at larger parameters than `Hill_01` (M0.8, untouched): one tall,
  low-jitter (0.22) "Peak" blob at radius 6.5, plus four smaller, more
  angular (jitter 0.3) "BaseRock" blobs clustered at its foot. Placed on
  the right side (`(17, 0, 6)`), beyond the existing right tree cluster.
- **`Waterfall`** is one tilted `MESH_CUBE` slab (`M_Water`) leaned
  against the mountain's near face — no particle system, no shader
  effect, just a static colored slab, consistent with the project's
  "simple shaders" mobile constraint.
- **The river** is built by a small helper, `emit_ribbon_segment` (plus
  `emit_water_path`/`emit_dirt_road` wrappers around it): given two XZ
  points, it computes the straight-line distance and Y-axis rotation
  between them and emits one flattened, rotated `MESH_CUBE` spanning that
  distance — a simple chained polyline, not a spline or custom mesh. The
  river chains 8 waypoints from the waterfall's pool, sweeping across the
  expanded southern margin (all at `z <= -3`, below the original
  clearing's `z = -9` southern edge) to the dock. The same helper, with a
  wider/flatter profile and `M_Dirt` instead of `M_Water`, builds the two
  dirt roads — visually distinct from the existing grey-tan stone
  `PathStone` walkway near the house.
- **`Dock`** (a plank box plus two post cylinders, `M_Trunk`) and
  **`Boat`** (a `ProceduralBlobMesh` hull with strong non-uniform
  transform scale for a simple elongated hull shape, plus a small mast)
  sit together in the front-left corner where the river ends.
- **Density**: 23 trees total (13 original + 10 added; 15
  `ProceduralConeMesh` pine canopies, 8 `ProceduralBlobMesh` deciduous
  canopies, cycling `species % 3` per tree), 6 bare/leafless trees for
  variety
  (`emit_bare_tree` — a trunk plus two thin angled branch capsules, no
  canopy), 14 rocks, 9 bushes, 20 grass tufts, all placed in the expanded
  margin rather than packed into the existing core.
- **Discoverable placement is unchanged throughout**: all 6 targets keep
  their exact M0.8 positions and design rationale (easy/angle-dependent/
  occluded/moving/careful-observation/moving), re-verified byte-identical
  after every scene regeneration — the richer surrounding geometry
  reinforces the same hiding logic rather than replacing it.
- **Mobile/performance**: each blob or cone is roughly 20/`sides`
  triangles built once at startup, never rebuilt. No textures, no custom
  shaders, no new physics/colliders, no `Update()`-driven work, no
  `Instantiate`/`Destroy` calls anywhere in the new code. Scene
  `GameObject` count grew from 126 to 256 across all rounds (`TerrainBase`
  and `MountainTerrace` added two; the tree-species mix and faceted heads
  changed component wiring, not object count) — still well below M02's
  original prototype footprint.

### Visual correction pass: organic geometry + environmental density

A follow-up round on the same M0.10 milestone, triggered by explicit
feedback that the result above still read as "primitive shapes" (cones as
trees, single blobs as rocks/bushes) rather than a handcrafted diorama,
and that the world was too sparse and too centered. Two new generators
replace primitives as the *final* visible geometry for trees/bushes/
rocks; primitives now only ever appear as minor accents (a branch
capsule, a fallen log) on top of already-organic shapes.

```
Assets/_Project/Scripts/World/ProceduralClusterMesh.cs
Assets/_Project/Scripts/World/ProceduralTrunkMesh.cs
```

- **`ProceduralClusterMesh`** merges several jittered-icosahedron lobes
  (the same per-lobe shape `ProceduralBlobMesh` uses) into ONE mesh, at
  randomized per-lobe offset/radius/height. Three parameter presets, one
  component: a tree **canopy** (`emit_cluster_prop` inside `emit_tree`,
  4–5 lobes biased to stack upward for layered depth), a **bush**
  (`emit_bush_cluster`, 4 lobes spread low and wide, little vertical
  stacking), and a **rock formation** (`emit_rock_formation`, 3 lobes,
  higher jitter, minimal vertical stacking so lobes sit side by side).
  Winding is self-corrected per lobe in the lobe's own local space (the
  same "outward from this shape's own center" check `ProceduralBlobMesh`
  uses), not the cluster's combined space, since a lobe far from the
  cluster's overall center would otherwise get the wrong answer. Still
  cheap: a handful of 20-triangle lobes merged into one draw call, built
  once in `Awake()`.
- **`ProceduralTrunkMesh`** replaces the plain `MESH_CYLINDER` trunk
  (every tree, plus `emit_bare_tree`) with a small tapered tube that
  leans slightly to one side (strongest partway up, eased with a sine)
  and has a jittered, non-circular cross-section — taper + lean +
  irregularity being the cheapest cues that read as "trunk" rather than
  "cylinder." Capped on top so it isn't hollow from a low camera angle.
  Larger/pine-species trees also get one small branch capsule breaking
  the trunk/canopy line (still a primitive, but only ever an accent on
  an already-organic trunk+canopy, never the tree's own silhouette).
- **`ProceduralConeMesh` is no longer used anywhere in the scene** as of
  this pass — replaced by `ProceduralClusterMesh` canopies per the
  explicit "do not use cones as trees" direction. The script and its
  tests are kept (still correct, still potentially useful for some other
  conical shape later) but nothing currently references it.
- **The mountain's `Peak`** switched from a single large blob to a
  6-lobe `ProceduralClusterMesh` mass; its `BaseRock` accents switched
  from single blobs to `emit_rock_formation` calls, same as every other
  named rock.
- **Terrain elevation variation**: 6 low, wide, heavily flattened
  `ProceduralClusterMesh` "knolls" (`TerrainKnoll_01..06`) scattered
  across open ground as gentle rises, plus 4 more recessed
  `GroundHollow_` patches (mossy/dirt, same flattened-cylinder technique
  as the original `MossyHollow`) and 2 `GroundPatch_` ground-texture
  accents — all shallow enough to never disturb the y=0 surface every
  prop's position assumes.
- **Dense environmental scatter** (`Assets/_Project/Scripts/World/` — no
  new script, this is generator logic in `gen_level01.py`): a
  deterministic (fixed-seed `random.Random`) rejection-sampling pass
  fills the rest of the ~40×40 playable footprint — edges, corners,
  riverbanks, road/path edges, the rocky mountain base — instead of
  leaving everything clustered near the center. Exclusion checks keep
  every new point clear of the 6 targets (1.4 units), the river/road/
  path centerlines (1.3–2.2 units), and (for the medium/large tier only)
  anything already placed, tracked in a single `PLACED_FOOTPRINTS`
  list of `(x, z, radius)` every hand-placed and scattered object
  registers itself into as it's emitted. Small-tier detail (pebbles,
  grass, flower clusters) is deliberately allowed to sit close to or
  under bigger elements — that's how real ground detail actually
  clusters — so it skips the footprint check.
  - Medium/large tier: 14 `ScatterTree_`, 20 `ScatterBush_`, 16
    `ScatterRock_`, 10 `ScatterBranch_` (fallen logs, two simple
    90°-lay orientations, no quaternion composition needed).
  - Small tier: 45 `ScatterPebble_` (`emit_pebble`, a single tiny blob —
    appropriately simple at stone scale), 40 `ScatterGrass_` (unchanged
    single-blob technique), 18 `ScatterFlowers_` (`emit_flower_cluster`,
    two tiny lobes in a new accent material, `M_Flower`, kept visually
    distinct from every green in the palette so a bloom doesn't read as
    another leaf).
  - Combined with the hand-placed content, the scene now has 43 tree
    trunks (37 canopied + 6 bare), 37 canopies, 30 named rock
    formations, 29 bush clusters, and well over 100 small-tier ground
    details — `GameObject` count grew from 256 to 498.
- **Discoverable placement is still completely unchanged**: all 6 target
  positions re-verified byte-identical to M0.8 after this regeneration
  too; discovery uses distance + viewport visibility only
  (`requireLineOfSight: 0`), so denser surrounding geometry changes what
  a target looks hidden behind, never whether it's mechanically
  discoverable.
- **Mobile/performance**: every new mesh is still built once in
  `Awake()` from a handful of merged 20-triangle lobes or a small tapered
  tube (roughly 50–150 triangles per compound object) — no textures, no
  new shaders, no per-frame cost. Total scene triangle count is in the
  low tens of thousands (dominated by Unity's own built-in sphere
  primitive on the 6 targets and the 24 firework/celebration spark
  spheres, unchanged from earlier milestones, not by anything new here).
  `GameObject` count (498) is still modest for a static, non-physics
  mobile scene.

### Round 2: smooth organic meshes (correcting the correction)

Further explicit feedback: the round above still read as "icospheres,
triangulated blobs, cones, angular rocks" — a collection of visible
triangles, not a handcrafted diorama. The root cause was **flat shading**
(unshared vertices, one hard normal per face) combined with **high-
frequency per-vertex jitter**: every triangle in `ProceduralClusterMesh`
was its own visibly distinct facet, and randomizing each one independently
produced a "broken glass"/"gem cluster" look rather than a rounded organic
form. `ProceduralClusterMesh` is now unused by the scene (0 instances),
same treatment as `ProceduralConeMesh` before it — kept, still correct,
still unit-tested, but superseded.

```
Assets/_Project/Scripts/World/OrganicRevolutionMesh.cs
Assets/_Project/Scripts/World/OrganicRockMesh.cs
```

- **`OrganicRevolutionMesh`** builds ONE smooth, shared-vertex mesh by
  revolving a hand-authored profile curve (parallel `profileHeights`/
  `profileRadii` float arrays, serialized as ordinary YAML lists) around
  the Y axis, sharing vertices between adjacent faces and shading via
  `Mesh.RecalculateNormals()` — the single highest-leverage fix, since
  smooth normals alone make even a 10-sided low-poly shape read as
  rounded rather than faceted. Asymmetry comes from one low-frequency
  lean/bulge direction for the whole shape (same idea as
  `ProceduralTrunkMesh`'s lean), eased to zero at the poles, never
  per-vertex noise. Winding is corrected once globally via a majority
  vote across all faces (a revolved shape's topology is consistent
  throughout, so either every face is backwards or none are — summing
  avoids picking one possibly-degenerate pole triangle as the sole
  sample). Three hand-authored profile presets in `gen_level01.py`
  (`CANOPY_VARIANTS`) give three tree-canopy silhouettes (tall/narrow,
  fuller/shorter, rounder/deciduous); two more (`BUSH_VARIANTS`) build
  bush clumps; tiny ad hoc profiles build mushroom caps and flower
  blooms. Reused across every placement via `emit_revolution_prop` — the
  same small set of profiles drives every canopy/clump/cap/bloom in the
  scene, not a structurally unique mesh per instance.
- **`OrganicRockMesh`** builds a one-subdivision icosphere (42 vertices,
  80 triangles, shared vertices throughout — unlike `ProceduralBlobMesh`'s
  unshared 20-triangle blob) and displaces it with a handful (3–6) of
  large, low-frequency "bumps": each bump pushes vertices near one random
  direction outward (occasionally a milder inward dent) with a smooth
  angular falloff, never per-vertex noise. Final radius is clamped well
  above zero so stacked negative bumps can never invert the local
  surface. Winding is corrected on the base 20 icosahedron faces *before*
  subdivision (subdivision preserves a parent face's winding in all 4 of
  its children, so this is sufficient for the whole sphere) — the same
  proven per-face "outward from center" check `ProceduralBlobMesh` uses.
  Shaded via `RecalculateNormals()`. Three reused bump/proportion presets
  (`ROCK_VARIANTS` in `gen_level01.py`, each a `(bumpCount, bumpStrength,
  bumpSharpness, non-uniform-scale-shape)` tuple) drive every rock, pebble,
  terrain knoll, and the mountain's peak/base rocks — variation comes from
  the caller's own position/rotation/scale, not from generating a new
  mesh shape per instance.
- **`ProceduralTrunkMesh` rewritten for smooth shading**: same public
  `Build()` signature and triangle count as before, but vertices are now
  shared between adjacent rings/sides (indexed once via a `[ringCount,
  sides]` array rather than duplicated per triangle) and shading comes
  from `RecalculateNormals()`. Side-tube and top-cap winding are corrected
  *independently* (radially-outward vs. straight-up are genuinely
  different "correct" directions — a single combined vote could satisfy
  one and silently get the other backwards).
- **Trees**: `emit_tree()` now builds each canopy from one of the three
  `CANOPY_VARIANTS` profiles via `OrganicRevolutionMesh` (species 0/1
  still the two pine silhouettes, species 2 the rounder deciduous one);
  the trunk is unchanged (`ProceduralTrunkMesh`, now smooth). The
  branch-capsule accent on pine species is unchanged.
- **Bushes**: `emit_bush()` (was `emit_bush_cluster`) builds two
  overlapping `OrganicRevolutionMesh` "Clump" children at a small offset
  from `BUSH_VARIANTS` — "several overlapping volumes," never a single
  spherical primitive standing in for the whole plant.
- **Rocks**: `emit_rock_formation()` now builds ONE `OrganicRockMesh`
  per named rock (was several merged `ProceduralClusterMesh` lobes);
  `emit_pebble()` reuses the same technique at loose-stone scale.
- **Small ground-detail variety**: `emit_flower_cluster()` (a thin stem
  primitive + a tiny `OrganicRevolutionMesh` bloom in the new `M_Flower`
  accent color) and the new `emit_mushroom()` (a thin stem + a tiny
  flattened-dome `OrganicRevolutionMesh` cap in the new `M_Mushroom`
  accent color) — both named small-vegetation variants the reference
  brief calls out explicitly. `emit_log()` (fallen logs/branches) now
  reuses the smooth `ProceduralTrunkMesh` laid on its side instead of a
  plain cylinder.
- **Terrain knolls and the mountain's `Peak`/`BaseRock`s** all switched
  from `ProceduralClusterMesh` to `OrganicRockMesh` (knolls heavily
  flattened via transform scale).
- **Mobile/performance**: per-object triangle budgets stayed modest —
  a canopy/clump/cap/bloom is one revolved mesh (roughly 60–140
  triangles depending on ring/side count), a rock is a fixed 80-triangle
  icosphere regardless of bump parameters (bumps only displace existing
  vertices, never add geometry). `GameObject` count grew from 498 to 628
  (mushrooms/extra small detail added, bushes gained a second clump
  child each). No new shaders; two new flat-color materials (`M_Flower`,
  `M_Mushroom`) reusing the existing URP/Lit template.
- **Discoverable placement remains completely unchanged**: all 6 target
  positions re-verified byte-identical to M0.8 after this regeneration
  too.

### Round 3: targeted fixes from a real screenshot comparison

The user compared an actual gameplay screenshot against the reference
image directly and flagged three concrete, specific problems — the first
time in this project's history that real rendered output (not just
generated YAML) could be checked against intent:

- **Canopies rounded off into a ball/pom-pom instead of a point.** The
  `CANOPY_VARIANTS` profiles tapered too gradually near the top (e.g. a
  0.4→0.58→0 radius change spread across 1.5 world units of height), and
  with smooth shading a gradual taper reads as rounded, not pointed. All
  three profiles were rewritten with a short, steep final segment (the
  last ring's radius drops to 0 over a small height delta, e.g. 0.08→0
  over 0.15 units) so the silhouette comes to an actual spike. The third
  profile, previously a rounder "deciduous" shape the reference doesn't
  actually show, became a third pine-pointed size variant instead.
- **The river and dirt roads read as "rectangular pieces placed on the
  floor"** — literally true: each was a chain of straight flat boxes
  meeting at sharp mitered corners. `gen_level01.py` gained
  `_resample_smooth()`, a Catmull-Rom spline interpolation of the
  original waypoint list (4 interpolated segments per original gap), so
  `emit_water_path`/`emit_dirt_road` now build many short segments that
  approximate a continuous curve instead of a few long straight ones.
  River: 8 waypoints → 28 segments (was 7). Each road: ~4 waypoints → 16
  segments (was 4).
- **`emit_house()` rebuilt.** The single diamond-rotated roof box is
  replaced with two thin roof slabs meeting at a real ridge line (each
  slab's pitch and mirrored rotation derived from actual eave/ridge
  coordinates, not eyeballed) plus a ridge cap covering the seam, and a
  stone-colored (`M_Rock`) foundation course under the walls so the
  house reads as sitting on/in the ground rather than floating on it.

## Input System

`Packages/manifest.json` already includes `com.unity.inputsystem`, and
`ProjectSettings/ProjectSettings.asset` has `activeInputHandler: 1` (Input
System Package only). `Hidden.Runtime.asmdef` references
`Unity.InputSystem` so `CameraInput` can use `UnityEngine.InputSystem`
APIs directly.

## Rendering

URP 17.6.0, one `UniversalRenderPipelineAsset` (`URP-Mobile`, created and
committed from the real Unity Editor — see `M0.1` history for why a
script-generated one was not reliable on device). Materials are flat-color
`Universal Render Pipeline/Lit`, no textures, kept mobile-lightweight.
