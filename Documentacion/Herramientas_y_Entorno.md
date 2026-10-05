# Herramientas y entorno de desarrollo

Todo lo que se usó e instaló para desarrollar *Ecos del Grimorio*.

## Equipo

| Elemento | Detalle |
|---|---|
| Sistema operativo | Windows 11 de 64 bits |
| Control de pruebas | DualShock 4 (PS4), conectado por Bluetooth |

## Motor y paquetes de Unity

| Herramienta | Versión | Para qué se usa |
|---|---|---|
| Unity Hub | Actual | Instalar el editor y abrir el proyecto |
| Unity Editor | 6000.6.0f1 (Unity 6) | Motor del juego, plantilla **2D URP** |
| Universal Render Pipeline | 17.6.0 | Render 2D y luces 2D (antorchas, lava, portal, cristales) |
| Input System | 1.20.0 | Teclado, mouse y control de PS4 / Xbox, con vibración |
| Unity UI (UGUI) | 2.6.0 | HUD, menús, diálogos, pausa, logros, grimorio y créditos |
| 2D Sprite | 1.0.0 | Importación y corte de sprites |
| Visual Studio Editor | 2.0.26 | Conecta Unity con Visual Studio Code |

**Configuración en Unity:** *Edit > Preferences > External Tools*:

1. En *External Script Editor* elegir **Visual Studio Code**.
2. Dar clic en **Regenerate project files**.

## Editor de código

| Herramienta | Versión | Para qué se usa |
|---|---|---|
| Visual Studio Code | Actual | Editar los scripts C# y manejar el repositorio |
| .NET SDK | 10.0.401 | Lo necesitan las extensiones de C# |

### Extensiones de Visual Studio Code

| Extensión | Autor | Para qué se usa |
|---|---|---|
| C# | Microsoft | Colores, autocompletado y errores en los scripts |
| C# Dev Kit | Microsoft | Explorador de la solución y herramientas de C# |
| Unity | Microsoft | Integración con Unity (depurar y reconocer las clases de Unity) |
| Spanish Language Pack for Visual Studio Code | Microsoft | Interfaz de VS Code en español |

## Control de versiones

| Herramienta | Para qué se usa |
|---|---|
| Git para Windows | Historial de cambios del repositorio |
| GitHub | Repositorio en línea: https://github.com/AlexAP1115web/Ecos-Del-Grimorio |
| Control de código fuente de VS Code | Hacer commits, *Publish Branch* y *Sync Changes* |

## Arte

| Herramienta | Para qué se usa |
|---|---|
| GIMP | Editar y recortar las imágenes de personajes, enemigos, ítems y fondos |

Las texturas de ladrillo, tablas, bordes, páginas, vasijas, grietas, paneles de la interfaz y decoración de los biomas se generan desde el script `EditorHelpers.cs` al correr **Ecos del Grimorio > Crear juego completo**.

## Audio

La música, las voces y los efectos de sonido son originales y se generaron por síntesis de audio con los scripts de `Codigo/Herramientas/Audio`.

| Herramienta | Para qué se usa |
|---|---|
| Python 3 | Lenguaje de los scripts de audio |
| NumPy y SciPy | Síntesis de las ondas, filtros y reverberación |
| FFmpeg | Convertir la música de WAV a OGG |

**Para volver a generar el audio:**

```
pip install numpy scipy
python musica.py <carpeta_salida>
python efectos.py <carpeta_salida>
python voces.py <carpeta_salida>
```

La música en WAV se convierte a OGG con FFmpeg, por ejemplo:

```
ffmpeg -i Menu.wav -c:a libvorbis -q:a 5 Menu.ogg
```

| Archivos | Carpeta en Unity |
|---|---|
| 9 pistas de música | `Assets/Audio/Musica` |
| 28 efectos de sonido | `Assets/Audio/Efectos` |
| 62 clips de voz | `Assets/Audio/Voces` |

## Documentación

| Herramienta | Para qué se usa |
|---|---|
| Microsoft Word | Documento de diseño (Producto 1) |
| Microsoft Excel | Documento de producción y gráficas (Productos 1 y 2) |
| Markdown | README, bitácora y documentos del repositorio |
