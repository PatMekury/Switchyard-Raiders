# Switchyard Raiders — UI direction v1
17 October 2026 · Visual proposal, not an implemented menu

**Layout superseded:** see UI_MOTION_V2.md. The user selected only a floating panel and buttons, with Spirit Sling's placement/follow behavior. The desk and locomotive display in the v1 illustration are not part of the chosen main menu.

## The feeling
A small railway dispatcher’s desk brought into the player’s room: tactile, welcoming, carefully made, and quick to operate. The visual setting remains Bergisches Land, 1910–1913. The UI belongs to the same world as the locomotives, signal cabins and freight facilities. Its railway styling is a game interpretation, not a claim that every control is historical equipment.

The approved title remains the most ornate element. Working controls are calmer: enamel faces, shallow iron edges, a little walnut and narrow brass detailing. Keep wear at edges, leave text fields quiet, and use readable bold humanist lettering for controls. The decorative title lettering does not become the body font.

## What comes from the three references
- **Spirit Sling:** the inspected menu prefabs and source use shaped components, material changes, hover/press scaling, sound and coordinated panel transitions. Adopt that consistent physical vocabulary and a familiar main-menu/lobby/readiness progression. Our surfaces are railway signboards and dispatch equipment.
- **First Hand:** its documented shaped plug/snap sequence separates an eligible target, accepted action and resulting reveal; hover highlights and accepted snaps serve different purposes. Apply that clarity to button presses, board placement and construction confirmation. Do not add a plug puzzle to our menu.
- **Demeo:** the supplied atlas emphasizes miniature presentation, clear candidate-versus-committed actions and persistent readable state. Apply that craftsmanship and decision clarity to the train display, route previews, inventory and reward feedback.

Evidence was reviewed from project files and the existing reference atlases, not newly observed live headset sessions.

## The three images
1. **SR_Button_Primary_Concept.png** — a blank green enamel button face, RGBA, 2073 × 758. Transparent outside the silhouette. Its ivory keyline frames a clear text area.
2. **SR_Panel_Concept.png** — a blank charcoal dispatch panel with shaped shoulders, walnut edge and restrained mounting hardware, RGBA, 1122 × 1402. Transparent outside.
3. **SR_MainMenu_Concept.png** — an illustrative composition, RGB, 1672 × 941. It includes a sample real-room background and adds sample labels to the skins. It is not an in-headset screenshot, final room layout or a promise of render performance.

The blank assets deliberately contain no labels. Live text, icons, interaction bounds and depth belong in separate UI elements. These are concept skins; production assets still need final sizing, edge inspection, scalable panel construction and in-headset legibility checks. Do not stretch the whole arched panel indiscriminately; keep the crown and corners fixed while adapting its plain center.

## Main menu
The title hangs above a shallow arrangement in front of the seated player. A compact menu sits beside a small locomotive-and-signal-cabin display. The room remains visible around both. The illustration uses a table to explain scale; the eventual MR layout must not require that exact real table.

- **PLAY 1v1:** find a match. The next state clearly says Finding opponent, with Cancel available.
- **PLAY WITH FRIENDS:** create or join a private room. Put the room code and player slots on the next screen, not on the main menu.
- **LEARN TO RAID:** practice the complete interception, collection and delivery loop. Recommend it to new players without adding a long instructions page.
- **SETTINGS / RECENTER:** smaller utility controls with comfortable interaction targets.
- Keep 2v2 out of the initial launch menu until supported.

For a first-time player, the practice action can receive a short Recommended tag. Matchmaking status must come from the real connection state; do not show invented player counts. No currency shop or daily-reward banners belong on this menu.

## Screen family and progression
**Title → main menu → chosen mode → placement/lobby preparation → ready → play.**

The existing title opening hands off to the menu once. The proposed menu reveal should follow it, not replay the title.

Placement uses explicit board handles and a Confirm position action. Back returns to the previous step. Adjusting the town must not also activate a menu control. Recenter affects the local layout.

The lobby looks like a concise dispatch roster. Each slot uses a player name, locomotive number, team emblem and colour. Ready locks that player's choice; any change to the required roster rechecks readiness. Start a short shared countdown only when the session is actually ready. Cancel/Leave stays available while waiting. A connecting state never falsely looks like a ready opponent.

Settings uses the same panel family with broad tabs and only a few controls visible at once. Group Music/SFX, comfort/layout, and accessibility. Sliders need a value as well as position; toggles need a clear On/Off state.

During a live multiplayer match, opening a personal menu does not imply that the match has paused. State this clearly in the pause/menu presentation.

## Personal gameplay tray
Use the same materials on a low, shallow, slightly curved console close to the player. It stays a comfortable physical size when the town is moved or scaled.

