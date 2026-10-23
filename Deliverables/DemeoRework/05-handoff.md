# Task 5 handoff: enlarged mixed-reality board

Implemented 2026-10-23 in `C:/Users/patmekury/Documents/Unity/Switchyard Raiders`, starting at HEAD `c0a9e67`. Tasks 1–4 handoffs and final contract were used. The pre-existing change to `Deliverables/RoomMenu/play-entry-timings.txt` was preserved. No commit, stash, reset, Unity restart, Play Mode, headset launch, player build or installation was performed.

## Delivered result and calibration

The request means approximately **twice the previous displayed linear width and depth**, therefore **four times footprint area**, not twice area. A previous actual MR display measurement was unavailable. The recorded baseline in [BASELINE_BEFORE_REWORK.md](BASELINE_BEFORE_REWORK.md) is calculated: old visible land 1.50 x 1.05 m multiplied by the old confirmed placement scale s. Its automatic fit search allowed s=.35–1.0; no specific successful device value was recorded. The old table was hidden in MR.

The adopted default is explicitly calculated s=1. `RaidersRoom.previousMRReferenceScale` is exposed in the existing `SR_Launch` scene and reusable prefab, labeled as calibration rather than measured data. If an actual previous s is later recovered, change this one field and re-confirm placement. Invalid nonfinite input defaults to 1; finite input clamps to the historical .35–1 range.

Doubling both old axes first gives a 3.00 x 2.10 m rectangular target. Task 1's square assets are retained without stretching, using the area-equivalent square, sqrt(3.00*2.10)=**2.509980 m** usable side. This shape compromise is **1.67332x old width and 2.390457x old depth** (16.3% narrower and 19.5% deeper than the doubled rectangle). It is exactly four times old area and twice its geometric-mean linear size; it is not claimed to be exactly twice each old axis.

The executable formula is `B(s)=2*sqrt(1.50*1.05)/1.05*s=2.390457*s`. The sole dynamic scale owner remains `RaidersTable.presentation`, whose scale is **B*q**, relative zoom q in **[1,6]**. The redesigned base tray is 1.30 m square, land 1.05 m square. Task 2's .01 authored-engine conversion, unit gameplay root, curved railway, wagon pitch, device positions and effect scaling remain unchanged. No second MR factor appears in those descendants. VR normal remains 1.

| Calculated default MR | Normal q=1 | Intermediate q=3 | Maximum q=6 |
|---|---:|---:|---:|
| Absolute presentation scale | 2.390457 | 7.171371 | 14.342742 |
| Outside tray width/depth | 3.107594 m | 9.322783 m | 18.64557 m |
| Usable land width/depth | 2.509980 m | 7.529940 m | 15.05988 m |
| Company engine length | .353788 m | 1.061363 m | 2.122727 m |
| Future 1.80 m scenery character | .043028 m | .129085 m | .258169 m |

Company engine/land-side ratio remains .140952. The before engine was measured at 14.80001 authored metres with effective .00368 scale, giving .054464 m. The after comparison includes both Task 2's corrected miniature ratio and Task 5's whole-board enlargement, each applied once. Other locomotives, rail gauge, curvature, couplers, wagons, devices, attached effects and future scenery share B*q; no final town was generated.

## Placement, reach and lifecycle

The existing permission and room-load path is unchanged. Head containment in a loaded native room remains required. Initial **Use your room** seeks a full-size suitable TABLE; when none fits, it offers **Free MR preview. Not attached to furniture.** The existing second click confirms either mode. Cancel, retry, out-of-room recovery and VR fallback remain available. Calibration changes invalidate old confirmation rather than multiplying an old saved baseline again.

Surface-constrained mode retains the 49-point full outer-footprint and obstruction checks, 15 mm edge allowance and underside 6 mm above the table plane. The previous shrinking search is removed. The ray-test origin now uses the actual scaled underside plane, so scales above 1 do not incorrectly fail a fixed ray distance. A proposed surface view must also keep wood rim at least .25 m below the head. For a 75 cm physical table the new normal rim would be 1.234091 m: too high for the synthetic seated head at 1.30 m, even on a sufficiently wide table. That case offers free MR. A 1.60 m head and clear 5 m table pass the synthetic surface case. These are calculated clearance tests, not comfort observations.

Free MR normal/recenter uses displayed half-depth plus .30 m, so centre distance is **1.853797 m** and near rim is **.30 m** ahead of current horizontal head direction. Rim is .50 m below eyes unless floor clearance takes precedence. For head=(0,1.30,0), floor=0, facing forward:

| Datum | World height |
|---|---:|
| Presentation origin | .405575 m |
| Terrain surface | .764143 m |
| Wood rim | .800000 m |
| Tray underside | .321909 m |

