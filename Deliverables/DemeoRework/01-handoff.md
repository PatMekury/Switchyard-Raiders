# Task 1 handoff — compact recessed table and terrain

Completed implementation and Edit Mode validation on 2026-10-22, America/Chicago, in `C:\Users\patmekury\Documents\Unity\Switchyard Raiders`. Connected editor verified Unity 6000.6.0b5, Android target, SR_Launch, Play Mode stopped. No Play Mode, player build/install, headset launch or Unity restart was performed.

## Adopted result

**1.30 x 1.30 m outside; 1.05 x 1.05 m usable land.** This is an implementation choice, not a user-approved size. The extra 5 cm over the prompt's starting candidate accommodates the retained cup insert diameter and manufactured rail profiles. The prior rectangular outside area is reduced about 36%.

The land is a newly authored 1.058 m square, 150 mm deep cartridge. It rests on the well floor with its top 15 mm below the wood rim. The real undercut lip conceals 4 mm of the land edge, with 2 mm vertical clearance and 3 mm side clearance. A continuous deep wooden well and structural bottom conceal the slab; cup sockets pass through the upper apron. No stretched table mesh, floating skirt or final town was created. Oak grain/material maps, rolled steel cups, shaped supports, brass adjustment collars, levelling feet and mounting brackets remain.

Read **[TABLETOP_VIEW_CONTRACT.md](../../TABLETOP_VIEW_CONTRACT.md)** before Tasks 2–6. Presentation origin is the land underside; terrain datum y=.150, rim y=.165, cup lip y=.168, tray underside y=-.035. TownMount is .885 above the authored table origin. Recenter is .95 m forward of head yaw, putting the new near edge .30 m forward and rim .50 m below the eyes. Normal VR scale 1 is now the minimum. MR minimum is its confirmed normal placement scale. Maximum 12× remains for Task 2 to calibrate.

The same structural tray is shown in VR and MR; MR hides the supports. Its whole outside footprint is checked against the scanned table and its underside is kept 6 mm above the plane, including anchored resizing. Updated reusable prefabs, launch prefab, launch scene, preview outline, seat/mount/handle anchors and current contact colliders agree with the contract.

The temporary railway root is uniformly .68 instead of .92 to fit inside the square land. Local locomotive scales compensate to keep their prior effective .00368 scale (roughly 54.45 mm long). This is fit maintenance, not Task 2's train calibration. Authoritative simulation, ownership, scores, 2/4-player flow, six-delivery 2v2, replenishing freight, networking, voice, train models and motion code are retained. No rig, manager or runner was added.

## Baseline and reference evidence

- **[BASELINE_BEFORE_REWORK.md](BASELINE_BEFORE_REWORK.md)** records old VR and configured/calculated MR sizes, train envelopes and placement rules before geometry edits. Task 5 must use this baseline. No observed MR scale was available; doubling the old configured full-scale land would mean 3.00 x 2.10 m, not twice the new square land. The later task must resolve the square-shape/reference-scale policy explicitly.
- Reviewed the supplied `video-contact-sheet.jpg` and `zoom-sequence.jpg`, and extracted fresh frames from `scene design/Demeo  Scaling.mp4` at 23, 26, 28, 31, 48, 51, 54 and 57 seconds into `ReferenceInspection/`. The enclosed frame, overview proportions and useful close inspection informed the design. The video establishes neither internal implementation nor exact dimensions/zoom ratios.

## Actual-asset evidence

All images are isolated Unity URP Camera.Render captures from the imported production prefabs, not concept art or headset captures. PreviewRenderUtility was not used. Seated image uses the contract's relative eye/board position and 65° vertical FOV, not a measured headset projection; the near frame can extend beyond that image at normal reach distance.

![Front, side, top and configured seated views](Evidence/00-four-views.jpg)

Individual images: [front](Evidence/01-front.png), [side](Evidence/02-side.png), [top](Evidence/03-top.png), [seated](Evidence/04-seated.png), [assembly](Evidence/05-assembled.png), [empty structural well](Evidence/06-empty-structural-well.png), [MR tray geometry](Evidence/07-MR-tray-geometry.png), [LOD1](Evidence/08-LOD1.png). The empty-well image shows the load-bearing construction beneath the cartridge. Final cup interiors were inspected after correcting apron interference and refreshing existing mesh GPU buffers.

[Actual railway fit](Evidence/09-railway-fit.png) additionally shows the retained temporary loop, controls and five authored locomotives on the smaller land. This is an isolated Edit Mode fixture with deliberately positioned trains, not a live match; their still-small display size is intentionally left for Task 2.

## Files and regeneration