Group controls by action:
- **Identity and progress:** locomotive number, team emblem, banked delivery score and spendable credits, visibly separate.
- **Drive:** latched movement, direction and a prominent brake/stop control. Releasing the hand does not stop the train automatically.
- **Equipment:** distinct decoupler and construction-kit slots/counts; C, Y and Straight choices use actual rail silhouettes.
- **Selected device:** its identifier, the matching approach bar and Fire action. The matching device in town receives the same identifier/highlight.

The firing bar fills with approach, becomes clearly marked READY only inside the legal window, then closes when the coupling leaves. Use icon/word and shape changes as well as colour. READY permits a shot; it does not guarantee a hit or trigger an automatic shot.

Driving over a public supply pickup briefly highlights the corresponding inventory slot and increments it by one when accepted. No maintenance parcels or manual pickup prompt. Delivery gives a distinct banked-score confirmation and a separate credit increment; carried cargo is not yet banked.

## Proposed material and colour roles
| Role | Direction |
|---|---|
| Main surfaces | Charcoal iron #202D30 and muted walnut #674535 |
| Primary actions | Bottle-green enamel #254D43, warm ivory text #F3E6C8 |
| Trim | Restrained aged brass #B28D4B |
| Hover / selection | A clean ivory edge and shallow lift; avoid glowing the entire panel |
| Attention / pending | Amber plus a clear symbol and label |
| Danger / detection | Red, paired with a warning shape and explicit state |
| Team identity | Cobalt versus burnt orange, reinforced by emblem and locomotive number |

Colours are provisional. Team identity must remain distinguishable from action readiness. The title's red field is branding, not an alert. Readability will be checked against bright and dark real rooms.

## Motion and sound
All durations are proposed tuning values, not measured usability results.

| Trigger | Motion and sound | Duration / resolution |
|---|---|---|
| Main menu becomes available | Backplate rises a few centimetres and settles; buttons arrive in a short stagger; quiet wood/metal seating sound | 350–450 ms overall; no repeated cinematic |
| Finger approaches eligible button | Small face lift, ivory keyline strengthens, one restrained contact-ready tick | 80–120 ms; reverses on exit |
| Deliberate press / pinch confirmation | Face compresses into its backing, dry enamel/metal click | 60–90 ms in, 100–140 ms return; action confirmed once |
| Change screen | Current content withdraws; next content settles into the same spatial anchor | 180–250 ms; controls disabled during the transition |
| Player becomes ready | Roster badge seats into its slot; checkmark and word READY appear | About 160 ms; stable afterward |
| Equipment collected | Corresponding tray slot flashes once and count steps up | 200–300 ms; no particle shower |
| Cargo delivered | Brief ledger/count change, small acknowledgement cue | 300–450 ms; score and credit remain visible |
| Invalid action | Short restrained lateral nudge and a specific reason | About 100 ms; never a prolonged shake |

Choose one restrained idle detail, such as a rare small locomotive steam puff. No constant bobbing of controls, pulsing text, spinning gears or endlessly sweeping highlights. Provide reduced motion; preserve state changes and feedback while removing travel/overshoot. Hands receive visual and audio confirmation, not a promise of controller vibration.

Direct touch is used near the player; point-and-pinch supports comfortable distant access. Inputs do not require sustained raised hands. The near-target press and feedback direction follows Meta's [Hands UI best practices](https://developers.meta.com/vr/design/hands-ui-best-practices). Exact physical target sizes and spacing must be established in headset, not inferred from this raster mockup.

## Implementation direction after visual review
Use shallow geometry for silhouettes and press depth, image skins for material character, and separate crisp text/icons. Reuse a small material set, animate only changing elements, and stop animation updates at rest. Reserve Timeline for the one-time menu entrance and major transitions; repeated button feedback should respond immediately. No VFX Graph or video is needed for ordinary UI feedback.

This pass creates only concept images and a plan under Deliverables. No scenes, prefabs, build settings or reference projects were changed. Quest Link remains the intended testing workflow; no build was started.

## Reviewed sources
- Spirit Sling: Assets/SpiritSling/Common/UI/Scripts/CustomButton.cs; TableTop/Menu/Scripts/UIAnimation.cs; UIConfig.cs; States/TabletopMainMenuState.cs; menu prefabs; existing SpiritSling gameplay guide.
- First Hand: output/pdf/FirstHand-Gameplay-VFX-World-Atlas-Revised.md, opening and plug/snap sections; Assets/Project/Prefabs/UI and Intro.
- Demeo: output/pdf/Demeo_Gameplay_Interaction_World_Atlas.md in the Spirit Sling reference project. Its evidence is the supplied recorded play, not source implementation.
- Current rules: Unity-SpiritSling/output/design/Switchyard_Raiders_Updated_Gameplay.md. The copy at Switchyard Raiders/Switchyard_Raiders_Updated_Gameplay.md is older and was not used to override updated supply-collectible rules.
