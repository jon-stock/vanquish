# Agent Notes — Vanquish

## Unity Editor location

Unity is installed outside Unity Hub's default location. The Editor executable is:

```
C:\Users\Jon.Stock\UnityEditors\6000.0.5f1\Editor\Unity.exe
```

Discovered via Unity Hub's editor registry (`%APPDATA%\UnityHub\editors-v2.json`) —
check that file first if this path stops working (e.g. after a version upgrade).

The repo root (`.` — this directory) is the actual Unity project (`Assets/`,
`Packages/`, `ProjectSettings/` live here). There is no separate project subfolder.

### Headless smoke test

There is no `com.unity.test-framework` package available offline in this install, so
Phase 0 uses a plain headless smoke test instead of EditMode tests:

```
Assets/_Project/Scripts/Editor/SmokeTest.cs  →  Vanquish.EditorTools.SmokeTest.Run
```

Run it after any change to `Assets/_Project/Scripts/**` to verify the project still
compiles and the core combat-instance logic still behaves correctly, using the
project root as `-projectPath`:

```powershell
& "C:\Users\Jon.Stock\UnityEditors\6000.0.5f1\Editor\Unity.exe" `
  -batchmode -nographics -projectPath "<repo root>" `
  -executeMethod Vanquish.EditorTools.SmokeTest.Run -quit -logFile "<log path>"
```

Exit code 0 + `[SmokeTest] PASSED` in the log means it's good; a non-zero exit code or
`[SmokeTest] FAILED` (or a compile error aborting batchmode before the method even
runs) means something needs fixing before moving on. Extend `SmokeTest.Run` with more
assertions as new gameplay logic is added, rather than only relying on compilation
succeeding.

If `Assets/_Project/Scripts/**` fails to compile in a way that looks stale/wrong
(references a file/path that was already moved or deleted), delete the gitignored
`Library/` folder to force a full reimport before assuming the code itself is broken
— Unity's cached asset database can lag behind manual file moves done outside the
Editor.

### Interactive debug harness (press Play and click things)

There's no real gameplay scene yet (no flight/spawning/UI), but there is a
click-through debug harness for the Phase 0/1 combat-instance logic:

```
Assets/_Project/Scenes/Phase1_DebugHarness.unity
Assets/_Project/Scripts/Combat/Debug/EngagementDebugHarness.cs
```

Open that scene in the Editor and press Play — it builds its own objective,
attacker/defender stockpiles, and point-defense battery in code at `Start()`, then
draws an OnGUI panel with buttons to commit decoy vs. real strikes and watch the
stockpile-drain tactic, damage/hardness soft-cap, and win conditions play out live.
Regenerate this scene (rather than hand-editing it) if the harness's setup logic
changes shape, via:

```powershell
& "C:\Users\Jon.Stock\UnityEditors\6000.0.5f1\Editor\Unity.exe" `
  -batchmode -nographics -projectPath "<repo root>" `
  -executeMethod Vanquish.EditorTools.SceneBuilder.BuildPhase1DebugScene -quit
```

There's a second harness for the Phase 2 theatre-map logic (hex grid, sites, turn
resolution, victory conditions):

```
Assets/_Project/Scenes/Phase2_TheatreDebugHarness.unity
Assets/_Project/Scripts/Theatre/Debug/TheatreDebugHarness.cs
```

Same idea — open the scene, press Play, step turns and trigger either victory
condition (economic collapse or sustained territorial control) via the OnGUI buttons.
Regenerate via `Vanquish.EditorTools.SceneBuilder.BuildPhase2DebugScene`.

`SceneBuilder` now has a shared `BuildSingleComponentScene<T>` helper — add new
one-component debug-harness scenes for future phases the same way rather than
duplicating the create/save/exit boilerplate.

### The actual visible/clickable theatre map (not OnGUI buttons)

```
Assets/_Project/Scenes/Phase2_TheatreMap.unity
Assets/_Project/Scripts/Theatre/Play/*
```

Open that scene and press Play for a real, visible hex-grid theatre map: a 9x7 patch
of procedurally-meshed hexes (`HexMeshFactory` — no imported art, same convention as
the drone visuals), colored by terrain (open/road/mountain, mountains rendered
taller) and by owner (blue Player / red Enemy), with a road cutting across the middle
and a couple of mountain flanks for terrain variety. Two factories and two bases per
side are rendered as simple colored markers. WASD or left-click-drag pans the camera,
right-click-drag orbits/rotates it, scroll zooms (smoothly — eases toward the target
distance rather than snapping), and clicking a hex without dragging selects it (shows
its terrain/owner/site in the corner panel) — if the
selected hex is adjacent to Player territory, a "Capture this hex for Player" button
appears (a simple stand-in for "won a combat instance here" until the real combat-
instance-to-theatre feedback loop exists). "Advance Turn" ticks site construction/
production and checks both victory conditions (economic collapse, sustained
territorial control) live.

