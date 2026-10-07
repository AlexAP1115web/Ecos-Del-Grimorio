# Estado del Sprint 2

## Arte en Unity

| Carpeta | Contenido |
|---|---|
| `Sprites/Characters` | Lira, Maestra Sable |
| `Sprites/Enemies` | 11 enemigos comunes y 4 jefes |
| `Sprites/Items` | 12 ítems |
| `Sprites/Backgrounds` | 5 fondos de nivel |
| `Sprites/UI` | Barras de vida y maná, marcos, ícono de logro |
| `Sprites/Spells` | 4 hechizos, 4 combos y esquive |
| `Sprites/Weapons` | 4 armas de enemigos |
| `Sprites/Achievements` | 10 logros |
| `Sprites/Sketches` | Bocetos |

## Código

| Carpeta | Scripts |
|---|---|
| `Data` | `Elemento`, `SpellData`, `ItemData`, `EnemyData` |
| `Player` | `PlayerController`, `SpellCaster`, `SpellProjectile`, `PlayerUpgrades`, `ModoArchimaga` |
| `Enemigos` | `EnemyBase`, `EnemyAI`, `EcoMenorAI`, `FlyingEnemyAI`, `RangedEnemyAI`, `MirrorEnemyAI`, `ColosoAI`, `GargolaAI`, `ArmaEnemigo`, `EnemyProjectile`, `BossController`, `KaelorBoss`, `IsoldeBoss`, `ThrenodyBoss`, `ElenoraBoss` |
| `Managers` | `GameManager`, `AchievementManager`, `Health`, `Pickup`, `Checkpoint`, `SecretChest`, `Cofre`, `CameraFollow`, `LevelExit`, `KillZone` |
| `UI` | `UIManager`, `HUD`, `NPCDialogue`, `MainMenu`, `OpcionesUI`, `Prologo`, `GrimorioUI`, `AchievementsList`, `CreditsScreen` |
| `Efectos` | `AreaEffect`, `Particula`, `Parallax`, `ParticulasAmbiente`, `LuzParpadeante`, `EstelaProyectil`, `SlowZone`, `MovingPlatform`, `Rompible`, `Desvanecer`, `Flotar`, `GeiserFuego`, `Carambano`, `CorrienteViento`, `Empujable`, `PlacaPresion`, `PuertaRunica` |
| `Audio` | `AudioManager`, `SonidoBoton` |
| `Sistema` | `Controles`, `VibracionControl`, `CapturaPantalla` |
| `Editor` | `CrearJuego`, `EditorHelpers`, `ConfigureSprites`, `Herramientas` |

## Programación Orientada a Objetos

- **Herencia:** `EnemyBase` es abstracta y cada tipo de enemigo solo implementa su comportamiento en `Think()`. `EcoMenorAI` hereda de `EnemyAI` y le agrega disparos. `BossController` agrega fases por porcentaje de vida, y cada jefe (`KaelorBoss`, `IsoldeBoss`, `ThrenodyBoss`, `ElenoraBoss`) define sus propios ataques.
- **Polimorfismo:** el juego trata a todos los enemigos como `EnemyBase` (aturdir, ralentizar, empujar) sin importar su tipo.
- **Interfaces:** `IDamageable` permite que cualquier objeto reciba daño.
- **Encapsulamiento:** los campos son privados con `[SerializeField]` y se exponen con propiedades de solo lectura.
- **Eventos:** `UnityEvent` para vida y maná (HUD), `Action` para desbloqueo de hechizos, combos, jefes derrotados y el Modo Archimaga.
- **Datos separados de la lógica:** los valores de hechizos, ítems y enemigos viven en ScriptableObjects.

## Escenas

| Escena | Contenido |
|---|---|
| MenuPrincipal | Nueva Partida, Continuar, Opciones, Controles, Logros, Créditos, Salir |
| Prologo | Historia inicial con imágenes |
| Nivel1_AlaDeAprendizaje | Maestra Sable, Espectros de tinta, Motas Corruptas, Espectro Mayor, Llave Rúnica y cofre secreto. Desbloquea Fuego |
| Nivel2_AlaDeFuego | Centinelas de Ceniza, Salamandras de Forja, Kaelor (2 fases). Desbloquea Hielo |
| Nivel3_AlaDeHielo | Espectros Escarchados, Cristales Vivientes, Isolde (congela el suelo). Desbloquea Viento |
| Nivel4_AlaDeViento | Aves de Tormenta, Golems, plataformas móviles, Threnody (vuela, empuja e invoca aves) |
| Nivel5_CorazonDelGrimorio | Ecos Menores, Guardián Espejo, Eco de la Archimaga Elenora (4 fases elementales + final) |
| Epilogo | Final de la historia después de vencer a Elenora |
| Creditos | Epílogo según el porcentaje de coleccionables y créditos |

Las herramientas, paquetes y extensiones que se usaron están en `Documentacion/Herramientas_y_Entorno.md`.

Las tablas de producción del Sprint 2 (backlog, gráficas, Buglist, pruebas Alfa y Sprint Review) están en `PérezAlcántaraAlejandro_producción_10D_Sprint2.xlsx`.
