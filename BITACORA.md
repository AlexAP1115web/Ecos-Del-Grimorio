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

- Primera prueba en Unity: el juego corre, pero el escenario se veía plano y las barras del HUD no cargaban su imagen.
- Versión 2 del juego:
  - Soporte para control de PS4 / Xbox (Input System) con vibración, además del teclado.
  - Movimiento con aceleración, coyote time, búfer de salto y esquive con invulnerabilidad.
  - Animación por código de los personajes (estirar, aplastar, inclinar) y efectos de golpe: partículas, temblor de cámara, retroceso y barras de vida en los enemigos.
  - Escenarios con texturas que se repiten, fondo con parallax, columnas o nubes, luces 2D (antorchas, lava, portal) y partículas de ambiente por cada ala.
  - Interfaz nueva con UGUI: diálogos con retrato, menú de pausa, Game Over, logros, avisos y título de cada nivel.

- Historia dentro del juego:
  - Prólogo con imágenes al empezar una partida nueva.
  - La Escena 1 del guion (Lira y Maestra Sable) al inicio del Ala de Aprendizaje.
  - Pensamientos de Lira al entrar a cada ala y susurros de los ecos en el camino.
  - Diálogo de cada jefe antes y después del combate.
  - Los Fragmentos de Grimorio, la Nota cifrada y el Diario revelan la historia de Elenora.
- Textos de la interfaz más grandes y diálogos con varios personajes y retratos.

### 5 de octubre
- Ajuste de dificultad después de probar con el control:
  - Lira tiene 150 de vida, recibe 35% menos daño y su invulnerabilidad después de un golpe dura más.
  - El maná se recupera más rápido, las pociones curan más y los enemigos sueltan cristales y pociones más seguido.
  - Los cuatro jefes tienen menos vida.
- Lira puede agacharse: pasa por pasadizos bajos y sus hechizos salen a ras de suelo contra enemigos pequeños.
- Modo Archimaga también desde el control (L1 + R1 + Triángulo).
- Música para el menú, cada ala, los jefes y los créditos, más 23 efectos de sonido (saltos, hechizos, golpes, cofres, menús, logros).
- Niveles más grandes y con cosas que buscar:
  - Salas secretas detrás de muros agrietados que se rompen con un hechizo.
  - Pasadizos bajos que solo se cruzan agachada.
  - Rutas de plataformas hacia lo alto de cada ala.
  - Vasijas que se rompen y cofres de madera con cristales y pociones.
  - Páginas Perdidas: tres por ala; cada una da +5 de vida máxima.
- Pantalla de Controles en el menú principal y en la pausa.

- Segunda prueba con el control:
  - No se escuchaba nada porque las cámaras no tenían AudioListener; ahora lo trae el AudioManager.
  - Los textos de Controles y Logros se salían del marco: ahora usan un panel que se estira sin deformarse.
- Voces de los personajes al hablar (Lira, Maestra Sable, los guardianes, Elenora y los ecos) y quejidos de Lira al recibir daño, al hacer combos y al caer.
- Lira camina: el sprite se separa en cuerpo y piernas, y las piernas se mueven al correr, saltar, esquivar y agacharse. También tiene sombra y sonido de pasos.
- Retrato del HUD: la cara de Lira ahora queda recortada dentro del marco redondo.
- Las orillas de la pantalla se ponen rojas al recibir daño y laten cuando queda poca vida.

- Final del juego corregido: al derrotar al Eco de Elenora sale "¡VICTORIA!", se entregan sus recompensas, sigue un epílogo con imágenes y luego los créditos. Los créditos ya no se saltan por accidente y "Continuar" ya no regresa a la batalla final.
- Personajes nuevos:
  - Coloso de Raíz (Ala de Viento y Corazón): lento y resistente; sus pisotones se esquivan saltando.
  - Gárgola de Runa (Ala de Hielo y Corazón): parece estatua hasta que Lira se acerca.
  - Proyección de la Maestra Sable antes de cada guardián, con consejos.
  - Espíritus de Kaelor, Isolde y Threnody antes de la batalla final; cada uno da una bendición (vida, maná o vida máxima).
