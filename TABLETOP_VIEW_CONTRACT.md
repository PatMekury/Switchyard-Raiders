# Tabletop view contract — Tasks 1 and 2

Implemented 2026-10-22 in Switchyard Raiders. This supersedes the old rectangular dimensions in TABLETOP_INTEGRATION.md. These are adopted design dimensions, not user-approved measurements or dimensions inferred from Demeo. Subsequent tasks must update this contract and its implementation together.

The executable dimension source is `Assets/SwitchyardRaiders/Startup/Runtime/TabletopViewContract.cs`. All values below are metres at normal personal-view scale. The Blender authoring script reads this file; regenerate and validate assets when changing dimensions. Manufactured profiles, joints and clearance details live in the editable source and must remain consistent with it.

| Contract | Final value |
|---|---|
| Outer frame | **1.300 x 1.300** |
| Clear usable land / inner lip opening | **1.050 x 1.050**, x/z in [-0.525, +0.525] |
| Actual land cartridge | 1.058 x 1.058 x 0.150 |
| Well below returned lip | 1.064 x 1.064; 3 mm clearance per cartridge side |
| Land edge concealed by lip | 4 mm each side; lip underside 2 mm above land |
| Personal presentation origin | Land underside / TownMount |
| Terrain surface datum | y=+0.150 relative to presentation origin |
| Wood rim top / cup lip | y=+0.165 / +0.168 |
| Tray underside | y=-0.035 |
| Authored TownMount | y=0.885 above standalone table origin |
| Authored wood rim top / cup lip | y=1.050 / 1.053 |
| Virtual table local origin | (0,-0.885,0) in personal presentation |
| Normal VR scale / minimum zoom | 1.0 / 1.0 |
| Normal MR scale / minimum zoom | Confirmed placement scale / same confirmed scale; existing 0.35–1.0 placement search retained pending Task 5 |
| Near edge horizontal clearance | 0.300 forward of current head yaw |
| Normal centre distance | outer half-depth 0.650 + clearance 0.300 = **0.950** |
| Wood rim below eyes | 0.500; presentation origin is 0.665 below eyes |
| Gameplay root | y=0.154, unit scale; the personal presentation owns all view scaling |
| Miniature asset scale | **0.010000 (1:100)** for every authored locomotive; no parent compensation |
| Relative inspection zoom | **1.0 minimum, 6.0 maximum**, continuous; 3.0 is the comparison midpoint |

The 1.30 m choice keeps the existing rolled cup profiles inside 125 mm rails and makes the frame square, reducing outside area from 2.66 to 1.69 m² (about 36%). It retains the oak, brushed steel, dark metal and brass direction. The structural floor and continuous deep well replace the former tabletop slab arrangement. There is no floating skirt. The land uses a new closed quad-grid source; future hills, buildings, bridges, trees and raised track may extend freely above y=.150. No final town is included.

`TabletopViewMath.ReachablePose` uses current horizontal head direction and the new half-depth. Four human-scale seats are at the same 0.95 m radial offset; two seats remain opposite. Their separate sibling root follows the normal home pose and never the inspection scale. TownFootprint corner anchors bound the usable opening; TerrainDatum is available on the land prefab. Personal presentation remains separate from authoritative `RaidersMatch` values and the tracked rig.

VR and MR use the same tray/land meshes. `RaidersTable.UpdateTableVisibility` hides only `Supports` in MR and at the existing VR support-hide threshold (1.6×). The structural tray stays visible. MR placement validates the whole outer frame with 15 mm physical edge clearance, not only the green land. The presentation origin is set to physical tabletop height + 0.006 - TrayBottom*scale, so the tray underside is 6 mm above that plane. Anchored resizing corrects origin height to keep that underside on the plane. The preview outline also follows the underside plane.

Task 2 replaces the temporary fit scale and the provisional 12x cap. `TabletopRailway` adapts the unchanged 32-unit simulation to the calibrated miniature railway. Task 3 replaces contact-only input with two tracked palm grabs anywhere and rigid VR room motion. The attached effects, land, tray, railway and vehicles share one uniform personal-view branch. Human seats, the room, tracked rig, hands and head are outside it.

