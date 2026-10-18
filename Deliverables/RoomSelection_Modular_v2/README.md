# Modular room selection

Installed scene: Assets/SwitchyardRaiders/Startup/Scenes/SR_Launch.unity
Saved prefab: Assets/SwitchyardRaiders/Startup/Prefabs/SR_Launch.prefab

## Replace the icons
Select SR_Launch > SR_RoomChoice. The ModularRoomMenuMotion component exposes Real Room Image and Virtual Room Image. Open either referenced Image and replace its Source Image with your PNG imported as Sprite (2D and UI). The ivory backplate is a separate sibling and remains intact. Keep Preserve Aspect enabled and Raycast Target disabled on the icon.

## Artwork
The existing SR_Button_Primary_Concept.png supplies the heading and both buttons, with live editable text. Rails and ivory icon backplates were made separately with the built-in image-generation tool, then assembled into an atlas. The small line icons are provisional replaceable PNGs. Source art is retained here; the actual runtime art is in Assets/SwitchyardRaiders/Startup/RoomMenu/Art/Modular.

Rail art prompt direction: preserve the approved branching ribbon route; sculpted miniature steel/brass rails and timber sleepers, true transparency, no ground, buildings, buttons or text. Match the existing enamel button's craftsmanship.
Backplate prompt direction: one blank pale ivory enamel railway token, thin green inset rim, brass bevel and dark iron underside; true transparency, no icons or text.

## Motion and interaction
A 0.65-second staggered entrance; subtle lift and tilt of icon pieces on hover; inward press of visual button faces; hover sheen; signal illumination and brief de-emphasis of the unselected destination after selection. The rail highlight sweeps occasionally; geometry remains still. Hover animations do not move the hit rectangles or SDK interaction surfaces. Hands and follower placement are unchanged.

RoomChoiceMenu.Hide() performs the exit animation when a following screen is ready. No following main-menu screen is currently connected, so selecting a room leaves this selector accessible for changing room instead of fading to an empty screen.

## Performance and verification
One 2048x1024 atlas, mipmaps, ASTC 6x6 on Android. Baked material detail with an XR stereo-compatible unlit shader; no real-time lights, VFX Graph, physics or per-frame texture generation for the menu. Decorative Images do not receive pointer hits. Entrance/confirmation/exit controller sleeps when settled. This is a cost-conscious implementation, not an FPS measurement.

Use unity-validation.txt and saved-editor-preview.png for static verification. No Play Mode, headset testing, restart or player build was performed. The original scene and prefab snapshots are retained beside this file for recovery.
