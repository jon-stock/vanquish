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

- [x] **Proportional navigation** guidance law: added `ProportionalNavigation :
  IGuidanceLaw` in `Simulation/Guidance/`, implementing true PN — the standard 3D vector
  form `a_cmd = N · Vc · (ω × r̂)` (ω = LOS rotation rate = `(r × v_rel)/|r|²`, Vc =
  closing velocity, N = navigation gain) rather than pursuit's "always steer toward the
  target's current position." Added `GuidanceLawFactory.Create(MissileLoadout)` as the
  single place that maps `SeekerDefinition.seekerType` to a guidance law
  (SemiActiveRadar/ActiveRadar/MultiSpectral → PN; everything else, including
  WireOrDatalinkGuided per this item's own example, → pursuit), called from
  `VehicleFactory.SpawnMissile` right after `GuidanceController.SetTarget`. Validated
  headlessly via a new pure-C# kinematic simulator (`Phase2CValidation.
  ValidateGuidanceLaws` — no scene/Play mode/Physics needed, mirrors FlightBody's own
  thrust/drag/steering-clamp model closely enough to be representative) against a
  target offset 250m laterally and weaving with 40m amplitude: unguided baseline missed
  by 196m; pursuit hit at 13.6m/17.26s; **PN hit tighter and faster, at 10.7m/12.66s** —
  confirmed out-intercepting pure pursuit at the same tuning, exactly this item's ask.
- [x] **Datalink mid-course update**: added `DatalinkNetworkDefinition.datalink`
  (optional) to `MissileLoadout`, and `DatalinkMidCourseGuidance : IGuidanceLaw` — a
  wrapper `GuidanceLawFactory` uses whenever a design's datalink has
  `supportsMidCourseUpdates = true`. Outside the missile's own `SeekerDefinition.
  detectionRangeMeters`, it flies toward a target position/velocity that's only
  re-sampled every `DatalinkUpdateIntervalSeconds` (2s) instead of every tick —
  simulating real relay latency; once within seeker range, it hands off to the
  missile's real terminal guidance law (PN for radar seekers) for full every-tick
  precision homing. Simplification (documented on the class itself): rather than
  threading `TeamAwareness`-relayed contact data through the whole `IGuidanceLaw`
  interface, the wrapper reproduces the behaviorally-important effect (stale mid-course
  data, precise terminal data) by re-sampling the same true position/velocity
  `GuidanceController` already provides at a slower cadence — the outcome is
  equivalent without a wider interface change. Seeded one real part,
  `Datalink_MidCourseRelay` (`Assets/_Project/Data/Support/`, via new
  `Phase2CGuidanceDepthSeeder`), gated behind a new `TN_2C_support_datalink_midcourserelay`
  TechNode, and wired as an optional "Datalink" row in the Workshop's Missile Loadout
  picker. Validated headlessly via `Phase2CValidation.ValidateDatalinkMidCourseHandoff`:
  a datalink+PN missile launched from 6000m away (far outside any seeker's range)
  correctly hands off to the terminal seeker before impact and still lands a hit
  (13.6m miss distance) — confirming the mid-course phase successfully closes the gap
  on stale data alone before precision terminal homing takes over.
- [x] **Probabilistic detection**: replaced `DetectionSensor`'s binary
  distance-vs-effective-range check with `ComputeDetectionProbability` — a smooth
  quadratic falloff (1.0 at zero distance, 0.0 at/beyond effective range) instead of a
  hard cutoff — and added intermittent contact loss/reacquisition via a new
  `reacquisitionGraceSeconds` field (a missed detection roll no longer instantly drops
  a tracked contact; it only drops after that many seconds of consecutive misses).
  Wired from `SeekerDefinition.reacquisitionTimeSeconds` for missiles in
  `VehicleFactory.SpawnMissile` — **previously-dead data (per this item's own note),
  now actually consumed**. `MissileAirframeDefinition.baseRadarCrossSection` and
  countermeasure RCS multipliers were already folded into the effective-range
  calculation before this item (via `DesignStatsCalculator`/`DetectableSignature`); this
  item's change is what makes that number matter *probabilistically* — a much lower-RCS
  design is now meaningfully harder to reliably detect near the edge of a sensor's
  range, not just detected at a shorter hard cutoff. Validated headlessly via
  `Phase2CValidation.ValidateDetectionAndJammingMath`: probability = 1.0 at 0m, 0.0 at
  and beyond the effective range, monotonically decreasing in between — all confirmed.
- [x] **Jamming/counter-jamming**: added `JammerSource` (`Simulation/Sensors/`) — added
  to a missile by `VehicleFactory.SpawnMissile` whenever its `MissileLoadout.jamming` is
  set (jamming equipment is currently only a missile-slot part — see `JammingDefinition`'s
  own doc comment on ECM/ECCM). `DetectionSensor.Rescan` now queries nearby enemy
  `JammerSource`s each scan (same brute-force pattern as its existing
  `DetectableSignature` scan, with the same "replace with a spatial query in the
  performance pass, not now" note) and reduces detection probability for every contact
  that scan by the strongest nearby jammer's `jammingStrength`, offset by the sensor's
  own `jamResistance` (wired from `MissileRuntimeStats.jamResistance` — i.e.
  `SeekerDefinition.jamResistance` + any equipped `JammingDefinition.
  counterJammingStrength` — both already computed by `DesignStatsCalculator` before
  this item, just never consumed against a live jammer until now). Validated headlessly
  via `Phase2CValidation.ValidateDetectionAndJammingMath`: strong jamming against weak
  resistance meaningfully reduces detection probability (0.8 jam vs. 0.2 resistance →
  0.4× multiplier); strong resistance fully offsets weaker jamming (0.3 jam vs. 0.9
  resistance → 1.0×, no effect); no jamming present has no effect — all confirmed.
- [x] Countermeasure decoys (`decoyCharges`/`decoySuccessChance`) now give a
  currently-locked missile a chance to break lock, via a new `CountermeasureController`
  (`Simulation/Sensors/`) and a check added to `GuidanceController.FixedUpdate`: each
  tick, if the current target has a `CountermeasureController` within its
  `threatRangeMeters`, it may auto-deploy a decoy (gated by its own cooldown so one
  missile can't be spoofed by every charge in a single engagement); a successful roll
  (`decoySuccessChance`) breaks the lock (`target = null`) and the missile flies
  ballistic from there. This is the "AI action" half of this item's own requirement —
  fully automatic self-defense needing no player input, so AI-controlled drones benefit
  too; `CountermeasureController.TryDeployDecoy` is also exposed standalone for a future
  manual player-triggered key bind (not wired to an input binding yet — the "player
  action" half is deliberately left as a ready-to-use API rather than forcing a new
  keybinding scheme into this pass). Required extending `DroneLoadout` with an optional
  `countermeasure` field (reusing `MissileLoadout`'s existing `CountermeasureDefinition`
  type) — decoy/flare-chaff equipment logically belongs to whatever's defending against
  an inbound missile, not the missile itself, so a drone needed its own countermeasure
  slot for this mechanic to make sense; the Workshop's Drone Loadout picker gained a
  "Countermeasure" row reusing 2A's already-seeded assets (no new assets needed —
  `PlayerProgress.IsPartUnlocked` works correctly against the same asset referenced from
  two different option arrays). Validated headlessly via `Phase2CValidation.
  ValidateCountermeasureDecoys`: 3 charges at 100% success chance all correctly break
  lock, a 4th attempt after charges are exhausted correctly fails, and a 0%-chance
  countermeasure correctly never breaks a lock — all confirmed.

**Technical notes:** `IGuidanceLaw` remained the sole extension point throughout — no
missile behavior was special-cased outside it; `GuidanceLawFactory` and
`DatalinkMidCourseGuidance` are themselves just more `IGuidanceLaw` implementations/
compositions, not a parallel system. The headless regression test for guidance law
comparison exists as `Phase2CValidation.ValidateGuidanceLaws`, but as a pure-C#
kinematic simulator rather than a `Phase1BatchRunner`-style Play-mode scene (as
originally sketched here) — a full scene/Physics/Play-mode cycle wasn't needed since
`IGuidanceLaw.ComputeSteering`'s interface is pure data in/data out, and the simulator
reproduces `FlightBody`'s thrust/drag/steering-clamp model closely enough to be
representative; this is faster, fully deterministic, and avoids the "Editor already has
the project open" conflict that blocks any Play-mode-based headless run. The initial
version of this test scenario (target dead-ahead with a weave smaller than the hit
threshold) accidentally made *every* guidance law — including no guidance at all —
trivially "hit" by construction, silently masking any real difference between laws;
fixed by giving the target a real lateral offset and a weave amplitude larger than the
hit threshold, which is what actually exercises guidance quality. Worth remembering for
any future guidance-law test: a test scenario that doesn't actually require correction
can't distinguish good guidance from none.

**Exit criteria:** A player can tell the difference in a fight between a pursuit-guided
missile, a PN-guided missile, and a datalink+PN missile (✅ — confirmed via the headless
kinematic comparison above: unguided missed by 196m, pursuit hit but with a larger
miss distance and later intercept, PN hit tighter and faster, and a datalink+PN missile
successfully closed a 6000m gap on stale mid-course data before terminal homing;
verifying the *player-perceivable* difference in an actual live dogfight is still worth
doing once Phase 2D gives AI-controlled enemies more varied loadouts to fire back with,
since the current Tier-0 enemy still only ever fires the same basic IR/pursuit missile);
jamming/countermeasures visibly affect whether a shot connects (✅ — confirmed via the
headless jamming-multiplier and decoy-roll checks above; both are live in
`DetectionSensor`/`GuidanceController` for any real design that equips them).

---

#### 2D — AI Depth
**Goal:** CPU opponents stop being a single patrol→engage FSM and start having distinct roles.

- [x] **Interceptor** archetype: aggressive, prioritizes closing distance and engaging
  the player's strike drone specifically. Formalized the Phase 1 `EnemyDroneAI` into
  `InterceptorAI` — moved from `Scripts/Combat/` into `Scripts/AI/` (the "CPU opponent
  behavior" folder `docs/CODING_STANDARDS.md` always described but never had content
  in, namespace `Vanquish.AI`); patrol/pursuit steering itself is unchanged (per this
  item's own note that `EnemyDroneAI` was already "close to this"). The one
  substantive behavior change: target *selection* now specifically prefers "the
  player's strike drone" instead of whichever contact is merely nearest. Added
  `DetectableSignature.isArmed` (baked in by `VehicleFactory.SpawnDrone` from
  `loadout.missileLoadout?.IsComplete`, so any role-aware AI can tell an armed strike
  drone apart from an unarmed scout without a `GetComponent<WeaponController>()` probe)
  and a new `armedOnly` parameter on `TeamAwareness.GetNearestKnownEnemy` (backed by a
  new pure `TeamAwareness.SelectNearest` helper, factored out specifically for headless
  testing). `InterceptorAI.AcquireTarget` queries `armedOnly: true` first and only
  falls back to any known contact if no armed one is known yet — so it isn't
  permanently inert if only a lone scout is in the fight. This is a real,
  previously-latent bug fix, not just a rename: in the existing Phase 1 arena the
  scout drone spawns fractionally closer to the enemy's spawn point than the strike
  drone it's escorting, so plain nearest-contact selection could have the enemy AI
  fixate on the harmless scout instead of the armed strike drone — exactly the gap
  this PLAN.md item called out. **Follow-up refactor** (done alongside the
  Scout-hunter item below, once a second nearly-identical archetype made the
  duplication concrete): the patrol/steer/fire loop was factored out of `InterceptorAI`
  into a shared abstract `DroneCombatAI` base (`enum PatrolEngageState`, was
  `InterceptorState`) so each archetype subclass only implements its own
  `AcquireTarget()` targeting policy — still separate MonoBehaviour types per archetype
  (per this sub-milestone's own technical note against "one mega-controller with
  branching modes"), just without re-typing identical boilerplate per archetype.
  Verified headlessly via new `Phase2DValidation.ValidateInterceptorArmedTargetPriority`:
  reproduced the scout-closer-than-strike-drone arrangement with disposable
  `DetectableSignature` GameObjects — confirmed plain nearest-contact selection would
  pick the closer unarmed scout, confirmed `armedOnly` selection correctly picks the
  (more distant) armed strike drone instead, and confirmed `armedOnly` selection
  returns `null` (not a wrong answer) when no armed contact is known at all, so the
  fallback path has a real reason to run — all PASS. Also re-ran
  `Phase1CombatSceneBuilder.BuildScene` headlessly (regenerates `Combat_Arena01.unity`
  against the new `InterceptorAI` type/GUID since the old `EnemyDroneAI.cs`/`.meta`
  were deleted as part of the move) with no missing-asset errors, and a full
  60-second `Phase1BatchRunner` headless Play-mode regression with no
  exceptions/missing-script errors — re-run again after the `DroneCombatAI` extraction
  to confirm the refactor didn't change live behavior.
- [x] **Scout-hunter** archetype: prioritizes targeting known/likely scout drones
  first (since killing the scout blinds the player's TeamAwareness). Added
  `ScoutHunterAI : DroneCombatAI` alongside `InterceptorAI` (see the refactor above —
  both now share the same patrol/steer/fire loop, differing only in `AcquireTarget()`).
  Role discrimination uses exactly the mechanism this item itself suggested:
  `DetectableSignature.isScout`, baked in by `VehicleFactory.SpawnDrone` from
  `SensorSuiteDefinition.sharesContactsWithTeam` (confirmed against the actual seeded
  assets: `Sensor_Basic.sharesContactsWithTeam = false`, `Sensor_Scout.
  sharesContactsWithTeam = true` — the flag genuinely discriminates strike vs. scout
  drones in real data, not just in theory). Added `TeamAwareness.
  GetNearestKnownScoutEnemy` alongside a generalized `TeamAwareness.SelectNearest`
  (now takes a `Func<DetectableSignature, bool>` role filter instead of a single
  `armedOnly` bool, so both Interceptor's armed-preference and Scout-hunter's
  scout-preference share one selection function rather than diverging bespoke copies).
  `ScoutHunterAI.AcquireTarget` prefers the nearest known scout, falling back to any
  known contact if no scout is known yet — mirrors Interceptor's own
  no-armed-contact-known fallback, so this archetype isn't inert against an opposing
  team with no scout. Verified headlessly via new `Phase2DValidation.
  ValidateScoutHunterScoutTargetPriority`: a scenario with an armed strike drone closer
  than a scout — confirmed plain nearest-contact selection would (wrongly) pick the
  strike drone, confirmed scout-priority selection correctly picks the farther-away
  scout instead, and confirmed scout-only selection returns `null` when no scout is
  known, so `ScoutHunterAI`'s fallback path has a real reason to run — all PASS.
  Re-ran `Phase1CombatSceneBuilder.BuildScene` and a full 60-second `Phase1BatchRunner`
  headless Play-mode regression (both unaffected by this item, since neither
  `ScoutHunterAI` nor a second enemy archetype is wired into the Phase 1 MVP arena yet
  — deliberately deferred: the MVP scene's win condition/HUD assume a single enemy
  drone, and this sub-milestone's own exit criteria expects interceptor + scout-hunter +
  SAM site to be demonstrated together once the SAM site item below also lands, not
  wired in piecemeal against the existing single-enemy arena) — no exceptions/
  missing-script errors, confirming the refactor and new type didn't regress anything.
- [x] **SAM site** archetype: static (or minimally-mobile) `BaseDefenseDefinition`-driven
  unit with a fixed position, long engagement range, high rate of fire — needs its own
  spawner path (not `VehicleFactory.SpawnDrone`, since it's not a drone) and a simple
  "engage anything in range" controller rather than patrol/pursuit logic. Added
  `InstallationFactory.SpawnBaseDefense` as a sibling static class to `VehicleFactory`
  (not a new `VehicleFactory` method) — per this item's own instruction and PLAN.md's
  independently-arrived-at 2F design intent ("placed installations need their own
  spawner, parallel to `VehicleFactory`, since they're static/semi-static"). It skips
  everything flight-specific (`Rigidbody`, `FlightBody`, `orientToVelocity`,
  `CrashDamage`) that `VehicleFactory.SpawnDrone` does, but reuses
  `DetectableSignature`/`DetectionSensor`/`Health`/`WeaponController` verbatim —
  confirmed beforehand that none of those four are actually drone-coupled (in
  particular, `WeaponController` only needs a `MissileLoadout` + `transform` + optional
  sibling `Collider`, nothing drone-specific). `SamSiteAI` (`Scripts/AI/`) is the
  "engage anything in range" controller: deliberately does **not** extend
  `DroneCombatAI` (which requires a `FlightBody`/`Rigidbody` and implements a
  patrol↔engage steering loop this unit has no use for) — it's a plain
  `MonoBehaviour` that finds the nearest known enemy of its own team each tick and
  fires if within `engagementRangeMeters`, no movement/steering/patrol point at all,
  the simplest archetype in the project by design. `BaseDefenseDefinition` gained a
  `missileLoadout` (`MissileLoadout`, embedded directly on the definition since a SAM
  site has no airframe/propulsion/sensor-suite of its own to assemble one around, per
  2F's later Workshop-placement flow not existing yet) and `ammoCount` field.
  `Phase2DSamSiteSeeder` seeds one real asset, `BaseDefense_SamSite_Basic` (long
  1500m engagement range and 1 shot/second — both well beyond a drone's typical
  250-400m engage range / 2.5s cooldown, per this item's own "long engagement range,
  high rate of fire" description — reusing the same Tier-0 missile parts as
  Phase1CombatSceneBuilder's "Basic Missile" rather than seeding new dedicated SAM
  missile parts, since this item is about the site/AI/spawner existing and behaving
  correctly, not new missile part breadth). Not yet wired into any tech tree/Workshop
  placement flow — that's Phase 2F's job; currently only consumed by
  `InstallationFactory`/`CombatTestSceneBuilder`. Wired into the dev-testing tool
  (`CombatTestSceneBuilder`/`CombatTestSceneBuilderWindow`) as a new `TestArchetype.
  SamSite` case — required restructuring `SpawnEnemyRoster`'s per-slot spawn call
  (previously always `VehicleFactory.SpawnDrone`) to branch between the drone spawn
  path and `InstallationFactory.SpawnBaseDefense`, exactly the "won't just be one
  switch case" caveat the tool's own doc comment already flagged. The window's
  "Armed" toggle is correctly disabled/ignored for `SamSite` groups (always armed via
  its own `BaseDefenseDefinition.missileLoadout`, not the drone strike/scout loadout
  toggle); the "Fire Cooldown" override still applies uniformly regardless of
  archetype. `BuildDefaultMultiArchetypeTestScene` now spawns all three combat
  archetypes together (1 Interceptor + 1 Scout-hunter + 1 unarmed bait scout + 1 SAM
  site) — directly demonstrating this sub-milestone's exit criteria. Verified
  headlessly: `Phase2DValidation.ValidateSamSiteDefinitionAsset` confirms the seeded
  asset exists with a complete missile loadout and positive
  engagement-range/fire-rate/health/ammo (ALL PASS); rebuilt the multi-archetype test
  scene and ran a full 60-second headless Play-mode battle with no exceptions/
  missing-script errors, with the console log confirming `Enemy_SamSite_*` actually
  fires — and at exactly the intended 1.0s cadence (`t=0.0s, 1.0s, 2.0s, 3.0s...`) once
  the demo composition's fire-rate override was set to match `BaseDefense_SamSite_
  Basic.rateOfFirePerSecond` rather than the tool's generic default; re-confirmed the
  original fixed MVP arena still builds cleanly, unaffected.
- [ ] Replace/augment the current hand-rolled FSM (`PatrolEngageState` enum + `if`/`else`
  in the shared `DroneCombatAI` base that `InterceptorAI`/`ScoutHunterAI` both extend,
  per the archetype items above) with actual behavior trees once there are 3+
  archetypes sharing building blocks (detect, evade, engage, retreat-when-low-health) —
  evaluate Unity's Behavior package (per the original tech stack notes) vs. continuing
  hand-rolled FSMs; don't adopt a framework speculatively, decide once the archetype
  count makes shared nodes clearly worth it.
- [ ] AI should react to being jammed/detected-by-countermeasure (from 2C) — e.g. break
  off or use its own countermeasures — otherwise 2C's systems are invisible to the AI
  side of the fight.

**Technical notes:** Keep archetypes as separate MonoBehaviours (like today's
`InterceptorAI`/`ScoutHunterAI`/`ScoutPatrol` split) rather than one mega-controller
with branching modes — matches the existing pattern and keeps each headlessly testable
in isolation. `InterceptorAI`/`ScoutHunterAI` do now share a common `DroneCombatAI`
base for their identical patrol/steer/fire plumbing (added once the Scout-hunter item
made that duplication concrete), but each remains its own concrete subclass overriding
only its targeting policy — not a single class branching on an archetype enum.

**Dev-testing infrastructure added alongside this sub-milestone** (not a PLAN.md
checklist item itself, but worth recording since it's now the answer to "how do I
test the next AI feature live"): the Workshop → Combat flow only ever built one fixed
arena (`Phase1CombatSceneBuilder`, one hardcoded enemy), so a new archetype like
Scout-hunter had no way to be exercised visually without hand-editing scene-building
code. Added `CombatTestSceneBuilder`/`EnemySpawnGroup`/`TestArchetype` (reusing
`Phase1CombatSceneBuilder`'s loadout-loading/scene-boilerplate helpers, now `internal`
instead of `private` so they can be shared) to build a combat scene from an arbitrary,
caller-specified enemy roster — any mix/count of archetypes — saved to a separate
`Combat_TestArena.unity` so it never collides with the fixed MVP arena
`Phase1BatchRunner` regression-tests. `CombatTestSceneBuilderWindow` is the interactive
`Vanquish/Debug/Combat Test Scene Builder` menu: add/remove enemy groups, pick each
group's archetype/armed-or-unarmed/count, then "Build Test Scene" or "Build & Enter
Play Mode" — no code changes needed to test a new mix. `Phase1BatchRunner`'s headless
Play-mode smoke test was generalized to accept any scene path (was hardcoded to the
MVP arena) so this tool's scenes get the same "does it actually run without
exceptions" regression check. Every future drone-based archetype just needs one
`case` added to `CombatTestSceneBuilder`'s spawn switch to show up in this tool too —
though the SAM site item below turned out to need slightly more than "just a case"
(see its own writeup) since it isn't a drone at all.
Verified headlessly: `Vanquish/Phase 2D/Build Default Multi-Archetype Test Scene
(Headless)` (1 Interceptor + 1 Scout-hunter + 1 unarmed bait scout) builds with no
missing-asset errors, and a full 60-second headless Play-mode run against that scene
completes with no exceptions/missing-script errors; re-confirmed
`Phase1CombatSceneBuilder.BuildScene` (the original fixed MVP arena) still builds
cleanly after the shared-helper access-modifier changes.

**Follow-up to the dev-testing tool above**: `EnemySpawnGroup` gained a
`fireCooldownSeconds` field (exposed in `CombatTestSceneBuilderWindow` as a per-group
"Fire Cooldown (s/shot)" field, disabled when a group is unarmed), applied post-spawn
via `WeaponController.fireCooldownSeconds` — so testing "what if the enemy fires twice
as fast" no longer needs a code/data change, just a field in the window.

**Dev-visibility pass** (also raised during manual testing of the tool above — real
combat/AI behavior was correct, but essentially invisible until impact): diagnosed as
not actually a draw-distance/clipping problem (camera far clip was already 3000m,
fog was already off) but a *conspicuity* problem — the prototype primitives
(`DroneVisualBuilder`'s ~2m drone, `VehicleFactory`'s 0.4m missile capsule) are too
small and too plain-grey to read against the sky/ground at real engagement distances,
with nothing marking a launched missile as "look here" before it's already close.
Fixed with four changes: (1) `TeamColorUtility` — bright red enemy / blue player
materials (with a slight emissive tint for shadow readability) applied to every drone
and missile instead of Unity's default grey; (2) `DroneVisualBuilder.AddEngineGlow` —
a small bright emissive core + point light at the tail of every missile and every
fixed-wing/jet drone (multirotors already have visible spinning rotors as their "it's
moving" cue, so skipped); (3) `HUDController.DrawDistantContactMarkers` — a red diamond
+ distance readout drawn directly over a known enemy contact's actual on-screen
position once it's farther than `distantMarkerMinDistanceMeters` (150m default),
complementing (not replacing) the existing corner mini-radar, which tells you *that*
something's out there but not *where to look* in the 3D view; off-screen contacts are
skipped for now (an edge-of-screen directional arrow is a natural follow-up, out of
scope for this pass). (4) Headroom bump: camera far clip `3000f → 12000f` (covers
`Seeker_MultiSpectral`'s 9000m detection range plus margin) and explicit
`RenderSettings.fog = false` / `fogEndDistance = 10000f` in `Phase1CombatSceneBuilder.
BuildLight` — previously each new scene silently inherited Unity's new-scene fog
defaults (off, but with a 300m end distance baked in), a landmine for whoever first
enabled fog without noticing it'd cut off well inside seeder range. `CombatTestSceneBuilder`
inherits both automatically since it reuses `BuildCamera`/`BuildLight`. Verified
headlessly (rebuild of both the MVP arena and the multi-archetype test scene, plus a
60-second Play-mode regression, all with no compile/runtime errors) and confirmed live
by manual playtesting — distant contacts are now visible via the marker well before a
missile is close enough to be a threat.

**Exit criteria:** A single battle can contain an interceptor, a scout-hunter, and a
SAM site simultaneously, each behaving visibly differently (✅ — all three now exist
and are demonstrated together via `CombatTestSceneBuilder.
BuildDefaultMultiArchetypeTestScene`/`Vanquish/Debug/Combat Test Scene Builder`: the
Interceptor beelines for the armed strike drone specifically, the Scout-hunter
diverts for the unarmed scout even when a closer strike drone is available, and the
static SAM site never moves but engages anything within its long 1500m range at a
fast, fixed 1s cadence — visibly distinct behaviors confirmed via headless Play-mode
regression and manual playtesting. The remaining two checklist items above — behavior
trees and AI reacting to jamming/countermeasures — are follow-on depth work, not
required for this exit criterion, and remain unchecked/deferred).

---

#### 2E — Maps & Scenarios
**Goal:** Combat isn't just "one flat arena, kill everything" anymore.

- [x] **Decide the terrain approach before building arenas**: `Phase1CombatSceneBuilder`'s
  scripted-primitive pattern works for flat arenas but can't reasonably generate actual
  mountains/valleys/plateaus from code. Author 2-3 heightmap terrains with Unity's
  Terrain tools (or a cheap terrain asset pack) and keep using the scripted-builder
  pattern only for unit/camera/objective placement on top of that terrain. **Decision:
  procedural, code-generated heightmap terrain** (`TerrainArenaBuilder`, new) — a
  height function (0..1 in, 0..1 out) sampled across a `TerrainData.SetHeights` grid,
  using only the built-in Terrain/TerrainPhysics modules (confirmed present in
  `Packages/manifest.json`, no `terrain-tools` package needed for scripted generation).
  Chosen specifically because it's the only approach consistent with this project's
  existing "everything reproducible via code" convention — hand-sculpted terrain can't
  be diffed/regenerated by changing a parameter the way every other part of these
  scenes can, and an asset pack adds an external dependency for no real benefit at this
  project's "ugly art is fine" stage. `GroundSampler` (Simulation/Flight) had already
  anticipated exactly this back in Phase 2B — its own doc comment says it samples via a
  real downward raycast specifically so it would "pick up actual terrain colliders
  unmodified once Phase 2E adds them" — confirmed true: `Terrain.CreateTerrainGameObject`
  adds a `TerrainCollider` automatically, so zero `GroundSampler` changes were needed
  for AGL altitude readouts to work correctly on the new terrain.
- [x] At least 2–3 additional arena layouts (terrain variation, cover, different
  engagement distances) — reuse `Phase1CombatSceneBuilder`'s scripted-scene-construction
  pattern rather than hand-placing in the Editor, so maps stay reproducible/diffable.
  Added `Phase2EArenaBuilder` with two new terrain arenas (reusing
  `Phase1CombatSceneBuilder`'s loadout-loading/light/camera/HUD helpers rather than
  duplicating them, same precedent as `CombatTestSceneBuilder`); the flat Phase 1 MVP
  arena (`Combat_Arena01.unity`) is deliberately left unchanged for stability, since
  `Phase1BatchRunner`'s regression and the "Core First" MVP loop both depend on it:
  - **Valley** (`Combat_Arena_Valley.unity`, 800×1200m, V-shaped valley floor, max
    80m walls): player and a SAM site/guarding Interceptor spawn ~1000m apart down the
    valley floor — a much longer engagement distance than the flat arena's ~400m —
    with the valley walls themselves acting as terrain cover from anything off the
    direct line.
  - **Plateau** (`Combat_Arena_Plateau.unity`, 600×600m, raised central plateau with
    steep cliff edges, max 50m): 2 enemies (Interceptor + Scout-hunter) at a
    deliberately tighter ~150m patrol radius (vs. the MVP arena's 250m) — closer-range
    engagements, with the cliffs blocking sightlines around the plateau's edges (a
    different tactical shape from the valley's long open sightline).
  Both scatter a handful of cube "cover rock" primitives (no imported art, same
  procedural-primitives convention as `DroneVisualBuilder`) and are registered in
  `EditorBuildSettings` automatically by the builder (confirmed: `SceneManager.
  LoadScene` requires build-settings registration; `Combat_TestArena.unity`, Phase 2D's
  dev-only scene, deliberately stays unregistered since it's never loaded by name).
- [x] At least one non-skirmish objective type (e.g. "destroy the enemy launch
  platform/base installation" rather than "destroy all enemy units") — needs
  `CombatManager`'s win condition to become pluggable (an `IObjective`/strategy
  interface) rather than the current hardcoded "all enemy `Health` destroyed."
  Added `IObjective` (`Description` + `IsVictoryAchieved()`), with two
  implementations: `DestroyAllEnemiesObjective` (the original Phase 1 logic,
  formalized rather than rewritten — still the default, so every pre-2E scene's exact
  behavior is unchanged) and `DestroyTargetObjective` (victory once one specific
  unit's `Health` is destroyed, independent of every other enemy in the scene — used
  by the Valley arena's SAM site). Defeat ("all player units destroyed") stays a
  universal rule inside `CombatManager` itself, per this item's own scope note that
  only the *victory* condition needs to vary. **Serialization-driven, not a live
  interface reference**: `IObjective` is a plain C# interface, so a scene-builder
  script assigning one directly to a `CombatManager` field at edit time would NOT
  survive Unity's scene save/reload (confirmed by checking — a plain interface
  reference isn't a `UnityEngine.Object` reference or a `[Serializable]` value type,
  so it silently reverts to a fresh default on the next deserialization, which would
  have made every DestroyTarget objective quietly turn back into DestroyAllEnemies
  the moment the built scene was ever closed and reopened). Instead, `CombatManager`
  stores a serializable `ObjectiveType` enum + a plain `GameObject objectiveTarget`
  reference (both serialize natively) and builds the actual `IObjective` instance in
  `Awake()` (`CombatManager.BuildObjective`, made `public` rather than `private`
  specifically so `Phase2EValidation` — in the separate Editor assembly, where
  `internal` isn't visible — can exercise it directly, since `Awake()` itself never
  runs outside Play mode and so can't be reached from a headless edit-mode test at
  all). Falls back to `DestroyAllEnemiesObjective` if `DestroyTarget` is selected but
  misconfigured (no target/no `Health` component), rather than leaving victory
  permanently unreachable. `HUDController`'s VICTORY/DEFEAT banner now also shows
  `CombatManager.ObjectiveDescription` underneath, so the player sees *which*
  objective they won/lost. Verified headlessly via new `Phase2EValidation.
  ValidateObjectives`: confirmed `DestroyAllEnemiesObjective` is false while a
  registered enemy is alive and true once destroyed, confirmed `DestroyTargetObjective`
  tracks its own specific target correctly, and confirmed the misconfigured-DestroyTarget
  fallback correctly rebuilds `DestroyAllEnemiesObjective` — all PASS (the first attempt
  at the fallback check was itself wrong, not the implementation: it tried to trigger
  `Awake()` via `GameObject.SetActive`, which doesn't run outside Play mode for a
  MonoBehaviour without `[ExecuteAlways]` — fixed by calling `BuildObjective()`
  directly instead). Also confirmed via the Valley scene's saved file that
  `objectiveType`/`objectiveTarget`/`objectiveTargetDescription` actually round-trip
  through `EditorSceneManager.SaveScene` correctly.
- [x] Scenario selection needs a place to live — likely a small scenario-picker screen
  before entering Combat, or a dropdown in the Workshop's "Enter Combat" flow. Added
  `ScenarioPickerOverlay` — an OnGUI immediate-mode picker in the Workshop scene
  (same "ugly art is fine, the loop must be complete" precedent as `HUDController`'s
  own combat HUD — deliberately not a `Workshop.uxml`/UI Toolkit addition, since this
  sub-milestone's goal is "more than one arena/objective exists and is choosable," not
  UI polish, which is Phase 3's job) listing every seeded `ScenarioDefinition` as a
  clickable row; selecting one sets `PlayerProgress.PendingScenario` (new field,
  same in-memory cross-scene-transient pattern as the existing `PendingStrikeDroneLoadout`/
  `PendingScoutDroneLoadout`). `WorkshopController.OnEnterCombatClicked` now loads
  `PendingScenario.sceneName` when set, falling back to its original hardcoded
  `combatSceneName` otherwise — purely additive, so `Combat_Arena01` stays reachable
  even from a Workshop scene that never shows the picker (e.g. an older save/scene
  state). Defaults to the first scenario automatically (Tier-0 Skirmish) so Enter
  Combat is meaningful even without ever clicking the picker. Verified headlessly:
  rebuilt the Workshop scene (picks up `ScenarioPickerOverlay` + the 3 seeded
  `ScenarioDefinition` assets) with no compile/load errors.

**Technical notes:** This is a good place to introduce a lightweight `ScenarioDefinition`
ScriptableObject (scene reference + objective type + starting unit placements) so
`Phase1CombatSceneBuilder`-style tools and `CombatManager` both read from one data
source instead of each scene hardcoding its own setup. **Actual implementation note:**
`ScenarioDefinition` ended up deliberately lightweight — just `id`/`displayName`/
`description`/`sceneName`/`objectiveSummary` metadata for the picker, not a full
scene-generation spec (no embedded unit-placement data). Every arena, including its
objective configuration, is still authored by a scripted Editor scene-builder
(`Phase1CombatSceneBuilder`/`Phase2EArenaBuilder`) that bakes everything directly into
the saved `.unity` scene — matching the project's existing convention (nothing is
runtime-composed from data) rather than introducing a second, parallel
scene-description format. `ScenarioDefinition` is the picker's index card for a
pre-built scene, not that scene's source of truth.

**Exit criteria:** Player can choose between at least 2 scenarios with different maps
and at least one has a non-"kill everything" win condition (✅ — 3 scenarios exist:
Tier-0 Skirmish (flat, `DestroyAllEnemies`), Valley Interdiction (terrain,
`DestroyTarget` — the non-skirmish win condition), and Plateau Skirmish (terrain,
`DestroyAllEnemies` at closer range); all 3 selectable via `ScenarioPickerOverlay` in
the Workshop scene). Verified headlessly: all three combat scenes build with no
errors and pass a full 60-second headless Play-mode regression with no exceptions —
the Plateau arena's tighter engagement distance produces real weapon fire within the
first few seconds (confirmed in the regression log); the Valley arena's much longer
~1000m distance means no engagement occurred within the 60-second *idle-player*
automated window, which is expected (patrol/detection/pursuit is unchanged, working
code — a real player actively closing distance, unlike a headless test's stationary
player, would engage far sooner) and not itself a defect, though it's worth tuning
detection/patrol ranges or spawn distances in a later pass if a real playtest confirms
the valley feels too slow to start.

---

#### 2F — Base-Building / Support Architecture
**Status: Deferred.** Skipped for now per project direction — still a genuine Phase 2
sub-milestone (not moved to Phase 3), just worked out of order; revisit before Phase 2
is considered fully closed out. 2G was completed ahead of it instead (PLAN.md's own
technical note on 2G already flagged it as "a good candidate to build early or
interleaved... rather than strictly last").

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

- [x] A "Test Range" mode reachable from the Workshop: spawns the player's current
  design (via the same `VehicleFactory` combat uses) against one or more stationary or
  simple-moving dummy targets, using the exact same simulation as real combat — this is
  explicitly the payoff of the "one data-driven part/stat model" principle from the
  plan's Concept Summary. `TestRangeSceneBuilder` builds `TestRange.unity`: the
  player's strike drone (via `CombatPlayerLoadoutApplier`, unmodified — it has no
  dependency on `CombatManager` existing at all, only on finding `"Player_Drone"`/
  `"Scout_Drone"` by name, so it's reusable here verbatim) against one **stationary**
  dummy (an unarmed drone via `VehicleFactory.SpawnDrone` with no AI component
  attached at all — electric multirotor propulsion applies zero thrust without a
  steering source, so it simply sits still, with zero new code) and one
  **simple-moving** dummy (same unarmed loadout + `ScoutPatrol`, identical in spirit
  to Phase 2D's `CombatTestSceneBuilder.TestArchetype.ScoutPatrolOnly`, reused here for
  its own purpose).
  - [x] No win/lose consequences, no currency cost/reward — purely observational
  (distance closed, hit/miss, time-to-kill against a dummy). No `CombatManager` is
  spawned in this scene at all — confirmed `HUDController` already degrades
  gracefully with no `CombatManager.Instance` (its `DrawCombatResult` no-ops), so no
  HUD changes were needed. `TestRangeTelemetry` (new) is the purely observational
  reporter: discovers every `Team.Enemy` unit with a `Health` component at `Start()`
  (same scene-scan technique `CombatManager.Start()` already uses), and shows a
  live per-target distance/HP readout that flips to `DESTROYED — TTK Xs` once
  `Health.OnDestroyed` fires — no currency, no victory/defeat state anywhere in this
  scene.
- [x] Reuse `Phase1CombatSceneBuilder`'s patterns for constructing the test range scene;
  reuse `Phase0TestHarness`-style telemetry logging so test-range results are inspectable
  the same way Phase 0's validation was (this doubles as a fast manual/headless sanity
  check any time new parts are added in 2A/2B). `TestRangeSceneBuilder` reuses
  `Phase1CombatSceneBuilder`'s ground/light/camera/HUD/loadout-loading helpers exactly
  like `CombatTestSceneBuilder`/`Phase2EArenaBuilder` already did — genuinely almost no
  new scene-building code was needed. `TestRangeTelemetry`'s OnGUI panel + `Debug.Log`
  time-to-kill line matches `Phase0TestHarness`'s "log the numbers, plain overlay,
  nothing fancier" style rather than inventing a new report format.
- [x] `WorkshopController` needs a button/flow to enter test range with the currently
  previewed design, and a way to return to the Workshop afterward (mirroring
  `CombatManager`'s return-to-Workshop flow, but without the currency award). Added
  `WorkshopController.EnterTestRange()` (same design-readiness gate as Enter Combat,
  same `PlayerProgress` loadout-stashing — factored the previously-duplicated stashing
  logic out into a shared `StashCurrentLoadouts()` helper reused by both entry points),
  exposed via a new `TestRangeEntryOverlay` — a single OnGUI button in the Workshop
  scene (same "ugly art is fine" precedent as `ScenarioPickerOverlay`/`HUDController`;
  deliberately not a `Workshop.uxml` addition, for the same reasons `ScenarioPickerOverlay`
  gave). `TestRangeTelemetry.workshopSceneName` provides the "return to Workshop"
  button — no currency award anywhere on this path, unlike `CombatManager.DeclareResult`.

**Technical notes:** This is the cheapest sub-milestone to build once 2A/2B exist,
since it's almost entirely reuse of existing spawner/scene-building/telemetry code with
new scene content and a UI entry point — a good candidate to build early or interleaved
with 2A/2B rather than strictly last. **Confirmed true in practice**: this sub-milestone
was completed immediately after 2E (ahead of 2F, which was deferred) precisely because
2A/2B/2D/2E had already built every piece it needed (`VehicleFactory`,
`CombatPlayerLoadoutApplier`, `ScoutPatrol`, `Phase1CombatSceneBuilder`'s reusable
helpers, the `ScenarioPickerOverlay`-style OnGUI-overlay-in-Workshop pattern) — the
only genuinely new code was `TestRangeTelemetry` and a thin scene builder wiring
existing pieces together.

**Exit criteria:** Player can fire a design at a dummy target from the Workshop without
entering a real battle, and see basic hit/miss/timing feedback (✅ — verified
headlessly: `TestRangeSceneBuilder.BuildScene` builds `TestRange.unity` with no errors,
registers it in `EditorBuildSettings`, and a full 60-second headless Play-mode
regression against it completes with no exceptions; rebuilt the Workshop scene to
confirm `TestRangeEntryOverlay`'s wiring compiles/loads cleanly, and rebuilt both
Phase 2E arenas to confirm the `EnsureSceneInBuildSettings` refactor — pulled out of
`Phase2EArenaBuilder` into a shared `Phase1CombatSceneBuilder` helper once a third
scene builder needed the same "register in Build Settings" logic — didn't regress
anything).

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
**Goal:** Make the full spectrum (up to hypersonic/stealth CCA tier) playable, balanced, and
polished. Ordered priority for this phase, agreed ahead of implementation:
**(3A) one connected main-scene flow tying Menu/Workshop/Test Range/Combat together, pulled
first since it's mostly wiring existing pieces and gives every later sub-milestone a real
loop to test through instead of ad hoc overlays → (3B) visual fidelity of designs in
Workshop and live combat → (3C) tech tree as a real branching tree with meaningful
trade-offs → (3D) playtesting/feel pass once 3A–3C exist to test against.** 3E/3F (top-tier
content, art/audio, balance, campaign/tutorial, performance, QA) are the remaining
Phase 2-era backlog items and stay last — they're cheaper and more accurate to do once the
flow/designer/tech-tree foundations under them stop moving.

Each of 3A–3C should get the same headless-verification treatment every Phase 2
sub-milestone got (a validation script or regression run confirming the change works,
not just "looked right in the Editor once") — that discipline shouldn't lapse just because
3D is where the *live/manual* playtesting formally happens.

---

#### 3A — One Connected Flow: Menu → Workshop → Test Range → Combat
**Goal:** Today's scenes are independently bootstrapped and jumped between via ad hoc
overlays/menu items (`ScenarioPickerOverlay`, `TestRangeEntryOverlay`, per 2G's "ugly art
is fine" placeholders) rather than one coherent player-facing flow. Pulled to the front of
Phase 3 because it's mostly wiring over pieces that already work
(`WorkshopController.EnterTestRange`, `CombatManager`'s return flow) rather than new
rendering or data-model work — landing it first means every 3B/3C change gets tested
through one real loop instead of the current disconnected overlays, and it's also what
makes future scenario/campaign testing (3E, and the deferred Sandbox Campaign deep-dive)
easier to build against.
- [x] **Main Menu scene**: implemented as `MainMenu.unity`/`MainMenuController` (built
  via `MainMenuSceneBuilder`), registered at build index 0. "Design Craft" is wired to
  the Workshop; "Campaign"/"Skirmish" are shown disabled ("Coming Soon") since neither
  has anywhere to send the player yet — see class doc comment.
- [x] **In-UI target/scenario selection**: `ScenarioPickerOverlay`'s OnGUI panel is
  deleted; `WorkshopController` now owns `scenarioOptions` directly and renders one
  row per scenario (title + objective summary) inside `Workshop.uxml`'s
  `scenario-picker-content`, highlighting the current selection.
- [x] **Consistent return path**: every scene transition (`Enter Combat`, `Enter Test
  Range`, `Return to Workshop` from both Combat and Test Range, plus the new escape
  menu's "Return to Workshop") now funnels through the single `GameFlowController`
  API rather than each call site hardcoding its own `SceneManager.LoadScene`.
- [x] **Single persistent app-flow primitive**: implemented as `GameFlowController` —
  a static utility (not a `DontDestroyOnLoad` singleton; there's no cross-scene state
  to carry beyond what `PlayerProgress` already owns) plus `SceneNames` for the
  always-present scene name constants. `ResolveCombatScene` is the pure/testable
  fallback-resolution piece. Every scene remains independently openable (headless
  regression tests that jump straight into a combat scene still work).
- [x] **Headless verification**: `Phase3AValidation` (static checks: `ResolveCombatScene`
  fallback/override logic, every `SceneNames` scene file exists on disk, Build
  Settings registration incl. MainMenu at index 0) plus `Phase1WorkshopSmokeTest`
  (Play-mode crash check on the rebuilt Workshop scene) all pass. A live *manual*
  Menu → Workshop → Combat → (escape menu) → Workshop traversal was also exercised
  while diagnosing the win/lose-condition bug below and confirmed working (the
  `DontDestroyOnLoad`/`PlayerProgress` persistence was visible in the Hierarchy). A
  fully scripted multi-scene Play-mode traversal (rather than manual click-through)
  was not built — see `Phase3AValidation`'s own doc comment for why (Unity Editor
  Play-mode sessions aren't reliably chainable across separate scene loads from a
  single headless batch invocation) — this remains a gap if a stronger automated
  guarantee is wanted later.
- [x] **Bonus, found via live testing, not originally scoped**: fixed a real defeat-
  condition bug (`CombatManager` required every `Team.Player` unit including the
  unarmed scout escort to be destroyed, so losing the player-controlled drone while
  the scout survived left the match stuck in `InProgress` forever) and added a
  "Return to Workshop" button to the VICTORY/DEFEAT HUD banner plus an ESC-bound
  pause/escape menu (Resume / Return to Workshop / Quit Game, via
  `EscapeMenuController`) wired into every combat/Test Range scene through the shared
  `Phase1CombatSceneBuilder.BuildHud` helper.

**Exit criteria:** Starting from the Main Menu, a player can reach the Workshop, pick or
change what they're testing against entirely from in-scene UI, go test it, and return to
the Workshop — without ever needing an external menu, without any scene transition feeling
like a discontinuity in the flow.

---

#### 3B — Visual Fidelity: Designs Look Like What They Are
**Goal:** The single biggest gap right now is that the 3D model doesn't visibly react to
the loadout. A design with 5 missiles on hardpoints must show 5; a design with 3 must show
3; a camera-nosed variant must look different from a seeker-nosed one. This has to be true
identically in the Workshop preview and in live combat (both go through
`VehicleFactory`'s spawn pipeline, so this is one implementation, not two) — it's also the
precondition for 3C, since a tech-tree trade-off (e.g. "diesel = heavier, longer range")
isn't legible to the player if the model never changes to reflect it. Build the
mesh-swapping keyed generically off the existing part enums/definitions (not hand-authored
per today's specific part roster) so 3E's later top-tier content (hypersonic missiles,
stealth CCA drones) plugs into the same system instead of requiring it to be reworked.

- [x] **Hardpoint-count-driven missile visuals**: confirmed the pre-3B behavior actually
  was "one placeholder capsule regardless of loadout, never attached to a drone at all" —
  `VehicleFactory`/`DroneVisualBuilder` had no hardpoint-socket concept whatsoever.
  `DroneVisualBuilder.Build{Multirotor,FixedWing}Visual` now return hardpoint socket
  transforms sized to `DroneAirframeDefinition.hardpointCount`, and `VehicleFactory`'s new
  shared `BuildVisualAndMountedMissiles` mounts one `MissileVisualBuilder`-built missile
  per currently-loaded round (capped to hardpoint count). Went further than "show N at
  spawn": a new `WeaponController.OnFired` event + `MountedMissileVisuals` component
  removes one mounted visual per shot fired in real combat, so the rack visibly empties as
  ammo depletes, not just at spawn time.
- [x] **Sensor/seeker/camera nose swapping**: audit confirmed nose/seeker/sensor meshes
  didn't exist at all pre-3B (missiles were one undifferentiated capsule; drones had no
  nose concept whatsoever). New `MissileVisualBuilder` gives every one of the 9
  `SeekerType` values its own nose treatment (radome cones for SARH/ARH, dark glassy domes
  for Infrared/ImagingInfrared, a pale glass dome for Optical, an emissive red lens for
  Laser, a blunt nose + trailing wire-spool for WireOrDatalinkGuided, a combined
  radome+IR-window for MultiSpectral). `DroneVisualBuilder.BuildSensorPod` gives drones a
  nose pod shaped by whichever of `SensorSuiteDefinition`'s radar/EO-IR/ESM ranges
  dominates — an EO-IR-dominant suite (scout drones) gets an actual camera/gimbal-ball
  look, directly landing the original "camera on the front rather than a sensor" ask.
- [x] **Missile-on-missile visual composition**: external weapon bays
  (`WeaponBayDefinition.isInternal == false`) show mounted missiles on hardpoints;
  internal bays show nothing mounted at all (still tracked normally by `WeaponController`,
  just not rendered) — implemented directly in `BuildVisualAndMountedMissiles`.
- [x] **Fixed-wing/rotor silhouette differentiation**: audit confirmed wing type, hull
  material, and rotor material/size were all pure stats with zero visual effect pre-3B
  (only rotor *count* and the multirotor-vs-fixed-wing split changed anything). Now:
  `LiftSurfaceType.DeltaWing`/`VariableSweepWing` get a real tapered/swept wing mesh (hand-
  authored via new `PrimitiveMeshFactory.CreateTaperedWing`, since a cube fundamentally
  can't represent a triangular planform — `FixedWing` keeps the original straight cube);
  `RotorMaterial`/`RotorSize` change rotor blade scale and finish (Plastic/CarbonFiber/
  Metal); hull material changes finish via a new `TeamColorUtility.ApplyTeamColor`
  overload (metallic/smoothness/darkening per `HullMaterialType`) while preserving team-
  hue recognition.
- [x] **Real-time preview updates**: audit found there was no 3D preview in the Workshop
  at all pre-3B — `RefreshDesignPreview` only ever built text `Label`s, never a model. Built
  a real live 3D preview: a `WorkshopPreviewStage` (culled-by-layer preview camera +
  RenderTexture, wired by `Phase1WorkshopSceneBuilder`) displayed in a new
  `design-preview-viewport` `Image` element, rebuilt via `VehicleFactory.
  BuildVisualOnlyDrone` (the same visual-build path combat uses, guaranteeing "identical
  in Workshop and combat" structurally rather than by convention) on every
  `RefreshDesignPreview` call. Goes beyond the checklist item: mouse-drag rotates the
  model and scroll-wheel zooms (forwarded from the UI Toolkit `Image` element's pointer/
  wheel events to `WorkshopPreviewStage`), with slow auto-rotation resuming a couple
  seconds after a drag ends.
- [x] **Headless verification**: `Phase3BValidation` — all 9 `SeekerType` values produce a
  nose detail piece; hardpoint-mounted missile count matches `min(ammoCount,
  hardpointCount)`; internal weapon bays show zero mounted visuals; all three
  `LiftSurfaceType` wing variants build a `MainWing`; Titanium vs. RAM hull materials
  produce distinct finish materials; a null/incomplete loadout builds an empty preview
  without throwing. All checks pass, alongside a full re-run of every existing headless
  regression (`Phase1BatchRunner`, both Phase 2E arenas, `CombatTestSceneBuilder`,
  `Phase1WorkshopSmokeTest`, `Phase3AValidation`) with zero exceptions.

**Exit criteria:** Given two designs that differ only in missile count, seeker type, or
hull/rotor material, a player can tell them apart by looking at the model alone — in both
the Workshop preview and a live combat spawn — without reading any stat panel. Met: verified
both via `Phase3BValidation`'s headless assertions and by rebuilding every affected scene
and running the full regression suite clean.

**Follow-up pass (direct user feedback on the first cut):** the initial viewport/UI shipped
with real problems the checklist above didn't catch since it only asserted structure, not
actual layout/readability — fixed all of the following:
- **Layout overflow bug**: `design-preview-content`'s ~11-line text stat dump had no fixed
  height/scroll, so it silently overflowed past the panel boundary and visually overlapped
  the "Test Against" scenario picker and deploy buttons below it. Replaced with a compact
  `design-stat-card` (3-4 short lines) overlaid on the viewport instead of stacked below it.
- **Viewport was tiny**: was a fixed 220px box sharing a narrow 380px-wide column with the
  text dump. `#design-preview-panel` now `flex-grow: 1` (fills all remaining horizontal
  space after the Tech Tree/part-picker columns) and the viewport itself `flex-grow: 1` with
  a 420px floor — the dominant element on screen, not a small inset.
