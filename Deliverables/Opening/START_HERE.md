# Switchyard Raiders opening

Open `Assets/SwitchyardRaiders/Startup/Scenes/SR_Launch.unity`. Headset mode is now the default: start Quest Link before pressing Play. For a flat preview, select **Switchyard Raiders → Opening → Use desktop preview (no headset)** first. The scene is also first in the global build scene list used by the Meta Quest profile. The same menu opens the scene, selects its Timeline and runs validation.

The preview menus now restore full scene/domain reload for repeat Play sessions. Desktop preview disables desktop XR initialization; selecting headset mode re-enables it. Stop Play before switching modes. The current workflow is Quest Link; no APK build is required.

## What plays

The additional Unity splash screen is disabled. Meta's system loading background is set to contextual passthrough, which keeps the room visible during loading when the user launched from a passthrough Home environment. The OS still controls this loading interval.

On Quest, the scene camera clears to transparent black and Meta passthrough presents the real room. After Meta reports a presented passthrough frame, the app waits for positional tracking and input focus, then leaves the room clear for 0.35 seconds. A 4.5-second Timeline reveals the railway nameplate, adds a very small settle and sweeps warm light across it. The room stays at full opacity. The title is placed once, 1.5 m ahead of the seated head pose and 8° below eye level. It does not follow every head movement.

The Timeline owns the animation and three audio cues. A single unlit, alpha-masked URP pass produces the light sweep; no video, particle system or VFX Graph package is needed. The logo is the approved transparent PNG. The shader includes stereo instancing support and premultiplied alpha for passthrough composition.

The sequence holds on the completed title. `LaunchSequence.onTitleReady` is the once-only handoff for table setup / a main menu when those screens are built. It currently has no listener. This scene does not pretend that gameplay or table placement already exists.

The opening runs automatically. On later device launches, a fresh high-confidence index pinch can skip after 0.45 seconds of the Timeline. Held pinches, system gestures and tracking reacquisition cannot skip. System focus loss pauses the Timeline. The local “opening seen” preference is written after a completed device opening; editor previews do not change it.

## Audio files to import

Three **temporary, procedurally synthesized preview cues** are already connected. They are timing placeholders, not final railway recordings or final music. Replace the WAV contents at these exact paths, retaining their `.meta` files to keep Timeline references:

`Assets/SwitchyardRaiders/Startup/Audio/`

| Filename | Sound brief | Length | Timeline start | Mixer |
|---|---|---:|---:|---|
| `SR_Startup_Coupler.wav` | A small, dry railway coupling clack, weighty but friendly; short metal tail, no large crash | 0.48 s | 0.90 s | SFX |
| `SR_Startup_Sweep.wav` | Quiet airy metal brush/steam swish for the brass highlight; subtle, no sci-fi laser | 0.90 s | 1.35 s | SFX |
| `SR_Startup_Sting.wav` | Original/licensed warm 3.2-second musical signature; restrained brass, plucked strings or celesta; resolve cleanly, no epic fantasy swell | 3.20 s | 0.90 s | Music |

Use 48 kHz PCM WAV, 16- or 24-bit. Mono is appropriate for the coupling and sweep; the music may be stereo. Keep the beginning tightly trimmed and the tail inside the listed duration. These times are relative to the Timeline; the room-only interval precedes it. If you change durations, adjust the corresponding Timeline clip and keep the final sound tail before 4.5 s. Rename the clips to remove `TEMP` after final recordings are installed.

`SR_Startup.mixer` contains **Master → Music / SFX**. Each Timeline AudioTrack is bound to its own routed AudioSource. Source gains are Coupler 0.60, Sweep 0.35 and Sting 0.45. Adjust the group faders for overall Music/SFX balance. No audio source starts outside Timeline.

## Preview and headset use

`LaunchSequence.desktopPreviewInEditor` defaults to false. Use **Switchyard Raiders → Opening → Use headset (Link or Simulator)** for headset testing. The optional desktop-preview menu enables a fixed camera and a plain petrol background when no XR display is active. This is explicitly a desktop preview, not a simulated capture of your room. Android builds always use the Meta rig.

See `HEADSET_TESTING.md` beside this document for Link prerequisites, native build instructions and performance limits.

Scene source: `Assets/SwitchyardRaiders/Startup/Scenes/SR_Launch.unity`  
Reusable prefab: `Assets/SwitchyardRaiders/Startup/Prefabs/SR_Launch.prefab`  
Timeline: `Assets/SwitchyardRaiders/Startup/Timeline/SR_Opening.playable`

## Validation boundaries

The editor validator covers readiness, focus gating, repeated completion, Timeline binding/duration, audio routes, shader import, build scene order and Meta feature settings. Desktop Play Mode also needs to reach `Holding` with the nameplate fully visible and audio finished.

Before release, run on Quest: confirm the real room appears before the title, passthrough stays clear at the edges, both eyes show the same highlight, seated placement is comfortable, a fresh returning-player pinch skips, and opening the system menu / removing and replacing the headset pauses and resumes without replaying cues. Check a cold launch and a recenter. The system splash screen is separate from this Unity scene and may precede it.

No headset was connected during authoring; XR Operator was unavailable and XR Simulator was stopped. A separate pre-existing Meta AI Agent Bridge port conflict (48735, multiple Unity editors) and old Photon Android importer warnings were observed. They are separate from the opening code; this task does not claim on-device or Bridge validation.

Meta SDK 207 behavior and current guidance were checked through the official Meta tools and installed package source: [Passthrough samples](https://developers.meta.com/vr/documentation/unity/unity-sample-starter-passthrough), [Meta core SDK agent skill](https://github.com/meta-quest/agentic-tools/blob/main/skills/hz-unity-meta-core-sdk/SKILL.md).
