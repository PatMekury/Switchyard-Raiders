# Switchyard Raiders: tabletop integration

**2026-10-22 Task 1 update:** current geometry, anchors and view placement are defined in [TABLETOP_VIEW_CONTRACT.md](TABLETOP_VIEW_CONTRACT.md), with validation and source handoff in [Deliverables/DemeoRework/01-handoff.md](Deliverables/DemeoRework/01-handoff.md). The historical rectangular dimensions and 0.2x minimum below are superseded: exterior is now 1.30 m square, usable land 1.05 m square, normal is minimum zoom, and the structural tray remains visible in MR. Existing gameplay/service behavior below remains applicable.

Implemented in `Assets/SwitchyardRaiders/Startup/Scenes/SR_Launch.unity` and the existing `SR_Launch.prefab`. The reusable assembled presentation is `Assets/SwitchyardRaiders/Startup/Prefabs/SR_TabletopPresentation.prefab`.

## Player flow

1. Choose **Play in VR** or **Use your room** after the existing opening. VR shows the authored table in the menu; manipulation and terrain are disabled there.
2. MR requests spatial permission only when selected. It loads the saved room, verifies that the headset is inside its actual bounds, and looks for a scanned TABLE surface. The whole terrain footprint must fit, with clearance. A green outline previews placement; choose **Use your room** again to confirm. Failure provides the existing retry/VR fallback; **Keep current mode** cancels an in-session change.
3. **Find a match** and **Play with friends** offer two/four-player selection. Choose the same player count when joining a private code. Online sessions use the existing Meta entitlement/identity service, Fusion runner and Photon Voice runner prefab. Fusion and Voice still request separate fresh authentication proofs.
4. The departure board shows occupied/ready slots. Everyone readies, the three-second countdown completes, and the terrain and minimal railway activate. The existing authored player and company locomotives are used. Simulation remains simultaneous.
5. **Match menu** provides view controls, VR/MR switching, microphone controls, leave, and developer slot switching when enabled. Results offer rematch through a fresh ready/countdown gate. A mid-match disconnect ends the match explicitly; return/retry starts a clean session. Host migration/resuming a disconnected match is not implemented.

## Rules decisions confirmed in this conversation

- Two-player mode retains its three-delivery objective.
- Four-player mode is documented **2v2**: P1/P3 share one home and total; P2/P4 share the other. Six team deliveries win, as requested.
- Freight arrives gradually and continues to replenish until a team wins. The integration uses a provisional **20-second interval**, one incoming wagon at a time, with up to six active cargo slots. Delivered slots are recycled. The interval is `RaidersMatch.FreightInterval`; it is a test cadence, not a finalized balance decision.
- Existing interception, ownership, collection, downhill motion and full-chain delivery behavior remains. This integration does not implement the updated design document's future C/Y routing system, detailed town, NPCs, track placement, train collisions, or final capacity rules. The railway remains the existing minimal loop used to exercise the flow.

## Headset solo testing

1. In `SR_Launch`, select **SR_RoomChoice** and find **RaidersSession** in the Inspector.
2. Enable **Enable Developer Solo Testing**. It defaults off. The option is effective only in the Editor or a **Development Build**; release builds ignore it even if accidentally serialized on.
3. In the headset, choose **Learn & Practice → Developer solo test → Two simulated slots** or **Four simulated slots (2v2)**.
4. Choose **Ready all simulated slots**. The normal ready/countdown/start flow runs locally without Photon or fabricated Meta identities.
5. **Match menu → Next simulated slot** cycles controls. The tray names the controlled SIMULATED slot; seat markers also say SIMULATED. Commands remain active for the other trains while all slots keep simulating.

This mode tests local controls, independent inventories, simultaneous movement, team scoring, terrain activation, mode/view UI and cleanup. It does not create network clients. Task 4 can load the actual local account avatar through entitlement/identity when available, independently of this simulation. It cannot validate replication, remote avatar pose streaming or two-way voice; those need multiple real clients/accounts. No headset build or installation was performed in this task.

## Personal table controls