- **Button-row pickers → dropdowns**: `BuildPartSlotRow` (a wrapped row of option buttons
  per slot) replaced with `BuildPartSlotDropdown` (one `DropdownField` per slot) — didn't
  scale well once slots had many unlocked options, per user feedback.
- **Missile/Craft visual separation pulled forward from 3C**: added Craft/Missile mode tabs
  (`SetDesignerMode`) to the part-picker column instead of one long stacked "Missile
  Loadout" + "Drone Loadout" list — the live 3D preview still always shows the assembled
  strike drone regardless of active tab, so editing a missile part under the Missile tab
  visibly updates the mounted missiles on the same previewed craft.
- **Hull material appeared to do nothing**: root cause was lighting, not logic — the
  isolated preview stage had a single directional light and no skybox/reflection probe, so
  PBR metallic/smoothness differences (which read mostly from reflected environment light)
  were nearly invisible. Added a second fill light plus flat scene-wide ambient light
  (`RenderSettings.ambientMode`/`ambientLight`) to `Phase1WorkshopSceneBuilder`'s preview
  stage so the existing per-`HullMaterialType` finish differences actually show.
- **Fixed-wing/flying-wing drones looked like "toy planes"**: `BuildFixedWingVisual` only
  ever branched on `wingOrPropeller.liftSurfaceType`, never on
  `DroneAirframeDefinition.airframeClass` — every one of FixedWing/FlyingWingStealth/
  CcaScale got an identical fuselage-capsule + flat-slab-tailplane silhouette. Now each
  class gets a real distinct body: `FlyingWingStealth` has no separate fuselage/tail at all
  (the wing *is* the airframe); `CcaScale` gets a flat blended wing-body with no vertical
  tail (X-47B-style tailless UCAV); `FixedWing` keeps a fuselage but thinner/longer with a
  canted V-tail instead of a flat slab and longer/thinner wings (Predator/MALE-style,
  rather than a toy biplane).

