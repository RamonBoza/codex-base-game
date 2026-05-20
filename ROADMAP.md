# Cozy Social Game Roadmap

## High-Level Vision

2D top-down cozy social game inspired by classic handheld RPG exploration and farming/social life games.

Players explore a connected cozy world made of themed towns. Each town specializes in a category of games, and each individual game lives inside its own building.

Core gameplay split:

- 80% playing games against other players.
- 10% housing and cozy progression.
- 10% social exploration and chill world traversal.

Long-term pillars:

- Modular game houses: Poker, Blackjack, Chess, Checkers, Ludo, and future games.
- Real-time multiplayer.
- Ranking/ELO per game.
- Seasonal resets and rewards.
- Cosmetics and housing progression.
- Server-authoritative critical gameplay.

## Versioning Strategy

The game stays in `0.x` while core systems are still being proven. Each minor version should be a playable milestone with a clear deliverable.

- `v0.x`: prototypes, vertical slices, alpha foundations.
- `v1.0`: first complete public release with stable social hub, multiple playable games, rankings, progression, and seasons.

## Milestones

### v0.1 - Unity Cozy World Foundation

Status: Released as `v0.1.0`.

Goal: Establish the Unity project and first playable exploration base.

Includes:

- Unity 6.4 2D project structure.
- Initial top-down scene.
- Base player movement in four directions.
- Camera follow.
- First generated cozy map with houses, paths, forest, water, and collision.
- GitHub repository connected through SSH.

Done when:

- Project opens in Unity.
- Player can move around the starter map.
- Changes are committed and pushed to GitHub.

Release notes:

- Product version set to `0.1.0`.
- Initial generated world, player movement, camera follow, 2D physics package, roadmap, and Unity Codex skill are versioned in GitHub.

### v0.2 - Identity & Authentication Foundations

Status: Released as `v0.2.0`.

Goal: Allow players to authenticate and enter the game world.

Client:

- Login screen.
- Google SSO for desktop using OAuth 2.0, PKCE, system browser, and local loopback callback.
- Apple SSO pending until Apple Developer and backend identity are configured.
- Email/password form.
- Authenticated transition into the world scene.

Identity foundation:

- Local identity service with a backend-ready shape.
- User persistence in Unity persistent data.
- PBKDF2-SHA256 password hashing for local email/password accounts.
- Development session token generation.
- Google identity provider linking by provider subject.

Done when:

- A new player can register.
- A returning player can log in.
- Unity receives a valid session token.
- Authenticated player reaches the game world.

Release notes:

- World generation is gated behind authentication.
- Email/password register and login work locally with PBKDF2 password hashing.
- Google sign-in works locally with a Desktop app OAuth Client ID.
- Apple sign-in remains pending until real provider credentials and backend endpoints exist.

Explicitly not included:

- Multiplayer.
- Ranking.
- Housing progression.
- Matchmaking.
- Real gameplay.

### v0.3 - World Exploration Prototype

Status: Implemented in `v0.3.0`.

Goal: Build the cozy world structure and exploration loop.

World:

- Player house as spawn point.
- Connected paths between map regions.
- Exploration areas between regions.
- Initial themed town: French Card Town.
- Scene-based world loading: one Unity scene per city/major area.

Town:

- Distinct visual identity.
- Palette shift when approaching town.
- Music transition when approaching town.
- Visible buildings for Poker House and Blackjack House.

Interaction:

- Enter and leave buildings.
- Explore town interiors.
- Reach Poker House from the player house.

Done when:

- Player logs in.
- Player spawns in their house.
- Player can explore the world.
- Player can reach French Card Town.
- Player can enter Poker House.
- Player can enter and leave placeholder interiors for the first houses.
- Player can move between city and forest scenes through configured entrances/exits.

Explicitly not included:

- Real game rules.
- Online matches.
- ELO.
- Housing systems.
- Persistence beyond minimal profile.

### v0.4 - Poker Vertical Slice Offline

Status: Implemented in `v0.4.0`.

Goal: Create the first complete playable gameplay loop.

Core loop:

- Explore world.
- Enter Poker House.
- Sit at a table.
- Transition into match mode.
- Play Poker against a bot.
- Return to exploration.

Poker House:

- Interior with tables.
- Dealer NPC placeholder.
- Cozy ambience.
- Table interaction prompt.
- Runtime placeholder Poker table in the Poker House interior.

Match flow:

- Smooth camera transition or zoom.
- Match UI appears.
- Exploration controls disabled during match.
- Match result screen.
- Return to world.

Poker gameplay:

- Offline/local poker engine.
- Turns.
- Cards.
- Betting rounds.
- Winner determination.
- Simple AI opponent.
- Lightweight heads-up Texas Hold'em slice with community cards and local bot decisions.

Client states:

- Exploration.
- TableLobby.
- InMatch.
- MatchResults.

Done when:

- Player can walk to Poker House.
- Player can sit at a table.
- Player can play a full poker match against a bot.
- Player can return to the world after results.

Release notes:

