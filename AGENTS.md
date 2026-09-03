# Vanquish — Agent Notes

## Unity Editor

- Installed Editor (matches this project's `ProjectSettings/ProjectVersion.txt`,
  `6000.0.5f1`): `C:\Users\Jon.Stock\UnityEditors\6000.0.5f1\Editor\Unity.exe`
- Run headless/batch commands (e.g. scene builders, validation `[MenuItem]`s) via:
  ```
  & "C:\Users\Jon.Stock\UnityEditors\6000.0.5f1\Editor\Unity.exe" -batchmode -quit -nographics `
    -projectPath "C:\Users\Jon.Stock\OneDrive - Access UK Ltd\Customers\Development\CSharp\Vanquish" `
    -executeMethod <Namespace>.<Class>.<Method>
  ```
  (drop `-quit`/`-nographics` for a normal interactive Editor session; drop
  `-executeMethod ...` entirely to just open the Editor UI).
- The Unity Hub CLI shim at `C:\Users\Jon.Stock\AppData\Local\Unity\bin\unity.exe`
  is **not** the actual Editor binary — use the path above instead.