Re-verified: `Phase3BValidation` (all checks still pass), plus the full regression suite
(`Phase1BatchRunner`, `Phase1WorkshopSmokeTest`, both Phase 2E arenas,
`CombatTestSceneBuilder`) with zero exceptions after this follow-up pass.

**Second follow-up pass (more direct user feedback, plus a user-suggested feature):**
- **Tech Tree didn't belong as a permanent column**: removed the always-visible
  `tech-tree-panel` column entirely; the tech tree is now a third "Research" tab
  sharing the same part-picker column/scroll as Craft and Missile
  (`DesignerMode.Research`). A real dedicated tech-tree graph view is still 3C's job
  (see 3C above) — this just gets it out of the designer's way for now, per feedback.
- **"Everything is blue"**: root cause was `TeamColorUtility`'s hull-finish materials
  multiplying each `HullMaterialType`'s finish by the *full* team color, so every
  hull's base color was still team-hue-dominated regardless of material. Rewrote to
  start from each material's own real-world base color (titanium/aluminum bare
  metal, RAM/carbon fiber dark, composite plastic light putty-grey) and blend in only
  a small team tint (`HullTeamTintWeight = 0.16`) for identification — plus a
  similar (slightly stronger) fix for the neutral gunmetal base missiles and other
  no-hull-material visuals use. Emissive glow strength reduced to match.
