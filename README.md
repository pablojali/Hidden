# Hidden

## Project

Hidden is a mobile-first 3D exploration game for Android and iOS. Players explore a
handcrafted, diorama-like world from an elevated / top-down perspective, discover
hidden details, interact with living characters and objects, and occasionally move
into closer, contextual interaction spaces with small learning challenges woven in.
The game draws inspiration from the hidden-object exploration genre but is being
built with its own original visual identity, characters, and gameplay. English,
Spanish, and French localization are planned.

This repository currently contains **M0.1 — Unity Project Foundation**,
**M0.2 — 3D Diorama Prototype**, **M0.3 — Living Character Prototype**,
**M0.4 — Discovery Prototype**, **M0.5 — Hidden World Core Loop**,
**M0.6 — Discovery UI & Confirmation Feedback**, **M0.7 — First
Playable / Game Feel**, and **M0.8 — First Real Level**. No learning or
content systems have been implemented yet — see `Docs/ROADMAP.md` for
what's done and what's next,
`Docs/GAME_DESIGN.md`
for the design vision, and `Docs/ARCHITECTURE.md`
for technical detail.

## Technology

- Unity 6000.3.0f1 (Unity 6.3 LTS)
- C#
- Universal Render Pipeline (URP)
- Unity Input System package (touch-ready)

## Platforms

- Android
- iOS

No backend, multiplayer, authentication, ads, in-app purchases, analytics, or
cloud services are included. Those are future decisions, out of scope for this
milestone.

## Project structure

```
Assets/
  _Project/
    Art/            Future 3D art, models, textures
    Audio/          Future audio assets
    Materials/      Shared materials (ground, foliage, rock, path, etc.)
    Prefabs/        Future prefabs
    Scenes/
      Bootstrap.unity                 M0.1 entry-point scene
      Worlds/
        M02_DioramaPrototype.unity      M0.2 prototype/test scene (unchanged since M0.7)
        Level_01_ForestDiorama.unity    M0.8 first real, designed level
    Scripts/
      Core/         GameBootstrap and other engine-agnostic core code
      Camera/       CameraInput, DioramaCameraController (M0.2)
      Characters/   CharacterMover, CharacterPath, CharacterVisual (M0.3)
      Discovery/    DiscoverySystem, Discoverable, DiscoveryManager,
                    DiscoveryPulseFeedback, CompletionFeedback (M0.4/M0.5),
                    FireworkEffect (M0.7)
      Levels/       LevelDefinition, LevelInfo (M0.8)
      UI/           DiscoveryUI (M0.6, trimmed in M0.7)
      World/        (empty placeholder — future world/diorama systems)
      Interaction/  (empty placeholder — future interaction systems)
      Learning/     (empty placeholder — future learning-challenge systems)
      Localization/ (empty placeholder — future localization systems)
      Editor/       Editor-only tooling (URP/material bootstrap)
    Data/
      Levels/       LevelDefinition assets, one per level (M0.8)
    Settings/       URP pipeline/renderer assets
    Resources/      Runtime-loaded resources (currently empty)
  ThirdParty/       Reserved for third-party assets (currently empty)
Docs/               ROADMAP, GAME_DESIGN, ARCHITECTURE
Tests/
  EditMode/         EditMode smoke tests (GameBootstrap, M0.2-M0.8)
```

The still-empty folders (World, Interaction, Learning, Localization, UI)
exist so those future systems can be added without restructuring the
project. They intentionally contain no code yet.

## Development

### Requirements

- Unity Editor **6000.3.0f1** (Unity 6.3 LTS), installed via Unity Hub.
- Android Build Support module (with SDK & NDK Tools, OpenJDK) for Android builds.
- A macOS host with Xcode for iOS builds (Unity can generate the iOS Xcode
  project on any platform, but building/signing the .ipa requires Xcode on macOS).

### Opening the project

