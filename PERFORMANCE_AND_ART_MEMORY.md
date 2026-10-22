# Performance allocation and art requirements

Recorded at the user's explicit request, 2026-10-22. This is project memory, not a measured performance certification.

## Target and counting rules

- Working target: standalone Quest 3/3S, 90 Hz (11.11 ms per frame), pending device profiling. CPU and GPU have separate budgets; their times are not simply added.
- Normal visible WORLD geometry: 400,000–450,000 triangles for town, trains, tracks, terrain, and vegetation combined.
- Provisional review thresholds: 600,000 visible triangles and 750,000 Unity-imported vertices across the frame's active LOD meshes. These are planning thresholds, not hardware limits or automatic permission to fill the budget.
- Count each visible mesh once at its active LOD for asset accounting. Stereo, shadow passes and actual GPU vertex processing require separate measurement. Blender authoring vertices do not equal Unity imported vertices: UV/normal/tangent seams can split vertices.
- Up to FIVE trains may be visible at once. The overhead board exposes most of the town simultaneously; do not assume strong occlusion savings.
- Original category allocation was a 600k-triangle upper envelope: locomotives 75k; up to ten future wagons 30k; buildings/industry 120k; tracks/bridges/tunnels 70k; terrain/rocks 70k; vegetation 100k; small props 30k; hands/UI/effect geometry 35k; reserve 70k. This is superseded for normal WORLD rendering by the 400–450k target above. Rebalance scenery to fit; do not add these envelope figures to the normal target.

## Asset targets

- Each locomotive at normal board viewing distance: about 15,000 triangles, at most 25,000 imported vertices. Five engines: about 75k triangles / 125k imported vertices.
- Close inspection locomotive LOD: 30–40k triangles, only when justified by screen size. Far LOD: approximately 3–5k triangles, with a cheaper very-distant LOD as needed.
- Future wagon: approximately 3k triangles; ordinary house 1–3k; hero building 5–10k; normal-distance tree 300–700; small prop 50–300. These draw from shared scene budgets, not additional allowances.
- Shared PBR textures/materials, baked small detail, LODs, economical topology and UVs. 4K authoring maps may use 2K ASTC variants on Quest. Track geometry and dense foliage also need distance reduction.

## VFX, audio, UI, and systems

The game will contain many VFX, sounds, player UI and panels. Their cost MUST be reserved explicitly; a small particle/UI vertex count does not imply low cost.

Initial GPU planning allocation at 90 Hz: world/trains including lighting/shadow passes 5.5 ms; VFX/water/smoke/steam 1.5 ms; hands/UI/panels 1.0 ms; other rendering overhead 1.0 ms; headroom about 2.1 ms. Not measured and subject to revision.

- VFX: pool, prioritize and distance-cull effects; limit overlapping transparent smoke and expensive full-screen effects. Profile screen coverage/overdraw as well as simulation.
- Audio: separately budget CPU, concurrent real/spatialized voices, DSP, memory and streaming. Prioritize nearby engines, warnings and interaction sounds; reduce distant layers. No approved fixed voice count yet.
- UI: separately budget draw calls, transparency, atlas memory and layout/text rebuilds. Separate static/frequently changing content; update on changes and stop rendering hidden panels.
- Also reserve CPU for train animation, interactions, physics, networking and voice chat. NPC/rocket gameplay adds CPU and VFX load.
- Validate on device with all five trains moving/steaming, industry effects, sound, hands, multiplayer load where applicable, and the busiest player panel open. Editor asset counts alone do not establish headset performance.

## Visual quality: explicit user direction

- Meta First Hand demo is the visual-quality reference. Do not claim parity without visual review.
- User rejected the test train's visibly basic primitive shapes. Require detailed modeling: shaped castings, recessed/inset panels, actual openings, curved profiles, forged/fluted mechanisms, convincing cab fixtures, manufactured edge treatment and carefully authored material detail. Primitive blockouts are not finished art.
- Preserve silhouette and functional detail in geometry; bake fine carving, fasteners, wear and microdetail to normal/base-color/roughness maps. Lower counts must not mean an unfinished blockout.
- Editable detailed source, baked PBR maps, optimized exported mesh, multiple LODs and correct mechanical animation are required.

## Locomotive design and motion

- Locomotive only in current scope; do not build wagons now. Retain attachment/coupler anchors for later.
- Game art direction: Germany 1910–1913. T18-inspired silhouette applies to 1912–1913. Private-company livery is fictional.
- New explicit gameplay exception: add a proper driver's CHAIR and STEERING WHEEL/control wheel; accommodate an NPC driving seated, standing, and firing a rocket launcher when another train blocks the route. This is a fictional gameplay adaptation, not a historically literal steam-locomotive cab.
- Provide usable cab access, standing space and an open firing position/roof opening, with seat/standing/hand/aim attachment markers. NPC size used for initial fit checks must be documented; final animated-character fit remains a separate integration check.
- The user suspects the previous animation ran backward. Check wheel contact direction against signed root travel, not only rod pin connections. Positive forward travel must move the smokebox end first (prefab +Z; Blender -X), with no-slip wheel rotation. Clearly label forward/reverse previews.
- Do not silently flip every wheel based on a visual suspicion; verify exported axes, root travel and contact velocity in Blender and Unity. Preserve correct quartering and synchronized rods.

## Locomotive V2 implementation record (2026-10-22)