- **Fixed-wing drones were "laughably tiny"**: `BuildFixedWingVisual`'s defaults were
  tuned to a ~2m "aircraft" against real references like the MQ-1 Predator (~14.8m
  span/~8.2m length) and X-47B (~18.9m span/~11.6m length). Bumped defaults to 6m
  span / 4m fuselage length (scaled down from full military-UAV size to fit this
  game's smaller drone universe, but unmistakably aircraft-scale) — combined with
  each airframe class's own span/length multipliers, this yields ~6.6-7.8m span
  aircraft. Missile length also bumped moderately (full length now ~1.5-2.6m vs. the
  original ~1.1-2.0m) per the same "think about real dimensions" feedback, with an
  explicitly documented open tension noted in `MissileVisualBuilder`'s doc comment:
  the same missile size range is used for every carrier size (tiny quad up to full
  fixed-wing UCAV), which isn't fully solved here. The Workshop preview camera's
  default distance/zoom range and far clip plane were widened to match (was tuned
  for the old ~2m scale).
- **User-suggested feature, implemented**: while the Missile tab is active, the live
  3D preview now swaps from the full strike drone to a close-up of just the missile
  (auto-framed at a much closer default zoom, per `WorkshopPreviewStage`'s new
  per-subject framing defaults) instead of a tiny missile mounted on a much bigger
  aircraft. The existing auto-rotate-when-not-dragging behavior applies unchanged, so
  the missile slowly rotates for detail inspection exactly as suggested. Switching
  back to Craft/Research restores the full-drone view at its own default framing.
  Manual zoom/rotation is only reset when the *subject* changes (drone <-> missile),
  never on every keystroke while editing the same one.

Re-verified again: `Phase3BValidation` all pass, plus the full regression suite
(`Phase1BatchRunner`, `Phase1WorkshopSmokeTest`, both Phase 2E arenas,
`CombatTestSceneBuilder`) with zero exceptions after this second follow-up pass.

**Third follow-up pass**: the second pass's missile length increase (for realism)
immediately collided with the multirotor case — a ~1.5-2.6m missile mounted on a
~1.8m-diameter SmallQuad visually swallowed the entire drone (screenshot evidence:
what should have been a quadcopter rendered as two giant rods with a barely-visible
body between them). Fixed by making `DroneVisualBuilder.Build{Multirotor,FixedWing}Visual`
each compute and return a `missileMountScale` derived from *that carrier's own*
characteristic size (armLength*2 for multirotors, an equivalent nose-to-center proxy
for fixed-wing bodies), targeting mounted missiles at ~45% of the carrier's own size
rather than a flat 0.85 constant regardless of platform — a tiny quad now gets
proportionally tiny mounted missiles, a large fixed-wing aircraft gets proportionally
larger ones, matching how real aircraft ordnance always reads smaller than its
carrier. Does not fully resolve the underlying size-class tension (a small quad's
missile is still the same stats/model as a big aircraft's, just drawn smaller here) —
that remains an explicitly open note in `MissileVisualBuilder`'s doc comment.
Re-verified once more: `Phase3BValidation` all pass, full regression suite clean.

**Fourth follow-up pass**: still "bonkers" per further screenshot evidence — two more
real, distinct root causes found:
1. **Hardpoints sat almost exactly at the wingtip** (0.48-0.58 × wingSpan), so mounted
   missiles rendered as two detached-looking blobs way out past the aircraft's own
   silhouette rather than pylons attached to its body — nothing like a real Predator/
   Reaper's inboard-mounted Hellfires. Moved to 0.14-0.22 × wingSpan (well inboard,
   close to the fuselage/wing root).
2. **The wing/fuselage were too thin to read at the new larger scale**, especially
   from the preview camera's original low, near-level angle — a flying-wing/blended-
   wing-body silhouette with almost no vertical thickness viewed nearly edge-on just
   disappears into a line. Thickened every wing/fuselage cross-section (`BuildWing`
   gained a `thickness` parameter, previously hardcoded to 0.06 everywhere) and
   raised the Workshop preview camera's default angle from a near-level 1.2m/10m
   height/distance to a steeper 5m/12m, so the wing's top surface is actually visible
   instead of viewed edge-on.

Re-verified again: `Phase3BValidation` all pass, full regression suite
(`Phase1BatchRunner`, `Phase1WorkshopSmokeTest`, both Phase 2E arenas,
`CombatTestSceneBuilder`) clean.

---

#### 3C — Tech Tree: A Real Tree With Real Trade-offs
**Goal:** Today's tech tree (per 2A/2B's `SeedTechTreeNodes` notes) is a flat/linear list
gated by simple prerequisite chains, and the Workshop UI still shows it as a plain list
(per the Full UI/UX pass item below). This sub-milestone makes it a genuine tree — visually
branching, properly scaled — and audits every unlock so it delivers either a clear
advantage or an explicit trade-off that's *directly reflected in the resulting vehicle*,
not just a bigger number on a stat sheet.

