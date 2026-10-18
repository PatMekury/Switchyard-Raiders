# Switchyard Raiders — menu layout and motion revision
17 October 2026. Supersedes the desk and locomotive-display arrangement in UI concept v1.

## Confirmed layout
Only the panel and its buttons float in the real room, in front of the seated player. No desk, plinth or locomotive display. Preserve the railway material language and approved title treatment, with the title incorporated into the compact menu hierarchy.

Match Spirit Sling's menu placement/follow baseline: TableTopMenu.prefab has distance 0.65, yDistance -0.15, lerp 3, distanceThreshold 0.6, fixedXRotation true, applyAtStart true and applyOnUpdate true. These are serialized project values, not new headset measurements. CanvasFollowPlayer smooths the forward direction, starts repositioning when the desired-position displacement exceeds its threshold, and stops when within 0.02 units. Small movements therefore do not rigidly move the panel with the head. This menu distance is separate from the existing opening title's 1.5 m distance.

## Reference material finding
Spirit Sling's M_UI_FireButton_Default material references SH_KodamaFire.shadergraph, with time-driven scrolling noise, distortion and coloured rim/interior layers. Its default material has scrolling-noise intensity 0.98 and distortion strength 0.0137. The liquid-like appearance is a stylized shader effect, not evidence of a fluid simulation. CustomButton swaps state materials and eases scale over 0.25 seconds; script defaults are 1.1 hover scale and 0.8 pressed scale. Exact per-button overrides must be checked when implementing.

## Recommended signature
Keep solid railway frames around green inset faces. Inside each face, softly overlapping green and pale warm steam-like currents travel slowly upward and sideways, resembling vapour seen beneath polished glass. This is intentional game stylization. The brighter motion lives near the edges; the label stays sharp over a calm dark central area. A broad soft reflection periodically traverses the surface. Avoid equally bright overlapping bands or flashing every button together.

The wood and iron remain rigid. Animate surface light and internal patterns continuously; reserve positional motion for entrance, follow and actual input. The menu should feel alive without becoming a moving target.

## Motion vocabulary
All timings are proposed tuning values.
- Idle: a subtle internal steam current repeats without a visible seam, with a broad roughly 4–6 second reflection cycle and different phases per button. The target and lettering remain fixed.
- Hover: over 100–150 ms, the face lifts slightly, an ivory edge becomes clearer and the internal current gathers toward the selected point. Keep the text undistorted.
- Press: the face travels inward with a short spring compression over 60–90 ms. A single soft ripple crosses its interior; the brass rim catches a brief warm highlight. Pair with one dry click and a restrained air-release cue.
- Release/accepted: return over 120–180 ms with one small settle. A short light trace follows the frame and resolves; fire the action once.
- Entrance: panel fades and settles from a small lower offset; buttons reveal in 45–60 ms staggering, around 400–500 ms overall. Steam motion starts as each face appears.
- Screen change: keep the outer frame spatially anchored, slide old content a short distance out and new content in with a masked crossfade, around 200–250 ms. No need to spin the whole board.
- Waiting: a restrained moving dash follows a small track-shaped status path beneath readable status text. Keep Cancel still and accessible.
- Disabled: pause the internal flow and reduce saturation; preserve readable text and an explicit reason where needed.

Reduced motion keeps state highlights and the click but removes ripples, overshoot and content travel. A proposed comfort refinement is to hold follow repositioning during an active press; this is a recommendation, not an observed Spirit Sling behavior.

## Performance intent
Use a small shared shader with scrolling textures and masks for the surface motion, plus shallow geometry/transform motion for presses. No fluid simulation, per-button particle systems, video or real-time refraction is needed. This should be inexpensive for a small menu, but its cost still requires profiling in Quest Link and later on-device. Do not claim an FPS guarantee.

## Status
Planning only. No Unity scenes, scripts or materials were changed for this revision. The existing static button/panel images remain material references; they cannot themselves demonstrate the proposed motion. The v1 main-menu illustration is superseded by the panel-only layout above.