Close **both hands** naturally anywhere to palm-grab your personal view (Task 3 supersedes rim contact). Both valid tracked palms must be closed before it moves. There is no contact requirement, pinch shortcut or arming menu. The SDK palm classifier feeds an explicit two-hand state machine; the former Grabbable/transformer and rim-collider proxy have been removed. Opening either hand freezes movement; reacquisition takes a fresh stable baseline. The terrain-related pivot, planar yaw/scale and deliberate common vertical translation prevent hand-height coupling and upward drift. See TABLETOP_VIEW_CONTRACT.md for the complete current contract.

Gameplay and menu commands are blocked during two-palm acquisition/motion and for 0.25 seconds after release. Gesture epochs cancel stale UI clicks; one-hand menu use remains available. Tracking/confidence loss, system gestures, focus loss and mode changes suspend manipulation and require both hands open before a fresh grab.

VR placement uses current horizontal head direction at match start: table center 0.95m ahead, rim approximately 30cm ahead, and tabletop 0.5m below the eyes. **Match menu > Table view / comfort > Recenter table in front** restores this placement relative to the current view. It does not move the XR rig. The full-sized table can extend outside the view; physical reach takes precedence over fitting the whole table on screen.

VR translates/yaws the existing room coherently with the table but never scales it. Only table supports hide at 1.6x; the tray remains. Zoom is continuous 1�6x relative to normal; tilt stays locked. The physical room and tracking stay fixed in MR. The adjustable Meta vignette operates only while deliberately changing the VR presentation, not during ordinary head movement.

MR starts anchored to the confirmed surface: height stays on that plane and each proposed footprint must remain within the scanned tabletop. **Explore beyond table** permits enlargement outside it while keeping passthrough visible. **Return to scanned table** restores the confirmed placement. Returning to VR restores the saved preferred VR view. Neither transition restarts the session or voice.

## Assets and anchors

- Reused `PlayTable/Prefabs/SR_PlayTable.prefab`: outside 1.90 x 1.40 m; usable inset 1.56 x 1.11 m. Its `TabletopPivot/TownMount` is 1.018 m above the authored table origin.
- Reused `TerrainBase/Prefabs/SR_TerrainBase.prefab`: 1.50 x 1.05 x 0.15 m; bottom-center origin. This leaves 30 mm around each edge of the inset, clear of rails and cup holders.
- `Personal tabletop presentation / Terrain mounting surface` aligns with the authored TownMount. The terrain sits at local zero; `Gameplay railway and controls` sits at y=0.154 m and uses a 0.92 presentation scale to keep the existing loop within the terrain.
- Named `Player seats`, `Manipulation handles`, and `MR tabletop placement preview` references are wired. `RaidersTabletopBindings` retains the reusable presentation's internal references. Existing menu/tray controls remain in the launch prefab.
- Two seats are opposite; four seats occupy four sides. Online clients map their own slot to the near side of their personal seat layout. Remote avatar origins are normalized from each sender's tracking origin into those local seats. Task 4 presents the local account avatar in SDK FirstPerson/Half mode under physical trackingSpace, with head/face excluded. Existing outline renderers are suppressed only after account loading and valid tracking; input providers and interactors remain enabled. Simulated players use labeled markers, not duplicate authenticated avatars. Missing avatars retain a labeled loading/unavailable marker.
- The non-null `Array.Empty<byte>()` avatar stream buffer is retained at initialization and cleanup. The host binds remote avatar user IDs to Fusion-authenticated player IDs before relay.
- Terrain and trains are miniature presentation descendants; local avatars remain under physical trackingSpace and remote avatars under unit-scale human seats; authoritative rail distances, speed, cargo ownership and score never use the presentation transform. No duplicate runner, rig, manager or permission service was added.

## Validation and remaining runtime checks

Connected editor: Switchyard Raiders, Unity 6000.6.0b5, active target Android. Initial integration was checked in Edit Mode. The follow-up inspected the user-started running simulation; it was already stopped before the contact-grab edits were applied.

