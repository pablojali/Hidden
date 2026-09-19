# Architecture

## Project structure

```
Assets/_Project/
  Scenes/
    Bootstrap.unity            M0.1 entry-point scene
    Worlds/
      M02_DioramaPrototype.unity   M0.2 diorama prototype scene
  Scripts/
    Core/         GameBootstrap (target frame rate, entry logging)
    Camera/       CameraInput, DioramaCameraController (see below)
    Characters/   CharacterMover, CharacterPath, CharacterVisual (see below)
    Discovery/    DiscoverySystem, Discoverable (see below)
    World/, Interaction/, Learning/, Localization/, UI/
                  empty placeholders for future milestones
    Editor/       Editor-only tooling (URP asset bootstrap, shader stripping)
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
  `Discoverable` targets (one, today: the `Character`) and a camera
  reference (defaults to the `Camera` on the same object, then
  `Camera.main`). Each `Update()` it checks, per undiscovered target:
  distance ≤ `discoveryRange`, inside the camera's viewport
  (`WorldToViewportPoint`, expanded by `viewportMargin`) and in front of
  it, and — only if `requireLineOfSight` is enabled — an unobstructed
  `Physics.Linecast` against `lineOfSightMask`. It never reaches into
  `CharacterMover`; it only calls `target.Discover()`. `discoveryRange` is
  scene-tuned rather than fixed: `M02_DioramaPrototype.unity` sets it to
  half the camera's `maxZoomDistance` (47.5 of 95), so discovery only
  becomes possible once the player has zoomed in at least 2× from the
  fully-zoomed-out starting view.
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
