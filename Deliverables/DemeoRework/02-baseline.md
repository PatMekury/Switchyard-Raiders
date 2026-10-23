# Task 2 baseline — measured after Task 1, before calibration

Recorded in the verified Switchyard Raiders editor, Unity 6000.6.0b5, SR_Launch, Play Mode false. This supplements, and does not replace, BASELINE_BEFORE_REWORK.md for Task 5's original MR-size request.

The checkout already contained Task 1's new tray/land, contact-grab changes, launch assets and play-entry timing work. Task2Baseline/ captures the starting Startup tree, player follower source, company motion source, contract and ignore rules. Its measurements-before.txt is the original connected-editor measurement output. Backups stay ignored; never restore a complete backup tree over later tasks.

| Measured/configured item | Task 2 starting value |
|---|---|
| Tray / usable land | 1.30 m square / 1.05 m square |
| Personal presentation scale | 1 |
| Gameplay root | y=.154, uniform .680 |
| All authored locomotive local scale | .0054117646 |
| Effective locomotive scale | .003680 |
| Company authored mesh bounds, W/H/L | 3.779993 / 4.358881 / 14.800000 m |
| Adler authored mesh bounds, W/H/L | 3.820000 / 4.245001 / 14.795000 m |
| Falke authored mesh bounds, W/H/L | 3.820000 / 4.126715 / 14.795000 m |
| Company displayed W/H/L | .013910 / .016040 / .054464 m |
| Player displayed length | .054446 m |
| Freight placeholder local bounds | .027 x .0405 x .060 m; multiplied by .680 |
| Railway | 128 broad box segments, .025 local width; ellipse radii .65/.42 before root scale |
| Simulation length / wagon spacing | 32 / .65 railway units |
| Player bogie centers | z=+/-4.15 authored metres; axle offsets +/-1.05 |
| Engine coupler anchors | z=+/-7.4, y=1.05 authored metres |
| Steam driving wheel centers | z=+1.85, 0, -1.85; y=.825 |
| Steam pilot centers | z=+5.20, +3.85, -4.10, -5.45; y=.50 |
| Seats | Under personal presentation, so would inherit zoom before this task |
| Prior zoom | Task 1 normal minimum, provisional absolute maximum 12 |

Bounds were assembled from imported mesh bounds transformed through the actual hierarchy, including SkinnedMeshRenderer assets; they are not headset measurements. No connected Quest was reported. XR Operator's CLI status reported its proxy unregistered/not running and the application server inactive. MR figures remain configured/calculated, with confirmed placement scale .35–1.0 pending Task 5.