Owned empty hexes also get a **"Build here"** menu (`SiteBuildCatalog`: Factory,
Warehouse, Base, Airstrip, Radar Installation, Recon Station, Lab — each with a turn
cost), blocked on Mountain/Road terrain or an already-occupied hex. Selecting an
**Operational, owned Lab** opens a design panel: name a new Plan (must be non-empty
and unique) and pick Quadcopter/Hexacopter/Missile — designed Plans get an actual 3D
preview model next to the Lab (`PlanPreviewBuilder`, reusing `Combat/Play/
DroneVisualBuilder` for quad/hex plans), not just a name in a list. Selecting an
**Operational, owned Factory** opens a production panel listing every designed Plan
with a "Build (Nt)" button; queued orders tick down via the normal Advance Turn flow
and land in a simple player-wide inventory count once complete. The same 3D preview
also appears next to whichever Factory is selected, showing what it's building.
Tech research at a Lab is not implemented yet (a placeholder note says so in the
panel) — see PLAN.md for what's deferred.

Regenerate via `Vanquish.EditorTools.SceneBuilder.BuildPhase2TheatreMapScene`.
`TheatreMapHarness.Build()` is exercised directly by `SmokeTest` (grid/site counts,
capture eligibility logic on both a front-line and a deep-territory hex, turn
production), same headless-first-then-visual pattern as everything else here.

### The actual playable game (flyable quadcopter + missiles, not a debug screen)

```
Assets/_Project/Scenes/Phase1_FlightTest.unity
Assets/_Project/Scripts/Combat/Play/*
```

Open that scene and press Play for a real 3D combat instance: WASD to move, Space/
Shift for altitude, mouse to fire missiles at a physical, destructible "Base" target,
right-drag to orbit the camera, scroll to zoom. This reuses/adapts the **pre-pivot
project's** flight-control and procedural-visual layer (see the `pre-pivot-old-plan`
git tag) — `PlayerDroneController`, `WeaponController`, `MissileBurnController`,
the chase camera, `RotorSpinner`/`QuadcopterTiltVisual`, and the "no imported art,
build everything from primitives" convention — ported from the new Input System to
legacy `UnityEngine.Input` (no package dependency) and rewired from that project's
old `CombatManager`/`Health` onto this pivot's `EngagementController`/`BaseObjective`/
`Damageable`, so the exact same stockpile-economy and payload/hardness-soft-cap logic
proven in `SmokeTest` is what's actually running under the hood — `WeaponController`
reads/spends ammo directly from the `EngagementController`'s `Stockpile` (one source
of truth, not a duplicate ammo counter), and `MissileImpact` applies damage via
`IDamageable.TakeDamage` instead of a flat health value.

Regenerate via `Vanquish.EditorTools.SceneBuilder.BuildPhase1FlightTestScene`.
`FlightTestHarness.Build()` is also exercised directly (no Play mode needed) by
`SmokeTest`, including actually firing a shot and checking the stockpile decrements
and a real missile GameObject gets spawned — this catches wiring mistakes (null refs,
mismatched part ids) headlessly, though it cannot exercise real player input or
physics collisions that way.

The drone visual (body/arms/spinning rotors/mounted-missile props that visually
deplete per shot — `DroneVisualBuilder`/`MountedMissileVisuals`) is styled after a
real FPV/racing quad rather than one painted box: a dark carbon-fiber-look frame
plate, a raised flight-controller/battery stack, a battery pack slung underneath, a
low forward FPV camera, twin-blade props, and landing legs — with each unit's
identifying color used only as small accents (stack trim, arm tips) rather than one
flat saturated hull color, matching how real hardware actually looks. Drone
Rigidbodies also freeze rotation now (no yaw/roll/pitch control exists yet, so a
physics bump has nothing to right itself with otherwise).

The scene now has **two independently-controllable units** — a quadcopter and a
hexacopter (`Vanquish.Combat.Play.DroneRotorConfiguration`: same visual builder, just
a different rotor count) — each with its **own missile stockpile** (distinct
`StockpileEntry` part ids in one shared `EngagementController`, so firing one never
touches the other's ammo). Press **1**/**2** to switch which one you're piloting
(`PlayerUnitSwitcher`); the inactive unit holds position rather than drifting. The
camera automatically follows whichever unit is active, and **V** toggles a third
camera mode that looks at the objective instead (`CameraModeController`). An
"available units" bar across the bottom of the screen (`UnitRosterHud`) shows each
unit's label, remaining missiles, and which one is currently active.

## Commit policy

Commit changes to git (and push to `origin`) after each set of changes you make to
this repo, not just at the end of a long session. Keep commits reasonably scoped to
the change just made (e.g. one commit per Phase 0 task or logical unit of work,
not one giant commit for everything). Write a concise commit message describing what
changed and why, consistent with the rest of this repo's commit history.