Normal underside has at least .06 m floor clearance. Floor uses the loaded room's floor anchor, otherwise room origin. Whole-board reach is not claimed: the board is intentionally large, and Task 3's grab-anywhere translation plus recenter brings content to the player.

Free inspection is not collision-constrained to furniture or floor. Task 3 keeps its terrain pivot vertically stable during yaw/scale; enlarged geometry below that pivot can cross the real floor, and deliberate vertical movement can do the same. At maximum the illustrative tray extends below the synthetic floor. Recenter restores normal clearance. No silent floor clamp lifts the terrain, no physical anchoring is claimed for free MR, and no real-room transform is changed.

**Match menu -> Table view / comfort** distinguishes:

- **Switch to free MR:** release surface constraint without changing accepted pose/zoom.
- **Recenter table in front:** cancel pending preview, stay free, restore reachable normal B and q=1.
- **Try scanned table -> Confirm on table:** explicitly preview/re-confirm a suitable full-size surface. Failure leaves the existing free view. Leaving this menu or recentering cancels the pending preview.
- Surface reset returns to confirmed normal placement. Surface movement/zoom rejects footprint overflow or insufficient rim clearance. Free MR permits the full [1,6] range.

Accepted MR pose is saved relative to confirmed home and q is saved separately. In-session VR/MR return restores only against the same confirmation version, clamps both bounds and revalidates a surface pose. No disk persistence was introduced. Task 3 remains the sole gesture writer; off-table two-palm acquisition, one-hand rejection, release/regrab, tracking suspension and immediate clamp reversal remain intact.

Local avatar remains a unit-scale child of physical trackingSpace. Human remote-seat locations use enlarged normal half-depth, never inspection q; their feet are placed at the loaded room floor. Avatar transforms remain unit scale and remote pose transport is unchanged. These are configuration/synthetic alignment checks, not a successful authenticated avatar rendering test.

## Evidence

- [Before/after comparison](Task5Evidence/mr-before-after.jpg): actual archived pre-Task1 land and original train sizing versus current tray/terrain/railway/trains, top and seated views.
- [Normal/intermediate/maximum](Task5Evidence/mr-zoom-stages.jpg): q=1/3/6 with one fixed seated camera, lens, lighting and terrain pivot.
- [Raw measurements](Task5Evidence/measurements.txt), plus full-resolution PNGs in the same directory.

Every sheet labels **synthetic Edit Mode, calculated baseline, not observed MR**. Before seating uses an illustrative .75 m physical-table plane and 1 m centre distance; it is not a recovered headset pose. The before/after top camera is identical. The seated camera is fixed at (0,1.3,0), vertical FOV 65 degrees, Euler (30.7695,352.2899,0). The new home is (0,.405575,1.853797). Zoom scales the complete board about a terrain point under the selected engine. There is no inverse train scaling or FOV change.

Projected engine width/height envelope at 1/3/6x is **17.22/15.40%, 66.59/47.69%, 316.26/162.05%**. Maximum is mechanical-detail inspection: the whole engine exceeds the frame, and the user translates to inspect the cab or another part. These actual-asset views justify retaining Task 2's finite 6x limit; they do not establish headset stereo comfort, near clipping or performance. LOD0 was forced for comparable detail.

`TabletopMRTask5.Render()` uses isolated prefab contents and URP Camera.Render; no PreviewRenderUtility. Archived old prefab copies receive temporary unique GUIDs, use original retained meshes, and are removed after rendering. It does not modify production assets or the open scene. Reproduction requires the local ignored Task 1 `Deliverables/DemeoRework/Baseline` archive; finished evidence is included. `compose_task5_evidence.py` only assembles and labels those actual renders.

The supplied `scene design/Demeo  Scaling.mp4` and sampled `zoom-sequence.jpg` / `video-contact-sheet.jpg` were inspected, including the 23–31s and 48–57s overview/close-view ranges, with additional source frames at 31s and 57s. They inform view contrast and presentation feel only. No exact Demeo dimension, gesture algorithm or internal implementation is inferred.

## Validation and preservation

**317 assertions pass**, plus existing flow configuration checks:

| Suite | Assertions |
|---|---:|
| Task 5 MR calibration/lifecycle/placement/synthetic gestures | 58 |
| Task 3 manipulation | 59 |
| Task 2 scale/train fit | 58 |
| Tabletop integration | 64 |
| Authoritative rules | 17 |
| Task 4 avatars | 61 |

[Task 5 log](validation-task5.txt) covers enlargement math and single ownership, no shrinking, free/strict distinction, cancelled/confirmed previews, room loss, calibration invalidation, reach/height/floor datum, off-table two hands, one-hand rejection, release/regrab/tracking loss, immediate clamp reversal, normal reset, mode/saved-view bounds, unit room/head/hand/avatar transforms, human seats, and actual-font status text fit. Native `IsPositionInRoom` is substituted only in Edit Mode by a private bounded fixture; player builds exclude the seam. Synthetic MRUK anchor raycasts use the installed managed API. No native room scan or live palm classification is represented.