- Executable shared constants: `Assets/SwitchyardRaiders/Startup/Runtime/TabletopViewContract.cs`.
- Source: `Source/SR_RecessedTable.blend`, `Source/SR_RecessedLand.blend`; table bevels remain editable and land retains its regular quad grid. LOD collections are retained. `Source/legacy_table_profiles.py` preserves the prior craftsmanship helpers used by `build_recessed_assets.py`.
- Exports and imported combined meshes: `Assets/SwitchyardRaiders/RecessedTable/Art/` and `Meshes/`. Existing PBR material/texture assets are reused.
- Public prefab paths/GUIDs retained: `Assets/SwitchyardRaiders/PlayTable/Prefabs/SR_PlayTable.prefab`, `Assets/SwitchyardRaiders/TerrainBase/Prefabs/SR_TerrainBase.prefab`, `Assets/SwitchyardRaiders/Startup/Prefabs/SR_TabletopPresentation.prefab`.
- Integration: `Startup/Prefabs/SR_Launch.prefab`, `Startup/Scenes/SR_Launch.unity`; only 35 dimension-related lines in each differed from the start-of-task snapshot. No broad scene regeneration was run.
- Tools: `Startup/Editor/RecessedTableTask1.cs` and `RecessedTableTask1Review.cs`. Existing table/terrain build/review menu entries route to these tools, so they do not silently restore old rectangular dimensions. Legacy implementation bodies remain for historical recovery.

Regenerate from project root with Blender 5.2 `--background --factory-startup --python-exit-code 1 --python Deliverables/DemeoRework/build_recessed_assets.py`. Then use **Switchyard Raiders → Demeo Rework → 1 - Import recessed assets**, **2 - Migrate presentation dimensions**, **3 - Validate and render actual assets**. Step 2 requires SR_Launch in Edit Mode with no unsaved scene edits; coordinate with other chats before rerunning it. Existing constant-based geometry validation catches source/contract mismatches. Regeneration never touches an interactive Blender document.

## Validation

**178 geometry/asset assertions passed**, in [validation-task1.txt](validation-task1.txt). Both LODs match measured outer and land bounds, have unit authored scales, coherent normals, finite UVs, tangents and supported material slots; no reversed-normal or degenerate triangles were found. Actual imported-triangle ray tests: all 468 exterior side samples hit the structural tray before the land slab; all 121 sampled top positions remain open. Both reusable presentations retain mounting references and previous effective train scales. Visual review covers all supplied views, including the well and cup interiors. These sampled checks do not replace later collision/comfort checks with future scenery.

| Asset | LOD0 triangles / imported vertices | LOD1 triangles / imported vertices |
|---|---:|---:|
| Complete table | 20,552 / 27,692 | 10,384 / 11,128 |
| Land | 1,438 / 921 | 222 / 185 |

**64 integration + 28 contact/placement + 17 gameplay-rule assertions passed**, plus existing room/voice service and scene wiring checks. See [validation-regression.txt](validation-regression.txt). Checks include simultaneous four-slot progress, six team deliveries, gradual/replenishing freight, snapshots, rematch/leave, personal transform behavior, normal minimum zoom, MR underside compensation, square footprint rejection/acceptance, support visibility, two-hand contact behavior, no missing scripts, exactly one XR rig/session, permission ownership, recorder gate and remote speaker wiring. They do not authenticate real users or verify a live network/voice session. Compilation passed; active scene ended saved and Play Mode remained false.

## Preservation and tooling

Before edits, existing contact-grab checks/setup, integration checks, launch scene/prefab, TABLETOP_INTEGRATION.md and play-entry timing changes were already present. They were captured under `Baseline/` with hashes and status. The hand-grab setup and timing changes are untouched; tests were amended only for the new dimensions and relevant checks. The launch scene/prefab were compared against the starting copies, not against HEAD alone. Original table/terrain/Startup trees and both original Blender sources are recoverable under `Baseline/`; do not restore that whole tree over later tasks. The old art FBXs, original source locations and original mesh assets remain available. Critical new source/evidence/handoff files are exempted from the broad Deliverables ignore rule; backup trees stay ignored.

Used Meta verify-first and Unity/Blender modern workflow skills. The Blender skill's textbook mount points to an unavailable `/sessions/...` environment; existing local authoring helpers and installed Blender 5.2 were used and validated instead. Current official [MRUK scene-data documentation](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-manage-scene-data) was fetched through connected metavr (page updated 2026-10-08), and installed MRUK 207.0.1 `Core/Scripts/MRUKAnchor.cs:263` and `MRUKRoom.cs:843` confirm the actual Raycast/IsPositionInRoom signatures. No new Meta API was introduced.

metavr device list was empty. Software discovery found XR Operator distribution files and XR Simulator v85.0; the CLI `npx -y metavr xroperator status` reported proxy binary not installed/registered, proxy not running and XR application server inactive. Port 8720 had no listener. These are differing installation/detection states, not a successful Operator session. Meta AI Agent Bridge still reports the pre-existing remote-server port 48735 conflict. Runtime Optimizer, Immersive Debugger and Hands Optimizer were not used for runtime claims. No tool was installed, enabled or launched to bypass the user's testing restriction.

## Remaining authorized-runtime work / later tasks

After explicit authorization, verify seated reach and view coverage, room-floor/support fit for actual eye height, clipping in both eyes, LOD transitions, two-hand input with the narrower rim, and native MR room/plane placement at several confirmed scales. Verify MR/VR mode changes during a live match and whether the tray blocks hand reach. Profile the five-train scene on Quest; editor counts are not performance certification. Multi-client simultaneous gameplay, replication, remote avatars and two-way voice still need real clients.

Task 2: calibrate trains/scenery against this 1.05 m usable land, set a useful finite maximum and inspect train detail. Task 3: replace collider contact with grab-anywhere and implement requested room motion. Task 4: first-person Meta avatar. Task 5: enlarge MR against the recorded old baseline. No final town was generated.
