# _UnityScaffold

Staging area for Phase 0 C# scripts, written before the Unity project itself existed.

Once you've created the Unity project (3D URP template) directly in the repo root
(`Vanquish/`), move the contents of `_UnityScaffold/Assets/` into the new project's
`Assets/` folder, then delete this `_UnityScaffold/` directory:

```powershell
# from the Vanquish repo root, after creating the Unity project here
Move-Item -Path "_UnityScaffold\Assets\_Project" -Destination "Assets\_Project"
Remove-Item -Recurse -Force "_UnityScaffold"
```

Contents map 1:1 to the folder structure described in `docs/CODING_STANDARDS.md`.
