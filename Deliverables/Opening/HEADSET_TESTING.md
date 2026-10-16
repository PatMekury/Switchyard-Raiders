# Headset and performance check — 16 October 2026

Both left and right OVRHand components are enabled, correctly assigned and connected to the opening. Meta XR extensions are enabled for Android and desktop OpenXR. The manifest requires hand tracking and passthrough. Your real hands remain visible through passthrough; the opening does not draw duplicate virtual hand meshes.

The opening starts automatically. Pinch-to-skip only applies after you have completed a previous opening on the device. The current scene ends at the title; gameplay and the next screen are not implemented here.

## Play through Quest Link

The Play/Stop repeat-start fix enables both domain and scene reload. The opening also resets its gate, Timeline presentation and Animator before playback. Do not use the previous configuration that skipped both reloads while testing this scene.

Verification: two separate desktop Play sessions completed successfully, with Stop between them. Both reached the visible final title, released the Timeline graph and stopped animation/audio updates. Results are saved in `repeat-play-first-pass.txt` and `repeat-play-second-pass.txt`. Quest Link playback after this fix still requires confirmation in the headset.

1. Enable hand tracking in the headset and start Quest Link.
2. In the Meta Horizon Link PC app, enable **Settings → Beta → Developer Runtime Features** and **Passthrough over Meta Quest Link**. Meta requires restarting Unity after enabling these switches. These machine settings were not changed by this task.
3. In Unity, open `SR_Launch` and choose **Switchyard Raiders → Opening → Use headset (Link or Simulator)**. This is the saved default.
4. Press Play after the Link session is running. The room should appear, followed by the title. Put the controllers down to test hands.

The PC's active OpenXR runtime was verified as the Oculus/Meta runtime. The failed session logged `ErrorFormFactorUnavailable xrGetSystem` and failed to initialize the OpenXR Display, before the opening's timeout. This means that session did not obtain an available headset from Link; the timeout was a consequence. A USB debugging device list does not establish whether a Link session is running.

If the Game view reports **No active OpenXR headset session**, stop Play, enter Link in the headset, then press Play again. If needed, reconnect Link first. If the session is active but no passthrough frame arrives, check the two Link development switches above. The editor now displays the waiting reason instead of leaving an unexplained black Game view.

For testing without a headset, stop Play and choose **Switchyard Raiders → Opening → Use desktop preview (no headset)**, then Play. This disables desktop XR initialization and plays against a solid background. Choose **Use headset (Link or Simulator)** before returning to Link; it restores desktop OpenXR initialization. Android XR settings are unchanged by these two preview menus.

## Test standalone on Quest

The attempted APK build was cancelled. No further builds are part of the current Quest Link testing request. The build menu below is available only for a later, explicitly requested standalone test.

Use **Switchyard Raiders → Opening → Build standalone Quest test APK**, or Build and Run with the Meta Quest build profile. The test build contains the opening scene and uses app identifier `com.Lumever.SwitchyardRaiders`, Vulkan, IL2CPP and ARM64. Enable developer mode and accept the USB debugging prompt before installing through Meta Quest Developer Hub or the Meta tools.

The APK output is `Builds/Quest/SwitchyardRaiders-Opening.apk`. Build status is recorded in `Builds/Quest/opening-build-status.json`. A successful build verifies compilation and packaging; it does not verify that passthrough works or that frame rate is sustained on the headset.

## Performance changes

- One unlit title mesh (two triangles), one texture sample, no video or particles.
- Dedicated forward renderer for the opening with no SSAO or other renderer features.
- GPU Resident Drawer is explicitly disabled in both pipeline assets. It is incompatible with this Forward renderer; Unity had already disabled it automatically. This removes the warning without adding render work to the single-mesh opening.
- Opening cameras disable depth/color texture copies, shadows, HDR, post-processing and occlusion culling.
- Android title texture uses ASTC 4×4 with mipmaps, no CPU-readable duplicate.
- Material/transform updates are skipped when animation values have not changed. On completion, Timeline's graph is released and the title Animator/presentation/controller stop updating.
- The Quest profile uses the existing valid **Mobile** quality level (4× MSAA, render scale 0.8). The template's extra default quality level had a missing pipeline reference and is excluded from this build profile.
- Native Quest requests **90 Hz only when the runtime reports support**; otherwise the current runtime rate is retained. Link uses the PC Link settings. No fixed CPU/GPU clock override is applied.

## What still needs a headset

Run a cold launch, a repeat launch with a fresh index pinch, system-menu interruption, headset removal/replacement and recentering. Confirm that the title remains stable in both eyes and the room is visible around its transparent edges.

Measure the standalone APK with Meta OVR Metrics / a performance trace: app FPS, CPU/GPU frame time and dropped/stale frames. At 90 Hz the frame budget is about 11.1 ms; at 72 Hz it is about 13.9 ms. A refresh-rate request is not a measurement or a guaranteed FPS result. PC/Link performance does not represent standalone Quest performance.

XR Operator was unavailable, XR Simulator was stopped, and no USB headset was detected during this pass. The existing Agent Bridge port 48735 collision between open editors also prevents claiming Bridge-based runtime validation.

Sources checked through Meta's official tools: [Passthrough and hands over Link](https://developers.meta.com/vr/documentation/unity/unity-passthrough-use-over-link), [display refresh rates](https://developers.meta.com/vr/documentation/unity/unity-set-disp-freq), [mobile GPU rendering](https://developers.meta.com/vr/documentation/unity/po-advanced-gpu-pipelines).
