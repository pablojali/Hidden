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

Exploring the world can now genuinely find something: panning/zooming a
target into camera view marks it discovered and it reacts once. This is
the first minimal proof of the "discover hidden details" pillar.

The world now holds 3 things to find, spread across it on purpose so
finding all of them means actually scanning the diorama rather than
staring at one spot: two living figures walking their own separate routes,
and one still, quieter thing tucked into the scenery. Finding the last one
gives a small shared moment (the world's own light responding) rather than
a score screen — the loop is "explore → discover → discover → discover →
done."

The player now gets a small, quiet acknowledgment for each find — a short
spark burst right where the thing was hiding — instead of wondering
silently whether something registered, plus a discreet running count
tucked in a corner. It stays out of the way on purpose: no score, no menu,
nothing that turns the diorama into a dashboard. An earlier version of
this acknowledgment used a pop-in line of text ("There you are!"); after
seeing it on device, the text was dropped in favor of the visual spark —
it reads faster, doesn't compete with the diorama for attention, and
doesn't need translating later.

A full playthrough now reads as one small, complete session: the world is
immediately explorable the moment it opens (no tutorial, nothing to
dismiss first), and finding the third thing gives a clearly bigger,
different response (the world's light) than finding the first or second —
so the player knows without being told that the session is over. Nothing
resets or reloads; the finished diorama just sits there to be looked at.

A new, small level (`Level_01_ForestDiorama`) puts that loop somewhere
built specifically for it, rather than the original blockout: a clearing
and path lead toward a house, flanked by two stands of trees and a low
hill with a rock nook, so the world reads as a handful of distinct places
rather than one open field. Six things are hidden across it — two that
move, two tucked partly out of sight, one easy enough to find first, and
one that takes real looking — and the starting view deliberately doesn't
show all of them: what's visible is a place worth walking into, not a
puzzle laid flat on the table. Whether that search is actually enjoyable,
rather than just technically working, is what this level is for finding
out — not asserted here as a finished result.

## Out of scope for now

Character interaction, learning challenges, puzzles, rewards, progression,
inventory, and save data are all designed for later milestones and are
intentionally not implemented yet. See `ROADMAP.md` for what's actually
built.
