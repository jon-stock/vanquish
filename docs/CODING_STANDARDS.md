# Vanquish — Project Structure & Coding Standards

## Unity Project Folder Layout

Everything project-specific lives under `Assets/_Project/` (the underscore keeps it
pinned to the top of Unity's Project window). Third-party packages/assets stay outside
this folder so they're never mistaken for project code.

```
Assets/
  _Project/
    Scripts/
      Core/            # Bootstrapping, game state, save/load, service locator/DI
      Data/            # ScriptableObject definitions (the "part catalog")
        Missiles/
        Drones/
        Support/
      Simulation/      # Shared physics/guidance/sensor code used by combat instances (real-time tactical layer)
        Flight/
        Guidance/
        Sensors/         # RCS/IR-based DetectionSensor (physical detection) and the separate
                         # ESM/RWR emissions-detection channel (radar-finding) side by side
        Damage/          # IDamageable, the pure DamageResolver soft-cap math, and the
                         # Damageable MonoBehaviour shared by units and static objectives
      Combat/          # Combat-instance-scene logic: engagement setup, stockpile/loadout,
                        # target-type objectives (supply line/factory/warehouse/base),
                        # win/lose, HUD bindings. Attack and defense reuse the same engine here.
      Control/         # Direct third-person manual flight control rig (player input -> FlightBody),
                        # shared by any drone or command-guided/datalink missile the player pilots
      Theatre/          # Turn-based strategic layer: front line/territory model, turn resolution,
                        # surveillance/intel (strategic fog-of-war), production & supply-line
                        # simulation, army building/design-assignment (absorbs the old "Workshop"
                        # design-editor activity), theatre-level AI opponent
        Personnel/      # Hireable operator roster, per-category experience/rank, bases
                        # (build/upgrade/relocate-from-front), permadeath-on-base-destruction
      AI/              # CPU opponent behavior (tactical-instance AI; Theatre/ holds strategic AI)
      UI/              # Shared UI Toolkit components/utilities
    Prefabs/
      Missiles/
      Drones/
      Support/
      Environment/
    Scenes/
      Theatre.unity
      Combat_TestRange.unity
      Combat_Base01.unity
      Combat_SupplyLine01.unity
      Combat_Factory01.unity
      Combat_Warehouse01.unity
      Combat_RadarSite01.unity
    Art/
      Models/
      Materials/
      VFX/
      Audio/
    Data/              # ScriptableObject *assets* (instances), mirrors Scripts/Data structure
      Missiles/
      Drones/
      Support/
      TechTree/
  Settings/            # URP render pipeline assets (Unity-managed)
```

Note: `Simulation/` remains mode-agnostic per the rule below, but "mode" now means
*combat instance vs. workshop test-fire* is replaced by *combat instance only* — the
Theatre layer is turn-based and does not touch `Simulation/` directly; it only reads/
writes stockpile and unit-design data that a combat instance then consumes.

## Coding Standards

- **Namespaces**: `Vanquish.<Area>` matching folder structure, e.g. `Vanquish.Data.Missiles`,
  `Vanquish.Simulation.Guidance`, `Vanquish.AI`.
- **ScriptableObjects** define static part data (stats, unlock requirements). They must
  contain **no runtime state** — only configuration. Runtime state (current fuel, health,
  lock status) lives on MonoBehaviours/components that reference the ScriptableObject.
- **Simulation code is mode-agnostic**: nothing under `Simulation/` may reference
  `Combat/` or `Theatre/` types. Any place a design is fired/flown (a combat instance,
  or a future test-range scenario) consumes `Simulation/` the same way, so behavior is
  consistent regardless of which target type or attack/defense role triggered it.
- **No magic numbers in gameplay code** — all tunable values come from ScriptableObject
  fields or a central `BalanceConfig` asset, so balancing never requires code changes.
- **Interfaces over concrete coupling** for cross-cutting systems, e.g. `IGuidanceLaw`,
  `ISeeker`, `IDetectable`, `IDamageable` — enables swapping/upgrading behavior via
  research unlocks without branching code.
- **Object pooling required** for anything spawned per-shot/per-frame (projectiles,
  hit VFX, tracer effects) — no `Instantiate`/`Destroy` in hot paths post-Phase 0.
- **Unit tests** (Unity Test Framework, EditMode) for pure-logic systems: stat
  aggregation, tech-tree unlock gating, detection probability math, save/load
  round-tripping. PlayMode tests for guidance law convergence (does a missile with
  proportional nav actually close on a moving target within tolerance).
- **Assembly Definitions (`.asmdef`)** should be introduced once the codebase grows
  past Phase 1, split at minimum into `Vanquish.Data`, `Vanquish.Simulation`,
  `Vanquish.Runtime`, `Vanquish.Editor`, `Vanquish.Tests` to keep compile times sane.
- **Commit hygiene**: `.gitignore` already excludes `Library/`, `Temp/`, `obj/`, build
  output, and IDE files. Only source-controlled: `Assets/`, `Packages/`,
  `ProjectSettings/`, `.gitignore`, docs.

## Git Branching (lightweight, adjust as the team grows)

- `main` — always buildable/playable.
- Feature branches per phase item, e.g. `feature/missile-data-schema`,
  `feature/pursuit-guidance`.
- Merge via PR once self-review is done (even solo — keeps history readable).
