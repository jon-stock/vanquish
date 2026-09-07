# Agent Notes — Vanquish

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

## Commit policy

Commit changes to git (and push to `origin`) after each set of changes you make to
this repo, not just at the end of a long session. Keep commits reasonably scoped to
the change just made (e.g. one commit per Phase 0 task or logical unit of work,
not one giant commit for everything). Write a concise commit message describing what
changed and why, consistent with the rest of this repo's commit history.
