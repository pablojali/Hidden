# Game Design

## Vision

Hidden is a mobile-first 3D exploration game for Android and iOS. Players
explore a handcrafted, diorama-like world from an elevated perspective,
discover hidden details, interact with living characters and objects, and
occasionally move into closer, contextual interaction spaces with small
learning challenges woven in. It draws inspiration from the hidden-object
exploration genre but is built with its own original visual identity,
characters, and gameplay — not an imitation of any existing title's look.
English, Spanish, and French localization are planned.

## Visual direction

- 3D, handcrafted/manual diorama feeling — the world should read as a small
  physical model, not a flat map.
- Colorful but pastel; soft, simple shapes; readable silhouettes.
- Pleasant, slightly whimsical lighting. Visually clean.
- Original identity — explicitly not a copy of any other game's style.

## Core experience (current scope)

The player looks down into a small physical world from a fixed, elevated
perspective (not orthographic, not first-person) and explores it by
panning and zooming. The world is composed with foreground, midground, and
background elements so it reads as a place with real depth, not a flat
backdrop — open, readable areas alternate with spots partially hidden by
terrain or objects.

A single autonomous placeholder character now walks a fixed loop through
that world — pausing, turning, and continuing on its own, never controlled
by the player. It's a first test of the "living world" feeling: does a
figure moving through the diorama make the player want to keep watching?
This is behavior only, not a character system — no interaction, no
dialogue, no identity yet.

## Out of scope for now

The discovery system, character interaction, learning challenges, puzzles,
rewards, progression, inventory, and save data are all designed for later
milestones and are intentionally not implemented yet. See `ROADMAP.md` for
what's actually built.
