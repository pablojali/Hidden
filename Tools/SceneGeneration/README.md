# Scene generator (historical, headless-authoring tool)

`gen_level01.py` is the Python script used to hand-author
`Assets/_Project/Scenes/Worlds/Level_01_ForestDiorama.unity` from scratch,
entirely as text, across the M0.8–M0.10 milestones, in an environment with
**no Unity Editor available**. It writes the whole scene's YAML directly:
GameObjects, Transforms, MeshFilters/MeshRenderers, and every
MonoBehaviour's serialized fields (including the procedural mesh
generators in `Assets/_Project/Scripts/World/`), using fileID allocation
and GUID references extracted by hand from the existing prototype scene
and materials.

Run it with:

```
python3 Tools/SceneGeneration/gen_level01.py
```

It overwrites `Level_01_ForestDiorama.unity` completely (`OUT_PATH` near
the top of the file) — it does not read the existing scene, it rebuilds
it from the constants and `emit_*()` calls in the script itself. Every
Discoverable target's position is a literal coordinate in the
`emit_static_target`/`emit_moving_target` calls near the bottom of the
file; changing the scene by editing this script and re-running it is far
safer than hand-editing the generated `.unity` file directly, since the
fileID bookkeeping (avoiding duplicates/dangling references) is handled
automatically by the shared `alloc()`/`alloc_n()` counter.

**Now that a real Unity Editor is available**, this script is a
reference/fallback, not the required workflow. Prefer using the Editor
directly for further scene changes (you can see what you're building,
which this script's author never could). Keep this script useful by
updating it alongside manual Editor changes if you want to preserve the
ability to regenerate the scene from scratch — otherwise it will drift
out of sync with the real `.unity` file and should be treated as
historical documentation of how M0.8–M0.10 were built rather than a live
source of truth.

See `Docs/ARCHITECTURE.md`'s M0.8–M0.10 sections for the reasoning behind
specific generator choices (mesh techniques, fileID/GUID conventions,
validation approach).
