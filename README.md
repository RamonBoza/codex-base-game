# Cozy Social Game

Version actual: `v0.2.2`

Base inicial para un cozy social game 2D en Unity 6.4.

## Que incluye

- Proyecto Unity preparado con la escena `Assets/Scenes/Main.unity`.
- Primer mapa 2D editable directamente en la escena `Assets/Scenes/Main.unity`: pradera, plaza, caminos, casas, bosque, estanque, limites con colision e interiores ampliados para las casas iniciales.
- Personaje base con movimiento en 4 direcciones usando `WASD` o flechas.
- Camara ortografica que sigue al personaje.
- Piezas visuales placeholder editables desde componentes de Unity, para poder empezar sin assets externos.
- Pantalla de login/registro local antes de entrar al mundo.
- Login local con contrasenas hasheadas mediante PBKDF2-SHA256.
- Sesion persistida localmente con token de desarrollo.
- Google SSO real para desktop usando OAuth 2.0, PKCE y callback loopback local.
- Apple SSO marcado como pendiente hasta configurar Apple Developer y backend.
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

## Edicion del mapa

El mapa ya no se genera proceduralmente en Play. La escena contiene una jerarquia editable bajo `Editable World`:

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
- `DoorTransition2D`: puerta con destino, bloqueo/desbloqueo y activacion automatica por trigger.
- `PlayerSpawnPoint2D`: marcador de aparicion o destino.

## Scripts principales

- `Assets/Scripts/CozyWorldBootstrap.cs`: conecta la escena editable, el jugador y la camara tras login.
- `Assets/Scripts/PlayerMovement2D.cs`: movimiento 2D del personaje.
- `Assets/Scripts/CameraFollow2D.cs`: seguimiento suave de camara.

## Estado de version

`v0.2.2` cierra una base de identidad local mas realista: login/registro con PBKDF2, Google OAuth desktop con PKCE, sesion persistida y entrada autenticada al mundo.