- 27 additional contact-grab/placement assertions passed, plus 60 integration assertions and 17 existing rule assertions. The new checks cover both-hands-only movement, release/reacquisition, midpoint scaling, recentering after turning during menus, contact wiring, palm-only grab configuration, and hand-grab priority over menu rays. Live two-hand gesture recognition is still unverified.
- 60 integration assertions passed, plus 17 existing rule assertions. Covers two/four-slot local flow, ready/countdown/start, independent commands with simultaneous simulation, team scoring, gradual/replenishing freight, rematch and leave/retry state, snapshot serialization, seat layout, midpoint manipulation, tilt/scale limits, synthetic MRUK tabletop ray/footprint checks, required scene wiring and non-null avatar buffers.
- Existing room/voice validation passed: one MRUK, manual permission ownership, inactive runner prefab, recorder permission gate, remote speaker wiring and configured Fusion/Voice app IDs. These are configuration checks, not successful online service connections.
- Compiled source and editor checks with Android as the active target. No player build, IL2CPP build, installation, Play Mode, Unity restart or headset launch.
- Isolated URP `Camera.Render` images are in `Deliverables/TabletopIntegration/Previews/`: `assembled-gameplay.png`, `playfield-fit.png`, `menu-table.png`. These show the actual assembled assets, not a running match or headset capture. No PreviewRenderUtility was used.
- Rerun using **Switchyard Raiders → Tabletop → Run Edit Mode integration checks**. The separate render command creates only isolated review images. The integration command rewires and saves the launch scene/prefabs; it defaults developer testing off.

Still requires authorized runtime/device verification: native room loading and containment, successful/denied/cancelled spatial permission, passthrough preview visibility, out-of-room recovery, hand tracking/release/reacquisition, seated comfort and narrow-FoV readability, VR/MR transitions during a live countdown/match, multi-client host/join/leave/rejoin, authentication, all four remote avatars, relay normalization and two-way voice/microphone retry. Quest CPU/GPU frame times and five-train presentation costs are unmeasured.

Tool status: during the follow-up the user-started application server was active at localhost:8720; the CLI MCP proxy was not installed/running, so live scene inspection used the connected Unity editor. The simulation showed only the right hand tracked. No live XR session was started by the agent. Unity also reports a pre-existing Agent Bridge remote-server port 48735 conflict. Runtime Optimizer, Immersive Debugger and Hands Optimizer were not used for runtime claims.

## Official references checked on 2026-10-22

- [Meta spatial data permission](https://developers.meta.com/horizon/documentation/unity/unity-spatial-data-perm) (updated 2026-10-07).
- [MRUK scene management and queries](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-manage-scene-data) (updated 2026-10-08).
- [Meta Hand Grab interactions](https://developers.meta.com/horizon/documentation/unity/unity-isdk-hand-grab-interaction) (updated 2026-08-26), checked against installed ISDK source.
- [OVRHand v207](https://developers.meta.com/horizon/reference/unity/v207/class_o_v_r_hand).

The local `scene design/Demeo AR.mp4` was inspected through extracted frames for the room/table presentation reference. Original authored models and unrelated scene/menu changes were retained. A pre-edit startup source/scene/prefab copy is in `Deliverables/TabletopIntegration/Baseline`.


Task 2 update: current scale/path/ownership values are in TABLETOP_VIEW_CONTRACT.md and Deliverables/DemeoRework/02-handoff.md. Engines now use 1:100, the gameplay root is unit scale, inspection is continuous 1–6x relative to normal, and human seats are outside the scale branch. Earlier temporary-fit values below/above are historical.

Task 3 update: grab-anywhere, room ownership, pivot math, command arbitration and current validation are recorded in TABLETOP_VIEW_CONTRACT.md and Deliverables/DemeoRework/03-handoff.md. Earlier contact-check counts and midpoint behavior in the validation history are superseded.


## Task 4 avatar update

See [04-handoff](Deliverables/DemeoRework/04-handoff.md) and [the shared contract](TABLETOP_VIEW_CONTRACT.md). Gameplay entry in solo and online uses the same local account-avatar lifecycle. Loading, tracking interruption and failure retain explicitly labeled temporary hand visuals. **Match menu -> Table view / comfort -> Avatar status / retry** shows the failure reason and retry action. Exit restores menu visuals. Renderer-only suppression leaves Task 3 palm-grab and UI input active. The existing manager prefab, identity gate, remote seat ownership and non-null stream buffer are retained.

61 synthetic avatar Edit Mode checks pass, including lifecycle, both-hand data conversion, visual restoration, remote slot cleanup and actual-font status text fit. These are configuration/simulated-input results, not authenticated rendering or live hand recognition. Existing 198 manipulation/scale/integration/rule assertions and flow configuration also pass. No Play Mode, device launch, build/install or Unity restart was performed.