[Preservation audit](validation-task5-preservation.txt) compares the pre-edit Task5Baseline. All other existing Startup files remain byte-identical, including authoritative simulation/session/identity/voice, avatars, Task 3 input state machine, room-selection permissions, railway and train animation adapters. Scene retains all 1856 object IDs; prefab retains all 1851, with zero additions/deletions and only the calibration field added. No new rig, runner or manager. No production mesh/material/LOD/animation/package changes. The pre-existing timing-file edit remains untouched. Local recovery snapshot is `Task5Baseline/Startup` plus original contract/integration/gitignore/status files.

Source changes:

- `Assets/SwitchyardRaiders/Startup/Runtime/TabletopViewContract.cs`: MR calibration and rim clearance.
- `TabletopViewMath.cs`: scale-aware reachable placement and normal seat radius.
- `RaidersRoom.cs`: explicit free/surface preview and confirmation, single baseline, strict full-size fit, floor query, invalidation/recenter.
- `RaidersTable.cs`: MR view memory, reset/mode/constraint transitions, surface acceptance and physical human seats.
- `RaidersMenu.cs`: clear mode/preview/recenter actions and cancellation.
- `Assets/SwitchyardRaiders/Startup/Scenes/SR_Launch.unity` and `Prefabs/SR_Launch.prefab`: exposed previous-scale reference 1.
- `Assets/SwitchyardRaiders/Startup/Editor/TabletopMRTask5.cs` and `TabletopMRTask5Review.cs`: repeatable guarded Edit Mode checks/evidence.
- Shared `TABLETOP_VIEW_CONTRACT.md`, integration guide, this handoff, logs/evidence and gitignore exceptions.

Editor menu entries under **Switchyard Raiders -> Demeo Rework** are **11 - Save MR baseline calibration**, **12 - Validate enlarged MR**, and **13 - Render calculated MR comparison**. Validation methods also run directly through connected Unity tools. Apply refuses an unrelated project, Play Mode, dirty scene or unexpected calibration before saving existing scene/prefab fields.

## Official APIs, tooling and remaining runtime work

Meta's `hz-quest-verify-first` skill was applied. Connected Unity editor verified Switchyard Raiders, Unity 6000.6.0b5, Android target, saved `SR_Launch`, Edit Mode and successful project compilation before edits and after checks. Official metavr documentation and CLI were used for discovery/status; no device was connected. XR Operator reported binary not installed, proxy not running and API server inactive at 127.0.0.1:8720. Simulator/Operator integration presence is not a running session. No live data from Runtime Optimizer, Immersive Debugger or Hands Optimizer was used. Existing Agent Bridge server conflict on port 48735 remains; final console also contains a Unity AI Account API timeout warning. Neither is a C# compilation failure. No port owner was killed or Unity restarted.

Official [MRUK scene management](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-manage-scene-data), updated 2026-10-08, was checked through metavr against installed MRUK 207.0.1 source. Installed signatures are authoritative for this checkout: `MRUK.cs` LoadSceneFromDevice at line 1050; `MRUKAnchor.cs` Raycast at line 263; `MRUKRoom.cs` IsPositionInRoom at line 843 delegates native containment. Package path is `Library/PackageCache/com.meta.xr.mrutilitykit@84bc45ca9f62`. Existing permission/load code is preserved; no obsolete schematic documentation signature was introduced.

Still requires explicitly authorized runtime/device/account testing:

1. Native permission grant/deny/cancel, actual saved-room containment/floor loading, loss/recovery, surface preview cancellation and free preview visibility in passthrough.
2. Actual displayed scale and seated comfort/reach on device; free versus suitable physical-table placement, floor/furniture overlap at inspection, stereo clipping, readable status/recenter controls.
3. Both-hand grabs in empty air with real confidence/system gestures, one-hand rejection, tracking/focus interruption, reset and mode switching, immediate reversal at both zoom limits.
4. Authenticated first-person avatar alignment through recenter and mode changes, no duplicate hand visuals, human remote-avatar floor placement and unchanged pose streaming.
5. Live two/four-player simultaneous play, six-delivery 2v2, replenishing freight, voice, train animation/VFX/automatic LOD behavior, Quest CPU/GPU performance.

Final read-only editor check: correct Switchyard Raiders Assets path, SR_Launch saved/clean, Playing=False, Compiling=False, CompilationFailed=False. `git diff --check` passes; temporary evidence imports are absent. Implementation, synthetic validation and evidence are complete. Passthrough, native scene validation and authenticated avatar loading are not claimed from Edit Mode.
