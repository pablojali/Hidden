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
    World/, Interaction/, Learning/, Localization/
                  empty placeholders for future milestones
    Editor/       Editor-only tooling (URP asset bootstrap, shader stripping)
  Data/
    Levels/       LevelDefinition ScriptableObject assets, one per level
  Materials/      Simple URP/Lit materials, flat colors only
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