- Current Unity prefab: `Assets/SwitchyardRaiders/CompanyLocomotive/Prefabs/CompanyLocomotive.prefab`; source and review output: `Deliverables/CompanyLocomotiveV2/`.
- Gameplay LOD1 verified in Unity: **23,471 imported vertices / 14,900 triangles**. Five: 117,355 vertices / 74,500 triangles. Close LOD0: 52,101 vertices / 39,748 triangles; far LOD2: 10,687 / 5,329; very far LOD3: 6,726 / 2,997. Only one LOD per train renders. NPC and effects costs are additional.
- Detailed chair/control wheel, shaped panels, profiled castings, real fluted rods, and a raised rear standing platform are modeled. Seated/standing/hand/aim/muzzle markers are exported for later character integration.
- Initial fit proxy assumes a 1.80 m NPC. Aim marker is 4.48 m above rail; straight forward clearance against model geometry passed. Final character animation, navigation, weapon and effects integration are not implemented by the model itself.
- Original wheel direction was already correct for positive travel. Its prior demo reversed halfway. New labeled fixed-camera preview explicitly distinguishes directions. Actual Unity contact checks across seven axles, both travel signs and three scales passed; do not reverse the solver just because the demonstration later backs up.
- This is asset-level validation, not a five-train headset performance result. First Hand parity remains subject to visual review.

## Trim and animation-controller requirements (2026-10-22)

- User identified visibly deformed rails/window beads. Thin manufactured tubing must have stable sweep orientation, clean closed seams and continuous UVs. Protect visible rails/window trim from decimation in close and gameplay LODs; do not fix them by sacrificing large body/wheel silhouettes. Validate in close-up renders and Unity after import.
- The user explicitly requires the imported animation attached to a Unity Animator Controller. The prefab now has `Animations/CompanyLocomotive.controller`, an enabled Animator on the imported model root, and the imported `Locomotive_Forward` clip in the default `Rolling` state. `PlaybackSpeed` 1/0/-1 gives forward/pause/reverse. Root translation stays with track gameplay.
- Animator playback and exact rail-distance motion must be exclusive. `SetTravelDistance` switches to RailDistance mode and disables Animator playback. `UseAnimatorPlayback(speed)` switches back. Do not silently strip the Animator again.
- Controller playback, reverse/pause, clip bindings and driver exclusivity were evaluated in the editor and passed; this is not headset performance validation.

## Driver orientation correction (2026-10-22)

- The chair backrest had its padded face toward the rear. It is now turned toward the seat and smokebox, including the editable source and retained LOD backrests. The entire driving station was already placed facing the smokebox; do not rotate the whole station 180 degrees.
- NPC seat/stand/hand/aim anchors must use Unity +Z forward and +Y up. Set and verify these after FBX import and during Animator playback. The control wheel is ahead of the seat; its hub faces the driver.
- Preserve this orientation in future regenerated assets. This correction adds no geometry and does not change the rolling mechanism.

## References

- https://developers.meta.com/horizon/documentation/unity/unity-perf
- https://developers.meta.com/vr/documentation/unity/unity-mobile-performance-intro/
- https://create.unity3d.com/Unity-UI-optimization-tips
- Local art direction: Deliverables/RoomMenu/ART_DIRECTION.md


## Functional completeness checks (2026-10-22)

- User identified a blocked chimney and floating brass whistle in the Unity model. Require a genuine open chimney bore through the smokebox shell, a visibly attached whistle stem/flange and mounted safety valves. Protect these and the driver chair/control wheel from destructive gameplay LOD reduction.
- Final gameplay LOD: 23,471 imported vertices / 14,900 triangles; five locomotives 117,355 vertices / 74,500 triangles. Far LOD now 5,329 triangles to preserve these details; budget remains provisional.
- Export explicit Forward and Reverse takes. Default controller Rolling uses Forward with signed PlaybackSpeed; explicit Reverse state uses the reverse take at speed 1. Both actual clip directions were checked at the rail contact point.
- Main steam outlet is VFX_ChimneyExhaust at the open front chimney; cylinder drain, safety valve and whistle anchors also exist. Chimney smoke is implemented; other steam outlets remain attachment markers.
- Finishing pipeline must include finish_functional_details.py and sync_functional_source.py after finalize_v2.py.


## Chimney smoke implementation (2026-10-22)

- The locomotive prefab now includes CompanyLocomotiveSmoke and one chimney ParticleSystem. Soft gray puffs rise, expand, rotate and dissipate. Emission follows movement/engine load, with a subtle four-pulse-per-wheel-revolution rhythm. EngineRunning=false stops new emission while old particles expire.
- Hard cap 48 particles per train, 240 across five trains; one shared 512x512 four-variant atlas (Android ASTC 6x6), one shared URP Particles/Unlit material, billboard quads. No particle lighting, shadows, collision, distortion or soft-particle depth requirement. This adds at most 96 triangles / 192 billboard vertices per train before stereo/pass accounting, plus transparency cost.
- Default simulation space is the train parent (tabletop), falling back to world. Set simulationSpaceRoot to the actual board/world root if the train parent is a moving train-specific container. Hierarchy scaling supports miniature viewing.
- Viewer defaults to Camera.main. Fade emission from 45 to 90 authored locomotive metres (world distance divided by train scale), clear/stop at 90, restart nearby. Tune on headset.
- Editor checks passed for correct outlet direction, lingering particles independent of train movement, engine-off dissipation, cull/resume, miniature scaling and five concurrent systems sharing one material. Five systems produced 146 live puffs in the test; no headset frame-time validation.
- Prefab rebuild calls CompanyLocomotiveSmokeBuilder.Attach so smoke survives regeneration. Other valve/whistle/drain anchors have no effects yet.
