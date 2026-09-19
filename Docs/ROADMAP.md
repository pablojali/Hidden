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
visible, nothing discoverable yet); `discoveryRange` was tuned down across
several passes (15 → 47.5 → 25) so discovery requires deliberately zooming
in, not just spotting something from the starting view.

Implemented ≠ validated: this milestone is done once the Product Owner has
confirmed on device that the character is genuinely out of range at the
starting zoomed-out view, that discovery only becomes possible after
zooming in meaningfully, and that it triggers the discovery pulse exactly
once.

## M0.5 — Hidden World Core Loop — implemented, pending Product Owner validation

Turns the single-target M0.4 prototype into the first minimal Explore →
discover → track progress → complete loop, with 3 `Discoverable` targets:
the M0.3 `Character` (moving), a second moving `Character2` (reuses
`CharacterMover`/`CharacterPath`/`CharacterVisual` as-is on a separate
west-side route — no new movement code), and a static `HiddenGem` tucked
next to a bush (feedback via the new, movement-independent
`DiscoveryPulseFeedback`). A new `DiscoveryManager` owns global progress
(`TotalTargets`, `DiscoveredCount`, `IsComplete`, `OnDiscovery`,
`OnCompleted`) by subscribing to each target's existing M0.4 `Discovered`
event — `DiscoverySystem` still only detects individual targets and never
touches progress, and `CharacterMover` remains fully unaware discovery
exists. Completion triggers one minimal, UI-free visual cue (`CompletionFeedback`
briefly boosts the key light). Still no UI/counter/score — the progress
API exists for M0.6 to build on. See `ARCHITECTURE.md`.

Implemented ≠ validated: this milestone is done once the Product Owner has
confirmed on device that all 3 targets are individually discoverable and
that finding the last one triggers the completion light pulse.

## M0.6 — Discovery UI & Confirmation Feedback — implemented, pending Product Owner validation

Adds the first UI layer: a `DiscoveryCanvas` with a small discreet
top-right progress readout ("0 / 3" → "3 / 3") and a lower-center
confirmation message that pops in/fades out (~1.3s) each time a target is
found, plus a slightly stronger completion message when all 3 are found.
`DiscoveryUI` (`Scripts/UI/DiscoveryUI.cs`) only subscribes to
`DiscoveryManager`'s existing `OnDiscovery`/`OnCompleted` events and reads
its `TotalTargets`/`DiscoveredCount` — `DiscoveryManager` has no reference
to it or any other UI object, and nothing about `CharacterMover` or
`DiscoverySystem` changed. Confirmation/completion copy is Inspector-
configurable on `DiscoveryUI` (`discoveryMessages`/`completionMessage`),
never hardcoded in gameplay code, so a future localization system can
replace it without touching `DiscoveryManager`/`DiscoverySystem`. See
`ARCHITECTURE.md` for the chosen microcopy and reasoning.

Implemented ≠ validated: this milestone is done once the Product Owner has
confirmed on device that the progress counter updates immediately, the
confirmation pop-in reads clearly without covering the diorama, and the
completion message is visibly distinct from a normal discovery.

**Superseded in M0.7**: device testing showed the per-discovery/completion
text competing with the diorama for attention, and on the test device the
progress counter's top-right corner placement was obstructed by the
phone's front-camera cutout. M0.7 replaced the text with a visual-only
firework effect and moved the counter to the top-left. The progress
readout itself (now numbers only, no message text) remains from this
milestone.

## M0.7 — First Playable / Game Feel — implemented, pending Product Owner validation

First validation pass on the whole loop as one playable session, not a
new feature set. Per-discovery feedback changed from M0.6's pop-in text to
a short, visual-only spark burst (`FireworkEffect`, in `Scripts/Discovery`)
at the discovered target's position — a single persistent, reusable rig
(never instantiated/destroyed) that subscribes to `DiscoveryManager`'s
existing `OnDiscovery` event the same way `DiscoveryUI` does.
`DiscoveryUI` itself was trimmed back to just the progress readout, now
top-left (numbers only, no background) instead of top-right, which the
front-camera cutout on the test device was covering. `DiscoveryManager`
gained a minimal `SessionState` (`Playing`/`Completed`) — a read-only view
over state it already tracked since M0.5, not a new framework. Also fixed:
`ProjectSettings/GraphicsSettings.asset` had an empty
`m_AlwaysIncludedShaders` list, a known cause of legacy UI Text/Shadow
rendering as solid magenta/purple in a build when nothing else in the
project references the `UI/Default` shader as a material asset — added it
explicitly. Character speeds, pauses, discovery range, and target
placement were reviewed and left as-is; they were already tuned through
several prior rounds of direct device feedback and hold up under the
M0.7 checklist. See `ARCHITECTURE.md`.

