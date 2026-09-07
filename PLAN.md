# Vanquish — Development Plan (v2 — Theatre/Combat Pivot)

> **This supersedes the original two-mode (Workshop/Combat) plan.** The prior plan is
> preserved in git history (see `git log -- PLAN.md`) if any detail needs to be
> recovered. See [What Carries Over](#what-carries-over-from-the-original-plan) below
> for exactly what existing code and design is being reused vs. dropped.

## Concept Summary

Vanquish is now a two-layer war game:

1. **Theatre Map** — a turn-based strategic layer. The player surveils the map to
   gather intelligence, manages territory along a front line, researches and produces
   armies (drones, missiles, SAMs, support infrastructure), and decides where and when
   to commit forces to an engagement.
2. **Combat Instances** — real-time tactical battles, triggered from the theatre map,
   fought over a specific objective: a supply line, factory, warehouse, or forward
   operating base. Each side enters with a **limited stockpile** of missiles,
   drones, and SAMs drawn from what the theatre layer has built up. Winning is about
   *how* you spend that stockpile, not just whether you "have more stuff" — e.g.
   baiting an expensive SAM interceptor into firing at a cheap quadcopter is a
   legitimate, often-correct tactic, because it depletes a scarce resource the
   defender needed for a more dangerous threat later.

Combat instances are **symmetric**: the same engagement engine runs whether the player
is attacking (trying to destroy/disrupt an enemy target before running out of ordnance
or time) or defending (trying to protect a target using point defenses/interceptors/CAP
before the attacker's stockpile or patience runs out). Only who owns the objective and
who initiated the engagement flips.

Command sits at **three levels**, all present at once: theatre-level tactics (strategic
taskings issued on the theatre map — "survey this region," "task available FPV drones
against targets of opportunity"), battlefield/standing tactics within a combat instance
("decoys in first, hold SAM fire for anything supersonic"), and **direct third-person
manual flight control** of any single committed drone at will. See
[Command & Control Model](#command--control-model) below for the full breakdown.

Movement and reach are **bottlenecked by the least capable committed unit**: an army's
speed relocating across the theatre map is capped by its slowest member (a fast strike
drone can't outrun the supply truck it's grouped with), and a strike package's reachable
engagement radius in a combat instance is capped by whichever committed unit has the
shortest range/endurance — a small drone can't fly far, so if it's in the package, the
package can't reach further than it can.

Drones and missiles are flown by either a **hired human operator** (better, but a real
person who can be killed) or **AI** (a free, always-available, permanently weaker
fallback) — see [Personnel & Bases](#personnel--bases) below. And not every munition
that hits a target destroys it: a target's toughness vs. the attacking payload's size
determines how much damage actually lands — see
[Damage & Payload Model](#damage--payload-model) below.

Radar comes in two flavors with very different stakes: **theatre-based radar**, which
is persistent strategic infrastructure that cannot be destroyed as a side-effect of an
unrelated combat instance — the only way to remove it is to deliberately strike the
radar site itself before it matters — and **battlefield radar**, deployed with a
specific engagement, which *can* be destroyed mid-fight as a legitimate SEAD
(suppression of enemy air defense) sub-objective. Both must be **found** before they
can be targeted, and a dedicated **Wild Weasel** tactic exists to provoke and expose
them. See [Radar & SEAD](#radar--sead) below.

**Build order:** combat instances first (tier 0–1 drones/missiles/point-defenses, all
four target types, attack and defense), theatre map second. The theatre map is treated
as a placeholder/stub during combat-instance development — a simple pre-set "army
loadout" picker stands in for it until Phase 2. The full personnel/hiring system
(operator rosters, XP, permadeath) is theatre-map-dependent and lands in Phase 2+; it
is not urgent for the Phase 0/1 combat-instance work, though the "base" target type and
the damage/payload model are foundational and are being built from Phase 0 onward.

---

## What Carries Over From the Original Plan

Kept, and why:

- **Part-based data model** (`Scripts/Data/**`) — `PartDefinition`, `PartCategory`,
  `TechTier`, and all missile/drone/support/shared part definitions. The idea that
  drones and missiles are assembled from modular parts with real stat trade-offs is
  central to the new plan too — it's *why* a tier-0 quadcopter can be a smart tactical
  choice against a tier-3 point-defense site (cheap, low-RCS, expendable) even though
  it's strictly worse on paper. No changes needed yet; will gain new fields as combat
  instances need per-unit ammo/cost data, a per-unit range/endurance stat needed
  for the "movement capped by least capable member" rule, and the payload-size/
  hardness fields needed for the damage/payload model (see Phase 0).
- **`MissilePayloadDefinition`/`WeaponBayDefinition`** — already carry payload
  size/yield and weapon-bay capacity fields respectively; these become the direct
  input to the new damage/payload model (a bigger weapon bay → bigger payload → more
  effective against hardened targets) rather than needing new data structures.
- **`RadarInstallationDefinition` (`Data/Support/RadarInstallationDefinition.cs`)** —
  kept as the seed for both theatre-based and battlefield radar; gains a scope field
  (Theatre vs. Battlefield) rather than needing a separate definition per tier, since
  the underlying stats (range, detection quality bonus, emission signature) are the
  same shape either way. See [Radar & SEAD](#radar--sead).
- **`SensorSuiteDefinition`'s existing ESM/RWR fields** — already present on drone
  sensor suites (per the original design doc) but never wired to any behavior. These
  become the basis for the new emissions-detection channel (finding radars by their
  transmissions) rather than needing new data structures.
- **`SeekerType` enum (`Data/Missiles/SeekerDefinition.cs`)** — gains an
  **anti-radiation** value (homes on an active radar's emissions rather than RCS/IR),
  the seeker type needed for the Wild Weasel tactic and any real SEAD strike missile.
- **`BaseDefenseDefinition` (`Data/Support/BaseDefenseDefinition.cs`)** — kept as the
  seed for a new consolidated **`PartCategory.PointDefense`** category. It already has
  `SamSite`/`PointDefenseInterceptor`/`CIWS` as separate enum values; these become
  implementation sub-types (missile-based, gun-based, and future directed-energy) of
  one category sharing common stats (range, rate of fire, intercept probability, ammo
  count), rather than distinct part categories. Player-facing language can still say
  "SAM" informally — even a point-defense machine gun gets called that colloquially —
  but the data model treats them as one thing. See Phase 0.
- **Flight/guidance/detection prototype** (`Simulation/Flight/FlightBody.cs`,
  `Simulation/Guidance/*`, `Simulation/Sensors/*`) — this *is* the combat-instance
  simulation core. A combat instance is exactly "N flight bodies with guidance and
  detection, resolved in real time until one side's objective/stockpile is spent."
  Kept as-is; extended with `IDamageable`, proportional nav, and stockpile-aware
  spawning as instances mature.
- **Save system** (`Core/SaveSystem.cs`, `Core/SaveData.cs`) — kept, but `SaveData`
  will need new fields for theatre state (territory, front line, stockpiles,
  production) once Phase 2 starts. Combat-instance-only saves (Phase 0/1) can reuse
  the existing design/currency fields largely unchanged.
- **Tech tree schema** (`Data/TechTree/TechNode.cs`) — kept; still gates part unlocks
  by tier and prerequisites. Research/production now happens *on* the theatre map
  rather than in a separate "Workshop mode," but the underlying data model is
  unaffected.
- **Coding standards & Unity/URP tech stack** (`docs/CODING_STANDARDS.md`) — kept.
  Namespace convention, ScriptableObject-for-data / MonoBehaviour-for-state rule,
  interface-driven simulation code, and object pooling requirements all still apply.
  The folder layout gains a `Theatre/` area (see that doc for the update).

Dropped or reframed:

- **"Workshop mode" as a separate game mode** is dropped as a top-level concept.
  Design/research/production still exist, but as an activity on the theatre map
  (basing/army management), not a standalone scene you enter from a main menu.
- **"Escalating CPU opponents" as the sole progression frame** is replaced by the
  front-line/territory frame: difficulty and stakes now come from *where* on the
  theatre map you choose to engage and what the enemy has amassed there, not purely
  from a linear tier ladder.
- **Single arena map (Phase 1 MVP)** is reframed: Phase 1 now has multiple *target-type*
  encounter templates (supply line, factory, warehouse, forward operating base) rather
  than one generic arena, because the target type is a core tactical variable.
- **"Troop concentration/army" as a target type is replaced by "base."** A generic
  field-army objective (destroy some anonymous squaddies) isn't interesting to fight
  over. A **forward operating base** — where hired operators live, which can be
  destroyed and killed along with them, and which can be pushed further from the front
  with tech upgrades — gives the same "reduce enemy local combat power" role a real
  stake attached to it. See [Personnel & Bases](#personnel--bases).

---

## Core Concepts

### Theatre Map (strategic layer, turn-based)

- **Map representation**: a literal **hex grid** (matching the reference mock-ups
  below), not an abstract node graph. Each hex has a **terrain type** that affects
  movement cost and, in some cases, blocks movement outright (mountains, etc.). Roads
  connect hexes with much cheaper movement cost than open terrain, and are the
  backbone of the logistics/reach system below. The front line is not a single fixed
  boundary but whatever shape naturally emerges from where each side's controlled
  hexes meet — it can be long, uneven, and produce de facto "hot" sectors (short,
  well-connected, heavily contested stretches) and "safe" sectors (distant, harder to
  reach, or terrain-blocked) without the game needing to explicitly label them as such
  — that's an emergent read for the player, not a separate system.
- **Sites**: buildable installations placed on hexes (factories, warehouses, bases,
  radar, launch platforms, recon stations, etc. — see the reference mock-ups' `FAB`,
  `LOG`, `UAV`, `ISR`, `RSD`/`DGC` (radar/datalink), `SENTRY`, `ASSY`, `NODE`, `DLC`
  icons for the kind of roster this implies). Sites:
  - **Take turns to build** — placing a new site queues construction that completes
    after N turns (scaled by site type/tier), during which it's not yet operational.
  - **Can be repaired** — a damaged (but not destroyed) site can be repaired over
    turns at a resource cost scaled to damage.
  - **Can be relocated "with effort"** — moving an existing site costs both turns and
    resources, and the site is offline (and undefended) for the duration of the move;
    this is deliberately the most expensive of the three actions, matching the
    real-world cost of relocating fixed infrastructure. Not every site type may be
    eligible for relocation — heavy fixed installations vs. mobile launch platforms is
    a natural tier distinction to design in later.
- **Front line & territory**: territory ownership per-hex determines what's
  reachable/defensible in a given turn; contested hexes near the front change hands as
  a side effect of combat instance results (see the feedback loop below).
- **Surveillance/intel**: recon assets (scout drones, radar, satellites — TBD scope)
  reveal enemy composition, defenses, and target value *before* the player commits to
  an engagement. Poor intel = engaging blind.
- **Army building**: players assign researched part designs and production output to
  units/stockpiles stationed along the front. This is where the old "Workshop" design
  activity now lives.
- **Production & logistics**: factories produce missiles/drones/point-defense stock
  over turns; warehouses store it; supply lines move it to front-line armies along the
  road/rail network. All three are also the *target types* combat instances let you
  attack — this is the loop that ties the two layers together: disrupting enemy
  logistics on the theatre map is achieved by winning combat instances against them.
- **Movement & multi-front reach**: an army's relocation speed across the theatre map
  (reinforcing a region, repositioning to a threatened front) is capped by its
  slowest/least capable member, same as a real convoy — pairing fast strike drones
  with a support/logistics element means the whole army moves at the logistics
  element's pace. Movement is much faster along roads/rail than across open terrain,
  and blocked entirely by impassable terrain (mountains, etc.). This is why **site
  placement matters as much as raw production**: a factory sited on a well-connected
  road hub can supply multiple front sectors within its logistics reach per turn, while
  an equally productive factory stuck in a poorly-connected corner can only reliably
  service the one front nearest it. Reaching the far end of a large front from a single
  rear site can take many turns even along roads, which is exactly what makes a second,
  well-placed site valuable rather than redundant.
- **Turn resolution**: player takes strategic actions (move/reinforce armies, research,
  build/repair/relocate sites, redeploy recon) *and* issues theatre-level taskings
  (e.g. "survey this region," "keep FPV drones on standby to hit targets of
  opportunity along this stretch of front" — see
  [Command & Control Model](#command--control-model)), then chooses to initiate a
  combat instance at a front-line location, or passes the turn.

### Combat Instances (tactical layer, real-time)

- **Objective/target types** (tier 0–1 focus first):
  - **Supply line** — a moving/point-to-point convoy; attacker wins by destroying
    enough of it before it reaches safety; defender wins by escorting it through.
  - **Factory** — static, high-value, expect the heaviest fixed defenses (dedicated
    point-defense sites).
  - **Warehouse** — static, stores stockpile; medium defense; destroying it removes
    banked theatre-map resources rather than production capacity.
  - **Base (forward operating base)** — static, houses stationed operators/drones;
    defended by lighter, shorter-range point defense than a factory site (more so if
    tech has pushed it further from the front — see [Personnel & Bases](#personnel--bases));
    the target type most directly tied to reducing an enemy army's combat power *and*
    killing its hired operators on the theatre map.
  - **Radar site** — a fifth, optional-but-often-necessary target type: strikes a
    theatre-based radar installation specifically, in its own combat instance, ahead
    of (and separate from) whatever it was defending. Only relevant once the radar has
    been *found* (see [Radar & SEAD](#radar--sead)) — you can't target what you
    haven't detected.
- **Roles**: **Attacker** commits a strike package (drones/missiles) to damage/destroy
  the objective within a stockpile and/or time budget, and within the package's
  reachable range (see movement/reach rule above). **Defender** commits
  interceptors/point-defense/CAP to protect the objective within their own stockpile.
  Same engine both ways — only spawn setup and the win condition's perspective differ.
- **Stockpile economy**: each side enters an instance with a *finite* count of each
  unit/munition type (not infinite spawns). Every shot fired or drone launched
  permanently reduces that side's available stock for this instance (and, once the
  theatre map exists, for the rest of the campaign). This is what makes "waste their
  expensive point-defense interceptors with cheap decoy quadcopters" a real, viable,
  central tactic rather than flavor text.
- **Command & control model**: see the dedicated section below — standing tactics,
  target priorities, and direct manual piloting all coexist within an instance.
- **Win/lose conditions**: attacker wins by destroying the objective (or enough of it
  per target-type rules) before running out of committed stock/time; defender wins by
  preserving the objective until the attacker's stock/time is exhausted.

### Command & Control Model

Three levels of command coexist, and the player moves between them freely rather than
picking one mode for a whole session:

1. **Theatre-level tactics** (turn-based, theatre map — Phase 2): strategic taskings
   issued to units/regions rather than individual control, e.g. "survey this region,"
   "keep available FPV drones on standby to strike targets of opportunity along this
   stretch of front," "hold this warehouse's stock in reserve." These are standing
   orders that persist across turns until changed or fulfilled.
2. **Battlefield tactics** (real-time, within a combat instance — Phase 0/1): standing
   orders/target priorities for the instance, e.g. "quadcopters attack first as
   decoys, strike missiles follow on the second wave," "point defense holds fire for
   anything faster than Mach 1," "prioritize the warehouse's north entrance," or a
   **Wild Weasel tasking** ("send the bait drone in first to provoke the radar, then
   strike whatever lights up" — see [Radar & SEAD](#radar--sead)). The engagement
   resolves in real time without requiring constant input at this level.
3. **Direct control** (real-time, within a combat instance — Phase 1): the player can
   drop into third-person manual flight control of any single committed drone at
   will, with direct flight inputs, then hand it back to AI/standing orders and pick
   up another unit or step back out to issue battlefield tactics. This is where
   individual piloting skill matters, layered on top of the standing tactics governing
   everything the player isn't currently flying.

**What can be directly/manually controlled, and what can't** — driven by guidance
type, not by unit type in general:

- **Drones**: any drone can be directly piloted in third-person, since a drone is
  inherently remote-crewed rather than autonomous-only. This includes FPV-style
  strike drones, which are *designed* around manual piloting.
- **Missiles with an active seeker** (heat-seeking/IR, active/semi-active radar) fly
  fully autonomously once launched — no manual steering, consistent with how those
  guidance types actually work. Player influence is limited to launch timing/targeting
  and standing tactics.
- **Fly-by-wire / datalink (command-guided) missiles** — anything with a live comms
  link back to the launching platform — can be manually flown by the player in flight,
  the same way a directly-piloted drone can, for as long as the link holds.
- **Laser-designated munitions** require an active designator (the player, an allied
  unit, or a scout drone) painting the target for terminal guidance; the player's
  "control" here is aiming/holding the designator on target rather than flying the
  weapon itself.

This means the guidance-type taxonomy already in `Data/Missiles/SeekerDefinition.cs`
directly determines manual-control eligibility — no separate "can this be piloted"
flag is needed, it falls out of `SeekerType` (see Phase 1 tasks).

Note: **AI is not just a Phase-0 placeholder for direct control — it is a permanent,
intentional feature.** Any unit can always be flown by AI (free, always available, no
risk of losing a person), it's just categorically worse than a competent hired
operator. Direct control and battlefield-tactics standing orders are executed either
by an assigned human operator (better, with skill scaling per [Personnel &
Bases](#personnel--bases)) or by AI (baseline) if no operator is assigned or available.

### Personnel & Bases

*(Theatre-map-dependent; full depth lands in Phase 2+. Not urgent for Phase 0/1, but
the "base" target type and the AI-vs-operator distinction in the control model above
are designed around this from the start so nothing has to be reworked later.)*

- **Operators**: hireable human pilots who fly drones/missiles under direct control or
  execute battlefield tactics, in place of AI. An operator is measurably better than
  AI at whatever they're assigned to — accuracy, reaction time, and access to more
  advanced standing tactics — which is the incentive to hire and protect them rather
  than default to free AI control.
- **Experience & rank**: operators gain experience and rank up in the specific
  drone-type *category* they fly most (e.g. "FPV strike," "recon/scout," "interceptor/
  point-defense") — not a single general level, and not per exact drone model. A
  veteran FPV pilot is better at FPV strikes specifically; assigning them to an
  unfamiliar category means flying closer to AI-level performance until they build
  experience there too.
- **Bases**: operators live at bases on the theatre map. A base can be:
  - **Attacked** — bases are the "Base" combat-instance target type (replacing the old
    "troop concentration" — see [What Carries Over](#what-carries-over-from-the-original-plan)).
  - **Upgraded to sit further from the front line** via tech-tree research, trading
    safety (harder for the enemy to reach/reduced exposure) for longer transit — this
    reuses the existing range/endurance bottleneck rule: a base further back means a
    longer trip for its units to reach the front, so basing decisions interact
    directly with the movement/reach system already defined above.
- **Permadeath, no insurance**: if a base is destroyed, operators stationed there are
  killed, permanently, along with all their accumulated rank/experience. There is no
  buy-back, evacuation, or capture mechanic (at least initially) — this is a
  deliberate stakes-raising design choice, not an oversight; revisit only if
  playtesting shows it's too punishing rather than tense.
- **Hiring economy**: recruiting new operators (cost, availability, upkeep) is an open
  design question, to be resolved when Phase 2 theatre-map economy is designed — flagged
  in Risks & Open Questions.

### Damage & Payload Model

Not every hit destroys its target — a target's toughness relative to the attacking
payload's size determines how much damage actually lands, so "which drone/missile to
use against which target" is a real decision, not just "send whatever's cheapest."

- Every munition/weapon bay already carries a **payload size/yield** stat
  (`MissilePayloadDefinition`, `WeaponBayDefinition`). Every target (structures like
  factories/warehouses/bases/point-defense sites, and units) gets a **hardness**
  rating alongside its health pool.
- **Small payloads (e.g. FPV drone munitions) against hardened targets (e.g. a
  warehouse or a big SAM site) accumulate damage but hit a soft cap** — they can chip
  a hardened target down to a residual damage percentage but cannot finish it off
  outright, no matter how many are thrown at it. A big drone/missile with a payload
  large enough to exceed the target's hardness threshold can land full, uncapped
  damage and finish/destroy it.
- This creates a **"soften vs. finish" tactical layer** on top of the existing
  stockpile-depletion tactic: cheap FPV drones remain useful for softening a target
  and draining the defender's point-defense stock, but the attacker still needs to
  commit at least one adequately-sized payload to actually secure the kill —
  mirroring how, e.g., a warehouse or a factory-grade SAM site can shrug off drone
  munitions all day without a proper strike missile to finish the job.
- Applies symmetrically: a defender's point-defense fire also needs to interrogate the
  incoming payload class, not just presence/absence of a target, to decide whether
  it's worth expending an expensive interceptor.

### Radar & SEAD

Radar is split into two tiers with different stakes, plus a shared "you must find it
before you can fight it" rule and a dedicated counter-radar tactic:

- **Theatre-based radar** (strategic infrastructure, Phase 2+): persistent, sits on
  the theatre map, and grants its owner both a strategic surveillance/intel bonus
  (harder for the enemy to scout/hide activity in its coverage) *and* a detection-
  range/quality bonus applied to **any** combat instance fought within its coverage
  area. Critically, **it cannot be destroyed as a side-effect of an unrelated combat
  instance** — the only way to remove it is a deliberate strike on the radar site
  itself, fought as its own combat instance (the **Radar site** target type) *before*
  engaging whatever it was covering. This makes pre-emptive SEAD a real theatre-level
  strategic decision: soften the area up before you commit to the target you actually
  want.
- **Battlefield radar** (tactical, Phase 0/1): deployed as part of a specific
  engagement (e.g. attached to a factory or base's defenses). While alive, it extends
  the defender's detection range/quality and improves point-defense lock quality in
  that instance; destroying it is a legitimate **in-combat SEAD sub-objective** —
  after it's down, the defender's point-defense falls back to each unit's own
  (weaker) organic sensors for the rest of that instance.
- **Finding radar (both tiers)**: radar installations are not detected the same way
  as physical units (RCS/IR). They're found via a separate **emissions-detection
  (ESM/RWR) channel** — a radar that stays passive/off is very hard to find; one
  that's actively emitting (searching, tracking, guiding a shot) can be picked up by
  an ESM/RWR-equipped scout or aircraft, distinct from and complementary to the
  existing RCS-based `DetectionSensor` model. This reuses `SensorSuiteDefinition`'s
  existing (currently unused) ESM/RWR fields rather than needing new sensor data.
  Theatre-level scouting works the same way at strategic scale: an un-scouted region's
  radar coverage is unknown/unconfirmed until recon locates it.
- **Wild Weasel tactic**: a drone role/tasking built specifically to provoke and
  expose radar. A Wild Weasel drone presents itself as a threat worth engaging (e.g.
  behaving like a valuable strike package), baiting the defender's radar into
  emitting/tracking/guiding a shot — at which point it becomes detectable via ESM/RWR
  and can be struck. Pairs naturally with a new **anti-radiation seeker type**
  (homes on the radar's own emissions rather than RCS/IR), giving the attacker a
  purpose-built weapon for the follow-up kill once the radar reveals itself. This is
  a variant of the existing decoy/stockpile-drain tactic, but aimed at exposing and
  removing a detection asset rather than depleting ammunition.

### Strategic Victory & Defeat Conditions

The theatre map needs an explicit game-over check distinct from any single combat
instance's win/lose result. **Victory conditions are pluggable and scenario-
configurable** — a given campaign/skirmish setup chooses which of these are active
rather than the engine always checking all of them:

- **Economic collapse**: a side loses when it has **zero operational factories and
  none currently under construction** — i.e. no remaining path back to production, not
  just a momentary zero. This ties directly into the existing factory target type and
  site-construction system; no new data model needed, just a check run each turn.
- **Territorial control**: a side wins by controlling **at least X% of the map's
  contestable hexes**, sustained for some minimum duration (to avoid a single lucky
  turn triggering it). Both the percentage and sustain duration are open tuning
  numbers, to be set via playtesting like the other thresholds in this plan.
- **Dominance project**: a top-tier tech-tree unlock that starts a multi-turn capstone
  construction (a strategic superweapon or decisive war-winning capability) **at a
  site**, exactly like any other site. Completing it wins the game outright. Because
  it lives on a site, it is discoverable (once scouted) and attackable like any other
  target — the opponent has a real, systemically-supported chance to race to destroy
  it before completion, using the existing site/target-type/combat-instance machinery
  rather than a bespoke new system.
- These are deliberately **layered on top of existing systems** (factories, hex
  ownership, sites) rather than introducing new data models — victory-condition logic
  is a set of turn-end checks over state the theatre map already tracks.

---

## Technology Stack

Unchanged from the original plan:

- **Engine**: Unity (C#), URP for rendering.
- **Data model**: ScriptableObjects for part definitions, JSON for save data.
- **Physics**: Unity PhysX (Rigidbody + custom aerodynamic force components) for
  combat instances.
- **AI**: FSM/behavior trees for both the combat-instance opponent AI and (new) the
  theatre-map strategic AI (these are different problems — tactical micro vs.
  strategic resource/front-line decisions — and will likely need different
  approaches; revisit post-MVP).
- **Version control**: Git + GitHub.
- **Testing**: Unity Test Framework (EditMode for stat/data/stockpile logic, PlayMode
  for flight/guidance/engagement-resolution behavior).

New for the theatre layer (Phase 2+):
- **Map rendering**: a literal 2D hex grid (per the reference mock-ups) — Unity's
  `Tilemap`/hex-grid tooling (or a lightweight custom hex-coordinate renderer) plus
  UI Toolkit for panels/HUD, rather than a fully physically simulated 3D space —
  turn-based strategy layers are cheaper to build as data + 2D grid + UI than as
  physical scenes, and this also keeps it decoupled from the combat-instance `Simulation/`
  physics layer per the mode-agnostic rule.

---

## Phases & Milestones

### Phase 0 — Combat Instance Foundations
**Goal:** De-risk the tactical core: one working attack-vs-defend engagement with a
real stockpile economy, using only tier 0–1 parts and one target type.

- [x] Extend `PartDefinition`/relevant subtypes with per-unit **cost** and **stockpile
      quantity** fields needed to run a finite-ammo engagement (`buildCost` already
      existed; per-instance stockpile quantity added as `StockpileEntry.startingCount`
      in `Combat/StockpileEntry.cs` rather than on `PartDefinition` itself, since it's
      a per-engagement loadout concern, not a static part stat).
- [x] Add a per-unit **range/endurance** stat (max operational radius) to drone/missile
      part data (`FuelDefinition.operationalRangeMeters`, authored directly for now —
      see the code comment on why this isn't derived from thrust/drag/burn yet), and
      implement the **"bottlenecked by least capable member"** rule as a pure utility
      (`Combat/StrikePackageRange.ComputeUsableRadiusMeters`).
- [x] Consolidate the old `BaseDefenseDefinition` into a single
      **`PartCategory.SupportPointDefense`** category (`Data/Support/PointDefenseDefinition.cs`)
      with a `PointDefenseImplementation` sub-type (Missile / Gun / DirectedEnergy),
      sharing common stats (range, rate of fire, intercept probability, health,
      ammo count) instead of separate near-duplicate definitions.
- [x] Add `IDamageable` + basic health/destruction resolution
      (`Simulation/Damage/IDamageable.cs`, `Damageable.cs`) so units and static
      objectives can actually be destroyed. Not yet wired into `FlightBody` collision
      handling — that needs an actual scene/prefab to test against (see note below).
- [x] Add a **payload size** stat to munitions/weapon bays (already present as
      `MissilePayloadDefinition.warheadMassKg` / `WeaponBayDefinition.payloadCapacityKg`
      — no change needed) and a **hardness** rating to `IDamageable` targets
      (`Damageable.hardness`); implemented the damage/payload soft-cap rule as a pure,
      unit-testable function (`Simulation/Damage/DamageResolver.ApplyHit`) — see
      [Damage & Payload Model](#damage--payload-model).
- [~] Build an **engagement setup** flow: `Combat/EngagementController.cs` wires a
      hardcoded attacker/defender `Stockpile` (Inspector-authored `StockpileEntry[]`
      loadouts — no theatre map yet) against one target type
      (`Combat/BaseObjective.cs`, per [Personnel & Bases](#personnel--bases)) and
      resolves win/lose. **Not yet done**: actually spawning attacker/defender units
      into a scene — there is no Unity scene/prefab in this repo yet to spawn into
      (see note below); `TryCommitAttackerUnit`/`TryCommitDefenderUnit` are the hooks a
      future spawner calls before instantiating a unit.
- [x] Implement **stockpile depletion**: `Stockpile`/`StockpileEntry` decrement a
      per-side, per-unit-type counter on commit; depleted entries refuse further
      commits (`Stockpile.TryCommit` returns false).
- [x] Implement one target-type objective/win-condition pair
      (`BaseObjective.HasMetAttackerWinCondition` — destroy N% of the base's health,
      subject to the damage/payload cap above; `EngagementController` resolves
      defender-win on attacker stockpile exhaustion or timeout).
- [~] Prototype the **battlefield-tactics layer** at minimum viability: added the
      `StandingOrder` enum (`AttackNow` / `Hold`) as the data-model stub. **Not yet
      done**: an actual AI executor that reads a `StandingOrder` and calls
      `TryCommitAttackerUnit` — deferred alongside the spawner above.
- [x] Keep existing `PursuitGuidance`/`DetectionSensor`/`FlightBody` as-is for this
      phase — untouched (only a stale doc-comment on `IGuidanceLaw` was corrected).

**Note on testability:** this repo has no actual openable Unity project yet (no
`ProjectSettings/`, `Packages/manifest.json`) — only loose scripts under
`_UnityScaffold/`. Everything above is written and internally consistent, but the
parts marked `[~]` (spawning real units into a scene, an AI executor for standing
orders) need a real scene + prefabs to finish and to actually press Play against.
`Simulation/Damage/DamageResolver.cs` has no Unity dependency and is the one piece
that could be unit-tested right now with plain NUnit if that's useful before a full
project exists. Next step to make this pressable/testable end-to-end: create a Unity
project (Hub) and move `_UnityScaffold/Assets/_Project` into its `Assets/`.

**Exit criteria:** A player can commit a small stockpile of tier-0/1 drones and
missiles against a defended base (or vice versa as defender against a CPU/scripted
attacker), watch the engagement resolve in real time with real destruction (respecting
the payload-vs-hardness cap) and real stockpile depletion, and get a clear win/lose
result.

---

### Phase 1 — Combat Instance MVP (all target types, attack & defense)
**Goal:** Prove out the full tactical layer described in the pivot, still without a
real theatre map.

- [ ] Implement the remaining three target types: **supply line** (moving objective),
      **factory**, **warehouse** — each with its own defense profile and win-condition
      rules.
- [ ] Make attack/defense fully symmetric: any target type can be run with the player
      on either side, using the same engagement engine, only the spawn/role setup
      differs.
- [ ] Basic opposing-side AI: when the player is attacker, an AI defender allocates
      point-defense fire; when the player is defender, an AI attacker sequences a
      strike package (including using cheap units as decoys — this is the tactical
      thesis of the pivot and should be visible in the *enemy's* behavior too, not
      just available to the player).
- [ ] Surface the stockpile economy in the UI clearly: show unit cost, remaining
      stock per type, and (ideally) an indicator of what the defender just expended
      responding to an attacker's decoy — the player needs to *feel* the trade-off to
      make good decisions.
- [ ] Expand the battlefield-tactics layer: richer standing orders (target priority by
      type, engagement range/altitude rules for point defense).
- [ ] Build the **direct control** layer: a third-person manual flight control rig the
      player can drop into for any single committed drone (reusing/extending
      `FlightBody` for player input instead of `IGuidanceLaw`), and back out of to
      resume standing orders/battlefield tactics for that unit.
- [ ] Extend `SeekerType`/guidance data to distinguish **autonomous seekers**
      (heat-seek, active/semi-active radar — no manual control), **command-guided /
      datalink** (manually flyable by the player while the link holds, same control
      rig as a drone), and **laser-designated** (requires an active designator —
      player, ally, or scout — painting the target rather than flying the weapon).
      Wire manual-control eligibility to this field rather than a separate flag.
- [ ] Extend the payload/hardness model to the remaining target types (supply line,
      factory, warehouse) so each has its own hardness rating and cap behavior — e.g.
      a factory should be at least as hardened as a base, a warehouse and supply line
      likely softer.
- [ ] Add **battlefield radar** as an embedded defensive asset on defended target
      types (`RadarInstallationDefinition`, scoped Battlefield): while alive it
      extends the defender's detection range/lock quality; add it as a destroyable
      sub-objective within the instance so attackers can choose to prioritize SEAD
      before the main strike.
- [ ] Add an **emissions-detection (ESM/RWR) channel**, separate from the existing
      RCS-based `DetectionSensor`: wire up `SensorSuiteDefinition`'s existing ESM/RWR
      fields so an equipped unit can detect an *actively emitting* radar that would
      otherwise be invisible; a passive/off radar stays hidden.
- [ ] Add an **anti-radiation `SeekerType`** (homes on active radar emissions) and a
      **Wild Weasel** drone role/tasking that provokes a radar into emitting (so it
      becomes findable via the ESM channel above) for a follow-up strike — see
      [Radar & SEAD](#radar--sead).
- [ ] Placeholder "army loadout" picker stands in for the theatre map: a simple
      pre-battle screen where the player picks what stockpile they're bringing to a
      canned scenario, per target type, respecting the range/endurance bottleneck rule
      from Phase 0.

**Exit criteria:** A player can fight and win/lose attack and defense instances
against all four target types using only tier 0–1 parts, the stockpile-tactics loop
(cheap decoys draining expensive defenses) is demonstrably effective/necessary against
at least one scripted scenario, and the engagement engine has no target-type-specific
special-casing that would block adding the theatre map later.

---

### Phase 2 — Theatre Map (turn-based strategic layer)
**Goal:** Replace the Phase 1 placeholder loadout picker with the real strategic
layer: front line, territory, intel, army building, production, and supply logistics,
all turn-based, feeding into and out of combat instances.

- [ ] **Hex grid data model**: per-hex terrain type (open/road-rail/impassable
      mountain, etc. at minimum), per-hex movement cost, per-hex territory ownership.
      Front line is derived each turn from ownership adjacency, not stored as its own
      separate boundary object.
- [ ] **Site placement/construction/repair/relocation**: sites (factory, warehouse,
      base, radar, launch platform, recon station) are placed on a hex; construction
      takes N turns before becoming operational; a damaged site can be repaired over
      turns at a cost scaled to damage; relocating an existing site costs turns *and*
      resources and leaves it offline/undefended for the duration — the most
      expensive of the three actions by design.
- [ ] **Army/supply movement over the hex grid**: relocation speed is capped by the
      slowest/least capable unit in the army (reuses the range/endurance bottleneck
      rule from Phase 0), with movement cost per hex sharply reduced along roads/rail
      and blocked outright by impassable terrain — this is what lets a well-placed
      rear site reach multiple front sectors while a poorly-placed one can't, without
      needing a separate "multi-front reach" system.
- [ ] Turn loop: player actions per turn (move/reinforce, research, build/repair/
      relocate sites, recon), then choose to trigger a combat instance at a front-line
      location or end turn.
- [ ] Theatre-level tactics/taskings: standing orders issued to units or regions that
      persist across turns rather than one-off actions, e.g. "survey this region,"
      "keep available FPV drones on standby to strike targets of opportunity along
      this stretch of front," "hold this warehouse's stock in reserve." These resolve
      automatically each turn until changed or fulfilled.
- [ ] Surveillance/intel system: recon reveals enemy composition/defenses at a
      location before commit; unscouted engagements are riskier (fog-of-war at the
      strategic layer, distinct from the existing tactical-layer `DetectionSensor`
      fog-of-war).
- [ ] **Theatre-based radar**: `RadarInstallationDefinition` scoped Theatre, placed on
      the theatre map; grants its owner a strategic surveillance/intel bonus in its
      coverage area plus a detection-range/quality bonus applied to any combat
      instance fought there. Unconfirmed/hidden until scouted, same as other
      unscouted theatre assets above.
- [ ] **Radar site** as a strikeable combat-instance target type: lets the player (or
      AI opponent) run a dedicated SEAD strike against a found theatre-based radar
      *before* committing to the target it was covering — the only way to remove a
      theatre radar's bonus, since it cannot be destroyed as a side-effect of an
      unrelated instance (see [Radar & SEAD](#radar--sead)).
- [ ] Production & logistics simulation: factories generate stockpile over turns,
      supply lines move it toward the front, warehouses bank it — and each of these
      is literally the object being fought over in a combat instance targeting that
      type.
- [ ] Feedback loop: combat instance results (objective destroyed/damaged, stockpile
      spent) write back into theatre-map state (reduced production, disrupted
      supply, depleted stock, territory/front-line shift).
- [ ] Army building UI: assign part designs + tech-tree unlocks to what a region can
      produce/station (this absorbs the old "Workshop mode" design activity).
- [ ] **Personnel system**: hireable operator roster (recruit, assign to a base and a
      drone-type category), per-category experience/rank progression, and stat
      bonuses (accuracy/reaction/tactics access) over AI baseline for whatever
      category they're assigned to. Extend `SaveData` with an operator roster.
- [ ] **Bases as theatre-map entities**: buildable/upgradeable structures that house
      operators; tech-tree upgrade to relocate a base further from the front line
      (trades safety against the range/endurance bottleneck rule — a base further
      back means a longer trip to the front for its units).
- [ ] **Base destruction consequence**: wire the existing "Base" combat-instance
      target type (Phase 0/1) up to real stakes — a destroyed base permanently kills
      its stationed operators (and their accumulated rank/experience) with no
      insurance/evacuation mechanic, and reduces the owning side's local combat power.
- [ ] Army building UI extension: assign specific hired operators (not just AI) to
      specific units/loadouts before a combat instance.
- [ ] Basic theatre-level AI opponent: decides where to reinforce, what to build, and
      when/where to attack across the front.
- [ ] **Strategic victory/defeat conditions**: implement economic collapse (zero
      operational factories + none under construction), territorial control (>=X% of
      contestable hexes, sustained), and dominance project (a top-tier tech-gated
      capstone build at a site, completable to win, attackable like any other site) as
      pluggable, scenario-selectable turn-end checks — see
      [Strategic Victory & Defeat Conditions](#strategic-victory--defeat-conditions).
      At minimum wire up economic collapse and territorial control for the Phase 2
      exit slice; the dominance project can land alongside Phase 3's top-tier content
      if the underlying tech tier doesn't exist yet.

**Exit criteria:** A full turn loop exists — scout, build, decide where to engage,
fight a combat instance, see the result change the map — on a hex grid large enough to
have at least two distinct front-line sectors, with terrain/roads visibly affecting how
fast each side can reinforce them, using tier 0–1 content throughout, including at
least one hired operator who can gain rank and can be killed if their base falls, and
at least one strategic victory condition (economic collapse or territorial control)
able to end the game.

---

### Phase 3 — Full Tech Spectrum & Deepened Systems (Alpha)
**Goal:** Expand breadth (tiers 2–4) and depth (guidance/sensors/ECM) across both
layers, now that the core loop is validated end-to-end.

- [ ] Full missile/drone/support part catalog across all tech tiers (reusing the
      existing `TechTier` scale, Tier0_Improvised → Tier4_Hypersonic).
- [ ] Proportional navigation + datalink mid-course guidance (upgrade from pursuit-only).
- [ ] RCS/stealth model with partial detection probability (upgrade from binary
      detect/no-detect); jamming/counter-jamming affecting lock quality.
- [ ] Deeper stockpile-cost tactics unlocked by higher tiers: countermeasures,
      decoy-specific parts, stealth shaping — all should reinforce the "cheap unit vs.
      expensive interceptor" trade-off rather than replace it.
- [ ] Multiple simultaneous fronts / larger theatre map; richer territory effects.
- [ ] Theatre-level and tactical-level AI both scale with player tech tier and
      territory pressure.
- [ ] Base-building/support architecture placement (radar installations, launch
      platforms, point defense) as a theatre-map activity.
- [ ] **Dominance project tech + content**: the top-tier tech-tree capstone that
      unlocks the dominance-project build (see
      [Strategic Victory & Defeat Conditions](#strategic-victory--defeat-conditions)),
      if it wasn't already stubbed in during Phase 2.

**Exit criteria:** All part categories from the design exist in some form; a player
can progress from grenade-drone tier through early supersonic/guided-missile tier,
across a multi-front theatre map, with both layers' AI providing credible opposition,
and all three strategic victory condition types are fully implemented and biteable.

---

### Phase 4 — Balancing, Polish & Content Completion (Beta)
**Goal:** Make the full spectrum (up to hypersonic/stealth CCA tier) playable,
balanced, and polished, on both layers.

- [ ] Top-tier content: stealth CCA-style drones, hypersonic air-to-air missiles.
- [ ] Balance pass across all tiers and both layers (part stats, tech/production
      costs, stockpile economy tuning, theatre AI difficulty curve).
- [ ] Full UI/UX pass: theatre map visualization, tech tree, army/loadout builder,
      combat-instance HUD.
- [ ] Art/audio pass.
- [ ] Campaign/mission structure (a defined war, not just open skirmish).
- [ ] Tutorial/onboarding (two layers now need teaching, not one).
- [ ] Performance pass (many simultaneous projectiles/drones in instances; theatre
      map turn-resolution performance at scale).
- [ ] Bug bash + QA pass.

**Exit criteria:** Feature-complete against this plan, stable performance, balanced
difficulty across both layers.

---

### Version 1.0 — Release
- [ ] Final balance pass based on beta feedback.
- [ ] Final QA/certification pass.
- [ ] Store page / release packaging (TBD).
- [ ] Launch.

### Post-1.0 — Live Improvements
- [ ] Multiplayer (PvP theatre wars, co-op vs. CPU).
- [ ] Theatre/scenario editor for user-generated content.
- [ ] Deeper AI (adaptive theatre and tactical opponents).
- [ ] Additional part tiers / exotic tech.
- [ ] Modding support (expose part/theatre definitions).
- [ ] Naval/ground unit expansion beyond air-centric combat.
- [ ] Replay/spectator system for analyzing engagements.
- [ ] Leaderboards / ranked skirmish modes.

---

## Suggested Milestone Timeline (relative, not calendar-locked)

| Milestone | Depends on | Rough relative effort |
|---|---|---|
| Phase 0 — Combat Instance Foundations | — | Small |
| Phase 1 — Combat Instance MVP (all target types) | Phase 0 | Medium |
| Phase 2 — Theatre Map | Phase 1 | Large |
| Phase 3 — Full Tech Spectrum (Alpha) | Phase 2 | Large |
| Phase 4 — Beta | Phase 3 | Large |
| v1.0 — Release | Phase 4 | Small (stabilization) |
| Post-1.0 | v1.0 | Ongoing |

Treat calendar estimates as unreliable until Phase 0/1 are complete — the stockpile
economy and three-level command model are the biggest unknowns for "is this fun," and
the theatre map's scope (Phase 2) should not be locked down until the tactical loop is
proven.

---

## Risks & Open Questions

- **Theatre map scope is the biggest open risk.** "Full territory control wargame" is
  a large design space (how many fronts, how granular is territory, how does the
  strategic AI behave, turn cadence). Recommend scoping Phase 2 tightly to "one front,
  a handful of regions" before expanding, and treating the detailed theatre-map design
  as its own follow-up design pass once Phase 1 is validated.
- **Hex grid size/performance vs. "the front might be much larger than this"**: the
  reference mock-ups show a modest board, but the stated intent is a much bigger front
  with multiple sectors. Pathfinding/movement-cost calculation and AI decision-making
  need to scale to that size from the start of Phase 2 rather than being hard-coded
  against a small test board.
- **Multi-front reach balance is the crux of the site-placement decision**: "one
  well-placed rear factory can service several fronts" only works if the road/rail +
  movement-cost model actually produces that outcome in practice, not just in theory —
  needs explicit playtesting/tuning once the hex grid and logistics model exist,
  alongside the stockpile and payload/hardness tuning already flagged below.
- **Victory condition tuning and interaction**: the territorial-control percentage/
  sustain-duration and the dominance project's build time/cost are unproven numbers
  needing playtesting like everything else in this plan. There's also a design
  question worth watching once more than one condition is active in the same scenario
  — e.g. should a side racing to finish a dominance project also worry about economic
  collapse if their factories are simultaneously under attack, or should scenario
  design keep conditions mutually exclusive per game mode initially.
- **Stockpile economy balance vs. fun**: the core new tactical thesis (cheap decoys
  draining expensive interceptors) needs to actually feel good and be legible to the
  player (not just true on a spreadsheet) — budget real playtesting time in Phase 1
  before building Phase 2 on top of it.
- **Two AI problems, not one**: tactical micro-AI (combat instances) and strategic
  AI (theatre map) are different disciplines; don't assume one approach serves both.
- **Direct control adds a third discipline to get right**: third-person manual flight
  feel (input/camera/handling) is a different skill set from data-driven simulation
  and AI work, and needs its own prototyping/playtesting pass in Phase 1 rather than
  being treated as a thin wrapper over `FlightBody`.
- **Command-guided/laser-designation guidance nuance**: gating manual control off
  `SeekerType` (autonomous vs. command-guided vs. laser-designated) needs to be
  decided before Phase 1's part catalog grows, since it affects how every future
  missile seeker type is authored, not just tier 0–1 ones.
- **Feedback loop complexity**: combat instance results changing theatre-map state
  (Phase 2) is the crux of the two-layer design and the least proven part of the pivot
  — worth a small vertical-slice prototype before fully committing to the theatre data
  model.
- **Operator permadeath tuning is unproven**: losing a veteran operator's entire rank
  with no insurance/evacuation option is a deliberate stakes-raising choice, but it
  could equally read as too punishing (players stop engaging risk entirely) or too
  weightless (if hiring replacements is trivially easy) — needs real playtesting once
  Phase 2's economy exists, and the "no insurance" stance should be treated as a
  hypothesis, not a locked decision.
- **Hiring/upkeep economy is undesigned**: operator recruitment cost, availability,
  and any ongoing upkeep are open questions deferred to Phase 2 design work; this
  directly affects how harshly permadeath bites and how quickly a destroyed base can
  be "recovered from."
- **Damage/payload soft-cap tuning**: the exact hardness values and soft-cap
  percentages per target type (how much can an FPV drone alone chip a base/warehouse/
  factory down to before it plateaus) are unproven numbers — needs the same kind of
  playtesting attention as the stockpile economy, ideally in the same Phase 1 pass
  since both systems interact (an attacker choosing "how many decoys, how big a
  finishing payload" is one combined decision).
- **Emissions-detection (ESM/RWR) model is a new, separate detection channel**: it
  needs to interoperate cleanly with the existing RCS-based `DetectionSensor` rather
  than becoming a bolted-on special case — worth designing both as instances of one
  more general "detectability" abstraction if possible, when Phase 1's ESM work
  starts.
- **Theatre-radar pre-strike adds a mandatory extra step to the "choose where to
  engage" decision** once radar coverage exists: needs playtesting to confirm it
  reads as satisfying strategic sequencing (scout → SEAD → strike) rather than
  busywork gating every engagement behind an extra mandatory mission.
- **Simulation complexity vs. fun** (carried over): realistic flight/guidance physics
  can become fiddly; keep tuning "arcade vs. sim" feel as tiers expand.
- **Scope creep** (carried over): the part list is extensive; timebox breadth in
  Phase 3 rather than gold-plating one category before all exist at a basic level.
- **Performance** (carried over): many simultaneous physics-simulated units in a
  combat instance; plan for object pooling from Phase 0 onward per the existing
  coding standards.
