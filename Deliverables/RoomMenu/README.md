# Room choice and opening — Quest Link

Open `Assets/SwitchyardRaiders/Startup/Scenes/SR_Launch.unity`. Start Quest Link, then press Play. No player build was requested or produced.

The sequence is the real room (when passthrough is available), the existing 4.5-second title Timeline, then the room selector. This is onboarding, not the main menu. Both title and selector use Spirit Sling's serialized menu distance of 65 cm, 15 cm below eye height. The title is 44 cm wide. The menu follows yaw with a 60 cm reposition threshold, smoothing at 3 per second, and holds still while pressed.

## Design and interaction

The room selector is a railway destination board with two green enamel destination plaques: an open window for Your Room and a signal cabin for Virtual Room. It shares the approved main menu's walnut, enamel, ivory and brass materials while using a distinct onboarding composition. The new atlas is `SR_Destination_Atlas.png`; the rejected flat geometric cards and older photoreal room tickets are unused.

Artwork and editable live text render through the same world-space uGUI canvas (0.527 m board width) and URP World UI material with explicit stereo instancing. Each caption and state label is parented inside its destination face, so entrance and 3 mm press travel move them together. The fixed button hit rectangles remain separate from the moving faces. Feedback includes staggered arrival, hover lift and a selected label.
Both hands use Meta OVRHands data/visual prefabs, with poke and ray interactors installed through Interaction SDK Quick Actions. Touch, or point and pinch. PointableCanvasModule routes these events to native uGUI buttons. Both choices remain available after selection. `RoomChoiceMenu.onRealRoomSelected` and `onVirtualRoomSelected` are handoff events for later table placement/main menu; that later flow is not part of this screen.

If tracked, focused XR has no passthrough frame after eight seconds, the chooser can offer Virtual Room. Real Room refuses selection until a frame is available. Missing XR/tracking is not bypassed.

## Virtual room provenance

Spirit Sling's `RoomRequirementsState.LoadFallback()` loads `Common/MR/Prefabs/DefaultEmptyRoom.prefab` through MRUK settings in Boot.unity. That prefab contains four walls, not furnishings. Its copy and MIT license are in `Assets/SwitchyardRaiders/Startup/RoomMenu/Reference/`. The reference project was not edited. The scene adds a floor and ceiling and places the room once around the player at the tracking-space floor; it does not follow the head.

## Rendering cost

The selector uses one 1254 x 1254 RGBA atlas, three sliced sprites, and live text. Android import uses ASTC 6x6, mipmaps and no CPU-readable copy. There are no UI dynamic lights, shadow passes, particles, videos or per-frame material instances. Baked surface shading is in the artwork; normal, roughness and metallic maps are not sampled by this UI material. Transform and color writes stop when feedback has settled.

The virtual background has six renderers, no colliders and no dynamic lighting. The title renderer/follower stops after handoff and its Timeline graph is disposed. These choices limit UI cost; they are not a headset FPS measurement.

## Play Mode entry

Recorded domain reloads took roughly 6–12 seconds, including package initialization and instance restoration. Desktop UI preview now skips domain reload, keeps scene reload, and disables automatic editor XR initialization. Quest Link retains a fresh domain because Meta OVRManager keeps static passthrough initialization state. Disabling that reset without repeated headset validation risks the earlier black second-Play failure.

Choose **Switchyard Raiders → Opening → Use desktop preview (no headset)** for UI iteration. Choose **Use headset (Link or Simulator)** before Link testing. `play-entry-timings.txt` records measured entry times and reload options. The deliberate title animation is separate from entering Play. The last inspected headset startup log also reported `XR_ERROR_FORM_FACTOR_UNAVAILABLE`, indicating that startup did not obtain an available OpenXR headset.

## Validation

