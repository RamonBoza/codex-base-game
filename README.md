# Cozy Social Game

Version actual: `v0.4.0`

Base inicial para un cozy social game 2D en Unity 6.4.

## Que incluye

- Proyecto Unity preparado con la escena `Assets/Scenes/Main.unity`.
- Primer mundo 2D separado por escenas: `PlayerHomestead`, `ForestPassage` y `FrenchCardTown`.
- Personaje base con movimiento en 4 direcciones usando `WASD` o flechas.
- Camara ortografica que sigue al personaje.
- Piezas visuales placeholder editables desde componentes de Unity, para poder empezar sin assets externos.
- Pantalla de login/registro local antes de entrar al mundo.
- Login local con contrasenas hasheadas mediante PBKDF2-SHA256.
- Sesion persistida localmente con token de desarrollo.
- Google SSO real para desktop usando OAuth 2.0, PKCE y callback loopback local.
- Apple SSO marcado como pendiente hasta configurar Apple Developer y backend.
- Spawn autenticado en `PlayerHomestead`.
- `ForestPassage` como bosque grande entre mapas, con diferentes zonas visuales: pinar, riachuelo, claro de setas y ruinas.
- `FrenchCardTown` como ciudad independiente con muralla, puerta oeste, Poker House y Blackjack House.
- Primera vertical slice offline de Poker en `Poker House`.
- Mesa interactiva de Poker dentro del interior de la casa.
- Partida local heads-up contra bot con cartas privadas, comunidad, apuestas, folds, showdown y resultados.
- Roadmap versionado en `ROADMAP.md`.

## Como abrirlo

1. Abre Unity Hub.
2. Selecciona `Add project from disk`.
3. Elige esta carpeta del repo.
4. Abre la escena `Assets/Scenes/Main.unity`.
5. Pulsa Play.
6. Crea una cuenta local, inicia sesion o usa Google SSO.

## Google SSO local

1. Crea un OAuth Client ID en Google Cloud Console con tipo `Desktop app`.
2. Copia el Client ID.
3. Pulsa Play en Unity.
4. Pega el Client ID en el campo `OAuth Client ID (Desktop app)`.
5. Pulsa `Guardar Client ID`.
6. Pulsa `Continuar con Google`.

El flujo abre el navegador del sistema, usa PKCE y recibe el `authorization code` en `http://127.0.0.1:<puerto>`. El cliente no guarda access tokens ni refresh tokens. Esta integracion sirve para desarrollo local; en produccion el backend debera validar tokens, emitir sesiones propias y controlar refresh/revocacion.

## Controles

- `W`, `A`, `S`, `D` o flechas: mover al personaje.
- Las puertas de las casas se activan automaticamente al acercarse.
- En Poker House, `E`: sentarse en la mesa si estas cerca.
- En partida: usa los botones de la UI para pasar/igualar, apostar/subir o retirarte.

## Escenas del mundo

`Main.unity` es la escena de arranque/login y solo contiene el runtime persistente: bootstrap, login, jugador y camara. Tras autenticar, carga la escena inicial `PlayerHomestead`.

- `Assets/Scenes/PlayerHomestead.unity`: casa del jugador con interior, jardin, vallas y salida este hacia el bosque.
- `Assets/Scenes/ForestPassage.unity`: bosque grande que conecta mapas y tiene varias vistas/ambientes internos.
- `Assets/Scenes/FrenchCardTown.unity`: ciudad de cartas con murallas, entrada/salida oeste y edificios de juegos.

Los cambios de escena se hacen con portales automaticos editables. El jugador y la camara persisten entre escenas.

## Edicion del mapa

El mapa ya no se genera proceduralmente en Play. Cada escena de mundo contiene una jerarquia editable bajo `Editable World`:

- `Ground Layer`: suelo, caminos, agua y plaza.
- `Buildings`: casas, puertas y decoracion de pueblo.
- `Nature`: arboles, flores y senales.
- `Collision`: limites invisibles y bloqueos generales.
- `Spawn Points`: puntos de aparicion y destinos de puertas.

Cada casa editable contiene:

- `Exterior View`: la fachada visible desde el pueblo.
- `Interior View`: la version ampliada que se activa al entrar y limita al jugador dentro.
- `Enter Door Trigger` y `Exit Door Trigger`: puertas automaticas editables.

Componentes principales:

- `SceneRect2D`: rectangulo visual editable por color, tamano y orden de dibujo.
- `WorldBlocker2D`: bloqueo editable mediante collider.
- `HouseInterior2D`: cambia entre fachada e interior ampliado de una casa y mantiene al jugador dentro.
- `RuntimeHouseInterior2D`: interior placeholder ampliado para casas de escenas separadas, con entrada/salida automaticas.
- `PokerMatchController2D`: mesa de Poker offline, transicion a modo partida y UI de mano local.
- `DoorTransition2D`: puerta con destino, bloqueo/desbloqueo y activacion automatica por trigger.
- `ScenePortal2D`: portal automatico entre escenas con escena destino y spawn destino configurables.
- `PlayerSpawnPoint2D`: marcador de aparicion o destino.
- `WorldMapRegion2D`: marcador editable de region/mapa para Player Homestead, Forest Passage y French Card Town.
- `WorldAreaMood2D`: zona editable que cambia paleta de camara y musica de ambiente al entrar en una ciudad.

## Scripts principales

- `Assets/Scripts/CozyWorldBootstrap.cs`: conecta la escena editable, el jugador y la camara tras login.
- `Assets/Scripts/PlayerMovement2D.cs`: movimiento 2D del personaje.
- `Assets/Scripts/CameraFollow2D.cs`: seguimiento suave de camara.
- `Assets/Scripts/Poker/PokerRules.cs`: reglas offline de Poker y evaluador de manos.
- `Assets/Scripts/Poker/PokerMatchController2D.cs`: mesa interactiva y UI de partida offline.

## Estado de version

`v0.3.0` cierra el prototipo de exploracion por escenas: el jugador entra en `PlayerHomestead`, cruza `ForestPassage` y llega a `FrenchCardTown`, donde puede acercarse a Poker House o Blackjack House.

`v0.4.0` anade la primera vertical slice de juego: el jugador puede entrar en Poker House, sentarse en una mesa, jugar una mano offline contra un bot y volver al interior.
