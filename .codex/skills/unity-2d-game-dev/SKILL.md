---
name: unity-2d-game-dev
description: Work on Unity 6.x 2D game projects, especially top-down cozy/social games. Use when Codex needs to inspect, edit, scaffold, refactor, debug, or validate Unity scenes, C# MonoBehaviours, ProjectSettings, Packages, .meta files, 2D physics, input, cameras, prefabs, tilemaps, UI, or Unity command-line/editor workflows.
---

# Unity 2D Game Development

Use this skill for Unity 6.x 2D projects, with special care for top-down cozy/social games.

## First pass

- Inspect the repository before editing: `Assets/`, `Packages/`, `ProjectSettings/`, active scenes, scripts, and `.gitignore`.
- Treat `Library/`, `Temp/`, `Obj/`, `Logs/`, `.utmp/`, `UserSettings/`, `.vs/`, and `.idea/` as local/generated unless the user explicitly asks otherwise.
- Preserve Unity `.meta` files. When adding Unity assets manually, add matching `.meta` files only when needed; prefer letting Unity generate them for imported binary/editor assets.
- Avoid broad ProjectSettings churn. If Unity generated settings after opening the editor, review them before committing.
- Keep commits scoped by milestone or feature.

## Unity 6 package hygiene

- Check `Packages/manifest.json` before using modules or packages.
- Built-in modules needed by common 2D starter projects include `com.unity.modules.physics2d` for `Rigidbody2D`, `Collider2D`, and related APIs, and `com.unity.modules.audio` for `AudioListener`/audio basics.
- Do not invent package names. Verify package IDs from Unity docs or an existing Unity-generated manifest.
- If Unity reports package resolution errors, inspect both `Packages/manifest.json` and `Packages/packages-lock.json`.

## C# and gameplay code

- Prefer small, focused `MonoBehaviour` scripts with serialized fields for designer-tunable values.
- Use `Rigidbody2D` movement in `FixedUpdate` for physics-backed player movement.
- Read input in `Update`; apply physics in `FixedUpdate`.
- Keep runtime state explicit: Exploration, TableLobby, InMatch, MatchResults, etc.
- Avoid putting authoritative match outcomes in the Unity client for online games. The client should present state and send requested actions; the server owns rules, RNG, validation, and results.
- Do not add large framework abstractions until a second use case proves the shape.

## Scenes, maps, and assets

- For early prototypes, generated placeholder sprites/maps are acceptable if they make the game playable quickly.
- As a milestone matures, move toward editable Unity assets: scenes, prefabs, ScriptableObjects, tile palettes, and serialized configuration.
- Keep top-down non-isometric assumptions unless the user changes the art direction.
- For cozy exploration, separate world concerns: player controller, camera, interactables, scene transitions, map/town data, and UI prompts.
- Prefer explicit spawn points and transition targets when implementing houses/towns.

## Verification workflow

- If Unity Editor is installed and command-line use is possible, prefer batch checks before finalizing substantial changes:
  - Open/compile project with `Unity.exe -batchmode -quit -projectPath <project> -logFile <log>`.
  - Run edit/play mode tests when tests exist.
- If command-line Unity is unavailable, state that verification is limited and ask the user to reopen the project in Unity.
- When the user shares Unity Console errors, map them to package/module issues, C# compile errors, missing references, or scene serialization problems.
- Never claim Unity compilation passed unless Unity or a Unity-compatible compile check actually ran.

## Git workflow for this project

- Check `git status --short --branch` before commits.
- Keep `.idea/`, `.vs/`, `Library/`, `Temp/`, `Obj/`, `Logs/`, and `UserSettings/` out of Git.
- Commit milestone docs and code separately when that makes review easier.
- Use SSH remote when available for GitHub push/pull.

## Current project context

- Project: cozy competitive social hub.
- Unity target: Unity 6.4, 2D top-down, PC first.
- Core loop: explore, enter game house, sit at table, matchmaking/local match, play, results, return to world.
- First game focus: Poker House, then Blackjack House.
- Architecture direction: modular game houses, server-authoritative backend later, ELO per game, seasons, housing progression.