`scene-validation.txt` checks missing scripts, two poke interactors, two ray interactors, two PointableCanvases and button events. `playback-check.txt` records the latest desktop Play result using synthetic Meta pointer events: title handoff, fresh state, real-to-virtual-to-real switching and panel follow/hold. Captures are actual desktop camera renders, not headset views. Read report timestamps: old layout results do not establish the new layout's correctness.

Commands: **Switchyard Raiders → Room Menu → Validate scene**, **Run desktop interaction check**, and **Measure full reload baseline**. Automated checks switch to desktop preview and stop Play on completion. Restore headset mode afterward.

Physical hand tracking, passthrough switching, eye comfort and sustained FPS require Quest Link validation. Meta XR Operator status reported no available binary/proxy/server, and the Agent Bridge reported a port conflict; no live Operator or Bridge validation is claimed. Test both hands, held input, tracking loss/reacquisition and two consecutive Link Play sessions.

Official SDK setup reference: https://developers.meta.com/horizon/documentation/unity/unity-isdk-poke-quick-action/

Latest desktop measurements: full domain reload 9.101 s; two scene-reload-only previews 0.310 s and 0.325 s. Both preview runs passed the synthetic Meta interaction checks. These are warm-editor samples, not headset startup or cold-compilation benchmarks.

## Stationary hand offset — remaining headset check

The user reports the offset persists while hands are still. Do not compensate with guessed positional offsets or label this a latency fix. Rig references and floor-level tracking were compared with Spirit Sling; the SDK default fingertip extension is the same. Installation now rebinds the hand data stack to the active rig and preserves existing interactors.

The most recent live Play attempt had no active OpenXR headset and no valid hand poses. Once Quest Link is active, run **Switchyard Raiders → Room Menu → Capture live hand alignment** during Play. This captures the raw tracked wrist transformed into world space, the SDK wrist, visual root and poke point for both hands in `live-hand-alignment.txt`. The tool rejects desktop preview. Agreement between these coordinates still needs confirmation against the actual passthrough image.

## Live Link rendering diagnosis — 18 October 2026

Both hands had valid tracked poses. Raw tracking wrists transformed to world coordinates, Interaction SDK wrists and rendered hand roots agreed to 0.00 mm at report precision; camera anchor and XR head position also agreed. A live material-only test using URP Unlit aligned the visible hands with the real hands according to the user. This isolates a rendering-path issue in this project rather than a constant tracking transform error. No compensating hand-position offsets were added.

The project now uses an explicit URP/stereo hand-edge shader and a matching URP world-space UI shader. SDK joint tracking and poke geometry are retained. The menu is scaled to 85% of its previous width and its follow displacement threshold matches Spirit Sling at 60 cm; it remains stationary within that dead zone and during presses. Final outline and UI validation follows the live material test; do not confuse the successful turquoise diagnostic with final shader confirmation.

Unity crashed during the first restart after saving this change. The stack was in native OpenXR message-loop processing, with a ManagedWebSocket.SendAsync frame. This does not establish the cause of the crash. The saved scene was reopened for continued validation.

User requested no further live testing or editor restarts. The follower now uses Spirit Sling CanvasFollowPlayer interpolation, head-relative vertical offset and world-space hysteresis, with the TableTopMenu prefab values: distance 0.65 m, height -0.15 m, interpolation speed 3, displacement threshold 0.6 m. The smaller board and explicit URP materials are saved for the user to test; final headset validation has not been performed.

## Poke and selection visual rendering

Spirit Sling's HandPokeInteractor uses the same distal index joint and 17 mm fingertip extension as this project's SDK prefab (legacy X / OpenXR Z, mirrored for the left hand). No tracking offsets were added. The hand alignment and menu placement confirmed by the user are preserved.

The remaining legacy-rendered visuals are migrated individually: OculusCursor retains Meta's procedural radial cursor shader properties with URP stereo transforms; OculusHandPinchArrowBlended and SelectionCircle retain their material colors/textures with an explicit URP pointer shader. Both left and right hands receive saved material assignments. This is an editor-only saved change; no live Play test or Unity restart was requested or performed for it.
