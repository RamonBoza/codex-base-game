# Changelog

## v0.2.0 - Identity & Authentication Foundations

Release date: 2026-05-17

Adds the first working authentication gate before entering the cozy world.

### Added

- Login/register screen shown before world generation.
- Local identity service with email/password register and login.
- Persistent local session stored through Unity `Application.persistentDataPath`.
- Development session token shaped like a JWT for future backend replacement.
- Google and Apple development-provider buttons.
- Session HUD with logout.

### Notes

- Real Google/Apple SSO and server-side JWT validation are intentionally not wired yet because they require provider credentials and backend deployment.
- The local identity service is structured as a replaceable foundation for the future backend identity service.

### Verification

- Lightweight C# compile check passed against Unity 6.4.7f1 assemblies.
- Full Unity batchmode validation was not run because Unity Editor was already open and holding the project lock.

## v0.1.0 - Unity Cozy World Foundation

Release date: 2026-05-17

Initial playable foundation for the cozy social game.

### Added

- Unity 6.4 2D project structure.
- Main scene at `Assets/Scenes/Main.unity`.
- Runtime-generated starter cozy map with meadow, paths, houses, trees, pond, fences, signs, and collision boundaries.
- Player character placeholder with four-direction movement using `WASD` or arrow keys.
- Orthographic camera follow.
- Required built-in Unity modules for audio and 2D physics.
- Product version set to `0.1.0`.
- Roadmap documentation.
- Versioned Codex Unity 2D skill under `.codex/skills/unity-2d-game-dev`.

### Verification

- Unity generated `Library/ScriptAssemblies/Assembly-CSharp.dll` after opening the project.
- Full Unity batchmode validation was not run because Unity Editor was already open and holding the project lock.