Implemented ≠ validated: this milestone is done once the Product Owner has
run through the full manual checklist in the M0.7 report on device,
confirming in particular that the purple/magenta UI issue is actually
fixed, the progress counter is now visible, and the firework reads clearly
without any confirmation text.

## M0.8 — First Real Level — implemented, pending Product Owner validation

First intentionally designed Hidden World level, built to validate whether
the *search itself* is enjoyable now that M0.7 proved the discovery loop
works technically — not a new feature set. A new scene,
`Assets/_Project/Scenes/Worlds/Level_01_ForestDiorama.unity`, holds a
small, purpose-built diorama with 6 `Discoverable` targets (2 moving, 2
partially hidden static, 1 easy, 1 requiring closer observation), laid out
across distinct areas (a clearing/path, a house, two tree stands, a small
hill/rock nook) so that not every target is visible from the starting
camera position. `M02_DioramaPrototype.unity` is left untouched as the
prototype/test scene (option B) — nothing about M0.1–M0.7's architecture
was rewritten, only reused: the same `CharacterMover`/`CharacterPath`,
`Discoverable`, `DiscoveryPulseFeedback`, `DiscoveryManager`,
`DiscoverySystem`, `FireworkEffect`, and `DiscoveryUI` as before, each
independent of the others exactly as already established. A new, minimal
`LevelDefinition`/`LevelInfo` pair (`Scripts/Levels/`) only names the level
and can flag a target-count mismatch — `DiscoveryManager` remains the sole
owner of discovery progress. See `ARCHITECTURE.md` for the full layout and
target-by-target design rationale.

Implemented ≠ validated, and this one is explicitly not yet claimed to be
*fun*: this milestone is done once the Product Owner has played the level
on device and confirmed that looking around the diorama and finding its
6 targets is actually satisfying, not just technically functional.

## M0.9 — Polish: Camera Framing & Completion Celebration — implemented, pending Product Owner validation

A polish pass on `Level_01_ForestDiorama.unity` requested before continuing
level-design work, addressing two pieces of direct feedback rather than
adding new mechanics. `M02_DioramaPrototype.unity` was left untouched, per
explicit direction this round.

**Wider starting camera.** The opening view now shows substantially more
of the diorama — `DioramaCameraController.maxZoomDistance` went from 40 to
65 and the scene's starting zoom distance from 24 to 52 (80% of the new
max), so the level reads as a small living miniature world at a glance
rather than a close-in crop, without putting every target in plain sight.
`minZoomDistance` (6) is untouched, so close-in inspection still works
exactly as before. `panBounds`, `cameraAngle`, and pan/zoom speed/smoothing
are all untouched — this is a defaults/limits change, not a camera
redesign. `DioramaBase`'s scale grew from 41 to 68 (proportional to the
zoom increase) purely so the ground still fills the frame at the new,
farther starting distance; nothing about its material or the rest of the
diorama's layout changed.

**Completion celebration.** A new `CompletionCelebration` component
(`Scripts/Discovery/CompletionCelebration.cs`) plays a clearly bigger,
longer spark burst when the 6th target is found — 16 sparks over ~1.1s,
vs. `FireworkEffect`'s 8 sparks over 0.6s per discovery. It's the same
architecture as `FireworkEffect` (a persistent, pre-placed rig animated by
one shared sine curve, no per-frame allocation, never
instantiated/destroyed) but parented to the Main Camera in camera-local
space instead of a world position, so the burst always reads as covering
the screen — with the diorama still visible around and behind it — no
matter where the camera has panned or zoomed to. It subscribes to
`DiscoveryManager.OnCompleted` the same one-directional way
`FireworkEffect` subscribes to `OnDiscovery`; `DiscoveryManager` still has
no reference back. `DiscoveryManager.OnCompleted` already only fires once
per manager (M0.5), and `CompletionCelebration.Play()` adds its own
`HasPlayed` guard on top, so the celebration cannot play twice regardless
of how it's triggered. `CompletionFeedback`'s existing light-pulse cue is
untouched and still runs alongside it. No "LEVEL COMPLETED" screen, no
buttons, no menus, no grayscale transition, no scoring — none of that was
added, per explicit scope.

See `ARCHITECTURE.md` for the exact numbers and wiring.

Implemented ≠ validated: this milestone is done once the Product Owner has
confirmed on device that the wider opening view actually reads as "a small
living world," that discovery-firework and counter behavior are unchanged,
and that the completion celebration feels clearly different from — and
better than — a single discovery, without asserting here that it does.

## M0.10+ — not started

The dive-in interaction, the learning-challenge system, localization,
progression, and save data are future work and intentionally out of scope
until explicitly requested.
