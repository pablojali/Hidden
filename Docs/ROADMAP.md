# Roadmap

This tracks milestone-level progress only. Design detail lives in
`GAME_DESIGN.md`, technical detail in `ARCHITECTURE.md`.

## M0.1 — Unity Project Foundation — done

Clean Unity 6.3 LTS / URP project: folder structure, Git setup, CI (Android
build via GitHub Actions), a minimal `Bootstrap` scene (camera, light, ground
plane, bootstrap entry point). No gameplay, art, or content systems.

## M0.2 — 3D Diorama Prototype — done

First playable 3D blockout: a small primitives-only forest diorama
(`Assets/_Project/Scenes/Worlds/M02_DioramaPrototype.unity`) viewed from a
fixed-angle elevated perspective camera, with mouse pan/zoom in the Editor
and one-finger-drag/pinch pan/zoom on touch devices, sharing the same
camera controller. Confirmed rendering and camera framing correctly on a
real Android device (Galaxy S10e). Composition/depth/camera feel only —
**not final art**.

## M0.3 — Living Character Prototype — implemented, pending Product Owner validation

Adds one autonomous placeholder character (capsule body + sphere head) that
walks a 5-waypoint loop through the diorama, pausing and smoothly turning
at each stop, with a procedural walk-bob/idle-sway to distinguish its two
states at a glance. The character is not player-controlled and knows
nothing about camera, input, discovery, or any other system — see
`ARCHITECTURE.md`.

Implemented ≠ validated: this milestone is done once the Product Owner has
watched the character move through the world in Play Mode and confirmed it
makes the world feel alive.

## M0.4+ — not started

The discovery system, the learning-challenge system, UI, localization,
progression, and save data are future work and intentionally out of scope
until explicitly requested.
