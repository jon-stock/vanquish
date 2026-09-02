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
  the existing hand-rolled FSM (`InterceptorAI`) is a perfectly viable permanent choice
  for AI this simple if the package isn't mature enough yet.
- **Multiplayer readiness debt (informational, not a v1.0 blocker)**: client-authoritative
  `FlightBody` physics, brute-force `FindObjectsByType` scans, and the singleton
  `TeamAwareness` contact-aggregator will all need rewriting (not adapting) for netcode
  if Post-1.0 multiplayer is pursued. Fine to defer, but don't be surprised later.
