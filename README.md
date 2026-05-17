# Cozy Social Game

Version actual: `v0.2.2`

Base inicial para un cozy social game 2D en Unity 6.4.

## Que incluye

- Proyecto Unity preparado con la escena `Assets/Scenes/Main.unity`.
- Primer mapa 2D generado al pulsar Play: pradera, plaza, caminos, casas, bosque, estanque y limites con colision.
- Personaje base con movimiento en 4 direcciones usando `WASD` o flechas.
- Camara ortografica que sigue al personaje.
- Sprites temporales generados por codigo, para poder empezar sin assets externos.
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

## Scripts principales

- `Assets/Scripts/CozyWorldBootstrap.cs`: genera el mapa, el jugador y la camara.
- `Assets/Scripts/PlayerMovement2D.cs`: movimiento 2D del personaje.
- `Assets/Scripts/CameraFollow2D.cs`: seguimiento suave de camara.

## Estado de version

`v0.2.2` cierra una base de identidad local mas realista: login/registro con PBKDF2, Google OAuth desktop con PKCE, sesion persistida y entrada autenticada al mundo.
