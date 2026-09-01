# Vanquish — Development Plan

## Project Philosophy & Core Priority
**Core First, Breadth Second:** The primary objective is to build a solid, functional baseline simulation loop before expanding into high-tier parts, micro-tuning sub-menus, or secondary mechanics. Data structures must be built to accommodate future expansion, but initial execution must focus on getting immediate phases fully operational end-to-end.

---

## Concept Summary

Vanquish is a two-mode combat game built in Unity (C#):

1. **Workshop / R&D mode** — research, assemble, fine-tune, and visually inspect modular missiles, drones, and support infrastructure within strict mass and power constraints.
2. **Combat & Sandbox Campaign mode** — deploy your designs across a dynamic, territory-based campaign map and real-time tactical battles, progressing from Cold War hardware up to cutting-edge stealth CCA platforms and hypersonic air-to-air missiles.

Both modes share one unified, data-driven physics model so that testing a design in the workshop uses the exact same simulation as live combat.

---

## Core Systems

### Mass Budget & Engineering Trade-offs
- **Maximum Take-Off Weight (MTOW)**: Every airframe and missile hull enforces a strict MTOW limit. Players trade off dry mass, engine weight, warhead mass, and fuel volume.
- **Discrete Parts vs. Continuous Loadouts**:
  - *Discrete Selection*: Airframes, engines, warheads, seekers, and coatings have fixed dry mass, cost, and baseline stats.
  - *Continuous Sliders*: Variable fuel/propellant fill levels and battery cell counts.
- **Estimated vs. Empirical Telemetry**: The Workshop UI displays *Estimated Range*, *Estimated Burn Time*, and *Estimated Thrust-to-Weight Ratio (TWR)* based on theoretical calculations. Environmental variables (drag, wind, altitude) make empirical validation in the Test Range necessary for exact performance curves.

### Visual Workshop Assembly & Telemetry Overlays
- **Modular 3D Mesh Swapping**: Real-time rendering of custom designs. Changing nose cones, seekers, engines, wings/rotors, or materials instantly updates the physical 3D model on designated airframe nodes.
  - *Reference UI target (see Phase 3's "Full UI/UX pass" for the tracked checklist item)*: a
    free-rotate/zoom 3D design viewport as the centerpiece of the missile/drone designer
    screen, with a named design title, a compact overlay spec card (length/diameter/range/
    ceiling/payload-style summary), and per-slot part-picker dropdowns showing mass/cost —
    not just a static preview icon or a plain text stat dump.
- **Visual Scaling**: Continuous fuel/battery sliders physically scale internal cell/tank meshes or adjust fill levels in an transparent view mode.
- **Toggleable Visualization Overlays**:
  - *Internal / X-Ray Mode*: Displays internal sub-components, mass distribution, and Center of Mass (CoM) vs. Center of Lift (CoL) shifts.
  - *Aerodynamic Airflow / Drag Mode*: Displays real-time wind-tunnel streamline vectors and drag hotspots. Mounting weapons on external hardpoints visually increases local drag turbulence, whereas internal bays maintain a clean aerodynamic profile.

### Physics, Aerodynamics & Environment
- **3-DOF Physics Core**: Point-mass aerodynamic model calculating Thrust, Lift, Drag, and Gravity. Orientation is visually aligned to velocity (`orientToVelocity`).
- **Modular Physics Interface (`IAerodynamicBody`)**: Decoupled interface allowing seamless expansion or post-1.0 upgrades without rewriting vehicle controllers.
- **Atmospheric Model**: Air density ($\rho$) decays exponentially with altitude ($h$). Drag ($F_d = \frac{1}{2} \rho v^2 C_d A$), aerodynamic lift, engine oxygen efficiency, and fuel burn scale dynamically.
- **Terrain & Line-of-Sight (LOS) Masking**: Mountains, terrain features, and weather physically block radar pings, laser designation lines, and command datalinks.

### Damage & Component Destruction Model
- **Dual-Layer Health System**:
  - *Airframe Structural HP*: Overall structural integrity. If total airframe HP reaches zero, the vehicle is destroyed immediately.
  - *Proximity Sub-Component Damage*: Explosive blasts and proximity detonations calculate damage against individual module hitboxes (engine, fuel tank, seeker, control surfaces, rotors).
  - *Functional Penalties*: Punctured fuel tanks accelerate fuel loss; seeker head damage breaks target tracking; rotor/wing damage degrades roll and lift control.

### Guidance, Sensors & Jamming Dynamics
- **Seeker Spectrum (Cold War → Cutting-Edge)**:
  - *Fire-and-Forget (FAF)*: IR reticle, Imaging IR, Active Radar. Decoys (flares/chaff) trigger a direct lock-break check based on target signature versus countermeasure effectiveness.
  - *Guided / Command*: Wire/SACLOS, Laser/Beam-riding, Semi-Active Radar (SARH), Datalink.
- **Signal-to-Noise Ratio (SNR) Jamming**: Guided sensors experience progressive SNR degradation ("fuzzing") from active ECM until reaching burn-through distance.

### Drone Propulsion & Fuel Spectrum
- **Electric**: Battery slider, KV motor tuning (multirotors/small fixed-wing).
- **Internal Combustion Engines (ICE)**: Petrol or Diesel fuel (high endurance, distinct thermal/acoustic signature, fixed dry engine mass).
- **Gas Turbine / Jet**: Jet Fuel (subsonic and supersonic jet/CCA tiers).

---

## Sandbox Campaign & Meta-Game Loop

- **Dynamic Territory Map**: Strategic overworld map divided into contested sectors, supply lines, and operational bases.
- **Resource Acquisition & Logistics**:
  - Win battles and capture strategic sectors to earn base currency and high-tier materials (e.g., Radar Absorbent Material, Titanium alloys, Scramjet components).
  - Manage base infrastructure (radar networks, SAM sites, ground launch platforms) to maintain datalink coverage across contested sectors.
- **Deployment & Interception**: Intel pings notify the player of incoming enemy strike groups, requiring fast deployment of custom-built interceptors or strike drones tailored to the specific threat.

---

## Technology Stack

- **Engine**: Unity (C#), URP
- **Data Model**: ScriptableObjects for part definitions and tuning parameters; JSON for campaign state and custom design saves.
- **UI**: Unity UI Toolkit (Tech tree graph, workshop tuning dashboard, dynamic HUD overlays, telemetry HUD).
- **Physics**: Unity PhysX + custom 3-DOF aerodynamic force and atmospheric density components.
- **AI**: Unity Behavior Trees / Custom FSM.
- **Version control**: Git + GitHub.
- **Testing**: Unity Test Framework (EditMode for stat calculation validation, PlayMode for flight/guidance aerodynamics).

---

## Phases & Milestones

### Phase 0 — Foundations (Pre-production)
*Focus: Core physics, MTOW data models, and guidance math.*
- [x] Set up Unity project, folder structure, source control, coding standards
- [x] Define ScriptableObject schema for part categories, MTOW limits, and fuel sliders
- [x] Prototype 3-DOF atmospheric flight physics (drag, lift, altitude-density drop) for one drone and missile
- [x] Prototype basic pursuit guidance and laser line-of-sight tracking checks
- [x] Prototype basic detection/RCS model
- [x] Implement JSON save/load framework

---

### Phase 1 — Vertical Slice (MVP)
*Focus: Complete end-to-end playable loop.*
- [x] **Workshop**: Basic parts (Tier 1 Cold War drone, Tier 1 wire/laser rocket), MTOW gauge, fuel slider, estimated range display.
- [x] **Combat**: Single arena, player controls one drone, simple CPU enemy, basic HUD (fuel, health, lock reticle).
- [x] **Meta**: Basic resource reward loop and tech tree unlock.

---

### Phase 2 — Content & Systems Expansion (Alpha)

#### 2A — Missile Part Breadth & Mass Balancing
**Goal:** Give missiles genuine part breadth and trade-offs before leaning on them for
guidance/AI depth in 2C/2D — those sub-milestones need real seeker/engine variety to be
worth building against.

- [x] **Prerequisite: migrate Workshop UI off `OnGUI` onto UI Toolkit** (per the
  Technology Stack section) before building real part-picker UI. `WorkshopController`
  now uses a `UIDocument` driven by `Assets/_Project/UI/Workshop/Workshop.uxml`/`.uss`
  (tech tree list, design preview, Enter Combat button all rebuilt as `VisualElement`s
  instead of per-frame `OnGUI` `Rect` calls); `Phase1WorkshopSceneBuilder` creates/wires
  the `PanelSettings` asset automatically. Verified in-editor: manual Play renders and
  updates the UI correctly, and `Phase1WorkshopSmokeTest` completes cleanly with no
  errors. (That smoke test's `EditorApplication.Exit(0)` is now gated on
  `Application.isBatchMode` so triggering it interactively from the menu no longer
  force-quits the whole Editor — only true headless/CI runs do that.) Still only a
  single hardcoded part per slot (no real multi-option picker yet) — that's the next
  2A/2B work, now unblocked.
- [x] Implement MTOW validation checks in missile assembly (dry mass + variable fuel
  slider vs. motor capacity). `MissileAirframeDefinition.maxTakeOffMassKg` (0 = no limit
  configured) is validated in `DesignStatsCalculator.Calculate` against total assembled
  mass, which now uses the fuel tank's continuous fill level
  (`MissileLoadout.fuelFillFraction`, the "Continuous Sliders" concept from the design
  doc) rather than always assuming a full tank. The Workshop UI has a live fuel-fill
  slider, shows mass-vs-MTOW, and disables Enter Combat when over the limit.
  `Airframe_Basic` was seeded with `maxTakeOffMassKg = 40` (its Tier 0 design is 30kg
  fully loaded) so this doesn't break the already-working Phase 1 loop. Verified
  headlessly via new `Phase2AValidation.ValidateTier0MissileMtow`: re-ran
  `Phase1DataSeeder` to pick up the field, confirmed `massKg=30`, `maxTakeOffMassKg=40`,
  `isWithinMtow=True` at full fuel, and that dropping `fuelFillFraction` to 0 correctly
  drops both `fuelMassKg` and total `massKg` — the slider genuinely affects the runtime
  calculation, not just the raw asset value.
- [x] Add payload variants (HE-Frag, Shaped Charge, Kinetic, Cluster, Grenades) with
  scaling mass penalties. `Phase2AMissileBreadthSeeder` (new Editor tool, run via
  `Vanquish/Phase 2A/Seed Missile Payload Variants`) seeds Shaped Charge (armor-piercing,
  focused/no-splash), Kinetic (no explosive, pure impact), Cluster (heaviest, wide-area
  submunitions), and Grenade (cheapest Tier 0 improvised) as real
  `MissilePayloadDefinition` assets alongside the existing HE-Frag. **Deliberately not
  wired into any TechNode or the Workshop preview yet** — Workshop still only supports a
  single hardcoded part per slot, so exposing these as "unlockable" before a real
  multi-option part-picker exists would let a player spend currency on a choice with no
  visible effect. That picker (and tech-tree wiring for all of 2A's part breadth, not
  just payloads) is unstarted follow-up work, not yet its own checklist line. Verified
  in-editor: `Vanquish/Phase 2A/Seed Missile Payload Variants` ran cleanly and logged
  the 4 new assets seeded under `Assets/_Project/Data/Missiles/`.
- [x] Implement engine types across the full spectrum (Solid Rocket, Liquid, Ramjet,
  Scramjet) as real `MissileEngineDefinition` assets. `Phase2AMissileBreadthSeeder`
  (`Vanquish/Phase 2A/Seed Missile Engine Variants`) seeds Liquid Rocket (Tier 1,
  throttleable/longer-burn but heavier), Ramjet (Tier 2, air-breathing/lighter/longer
  sustained burn), and Scramjet (Tier 4, heaviest/most expensive/highest top speed)
  alongside the existing Solid Rocket. Data-only simplification: ramjet/scramjet
  airspeed-gated ignition (they can't produce thrust below roughly Mach 0.5-1 in
  reality) isn't modeled — noted in code as a Phase 2C propulsion-model candidate, same
  deferral pattern as jamming consumption in 2A's technical notes. Same
  not-yet-wired-into-tech-tree caveat as the payload variants above — covered by the
  new picker/tech-tree checklist item. Verified headlessly (`Unity.exe -batchmode -quit
  -executeMethod ...SeedEngineVariants`): ran cleanly, logged success, all 3 assets
  confirmed on disk.
- [x] Implement seeker spectrum: Wire/SACLOS, Laser-guided, Optical/TV, SARH, ARH,
  Imaging IR, Multi-spectral. `SeekerType` enum extended with `Laser`, `ImagingInfrared`,
  `MultiSpectral` (appended, not inserted, to keep existing serialized ordinals stable —
  `Optical`/`SemiActiveRadar`/`ActiveRadar`/`WireOrDatalinkGuided` already existed but
  had no assets). `Phase2AMissileBreadthSeeder.SeedSeekerVariants` seeds all 7 as real
  `SeekerDefinition` assets with genuine trade-offs (Wire/SACLOS near-immune to
  jamming/countermeasures but short range; ARH longest range but most
  jam/countermeasure-susceptible; Multi-Spectral best-all-around but heaviest/priciest).
  Same not-yet-wired-into-tech-tree caveat as payloads/engines above. Verified
  headlessly: ran cleanly, logged success, all 7 assets confirmed on disk.
- [ ] Implement FAF decoy checks vs. guided missile SNR "fuzzing" jamming mechanics.
  **Rescoped**: this is runtime logic (an active decoy attempt breaking a live seeker
  lock, SNR degradation from ECM), not part-breadth authoring — it belongs with the
  rest of "wiring jamming into `DetectionSensor`" in 2C per this sub-milestone's own
  Technical notes below, not here. Leaving unchecked in 2A; will be addressed in 2C.
- [x] Add countermeasure dispensers, RCS-shaping packages, ECM/ECCM modules as seeded
  assets. `Phase2AMissileBreadthSeeder.SeedCountermeasureAndJammingVariants` seeds
  Flare/Chaff Dispenser and RCS-Shaping Package (`CountermeasureDefinition`) and ECM
  Jamming Pod and ECCM Suite (`JammingDefinition`) — data/assets only, runtime
  consumption is the 2C item directly above. Verified headlessly: ran cleanly, logged
  success, all 4 assets confirmed on disk.
- [x] Extend the seeder to create all of the above under `Assets/_Project/Data/Missiles/`.
  Done via the new `Phase2AMissileBreadthSeeder` (payloads, engines, seekers,
  countermeasures/jamming — 4 menu commands, one per part category) rather than
  extending `Phase1DataSeeder` directly, keeping the Phase 1 MVP seeder and Phase 2A
  breadth seeder independently re-runnable.
- [x] **Wire this breadth into the Workshop**: added TechNodes for every new part
  variant seeded above, and replaced `WorkshopController`'s single-hardcoded-part-per-slot
  fields (for Payload/Engine/Seeker/Countermeasure/Jamming) with a real multi-option
  picker. `Phase2AMissileBreadthSeeder.SeedTechTreeNodes` (new menu command, run after
  the four "Seed Missile ... Variants" commands) creates 18 `TN_2A_*` TechNode assets —
  one per unwired variant — each gated behind a sensible prerequisite (e.g.
  `Engine_Scramjet` requires `Engine_Ramjet` requires `Engine_LiquidRocket` requires the
  Tier-0 solid rocket node; `Seeker_MultiSpectral` requires both ARH and Imaging IR;
  Countermeasure/Jamming gate behind the base missile airframe node since those
  categories didn't have a Tier-0 node of their own). `WorkshopController` now exposes
  `missileEngineOptions`/`missileSeekerOptions`/`missilePayloadOptions`/
  `missileCountermeasureOptions`/`missileJammingOptions` arrays (Airframe/Fuel stay
  single-option — only one variant exists for each so far) and builds a new "Missile
  Loadout" panel in the Workshop UI: one row per slot, a button per currently-unlocked
  option (filtered live via the same `PlayerProgress.IsPartUnlocked` the tech tree uses),
  highlighting the current selection; Countermeasure/Jamming also get an explicit "None"
  button since those slots are optional. Required slots auto-default to the first
  unlocked option so a fresh save still gets a working missile; `DesignStatsCalculator`
  needed no changes since it already reads `MissileLoadout`'s fields directly.
  `Phase1WorkshopSceneBuilder` wires the new tech nodes and option arrays.
  Drone part-picker wiring is out of scope here — 2B hasn't seeded multi-option drone
  breadth yet. Verified headlessly: `Phase2AValidation.ValidateMissileBreadthTechWiring`
  confirms all 18 TechNodes exist, each unlocks exactly one part and has a non-null
  prerequisite, and the Scramjet→Ramjet chain resolves correctly (18/18 PASS); re-ran
  `Phase1WorkshopSceneBuilder.BuildScene` with no "could not load asset" errors; re-ran
  `Phase1WorkshopSmokeTest` (Play mode, new picker UI actually building/querying
  elements) with no exceptions/NullReferenceExceptions.

**Technical notes:** `JammingDefinition`, `CountermeasureDefinition`, and
`SeekerDefinition.jamResistance` already exist and roll up into
`MissileRuntimeStats.jamResistance` via `DesignStatsCalculator`, but nothing consumes
that value at runtime yet — this sub-milestone is scoped to part breadth/data only;
wiring jamming into `DetectionSensor` belongs in 2C.

**Exit criteria:** Every `PartCategory.Missile*` enum value has at least 2–3 real
assets; a Tier 1 wire-guided rocket and a Tier 3+ ARH missile both exist and both
fly/guide according to their own seeker model; a player can actually pick between those
variants in the Workshop (not just have them exist as unused assets).

---

#### 2B — Drone Part Breadth & Fuel Spectrum
**Goal:** Give drones the same part breadth as missiles, plus the propulsion/airframe
variety the AI and combat systems in 2C–2E need to be interesting.

- [x] Implement MTOW validation checks in drone assembly (airframe limit vs. engine
  mass, payload, and fuel/battery slider). `DroneAirframeDefinition.maxTakeOffMassKg`
  (mirrors `MissileAirframeDefinition`'s 2A field) is validated in
  `DesignStatsCalculator.Calculate(DroneLoadout)` against total assembled mass, which
  now uses `DroneLoadout.fuelFillFraction` (new field, mirrors
  `MissileLoadout.fuelFillFraction`) rather than always assuming a full battery/tank.
  `Airframe_SmallQuad` seeded with `maxTakeOffMassKg = 180` (a fully-loaded Tier-0
  strike drone — drone parts + 4x Tier-0 missiles — is 141kg), so this doesn't break
  the already-working Phase 1 loop. Verified headlessly via new `Phase2BValidation.
  ValidateTier0DroneMtow`: confirmed `massKg=21` (bare drone)/`141` (armed with 4x
  Tier-0 missiles) both `isWithinMtow=True` against the 180kg limit, and that dropping
  `fuelFillFraction` to 0 correctly drops both `fuelMassKg` and total `massKg`.
- [x] Implement propulsion types: Electric (battery slider), ICE (Petrol/Diesel),
  Subsonic Jet (Jet Fuel), Supersonic Jet. `PropulsionType.InternalCombustion` and
  `FuelType.Petrol`/`Diesel` added (appended, not inserted, preserving existing
  serialized ordinals — same convention as 2A's `SeekerType` additions).
  `Phase2BDroneBreadthSeeder.SeedPropulsionEngineFuelVariants` seeds
  `Propulsion_ICE_Basic`/`Engine_ICE_Basic` (paired with new `Fuel_Petrol_Basic` and
  `Fuel_Diesel_Basic` — both fuel types the design doc calls out exist as real assets),
  `Propulsion_Jet_Subsonic`/`Engine_Jet_Subsonic`, and
  `Propulsion_Jet_Supersonic`/`Engine_Jet_Supersonic` (paired with new
  `Fuel_JetFuel_Basic`). Verified headlessly: all 9 new assets confirmed on disk with
  correct `requiresForwardFlight` flags (see below).
- [x] Add airframe classes: Small Quad, Hexacopter, Fixed-Wing, Flying-Wing Stealth,
  CCA-scale. `DroneAirframeClass.Hexacopter` added (appended). Fixed-wing/jet-style
  airframes' "needs an `orientToVelocity = true` propulsion pairing" requirement is
  satisfied via a new `PropulsionDefinition.requiresForwardFlight` bool (true for the
  Jet Subsonic/Supersonic propulsion above, false for Electric/ICE) — `VehicleFactory.
  SpawnDrone` now reads `DroneRuntimeStats.requiresForwardFlight` (threaded through
  from the loadout's propulsion choice by `DesignStatsCalculator`) to set
  `FlightBody.isThrusting`/`orientToVelocity` per-design instead of hardcoding
  quadcopter behavior for every drone. `Phase2BDroneBreadthSeeder.
  SeedAirframeVariants` seeds `Airframe_SmallHexa`/`FixedWing`/`FlyingWingStealth`/
  `CcaScale`. Verified headlessly via `Phase2BValidation.ValidateDroneBreadthAssets`
  and a full 60-second `Phase1BatchRunner` headless combat regression run confirming
  the existing electric-quadcopter Tier-0 drones still behave identically (still
  omnidirectional, still no forward thrust) after the flag became data-driven.
- [x] **Quadcopter → hexacopter upgrade path**: added `rotorCount` to
  `DroneAirframeDefinition` (4 on the existing `SmallQuad` seeded via
  `Phase1DataSeeder`, 6 on the new `Airframe_SmallHexa` from
  `Phase2BDroneBreadthSeeder`, 0 on the three non-multirotor airframe classes above).
  `Airframe_SmallHexa` raises `hardpointCount` (4 vs. `SmallQuad`'s 2) at the cost of
  higher `structuralMassKg` (10 vs. 6) and more individual rotor mass paid (6 rotors vs.
  4) — a genuine mass-vs-capacity trade-off, not a strict upgrade.
  `VehicleFactory.SpawnDrone` now passes `loadout.airframe.rotorCount` into
  `DroneVisualBuilder.BuildMultirotorVisual` (which already generically supported
  arbitrary counts, clamped to a minimum of 3) instead of hardcoding 4; airframes with
  `rotorCount == 0` (Fixed-Wing/Flying-Wing Stealth/CCA-Scale) get a new
  `DroneVisualBuilder.BuildFixedWingVisual` fuselage+wings silhouette instead, so a jet
  drone doesn't spawn looking like a quadcopter. Verified via the same headless combat
  regression run above (SmallQuad's rotorCount=4 visual unchanged) plus
  `Phase2BValidation.ValidateDroneBreadthAssets` confirming `Airframe_SmallHexa.
  rotorCount == 6` and the three fixed-wing-style airframes' `rotorCount == 0`.
- [x] **Rotor material & size breadth**: added `RotorMaterial` (`Plastic`,
  `CarbonFiber`, `Metal`) and `RotorSize` (`Small`, `Medium`, `Large`) enums plus
  matching fields to `WingOrPropellerDefinition`, alongside a new `structuralIntegrity`
  (0-1) durability stat — informational for now (no runtime consumer yet), a hook for a
  future rotor-damage mechanic per this item's own note. `Phase2BDroneBreadthSeeder.
  SeedRotorVariants` seeds all 9 Material x Size combinations
  (`Propeller_Plastic_Small` through `Propeller_Metal_Large`) as real assets with
  genuine trade-offs: Plastic cheapest/low durability, Carbon Fiber lightest but least
  durable, Metal heaviest but most durable (0.85 `structuralIntegrity` vs. Plastic's
  0.5 and Carbon Fiber's 0.4); size scales `liftCoefficient`/mass/drag up independent
  of material (`Propeller_Basic` from Phase 1 remains the Plastic/Medium equivalent).
  Verified headlessly: all 9 assets confirmed on disk with correct material/size fields.
- [x] Wing/propeller types: added `Wing_FixedWing`/`Wing_DeltaWing`/
  `Wing_VariableSweepWing` assets (enum already existed) via
  `Phase2BDroneBreadthSeeder.SeedWingTypeVariants`, with genuine speed-vs-maneuver
  trade-offs (Fixed Wing: best low-speed lift, least maneuverable, cheapest; Delta
  Wing: less low-speed lift but far more maneuverable and lower drag; Variable-Sweep:
  best maneuverability and good cruise efficiency at the highest mass/cost — Phase 1
  simplification modeled as one averaged stat block rather than a real in-flight sweep
  state machine). Verified headlessly: all 3 assets confirmed on disk.
- [x] Hull materials: added `Hull_AluminumAlloy`/`Hull_CarbonFiber`/
  `Hull_RadarAbsorbentMaterial`/`Hull_TitaniumAlloy` (enum already existed) via
  `Phase2BDroneBreadthSeeder.SeedHullMaterialVariants`. RAM cuts
  `radarCrossSectionMultiplier` to 0.35 at a mass/cost premium with no armor upside
  (the dedicated stealth choice); Titanium raises `maxTemperatureCelsius` to 650°C (vs.
  Composite Plastic's 150°C) for the hypersonic/CCA tier, at the highest armor rating
  and mass/cost of the four. Verified headlessly: all 4 assets confirmed on disk.
- [x] Engines/fuel: jet-appropriate `DroneEngineDefinition`/`FuelDefinition` (JetFuel
  type) pairing — see the propulsion types item above; `Engine_Jet_Subsonic`/
  `Engine_Jet_Supersonic` paired with `Fuel_JetFuel_Basic`.
- [x] Weapon bays: added `WeaponBay_Large` (external, higher capacity/munition count
  than the Tier-0 `WeaponBay_Small`) and `WeaponBay_InternalMedium` (`isInternal =
  true`, pairs with the Flying-Wing Stealth airframe/RAM hull for the stealth stack —
  the RCS-exposure consequence of `isInternal` itself is still a 2C/3 runtime-wiring
  item, per `WeaponBayDefinition`'s existing doc comment, not part of this seeding
  work). Verified headlessly: both assets confirmed on disk, `WeaponBay_InternalMedium.
  isInternal == true`.
- [x] **Altitude control modes**: implemented `AbsoluteMSL`/`RelativeAGL` via a new
  `ICommandReceiver` interface and `AltitudeController : MonoBehaviour, ICommandReceiver`
  (`Assets/_Project/Scripts/Simulation/Flight/`), per Deep Dive §5. `RelativeAGL` holds
  `Y_target = Y_ground + desiredAlt` using a new `GroundSampler` (downward raycast,
  falling back to the flat y=0 placeholder ground every current scene builder already
  uses when nothing is hit — so this picks up real terrain unmodified once Phase 2E
  adds heightmap colliders, per this item's own "build against a flat placeholder
  ground first and revisit" deferral); `AbsoluteMSL` holds a fixed world Y ignoring
  terrain. Climb-rate limiting clamps commanded vertical speed to
  `maxClimbRateMetersPerSecond` before converting the remaining gap to an acceleration,
  applied via the same `FlightBody.ApplySteering` path player input and AI guidance
  already use. Also added `TerrainCollisionCheck`/`TerrainCollisionChecker` (cliff
  detection + Deep Dive §5's required-climb-rate formula) for deciding when
  `AbsoluteMSL`'s terrain-blindness is about to be a problem. Opt-in component (not
  attached to every drone yet — no AI archetype consumes it until Phase 2D); the core
  altitude/target-resolution math is factored into pure static functions specifically
  so it's headlessly testable without a live scene. Verified headlessly via new
  `Phase2BValidation.ValidateAltitudeAndLandingMath`: AbsoluteMSL/RelativeAGL target
  resolution, climb-rate clamping, settled-at-target zero-accel, and vertical-cliff
  detection all confirmed correct.
- [x] **Landing & touchdown validation**: implemented the sink-rate/ground-speed/slope
  safe-landing check (`LandingValidator.CanLandSafely`) and the Deep Dive §6 Surface
  Friction Matrix (`SurfaceFrictionMatrix`/`LandingSurfaceType`: Paved Runway/Helipad,
  Flat Grass/Soil, Uneven/Rock, Water/Marsh) as pure calculation utilities — no
  MonoBehaviour/landing-gear physics model, matching this item's own scope note.
  Water/Marsh is correctly never landable regardless of speed/slope (Deep Dive §6's
  "Destruction/Vehicle Sink" risk rating). Verified headlessly via
  `Phase2BValidation.ValidateAltitudeAndLandingMath`: safe landing on flat grass at low
  speed, rejected on Water/Marsh unconditionally, rejected for excessive sink rate on
  an otherwise-forgiving paved runway, and rejected for a 20° slope exceeding Uneven/
  Rock's 5° max — all confirmed PASS.
- [x] Extend the seeder to create all of the above under `Assets/_Project/Data/Drones/`.
  Done via the new `Phase2BDroneBreadthSeeder` (airframes, rotors, wing types, hull
  materials, propulsion/engine/fuel, weapon bays — 6 menu commands plus a "Seed All"
  convenience command), mirroring 2A's `Phase2AMissileBreadthSeeder` pattern exactly
  (idempotent `CreateOrReplace<T>`, one seeding method per part category).

**Technical notes:** The propulsion/orientation flag change touched `VehicleFactory`
(reads `DroneRuntimeStats.requiresForwardFlight` instead of the previous hardcoded
`orientToVelocity = false`) as one isolated change, verified via a headless regression
check on the Phase 1 combat scene (`Phase1BatchRunner`, full 60-second battle) —
electric quadcopters kept behaving exactly as before. Altitude-mode and landing work
was built against the flat placeholder ground every scene builder already uses (Phase
2E's real terrain doesn't exist yet), per this item's own sanctioned deferral —
`GroundSampler`'s raycast-based approach means it needs no changes once real heightmap
terrain/slopes exist; the "hold `RelativeAGL` over *sloped* terrain" half of the exit
criteria below should be re-verified against an actual sloped arena once Phase 2E lands
a heightmap terrain, since no sloped ground exists in the project yet to test against.
Unlike 2A, this sub-milestone's own PLAN.md checklist did not require wiring the new
part breadth into a Workshop multi-option picker — but since 2A's picker
infrastructure (`WorkshopController.BuildPartSlotRow<T>`/`ResolveSelection<T>`) already
generalized to any `PartDefinition`, this was done as a cheap follow-up rather than left
sitting as inert data: `Phase2BDroneBreadthSeeder.SeedTechTreeNodes` wires all 31
previously-unwired 2B drone parts behind their own `TN_2B_*` TechNodes (mirroring 2A's
`SeedTechTreeNodes`, with the same category-internal progression-chaining approach —
e.g. `Engine_Jet_Supersonic` requires `Engine_Jet_Subsonic`; RAM hull requires Carbon
Fiber requires Aluminum Alloy), and `WorkshopController` gained a "Drone Loadout"
picker section (Propulsion/Airframe/Wing-or-Rotor/Hull Material/Engine/Fuel/Weapon Bay)
right below the existing "Missile Loadout" section in the same scroll, using the exact
same `BuildPartSlotRow`/`ResolveSelection` machinery 2A already built — no picker-UI
code needed to change, only new option arrays on `WorkshopController` and their
`Phase1WorkshopSceneBuilder` wiring. Sensor suites (basic/scout) stayed single-option
fields since they're fixed by drone role, not a player choice. Verified headlessly via
new `Phase2BValidation.ValidateDroneBreadthTechWiring` (31/31 nodes correctly wired,
progression chain check passed), a `Phase1WorkshopSceneBuilder.BuildScene` rebuild with
no missing-asset errors, a `Phase1WorkshopSmokeTest` Play-mode run with no exceptions,
and a full 60-second `Phase1BatchRunner` combat regression re-run confirming the rest
of the loop still behaves correctly.

**Exit criteria:** Every `PartCategory.Drone*` enum value has at least 2–3 real assets
(✅ — Propulsion: 4, Airframe: 5, WingOrPropeller: 13, HullMaterial: 5, Engine: 4,
Fuel: 4, WeaponBay: 3, SensorSuite: 2 from Phase 1, unchanged); a fixed-wing supersonic
jet drone and an electric quadcopter both exist and both fly according to their own
propulsion model (✅ — verified via headless regression **and** live playtesting, which
caught real gaps headless verification alone couldn't have: `requiresForwardFlight`
drove `FlightBody`'s per-design *values* correctly (verified headlessly), but
(1) `PlayerDroneController` unconditionally forced `isThrusting = false` regardless of
airframe, so the player's own jet/fixed-wing drone still flew like a quadcopter even
though AI-controlled/default spawns were already correct, and (2) `FlightBody` itself
had no real aerodynamic model yet — exactly the gap its own long-standing doc comment
flagged ("sufficient to validate... before investing in a full aerodynamic model in
Phase 2") — so even with the propulsion flag read correctly, a fixed-wing drone had no
lift to stay airborne and no distinct player control feel.
Both are now fixed. `FlightBody` gained a real (if simplified) aerodynamic model,
opt-in via `useAerodynamicLift`: lift force along `transform.up`, quadratic in speed
(`liftCoefficient * speed²`, from the design's wing part — `WingOrPropellerDefinition.
liftCoefficient` was already a field but was never actually consumed by physics before
this), clamped to `maxGForce`; gravity is force-enabled alongside it. Deliberately no
separate tunable "stall speed" — the quadratic falloff at low speed already produces a
natural nose-drop/stall on its own. A second `FlightBody.Configure` overload engages
this (missile/multirotor spawn code keeps using the original 4-argument overload
unchanged). `PlayerDroneController` now has two genuinely distinct control schemes
selected per-design at spawn time (from `FlightBody.isThrusting`): multirotor keeps the
original omnidirectional `ApplySteering`-based hover/strafe/auto-brake; fixed-wing/jet
gets a dedicated roll-to-turn stick-and-throttle model instead — **A/D roll (bank)**,
**W/S pitch (climb/dive)**, **Space/Shift throttle** — deliberately with *no direct yaw
input at all*, turning instead emerging from banking + pulling into the turn (same as a
real aircraft/most arcade flight games), which the new lift model makes physically
meaningful since banking tilts the lift vector sideways and curves the flight path.
Rotation is driven directly via `Rigidbody.MoveRotation` rather than through
`FlightBody.ApplySteering`, and a new `FlightBody.alignVelocityToForward` flag (damps
velocity components perpendicular to `transform.forward`) is enabled for the player
specifically — the opposite relationship from `orientToVelocity` (nose chases
velocity, still used unchanged by missile/AI guidance) — so the flight path follows
wherever the player points the nose rather than fighting a velocity-chasing autopilot,
which a first pass (reusing `orientToVelocity` + a direct-yaw input) found felt far too
subtle/momentum-dominated and had no throttle at all; a drone can hold `RelativeAGL` altitude over sloped
terrain (⚠️ system implemented and headlessly verified against the target-resolution
math, but no sloped terrain exists in the project yet to test against live — see
technical note above) and land safely on at least one surface type (✅ — verified via
`LandingValidator` against the Flat Grass/Soil profile).

---

#### 2C — Guidance & Sensor Depth
**Goal:** Combat mechanics gain real depth once there's seeker/jamming variety to test against (do this after 2A).

- [ ] **Proportional navigation** guidance law: new `ProportionalNavigation :
  IGuidanceLaw` in `Simulation/Guidance/`, implementing true PN (steering ∝ line-of-sight
  rate × closing velocity × navigation constant), not just pursuit. Validate headlessly
  the same way `PursuitGuidance` was validated in Phase 0 (missile vs. weaving target,
  confirm it out-intercepts pure pursuit at the same tuning).
  `WeaponController`/`GuidanceController` need a way to pick the guidance law based on
  the missile's `SeekerDefinition.seekerType` (e.g. wire/datalink-guided early tiers
  stay on pursuit, radar-seeker tiers get PN).
- [ ] **Datalink mid-course update**: for `WireOrDatalinkGuided`/`ActiveRadar` missiles
  with a `DatalinkNetworkDefinition.supportsMidCourseUpdates` platform, the missile
  should fly toward a periodically-updated target position/velocity relayed from the
  launching platform (using whatever contact TeamAwareness has) rather than needing its
  own seeker lock for the whole flight, only activating its own seeker for terminal
  homing within `SeekerDefinition.detectionRangeMeters`. New `DatalinkMidCourseGuidance`
  or a wrapper that switches from "fly to relayed position" to the missile's own
  `IGuidanceLaw` once in seeker range.
- [ ] **Probabilistic detection**: replace `DetectionSensor`'s binary
  distance-vs-effective-range check with a probability curve (e.g. detection chance
  falls off with distance/RCS rather than a hard cutoff) and add intermittent contact
  loss/reacquisition (using `SeekerDefinition.reacquisitionTimeSeconds`, currently
  unused). This is also where `MissileAirframeDefinition.baseRadarCrossSection` and
  countermeasure RCS multipliers actually start to matter tactically instead of just
  changing a hard range number.
- [ ] **Jamming/counter-jamming**: `JammingDefinition.jammingStrength`/`jammingRangeMeters`
  should degrade nearby enemy `DetectionSensor` lock probability/quality within range;
  `counterJammingStrength` on the target's own systems should offset it. Needs a way for
  `DetectionSensor` to query nearby active jammers (similar brute-force scan pattern as
  today, replace with spatial query in the Phase 2/3 performance pass, not now).
- [ ] Countermeasure decoys (`decoyCharges`/`decoySuccessChance`) should give a
  currently-locked missile a chance to break lock/retarget a decoy instead — needs a
  "counter-fire" player/AI action and a check in the guidance/seeker update loop.

**Technical notes:** Keep `IGuidanceLaw` as the extension point — don't special-case
missile behavior outside it. Add a small headless regression test scene (reuse the
`Phase1BatchRunner` pattern) specifically for guidance law comparison: same start
conditions, swap the guidance law, compare hit rate/time-to-intercept.

**Exit criteria:** A player can tell the difference in a fight between a pursuit-guided
missile, a PN-guided missile, and a datalink+PN missile; jamming/countermeasures
visibly affect whether a shot connects.

---

#### 2D — AI Depth
**Goal:** CPU opponents stop being a single patrol→engage FSM and start having distinct roles.

- [ ] **Interceptor** archetype: aggressive, prioritizes closing distance and engaging
  the player's strike drone specifically (today's `EnemyDroneAI` is close to this
  already — formalize it as one archetype rather than "the" AI).
- [ ] **Scout-hunter** archetype: prioritizes targeting known/likely scout drones
  first (since killing the scout blinds the player's TeamAwareness) — needs
  `TeamAwareness` to expose "is this contact a scout" (e.g. via
  `SensorSuiteDefinition.sharesContactsWithTeam` on the spawned unit) so the AI can
  discriminate targets, not just "nearest."
- [ ] **SAM site** archetype: static (or minimally-mobile) `BaseDefenseDefinition`-driven
  unit with a fixed position, long engagement range, high rate of fire — needs its own
  spawner path (not `VehicleFactory.SpawnDrone`, since it's not a drone) and a simple
  "engage anything in range" controller rather than patrol/pursuit logic.
- [ ] Replace/augment the current hand-rolled FSM (`EnemyAIState` enum + `if`/`else` in
  `EnemyDroneAI`) with actual behavior trees once there are 3+ archetypes sharing
  building blocks (detect, evade, engage, retreat-when-low-health) — evaluate Unity's
  Behavior package (per the original tech stack notes) vs. continuing hand-rolled FSMs;
  don't adopt a framework speculatively, decide once the archetype count makes shared
  nodes clearly worth it.
- [ ] AI should react to being jammed/detected-by-countermeasure (from 2C) — e.g. break
  off or use its own countermeasures — otherwise 2C's systems are invisible to the AI
  side of the fight.

**Technical notes:** Keep archetypes as separate MonoBehaviours (like today's
`EnemyDroneAI`/`ScoutPatrol` split) rather than one mega-controller with branching
modes — matches the existing pattern and keeps each headlessly testable in isolation.

**Exit criteria:** A single battle can contain an interceptor, a scout-hunter, and a
SAM site simultaneously, each behaving visibly differently.

---

#### 2E — Maps & Scenarios
**Goal:** Combat isn't just "one flat arena, kill everything" anymore.

- [ ] **Decide the terrain approach before building arenas**: `Phase1CombatSceneBuilder`'s
  scripted-primitive pattern works for flat arenas but can't reasonably generate actual
  mountains/valleys/plateaus from code. Author 2-3 heightmap terrains with Unity's
  Terrain tools (or a cheap terrain asset pack) and keep using the scripted-builder
  pattern only for unit/camera/objective placement on top of that terrain.
- [ ] At least 2–3 additional arena layouts (terrain variation, cover, different
  engagement distances) — reuse `Phase1CombatSceneBuilder`'s scripted-scene-construction
  pattern rather than hand-placing in the Editor, so maps stay reproducible/diffable.
- [ ] At least one non-skirmish objective type (e.g. "destroy the enemy launch
  platform/base installation" rather than "destroy all enemy units") — needs
  `CombatManager`'s win condition to become pluggable (an `IObjective`/strategy
  interface) rather than the current hardcoded "all enemy `Health` destroyed."
- [ ] Scenario selection needs a place to live — likely a small scenario-picker screen
  before entering Combat, or a dropdown in the Workshop's "Enter Combat" flow.

**Technical notes:** This is a good place to introduce a lightweight `ScenarioDefinition`
ScriptableObject (scene reference + objective type + starting unit placements) so
`Phase1CombatSceneBuilder`-style tools and `CombatManager` both read from one data
source instead of each scene hardcoding its own setup.

**Exit criteria:** Player can choose between at least 2 scenarios with different maps
and at least one has a non-"kill everything" win condition.

---

#### 2F — Base-Building / Support Architecture
**Goal:** Launch platforms, radar installations, datalink, and point defense become placeable, not implicit.

- [ ] A pre-combat "placement" phase/mode where the player positions
  `LaunchPlatformDefinition`/`RadarInstallationDefinition`/`BaseDefenseDefinition`
  instances within a designated zone before the battle starts (or before an
  objective-based scenario's timer starts).
- [ ] Placed installations need their own spawner (parallel to `VehicleFactory`, since
  they're static/semi-static, not flying units) and their own `Health`/`DetectionSensor`
  wiring so they participate in win/lose and detection like any other unit.
- [ ] `DatalinkNetworkDefinition` needs to actually gate the mid-course guidance/seeker
  handoff features built in 2C — without a placed datalink installation (or one on the
  launch platform itself), those features shouldn't be available.
- [ ] Basic placement UI — given the "ugly art is fine" precedent from Phase 1, an
  OnGUI or simple drag-in-3D-space placement tool is acceptable for Phase 2; save the
  polished UI for Phase 3.
- [ ] **Placement footprint validation** (Deep Dive §7): before confirming a placed
  structure, check slope angle vs. `maxPlacementSlope`, elevation above sea level, and
  whether the target grid cells are already occupied (`PlacementValidation.CanBuild`).
  Ground structures should also suppress nearby procedural vegetation within a clearance
  radius and evaluate radar/sensor line-of-sight from an elevated phase-center offset
  (`h_emitter`) rather than ground level.
- [ ] **World grid & spatial occupancy** (Deep Dive §8): back placement with a discrete
  world-grid (`WorldGridCell`: coords, `CellState` [Clear/OccupiedStatic/ReservedMobile/
  BlockedTerrain], elevation, slope, occupant ID) so placed structures snap to grid
  cells, register occupancy, and update `NavMeshObstacle` bounds for ground-vehicle
  pathfinding. This is the data structure the placement UI and footprint validation
  above should both read/write, rather than each placement check doing its own ad-hoc
  overlap test.

**Technical notes:** This is the most UI-heavy sub-milestone in Phase 2 — timebox it. If
placement UI turns into a rabbit hole, ship a simplified version (e.g. pick from a
handful of preset placement slots rather than freeform placement) and revisit freeform
placement in Phase 3's UI/UX pass. Build the world grid first (it's pure data/math, no
UI dependency) so footprint validation and the placement UI both have something to
target from day one instead of being built against a placeholder.

**Exit criteria:** A player can place at least a launch platform and a radar
installation before a battle, both snap to valid world-grid cells (rejecting
too-steep/occupied placements), and both are destructible, detectable targets during it.

---

#### 2G — Workshop Test Range
**Goal:** Let the player validate a design's real flight/combat behavior before spending resources committing to an actual battle.

- [ ] A "Test Range" mode reachable from the Workshop: spawns the player's current
  design (via the same `VehicleFactory` combat uses) against one or more stationary or
  simple-moving dummy targets, using the exact same simulation as real combat — this is
  explicitly the payoff of the "one data-driven part/stat model" principle from the
  plan's Concept Summary.
  - [ ] No win/lose consequences, no currency cost/reward — purely observational
  (distance closed, hit/miss, time-to-kill against a dummy).
- [ ] Reuse `Phase1CombatSceneBuilder`'s patterns for constructing the test range scene;
  reuse `Phase0TestHarness`-style telemetry logging so test-range results are inspectable
  the same way Phase 0's validation was (this doubles as a fast manual/headless sanity
  check any time new parts are added in 2A/2B).
- [ ] `WorkshopController` needs a button/flow to enter test range with the currently
  previewed design, and a way to return to the Workshop afterward (mirroring
  `CombatManager`'s return-to-Workshop flow, but without the currency award).

**Technical notes:** This is the cheapest sub-milestone to build once 2A/2B exist,
since it's almost entirely reuse of existing spawner/scene-building/telemetry code with
new scene content and a UI entry point — a good candidate to build early or interleaved
with 2A/2B rather than strictly last.

**Exit criteria:** Player can fire a design at a dummy target from the Workshop without
entering a real battle, and see basic hit/miss/timing feedback.

---

**Phase 2 overall exit criteria:** All part categories from the design doc exist in
some form with genuine trade-offs; a player can progress from grenade-drone tier
through to at least early supersonic/guided-missile tier; combat has visible depth
beyond "fly at the dot and shoot"; at least one scenario has a non-skirmish objective.

---

#### Pre-Phase-3 deliverable — Sandbox Campaign Design Deep-Dive
**Goal:** Unlike 2A-2G, the sector-based campaign map below currently has zero data
model — no `SectorDefinition`, no territory/adjacency graph, no resource-flow or
persistence schema. It's realistically its own subsystem (closer to a lightweight 4X
meta-layer than a UI polish pass) and is the single largest unscoped item in this plan.

- [ ] Write a design deep-dive for the sector map before Phase 3 implementation starts,
  covering: sector/territory graph representation, resource acquisition & flow between
  sectors, how a won/lost battle changes sector ownership, what persists between
  sessions (save schema), and how placed base installations (2F) relate to sector
  ownership.
- [ ] Only start Phase 3's overworld-map implementation once that deep-dive exists —
  treat it the same way the Subsystem Design Deep Dive unblocked Phase 2's 2A-2G.

---

### Phase 3 — Balancing, Polish & Content Completion (Beta)
**Goal:** Make the full spectrum (up to hypersonic/stealth CCA tier) playable, balanced, and polished.

- [ ] Top-tier content: stealth CCA-style drones, hypersonic air-to-air missiles
- [ ] AI scaling — CPU tech/behavior escalates alongside player progression
- [ ] Full UI/UX pass: tech tree visualization, workshop part comparison tools, combat HUD polish
  - [ ] **Missile/drone designer screen redesign** (reference mockup provided during
    Phase 2 development — see design notes below): replace the current
    Phase 1/2A/2B functional-but-plain `WorkshopController` UI (currency bar,
    linear tech-tree list, per-slot option buttons, plain-text stat readout) with
    a sleek, modern, professional layout once Phase 2's full part breadth exists
    to design against. Target layout:
    - **Editable design name field** at the top of the screen (the mockup shows a
      title like "3M22 Zircon Quasi-Ballistic Missile" with a subtitle — the
      player should be able to name/rename their own design here, persisted with
      the saved design).
    - **Live 3D design preview, front and center**, replacing the plain
      text-stats-only preview: the actual assembled model (reusing
      `VehicleFactory`'s spawn pipeline / the "Modular 3D Mesh Swapping" system
      from this doc's Concept Summary above — nose cone, seeker, engine,
      wings/rotors, materials all reflecting the current part selection) shown
      free-floating with mouse-drag rotate and scroll-wheel zoom, not just a
      static icon.
    - **Overlay stat card** anchored near the model (mockup shows a
      semi-transparent "Missile Specifications" box: Length, Diameter, Range,
      Flight ceiling, Payload) — a compact, glanceable summary distinct from the
      fuller stat breakdown, styled to sit on top of the 3D viewport rather than
      competing with it for a whole side panel.
    - **Per-slot part pickers as labeled dropdowns/selectors** (mockup shows
      "Engine: <Please Select>", "Seeker: <Please Select>" style rows) rather
      than 2A's current always-expanded row-of-buttons-per-slot — more compact
      and scales better as part counts per slot grow through Phase 2's breadth
      work; should still show per-part mass/cost at a glance (e.g. in the
      dropdown's option list) so "components and weight" stay visible without a
      separate stats panel, per the reference.
    - Overall visual language: dark professional/technical theme (dark
      panels, clean sans-serif labels, subtle borders — closer to the reference
      mockup's dark card layout than Phase 1's flat colored-rectangle buttons).
    - This item intentionally supersedes/replaces (not stacks on top of) the
      row-of-buttons part-picker UI built in 2A (`WorkshopController.BuildPartSlotRow`)
      and the plain-text `RefreshDesignPreview` stat dump — those were explicitly
      built as functional Phase 2 scaffolding ("ugly art is fine" precedent, Phase
      2F's technical notes) to unblock part-breadth work, not as the final UI.

- [ ] Art/audio pass: real models for parts (or modular part meshes), VFX for engines/explosions/countermeasures, SFX, music
- [ ] Balance pass across all tiers (part stats, tech costs, mission difficulty curve)
- [ ] Campaign/mission structure (progression of scenarios, not just skirmish)
- [ ] Tutorial/onboarding flow
- [ ] Performance pass (many simultaneous projectiles/drones, LOD, object pooling)
- [ ] Bug bash + QA pass

**Exit criteria:** Feature-complete against the original design (all part categories, full
tech tree, campaign structure, polished UI/art/audio), stable performance, balanced difficulty.

---

### Version 1.0 — Release
**Goal:** Ship a complete, polished, bug-free experience matching the beta feature set.

- [ ] Final balance pass based on beta feedback
- [ ] Final QA/certification pass
- [ ] Store page / release packaging (Steam, itch.io, etc. — TBD)
- [ ] Launch

**v1.0 scope = everything in Phase 3**, refined: full part spectrum, full tech tree,
campaign + skirmish modes, scout/recon mechanics fully integrated, polished art/audio/UX.

---

### Post-1.0 — Live Improvements

Prioritize based on player feedback, but likely candidates:

- [ ] Multiplayer (PvP skirmish, co-op vs. CPU)
- [ ] Map/scenario editor for user-generated content
- [ ] Deeper AI (ML-Agents-driven adaptive CPU opponents)
- [ ] Additional part tiers / exotic tech (directed-energy weapons, hypersonic glide vehicles, swarm drone coordination)
- [ ] Modding support (expose ScriptableObject part definitions for community content)
- [ ] Naval/ground unit expansion beyond air-centric combat
- [ ] Replay/spectator system for analyzing engagements
- [ ] Leaderboards / ranked skirmish modes

---

## Suggested Milestone Timeline (relative, not calendar-locked)

| Milestone | Depends on | Rough relative effort |
|---|---|---|
| Phase 0 — Foundations | — | Small |
| Phase 1 — MVP | Phase 0 | Medium |
| Phase 2 — Alpha (full breadth) | Phase 1 | Large |
| Phase 3 — Beta (full spectrum + polish) | Phase 2 | Large |
| v1.0 — Release | Phase 3 | Small (stabilization) |
| Post-1.0 | v1.0 | Ongoing |

Treat calendar estimates as unreliable until Phase 0 is complete — the flight physics and
guidance prototypes will reveal how much custom simulation work is really required.

---

## Version 2.0 — C4ISR & Tactical Theater Expansion

### Core Scope
Expand the simulation from single-craft tactical engagements into full-spectrum air defence management, airborne command (AWACS), multi-asset strike coordination, and dynamic theater control.

### Features & System Milestones

#### 2.0A — High-Altitude Tactical Command Interface
- [ ] Implement seamless transition between 3rd-person direct craft piloting and the tactical theater map layer.
- [ ] Add dynamic multi-unit command issuance (strike routes, loiter orbits, target prioritization, radar-silent holding areas).
- [ ] Build formations and automated wingman/swarm execution logic.

#### 2.0B — Integrated Air Defence Systems (IADS) & Base Engineering
- [ ] Modular ground defence editor: Build and custom-configure short, medium, and long-range SAM batteries, radar stations, and point-defence CIWS.
- [ ] Implement networked radar nodes: Command posts aggregate sensor feeds to allow passive/optical tracking and delayed radar illumination to minimize SEAD vulnerability.
- [ ] Base logistics & rearm cycles for static launchers and mobile TELs (Transporter Erector Launchers).

#### 2.0C — Airborne Early Warning & Cooperative Engagement (C4ISR)
- [ ] Implement AWACS and high-altitude endurance ISR drone platforms to clear fog-of-war and maintain long-range tracking tracks.
- [ ] Implement Cooperative Engagement Capability (CEC): Fire weapons from platform A using real-time targeting/datalink telemetry provided by platform B or ground radar networks.
- [ ] Add electronic warfare (EW) support aircraft to project stand-off jamming corridors for incoming strike packages.
- [ ] Build the CEC/AWACS datalink layer on the `TargetTrack`/`IDatalinkNode` schema
  from the Subsystem Design Deep Dive §3 (`TrackID`, `EstimatedPosition/Velocity`,
  `TrackQuality`, `ReportingNodeID`) so PN guidance and other consumers process
  onboard-sensor and datalink-relayed tracks identically — this is the natural home for
  that schema; nothing in Phase 2 strictly requires it before this point.

## Risks & Open Questions

- **Simulation complexity vs. fun**: realistic flight/guidance physics can become fiddly, however if a decision has to be made between realism and arcadiness, pick realism. Use real world inspired parts, not sci-fi.
- **Scope creep**: the part list is extensive — Phase 2 should timebox breadth rather than
  gold-plating any single category before all categories exist at a basic level.
- **AI difficulty scaling**: keeping CPU opponents credible across the full tech spectrum
  (grenade-drones to hypersonic stealth) without reworking AI at every tier needs early design attention.
- **Performance**: large numbers of physics-simulated projectiles/drones in combat scenes —
  plan for object pooling and simplified physics LOD from Phase 1 onward.
- **Part-combinatorial art/data explosion**: rotor material × size × drone airframe class
  × hull material × seeker type × engine type multiplies fast. If each combination needs
  a hand-authored mesh/prefab, art alone can eat all of Phase 2. Keep visuals procedural/
  parametric (generate geometry from data, tint/scale via `MaterialPropertyBlock`) the
  way `DroneVisualBuilder` already does for the quadcopter, rather than hand-authoring a
  mesh per combination — reserve hand-authored assets for airframe silhouettes only.
- **Dual-layer damage integration is bigger than one checkbox**: making sub-component
  damage matter means wiring functional penalties into systems that don't know about
  sub-components today — fuel-tank damage into `FlightBody`'s fuel drain, flight-surface
  damage into lift/turn-rate, seeker damage into `GuidanceController`. Treat this as
  several integration tasks in 2C, not one.
- **Sandbox Campaign (Phase 3) has no data model yet**: see the Pre-Phase-3 deliverable
  above — this is the largest unscoped item in the plan and should get its own design
  deep-dive before implementation starts, the same way 2A-2G did.
- **Behavior-tree package maturity**: if 2D adopts Unity's `com.unity.behavior` package,
  confirm it's stable/out of preview for the project's Unity version before committing —
  the existing hand-rolled FSM (`EnemyDroneAI`) is a perfectly viable permanent choice
  for AI this simple if the package isn't mature enough yet.
- **Multiplayer readiness debt (informational, not a v1.0 blocker)**: client-authoritative
  `FlightBody` physics, brute-force `FindObjectsByType` scans, and the singleton
  `TeamAwareness` contact-aggregator will all need rewriting (not adapting) for netcode
  if Post-1.0 multiplayer is pursued. Fine to defer, but don't be surprised later.
