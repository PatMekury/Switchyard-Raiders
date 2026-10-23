# Task 4 — local Meta avatar gameplay presentation

Implemented in **Switchyard Raiders** on 2026-10-23. Tasks 1–3 handoffs and `TABLETOP_VIEW_CONTRACT.md` were used as the current contract: 1.30 m square tray, 1.05 m usable land, .150 m terrain datum, .01 train conversion and continuous relative zoom [1,6]. Their geometry, locomotives, animation drivers, rail fit and manipulation behavior were preserved. No town was generated.

## Result and user flow

`RaidersAvatars` previously returned early for `session.IsLocal`; `RaidersAvatarEntity` hid the local entity unconditionally. Gameplay now starts the same local account-avatar presentation path for developer solo, practice and online play. Solo requests legitimate Platform identity and entitlement without joining Photon. It does not invent accounts for simulated slots. The simulated slot labels remain explicit.

The existing Meta initialization, `Entitlements.IsUserEntitledToApplication`, `Users.GetLoggedInUser` and access-token path is retained, with a 15-second identity deadline and stale-completion guard. Access tokens are not logged. Online matchmaking/proof and remote packet authentication remain in the unchanged `RaidersSession`/`RaidersVoice` sources.

Before native creation, the local entity requests FirstPerson-only rendering, Half manifestation and Body|Outfit submeshes. Head, face, eyes, hair and head accessories are excluded; this is not an unrestricted full-body `Hidden=false` change. Default-model loading is disabled for local and remote entities. The account-avatar query and load have bounded waits, explicit failures and an account-loaded callback. Temporary hand visuals are never described as an authenticated avatar.

While Identity/Loading, the existing hands remain usable and the gameplay UI shows their temporary status. Ready requires the real account-loaded callback plus valid ISDK head and both-hand tracking, confidence, roots and joints. Tracking loss returns to visible temporary hands and recovers automatically. Loading/identity failure exposes an explanation and retry at **Match menu -> Table view / comfort -> Avatar status / retry**. Results, leave, disable and exit restore menu hand rendering. Retry replaces the local entity and reuses the single warm service.

Only `Renderer.forceRenderingOff` changes on the four authored OVR/OpenXR hand meshes. Their previous values are restored. No tracking parent, hand provider, HandVisual, palm classifier, UI interactor or input GameObject is disabled. UI arrows/cursors and vignette remain separate. Task 3's gesture arbitration still gates menu commands, including retry, during the two-hand gesture and release guard. The footer was enlarged from 42 to 50 pixels at its existing font size to fit score plus status; voice remains visible on the match menu, with avatar status in its larger feedback area.

## Hierarchy and SDK integration

- Local entity: child of the existing physical `RaidersAvatars.trackingSpace`, local position zero, rotation identity and scale one. It is outside the tabletop and moving VR room branches. No new rig or camera exists.
- Input: existing ISDK LeftHand, RightHand and OVRHmd components feed SDK 40 delegated providers through `RaidersAvatarInput`. World poses convert into tracking-local metres once. Installed ISDK 207 uses OpenXR joints despite OVR object names. The official OpenXR sample's 17-avatar-joint mapping, absent thumb-base identity, per-hand coordinate bases and wrist offsets are used. `ISDK_OPENXR_HAND` is local to the SDK assembly; this application adapter targets the installed OpenXR API directly.
- Remote entities: ThirdPerson/Full, no local input manager, parented to the current unit-scale human seats. Sender tracking origin/yaw normalization is retained. Human seats follow normal home per Tasks 2–3; they do not become board miniatures or follow inspection scale. Existing slot/identity ownership, remote replacement, vacated-seat cleanup and relay authentication are preserved.
- One existing `SR_AvatarManager.prefab` is instantiated on demand if none exists; an existing manager is reused. Multiple managers cause an explicit fallback. The warm service survives match exit/retry; only owned services are destroyed with the owner. Its single `RaidersAvatarInput` is reused.
- Non-null `Array.Empty<byte>()` streaming buffer remains initialized and is restored on cleanup. Solo skips stream recording and transport. Online keeps Medium stream snapshots at the existing 10 Hz cadence.