Task 5 must use `Deliverables/DemeoRework/BASELINE_BEFORE_REWORK.md`. There was no live MR session. The old configured full-scale land was 1.50 x 1.05 m; old confirmed scales could range .35–1.0. Do not mistake the new 1.05 m square land for the user's pre-rework MR baseline or stretch this square frame to satisfy a rectangular target.

Assets retain their existing public prefab paths and GUIDs: `PlayTable/Prefabs/SR_PlayTable.prefab`, `TerrainBase/Prefabs/SR_TerrainBase.prefab`, and `Startup/Prefabs/SR_TabletopPresentation.prefab`, beneath `Assets/SwitchyardRaiders/`. New imported geometry is under `RecessedTable/Art` and `RecessedTable/Meshes`. Editable Blender sources and the regeneration script are under `Deliverables/DemeoRework/`. See `01-handoff.md` for validation, recovery and runtime follow-up.


## Task 2 miniature-world contract

Measured from the CURRENT imported meshes and saved hierarchy, before edits: company 14.800 m authored length; player types 14.795 m; effective presentation scale .003680 (about 54.46 mm engines). Task 1's .680 railway root was compensated by .0054117646 locomotive scale. Current engines are **2.7174 times their previous displayed linear size**, measured rather than based on the prompt's old .004 estimate.

| Element | Normal view | At 3x | At 6x |
|---|---:|---:|---:|
| Outside tray, square | 1.300 m | 3.900 m | 7.800 m |
| Usable land, square | 1.050 m | 3.150 m | 6.300 m |
| Company locomotive length | .14800 m | .44400 m | .88800 m |
| Company width / complete mesh height | .03780 / .04359 m | .11340 / .13077 m | .22680 / .26153 m |
| Adler / Falke length | .14795 m | .44385 m | .88770 m |
| Adler / Falke height | .04245 / .04127 m | .12735 / .12380 m | .25470 / .24760 m |
| Rail inside-face gauge | .01435 m | .04305 m | .08610 m |

The adopted circular centerline radius is .440 m (44 authored metres). `TabletopRailway.Point` preserves the old fitted loop's 0–.0612 m rise as a smooth two-crest grade; its supporting formation reaches the terrain. Rails have modeled foot/web/crown profiles and .00165 m head width. Railhead top is +.003 m over the path; game root is +.154 above the presentation origin. The track source is `Startup/Editor/TabletopScaleTask2.cs`; two mesh LODs are under `RecessedTable/Railway/`. This calibration loop is not a final town layout.

Authoritative distances, `Length=32`, `Spacing=.65`, speeds, cargo capture/fire/delivery windows and height-based loose-cargo physics stay unchanged. The circumference maps to .0863938 displayed metres per simulation unit in plan. A wagon pitch is **.056156 m**, with slight 3D arc variation on grades. Freight bodies are .049 m long, .026 m wide; cargo load .043 x .024 x .024 m; coupler height .0105 m above railhead. Running gear has four wheels and two LODs. These remain calibration freight proxies, not finished period freight art.

A locomotive center is **+.531543 simulation units** ahead of its existing simulation distance, keeping the simulation's half-pitch coupling reference behind the long engine. Wagons retain their authoritative center distances. This avoids changing network units or adding a second coupling simulation. Chord-based placement seats the .083 m player bogie span and .037 m steam driving axle span on the curve. Player bogie yaw and steam pilot steering follow the path; the existing distance driver retains exclusive wheel/rod ownership. Coupling links join existing engine anchors and new freight anchors; maximum measured link length .005576 m. Eight decouplers remain at the same simulation sites, .057 m outboard on supported plinths; visual aim points toward the actual simulation firing target on the new path. Mechanism head .015 x .022 x .028 m, in the same miniature scale.

## Scale ownership and lifecycle

