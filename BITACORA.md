# Bitácora de avances

## Sprint 1 (1 al 18 de septiembre de 2026)

- Documento de diseño con la plantilla Huddle: concepto, mecánicas, niveles, personajes, enemigos, habilidades, armas, ítems, guion, logros y códigos secretos.
- Comparación de metodologías (Waterfall y Scrum) y justificación de Huddle.
- Arte conceptual de personajes, enemigos, jefes, ítems, fondos y UI; limpieza en GIMP.
- Bocetos a lápiz de Lira, Espectro de tinta y Ala de Aprendizaje.
- Documento de producción: Feature Log, Sprint Plan, Sprint Backlog y Buglist.
- Proyecto de Unity 6 (2D URP) creado.

## Sprint 2 (desde el 19 de septiembre de 2026)

### 30 de septiembre
- Se ordenó la carpeta de sprites de Unity y se sacaron las imágenes que no son del catálogo del juego.
- Se agregaron al proyecto los 15 enemigos y jefes y los 12 ítems del documento de diseño.
- Código C# reorganizado en `Data`, `Player`, `Enemigos`, `Managers`, `UI` y `Efectos`:
  - Datos con ScriptableObjects (`SpellData`, `ItemData`, `EnemyData`).
  - Herencia de enemigos: `EnemyBase` → `EnemyAI`, `FlyingEnemyAI`, `RangedEnemyAI`; `BossController` → `KaelorBoss`.
  - `SpellCaster` con tres hechizos equipados y los cuatro combos elementales.
  - `Health` con la interfaz `IDamageable` y resistencias por elemento.
  - `GameManager` con los estados del juego de la sección 2.5.
  - Controles con el New Input System.
- Herramienta de editor para armar el Nivel 1 (Ala de Aprendizaje).

### 4 de octubre
- Arte terminado: 9 hechizos, 4 armas, 10 logros, marco de menú e ícono de logro.
- Arte agregado a Unity en `Sprites/Spells`, `Sprites/Weapons` y `Sprites/Achievements`.
- Se creó este repositorio con los documentos, el arte, el código y las evidencias.

- Juego completo con la herramienta `Ecos del Grimorio > Crear juego completo`:
  - Menú principal (Nueva Partida, Continuar, Logros, Créditos, Salir) con guardado de progreso.
  - Nivel 1, Ala de Aprendizaje: Espectros de tinta, Motas Corruptas y Espectro Mayor; Llave Rúnica y cofre secreto.
  - Nivel 2, Ala de Fuego: Centinelas de Ceniza, Salamandras de Forja y Kaelor.
  - Nivel 3, Ala de Hielo: Espectros Escarchados, Cristales Vivientes e Isolde.
  - Nivel 4, Ala de Viento: Aves de Tormenta, Golems de Piedra Suspendida, plataformas móviles y Threnody.
  - Nivel 5, Corazón del Grimorio: Ecos Menores, Guardián Espejo y el Eco de la Archimaga Elenora (cuatro fases + fase final).
  - Créditos con epílogo según el porcentaje de coleccionables.
  - Sistema de los 10 logros, Piedras de Reaparición y barra de vida de los jefes.

### Pendiente
- Probar los cinco niveles en Unity y ajustar dificultad.
- Subir capturas a `Evidencias`.
- Diagrama de Gantt.
