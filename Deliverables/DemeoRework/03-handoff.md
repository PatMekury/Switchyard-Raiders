# Task 3 handoff — two-hand grab anywhere and rigid VR room motion

Implemented in **C:/Users/patmekury/Documents/Unity/Switchyard Raiders** on 2026-10-22/23. Unity 6000.6.0b5, Android compilation target. Tasks 1 and 2 final geometry/scale contracts were read and retained. Task 3 implementation and permitted Edit Mode validation are complete; live hand recognition and headset comfort remain unverified.

## Result

Close both hands naturally anywhere, including empty air, to manipulate the personal table view. There is no contact restriction, pinch shortcut, one-hand movement or Move Table arming menu. The existing Meta HandGrabAPI components classify palm closure on the existing tracked hands. Middle, ring and pinky must all be curled on each hand; thumb/index are unrestricted. Opening any required finger on either palm releases the gesture.

The VR room receives the same intentional translation and yaw as the table, but no scale. The existing room root stays at unit scale. MR never writes a room transform. Physical tracking, head/hands, passthrough, human-scale seats, avatar tracking and simulation are outside the scalable content branch. No rig, manager, runner or new interaction collider was introduced.

Task 2 values remain: 1.30 m outside, 1.05 m usable square land, terrain datum +.150 m, normal-relative continuous zoom **1–6x**, engines about .148 m long at normal scale, .95 m seated center placement. The executable dimensions remain in TabletopViewContract.cs.

## Input, pivot and arbitration

RaidersTableManipulation owns an explicit AwaitOpen / Ready / OneHand / Acquiring / Moving state machine. Both hands must have valid connected high-confidence IHand data, valid root poses, high-confidence required fingers, OVR tracking/high confidence, and no system gesture. Application pause/focus and OVR input/VR focus interrupt movement. After an interruption, reset or mode change, both hands must open before reacquisition. Ordinary release permits the other hand to stay closed.

Acquisition waits .10 seconds, restarting after >25 mm per-sample palm displacement or a horizontal span under 80 mm. The latest accepted pose and latest hand pair form the baseline; acquisition itself never moves the board. Release/regrab does not reuse old hand positions. The existing menu follower is held only during acquisition/movement.

The pivot is the palm midpoint projected into the tabletop's local coordinates, clamped to the usable land x/z bounds and placed at terrain y=.150. It stays at that local point throughout the grab. Horizontal palm separation controls yaw and scale. Common midpoint motion controls horizontal translation. Vertical translation is only the same-direction displacement shared by both palms, using the smaller absolute delta; one raised hand or opposing height changes cannot lift the pivot.

Scale/yaw preserve the world terrain-pivot height unless both hands deliberately move vertically. The table origin moves as needed to keep that pivot fixed while the geometry grows. This origin compensation is explicitly excluded from room translation. The previous code scaled the table around the 3D hand midpoint and used 3D separation; the new decomposition removes that height coupling.

RaidersTable.ApplyPersonalView writes the scalable presentation and the separate rigid environment from one accepted gesture delta. RoomChoiceMenu retains its one-time initial room placement. Room home is captured after that placement; reset restores it consistently with the new seated home. The hidden VR room retains its pose across MR. In anchored MR, existing underside-height/physical-fit validation still constrains motion; exploration allows inspection beyond physical-table fit. Task 5 owns changes to MR sizing/placement policy.

At either zoom clamp, invalid small span or rejected MR placement, the latest hands become the next baseline. Reverse movement responds immediately rather than consuming accumulated overshoot.

Pending pair intent, acquisition, movement and the .25-second release guard block gameplay and Raiders menu commands. The command boundary queries current classification too, so it does not rely solely on LateUpdate ordering. Gesture epochs cancel UI presses begun before a gesture/release, including late click delivery after the guard. One-hand menu pointing/pinching remains usable. Existing hand contact interactors remain for other objects; no global ray blocker or invisible giant collider was added. Programmatic room recovery is not gated by a UI guard.

## Files and scene ownership

Under Assets/SwitchyardRaiders/Startup:

- Runtime/RaidersTableManipulation.cs — official hand adapter, state machine and gesture arbitration.
- Runtime/TabletopViewMath.cs — terrain pivot, planar yaw/scale, common vertical movement and rigid room transform.
- Runtime/RaidersTable.cs — environmentMotion reference, unified personal view application, room home/reset lifecycle.
- Runtime/RaidersMenu.cs and RaidersMenuAction.cs — updated instructions, command guard and stale-press cancellation.
- Editor/TabletopHandGrabSetup.cs — migrates existing scene/prefab bindings in place. Binds verified authored LeftHand/RightHand nodes because live Hand.Handedness is uninitialized in inactive prefab contents.
- Editor/TabletopManipulationTask3.cs — reproducible synthetic state, API-adapter, math, command and binding checks.
- Editor/TabletopContactGrabChecks.cs — historical entry point forwards to the superseding Task 3 checks.
- Editor/TabletopScaleTask2Checks.cs, TabletopIntegrationChecks.cs and RecessedTableTask1.cs — remove old transformer/contact assumptions and test the current contract.
- Prefabs/SR_Launch.prefab and Scenes/SR_Launch.unity — existing distinct palm APIs and SR_VirtualRoom bound; obsolete Table rim hand grab GameObject, Rigidbody, Grabbable, HandGrabInteractable, movement provider and four trigger colliders removed.