`RaidersTable.presentation` is the only changing scale owner. `terrainMount`, land and gameplay root have unit local scale; locomotives convert authored metres once with .01. Normal VR is 1; normal MR is its confirmed placement scale `s`. The relative zoom is `presentation.scale / normal`, clamped to **[1,6]**. Thus confirmed MR s=.6 permits physical scale [.6,3.6], subject to surface-fit rejection while anchored; entering exploration allows inspection above the surface-fit limit. Task 5 may change normal MR sizing, but must preserve this relative rule.

`RaidersTableManipulation` rebases hand samples to the accepted transform after every update, including clamp overshoot and rejected anchored movement. Closing and reopening cannot accumulate a long reverse dead zone. The existing 8 cm separation guard avoids unstable division when hands nearly meet. Regrab begins at the accepted view; reset is exactly normal. Save/restore, mode return, external scale sanitization and `AcceptView` enforce both bounds. No camera FOV manipulation or inverse train scale exists. Rotation remains yaw-only. The existing vignette and input eligibility remain intact.

`Human scale player seats` is a sibling of the presentation, referenced by `RaidersTable.humanPresentation`. It follows the normal home pose at unit scale. Existing avatar components, seat references and networking stay intact. The reusable miniature prefab no longer embeds human seats; `RecessedTable/Railway/SR_HumanSeats.prefab` retains the reusable seat source. Attached smoke uses custom simulation space = gameplay root and hierarchy scaling. Personal manipulation does not change match state or other clients' views.

## Future scenery size guide

Author future scenery in full-size metres, then apply `.01` once beneath gameplay (or author directly in miniature metres with unit scale). Never apply both conversions. Do not rescale humans in the remote-avatar branch.

| Future item | Authored size | Normal miniature | 6x inspection |
|---|---:|---:|---:|
| Adult character height | 1.80 m | 18 mm | 108 mm |
| Typical doorway | 2.1 m high / .9 m wide | 21 / 9 mm | 126 / 54 mm |
| Storey height | 3 m | 30 mm | 180 mm |
| Small house footprint | 8 x 6 m | 80 x 60 mm | 480 x 360 mm |
| Tree height | 8–15 m | 80–150 mm | 480–900 mm |
| Rail overbridge clear height | at least 5 m above railhead | at least 50 mm | at least 300 mm |

Reserve a conservative clear rail corridor of 25 mm either side of centerline and 50 mm above railhead, then validate actual swept bounds, smoke, bridges and character animations. Wheels, couplers, cab seats and VFX anchors retain authored proportions. A 1.8 m person is about 12.2% of an engine's length. Inspection focus and personal translation can bring a character closer; do not independently enlarge it. No final scenery was generated. Existing PERFORMANCE_AND_ART_MEMORY.md budgets still apply.

## Evidence and limits

See `Deliverables/DemeoRework/02-handoff.md` and `Task2Evidence/zoom-comparison.jpg`. The fixed camera/lens captures give engine screen-envelope widths 9.5%, 30.2%, 70.5% at 1/3/6x; heights 8.8%, 26.5%, 53.2%. All use LOD0 for direct detail comparison, not an automatic-LOD or performance claim. The camera and selected engine focus remain fixed while the entire board is uniformly transformed about that focus. These Task 2 captures use a selected engine focus for comparison. Task 3 instead captures a terrain pivot below the two palms, clamped to the usable land; its gesture may combine translation and scaling.

The 6x limit gives useful cab/mechanical inspection without an unbounded range. It is a project design choice informed by the supplied video's overview/close-view contrast, not a measured Demeo ratio or implementation claim. Actual headset comfort, stereo clipping, native MR anchoring, automatic LOD changes, input tracking and performance remain to be tested after authorization.


## Task 3 manipulation and room-motion contract