No view manipulation writes authoritative state, the tracking origin, tracked head/hands, or avatar scale. Mode switches and recenter use current physical tracking-local input; the avatar is not reparented into a moved room/table branch.

## Files

Under `Assets/SwitchyardRaiders/Startup/`:

- `Runtime/RaidersAvatars.cs`: identity/presentation lifecycle, explicit fallback/retry, renderer ownership, service reuse and remote ownership/cleanup.
- `Runtime/RaidersAvatarEntity.cs`: pre-creation first/third-person filters, account-load state and bounded failures.
- `Runtime/RaidersAvatarInput.cs`: official ISDK/OpenXR-to-Avatar delegated provider adaptation. Meta's license header is retained.
- `Runtime/RaidersMenu.cs`: visible gameplay status and avatar status/retry page, preserving existing gesture guards and voice UI.
- `Editor/AvatarTask4Setup.cs`: in-place scene/prefab binding migration, refusing Play Mode, wrong project or dirty launch scene.
- `Editor/AvatarTask4Checks.cs`: reproducible synthetic Edit Mode lifecycle, provider, remote and configuration checks. Test seams are private, nonserialized and Editor-only; player builds contain no test bypass.
- `Editor/TabletopIntegrationSetup.cs`: preserves the avatar bindings when the existing integration installer is used.
- `Editor/RaidersFlowSetup.cs`: current input validation/report language and reusable footer dimensions.
- `Prefabs/SR_Launch.prefab`, `Scenes/SR_Launch.unity`: existing hand/HMD/renderer references and footer size only. The avatar manager prefab itself is unchanged.

Shared contract and integration documentation are updated. `Task4Baseline/` retains the pre-edit Startup tree, contract, integration document, gitignore and checkout status for local recovery; `Task4Reference/` retains consulted official sample sources. These local reference/recovery folders remain ignored. Do not copy the full snapshot over subsequent tasks.

## Validation and evidence

Connected Unity tools verified `C:/Users/patmekury/Documents/Unity/Switchyard Raiders/Assets` before edits. The active scene remains saved `SR_Launch`, not dirty, in Edit Mode. C# project compilation succeeds (`EditorUtility.scriptCompilationFailed == false`), with the new test/setup types loaded. Earlier adapter compile errors were fixed against the installed OpenXR namespace/API; historical compiler messages in the Editor log are not current failures.

**61 Task 4 assertions pass** in `validation-task4.txt`:

- First-person-only filters and head exclusion; remote third-person; no default placeholder model.
- Solo/online gameplay routes; no menu/countdown replacement; repeated events; identity failure/retry; exit during pending identity; menu restoration; non-null buffers.
- Renderer-only suppression, prior render flags and exclusion of UI objects.
- Rotated tracking-local conversion, OpenXR root and both-hand joint conversions, physical hand scale, confidence/root/joint/head loss and recovery.
- Synthetic loaded/failure callbacks, temporary visuals, no live authentication or native avatar creation in fixtures.
- Solo remote rejection; own slot/invalid ID/empty packet rejection; unit-scale remote seat placement, reuse, changed ownership, vacated slots and cleanup.
- Scene/prefab bindings, one rig/session, one input manager in the existing prefab, no manager instantiated during Edit Mode checks.
- Actual menu font measurement: score/status footer needs 45 px within 50 px; an entitlement failure explanation needs 78 px within the existing 100 px feedback area.

Prior suites rerun successfully: **59 Task 3 manipulation assertions, 58 Task 2 scale/train-fit assertions, 64 integration assertions and 17 authoritative gameplay-rule assertions**, plus `RaidersFlowSetup.Validate`. Reports remain in DemeoRework, Deliverables/TabletopIntegration and Deliverables/MainMenuFlow. These cover current two/four-player rules, six-delivery 2v2, replenishment and prior view/room behavior; they are not multiplayer runtime evidence.

