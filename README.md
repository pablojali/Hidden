# Hidden

## Project

Hidden is a mobile-first 3D exploration game for Android and iOS. Players explore a
handcrafted, diorama-like world from an elevated / top-down perspective, discover
hidden details, interact with living characters and objects, and occasionally move
into closer, contextual interaction spaces with small learning challenges woven in.
The game draws inspiration from the hidden-object exploration genre but is being
built with its own original visual identity, characters, and gameplay. English,
Spanish, and French localization are planned.

This repository currently contains **M0.1 — Unity Project Foundation** only. No
gameplay, art, or content systems have been implemented yet.

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
    Materials/      Shared materials (e.g. M_Ground)
    Prefabs/        Future prefabs
    Scenes/         Bootstrap.unity
    Scripts/
      Core/         GameBootstrap and other engine-agnostic core code
      World/        (empty placeholder — future world/diorama systems)
      Characters/   (empty placeholder — future character systems)
      Interaction/  (empty placeholder — future interaction systems)
      Camera/       (empty placeholder — future camera systems)
      Learning/     (empty placeholder — future learning-challenge systems)
      Localization/ (empty placeholder — future localization systems)
      UI/           (empty placeholder — future UI systems)
      Editor/       Editor-only tooling (URP/material bootstrap)
    Settings/       URP pipeline/renderer assets (auto-created on first open)
    Resources/      Runtime-loaded resources (currently empty)
  ThirdParty/       Reserved for third-party assets (currently empty)
Tests/
  EditMode/         Minimal EditMode smoke test for GameBootstrap
```

The empty folders exist so future systems (World, Characters, Interaction,
Camera, Learning, Localization, UI) can be added without restructuring the
project, per the project's modular architecture principle. They intentionally
contain no code yet.

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
Unity inside a Docker image. **No local Unity install is required** — Unity
only runs inside the GitHub Actions runner.

This needs a Unity license available as repository secrets. That's a one-time
setup per Unity account:

### One-time setup: get a Unity license for CI

Unity requires an active license to run, even in CI. For a free Unity Personal
account:

1. **Generate an activation request file.** In this repo's GitHub page, go to
   **Actions > Request Unity Activation File > Run workflow**. This runs
   `.github/workflows/request-activation-file.yml`, which spins up the Unity
   Docker image just to produce a `.alf` request file — still no local Unity
   needed.
2. When the run finishes, open it and download the **`unity-activation-file`**
   artifact. Unzip it; you'll have a file like `Unity_v6000.x.alf`.
3. Go to **https://license.unity3d.com/manual**, upload that `.alf` file, and
   choose **Unity Personal** (or your license type). Unity emails/generates a
   `.ulf` license file back — download it.
4. In this repo, go to **Settings > Secrets and variables > Actions > New
   repository secret** and add:
   - `UNITY_LICENSE` — paste the **entire contents** of the downloaded `.ulf`
     file (open it in a text editor and copy everything, including the XML
     tags).
   - `UNITY_EMAIL` — the email of the Unity account used above.
   - `UNITY_PASSWORD` — that account's password.

   (If you have a Unity Pro/Plus seat instead, you can use a `UNITY_SERIAL`
   secret and skip the `.alf`/`.ulf` dance — see the game-ci docs linked
   above.)

You only need to do this once; the same secrets are reused by every build.

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

## Current milestone — M0.1

M0.1 establishes the technical foundation only: project structure, a minimal
Bootstrap scene (camera, light, ground, bootstrap entry point), URP rendering,
Android/iOS platform configuration, and a clean Git setup. No forest, no
characters, no hidden-object mechanics, no learning mechanics, no camera
transitions, no menus, no localization UI, and no monetization/backend have
been implemented. Those belong to future milestones.

## Environment note

This foundation was authored in a headless environment without a Unity Editor
or Unity command-line tooling installed, so the project files could not be
opened, compiled, or run by Unity itself before committing. All project,
scene, and settings files were hand-authored to Unity's standard YAML/asset
formats. **Before relying on this project, open it once in Unity 6000.3.0f1
(or the closest 6000.3.x LTS patch) to let Unity import assets, resolve
packages, and run the one-time editor setup script**, then verify Play Mode
and the build platforms as described above.
