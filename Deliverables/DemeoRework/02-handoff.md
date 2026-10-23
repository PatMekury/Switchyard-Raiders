# Task 2 handoff — coherent miniature scale and useful inspection zoom

Implemented and validated in **C:\Users\patmekury\Documents\Unity\Switchyard Raiders**, using the connected Unity 6000.6.0b5 editor. SR_Launch stayed in Edit Mode. No Play Mode, headset launch, Unity restart, player build or install was performed.

## Adopted result

The 1.30 m square tray and 1.05 m usable land from Task 1 remain. All five authored engines now use **1:100** miniature scale, about **148 mm long**, versus the measured 54.46 mm before this task. The gameplay root is unit scale. The detailed locomotive meshes, PBR materials, clips, controllers, cab anchors, VFX anchors and LOD meshes are retained.

Inspection is smooth and continuous from **1.0 to 6.0 relative to normal**, with 3.0 used as the intermediate comparison. This is an implementation choice, not a user-approved measurement or a recovered Demeo ratio. At the fixed camera, the company engine occupies about 9.5%, 30.2% and 70.5% of image width at these stages. The cab, chimney, trim and mechanical details are visible at maximum. Reset, saved-state restoration, mode return, regrab and the view-acceptance boundary enforce the range. Accepted-transform rebasing prevents clamp overshoot from creating a long reverse-gesture dead zone.

**The shared current contract is [TABLETOP_VIEW_CONTRACT.md](../../TABLETOP_VIEW_CONTRACT.md).** It contains physical sizes, all scale ownership, MR-relative rules, coupling/path mapping, future scenery ratios and later-task constraints. The executable source is `Startup/Runtime/TabletopViewContract.cs`.

## Railway and rolling-stock calibration

- Replaced the broad ribbon with a modeled rail pair: 14.35 mm inside-face gauge, 1.65 mm crowns, webs/feet, sleepers and a solid graded formation. The .44 m radius circular layout suits the square land. The prior fitted 0–61.2 mm rise is retained; the formation and device plinths reach the terrain.
- `TabletopRailway` maps the unchanged 32-unit simulation into presentation metres. Freight pitch remains .65 authoritative units, now 56.156 mm in plan. Engines are placed .531543 units forward of their existing coupling reference. This accommodates their 148 mm length without enlarging the cargo spacing or changing gameplay rules.
- Re-authored freight running gear has four wheels, two LODs, 49 x 26 mm body dimensions and coupling anchors at the locomotives' 10.5 mm coupling height. Existing cargo identity markers remain. These are calibrated freight proxies; finished period wagon artwork remains future work.
- Player bogies articulate along the new curve. Steam pilot axles receive track-relative steering offsets inside the existing motion driver. The wheel/rod distance driver remains exclusive with Animator playback. The imported source meshes and clips were not altered.
- Coupling links use the authoritative owner/order/loose-chain relationships and bridge the short curve/grade gaps. Device sites and aim target the unchanged firing-window distances; no second coupling or firing simulation was added.
- `Human scale player seats` is a separate sibling outside the zoom branch, anchored to normal home seating. Existing avatar/voice/session references remain. The tracked rig, head, hands and environment are outside the miniature branch. Smoke uses the gameplay root's custom simulation space and hierarchy scaling.

## Actual-asset evidence

![Fixed-camera scale comparison](Task2Evidence/zoom-comparison.jpg)

Individual labeled images: [normal](Task2Evidence/labeled-1x.jpg), [intermediate](Task2Evidence/labeled-3x.jpg), [maximum](Task2Evidence/labeled-6x.jpg). Additional [whole railway fit](Task2Evidence/railway-fit.png), [company coupling detail](Task2Evidence/company-coupling-detail.png), [player gauge detail](Task2Evidence/player-gauge-detail.png), and [camera/measurement record](Task2Evidence/measurements.txt).

These are actual production prefab meshes rendered by an isolated Unity URP camera in Edit Mode. The three comparison images share the same camera pose, 65-degree vertical FOV, lighting and selected engine focus. The entire board transforms uniformly about that focus; there is no inverse train scaling or camera trick. LOD0 is forced equally to compare authored detail; these are not automatic-LOD, headset or performance captures. Manual skinning matrices are refreshed per render as required when capturing several transforms in one editor update. PreviewRenderUtility was never used.

Reviewed the supplied Demeo reference sheets and extracted frames in ReferenceInspection/, especially the overview and close-range sequences around 23–31 and 48–57 seconds, including 28 and 51 seconds again for this calibration. The reference supports a meaningful contrast between overview and close inspection, not exact dimensions or knowledge of Demeo's implementation. Switchyard Raiders retains its oak/brass/steel frame and detailed railway art. No town was generated.

## Validation

[validation-task2.txt](validation-task2.txt): **58 Task 2 assertions pass**, including:

