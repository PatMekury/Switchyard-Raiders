# Spirit Sling menu interaction audio

Copied from the local Unity-SpiritSling project, with its MIT copyright/license notice retained in `Assets/SwitchyardRaiders/Startup/Audio/SpiritSlingUI/SpiritSling-LICENSE.txt`.

Reference inspection: `Common/UI/Scripts/CustomButton.cs` plays hover on pointer entry and click on successful release; `TableTop/Menu/Prefabs/TableTopMenu.prefab` assigns confirm, back and begin variants. `Common/Scenes/Boot.unity` assigns `sfx_transition_menu` to `TabletopMusicManager._menuTransition`. The menu prefab assigns `sfx_begin_game_decompte` to its countdown state.

| Clip | Switchyard Raiders use |
| --- | --- |
| sfx_ui_over_menu | First hand pointer entering any enabled menu button |
| sfx_ui_clic_valid_menu | Your Room and Virtual Room clicks |
| sfx_ui_clic_back_menu | Change Room click |
| sfx_ui_clic_begin | Find a Match, Play with Friends, Learn and Practice; plays only once those buttons become enabled |
| sfx_transition_menu | Room selector and main-menu opening |
| sfx_begin_game_decompte | Imported and assigned for the future game-start countdown; not played during room selection |

All three audio channels route to the existing SR_Startup mixer / SFX group. No mixer structure changes. Separate hover, click and transition AudioSources remain above the panels so menu dismissal does not cut off a click. Each channel uses one voice; hover is throttled to 90 ms, click to 50 ms. Stereo clips use 2D playback, matching their UI role. New source volumes are 0.45 hover, 0.65 click and 0.4 transition; adjust these on the `SR_RoomChoice` audio children if desired.

Import settings: original WAV files retained, stereo preserved, source sample rate retained, Vorbis quality 0.75, Decompress On Load, preload enabled. No runtime file loading, per-click GameObject creation or VFX changes.

The setup command `Switchyard Raiders / Audio / Connect Spirit Sling UI sounds` is repeatable and only updates audio routing/components. It preserves the current UI art and typography. The validation command writes `validation.txt` here. Validation is in Edit Mode; no headset audition, Play Mode, restart or player build is performed.
