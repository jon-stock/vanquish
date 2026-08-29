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
- [ ] Implement MTOW validation checks in missile assembly (dry mass + variable fuel slider vs. motor capacity).
- [ ] Add payload variants (HE-Frag, Shaped Charge, Kinetic, Cluster, Grenades) with scaling mass penalties.
- [ ] Implement engine types across full spectrum (Solid Rocket, Liquid, Ramjet, Scramjet).
- [ ] Implement seeker spectrum: Wire/SACLOS, Laser-guided, Optical/TV, SARH, ARH, Imaging IR, Multi-spectral.
- [ ] Implement FAF decoy checks vs. guided missile SNR "fuzzing" jamming mechanics.
- [ ] Add countermeasure dispensers, RCS-shaping packages, ECM/ECCM modules.

#### 2B — Drone Part Breadth & Fuel Spectrum
- [ ] Implement MTOW validation checks in drone assembly (airframe limit vs. engine mass, payload, and fuel/battery slider).
- [ ] Implement propulsion types: Electric (battery slider), ICE (Petrol/Diesel), Subsonic Jet (Jet Fuel), Supersonic Jet.
- [ ] Implement rotor count & materials: Plastic, Carbon Fiber, Metal across Small/Medium/Large sizes.
- [ ] Add airframe classes: Small Quad, Hexacopter, Fixed-Wing, Flying-Wing Stealth, CCA-scale.
- [ ] Add material choices (Aluminum, Carbon Fiber, RAM, Titanium) affecting mass, thermal limits, and RCS.
- [ ] Differentiate external hardpoint drag/RCS penalties vs. internal weapon bays.

#### 2C — Guidance, Proximity Damage & Debug Telemetry
- [ ] Add Proportional Navigation (PN) and Datalink mid-course guidance laws.
- [ ] Implement Laser/Optical Line-of-Sight (LOS) terrain masking mechanics.
- [ ] Implement sub-component proximity damage system (airframe HP vs. module hitboxes).
- [ ] Build **Debug Telemetry Overlay** toggling real-time display of drag forces, air density, power/fuel burn, SNR, and flight vectors.

#### 2D — AI Depth
- [ ] Implement Interceptor, Scout-Hunter, and static SAM site AI archetypes.
- [ ] Give AI awareness of guidance limitations (e.g., AI maneuvers behind terrain to break laser LOS or deploys decoys against FAF pings).
- [ ] Scale AI tactics based on tech tier.

#### 2E — Maps & Scenarios
- [ ] Multi-terrain arenas (mountainous valleys breaking LOS, high-altitude plateaus with thin air).
- [ ] Objective-based scenarios (escort scout drone, destroy SAM network, base strike).

#### 2F — Support Architecture & Base Management
- [ ] Pre-combat placement of ground launch platforms, radar sites, and datalink relays.
- [ ] Base structures act as datalink nodes for missile mid-course updates.

#### 2G — Workshop Visual Assembly & Test Range
- [ ] Implement runtime 3D modular mesh swapping logic on assembly nodes.
- [ ] Build **Toggleable Overlays**: X-Ray / CoM / CoL view mode and visual aerodynamic airflow drag stream vectors.
- [ ] Implement visual fuel tank / battery cell scaling tied to continuous sliders.
- [ ] Seamless transition sequence (dynamic loading mask showing vehicle moving from editor bay to launch pad).
- [ ] Integrated Test Range to evaluate actual flight performance vs. Workshop estimated range using Debug Telemetry.

---

### Phase 3 — Dynamic Sandbox Campaign & Polish (Beta)
- [ ] Implement sector-based dynamic overworld map with territory control and resource logistics.
- [ ] Complete Tier 4–5 cutting-edge content (Hypersonic Air-to-Air, Stealth CCAs, Cognitive ECM).
- [ ] Full HUD polish (Dynamic LAR reticle, LOS status indicators, SNR fuzzing overlay, RWR audio/visuals).
- [ ] VFX/SFX pass (sonic booms, rocket plumes, thermal pings, ICE engine noise).
- [ ] Performance optimization (projectile object pooling, physics LODs).

---

### Version 1.0 — Release
- [ ] Final balance and QA pass across campaign mode and tech tree.
- [ ] Release packaging (Steam / itch.io).

---