- Biomas mejorados:
  - Aprendizaje: libreros.
  - Fuego: cadenas y géiseres de fuego.
  - Hielo: cristales, carámbanos bajo las plataformas y carámbanos que caen.
  - Viento: pasto, enredaderas y corrientes de aire que impulsan hacia arriba.
  - Corazón: cristales y círculos de runas flotando.
  - Todas las alas: montículos en primer plano para dar profundidad.

- Tercera prueba con el control:
  - Apuntar a cualquier dirección con el stick derecho, o con el mouse al dar clic. Arriba y abajo lanzan en diagonal o en vertical.
  - Cada espacio tiene su propio botón (Cuadrado, R1, L1 / 1, 2, 3) para encadenar combos rápido, y hay un combo rápido con un solo botón (L2 / C).
  - El HUD muestra el botón de cada hechizo y el combo disponible.
  - Los enemigos usan las armas del documento: Garras de Tinta Corrosiva (Espectros), Lanza Incandescente (Centinelas) y Embestida Rocosa (Golems).
  - Pantalla Grimorio en la pausa con hechizos, combos, mejoras, esquive y armas de los enemigos.
  - El Modo Archimaga ya no se reinicia si se presiona otra tecla mientras se escribe el código; en el control funciona con L1 + R1 + Triángulo en cualquier orden.
  - Voces más suaves.

- Créditos corregidos: el texto empezaba fuera de la pantalla y tardaba como 15 segundos en aparecer; ahora se ve desde el inicio y al terminar regresa solo al menú.

- Lo que faltaba del documento de diseño:
  - Pantalla de **Opciones** (2.5 y 2.6) en el menú principal y en la pausa: volumen de música, efectos y voces, pantalla completa y vibración del control.
  - Habilidades de la sección 2.11:
    - **Fuego** quema zarzas de tinta y derrite muros de hielo.
    - **Hielo** congela a un enemigo al tercer golpe seguido.
    - **Viento** empuja cajas y permite **planear** si se mantiene saltar mientras se cae.
  - Acertijos ambientales (2.1 y 2.3), cada hechizo abre rutas que antes estaban bloqueadas:
    - Ala de Fuego y Corazón: pasadizos sellados con zarzas.
    - Ala de Hielo: pasadizo sellado con hielo.
    - Ala de Viento: caja, placa de presión y puertas rúnicas.
  - Los Ecos Menores también atacan con las Garras de Tinta Corrosiva (2.12).
  - Estinger musical al derrotar a un guardián (2.17, S4).
- Pantalla de **resultados** al terminar cada ala: tiempo, enemigos derrotados, daño recibido, Páginas Perdidas, salas secretas, fragmentos y el hechizo nuevo.
- Nueva fuente de la interfaz: **Poppins Bold**, más clara, con DejaVu Sans Bold de respaldo para el símbolo □ del control.

### 7 de octubre
- El juego ahora siempre termina al derrotar al Eco de Elenora: si falla algo en los logros o en el diálogo final, igual pasa a ¡VICTORIA!, epílogo y créditos.
- Capturas de pantalla con **F12** (o el botón Share del control) para las evidencias; se guardan en la carpeta `Capturas` del proyecto.
- Menú **Ecos del Grimorio**: *Capturar niveles para evidencias* y *Compilar juego (.exe)*.
- El panel de depuración de Unity ya no se abre al presionar los dos sticks del control.
- Producción del Sprint 2 en Excel: Feature Log actualizado, Sprint Plan replaneado, Sprint Backlog, Daily Huddle, Project Chart, Burn-down Chart, Task Chart, Buglist, Pruebas Alfa, matriz de coherencia con el GDD y Sprint Review.

### Pendiente
- Probar los cinco niveles en Unity y ajustar dificultad.
- Terminar las pruebas Alfa pendientes con el ejecutable.
- Documento del Producto 2 en Word y PDF.
- Diagrama de Gantt.