The only gesture writer is `RaidersTableManipulation.ProcessHands`, through `RaidersTable.ApplyPersonalView`. Lifecycle placement/reset remains in `RaidersTable`; the old ISDK `ITransformer`, rim proxy, Rigidbody, Grabbable and its four trigger colliders are removed. `TabletopHandGrabSetup.Configure` now binds existing `HandGrabAPI` components on the verified LeftHand/RightHand nodes and the existing `SR_VirtualRoom` root. It preserves the existing hand contact interactors for other objects and updates the launch scene and prefab in place. Do not reinstall the old proxy or attach another transformer to `presentation`.

Both real tracked hands must have valid connected high-confidence ISDK data, valid root poses, high-confidence middle/ring/pinky fingers, OVR tracking/high confidence, and no system gesture. `HandGrabAPI.HandPalmGrabbingFingers()` supplies the SDK's palm curl classification/hysteresis. Middle, ring and pinky must all be closed on BOTH hands; thumb/index are unrestricted. A thumb/index pinch alone cannot substitute. `GetPalmCenter()` supplies world-space positions. There is no contact, proximity, giant collider, controller fallback, special pinch or Move Table arming control.

State progression is `AwaitOpen -> Ready / OneHand -> Acquiring -> Moving`. Startup, tracking/confidence/system-gesture loss, application/OVR focus loss, pause, reset and mode changes require both hands open before a new gesture. During ordinary release, the still-closed hand may remain closed; reacquiring the other hand captures a fresh baseline. One hand never moves the view. Acquisition waits 0.10 s, restarting for a >25 mm sample motion or horizontal span below 80 mm. It captures the latest pair and accepted view, with zero transform change on acquisition. Opening either palm freezes movement immediately. Near-coincident hands suspend yaw/scale calculation; translation still requires both closed valid hands.

**Pivot:** at acquisition, project the palm midpoint into the tabletop's local space, clamp x/z to [-0.525,+0.525], set local y=.150, and retain that local point for the whole grab. It is always on the usable terrain surface, even for off-table grabs. Scale and yaw use horizontal palm separation, not 3D hand distance. Horizontal translation is the midpoint delta. Vertical translation is only the common same-direction part of both hand deltas: sign(dyL) * min(abs(dyL),abs(dyR)) when dyL*dyR>0, otherwise zero. Asymmetric hand heights, one lifted hand, and opposing hand-height changes cannot implicitly lift the pivot. Deliberate common vertical movement remains available in VR and MR exploration; anchored MR keeps its physical underside plane.

For accepted scale ratio k, yaw Y, common translation T and world terrain pivot P:

- Table origin: P + T + Y*(oldOrigin-P)*k; table rotation: Y*oldRotation.
- VR environment origin: P + T + Y*(oldRoomOrigin-P); room rotation: Y*oldRoomRotation; local and world scale stay (1,1,1).
- The table origin can change height to compensate for enlarged geometry below P. P itself does not drift vertically during pure yaw/scale. This compensation never becomes room translation.
- In MR, no room transform is written. Passthrough, tracking origin, physical head/hands and human avatar presentation remain outside the scalable branch.

`environmentMotion` references the existing unit-scale `SR_VirtualRoom` sibling; no wrapper rig, manager or runner is added. `RoomChoiceMenu` positions that room only on initial VR selection; `RaidersTable` then owns its personal motion/reset. The hidden VR room pose is retained across MR, and reset restores the captured room home consistently with the recentered table home. Human seats still follow normal home at unit scale as Task 2 defined; Task 4 must preserve tracking ownership when adding the local avatar.

Normal-relative zoom remains **[1,6]**, continuous. Every moving sample rebases to the accepted view and latest hand pair, even after clamp overshoot or rejected physical-table fit. Immediate reverse does not consume accumulated overshoot. Both lifecycle and gesture paths clamp. MR physical-fit constraints are intentionally unchanged pending Task 5; exploration permits zoom beyond the physical surface. Task 5 must retain the original pre-rework MR baseline and must not scale the room or rig.

