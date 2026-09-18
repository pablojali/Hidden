# Roadmap

This tracks milestone-level progress only. Design detail lives in
`GAME_DESIGN.md`, technical detail in `ARCHITECTURE.md`.

## M0.1 — Unity Project Foundation — done

Clean Unity 6.3 LTS / URP project: folder structure, Git setup, CI (Android
build via GitHub Actions), a minimal `Bootstrap` scene (camera, light, ground
plane, bootstrap entry point). No gameplay, art, or content systems.

## M0.2 — 3D Diorama Prototype — implemented, pending Product Owner validation

First playable 3D blockout: a small primitives-only forest diorama
(`Assets/_Project/Scenes/Worlds/M02_DioramaPrototype.unity`) viewed from a
fixed-angle elevated perspective camera, with mouse pan/zoom in the Editor
and an input architecture ready for touch. Validates composition, depth,
and camera framing only — **not final art**.

Implemented ≠ validated: this milestone is done once the Product Owner has
tested the scene in Unity Editor Play Mode and confirmed the camera feel,
depth, and readability.

## M0.3+ — not started

Anything beyond camera exploration — characters, the discovery system, the
learning-challenge system, UI, localization, progression, save data — is
future work and intentionally out of scope until explicitly requested.
