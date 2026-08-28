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
      Simulation/      # Shared physics/guidance/sensor code used by both Workshop test-fire and Combat
        Flight/
        Guidance/
        Sensors/
      Combat/          # Combat-scene-only logic (spawning, win/lose, HUD bindings)
      Workshop/         # Workshop/tech-tree UI and design-editor logic
      AI/              # CPU opponent behavior
      UI/              # Shared UI Toolkit components/utilities
    Prefabs/
      Missiles/
      Drones/
      Support/
      Environment/
    Scenes/
      Workshop.unity
      Combat_TestRange.unity
      Combat_Arena01.unity
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

## Coding Standards

- **Namespaces**: `Vanquish.<Area>` matching folder structure, e.g. `Vanquish.Data.Missiles`,
  `Vanquish.Simulation.Guidance`, `Vanquish.AI`.
- **ScriptableObjects** define static part data (stats, unlock requirements). They must
  contain **no runtime state** — only configuration. Runtime state (current fuel, health,
  lock status) lives on MonoBehaviours/components that reference the ScriptableObject.
- **Simulation code is mode-agnostic**: nothing under `Simulation/` may reference
  `Combat/` or `Workshop/` types. Both modes consume `Simulation/` the same way, so a
  design tested in the Workshop behaves identically in real Combat.
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

## Headless Testing Workflow (use this proactively during development)

Unity supports running fully headless via the command line, which is far faster and
more reliable for verifying gameplay/physics logic than asking a human to click Play
and read the Console. Use this workflow whenever validating simulation behavior
(flight, guidance, combat outcomes, etc.) rather than relying solely on manual testing:

1. **Scene construction via script, not by hand.** Write an Editor script under
   `Assets/_Project/Scripts/Editor/` with a `[MenuItem]`-decorated static method that
   builds the test scene programmatically (`GameObject.CreatePrimitive`, `AddComponent`,
   `EditorSceneManager.SaveScene`). This makes scenes reproducible, diffable, and
   rebuildable without manual GameObject placement. See `Phase0TestSceneBuilder.cs`
   for the pattern.

2. **Run scene-building/compilation checks headlessly:**
   ```
   & "<UnityEditorPath>\Unity.exe" -batchmode -quit -nographics -projectPath "<repoRoot>" \
     -executeMethod Vanquish.EditorTools.YourBuilder.BuildMethod -logFile "<logPath>"
   ```
   Requires the Editor to be fully closed first (only one process may hold the
   project's `Temp/UnityLockfile` at a time — delete that file if a stale lock remains
   after a crash). Check the log for `error CS`, `Exception`, or `Aborting batchmode`
   to catch compile/runtime errors immediately instead of waiting on manual reports.

3. **Run actual Play-mode simulation headlessly** for behavior verification (does the
   missile actually hit the target, does a battle actually resolve, etc.) using a
   runner pattern like `Phase0BatchRunner.cs`: open the scene, set
   `EditorApplication.isPlaying = true`, subscribe to `EditorApplication.update` to poll
   elapsed time, then call `EditorApplication.isPlaying = false` followed by
   `EditorApplication.Exit(0)` once a fixed duration has passed (don't pass `-quit` for
   this mode — the method must return control to the loop first). This runs the real
   physics/game loop without any window or human interaction, and all `Debug.Log` calls
   land in the log file for direct inspection.

4. **Prefer this over asking the user to manually test** whenever a change is purely
   about simulation/gameplay logic correctness. Reserve manual Play-mode testing for
   things that genuinely need a human (visual/art review, feel/juice, input handling).
   This was validated during Phase 0: a chain of bugs (fake-null `??` operator, a baked
   mesh-orientation correction that also rotated the physics thrust axis, gravity being
   force-enabled in `Awake`, and a guidance gain far too weak to use the airframe's real
   G-limit) was diagnosed and fixed in a few iterations using headless runs with dense
   telemetry logging, instead of many slow rounds of "user clicks Play, describes what
   they saw, repeat."

## Git Branching (lightweight, adjust as the team grows)

- `main` — always buildable/playable.
- Feature branches per phase item, e.g. `feature/missile-data-schema`,
  `feature/pursuit-guidance`.
- Merge via PR once self-review is done (even solo — keeps history readable).
