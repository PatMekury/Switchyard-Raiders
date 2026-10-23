# Shared pre-rework measurement baseline

Captured 2026-10-22 (America/Chicago), before Task 1 geometry edits, checkout `ce0fdcf19adc0445449001aa4723638541fbb376`. Raw Edit Mode measurements: `Baseline/editor-before.txt`. Original startup, table and terrain assets (including uncommitted changes) and editable Blender sources are copied under `Baseline/`; initial hashes/status are there too. Do not restore the entire startup snapshot over later work.

These are configured/calculated measurements, NOT observed headset VR/MR dimensions. Unity was in Edit Mode; metavr returned no devices and port 8720 had no listener. No live MR placement scale is available.

| Quantity | Before Task 1 |
|---|---|
| Normal VR presentation scale | 1 |
| Table exterior | 1.900 x 1.400 m |
| Usable table inset | 1.560 x 1.110 m |
| Land slab | 1.500 x 1.050 x 0.150 m, bottom-centre origin |
| Table height / rim | wood top 1.050 m, cup lip 1.053 m |
| TownMount in table | y=1.018 m |
| Table origin in presentation | y=-1.018 m |
| Land surface in presentation | y=0.150 m (exposed above table) |
| Railway root | y=0.154 m; uniform scale 0.920 |
| Locomotive local / effective scale | 0.004 / 0.003680 |
| VR recenter | centre 1.000 m forward of horizontal head direction, origin 0.500 m below eyes; near exterior edge 0.300 m forward |
| Previous zoom limits | absolute 0.2 to 12; table hidden above 1.6 |

Calculated rest-pose skinned mesh envelopes, including available LODs, excluding smoke and labels: company locomotive width 0.013910 m, height 0.016040 m, length 0.054464 m; Adler width 0.014058 m, height 0.015622 m, length 0.054446 m; Falke width 0.014058 m, height 0.015186 m, length 0.054446 m. These use imported mesh bounds transformed through the existing hierarchy, not animated or device-observed bounds. MeshRenderer-only inspection returns zero for these skinned locomotives and is not a valid size measurement.

MR previously hid the complete virtual table and displayed the same land/trains. `RaidersRoom.TryPlace` tested scale 1.00 down to 0.35 in 0.05 increments, two orthogonal orientations, picking the nearest suitable TABLE with a scale penalty. Thus configured MR land bounds were `(1.50*s, 1.05*s)` m, with s in [0.35,1.00]; 1.50 x 1.05 m at s=1, 0.525 x 0.3675 m at s=.35. Train linear dimensions above multiply by s. Placement origin was 6 mm above the scanned surface; the 7x7 footprint query added 15 mm physical clearance outside each edge. MR exploration could exceed the scanned table.

Task 5 must double the chosen PRE-REWORK displayed linear reference, not blindly double Task 1's new square geometry. At the old configured full scale the requested target is 3.00 x 2.10 m of land; at an observed old scale s it is 3.00*s x 2.10*s. There is no measured s here. Resolve that baseline policy explicitly; do not invent a prior physical-table observation or silently stretch the new square tray.

Simulation placement remains `RaidersMatch.Point(d)=(sin(a)*.65, Height(d)*.25, cos(a)*.42)`, a=d/Length*2*pi, Height=.18*(1-cos(d*pi/8)). Players start at `(i%2)*16+(i/2)*2.2`, company at d=8, using the same personal presentation transform. Authoritative travel, speed, score, cargo and network state are independent of that transform.
