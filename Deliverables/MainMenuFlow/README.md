# Main menu and session flow

Gameplay remains simultaneous, as confirmed by the user. The editor adapter changes the controlled player; it does not alternate simulation turns.

## Implemented source

- Existing room selection leads to Find a Match, Play with Friends, Learn & Practice, and Settings.
- Public Fusion Host/Client 1v1 matchmaking; private host and six-character join code; ready gate and three-second countdown; cancellation, errors, retry, leaving, disconnect handling, and mutual rematch.
- A shared authoritative freight-yard simulation is used by online, practice, and editor test sessions. Separate player locomotives, device inventory, ownership, score, throttle, and brake state. Decoupling, loose cargo motion, collection, home delivery, victory, and reset.
- Meta Platform initialization, entitlement, logged-in identity and token; Avatars SDK manager, local tracking, remote stream transport, and lifecycle cleanup. The local avatar is hidden to retain the existing interaction hands. A visible presence marker is the fallback; local tests never fabricate an authenticated second user.
- Live UI text, approved button artwork, stable hand targets, restrained fades, and existing menu audio/countdown bus.
- Real Room now requests spatial data permission through a single serialized permission coordinator, loads MRUK scene data, opens Space Setup when no saved scan exists, and supports retry or Virtual Room fallback. Choosing Virtual Room cancels pending selection work. Yard placement samples the full footprint against room boundaries and scanned furniture, uses scanned table heights where available, and offers reposition/retry when space is unsuitable.
- Online sessions instantiate an inactive, configured NetworkRunner/FusionVoiceClient prefab, then activate it atomically. Voice follows the Fusion room and region, plays remote streams through a Speaker prefab, and has microphone permission, listen-only denial handling, mute, retry, noise suppression, echo cancellation, pause handling, and leave/disconnect cleanup. Practice and local editor matches do not create a voice connection.
- Fusion now sends Meta user ID plus a fresh GetUserProof nonce through Oculus custom authentication. Voice obtains its own fresh nonce; it never reuses the Fusion nonce. No anonymous fallback is implemented.
- Microphone capture is disabled in the prefab, including Photon's RecordWhenJoined automatic recording. It is enabled only after permission and the user's unmuted state. Spatial and microphone dialogs cannot overlap; OVRManager and MRUK startup prompts and Unity automatic permission dialogs are disabled.
- Scene, launch prefab, runner prefab and speaker prefab are wired and saved. Android manifest declares USE_SCENE and RECORD_AUDIO; the final Gradle callback checks that both survive Meta SDK processing.

## Editor test

In SR_Launch, select the object containing RoomChoiceMenu (SR_RoomChoice) and find RaidersSession. Enable **Solo 1v1 Test â€” Control Both Players**. When Play Mode is separately authorized, choose Learn & Practice > Solo 1v1 > Ready. Match menu > Switch controlled player changes the input owner while both locomotives keep simulating. Public/private routes never consult this flag, and the flag is absent from player builds.

## Verification status

Runtime and editor C# source compilation passed with Unity's generated response files and installed assembly references. This was a compiler-only check, not a player build. An existing Photon Voice demo serialization warning remains.

The target editor completed compilation and saved SR_Launch scene/prefab and SR_AvatarManager. Scene validation passed for 23 action callbacks, live text, world-space hand interaction surfaces, tracking/table anchors, missing scripts, and the single intended avatar input provider. All 17 Edit Mode rules assertions passed. Existing room-selector text, placement, and follower configuration passed the preservation comparison.

The guarded setup is available at Switchyard Raiders > Main Menu > Apply game-start flow. Reports are validation.txt and rules-checks.txt. The avatar import's repeating showcase dialogs were resolved; unused example scenes/prefabs were removed through AssetDatabase, and the standalone manager retains its SDK configuration with two concurrent avatar loads and no preset preload. Sample API compatibility fixes were compiled. Existing Photon Android plugin importer warnings and a Meta AgentBridge port 48735 conflict remain; neither is a gameplay verification result.

No Play Mode, headset session, Unity restart, or player build was started. Spirit Sling was used as a read-only reference.

## Remaining limits and external dependencies

The yard is a compact playable foundation, not the full design-document town. C/Y connection placement, full branching track topology, terrain shot occlusion, free decoupler placement, and final town artwork are not implemented.

Fusion, Voice and Meta App IDs are present. At the user's direction, the desktop and Android Meta fields use the same existing App ID. Their account/service validity has not been tested. Authenticated multiplayer still requires an entitled account and the relevant platform/avatar access permissions. No separate Meta application or new App ID is required by this project configuration.

Public/private joining, actual avatar rendering/pose streaming, physical hand interaction, full match/rematch usability, and Quest performance still require authorized runtime testing. Meta XR Operator is unavailable; no live XR validation is claimed.

## Room, voice and authentication wiring — 2026-10-21

The target Unity editor compiled the final runtime and editor scripts successfully. Service validation passed for scene and prefab references, exactly one MRUK, manual permission ownership, inactive runner setup, recorder permission gate including RecordWhenJoined=false, Speaker playback references, active Fusion/Voice IDs, and room-aware table placement. Existing 23 action callbacks and all 17 simultaneous gameplay assertions passed. Meta's real Gradle manifest processor followed by the project correction retained both runtime permissions and the manual-dialog flag. The Quest render pipeline preflight passed. These are Edit Mode checks, not runtime validation.

**Dashboard configuration verified:** after the user saved both providers, rechecked the signed-in Photon dashboard. Both `switchyard-raiders-Fusion` and `switchyard-raiders-Voice` now show an Oculus provider with Meta app ID `1126632653188810`, a hidden app-secret field, anonymous connections disabled, and rejection enabled when the provider is unavailable. The user performed the credential entry and save. No secret was retrieved or copied into the project. This confirms saved configuration, not secret validity or a successful runtime authentication handshake.

The previously installed APK does not include these source changes. A new build/install and live headset test remain pending the explicit authorization required by the original request. End-to-end remote voice requires a second entitled account/client. Meta XR Operator was unavailable during the earlier device inspection; no Operator validation is claimed.

Expected next-build flow: choose Real Room -> spatial permission if not already granted -> saved room or Space Setup -> main menu. Choose an online match -> Meta entitlement and fresh Photon authentication -> voice verification and microphone permission if unmuted. Denied microphone remains listen-only. Match menu exposes microphone mute, Retry voice, and Reposition yard. Virtual Room and local practice remain usable without room or microphone permission.

References: [Meta spatial permission](https://developers.meta.com/horizon/documentation/unity/unity-spatial-data-perm), [Meta user verification](https://developers.meta.com/horizon/documentation/unity/ps-ownership), [Photon Oculus authentication](https://doc.photonengine.com/realtime/v5/connection-and-authentication/authentication/oculus-auth), [Photon Fusion voice](https://doc.photonengine.com/voice/v2/getting-started/voice-for-fusion).
