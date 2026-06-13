# Library Drop-In Folder

Place external modding libraries here:
- `BepInEx.dll`
- `0Harmony.dll` or the Harmony DLL you use

Why this folder exists:
- the game does not ship with BepInEx/Harmony
- the mod project needs their types at compile time
- keeping them outside `src/` avoids mixing source code with third-party binaries

For Darkwood references, the project also points directly at the game's `Darkwood_Data\Managed` DLLs.