**Arbitration:** the pending two-palm candidate, active move and 0.25 s release guard block gameplay and Raiders menu commands. The command boundary rechecks current palm classification to avoid relying only on LateUpdate ordering. Gesture epochs cancel UI clicks begun before a gesture/release even if their click arrives after the guard. Ordinary one-hand menu pointing/pinching remains available; the menu follower is held only while the pair is acquiring/moving. No global UI-intercepting collider or permanently disabled interactor is used. Programmatic room/lifecycle recovery remains callable.

Validation: 59 new synthetic Edit Mode assertions, 58 scale/train-fit assertions, 64 integration assertions and 17 authoritative-rule assertions pass. 720 repeated yaw/zoom samples measured maximum terrain-height drift 7.75e-7 m. Injected official IHand/IFingerAPI data verifies adapter gating, not real-world recognition. See `Deliverables/DemeoRework/03-handoff.md` and `validation-task3.txt`. No Play Mode or device session was started. Live palm recognition, simultaneous menu arbitration, tracking interruptions, seated comfort and VR/MR behavior still require authorized runtime testing.


## Task 4 human-avatar presentation contract (2026-10-23)

`RaidersAvatars` owns local gameplay presentation independently of Photon. Entering `Playing` (developer solo, practice, or online) requests the existing legitimate Meta Platform initialization -> entitlement -> logged-in user -> access-token path. Solo creates no runner and fabricates no identities. Simulated board slots remain explicitly labeled SIMULATED; their markers are not authenticated avatars. Online transport still obtains its own fresh proof and verifies remote identity at the existing session boundary.

The local `RaidersAvatarEntity` is a unit-scale child of the existing physical `trackingSpace`, outside both `presentation` and `environmentMotion`. It uses SDK `FirstPerson` load/view flags, Half manifestation and Body|Outfit submeshes; head, face, eyes, hair and head accessories are excluded before native creation. SDK default-model loading is disabled, so an account failure cannot become an unlabeled preset. Human body proportions are never multiplied by the 1:100 miniature conversion or 1�6 inspection zoom.

`RaidersAvatarInput` registers the SDK 40 delegated input/hand providers and reads the existing ISDK `IHand`/`IHmd` sources. Installed ISDK 207 exposes OpenXR hand joints; the bridge uses Meta's official OpenXR 17-avatar-joint mapping and per-hand wrist/bone coordinate conversions. World poses enter physical tracking-local space once, including a rotated/recentered rig. This bridge does not write any input transform or disable any hand data, palm classifier, HandVisual or UI interactor.

States are Menu -> Identity -> Loading -> Ready, with TrackingPaused and explicit Fallback. The local account-loaded callback AND valid head/both-hand confidence, root and joint data are required before replacing outlines. Only the four existing OVR/OpenXR hand mesh renderers have `forceRenderingOff` changed; previous flags are restored on tracking loss, failure, gameplay exit or disable. UI cursors, pinch arrows and vignette are excluded. During loading/unavailability the existing temporary hands remain usable, with visible status. Match menu -> Table view / comfort -> Avatar status / retry exposes the reason and retry. No authenticated-success claim is made for the temporary visuals.

Remote entities remain ThirdPerson/Full under the existing unit-scale human seats at normal home, with sender origin/yaw normalization and no local input driver. Seat changes, vacated slots and match exit dispose the appropriate remote entities. Streaming retains a non-null `Array.Empty<byte>()` buffer; solo skips native snapshot/transport work. One existing avatar-manager prefab is instantiated on demand, or an existing manager is reused; the owned service stays warm between matches/retries and only owned resources are destroyed with the service. Late asynchronous completion cannot recreate an avatar after exit.

Task 4 validation: 61 synthetic Edit Mode assertions; prior 59 manipulation, 58 scale, 64 integration and 17 rule assertions rerun, plus flow configuration validation. No authenticated native avatar was loaded or rendered in this session. Live first-person appearance, alignment, hand-input coexistence, VR/MR/recenter transitions and remote pose fidelity require an authorized account/runtime test. See `Deliverables/DemeoRework/04-handoff.md`. Task 5 must leave both human-avatar branches at physical scale.
