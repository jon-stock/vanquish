# Agent Notes — Vanquish

## Save data policy — keep it in sync with new features

The theatre map has a real save/load system (`Core/SaveData.cs`/`SaveSystem.cs`,
reachable via the Escape pause menu — see below). **Whenever you add a new piece of
persistent player state to the theatre map, extend `SaveData` and
`TheatreMapHarness.SaveGame`/`ApplySaveData` (or the equivalent for whatever system
you're adding) in the same change, not as a follow-up.** It's easy to add a new
feature (a new site type's extra fields, a new per-Plan stat, a new resource, a new
per-tile flag, etc.), forget the save file doesn't know about it, and end up with data
that silently vanishes on save/load or New Game — the player has no way to know their
progress on that feature isn't actually being persisted until they lose it.

Concretely, when adding a feature, ask: "if the player saves, loads, or starts a new
game right after using this, does it survive?" If not, either add the field(s) to
`SaveData` (see `SavedHexOwnership`/`SavedSite`/`SavedPlan`/`SavedInventoryEntry` for
the existing pattern — plain serializable data, enums stored as strings so `Core`
doesn't need to depend on `Vanquish.Theatre`) and wire it into both `SaveGame` and
`ApplySaveData`, or explicitly document in `PLAN.md`/a code comment that it's
deliberately not saved yet and why (matching how turn number, resource pool, and
production queues are currently called out as known, deliberate gaps — not
oversights). Silent gaps are the failure mode to avoid; explicit, documented gaps are
fine.

If you add an entirely new save-load-worthy subsystem outside the theatre map (e.g.
personnel/operators once that exists), it's fine to give it its own saved-state
section following the same plain-DTO-with-string-enums pattern, rather than trying to
force everything through the existing theatre-specific fields.

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

### Moving specific assets beyond "fully procedural" (art pipeline, experimental)

Every visual in this project (drones, missiles, hex tiles, site buildings) is
built procedurally from Unity primitives at runtime, colored with a flat
`material.color` — no imported textures/meshes anywhere (see `SiteVisualBuilder`/
`DroneVisualBuilder`/`HexMeshFactory`). That was a convention, not a hard
constraint from the project owner — real (hand-drawn or AI-generated) textures
are welcome for specific assets when asked for, starting experimentally with the
quadcopter.

The workflow for that: `Assets/_Project/Scripts/Editor/ObjExporter.cs` is a
generic Wavefront OBJ/MTL exporter for any GameObject hierarchy (every
`MeshFilter` becomes its own named `o` group — e.g. frame/arms kept separate
rather than fused into one blob — vertices baked into the export root's local
space, MTL colors sampled from each part's current placeholder color as a
rough paint-by-numbers reference). `QuadcopterWireframeExporter.cs` uses it via
**Vanquish > Export Quadcopter Wireframe (OBJ)** in the Editor menu (or
`-executeMethod Vanquish.EditorTools.QuadcopterWireframeExporter.
ExportQuadcopter` in batch mode) to build `DroneVisualBuilder.
BuildStaticAirframeForExport` — the static airframe only (frame/arms/legs/
camera/battery), deliberately excluding motors/rotor blades — and write it to
`Assets/_Project/Art/Resources/Models/Quadcopter.obj` (+ `.mtl`), which is
*also* the live asset `DroneVisualBuilder.Build` now instantiates in-game for
every Quadcopter (via `Resources.Load`, so it works in both the Editor and real
builds) instead of the old procedural body — motors and spinning rotor blades
are always still built procedurally on top of it (`BuildRotor`/`RotorSpinner`),
never imported, since a plain OBJ export has no per-part pivots/animation to
carry (every vertex bakes into one shared root space, and Unity's importer
further merges every part into a single combined mesh) — importing the rotors
too would make them static and stop spinning. Replace
`Assets/_Project/Art/Resources/Models/Quadcopter.obj` directly (same path) once
real textured art exists — no code changes needed for that swap. Same exporter
pattern (`ObjExporter.Export(root, path)`) is reusable for any other asset that
needs the same treatment later (a building via `SiteVisualBuilder`, missiles,
etc.) — just build it in an Editor script and call the exporter, no need to
duplicate the OBJ-writing logic.

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

Open that scene and press Play for a real, visible hex-grid theatre map: a 10x10 patch
of procedurally-meshed hexes (`HexMeshFactory` — no imported art, same convention as
the drone visuals), 50 hexes per side, colored by terrain (open/road/mountain)
and by owner (blue Player / red Enemy), with a road cutting across the middle and a
couple of mountain flanks for terrain variety. Mountains are wild, impassable,
Neutral terrain that renders as a distinct conical peak with a snow-capped summit
(`HexMeshFactory.CreateConeMesh` drives a rocky cone + a small white cap stacked on
the raised hex prism — still procedural primitives, no imported art) rather than a
flat-topped cylinder; they can never be built on, captured, or occupied by an army.
Two factories and two bases per side are rendered as distinct procedural silhouettes (see
`SiteVisualBuilder`, below) rather than plain markers. WASD or left-click-drag pans
the camera, right-click-drag orbits/rotates it, scroll zooms (smoothly — eases toward
the target distance rather than snapping).

The UI is deliberately split into two lightweight pieces rather than one always-open
panel dumping everything at once: hovering the mouse over a hex/site/army (no click)
shows a small tooltip with just the basics (`TheatreMapHarness.DrawHoverTooltip`) —
e.g. an army's name/rank, or a site's type/owner and a one-line hint at what clicking
it opens; clicking selects it and populates a fixed **bottom action bar**
(`TheatreMapHarness.DrawBottomBar`) with one card per relevant action/unit, plus the
turn counter/resources and an "END TURN" button that live on the bar's right side at
all times. The selected hex (or a selected army's hex) also gets a strong orange glow
(`HexTileView.SetSelected`) distinct from the (yellow) move-destination highlight
described below, so it's always obvious what's currently open — if the selected hex
is adjacent to Player territory, a "Capture" button appears in its info line (a simple
stand-in for "won a combat instance here" until the real combat-instance-to-
theatre feedback loop exists). The button is disabled for mountains, which are
impassable wild terrain and never capturable. "END TURN" ticks site construction/production and
checks both victory conditions (economic collapse, sustained territorial control)
live.

Owned empty hexes get a row of **build cards** (`SiteBuildCatalog`: Factory,
Warehouse, Base, Airfield, Radar Installation, Recon Station, Lab — each with a turn
cost), blocked on Mountain/Road terrain or an already-occupied hex. Selecting a
mountain shows an "Impassable — no capture or construction" card (no Capture button,
no build cards). Selecting an
**Operational, owned Lab** shows a design card: name a new Plan (must be non-empty
and unique) and pick Quad/Hex/Missile — designed Plans get their own summary card
with a small rendered preview icon (`PlanIconRenderer`, reusing `PlanPreviewBuilder`/
`Combat/Play/DroneVisualBuilder`), not just a name. Selecting an **Operational,
owned Factory** shows one card per designed Plan (same preview icon), each with a
"Build" button that delivers straight to the first eligible Warehouse with room
(`SiteStorageCatalog`/`TheatreMapHarness.TryQueueProduction`) — manufactured output
always lands in a Warehouse first, never straight into an Airfield (storage is
site-scoped and capacity-limited: Airfields 50 drones/200 missiles, Warehouses 1000
drones/4000 missiles; Factories hold no stock of their own), not an unlimited global
pool. Queued orders tick down via the normal turn flow and land in the Warehouse's
storage once complete (an order whose Warehouse has since filled up waits at
"awaiting storage space" rather than being lost, shown as its own card). Tech
research at a Lab is not implemented yet — see PLAN.md for what's deferred.

Every Plan preview icon (`PlanIconRenderer.GetOrCreateIcon`) is a real rendered
snapshot, not hand-drawn art: the preview model is built far below the visible map
(never in the main theatre camera's view), captured once by a disposable one-shot
camera into a cached `Texture2D` keyed by the plan's name/category/accent color, then
reused everywhere that Plan appears (Lab/Factory/Warehouse/Airfield/army-composition
cards) — designing/producing more of the same Plan never re-renders it. This replaced
an earlier version that instead spawned actual 3D models floating in world space next
to whichever building was selected, which read as unclear/out of place; the icons
live entirely inside the bottom bar's cards now.

Every stock card at a Warehouse or Airfield has its own **Move** button — clicking it
enters "pick a destination" mode: every other Player-owned, Operational Warehouse/
Airfield with room lights up (the same highlight used for army moves — see
`HexTileView.SetHighlighted`), and clicking one of them queues a
`Theatre/Play/TransferOrder` moving that Plan's *entire* current stock there
(`TheatreMapHarness.TryBeginTransfer`/`EligibleTransferDestinations`) — clicking
anywhere else instead cancels picking mode. The shipped amount leaves the source
site's storage the instant the transfer is queued (so the number visibly drops right
away) and only lands at the destination once the transfer completes — always exactly
1 turn later, regardless of how far apart the two sites are (no travel-time-by-
distance modeling in this POC); in-progress transfers show as their own "in"/"out"
cards on both ends. This is how stock ever reaches an Airfield to be deployed, since
production can no longer target one directly.

Every Operational site's own summary card also shows its health and, if damaged, a
**Repair** button (`Site.BeginRepair`) — but only if it has an unbroken supply line
of same-faction-owned hexes back to one of that faction's Base sites
(`TheatreMapHarness.HasSupplyLineToBase`, a BFS over `HexGrid`); a site cut off behind
enemy-held territory shows "Repair needs a supply line to a Base" instead of the
button, even if it's still standing. This only gates Repair for now — a natural
extension point if other logistics-dependent actions ever need the same rule.

Selecting an **Operational, owned Airfield** shows a card per stored drone/missile
Plan with +/- steppers to stage a selection, plus a "Deploy Army" card that moves the
staged selection out of storage into a brand-new field **Army** (`Theatre/Play/
Army.cs`) — only Airfields can deploy armies. Every army gets a random name
(`Army.Name`, drawn from 10 adjectives x 10 nouns — "Iron Wolves," "Crimson Falcons,"
etc. — 100 combinations) and starts at Private rank, ranking up (`ArmyRank`/
`Army.Rank`) as it accrues experience (`Army.Experience`) from combat wins — shown as
a row of chevrons (one per rank tier: Private=1 chevron, Corporal=2, etc. —
`TheatreMapHarness.DrawRankBadge`) rather than a numeric progress bar, purely cosmetic
progression with no stat bonuses yet. Every army is rendered as an actual small flying
drone (`ArmyMarkerView`, built via the same `Combat/Play/DroneVisualBuilder` combat
units and Plan previews use, quad/hex silhouette picked by whichever category it
holds more of) with a name+rank tag floating above it and a status disc underneath
that dims once it runs out of missiles, regardless of which faction owns it. An army
is directly clickable (no more "select the tile, then find it in a list" detour,
though a small per-army card on the selected hex still exists to disambiguate
multiple stacked armies) via its own collider. Selecting a movable Player army
highlights its 6 neighboring hexes on the map (`HexTileView.SetHighlighted`) —
clicking a highlighted hex (whether it's empty, holds a hostile army, or holds a
hostile site) both selects the destination and issues the move/attack order in one
click (`TheatreMapHarness.TryMoveArmy`), no directional buttons needed. Moving onto
an empty hex not already owned by the mover captures it (same stand-in as the
"Capture" button above); moving onto a hostile army or hostile site resolves a real,
instant combat-instance fight via `Theatre/Play/TheatreCombatResolver.cs` — it builds
an actual `EngagementController`/`Stockpile`/`IObjective` (the same combat-instance
machinery Phase 0/1 built, reusing the payload/hardness soft-cap damage model) and
runs an AI strike loop headlessly rather than opening a separate playable 3D scene,
awarding the attacker experience on a win. An army needs both drones and missiles to
be combat-effective; a fight it can't win destroys it outright, while a fight it wins
damages/destroys the target site or wipes the defending army. Neither a defended
site nor a defending army has any active defensive fire of its own yet (no
theatre-level point-defense/garrison model) — a known, deliberate simplification
alongside the still-fully-deferred combat-instance-to-theatre feedback loop for
player-fought (non-army) engagements. Since nothing actually shoots back at an army,
losing a fight never destroys the attacker outright — it just fails to break through
and holds its original position having expended its missiles (only a fully-depleted,
drone-less army can actually disappear from the map). Selecting an army shows one
card per Plan it holds (the closest analogue to "a card per unit" the pooled
army-composition model supports), each with **Resupply**/**Unload** buttons against
whichever Airfield it's currently standing on (`TheatreMapHarness.TryRestockArmy`/
`TryUnloadArmy`); if another same-faction army shares its hex, a **Merge in** card
folds it wholesale into the selected army (`TryMergeArmies`, summing composition and
experience) — no auto-merge, so multiple same-owner armies can freely stack on one
hex until the player chooses to combine them. A small compass in the top-right
corner (`TheatreMapHarness.DrawCompass`) counter-rotates against the camera's orbit
yaw (`TheatreMapCameraController.Yaw`), with a red needle, so "N" always points to
true north regardless of how the map's been rotated.

Sites are no longer uniformly-scaled colored cubes — `Theatre/Play/
SiteVisualBuilder.cs` builds a distinct primitives-only silhouette per `SiteType`
(Factory: hall + annex + smokestack; Warehouse: body + angled roof panels; Base:
barracks + annex + radio mast; RadarInstallation: mast + tilted dish; LaunchPlatform/
Airfield: pad + control tower; ReconStation: tower + dome; Lab: block + dome), still
tinted by owner/state like before (`SiteMarkerView` now tints every renderer in the
shape, not just one). True photorealistic/"ultra HD" building art isn't achievable
this way — that needs authored 3D art (modeled/textured/PBR meshes from an art
pipeline or asset store), a fundamentally different scope than assembling primitives
procedurally at runtime, consistent with every other visual in this project (drones,
missiles, hex tiles) being primitive-built rather than imported art.

**Escape** opens a pause menu (`GameMenuController`): Save Game, Load Game, New Game
(wipes and rebuilds the map fresh), and Quit. Save/Load round-trips every piece of
persistent theatre-map state: tile ownership, which sites are on which tile (type/
owner/state/turns-remaining/health), designed Plans, each Airfield's/Warehouse's
stored drone/missile counts, every deployed army (name, position, composition,
experience/rank, moved-this-turn flag), the turn counter and result, the per-faction
resource pool, in-progress
Factory production orders (including their chosen destination site), and the
territorial-control victory condition's sustain-turn counters (via
`Core/SaveData.cs`/`SaveSystem.cs`, JSON; `Site.Restore(...)` reconstructs a site
directly into its saved state; `TheatreTurnController.RestoreProgress`/
`TerritorialControlCondition.RestoreConsecutiveTurns` restore the rest). Camera
position/current selection are deliberately not saved (transient UI state, not player
progress).

Clicking a button in the corner panel or the pause menu no longer "reaches through"
to the 3D scene underneath (the previously-reported focus/selection bug) —
`TheatreMapHarness.IsPointerOverUI()` gates both the harness's own hex-click handling
and `TheatreMapCameraController`'s pan/orbit/zoom (now checking whether the pointer
is within the bottom action bar's screen-space height rather than a small corner
rect).

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