SR_VirtualRoom is a unit-scale sibling of the miniature presentation. The XR rig is outside both branches. Human seats remain a separate unit-scale normal-home branch per Task 2. Task 4 must preserve that separation when adding a local avatar. Reusable table/land/train meshes and miniature presentation prefab were not changed by Task 3.

TABLETOP_VIEW_CONTRACT.md is updated. TABLETOP_INTEGRATION.md now directs current interaction behavior to that contract while retaining historical validation context.

## Validation performed

Connected Unity tools verified the project before changes and after validation. Final state: SR_Launch saved, not dirty, not playing, not compiling; room and rig scale exactly (1,1,1).

- **59 Task 3 assertions**: off-table acquisition, one-hand rejection, second-hand capture, release/regrab, tracking/focus/pause/mode interruption, asymmetric heights, rotated head/rig orientation, yaw and scale stability, simultaneous XYZ/yaw/scale, room rigid transform, both clamp reversals, reset, MR room immobility and physical-fit rejection, API confidence/system-gesture gates, command suppression/recovery, stale UI clicks, scene/prefab bindings and no ITransformer ownership.
- **58 Task 2 assertions** rerun: zoom lifecycle, current assets, railway fit, locomotive/coupler/wagon clearance, LOD/mesh checks and human-scale branch separation.
- **64 integration assertions + 17 production gameplay-rule assertions** rerun: two/four simultaneous slots, six-delivery 2v2 objective, replenishing freight, readiness/match/rematch flow, serialization, synthetic MR surfaces and existing rig/service/avatar bindings.
- 720 synthetic yaw/zoom samples measured maximum terrain-pivot-height drift **7.74860382e-7 m**.
- git diff --check passed.
- Preservation audit: each launch asset has zero added Unity object IDs, ten removed obsolete proxy objects/components and only five changed existing serialized blocks. 1,841 prefab blocks and 1,846 scene blocks remain unchanged. Session, voice, avatars, launch and room-choice serialized components remain unchanged. Authoritative simulation/session, voice/avatar sources, railway mapping, dimension contract and train follower sources are byte-identical to the Task 3 starting snapshot.

Evidence: validation-task3.txt and validation-task3-preservation.txt alongside this handoff. Task 2 regression output is validation-task2.txt; integration/rule reports remain in Deliverables/TabletopIntegration and Deliverables/MainMenuFlow. These are synthetic Edit Mode tests, including injected official IHand/IFingerAPI data, not live recognition observations.

## Meta tooling and reference

Used the connected official metavr documentation tools, Meta VR CLI discovery/status, and the installed Meta verify-first skill. Consulted [Meta Hand Grab Interactions](https://developers.meta.com/horizon/documentation/unity/unity-isdk-hand-grab-interaction), updated 2026-08-26, and verified the actual APIs against installed ISDK/Core 207.0.1 source: HandGrabAPI, IHand, IFingerAPI, GrabbingRule, OVRHand and OVRManager focus properties. No SDK package upgrade was made.

Device list was empty. Meta XR Operator CLI status reported binary not installed/registered, proxy not running and application server inactive. Operator package files and Simulator tooling are present locally, but neither supplied a live session. The Meta AI Agent Bridge reports its pre-existing port 48735 conflict. No Runtime Optimizer, Hands Optimizer or Immersive Debugger live data was claimed. Unity editor tools remained usable; the final console has that bridge error, not a C# compilation error.

Reviewed the supplied Demeo Scaling video samples and sheets, including the 23–31s and 48–57s sequences. They inform overview-to-close-up feel and the stable diorama presentation, not exact dimensions, SDK choices, internal hierarchy or Demeo's implementation.

## Recovery and next tasks

Pre-edit Startup files, the shared contract, gitignore and checkout status are retained in Deliverables/DemeoRework/Task3Baseline (ignored local recovery snapshot). No commit, stash, reset, branch switch, Unity restart, Play Mode, device launch or player build/install was performed. Existing checkout changes were retained. Restore individual files only after comparing current work; never copy the entire snapshot over later tasks.

After explicit runtime authorization, validate real palm curl classification at the table, above it and in empty air; varying hand sizes/heights; release/regrab and low-confidence occlusion; system-menu/input-focus interruptions; combined slow yaw/scale/translation at both bounds; UI poke/ray click arbitration; seated comfort/vignette and room motion; MR anchoring/passthrough and VR/MR return; and multi-client view isolation with voice/avatar tracking. Headset performance is unmeasured.

Task 4 owns first-person avatar presentation. Task 5 must use BASELINE_BEFORE_REWORK.md for the original MR doubling request, preserve the 1–6 relative zoom contract, and keep room/tracking outside scaling. Task 6 should rerun the above suites and perform authorized runtime integration. No town was generated.