- Product version set to `0.4.0`.
- Poker House now contains an interactive table.
- The player can sit with `E`, play a local Poker hand against a bot, see results, start another hand, or return to exploration.

Explicitly not included:

- Real multiplayer.
- ELO.
- Matchmaking.
- Seasons.
- Cosmetics.
- Housing progression.

### v0.5 - Backend Match Framework

Goal: Prepare the server architecture for online games before wiring Poker fully online.

Backend:

- Kotlin or Java backend.
- Spring Boot or Ktor decision finalized.
- Hexagonal architecture baseline.
- User/profile domain boundaries.
- Match domain boundaries.
- WebSocket gateway prototype.
- Event-friendly internal structure.

Shared match model:

- Match lifecycle.
- Player actions.
- Server state snapshots.
- Result events.
- Error handling.

Done when:

- Backend can create a test match.
- Backend can accept a WebSocket connection.
- Backend can broadcast a simple authoritative match state.
- Unity can connect to the backend in a development environment.

Explicitly not included:

- Production matchmaking.
- Poker ELO.
- Seasons.
- Anti-cheat beyond server-owned state model.

### v0.6 - Online Poker + Matchmaking + ELO

Goal: Convert Poker into a real online multiplayer game.

Matchmaking:

- Queue for Poker.
- Cancel queue.
- Match found event.

Online Poker:

- Server-authoritative rules.
- Server-owned RNG and card dealing.
- Client sends actions only.
- WebSocket state updates.
- Reconnection handling baseline.

Ranking:

- Poker-specific ELO.
- Win/loss tracking.
- Post-match ELO delta.

Done when:

- Two players can queue online.
- Two players can play Poker against each other.
- Match outcome is decided by the server.
- Both players see result and ELO change.

Explicitly not included:

- Seasons.
- Housing progression.
- Cosmetics.
- Additional games.

### v0.7 - Identity, Profiles & Housing v1

Goal: Add cozy identity and progression.

Profiles:

- Display name.
- Avatar placeholder.
- Game stats.
- Public profile summary.

Housing:

- Basic editable player house.
- Simple furniture placement.
- Save/load house layout.

Rewards:

- Soft currency.
- Cosmetic unlocks.
- Furniture rewards from play.

Done when:

- Player can customize basic identity.
- Player can decorate their house.
- Player earns at least one reward from gameplay.
- Player progression persists.

### v0.8 - Second Game Framework Validation

Goal: Prove the architecture scales beyond Poker.

Adds:

- Blackjack House.
- Blackjack gameplay.
- Blackjack matchmaking.
- Blackjack-specific ELO.

Reuses:

- House/table interaction flow.
- Matchmaking framework.
- WebSocket match model.
- Backend domain patterns.
- Results flow.

Done when:

- Player can enter Blackjack House.
- Player can queue for Blackjack.
- Player can play online Blackjack.
- Blackjack has independent ranking from Poker.

### v0.9 - Seasons, Rankings & Anti-Cheat Foundations

Goal: Add long-term retention and competitive structure.

Seasons:

- Quarterly season model.
- Soft ELO reset.
- Season metadata.

Rankings:

- Leaderboards per game.
- Leaderboards per season.
- Player rank display.

Rewards:

- Seasonal cosmetics.
- Housing decorations.
- Titles or badges.

Anti-cheat foundations:

- Server-authoritative gameplay enforced for critical logic.
- Suspicious pattern logging.
- Basic player reports.

Done when:

- Rankings are visible.
- Season state exists.
- Players can receive seasonal rewards.
- Suspicious match patterns can be logged for review.

### v1.0 - First Public Release

Goal: Ship the first complete public version of the cozy competitive social hub.

Release scope:

- Stable login.
- Cozy world hub.
- Player house.
- French Card Town.
- Poker House.
- Blackjack House.
- Online Poker.
- Online Blackjack.
- ELO per game.
- Seasonal rankings.
- Basic profile.
- Basic housing progression.
- Server-authoritative match logic.

Done when:

- New users can onboard without developer help.
- Core loop is stable from login to match to rewards.
- Backend and client have deploy/release process.
- Critical game logic is server-owned.
- Known release-blocking bugs are resolved.

## Architecture Principles

### Game Modules

Each game should be modular:

- Poker module.
- Blackjack module.
- Future Chess module.
- Future Checkers module.
- Future Ludo module.

Each module owns:

- Rules.
- Match state.
- Player actions.
- UI mapping.
- Results.
- Ranking integration.

### World Structure

Each town contains buildings dedicated to games.

Reusable flow:

- Explore.
- Enter house.
- Sit at table.
- Matchmaking or local match start.
- Play.
- Results.
- Return to world.

### Server Authority

Critical logic must live server-side:

- Match rules.
- RNG.
- Card dealing.
- Legal action validation.
- Match outcomes.
- Ranking updates.

The client should present state and request actions, not decide authoritative outcomes.

## Current Next Step

Recommended next milestone: `v0.5 - Backend Match Framework`.

`v0.4` proves the first offline match loop inside Poker House. The next product risk is preparing the backend match architecture before converting Poker to online multiplayer.