- [ ] **Tech tree graph UI**: replace the linear tech-tree list in the Workshop with an
  actual branching node-graph view (per the Technology Stack section's original UI
  Toolkit "Tech tree graph" intent) — nodes positioned by tier/category with visible
  prerequisite edges, pan/zoom for scale as the tree grows across all of Phase 2's part
  categories, locked/unlocked/affordable visual states per node.
- [ ] **Trade-off audit pass** across every existing `TechNode`/part pairing — for each
  unlock, confirm (and where missing, add) a real, opposed stat consequence rather than
  a strict upgrade. Concrete examples already implied by existing data that should be
  double-checked end-to-end (stat exists **and** is legible to the player in the
  designer, not just present in `DesignStatsCalculator`'s internals):
  - Diesel (`Fuel_Diesel_Basic`) vs. Petrol vs. Electric: longer range/endurance,
    heavier and slower than battery-electric — confirm the resulting design's
    estimated range/top-speed actually diverge, not just its fuel-type label.
  - RCS-shaping / RAM hull (`Hull_RadarAbsorbentMaterial`): should measurably shrink
    an enemy's detection *and* lock range against this design (via
    `DetectableSignature`/`DetectionSensor`'s existing RCS-multiplier math per 2C) —
    confirm this is visible somewhere in the designer as an estimate, not only
    provable by reading source.
  - Titanium hull (`Hull_TitaniumAlloy`): lighter-for-its-armor-rating and higher
    `maxTemperatureCelsius` vs. cheaper/heavier alternatives — confirm the mass
    delta actually changes estimated TWR/range in the stat card, not just an armor
    number.
  - Heavy/high-fuel-fill missiles vs. small airframes: a missile loadout at high
    `fuelFillFraction` should be able to exceed a small drone's per-hardpoint mass
    budget or the airframe's MTOW headroom — confirm MTOW validation (2A/2B) already
    surfaces this as a real "too heavy for this airframe" block in the UI, not just a
    theoretical possibility.
  - Weather-driven choices: **scoped down to a single concrete instance** rather than
    an open-ended goal, since nothing in the current data model ties weather to part
    choice yet — pick one paired mechanic (e.g. IR/Imaging-IR seeker detection
    probability penalized in a cloud/rain weather state, RAM's RCS-multiplier
    advantage increasing further when enemy radar is already degraded by weather) and
    wire that one instance end-to-end. Anything beyond that single instance is
    explicitly deferred to 3E, not left as an open bullet here.
- [ ] **Visual/designer separation for missile vs. drone/plane design**: the Workshop
  currently shows a single scrolling panel with a "Missile Loadout" section and a
  "Drone Loadout" section stacked together (per 2A/2B). Split these into distinct
  designer views/tabs the player explicitly switches between (e.g. a mode toggle or
  two tabs at the top of the Workshop), each with its own 3D preview instance — not one
  shared preview trying to represent both an armed drone and its missile at once — so
  switching modes is an obvious, visible context change per the reference. **Reuse 3A's
  navigation/mode-switch primitive** for this rather than building a separate
  ad hoc toggle mechanism.
- [ ] **Immediate visual reflection tied to 3B**: since 3B already makes part swaps
  visible on the model, confirm tech-tree unlocks that change a *currently equipped*
  part's availability (e.g. unlocking a new engine tier) don't require leaving and
  re-entering the designer to see the option — or the model to update once selected.
- [ ] **Headless verification**: a validation script confirming the tech-tree graph
  data (nodes/edges/prerequisites) resolves correctly at scale, and that the
  trade-off audit's stat assertions above (e.g. RAM detection-range reduction,
  titanium mass-vs-armor delta) hold via `DesignStatsCalculator`/`DetectionSensor`
  checks — not just eyeballed once in the Editor.

