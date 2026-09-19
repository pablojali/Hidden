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

## M0.4 — Discovery Prototype — implemented, pending Product Owner validation

Adds the first minimal discovery mechanic: `DiscoverySystem` (on the
`Main Camera`) evaluates the world from the camera's point of view —
range, in-view, optional line-of-sight — and marks a `Discoverable` (on
the M0.3 `Character`) discovered exactly once. `CharacterVisual` reacts
with a one-shot scale pulse; `CharacterMover` was not touched and has no
knowledge discovery exists. See `ARCHITECTURE.md`.

The M0.2 diorama's footprint was enlarged twice during tuning (~2.5×, then
another ~1.3×, positions only — prop sizes unchanged) with pan bounds/zoom
range widened to match: at the original size the whole world fit on screen
almost regardless of camera position, so "discovered" was meaningless
(always true). Trees, rocks, bushes, and houses were then tripled in count
(11→33, 11→33, 3→9, 1→3) and scattered across the world so it reads as a
full, lived-in place rather than a handful of props in empty space. Camera
pan sensitivity was softened for a gentler, kid-friendlier feel; zoom was
tuned in the opposite direction (faster/more agile) since the zoom range is
now much wider. Play Mode starts fully zoomed out (the whole diorama
visible, nothing discoverable yet), and `discoveryRange` is set to half the
camera's max zoom distance, so discovery only becomes possible once the
player has zoomed in at least 2× from that starting view.

Implemented ≠ validated: this milestone is done once the Product Owner has
confirmed on device that the character is genuinely out of range at the
starting zoomed-out view, that discovery only becomes possible after
zooming in meaningfully, and that it triggers the discovery pulse exactly
once.

## M0.5+ — not started

The dive-in interaction, the learning-challenge system, UI, localization,
progression, and save data are future work and intentionally out of scope
until explicitly requested.
