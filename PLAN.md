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

- [ ] Set up Unity project, folder structure, source control, coding standards
- [ ] Define ScriptableObject schema for all part categories (missile, drone, support)
- [ ] Prototype flight physics for one drone and one missile using placeholder geometry
- [ ] Prototype one guidance law (pursuit) against a static target
- [ ] Prototype basic detection/RCS model (binary detect/no-detect first)
- [ ] Decide on save/load format (JSON) and implement bare-bones save system

**Exit criteria:** A capsule-and-cube missile can be launched at a moving cube drone and
score a hit using real physics + one guidance law, all driven by data (not hardcoded values).

---

### Phase 1 — Vertical Slice (MVP)
**Goal:** One complete gameplay loop, minimal content, proves the concept is fun.

**Workshop:**
- [ ] Basic part catalog: 1 airframe, 1 engine, 1 seeker, 1 payload per missile/drone tier (low tier only)
- [ ] Simple design editor: pick parts, see computed stats (mass, speed, range, RCS)
- [ ] Minimal tech tree: ~10 nodes, linear unlock path

**Combat:**
- [ ] Single arena map
- [ ] Player controls/deploys one drone + fires one missile type
- [ ] One CPU enemy archetype (basic drone with simple FSM: patrol → detect → engage)
- [ ] One scout drone type providing basic detection/reveal
- [ ] Win/lose condition (destroy enemy base or all enemy units / player is destroyed)

**Meta:**
- [ ] Currency/resource loop: win battles → earn resources → unlock next tier in workshop
- [ ] Basic HUD (health, ammo, radar/contact ping)

**MVP Definition of Done:** A player can research a part, build a drone/missile loadout,
enter combat, use a scout to find the enemy, engage and win or lose, then return to the
workshop with earned resources. This is the minimum playable loop — ugly art is fine,
scope of content is minimal, but the loop must be complete and fun.

---

### Phase 2 — Content & Systems Expansion (Alpha)
**Goal:** Flesh out the "full spectrum" — expand breadth of parts, tiers, and combat depth.

- [ ] Full missile part set: all payload types/sizes, all engine types, all seeker types, fuels, countermeasures, jamming/counter-jamming
- [ ] Full drone part set: all propulsion tiers (electric → subsonic → supersonic), airframe classes, wing/propeller types, hull materials, weapon bay sizes
- [ ] Expand tech tree to full breadth (all tiers, branching paths, meaningful trade-offs)
- [ ] Proportional navigation + datalink mid-course guidance
- [ ] RCS/stealth model with partial detection probability (not just binary)
- [ ] Jamming/counter-jamming affecting lock quality
- [ ] Multiple CPU archetypes (interceptor, SAM site, scout-hunter) with behavior trees
- [ ] Multiple maps/scenarios with different objectives
- [ ] Base-building/support architecture placement (radar installations, launch platforms, point defense)
- [ ] Testing range mode in the workshop (fire designs at dummy targets before committing to a battle)

**Exit criteria:** All part categories from the design doc exist in some form; a player can
progress from grenade-drone tier through to at least early supersonic/guided-missile tier.

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

- **Simulation complexity vs. fun**: realistic flight/guidance physics can become fiddly;
  budget time in Phase 0 to tune "arcade vs. sim" feel before committing to full part depth.
- **Scope creep**: the part list is extensive — Phase 2 should timebox breadth rather than
  gold-plating any single category before all categories exist at a basic level.
- **AI difficulty scaling**: keeping CPU opponents credible across the full tech spectrum
  (grenade-drones to hypersonic stealth) without reworking AI at every tier needs early design attention.
- **Performance**: large numbers of physics-simulated projectiles/drones in combat scenes —
  plan for object pooling and simplified physics LOD from Phase 1 onward.