- Both clamps, large overshoot, immediate reversal, stationary clamped pivot, regrab, reset, stale saved states, nonfinite scale sanitization and relative MR limits.
- All five oriented engine envelopes across 256 loop samples stay at most .464738 m from the center in x/z, leaving over 60 mm to the land edge. Player axle center/height discrepancies are at most .191/.312 mm. Steam pilot center/height errors are below .001 mm in this numerical check.
- All player and company coupling links remain at most 5.576 mm. Conservative first-wagon body clearance is at least 3.271 mm; ordinary wagon bodies have over 6 mm pitch clearance. The test is geometric, not a new collision simulation; the existing game can still place independently driven engines at the same distance.
- Forward/reverse/stop adapter behavior, wheel phase, rod motion, driver exclusivity, zero wheel travel caused by board manipulation, common VFX scale ownership, unchanged match snapshot under zoom, no missing scripts and one rig/session.
- Generated mesh triangles are nondegenerate and have normals/UVs. Track LOD0 is 28,512 triangles; LOD1 14,304. Freight running gear is 316/188 triangles per wagon plus the existing cargo box. These are asset counts, not Quest frame-time certification.

Existing regressions also pass: **64 integration, 28 contact/placement, 17 gameplay-rule assertions**, service/voice wiring and room-menu scene validation. [Integration report](Task2Evidence/integration-validation.txt), [contact report](Task2Evidence/contact-validation.txt). They cover two/four-player simultaneous operation, six-delivery 2v2, gradual replenishing freight, snapshots, mode/placement and singleton wiring without authenticating external services.

The original [player asset/animation validation](Task2Evidence/player-animation-validation.txt) reports **zero failures**. [Company controller validation](Task2Evidence/company-animation-validation.json) passes actual imported-clip comparison at ten poses, forward/reverse symmetry, pause and exclusive drivers. Both were run in an isolated additive Edit Mode scene, which was then closed. This does not replace a running XR test.

## Files, regeneration and preservation

- Runtime: `TabletopViewContract.cs`, `TabletopRailway.cs`, `TabletopViewMath.cs`, `RaidersTable.cs`, `RaidersTableManipulation.cs`, `CompanyTableFollower.cs`, `PlayerVehicleTrackFollower.cs`; the small pilot-steering extension is in `CompanyLocomotiveMotion.cs`.
- Editable authoring/validation/render source: `Startup/Editor/TabletopScaleTask2.cs`, `TabletopScaleTask2Checks.cs`, `TabletopScaleTask2Review.cs`.
- Generated mesh/material/seat assets: `Assets/SwitchyardRaiders/RecessedTable/Railway/`.
- Updated existing reusable presentation, launch prefab and launch scene. Original locomotive prefab GUIDs and imported art are preserved. Legacy table integration routes through calibration so it does not restore the old railway/scale; the Task 1 migration tolerates seats moving out of its branch.
- Regenerate via **Switchyard Raiders → Demeo Rework → 4 - Calibrate railway and miniature scale** in saved SR_Launch. Then run **5 - Render calibrated zoom stages** and **6 - Validate scale bounds and train fit**. The builder refuses Play Mode, a different project, a different scene or unsaved scene edits. It is targeted and repeatable; it does not regenerate room/menu/network wiring.

[02-baseline.md](02-baseline.md) records measurements taken before Task 2 edits. Task2Baseline/ preserves starting files, including Task 1's work; the older Baseline/ remains recoverable. The old rail ribbon stays disabled in the assets. Do not restore whole backup trees over subsequent work. New handoff/evidence/source files are retained outside the broad Deliverables ignore rule.

[Preservation checks](Task2Evidence/preservation-checks.txt) confirm RaidersMatch, RaidersSession, RaidersVoice, RaidersAvatars, the other tab's hand-grab setup and contact checks are byte-identical to Task 2's start. Session, voice, avatar, launch and room-choice serialized components are unchanged in both launch assets. Only the needed table/presentation/art references were changed. No commit, reset, stash or broad overwrite was performed.

## Tooling and remaining runtime checks

Used the already-read Meta verify-first and Unity workflow skills, connected `metavr` documentation/device tools, and `npx -y metavr xroperator status`. The device list was empty. Operator reported proxy not installed/registered, not running, and application server inactive; previously discovered distribution files are not a live session. XR Simulator v85.0 is installed but was not launched. The existing AI Agent Bridge port conflict remains; Runtime Optimizer, Immersive Debugger and Hands Optimizer were not used for runtime claims.

Verified [Meta's current two-hand interaction documentation](https://developers.meta.com/horizon/documentation/unity/unity-isdk-two-handed-grab-interaction) against installed ISDK 207.0.1 `ITransformer`/`IGrabbable`/`Grabbable` source. Retained the custom transformer and contact wiring; no deprecated two-grab transformer was introduced. The capture-only skinning flag follows [Unity's documented per-render matrix refresh](https://docs.unity3d.com/ja/6000.0/ScriptReference/SkinnedMeshRenderer-forceMatrixRecalculationPerRender.html).

After explicit authorization, test seated comfort, both-eye clipping, real hand tracking at both limits, regrab/mode changes, anchored MR versus exploration, automatic LOD transitions, smoke scaling, all trains moving and reversing, native room loss, multi-client personal views/avatars and two-way voice. Profile Quest 3/3S with the planned scenery/effects load. Editor checks establish neither 90 Hz nor complete live interaction feel.

Task 3 should retain [1,6] relative zoom, accepted-transform rebasing, unit gameplay root and unscaled human seats while adding grab-anywhere/room motion. Task 5 must still use the original BASELINE_BEFORE_REWORK.md for the requested doubling of MR dimensions; do not substitute Task 2's square land for that original baseline. Future scenery uses 1:100 (1.8 m adult = 18 mm normal / 108 mm at 6x) and must be validated against the rail clearance guide. No final town is included.
