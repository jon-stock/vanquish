# Vanquish — Development Plan

## Concept Summary

Vanquish is a two-mode combat game built in Unity (C#):

1. **Workshop / R&D mode** — research, design, and test missiles, drones, and support
   infrastructure from modular parts.
2. **Combat mode** — deploy your designs in real-time battles against escalating CPU
   opponents, from small grenade-drop drones up to supersonic stealth CCA-style drones
   armed with hypersonic air-to-air missiles.

Both modes share one data-driven part/stat model so that "testing" a design in the
workshop uses the exact same simulation as live combat.

---

## Core Systems

### Missile components
- Payload (type, size, yield/blast radius, guidance-compatible fusing)
- Engine (solid/liquid/ramjet/scramjet, thrust curve, burn time)
- Airframe materials (mass, drag coefficient, heat resistance)
- Seeker (IR, radar/active-radar, semi-active, wire/datalink, optical)
- Fuel type (solid propellant, liquid, hybrid)
- Countermeasures (chaff/flare dispensers, maneuverability/agility packages, RCS-reducing shaping)
- Jamming / counter-jamming modules (ECM, ECCM, seeker resistance)

### Drone components
- Propulsion (electric, subsonic jet, supersonic jet)
- Airframe class (small quad, fixed-wing, flying-wing stealth, CCA-scale)
- Wing/propeller types (efficiency vs. speed vs. maneuverability trade-offs)
- Hull material (composite, metal alloy, radar-absorbent material)
- Engine (matched to propulsion type; power output, fuel consumption, heat signature)
- Fuel type (battery, jet fuel, hybrid)
- Weapon bay size/count (constrains payload/missile loadout)
- Sensor suite (radar, IR/EO camera, ESM/RWR)
- **Scout/recon variant**: long-endurance, low-signature, sensor-focused, unarmed or lightly armed — detects and marks enemy positions for other units

### Support architecture
- Ground/carrier launch platforms
- Radar and early-warning installations
- Command datalink network (affects seeker handoff, jamming resistance, scout-to-strike targeting)
- Base defenses (point-defense interceptors, SAM sites)

### Research / Tech Tree
- Directed-graph tech tree, tiers gated by prerequisites and in-game currency/resources
- Each node unlocks or upgrades a specific part stat block
- Tree spans the "full spectrum": grenade-drone tier → precision-guided tier → stealth/supersonic tier → hypersonic/CCA tier

### Combat Simulation
- Physics-based flight (thrust, drag, lift, mass) rather than pure stat rolls
- Guidance laws: pursuit (dumb-fire), proportional navigation (guided), datalink mid-course update + terminal seeker handoff (advanced)
- Sensor model: detection cones, radar cross-section modified by stealth parts, jamming as lock-quality/SNR degradation
- Scout drones feed a shared "contact picture" (fog-of-war reveal) that other units and the player can act on
- CPU AI: finite-state/behavior-tree opponents that scale in sophistication with player tech tier

---

## Technology Stack

- **Engine**: Unity (C#), URP for rendering
- **Data model**: ScriptableObjects for part definitions, JSON for save data/tech-tree state
- **UI**: Unity UI Toolkit (tech tree graph view, workshop editor, HUD)
- **Physics**: Unity PhysX (Rigidbody + custom aerodynamic force components)
- **AI**: Unity Behavior Trees (or custom FSM to start; revisit ML-Agents post-1.0 for adaptive AI)
- **Version control**: Git + GitHub
- **Testing**: Unity Test Framework (EditMode for stat/data logic, PlayMode for flight/guidance behavior)

---

## Phases & Milestones

### Phase 0 — Foundations (Pre-production)
**Goal:** De-risk the technical core before building content.

- [x] Set up Unity project, folder structure, source control, coding standards
- [x] Define ScriptableObject schema for all part categories (missile, drone, support)
- [x] Prototype flight physics for one drone and one missile using placeholder geometry
- [x] Prototype one guidance law (pursuit) against a static target
- [x] Prototype basic detection/RCS model (binary detect/no-detect first)
- [x] Decide on save/load format (JSON) and implement bare-bones save system

**Exit criteria:** A capsule-and-cube missile can be launched at a moving cube drone and
score a hit using real physics + one guidance law, all driven by data (not hardcoded values).

---

### Phase 1 — Vertical Slice (MVP)
**Goal:** One complete gameplay loop, minimal content, proves the concept is fun.

**Workshop:**
- [x] Basic part catalog: 1 airframe, 1 engine, 1 seeker, 1 payload per missile/drone tier (low tier only)
- [x] Simple design editor: pick parts, see computed stats (mass, speed, range, RCS)
- [x] Minimal tech tree: ~10 nodes, linear unlock path

**Combat:**
- [x] Single arena map
- [x] Player controls/deploys one drone + fires one missile type
- [x] One CPU enemy archetype (basic drone with simple FSM: patrol → detect → engage)
- [x] One scout drone type providing basic detection/reveal
- [x] Win/lose condition (destroy enemy base or all enemy units / player is destroyed)

**Meta:**
- [x] Currency/resource loop: win battles → earn resources → unlock next tier in workshop
- [x] Basic HUD (health, ammo, radar/contact ping)

**MVP Definition of Done:** A player can research a part, build a drone/missile loadout,
enter combat, use a scout to find the enemy, engage and win or lose, then return to the
workshop with earned resources. This is the minimum playable loop — ugly art is fine,
scope of content is minimal, but the loop must be complete and fun.

---

### Phase 2 — Content & Systems Expansion (Alpha)
**Goal:** Flesh out the "full spectrum" — expand breadth of parts, tiers, and combat depth.

Phase 2 is too large to tackle as one undifferentiated block — it's split into seven
sub-milestones (2A–2G) with a recommended sequence based on dependencies. Each has its
own concrete tasks, technical notes, and exit criteria so scope stays bounded and any
one sub-milestone can be picked up, paused, or reordered without derailing the rest.

**Recommended order:** 2A → 2B → 2G → 2C → 2D → 2E → 2F. Content breadth (2A/2B) comes
first because every later sub-milestone (test range, guidance depth, AI depth) is more
meaningful once there's actually more than one option per part slot. 2G (test range) is
cheap and high-value once that breadth exists. 2F (base building) is the most UI-heavy
and least essential to "full spectrum" feeling real, so it's last.

---

#### 2A — Missile Part Breadth
**Goal:** Every missile category has multiple real options with genuine trade-offs, not just Tier-0's one-of-each.

- [ ] Payloads: add remaining `PayloadType` variants (ShapedCharge, Kinetic, Cluster,
  Grenade) at multiple size tiers each (e.g. Small/Medium/Large HE-Frag), tuning
  `warheadMassKg`/`blastRadiusMeters`/`directDamage`/`splashDamage` so size is a genuine
  mass-vs-damage trade-off, not a strict upgrade.
- [ ] Engines: add assets for `SolidRocket` (upgraded), `LiquidRocket`, `Ramjet`,
  `Scramjet` (Tier 3-4 gate) — differentiate via `thrustNewtons`/`burnTimeSeconds`/
  `maxSpeedMetersPerSecond`/`infraredSignature` so each has a clear niche (e.g. solid =
  cheap/short-range, ramjet = fast/long-burn but needs high entry speed conceptually,
  scramjet = hypersonic tier).
- [ ] Seekers: add `SemiActiveRadar`, `ActiveRadar`, `WireOrDatalinkGuided`, upgraded
  `Optical`/`Infrared` tiers — differentiate via `detectionRangeMeters`/
  `fieldOfViewDegrees`/`jamResistance`/`countermeasureSusceptibility`. Active radar
  should be the first seeker type that doesn't need the launching platform to keep
  illuminating the target (relevant once semi-active exists as a contrast).
- [ ] Fuels: add `LiquidPropellant`, `HybridPropellant` missile fuel variants
  differentiated via `energyDensityMjPerKg`/`capacityKg`/`volatility` (volatility
  matters once splash damage from a fuel-tank hit becomes a system — see 2C/Phase 3).
- [ ] Countermeasures: add multiple tiers (basic flare/chaff → RCS-shaping →
  thrust-vectoring maneuverability package), each modifying a different subset of
  `radarCrossSectionMultiplier`/`infraredSignatureMultiplier`/`maxGForceBonus`/
  `decoyCharges`/`decoySuccessChance` rather than one part doing everything.
- [ ] Jamming/counter-jamming: add missile-mountable ECM (jamming) and ECCM
  (counter-jamming) modules at increasing tiers, per `JammingDefinition`.
- [ ] Extend `Phase1DataSeeder` (or split into a new `Phase2MissilePartSeeder`) to
  create all of the above as real assets under `Assets/_Project/Data/Missiles/`.

**Technical notes:** No new component types needed — this is pure content authored
against the existing `PartDefinition` subclasses. The main design work is tuning
numbers so choices are genuine trade-offs (heavier payload = less range/maneuverability,
better seeker = more mass/cost, etc.), not strictly-better upgrades. Consider a short
spreadsheet/table pass outside Unity to sanity-check the numbers before creating assets.

**Exit criteria:** Every `PartCategory.Missile*` enum value has at least 2–3 real assets
spanning Tier 0–2, each with a clear reason to pick it over the others.

---

#### 2B — Drone Part Breadth
**Goal:** Same as 2A, for drones — genuine propulsion/airframe/hull trade-offs across the tier spectrum.

- [ ] Propulsion: add `SubsonicJet` and `SupersonicJet` tiers alongside the existing
  `Electric` — this is the point where `FlightBody.orientToVelocity` and the
  "quadcopter vs. plane" distinction (from Phase 1 playtesting) becomes a real gameplay
  choice: electric = omnidirectional/hover, jet = forward-flight/banking. Add an
  `orientToVelocity`-equivalent flag to `PropulsionDefinition` so `VehicleFactory` can
  read it instead of hardcoding "all drones are quadcopters."
- [ ] Airframe classes: add `FixedWing`, `FlyingWingStealth`, `CcaScale` assets (the
  enum already exists on `DroneAirframeDefinition`) — differentiate via
  `hardpointCount`/`internalBayCount`/`baseRadarCrossSection`/`dragCoefficient`, with
  `FlyingWingStealth` specifically having a much lower `baseRadarCrossSection` and
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