**Exit criteria:** The tech tree renders as a real branching graph, not a list; picking
between two unlocked options for the same slot produces a genuinely different, player
legible vehicle (visually and in its estimated stats) rather than a strict upgrade; missile
and drone/plane design are two visually distinct designer contexts the player switches
between explicitly.

---

#### 3D — Feel & Usability Pass
**Goal:** Once 3A–3C exist, spend a dedicated pass actually playing the loop end-to-end to
find flight feel, control, and UX gaps that are hard to see from data/headless validation
alone (2B's own exit-criteria note already found real gaps — unconditional
`isThrusting = false`, no aerodynamic lift model — only via live playtesting after headless
checks passed clean). Treat this as an explicit checklist-building pass, not a fixed list
written ahead of time:
- [ ] Playtest fixed-wing/jet vs. multirotor control schemes across the full propulsion
  spectrum now that 3B's visuals and 3C's trade-offs make each design meaningfully
  different to fly, not just to look at. **Superseded for the flight-model half of this
  ask** — direct user feedback mid-3D was that fixed-wing/jet flight was simply broken
  (no real reason to stall, banking didn't correctly redirect lift, throttle was an
  ad-hoc bolt-on), not just "needs a feel pass." That's now its own sub-milestone, 3G
  below, which rebuilds the flight model itself before this item's original "playtest
  and log feel issues" scope applies to it.
  - [ ] Log every "feels off" / "hard to use" / "something's missing" observation as its
  own tracked item as it comes up, rather than guessing them in advance.
- [ ] Triage the resulting list into: fix now (3D), defer to 3E/3F (balance/content), or
  defer to Post-1.0.

**Exit criteria:** A punch-list of concrete flight-feel/UX issues exists, has been triaged,
and the "fix now" subset is closed out.

---

#### 3G — Fixed-Wing Flight Model Rework
**Goal:** Fixed-wing/jet drones fly correctly (thrust, lift, and maneuvering behave like a
real aircraft, including a genuine stall and coordinated banked turns) instead of the thin,
physically-incorrect model 2B originally shipped, and the Workshop lets a player pick
"Multirotor" vs. "Fixed-Wing" as a real filter on which parts they're offered — without
touching the tech tree topology or introducing a second, parallel physics/part pipeline.

**Context — why this needed a rework, not a tuning pass:** the original fixed-wing model
(`FlightBody.useAerodynamicLift`, added in 2B) computed lift as a flat
`liftCoefficient * speed^2` along `transform.up`, with no angle-of-attack term at all. That
meant: (1) no real stall — a design was airborne at a given speed regardless of how it was
pointed, since attitude never entered the lift calculation; (2) banking didn't correctly
redirect lift relative to the actual direction of travel (it used the raw local `up` axis,
not `up` projected against the real relative airflow), which mostly happened to work by
coincidence at small angles but wasn't the real relationship; (3) the player's Space/Shift
throttle was an ad-hoc *extra* force stacked on top of a constant baseline thrust, not a
real throttle lever — idling back never actually reduced thrust to near-zero. None of this
was a balance/tuning problem; the underlying force model was wrong, which is exactly why
this is a rework, not a 3D-style feel pass.

