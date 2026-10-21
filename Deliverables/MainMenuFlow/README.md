# Main menu and session flow

Gameplay remains simultaneous, as confirmed by the user. The editor adapter changes the controlled player; it does not alternate simulation turns.

## Implemented source

- Existing room selection leads to Find a Match, Play with Friends, Learn & Practice, and Settings.
- Public Fusion Host/Client 1v1 matchmaking; private host and six-character join code; ready gate and three-second countdown; cancellation, errors, retry, leaving, disconnect handling, and mutual rematch.
- A shared authoritative freight-yard simulation is used by online, practice, and editor test sessions. Separate player locomotives, device inventory, ownership, score, throttle, and brake state. Decoupling, loose cargo motion, collection, home delivery, victory, and reset.
- Meta Platform initialization, entitlement, logged-in identity and token; Avatars SDK manager, local tracking, remote stream transport, and lifecycle cleanup. The local avatar is hidden to retain the existing interaction hands. A visible presence marker is the fallback; local tests never fabricate an authenticated second user.
- Live UI text, approved button artwork, stable hand targets, restrained fades, and existing menu audio/countdown bus.

## Editor test

In SR_Launch, select the object containing RoomChoiceMenu (SR_RoomChoice) and find RaidersSession. Enable **Solo 1v1 Test — Control Both Players**. When Play Mode is separately authorized, choose Learn & Practice > Solo 1v1 > Ready. Match menu > Switch controlled player changes the input owner while both locomotives keep simulating. Public/private routes never consult this flag, and the flag is absent from player builds.

## Verification status

Runtime and editor C# source compilation passed with Unity's generated response files and installed assembly references. This was a compiler-only check, not a player build. An existing Photon Voice demo serialization warning remains.

The target editor completed compilation and saved SR_Launch scene/prefab and SR_AvatarManager. Scene validation passed for 23 action callbacks, live text, world-space hand interaction surfaces, tracking/table anchors, missing scripts, and the single intended avatar input provider. All 17 Edit Mode rules assertions passed. Existing room-selector text, placement, and follower configuration passed the preservation comparison.

The guarded setup is available at Switchyard Raiders > Main Menu > Apply game-start flow. Reports are validation.txt and rules-checks.txt. The avatar import's repeating showcase dialogs were resolved; unused example scenes/prefabs were removed through AssetDatabase, and the standalone manager retains its SDK configuration with two concurrent avatar loads and no preset preload. Sample API compatibility fixes were compiled. Existing Photon Android plugin importer warnings and a Meta AgentBridge port 48735 conflict remain; neither is a gameplay verification result.

No Play Mode, headset session, Unity restart, or player build was started. Spirit Sling was used as a read-only reference.

## Remaining limits and external dependencies

The yard is a compact playable foundation, not the full design-document town. C/Y connection placement, full branching track topology, terrain shot occlusion, free decoupler placement, and final town artwork are not implemented.

Fusion and Meta App IDs are present. At the user's direction, the desktop and Android Meta fields use the same existing App ID. Their account/service validity has not been tested. Authenticated multiplayer still requires an entitled account and the relevant platform/avatar access permissions. No separate Meta application or new App ID is required by this project configuration.

Public/private joining, actual avatar rendering/pose streaming, physical hand interaction, full match/rematch usability, and Quest performance still require authorized runtime testing. Meta XR Operator is unavailable; no live XR validation is claimed.
