# Switchyard Raiders: tabletop integration

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

This mode tests local controls, independent inventories, simultaneous movement, team scoring, terrain activation, mode/view UI and cleanup. It does not create network clients and cannot validate replication, real-user authentication, remote avatar pose streaming or two-way voice. Those need multiple real clients/accounts. No headset build or installation was performed in this task.

## Personal table controls

Open **Match menu → Table view / comfort → Move table**. Pinch thumb and **middle finger on both hands**. Moving both hands translates the assembly, turning the hand-to-hand line rotates it horizontally, and spreading/closing hands scales around their midpoint. Release either hand to stop. Both hands must release before reacquiring; low confidence, tracking loss, system gestures, focus loss and mode transitions stop the gesture.

Gameplay commands are blocked while Move table is enabled and briefly after release. Finish moving or return to Train controls to resume commands. The tray keeps its own size and is held in place during manipulation.

VR leaves the environment stationary and hides the supporting table once scale exceeds 1.6x. Scale range is 0.2x to 12x; tilt stays locked. The adjustable Meta vignette operates only while deliberately changing the VR presentation, not during ordinary head movement.

MR starts anchored to the confirmed surface: height stays on that plane and each proposed footprint must remain within the scanned tabletop. **Explore beyond table** permits enlargement outside it while keeping passthrough visible. **Reset / return to table** restores the confirmed placement. Returning to VR restores the saved preferred VR view. Neither transition restarts the session or voice.

## Assets and anchors

- Reused `PlayTable/Prefabs/SR_PlayTable.prefab`: outside 1.90 x 1.40 m; usable inset 1.56 x 1.11 m. Its `TabletopPivot/TownMount` is 1.018 m above the authored table origin.
- Reused `TerrainBase/Prefabs/SR_TerrainBase.prefab`: 1.50 x 1.05 x 0.15 m; bottom-center origin. This leaves 30 mm around each edge of the inset, clear of rails and cup holders.
- `Personal tabletop presentation / Terrain mounting surface` aligns with the authored TownMount. The terrain sits at local zero; `Gameplay railway and controls` sits at y=0.154 m and uses a 0.92 presentation scale to keep the existing loop within the terrain.
- Named `Player seats`, `Manipulation handles`, and `MR tabletop placement preview` references are wired. `RaidersTabletopBindings` retains the reusable presentation's internal references. Existing menu/tray controls remain in the launch prefab.
- Two seats are opposite; four seats occupy four sides. Online clients map their own slot to the near side of their personal seat layout. Remote avatar origins are normalized from each sender's tracking origin into those local seats. The local avatar remains hidden so existing first-person hands and camera are preserved. Simulated players use labeled markers, not duplicate authenticated avatars. Missing avatars retain a labeled loading/unavailable marker.
- The non-null `Array.Empty<byte>()` avatar stream buffer is retained at initialization and cleanup. The host binds remote avatar user IDs to Fusion-authenticated player IDs before relay.
- Terrain, trains and avatars are presentation descendants; authoritative rail distances, speed, cargo ownership and score never use the presentation transform. No duplicate runner, rig, manager or permission service was added.

## Validation and remaining runtime checks

Connected editor: Switchyard Raiders, Unity 6000.6.0b5, active target Android, Edit Mode throughout.

- 60 integration assertions passed, plus 17 existing rule assertions. Covers two/four-slot local flow, ready/countdown/start, independent commands with simultaneous simulation, team scoring, gradual/replenishing freight, rematch and leave/retry state, snapshot serialization, seat layout, midpoint manipulation, tilt/scale limits, synthetic MRUK tabletop ray/footprint checks, required scene wiring and non-null avatar buffers.
- Existing room/voice validation passed: one MRUK, manual permission ownership, inactive runner prefab, recorder permission gate, remote speaker wiring and configured Fusion/Voice app IDs. These are configuration checks, not successful online service connections.
- Compiled source and editor checks with Android as the active target. No player build, IL2CPP build, installation, Play Mode, Unity restart or headset launch.
- Isolated URP `Camera.Render` images are in `Deliverables/TabletopIntegration/Previews/`: `assembled-gameplay.png`, `playfield-fit.png`, `menu-table.png`. These show the actual assembled assets, not a running match or headset capture. No PreviewRenderUtility was used.
- Rerun using **Switchyard Raiders → Tabletop → Run Edit Mode integration checks**. The separate render command creates only isolated review images. The integration command rewires and saves the launch scene/prefabs; it defaults developer testing off.

Still requires authorized runtime/device verification: native room loading and containment, successful/denied/cancelled spatial permission, passthrough preview visibility, out-of-room recovery, hand tracking/release/reacquisition, seated comfort and narrow-FoV readability, VR/MR transitions during a live countdown/match, multi-client host/join/leave/rejoin, authentication, all four remote avatars, relay normalization and two-way voice/microphone retry. Quest CPU/GPU frame times and five-train presentation costs are unmeasured.

Tool status: the supplied Unity screenshot confirms XR Operator binaries present and activated. The CLI proxy status reported unavailable/inactive and Simulator reported no running application. No live XR session was started. Unity also reports a pre-existing Agent Bridge remote-server port 48735 conflict. Runtime Optimizer, Immersive Debugger and Hands Optimizer were not used for runtime claims.

## Official references checked on 2026-10-22

- [Meta spatial data permission](https://developers.meta.com/horizon/documentation/unity/unity-spatial-data-perm) (updated 2026-10-07).
- [MRUK scene management and queries](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-manage-scene-data) (updated 2026-10-08).
- [OVRHand v207](https://developers.meta.com/horizon/reference/unity/v207/class_o_v_r_hand).

The local `scene design/Demeo AR.mp4` was inspected through extracted frames for the room/table presentation reference. Original authored models and unrelated scene/menu changes were retained. A pre-edit startup source/scene/prefab copy is in `Deliverables/TabletopIntegration/Baseline`.