- [x] **Angle-of-attack-driven lift + stall model**: `FlightBody.ComputeAngleOfAttackDegrees`
  (pure function: signed angle between the nose and the actual velocity vector, in the
  pitch plane, sideslip excluded) and `FlightBody.ComputeLiftFactor` (a lift-curve lookup —
  1.0 at the wing's tuned `referenceAoADegrees`, rising toward `criticalAoADegrees`, then
  collapsing to a ~35% post-stall plateau over the next 10 degrees, mirrored onto the
  negative side around `zeroLiftAoADegrees`) replace the flat `speed^2` formula. Lift now
  acts along `transform.up` projected perpendicular to the actual velocity vector (not the
  raw local axis), so banking genuinely redirects lift — the real mechanism behind
  "turn by banking, not by yawing." `WingOrPropellerDefinition` gained
  `zeroLiftAoADegrees`/`referenceAoADegrees`/`criticalAoADegrees`/`inducedDragFactor` (new
  fields, additive — existing Propeller-type rotor assets are unaffected since
  `useAerodynamicLift` is never true for multirotors); the three existing fixed-wing wing
  assets (`Wing_FixedWing`/`Wing_DeltaWing`/`Wing_VariableSweepWing`) were re-tuned rather
  than replaced, keeping their stable ids/tech-tree wiring — Delta Wing's much higher
  `criticalAoADegrees` (28 vs. FixedWing's 15) is what "far more maneuverable" now
  concretely means under a real AoA model, not just a `turnRateDegreesPerSecond` number.
  Verified headlessly via `Phase3GFixedWingValidation` (lift-curve shape, AoA sign
  convention against a known pitched attitude, trim-level-flight equality, insufficient
  lift at half cruise speed even at max AoA, and a banked-lift force-vector decomposition
  — all PASS).
- [x] **Real throttle lever**: `FlightBody.throttleFraction` (0-1, defaults to 1 so
  AI/missile-style bodies that never touch it are unaffected) scales `thrustNewtons`
  directly; `PlayerDroneController`'s Space/Shift now ramp this lever up/down over time
  (`throttleChangeRatePerSecond`) instead of adding an ad-hoc extra force on top of a
  constant baseline thrust.
- [x] **Control authority scales with airspeed**: `PlayerDroneController.
  ComputeControlAuthority` (pure function, speed^2/referenceSpeed^2, clamped 0-1) scales
  roll/pitch rate — a slow or near-stalled fixed-wing design is now genuinely sluggish to
  steer, not just slow to look at, matching real control-surface effectiveness depending on
  airflow. Verified headlessly (0 at zero speed, 1 at/above the reference speed, correctly
  between the two at half-reference).
- [x] **"Little flying rectangle" prototype rig**: `Phase3GFixedWingPrototypeSceneBuilder`
  (`Vanquish/Phase 3G/Build Fixed-Wing Prototype Scene`) builds a standalone
  `FixedWingPrototype.unity` — a single stretched, brightly-colored cube with `FlightBody`
  configured directly in aerodynamic-lift mode (hand-tuned numbers derived from "cruise at
  25 m/s at the wing's referenceAoA," not from any seeded part asset) and a live
  `PlayerDroneController`, plus `FixedWingPrototypeTelemetry` (an OnGUI airspeed/altitude/
  AoA/throttle overlay, same "ugly art is fine, log the numbers" precedent as
  `Phase0TestHarness`/`TestRangeTelemetry`) — deliberately bypassing
  `DroneLoadout`/`DesignStatsCalculator`/`VehicleFactory` entirely, so thrust/lift/
  maneuvering could be validated and felt in isolation before any real fixed-wing craft
  content depended on the model being right, per the plan's own request. Disposable/
  unwired into the Workshop-Combat flow by design — open the scene and press Play.
- [x] **Headless kinematic validation of the whole model**: `Phase3GFixedWingValidation`
  (`Vanquish/Phase 3G/Validate Fixed-Wing Flight Model (Headless)`) mirrors
  `Phase2CValidation`'s guidance-law kinematic-simulator pattern — a plain C# loop calling
  `FlightBody`'s and `PlayerDroneController`'s actual production static functions (not a
  parallel reimplementation) at 50Hz for a simulated banked-turn-with-back-pressure
  maneuver. Confirmed: heading yaw changes by >20° purely as an emergent result of banking
  (there is still no direct yaw control anywhere in this flight model — matches the
  existing "turn by banking" design decision from 2B's own exit-criteria writeup), the
  velocity vector tracks the nose to within ~1.5° (a coordinated turn, not a skid, thanks
  to `alignVelocityToForward`), and altitude holds within ~3m of the start over the
  5-second maneuver. All 8 sub-checks (lift curve shape, AoA sign, trim flight, low-speed
  stall, banked-lift decomposition, control authority scaling, the coordinated-turn
  simulation, and `DroneCompatibility` mismatch detection below) PASS.
- [x] **Part-compatibility validation** (a real gap found while scoping this rework —
  nothing previously stopped equipping a jet engine on a quadcopter airframe):
  `DroneCompatibility` (`Vanquish/Data/Drones/`) maps each of Airframe/WingOrPropeller/
  Propulsion/Engine to a `FlightConfiguration` (`Multirotor`/`FixedWing`) from the field
  that already drives simulation behavior (`rotorCount`, `liftSurfaceType`,
  `requiresForwardFlight`) — deliberately not a duplicated field on every part, to avoid a
  second source of truth. `DroneEngineDefinition` gained its own
  `requiresForwardFlight` bool (the one part type that previously had no field implying a
  flight model at all) mirroring `PropulsionDefinition`'s own flag; set `true` on
  `Engine_Jet_Subsonic`/`Engine_Jet_Supersonic`. `DesignStatsCalculator` now computes
  `isFlightConfigurationCompatible`/`flightConfigurationMismatchReason` on
  `DroneRuntimeStats`, and `WorkshopController` gates Enter Combat readiness on it exactly
  like the existing MTOW check, surfacing the specific mismatched slot if one exists.
- [x] **Workshop "Airframe Type" toggle** (the actual "toggleable option in the workshop to
  choose parts" ask — tech tree and physics pipeline unchanged, confirmed with the user
  before implementing): a two-way Multirotor/Fixed-Wing segmented toggle
  (`WorkshopController.BuildAirframeTypeToggleRow`, reusing the existing
  `designer-mode-tabs` USS classes rather than new UI) sits above the Craft tab's part
  list and filters the Propulsion/Airframe/Wing-or-Rotor/Engine dropdowns to
  `DroneCompatibility`-compatible options for the selected side via
  `WorkshopController.FilterByFlightConfig`; Hull Material/Fuel/Weapon Bay/Countermeasure
  stay unfiltered since they're flight-model-agnostic. `ResolveSelection` now also checks
  array membership (not just still-unlocked) so flipping the toggle away from a
  currently-selected part correctly falls back to the first compatible unlocked option
  instead of keeping an now-hidden-but-still-unlocked selection. This is purely a Workshop
  UI filter — no new `DroneLoadout` field, no new TechNode, no parallel part/workshop
  system, exactly as scoped.
- [x] **Regression-checked against the rest of the pipeline**: re-ran
  `Phase2BValidation.ValidateDroneBreadthAssets`/`ValidateDroneBreadthTechWiring`/
  `ValidateTier0DroneMtow` (all PASS — the electric-quadcopter Tier-0 loop is byte-for-byte
  unaffected) and `Phase3BValidation.ValidateVisualFidelity` (all PASS — `DroneVisualBuilder`
  still builds correct fixed-wing silhouettes/wing planforms against the re-tuned wing
  assets), rebuilt `Workshop.unity` with no missing-asset errors, and ran a full 60-second
  `Phase1BatchRunner` headless Play-mode combat regression against `Combat_Arena01` with
  zero exceptions/`NullReferenceException`/`MissingReferenceException` — confirming this
  rework didn't regress the already-working electric-quadcopter MVP loop.

**Technical notes:** Deliberately did NOT introduce a separate `FixedWingFlightBody`
class/component or a formalized `IAerodynamicBody` interface (both considered while scoping
this work) — `FlightBody` is `[RequireComponent]`-depended-on by `DroneCombatAI`,
`AltitudeController`, `GuidanceController`, `ScoutPatrol`, and `PlayerDroneController`, all
via the concrete type; splitting it into two classes would have meant either duplicating
that dependency surface or a much larger interface-extraction refactor across all five,
which is a bigger and different piece of work than "make the existing fixed-wing branch
physically correct." One shared `FlightBody` with a real aerodynamic mode for fixed-wing
(vs. the missile/multirotor mode) matches how the class already worked and keeps every
existing AI/guidance dependency unchanged. No atmospheric/altitude-density model was added
(air density is the constant sea-level value throughout, same simplification the rest of
the codebase already carries per PLAN.md's own Atmospheric Model aspiration, still
unimplemented) — noted as a still-open future-phase item, not something this rework
silently regressed.

**Exit criteria:** A player can fly a fixed-wing design that genuinely stalls at low
speed/high AoA, turns via banking rather than skidding, and responds to a real throttle
lever (✅ — validated both by the standalone prototype rig, playable directly, and by the
headless kinematic simulation above); the Workshop lets a player filter parts by airframe
type without any change to the tech tree or the underlying simulation pipeline (✅); a
design combining incompatible flight-model parts is flagged rather than silently accepted
(✅ — `DroneCompatibility`, verified headlessly against a real seeded-asset mismatch case).

---

#### 3H — Planform Presets
**Goal:** Merge the separate Airframe and Wing slots into curated, real-world-referenced
"Planform" presets — one per reference silhouette supplied by the user (Northrop Grumman
X-47B, Anduril YFQ-44A Fury, General Atomics Gambit, plus a "Brontanax" fan-art cutaway
used for internal-layout/tail-control-surface reference) — with believable real-world
scale, correctly-proportioned mounted munitions, and a neutral military color scheme
instead of the previous saturated team-color tint.

- [x] **Merged Airframe+Wing "Planform" picker**: new `DronePlanformDefinition` (a plain
  ScriptableObject, not a `PartDefinition` — see its own doc comment for why: a preset has
  no independent cost/tier, it's a named pointer at an already-tech-gated airframe+wing
  pair) pairs one `DroneAirframeDefinition` with one `WingOrPropellerDefinition`.
  `WorkshopController`'s Fixed-Wing toggle branch now shows one "Planform" dropdown
  (`BuildPlanformSlotDropdown`) instead of separate Airframe/Wing-or-Rotor dropdowns;
  selecting a planform sets both `_selectedDroneAirframe`/`_selectedDroneWing` internally,
  so `TryBuildDroneLoadout`/`DesignStatsCalculator`/`VehicleFactory` need zero changes —
  none of them are aware a merged picker exists. Multirotor mode is unaffected (still two
  independent Airframe/Wing-or-Rotor dropdowns), since a rotor is a genuinely separable
  accessory the way a wing planform isn't.
- [x] **Three curated planforms, one per reference silhouette**, tied into the tech tree
  as a single merged purchase each (unlocking both the airframe and wing together, not
  two separate purchases):
  - **Twin-Tail Fighter Planform** (Fury/YFQ-44A/"Brontanax"-class): `Airframe_FixedWing`
    (reused, retuned) + `Wing_DeltaWing` (reused, retuned in the fixed-wing flight-model
    rework). `DroneVisualBuilder.BuildConventionalFuselageAndWing` rebuilt: a flattened,
    chined-look fuselage (a stretched cube, not the previous round capsule — a capsule
    can't read as "chined"), a pointed nose cone (`PrimitiveMeshFactory.CreateCone`,
    previously only used for missile noses), wings repositioned closer to mid-fuselage,
    and a canted twin tail (generalized from the old single-purpose `BuildVTail` into
    `BuildTwinCantedTails`, shared with the recon planform below at different
    proportions).
  - **Cranked-Kite Recon Planform** (Gambit-class): `Airframe_CcaScale` (reused, retuned) +
    `Wing_VariableSweepWing` (reused). `BuildBlendedWingBody` gained a pair of small
    outward-canted tails (`BuildTwinCantedTails`) — the reference Gambit variants clearly
    have twin tails, unlike the fully tailless treatment this body style got in the
    original fixed-wing visual pass, which conflated every tailless-*looking* UCAV into
    one silhouette.
  - **Flying-Wing Stealth Planform** (X-47B-class): `Airframe_FlyingWingStealth` (reused,
    retuned) + new `Wing_FlyingWingKite`. New `LiftSurfaceType.FlyingWing` enum value
    (appended) and new `PrimitiveMeshFactory.CreateKiteWing` — a hexagonal
    cranked/double-delta planform mesh (leading edge sweeps at one angle root-to-crank,
    a different steeper angle crank-to-tip) instead of the plain single-sweep triangle
    `CreateTaperedWing` already covered, matching the X-47B's distinctive
    "broad-shouldered" kite silhouette. `AddWingHalf`'s fan-triangulation was generalized
    from a hardcoded quad to an arbitrary convex N-gon (verified equivalent for the
    quad case first) so one function serves both the delta/swept quad and the new
    hexagonal kite. `BuildFlyingWingBody` also gained a low, broad dorsal hump standing
    in for the real X-47B's distinctive top-mounted engine air intake — the single most
    recognisable non-wing detail on the real aircraft. Remains fully tailless, unlike the
    other two.
  Retired the six individual per-airframe/per-wing TechNodes the fixed-wing-flight-model
  rework (3G) had created (`TN_2B_drone_airframe_fixedwing/flyingwingstealth/ccascale`,
  `TN_2B_drone_wing_fixedwing/deltawing/variablesweepwing`) in favor of three merged
  `TN_3H_planform_*` nodes, and deleted the now-fully-unused `Wing_FixedWing.asset` (the
  plain straight wing wasn't chosen for any of the three curated planforms) rather than
  leaving it as orphaned, unreachable content. `Phase2BDroneBreadthSeeder` was updated to
  stop re-creating any of the retired content if re-run.
- [x] **Believable, real-world-referenced scale**: `DroneAirframeDefinition` gained
  `wingSpanMeters`/`fuselageLengthMeters` (visual-only — does not touch mass/drag/MTOW,
  which stay within this game's existing Tier balance envelope). Fury's dimensions are
  the real disclosed figures (17ft/20ft → 5.2m/6.1m); X-47B's are the real dimensions
  scaled by ~0.74x (18.92m/11.63m → 14m/9m — still unmistakably the largest of the three,
  matching reality, without making the in-game aircraft absurdly large relative to
  existing ~600-1200m arenas); Gambit's are an estimate between the other two (General
  Atomics hasn't published exact figures). `DroneVisualBuilder.BuildFixedWingVisual` now
  reads these from the design's own airframe instead of one flat 6m/4m constant every
  fixed-wing design previously shared. Verified headlessly (`Phase3HValidation.
  ValidateSizeOrdering`): Fighter (5.2m) < Recon (9.5m) < Stealth (14m), matching the
  real Fury < Gambit-estimate < X-47B ordering.
- [x] **Correctly-scaled mounted munitions**: `DroneVisualBuilder.ComputeMissileMountScale`'s
  upper clamp was 1x a nominal ~2m missile length — fine for the old ~6-8m fixed-wing
  bodies, but a real Fury-class fighter (~6.1m) carries AIM-120s (~3.7m) at roughly 60% of
  its own body length, which the old clamp couldn't reach. Raised to 2.2x (low-end 0.15x
  clamp protecting tiny multirotors unchanged). Verified headlessly
  (`Phase3HValidation.ValidateMissileMountScaleIsBelievable`): the Twin-Tail Fighter
  planform mounts a ~2.7m missile against its 6.1m fuselage (44% ratio) — comfortably in
  the believable 25%-100% AIM-120-vs-Fury-like range, not clamped to a comparatively tiny
  fixed length regardless of carrier size.
- [x] **Neutral military color scheme, not a "blue rinse"**: direct user feedback that
  real reference aircraft (X-47B, Fury, Gambit) are neutral greys with only small
  national-insignia-sized color accents, not a fully tinted airframe. `TeamColorUtility.
  PlayerColor`/`EnemyColor` desaturated from a saturated cyan-blue/pure-red toward muted
  low-visibility roundel tones; `HullTeamTintWeight` cut further (0.16 → 0.08); the hull
  finish's emissive glow contribution was removed entirely (a matte military airframe
  doesn't glow — the small emissive tint was part of what read as an overall blue rinse
  across the whole hull). Missiles (which have no hull material, and genuinely benefit
  from being spottable at range) keep a modest emissive tint, just reduced.

**Technical notes:** Deliberately did not introduce a fourth, generic "mix your own"
fixed-wing path alongside the three curated planforms — every asset (`Wing_DeltaWing`,
`Wing_VariableSweepWing`, the retuned airframes) still exists as real, independently
inspectable data, so a future planform can still reuse them in a new pairing, but the
Workshop only ever *offers* the three deliberately-designed combinations. The user's own
"we will probably add more later" is exactly why `DronePlanformDefinition` was kept as a
thin preset pointer rather than folding the wing's stats directly onto
`DroneAirframeDefinition` — adding a fourth planform is one new preset asset plus one
TechNode, not a data-model change. `PrimitiveMeshFactory.CreateKiteWing`'s crank point,
sweep angles, and chord ratios were hand-tuned by eye against the reference images rather
than measured from real X-47B CAD data (no such data is publicly available in a form this
project could consume) — "as close as possible" within the constraints of a
procedural-primitives-only art pipeline, not a scanned/traced reproduction.

**Visual-polish follow-up (direct user feedback with screenshots: "they all look super
janky"):** built `Phase3HScreenshotTool` (`Vanquish/Phase 3H/Render Planform Screenshots
(Debug)` and `.../Dump Planform Part Transforms (Debug)`) to actually render each
planform headlessly to a PNG and dump every part's world-space transform/bounds, rather
than reasoning about the mesh math blind — this caught three real bugs the Workshop
screenshots alone didn't make obvious the cause of:
1. `BuildTwinCantedTails`'s two fins shared the exact same position (only their
   *rotation* differed) — a real single-root V-tail configuration, but oversized
   (up to 18% of fuselage length) it read as a giant X slapped across the fuselage, and
   none of the three reference aircraft actually use a single-root V-tail anyway (Fury/
   Gambit/"Brontanax" all show two separately-rooted fins). Fixed: fins now get a real
   lateral offset before being canted, and are roughly half the previous size.
2. `VariableSweepWing`'s and the new `FlyingWing` kite's sweep-back distances were
   computed as multiples of root chord — dimensionally the wrong basis (sweep is a
   function of how far you travel *spanwise*, not chord depth) — which blew the
   Cranked-Kite Recon wing out to ~9.6m of depth against an intended ~8m fuselage, and
   the Flying-Wing Stealth kite out to ~16m against an intended ~9m. Retuned both to
   scale sweep off actual span-segment distance instead; measured full-model bounds
   after the fix landed within ~10% of each airframe's own fuselageLength.
3. `WorkshopPreviewStage`'s fixed 12m/24m framing distance/max-zoom (tuned for the
   pre-planform-preset ~6-8m aircraft) put the camera uncomfortably close to/inside the
   new largest planform (~14m span) — raised to 20m/40m (`Phase1WorkshopSceneBuilder`'s
   matching `PreviewCamera` rig updated to stay in sync).
Also nudged the sensor pod off the conventional-fuselage nose cone's exact apex (was
z-fighting/overlapping it) and scaled the missile hardpoint's vertical/longitudinal
offset with fuselageLength instead of a flat constant (mounted missiles were clipping
into the now-much-larger fuselage bodies). Re-verified via
`Phase3HScreenshotTool.RenderAll` (visually inspected) and the full
`Phase3HValidation`/`Phase3BValidation`/60-second combat regression suite (all still
PASS) after every change.

**Second visual-polish round (direct user feedback with fresh Workshop screenshots after
the first round: "still not ideal" — cranked-kite mounted two missiles on the same side,
flying-wing showed "a mysterious flying secondary structure underneath," twin-tail's body
looked like "a cereal box with wings"):**
- **Same-side missiles**: `CreateHardpointSockets` laid hardpoints out strictly
  left-to-right, and `VehicleFactory` always mounts `hardpoints[0..ammoCount)` when a
  design carries fewer missiles than it has hardpoint sockets — so a 6-hardpoint CCA-scale
  airframe carrying 2 missiles mounted both of them on hardpoints 0 and 1, the two
  *leftmost* sockets. Reordered the hardpoint array center-out, alternating sides
  (innermost pair first), so any prefix of the array is bilaterally symmetric regardless
  of how many hardpoints are actually filled. Verified via `Phase3HScreenshotTool`'s
  transform dump: the Cranked-Kite Recon's two mounted missiles now sit at x=-0.30 and
  x=+0.30 (were both around x=-1.5/-0.9, same side).
- **"Mysterious floating structure"**: hardpoint sockets were bare empty transforms with
  no pylon/rack geometry connecting a mounted missile to the airframe at all — barely
  noticeable on the fighter (which has a fuselage/wing nearby to visually anchor it) but
  glaring on the flying wing, where a ~4m missile hung in open space below a thin wing
  with nothing visibly attaching it. New `DroneVisualBuilder.BuildPylon` adds a small
  vertical strut bridging the body surface (local Y=0) down to each mounted missile,
  built by `VehicleFactory` alongside each missile visual (both multirotor and fixed-wing
  hardpoints benefit, not just the three planforms).
- **"Cereal box" fuselage**: `BuildConventionalFuselageAndWing`'s body was a single
  uniform-cross-section stretched cube. Split into two segments — a wider forward
  body and a narrower aft boom — so the silhouette actually necks down toward the tail
  like a real fighter fuselage, instead of reading as one flat-sided brick with a nose
  cone glued to the front. (Deliberately still two boxes, not a fully lofted mesh — see
  this sub-phase's earlier "as close as possible within a procedural-primitives-only
  pipeline" framing.)
Re-verified via `Phase3HScreenshotTool.RenderAll` (re-rendered and visually inspected all
three planforms) and the full `Phase3HValidation`/`Phase3BValidation`/60-second combat
regression suite (all still PASS) after every change.

---

#### 3J — Part Depth Pass (missiles, propulsion, weapon bays, sensors)
**Goal:** A long list of direct user feedback that changing parts "doesn't make much
difference" — missile fuel not affecting range, a missile that always hits, "too heavy to
ever be on a missile", engine type not affecting maneuverability, Propulsion/Engine being
functionally the same slot, RCS not shown, tiny masses, planform not affecting flight, no
radar sensor option, fuel type only affecting mass, no throttle readout, a tiny sandbox
terrain, a weapon bay that barely does anything, no scaling of ammo capacity to craft size,
no internal-bay-first-then-pylon-overflow, no multi-missile-in-flight, and no visible
countermeasure effect. Addressed each with a real, load-bearing mechanic rather than a
cosmetic tweak, verified via `Phase3IValidation`/re-run `Phase2A`/`Phase2B`/`Phase3H`/
`Phase3G` validation suites (all PASS) plus a 60-second headless Play-mode regression that
happened to exercise both `Combat_Arena01` and a live `Workshop` scene transition (zero
exceptions).

- [x] **Missile fuel now genuinely limits range**: new `MissileBurnController` cuts thrust
  once `MissileEngineDefinition.burnTimeSeconds * fuelFillFraction` elapses — before this,
  `VehicleFactory` set `isThrusting = true` once at spawn and nothing ever turned it back
  off, so fuel fill only ever changed mass. A half-full tank now genuinely reaches less far.
- [x] **The "basic missile always hits" is fixed**: `GuidanceController` now gates
  correction on the seeker's own `detectionRangeMeters`/`fieldOfViewDegrees` (previously
  computed but never actually consulted — the terminal guidance law ran unconditionally
  regardless of range). A target that maneuvers outside the seeker's cone, especially near
  max range, now genuinely breaks the shot.
- [x] **Engine type affects maneuverability**: new `MissileEngineDefinition.
  maneuverabilityMultiplier` scales the airframe's `maxGForce` — a Solid Rocket's short
  violent boost (1.15x) now out-turns a Scramjet's sustained-cruise airframe (0.65x) on the
  same airframe, not just flies faster/further.
- [x] **Missile airframes**: was a single `Airframe_Basic` (40kg MTOW) for every tier —
  new Interceptor/Heavy Strike/Hypersonic tiers (55/78/95kg MTOW) so heavier Tier2-4 combos
  (e.g. Scramjet + Cluster + Multi-Spectral, ~46kg with zero optional modules) are actually
  buildable. Also promoted from a hardcoded single field to a real Workshop dropdown — it
  was never player-selectable before this pass at all.
- [x] **Countermeasures are now visible and seeker-quality-dependent**: a successful decoy
  now spawns a visible flare burst (`CountermeasureVisualEffect`) instead of only a log
  line, and the decoy's success roll is weighted by the inbound missile's own
  `SeekerDefinition.countermeasureSusceptibility` (previously seeded but never read) — a
  Multi-Spectral seeker (0.1) now genuinely resists the same flare a basic IR seeker (0.7)
  falls for.
- [x] **Propulsion+Engine merged**: new `DronePropulsionPackageDefinition` (same preset
  pattern as the Planform merge) pairs one Propulsion with one Engine as a single
  "Propulsion" dropdown — research confirmed the two slots substantially duplicated each
  other (mass and IR signature both double-counted, thrust from the engine alone,
  `requiresForwardFlight` only load-bearing from the propulsion side). Retired the 3 pairs
  of individually-unlockable ICE/Jet-Subsonic/Jet-Supersonic TechNodes in favor of 3 merged
  package nodes; Electric's Tier-0 unlock path (bundled with other starter parts) is
  unchanged.
- [x] **RCS is now shown**: `radarCrossSection` was computed but never surfaced in the
  Workshop — now on the Missile/Strike Drone/Scout Drone stat lines.
- [x] **Fuel/propulsion compatibility enforced**: `DroneCompatibility.IsFuelCompatible`
  checks the fuel part's `FuelType` against what the propulsion actually needs (Electric
  needs Battery, ICE needs Petrol/Diesel, any Jet needs Jet Fuel) — a battery-powered
  supersonic jet is now flagged the same way a flight-configuration mismatch already was,
  gating "Enter Combat" the same way.
- [x] **Real radar sensor options**: only `Sensor_Basic` (1500m) and `Sensor_Scout` (4000m)
  ever existed, and the strike drone's own sensor was hardcoded to `Sensor_Basic`
  regardless of what was unlocked (no Sensor dropdown existed in the Workshop at all).
  Added `Sensor_Radar_Advanced` (6000m) and `Sensor_Radar_LongRange` (10000m — beyond even
  the longest seeded seeker, 9000m) plus a real Sensor dropdown, so a drone's own detection
  isn't always the bottleneck against a well-equipped inbound missile.
- [x] **Throttle indicator**: `HUDController`'s flight panel now shows throttle % (reads
  `FlightBody.throttleFraction`) for fixed-wing designs — there was no power-setting
  readout anywhere before this.
- [x] **Much bigger sandbox terrain**: the ground plane (`Phase1CombatSceneBuilder.
  BuildGround`, shared by the Test Range and every combat scene) was a fixed 600x600m
  Plane — seeded sensor/seeker ranges already reach up to 10000m and the camera's far clip
  is 12000m. Scaled to ~20000x20000m — bigger than the far clip in every direction, so the
  edge is never visible regardless of engagement range, reading as effectively infinite
  without a real streaming/tiled terrain system.
- [x] **Weapon bay capacity is real, and smaller craft carry fewer missiles**:
  `WeaponBayDefinition.maxMunitionCount`/`payloadCapacityKg` were seeded but never read —
  `DroneRuntimeStats.effectiveAmmoCount` now clamps `DroneLoadout.ammoCount` to the bay's
  real capacity (used for actual `WeaponController.ammoRemaining`, not just a visual
  hardpoint cap). `WeaponBay_Small`'s capacity was cut from 4 to 2 (a Tier-0 starter bay
  shouldn't match its own Tier-1 upgrade). Ammo count is also now a real Workshop slider
  instead of a hardcoded 4.
- [x] **Internal bay used first, then pylon overflow (affecting RCS)**: new
  `WeaponBayDefinition.internalCapacity` splits a bay's `maxMunitionCount` into a hidden
  internal portion (zero RCS contribution, no visible mesh) and an external overflow
  portion (visible, adds RCS) — `WeaponBay_InternalMedium` is now a genuine mixed bay (4
  internal + 2 external) rather than the old all-or-nothing `isInternal` flag. Each
  externally-mounted missile now adds a fraction of its own RCS to the carrier's exposed
  signature (`DesignStatsCalculator`), computed and rendered by `VehicleFactory`/
  `DroneVisualBuilder` identically between the Workshop preview and live combat.
- [x] **Multiple missiles in flight, gated by seeker tech**: `WeaponController.
  maxConcurrentInFlight` (derived from the missile's seeker type — fire-and-forget
  seekers like Active Radar/Imaging IR/Multi-Spectral allow up to 4 concurrent; wire/SARH/
  laser, which need the launcher's continuous guidance/illumination, allow only 1) now
  actually caps concurrent missiles — before this, nothing capped it at all beyond
  ammo/cooldown. New `MissileLifecycleNotifier` frees a slot when a missile is destroyed.

**Deliberately deferred, not attempted this pass:** a full real-world mass rebalance
("masses are extremely small") and "planform doesn't affect how the aircraft flies" beyond
what 3H/3G already wired (liftCoefficient/dragCoefficient/turnRate already flow from the
wing into flight stats — a deeper pass tying wing *shape* more distinctly into handling
would mean re-deriving the validated stall/lift-curve tuning from 3G, which carries real
regression risk against that already-hard-won, headlessly-verified flight model for a
payoff that's mostly about degree, not kind). Flagged as a follow-up rather than risked in
the same pass as everything above.

**Exit criteria:** Three visually distinct, real-world-scale fixed-wing planforms exist,
each tied into the tech tree as a single purchase (✅ — verified headlessly via
`Phase3HValidation`: presets load with the expected airframe/wing pairing, TechNodes
unlock both parts together with a sane prerequisite chain, and `VehicleFactory`/
`DroneVisualBuilder` build a working visual for each without throwing); mounted munitions
read as a believable fraction of the carrier's own body length instead of a fixed size
regardless of aircraft scale (✅); the airframe's color scheme reads as neutral military
grey with team color as a small accent, not a dominant tint (✅ — `TeamColorUtility`
retuned; no automated visual-color assertion beyond the emissive-material check
`Phase3BValidation` already had, since actual on-screen color perception isn't something
a headless test can meaningfully assert beyond "the material values changed as intended").

---

#### 3E — Content, Balance & Campaign Completion
*(Carried over from the original Phase 3 scope — sequenced after 3A–3D since content/balance
work is far cheaper to do once the designer, tech tree, and flow it's built against have
stopped changing shape.)*
- [ ] Top-tier content: stealth CCA-style drones, hypersonic air-to-air missiles
- [ ] AI scaling — CPU tech/behavior escalates alongside player progression
- [ ] Art/audio pass: real models for parts (or modular part meshes), VFX for
  engines/explosions/countermeasures, SFX, music
- [ ] Balance pass across all tiers (part stats, tech costs, mission difficulty curve)
- [ ] Campaign/mission structure (progression of scenarios, not just skirmish) — depends
  on the still-outstanding Pre-Phase-3 Sandbox Campaign design deep-dive above, which
  remains a prerequisite for the overworld-map implementation specifically, independent
  of 3A–3D's reordering.
- [ ] Tutorial/onboarding flow — natural to build once 3A's flow is final, so it doesn't
  have to be redone if the flow changes shape.

---

#### 3F — Stabilization
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
  the existing hand-rolled FSM (`InterceptorAI`) is a perfectly viable permanent choice
  for AI this simple if the package isn't mature enough yet.
- **Multiplayer readiness debt (informational, not a v1.0 blocker)**: client-authoritative
  `FlightBody` physics, brute-force `FindObjectsByType` scans, and the singleton
  `TeamAwareness` contact-aggregator will all need rewriting (not adapting) for netcode
  if Post-1.0 multiplayer is pursued. Fine to defer, but don't be surprised later.
