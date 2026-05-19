# Changelog

## Unreleased

### Added

- Added the `v0.3.0` exploration loop: authenticated players now spawn in Player Homestead before entering the wider world.
- Added an editable French Card Town mood zone that shifts camera palette and crossfades procedural ambient music as the player approaches town.
- Added French Card Town hierarchy markers in the scene for designer-editable town tuning.
- Added editable map region markers for Player Homestead, Forest Passage, and French Card Town.
- Added separate world scenes for `PlayerHomestead`, `ForestPassage`, and `FrenchCardTown`.
- Added `ScenePortal2D` automatic scene portals with target scene and spawn configuration.
- Added `RuntimeHouseInterior2D` interiors for Player House, Poker House, and Blackjack House in the separated scene flow.

### Changed

- Replaced the runtime procedural starter map with an editable Unity scene hierarchy under `Editable World`.
- Converted visual map pieces, blockers, doors, and spawn points into inspector-editable components.
- Added in-place expanded house interiors for the initial houses, toggled by editable door triggers.
- Made house enter/exit triggers automatic and clamped the player inside active interiors.
- Kept `CozyWorldBootstrap` as an authentication/world activation coordinator instead of a map generator.
- Reworked the starter world layout into visibly separated map regions connected by forest paths.
- Reworked exploration loading so the login scene keeps player/camera persistent and loads world scenes through Unity `SceneManager`.
- Cleaned `Main.unity` so it no longer contains the old world map.
- Updated the authenticated start spawn to begin inside Player House.
- Updated product version to `0.3.0`.

## v0.2.2 - Google OAuth and Password Hardening

Release date: 2026-05-17

### Added

- Added real Google OAuth 2.0 desktop sign-in using the system browser, PKCE, state validation, and a local loopback callback.
- Added Google profile persistence fields for provider subject and avatar URL.
- Added local Google OAuth Client ID storage through Unity `PlayerPrefs`.
- Added the Unity built-in `com.unity.modules.unitywebrequest` module required for OAuth token/profile requests.

### Changed

- Email/password accounts now use PBKDF2-SHA256 with per-user salts instead of the earlier development SHA256 hash.
- Legacy local password hashes are upgraded to PBKDF2 after successful login.
- Apple SSO is now shown as pending instead of pretending to be a complete provider.
- Updated product version to `0.2.2`.

### Notes

- Google SSO is real for local desktop development, but production identity still needs backend-side token validation, refresh handling, and first-party session issuance.

## v0.2.1 - JSONSerialize Hotfix

Release date: 2026-05-17

### Fixed

- Added the Unity built-in `com.unity.modules.jsonserialize` module required by `JsonUtility` in `LocalIdentityService`.
- Updated product version to `0.2.1`.

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
