# Codex Base Game

Base inicial para un cozy game 2D en Unity 6.4.

## Que incluye

- Proyecto Unity preparado con la escena `Assets/Scenes/Main.unity`.
- Primer mapa 2D generado al pulsar Play: pradera, plaza, caminos, casas, bosque, estanque y limites con colision.
- Personaje base con movimiento en 4 direcciones usando `WASD` o flechas.
- Camara ortografica que sigue al personaje.
- Sprites temporales generados por codigo, para poder empezar sin assets externos.

## Como abrirlo

1. Abre Unity Hub.
2. Selecciona `Add project from disk`.
3. Elige esta carpeta del repo.
4. Abre la escena `Assets/Scenes/Main.unity`.
5. Pulsa Play.

## Controles

- `W`, `A`, `S`, `D` o flechas: mover al personaje.

## Scripts principales

- `Assets/Scripts/CozyWorldBootstrap.cs`: genera el mapa, el jugador y la camara.
- `Assets/Scripts/PlayerMovement2D.cs`: movimiento 2D del personaje.
- `Assets/Scripts/CameraFollow2D.cs`: seguimiento suave de camara.