`validation-task4-preservation.txt` compares with the Task 4 snapshot. All 1,846 prior prefab object IDs and 1,851 prior scene IDs remain. Each asset adds five stripped references to existing nested-prefab components (HMD and four renderers), not new GameObjects, rigs or managers. Only two existing serialized blocks change: avatar references and footer RectTransform. All other pre-existing Startup files are byte-identical apart from the eight explicitly listed edited files. Session/auth transport, voice, rules, table/room manipulation and railway sources are preserved. Standard repository-wide `git diff --check` reports Unity YAML trailing spaces against HEAD, including pre-existing changes; no blanket whitespace rewrite of another task's work was performed.

## Official references and available tooling

Read the project instructions/memory/integration/order documents and Meta verify-first/platform skills. Used connected official `metavr` documentation tools and Meta VR CLI status, alongside installed package source:

- [Meta ISDK Avatar Integration sample guidance](https://developers.meta.com/horizon/documentation/unity/unity-sample-isdk-avatar), updated 2026-05-11.
- [Official sample repository](https://github.com/oculus-samples/Unity-MetaXRInteractionSDK-AvatarSample), including OpenXRHandTrackingDelegate, HandTrackingInputManager and provider conversions.
- [OvrAvatarEntity guidance](https://developers.meta.com/horizon/documentation/unity/meta-avatars-ovravatarentity), FirstPerson/render filters and current SDK 40.0.1.
- [Platform entitlement guidance](https://developers.meta.com/horizon/documentation/unity/ps-entitlement-check), updated 2026-08-14.

Installed source verified: Avatars 40.0.1 (`com.meta.xr.sdk.avatars@45b03b85dc05`), ISDK/Core/Platform 207.0.1, delegated providers/interfaces, entity creation/view/manifestation callbacks, OpenXRHandPrimitives, HandMirroring and HandTranslationUtils. No SDK upgrade or application identity settings change was made.

Meta device list was empty. `npx -y metavr xroperator status` reported Operator binary not installed, proxy not running and application server inactive. Simulator/Operator package files exist, but no live runtime session was available. The Meta AI Agent Bridge still reports the existing port 48735 conflict; the Unity AI account service also emitted an Editor connectivity/focus warning. Neither is a C# compile failure. Unity editor tools remained usable. No live Runtime Optimizer, Hands Optimizer or Immersive Debugger data is claimed.

Reviewed the provided Demeo video sheets covering approximately 23–31s and 48–57s. They inform the inherited overview/close-up presentation contract; they do not establish Demeo's exact dimensions, avatar SDK or internal implementation.

## Remaining authorized runtime/account checks

**No authenticated avatar loading or native first-person appearance has been demonstrated.** No Play Mode, headset application, Unity restart, build/install or network session was started. Edit Mode fixtures cannot establish that an account owns an avatar, that CDN loading succeeds, or that generated meshes are comfortable in stereo.

A subsequent authorized runtime check needs the correctly configured app, an entitled real/test account or supported authorized Editor test user, required Platform User ID/access permissions, an account with a created Meta avatar, network access and valid hand/head tracking. XR Simulator alone may not supply the legitimate account context; it must visibly fall back rather than fabricate success.

Verify on a real account: solo gameplay loads the user's avatar without a Photon runner; first-person hands/arms/torso align and never show camera-occluding head/face; no duplicate outlines; finger articulation and both-hand grab/UI work together; low-confidence tracking, system/focus interruptions, regrab, recenter, [1,6] zoom, room translation/yaw and VR/MR transitions preserve physical avatar scale. Exercise unavailable identity, denied entitlement, no-avatar/CDN failure, retry, leave during loading, results/rematch and menu restoration. With multiple legitimate clients, verify remote body pose/seat normalization, ownership changes, disconnect cleanup, streaming and voice. GPU/CPU and avatar skinning cost remain unmeasured.

Task 5 must preserve the physical avatar branches, use `BASELINE_BEFORE_REWORK.md` for MR doubling, and keep the shared relative zoom [1,6]. Task 6 should include these runtime checks after explicit authorization.