1. Install Unity 6000.3.0f1 via Unity Hub (or the closest available 6000.3.x LTS
   patch release).
2. In Unity Hub, choose **Add** and select this repository's root folder.
3. Open the project. On first open, Unity will import all assets and resolve the
   packages listed in `Packages/manifest.json` (Universal Render Pipeline, Input
   System, Test Framework).
4. On first open, an editor-only bootstrap script
   (`Assets/_Project/Scripts/Editor/ProjectFoundationSetup.cs`) automatically
   creates the URP pipeline/renderer assets under `Assets/_Project/Settings/` and
   assigns them as the project's render pipeline, and verifies the ground
   material uses the URP Lit shader. This runs once automatically — no manual
   steps are required, but it can only run inside the real Unity Editor.

### Running the Bootstrap scene

1. Open `Assets/_Project/Scenes/Bootstrap.unity`.
2. Press Play. You should see a ground plane lit by a directional light, viewed
   from an elevated, angled-down perspective camera. The console should log
   `GameBootstrap: initialized.`.

### Running the M0.2 diorama prototype

1. Open `Assets/_Project/Scenes/Worlds/M02_DioramaPrototype.unity`.
2. Press Play. You should see a small primitives-only forest diorama (base
   terrain, hills, a house, trees, bushes, rocks, a path) from a fixed
   elevated perspective camera — no black screen, no missing pipeline.