## Post-1.0 — Live Improvements
- [ ] Thermal & High-G Material Degradation (e.g., sustained high-speed flight damaging RAM coatings or overheating engines).
- [ ] Engine Micro-Tuning Research (unlock ability to tweak engine internals to reduce dry mass or boost thrust).
- [ ] 6-DOF Physics Evaluation (expand `IAerodynamicBody` to support individual control surface deflections if desired).
- [ ] Multiplayer (PvP skirmish, co-op vs. CPU).
- [ ] Player map/scenario editor.
- [ ] Advanced adaptive AI (ML-Agents).
- [ ] Exotics tier (Directed Energy Weapons, Drone Swarm-Logic).
arCrossSection` and
  `internalBayCount` (stealth means internal weapons carriage).
  Fixed-wing airframes need `orientToVelocity = true` propulsion pairing to make sense.
- [ ] **Quadcopter → hexacopter upgrade path**: add a `rotorCount` field to
  `DroneAirframeDefinition` (e.g. 4 for the existing `SmallQuad`, 6 for a new
  `SmallHexa`/`MediumHexa` airframe tier). A hexacopter airframe should raise
  `hardpointCount`/`payloadCapacityKg`-relevant carry capacity (more weapon bay
  headroom, more sensor/countermeasure slots down the line) at the cost of higher
  `structuralMassKg` and more rotor mass (see rotor breadth below — more rotors
  mounted means their individual mass is paid multiple times). This is a genuine
  mass-vs-capacity trade-off, not a strict upgrade, matching the design goal of every
  part choice mattering. `VehicleFactory`'s procedural drone visual (see the "make the
  block look like a quadcopter" work item) needs to read `rotorCount` and generate the
  correct number of arms/rotors rather than hardcoding 4.
- [ ] **Rotor material & size breadth**: expand `WingOrPropellerDefinition` (this is
  the "rotor" part slot for multirotor drones) with a `RotorMaterial` enum (`Plastic`,
  `CarbonFiber`, `Metal`) and a `RotorSize` enum (`Small`, `Medium`, `Large`), each
  combination authored as its own asset:
  - Plastic: cheapest, low mass, low durability.
  - Carbon fibre: lightest, but weaker/less durable than plastic or metal (mass vs.
    structural trade-off, not a strict upgrade over plastic).
  - Metal/steel: heaviest, but strongest/most durable.
  - Size (small/medium/large) scales `liftCoefficient` and lift capacity up with size,
    at a mass and drag cost, independent of material choice — so e.g. "small carbon
    fibre" and "large plastic" are both valid builds for different purposes.
  A durability/structural-integrity stat is worth adding now even if nothing consumes
  it yet (informational, and a hook for a future "rotor damage" mechanic — see Phase 3
  stretch goals) so the material choice isn't purely cosmetic from day one.
- [ ] Wing/propeller types: add `FixedWing`, `DeltaWing`, `VariableSweepWing` assets
  (enum already exists on `WingOrPropellerDefinition`) with genuine speed-vs-maneuver
  trade-offs via `liftCoefficient`/`turnRateDegreesPerSecond`/`cruiseEfficiencyMultiplier`.
- [ ] Hull materials: add `AluminumAlloy`, `CarbonFiber`, `RadarAbsorbentMaterial`,
  `TitaniumAlloy` (enum already exists) — RAM should meaningfully cut RCS at a
  mass/cost premium; titanium should raise `maxTemperatureCelsius` for supersonic tiers.
- [ ] Engines/fuel: add jet-appropriate `DroneEngineDefinition`/`FuelDefinition`
  (JetFuel type) pairing with the new propulsion tiers.
- [ ] Weapon bays: add larger/internal bay variants (`isInternal = true` matters once
  stealth RCS is a system — an external hardpoint should add exposed RCS that an
  internal bay doesn't).
- [ ] Extend the seeder to create all of the above under `Assets/_Project/Data/Drones/`.

**Technical notes:** The propulsion/orientation flag change touches `VehicleFactory`
(read the flag instead of the current hardcoded `orientToVelocity = false`) — do this
as one small, isolated change with a headless regression check on the Phase 1 combat
scene (electric quadcopters must keep behaving exactly as before) before adding jet
content on top.

**Exit criteria:** Every `PartCategory.Drone*` enum value has at least 2–3 real assets;
a fixed-wing supersonic jet drone and an electric quadcopter both exist and both fly
according to their own propulsion model.

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

**Technical notes:** This is the most UI-heavy sub-milestone in Phase 2 — timebox it. If
placement UI turns into a rabbit hole, ship a simplified version (e.g. pick from a
handful of preset placement slots rather than freeform placement) and revisit freeform
placement in Phase 3's UI/UX pass.

**Exit criteria:** A player can place at least a launch platform and a radar
installation before a battle, and both are destructible, detectable targets during it.

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

### Phase 3 — Balancing, Polish & Content Completion (Beta)
**Goal:** Make the full spectrum (up to hypersonic/stealth CCA tier) playable, balanced, and polished.

- [ ] Top-tier content: stealth CCA-style drones, hypersonic air-to-air missiles
- [ ] AI scaling — CPU tech/behavior escalates alongside player progression
- [ ] Full UI/UX pass: tech tree visualization, workshop part comparison tools, combat HUD polish
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

## Risks & Open Questions

- **Simulation complexity vs. fun**: realistic flight/guidance physics can become fiddly, however if a decision has to be made between realism and arcadiness, pick realism. Use real world inspired parts, not sci-fi.
- **Scope creep**: the part list is extensive — Phase 2 should timebox breadth rather than
  gold-plating any single category before all categories exist at a basic level.
- **AI difficulty scaling**: keeping CPU opponents credible across the full tech spectrum
  (grenade-drones to hypersonic stealth) without reworking AI at every tier needs early design attention.
- **Performance**: large numbers of physics-simulated projectiles/drones in combat scenes —
  plan for object pooling and simplified physics LOD from Phase 1 onward.