3. In the Game view: **left-click and drag** to pan, **scroll wheel** to
   zoom. On a touch device (Android build), **one-finger drag** pans and
   **two-finger pinch** zooms instead. Both are clamped (`panBounds`,
   `minZoomDistance`/`maxZoomDistance` on the `CameraRig`'s `Diorama Camera
   Controller` component) so you can't pan off the world or zoom through
   the terrain.
4. Watch the small character (`Character` in the Hierarchy) walk its
   5-waypoint loop (`CharacterPath`) — it pauses and turns smoothly at each
   stop, with a visible bob while walking vs. a slower sway while idle. It
   moves on its own; nothing in the scene lets you control it directly.
5. Play Mode starts fully zoomed out — the whole diorama is visible but
   nothing is in discovery range yet (discovery only becomes possible once
   you've zoomed in meaningfully from that starting view). There are 3
   things to find, spread across the map so you have to actually explore:
   `Character` (the M0.3 walker), a second walker `Character2` on the
   opposite side of the diorama, and a static `HiddenGem` tucked next to a
   bush. A small "0 / 3" readout in the top-left corner (plain numbers, no
   background) tracks progress — moved from the top-right in M0.6 because
   that corner is where a phone's front-camera cutout tends to sit. Zoom in
   on each target and it should pulse briefly (scale up and back) *and* set
   off a short spark burst (firework) right where it was found, then never
   again for that target — as of M0.7 there's no confirmation text, just
   the visual. Find all 3 and you'll see the scene's light briefly
   brighten once, and nothing else changes: the diorama stays as it is,
   nothing resets or reloads. Still no menus, score, or sound.

This is a blockout for testing composition, depth, and camera feel — not
final art, and no longer the primary content scene as of M0.8 — kept
around as a prototype/test scene. See `Docs/ARCHITECTURE.md` for how the
camera is structured.

### Running Level 01 — the first real level (M0.8)

1. Open `Assets/_Project/Scenes/Worlds/Level_01_ForestDiorama.unity`.
2. Press Play. You should see a small, purpose-built forest diorama — a
   clearing and path leading toward a house, flanked by two stands of
   trees and a low hill with a rock nook — from the same fixed elevated
   camera as M02, starting at a partial zoom that shows the clearing and
   path clearly but not everything beyond it.
3. Pan and zoom the same way as M02 (mouse drag/scroll in the Editor,
   one-finger drag/two-finger pinch on a touch device).
4. There are 6 things to find this time: two static ones near the
   clearing (one easy, one tucked beside a bush), one static one just
   behind the house (only visible once you pan around it), one static one
   set into the rock/hill nook at the back (small and easy to walk past
   without looking closely), and two moving ones patrolling the two tree
   stands. Not all of them are visible from where you start — that's
   deliberate, see `Docs/ARCHITECTURE.md` for the layout and the reasoning
   behind each target's placement.
5. Discovery, feedback, and completion behave exactly as in M02: a spark
   burst at the target's position, the top-left counter (now "0 / 6")
   updating immediately, and a light-brightening cue once all 6 are found,
   after which nothing resets or reloads.

This level is the first one built specifically to test whether searching
this world is actually satisfying, not just technically functional — see
`Docs/ROADMAP.md`'s M0.8 entry for what "done" means for this milestone.

### Building for Android

1. `File > Build Settings`, select **Android**, click **Switch Platform**.
2. Ensure the Bootstrap scene is included (it is already listed in Build
   Settings).
3. `File > Build Settings > Build` (or **Build And Run** with a device
   connected).
4. Default orientation is portrait; the project already targets Android API
   level 24+ with IL2CPP/ARM64.

### Building for iOS

1. `File > Build Settings`, select **iOS**, click **Switch Platform**.
2. `File > Build Settings > Build` to generate an Xcode project.
3. Open the generated Xcode project on macOS, set a signing team, and build to
   a device or the simulator.

iOS builds require Xcode and can only be produced on macOS. If you're on
Windows/Linux, you can still switch the active platform and validate project
settings, but you cannot produce a signed `.ipa` without a macOS + Xcode step.

## CI/CD: Android build via GitHub Actions

`.github/workflows/android-build.yml` builds an Android `.apk` on GitHub's
hosted runners using [`game-ci/unity-builder`](https://game.ci/), which runs
Unity inside a Docker image — the actual build (importing assets, compiling,
packaging the APK) happens entirely on GitHub's servers, not on your machine.

This needs a Unity license available as repository secrets. Getting that
license now requires one lightweight local step, because **Unity discontinued
manual web-based activation for Personal licenses** (the old
`license.unity3d.com/manual` `.alf`-upload flow this README previously
described no longer works, which is also why the old
`unity-request-activation-file` GitHub Action was retired). You do **not**
need to install the full Unity Editor or any platform modules locally —
only the small Unity Hub application.

### One-time setup: get a Unity license for CI

1. **Install Unity Hub** (not the Editor) from
   https://unity.com/download — it's a small installer (well under 500 MB),
   separate from any specific Editor version.
2. Open Unity Hub and sign in with (or create) a free Unity account.
3. In Unity Hub, open license management — currently under the Hub's gear/
   profile menu as **"Manage licenses"** (wording may vary slightly by Hub
   version) — and add a new **Unity Personal** license (free, non-commercial
   use). This activates a license on your machine.
4. Unity writes an activated license file to disk. Look for a file named
   `Unity_lic.ulf`:
   - **Windows:** `C:\ProgramData\Unity\Unity_lic.ulf`
   - **macOS:** `/Library/Application Support/Unity/Unity_lic.ulf`
   - **Linux:** search your home folder for `Unity_lic.ulf` (location varies
     by distro/Hub version) — e.g. `~/.local/share/unity3d/Unity/Unity_lic.ulf`.

   If you can't find it, opening any Unity Editor version once (even briefly)
   after adding the license in Hub will ensure it's written.
5. In this repo, go to **Settings > Secrets and variables > Actions > New
   repository secret** and add:
   - `UNITY_LICENSE` — paste the **entire contents** of `Unity_lic.ulf`
     (open it in a text editor and copy everything, including the XML tags).
   - `UNITY_EMAIL` — the Unity account email used above.
   - `UNITY_PASSWORD` — that account's password.

   (If you have a Unity Pro/Plus seat instead, use a `UNITY_SERIAL` secret —
   see the [game-ci activation docs](https://game.ci/docs/github/activation)
   for that path.)

You only need to do this once; the same secrets are reused by every build,
and you can uninstall Unity Hub afterward if you like — nothing further runs
locally.

> This area of the Unity/game-ci ecosystem has changed more than once
> recently, so if a step above doesn't match what you see on screen, treat
> the goal (an activated `Unity_lic.ulf` file) as the target and follow
> Unity Hub's current on-screen flow, or check
> https://game.ci/docs/github/activation for the latest wording.

### Getting the APK

Once the secrets are set:

1. Go to the **Actions** tab of this repository.
2. Select **Android Build** in the left sidebar, then click **Run workflow**
   (or just push a commit to `main`/`master`, or open a PR against them — both
   trigger it automatically).
3. Wait for the run to finish (the first build imports everything from
   scratch and is slow, ~15-25 min; later builds reuse a cached `Library/`
   folder and are much faster).
4. Open the finished run and scroll down to **Artifacts**. Download
   **`Hidden-Android-apk`** — it's a zip containing the `.apk`.
5. Unzip it, copy the `.apk` to your Android phone (e.g. via USB, a cloud
   drive, or `adb install path/to/file.apk`), and install it. You'll need to
   allow "install from unknown sources" for whichever app you use to open it,
   since it isn't signed for the Play Store.

The workflow builds an unsigned debug-style APK suitable for testing on your
own device. Play Store distribution would additionally require a signing
keystore, which is out of scope for this foundation milestone.

## Current milestone — M0.8

M0.1 established the technical foundation: project structure, a minimal
Bootstrap scene, URP rendering, Android/iOS platform configuration, and a
clean Git setup. M0.2 added the first playable 3D diorama prototype: a
primitives-only forest environment and an elevated, mouse/touch-pannable
exploration camera. M0.3 added a single autonomous placeholder character
that walks a waypoint loop through that world, pausing and turning on its
own — not player-controlled. M0.4 added the first discovery mechanic: the
camera's `DiscoverySystem` marks a `Discoverable` once it's panned/zoomed
into range and view, triggering a one-shot visual pulse. M0.5 turned that
into the first minimal core loop — 3 `Discoverable` targets (two moving,
one static) tracked by a `DiscoveryManager`, which fires a single
completion cue once all 3 are found. M0.6 added the first UI: a discreet
progress readout and a per-discovery confirmation/completion message,
driven entirely by subscribing to `DiscoveryManager`'s events. M0.7 was a
first playable / game-feel validation pass: per-discovery text became a
lightweight spark-burst effect (`FireworkEffect`), the progress readout
moved to the top-left corner (a real device's front-camera cutout was
covering the top-right), a `GraphicsSettings.asset` fix stops legacy UI
`Text` from ever rendering as solid magenta/purple in a build, and
`DiscoveryManager` gained a minimal read-only `Playing`/`Completed`
session state. M0.8 is the first *intentionally designed* level: a new
scene, `Level_01_ForestDiorama`, built around the discovery loop rather
than being an enlarged version of the M0.2 blockout, with 6 targets laid
out so not everything is visible from the starting camera position — see
`Docs/ARCHITECTURE.md` for the layout. `M02_DioramaPrototype` is kept as
a prototype/test scene. No learning mechanics, no menus, no localization
system, and no monetization/backend have been implemented. See
`Docs/ROADMAP.md` for what's next.

## Environment note

This foundation (and the M0.2-M0.8 scenes/scripts) were authored in a headless
environment without a Unity Editor or Unity command-line tooling installed,
so the project files could not be opened, compiled, or run by Unity itself
before committing. All project, scene, and settings files were hand-authored
to Unity's standard YAML/asset formats. **Before relying on this project,
open it once in Unity 6000.3.0f1 (or the closest 6000.3.x LTS patch) to let
Unity import assets and resolve packages**, then verify Play Mode and the
build platforms as described above.
