using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static EditorHelpers;

// Menú: Ecos del Grimorio > Crear juego completo
// Genera los datos (hechizos, ítems, enemigos, logros), los prefabs y las escenas:
// Menú Principal, los 5 niveles de la Torre y los Créditos, y los agrega a Build Settings.
// Se puede volver a ejecutar cuando cambie el arte: sobreescribe prefabs y escenas.
public static class CrearJuego
{
    const string Data = "Assets/Data";
    const string Prefabs = "Assets/Prefabs";
    const string Scenes = "Assets/Scenes";
    const float G = -4f; // altura del suelo (parte de arriba)

    static Sprite square, circle, ladrillo, tablas, borde, vineta, pagina, vasija, grietas, panelUI, vinetaBlanca;
    static Sprite estante, cadena, carambanos, carambanoGrande, cristales, enredadera, hierba, runaCirculo, monticulo, llama;
    static PhysicsMaterial2D noFriction;
    static Font font;
    static readonly Dictionary<Elemento, SpellData> spells = new Dictionary<Elemento, SpellData>();
    static readonly Dictionary<string, ItemData> items = new Dictionary<string, ItemData>();
    static readonly Dictionary<string, GameObject> P = new Dictionary<string, GameObject>();

    [MenuItem("Ecos del Grimorio/Crear juego completo (menú + 5 niveles + créditos)")]
    public static void CrearTodo()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        try
        {
            EditorUtility.DisplayProgressBar("Ecos del Grimorio", "Preparando datos y prefabs...", 0.1f);
            Preparar();

            var escenas = new List<string>();
            EditorUtility.DisplayProgressBar("Ecos del Grimorio", "Menú principal", 0.2f); escenas.Add(CrearMenu());
            EditorUtility.DisplayProgressBar("Ecos del Grimorio", "Prólogo", 0.25f); escenas.Add(CrearPrologo());
            EditorUtility.DisplayProgressBar("Ecos del Grimorio", "Nivel 1", 0.3f); escenas.Add(Nivel1());
            EditorUtility.DisplayProgressBar("Ecos del Grimorio", "Nivel 2", 0.45f); escenas.Add(Nivel2());
            EditorUtility.DisplayProgressBar("Ecos del Grimorio", "Nivel 3", 0.6f); escenas.Add(Nivel3());
            EditorUtility.DisplayProgressBar("Ecos del Grimorio", "Nivel 4", 0.75f); escenas.Add(Nivel4());
            EditorUtility.DisplayProgressBar("Ecos del Grimorio", "Nivel 5", 0.85f); escenas.Add(Nivel5());
            EditorUtility.DisplayProgressBar("Ecos del Grimorio", "Epílogo", 0.92f); escenas.Add(CrearEpilogo());
            EditorUtility.DisplayProgressBar("Ecos del Grimorio", "Créditos", 0.95f); escenas.Add(CrearCreditos());

            var build = new List<EditorBuildSettingsScene>();
            foreach (var s in escenas) build.Add(new EditorBuildSettingsScene(s, true));
            EditorBuildSettings.scenes = build.ToArray();
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(escenas[0]);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog("Ecos del Grimorio",
            "Juego creado: Menú Principal, 5 niveles y Créditos.\n\n" +
            "Controles (también en el menú > Controles):\nA/D o flechas: moverse    Espacio: saltar    S: agacharse\n" +
            "1, 2, 3 (o J / clic): hechizos    C: combo rápido    Q: cambiar hechizo\nShift: esquive    E: hablar / abrir    Esc: pausa\n" +
            "Control: Cuadrado, R1, L1 hechizos; stick derecho apunta; L2 combo rápido\n\n" +
            "Abre MenuPrincipal y presiona Play.", "OK");
    }

    // =====================================================================
    // Datos y prefabs
    // =====================================================================

    static void Preparar()
    {
        AddTag("Ground");
        AddTag("Enemy");
        CreateFolders(Data + "/Hechizos", Data + "/Items", Data + "/Enemigos", Prefabs + "/Items", Prefabs + "/Enemigos",
                      Prefabs + "/Jefes", Scenes, Sprites + "Generados");

        square = GeneratedSprite("Cuadrado", false);
        circle = GeneratedSprite("Circulo", true);
        ladrillo = Ladrillo();
        tablas = Tablas();
        borde = Borde();
        vineta = Vineta();
        pagina = EditorHelpers.Pagina();
        vasija = EditorHelpers.Vasija();
        grietas = Grietas();
        panelUI = PanelUI();
        vinetaBlanca = VinetaBlanca();
        estante = Estante(); cadena = Cadena(); carambanos = Carambanos(); carambanoGrande = CarambanoGrande();
        cristales = Cristales(); enredadera = Enredadera(); hierba = Hierba(); runaCirculo = RunaCirculo();
        monticulo = Monticulo(); llama = Llama();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(Data + "/SinFriccion.physicsMaterial2D");
        if (noFriction == null)
        {
            noFriction = new PhysicsMaterial2D("SinFriccion") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(noFriction, Data + "/SinFriccion.physicsMaterial2D");
        }

        // Proyectiles
        P["ProyEnemigo"] = ProjectilePrefab("Proyectil_Enemigo", null, 0.45f, true);
        P["ProyEsquirla"] = ProjectilePrefab("Proyectil_Esquirla", Sprites + "Weapons/Esquirlas de Hielo.png", 0.8f, false);

        // Hechizos (sección 2.11)
        spells[Elemento.Arcano] = Spell("Hechizo Arcano", Elemento.Arcano, 10, 15, 0.3f, 12, 1f, 0f, 0f);
        spells[Elemento.Fuego] = Spell("Hechizo de Fuego", Elemento.Fuego, 18, 25, 0.6f, 11, 1f, 0f, 0f);
        spells[Elemento.Hielo] = Spell("Hechizo de Hielo", Elemento.Hielo, 12, 10, 0.5f, 10, 0.5f, 2f, 0f);
        spells[Elemento.Viento] = Spell("Hechizo de Viento", Elemento.Viento, 12, 8, 0.5f, 13, 1f, 0f, 7f);

        // Ítems (sección 2.13)
        Item("Cristal de Maná", TipoItem.Mana, 30, "Items/Cristal de Maná.png", "Recupera maná.");
        Item("Cristal de Maná Grande", TipoItem.Mana, 60, "Items/Cristal de Maná Grande.png", "Recupera mucho maná.");
        Item("Poción Menor de Vida", TipoItem.Vida, 50, "Items/Poción de Vida.png", "Restaura parte de la vida.");
        Item("Poción Mayor de Vida", TipoItem.Vida, 999, "Items/Poción Mayor de Vida.png", "Restaura toda la vida.");
        Item("Página Perdida", TipoItem.PaginaPerdida, 0, "Items/Fragmento de Grimorio.png", "+5 de vida máxima.");
        items["Página Perdida"].icon = pagina;
        EditorUtility.SetDirty(items["Página Perdida"]);
        string[] romanos = { "I", "II", "III", "IV", "V" };
        string[] fragmentos =
        {
            "«Los cuatro elementos no deberían pelear entre sí. Si logro unirlos en un solo grimorio, la Torre nunca volverá a temerle a la magia.» — Elenora",
            "«Elegí a cuatro aprendices para custodiar cada elemento. Kaelor guardará el fuego: es terco, pero leal.» — Elenora",
            "«Isolde y Threnody aprenden rápido. Pero el grimorio empieza a responder solo... escucho ecos de mi propia voz entre las páginas.» — Elenora",
            "«Si el grimorio se rompe, cada elemento buscará un guardián. No puedo permitir que mis aprendices paguen por mi error.» — Elenora",
            "«Voy a sellarme dentro del grimorio. Si alguien lo abre algún día, que sea alguien que quiera entender y no solo tener poder.» — Elenora"
        };
        for (int i = 0; i < romanos.Length; i++)
            Item("Fragmento de Grimorio " + romanos[i], TipoItem.FragmentoGrimorio, 1, "Items/Fragmento de Grimorio.png",
                "Revela el pasado de la Archimaga Elenora.", fragmentos[i]);
        Item("Núcleo de Ascua", TipoItem.NucleoDeAscua, 0, "Items/Núcleo de Ascua.png", "Tu hechizo de Fuego hace más daño.");
        Item("Anillo de Escarcha", TipoItem.AnilloDeEscarcha, 0, "Items/Anillo de Escarcha.png", "Recibes menos daño de fuego.");
        Item("Pluma Ligera", TipoItem.PlumaLigera, 0, "Items/Pluma Ligera.png", "Saltas más alto.");
        Item("Llave Rúnica", TipoItem.LlaveRunica, 0, "Items/Llave Rúnica.png", "Abre el cofre sellado del Ala de Aprendizaje.");
        Item("Nota cifrada de Elenora", TipoItem.Coleccionable, 0, "Items/Fragmento de Grimorio.png", "Primer indicio del plan de la Archimaga.",
            "Las letras están desordenadas, pero se alcanza a leer una frase:",
            "«El equilibrio no se impone. Se escucha.»");
        Item("Diario de la Archimaga Elenora", TipoItem.Coleccionable, 0, "Items/Diario de la Archimaga Elenora.png", "Las últimas palabras de Elenora.",
            "Última entrada del diario:",
            "«Mis aprendices me odiarán por esconderme. Sable, si algún día lees esto, cuida a quien se atreva a abrir el grimorio.»",
            "Lira: ¿Sable...? La maestra conocía a Elenora desde el principio.");
        Item("Grimorio Completo", TipoItem.Coleccionable, 0, "Items/Grimorio Completo.png", "El grimorio vuelve a estar completo.",
            "Las páginas perdidas vuelven a su lugar. El grimorio brilla con los cuatro colores al mismo tiempo.");

        foreach (var pair in items)
        {
            var tint = pair.Key == "Nota cifrada de Elenora" ? new Color(0.7f, 0.85f, 1f) : Color.white;
            bool esPagina = pair.Value.type == TipoItem.PaginaPerdida;
            P[pair.Key] = PickupPrefab(pair.Value, tint, esPagina ? new Color(0.6f, 0.85f, 1f) : new Color(1f, 0.85f, 0.5f), esPagina ? 1.3f : 0.8f);
        }

        // Enemigos (sección 2.10)
        var espectro = Enemy("Espectro de tinta", 30, 0, 10, 1.5f, 3f, 5f, P["Cristal de Maná"], 0.25f);
        var mota = Enemy("Mota Corrupta", 10, 0, 0, 1.5f, 1.5f, 6f);
        var mayor = Enemy("Espectro Mayor", 60, 3, 20, 1f, 2.2f, 6f, P["Poción Menor de Vida"], 1f);
        var centinela = Enemy("Centinela de Ceniza", 45, 0, 15, 1.5f, 3f, 6f, P["Poción Menor de Vida"], 0.2f);
        var salamandra = Enemy("Salamandra de Forja", 25, 0, 12, 2.5f, 4f, 7f, P["Cristal de Maná"], 0.3f, Elemento.Fuego);
        var escarchado = Enemy("Espectro Escarchado", 45, 0, 12, 1.6f, 3.2f, 6f, P["Poción Menor de Vida"], 0.2f, Elemento.Hielo);
        var cristal = Enemy("Cristal Viviente", 35, 0, 8, 0f, 0f, 9f, P["Cristal de Maná"], 0.35f);
        var ave = Enemy("Ave de Tormenta", 20, 0, 10, 2f, 2f, 7f, P["Cristal de Maná"], 0.25f);
        var golem = Enemy("Golem de Piedra Suspendida", 60, 0, 20, 1f, 1f, 6f, P["Poción Menor de Vida"], 0.3f);
        var eco = Enemy("Eco Menor", 30, 0, 10, 1.8f, 3.2f, 7f, P["Cristal de Maná"], 0.3f);
        var espejo = Enemy("Guardián Espejo", 80, 0, 15, 1.5f, 1.5f, 10f, P["Poción Mayor de Vida"], 0.5f);

        // Armas de los enemigos (sección de armas del documento)
        var garras = LoadSprite(Sprites + "Weapons/Garras de Tinta Corrosiva.png");
        var lanza = LoadSprite(Sprites + "Weapons/Lanza Incandescente.png");
        var embestida = LoadSprite(Sprites + "Weapons/Embestida Rocosa.png");
        P["Espectro"] = Walker("Espectro_de_Tinta", "Enemies/Espectro_De_Tinta.png", 1.5f, espectro,
            ai => Arma(ai.gameObject, garras, TipoGolpe.Zarpazo, 1.6f, 10f, 1.8f, 1.2f, 0f, null));
        P["Mayor"] = Walker("Espectro_Mayor", "Enemies/Espectro_Mayor.png", 2.3f, mayor,
            ai => Arma(ai.gameObject, garras, TipoGolpe.Zarpazo, 2.2f, 15f, 2.2f, 1.8f, 0f, null));
        P["Centinela"] = Walker("Centinela_de_Ceniza", "Enemies/Centinelas_De_Ceniza.png", 1.9f, centinela,
            ai => Arma(ai.gameObject, lanza, TipoGolpe.Estocada, 2.4f, 14f, 2.2f, 2.2f, -45f, Elemento.Fuego));
        P["Salamandra"] = Walker("Salamandra_de_Forja", "Enemies/Salamandras_De_Forja.png", 0.9f, salamandra,
            ai => { Set(ai, "jumpAttack", true); Set(ai, "jumpAttackForce", 8f); });
        P["Escarchado"] = Walker("Espectro_Escarchado", "Enemies/Espectros_Escarchados.png", 1.7f, escarchado);
        P["Eco"] = Walker("Eco_Menor", "Enemies/Ecos_Menores.png", 1.4f, eco,
            ai => Set(ai, "projectilePrefab", P["ProyEnemigo"]), typeof(EcoMenorAI));
        P["Espejo"] = Walker("Guardian_Espejo", "Enemies/Guardian_Espejo.png", 2.1f, espejo,
            ai => Set(ai, "projectilePrefab", P["ProyEnemigo"]), typeof(MirrorEnemyAI));

        // Personajes nuevos (arte de Arte_EcosDelGrimorio/Faltantes)
        var coloso = Enemy("Coloso de Raíz", 150, 0, 15, 0.9f, 1.3f, 9f, P["Poción Mayor de Vida"], 0.5f);
        var gargola = Enemy("Gárgola de Runa", 45, 0, 12, 3f, 3f, 8f, P["Cristal de Maná"], 0.4f);
        P["Coloso"] = Walker("Coloso_de_Raiz", "Enemies/Coloso_De_Raiz.png", 3.0f, coloso, ai =>
        {
            ai.GetComponent<Rigidbody2D>().mass = 10f;   // Lira no lo puede empujar
            SetResistances(ai.GetComponent<Health>(), (Elemento.Fuego, 1.6f), (Elemento.Viento, 0.5f));
        }, typeof(ColosoAI));
        {
            var go = EnemyBody("Gargola_de_Runa", "Enemies/Gargola_De_Runa.png", 1.6f, true, out _);
            var ai = go.AddComponent<GargolaAI>();
            Set(ai, "data", gargola);
            Set(ai, "projectilePrefab", P["ProyEnemigo"]);
            P["Gargola"] = SavePrefab(go, $"{Prefabs}/Enemigos/Gargola_de_Runa.prefab");
        }

        P["Cristal"] = Ranged("Cristal_Viviente", "Enemies/Cristales_Vivientes.png", 1.5f, cristal);

        P["Mota"] = Flyer("Mota_Corrupta", "Enemies/Motas_Corruptas.png", 0.9f, mota,
            ai => { Set(ai, "explodeOnContact", true); Set(ai, "driftTowardsPlayer", true); Set(ai, "hoverRadius", 0.8f); });
        P["Ave"] = Flyer("Ave_de_Tormenta", "Enemies/Aves_De_Tormenta.png", 1.0f, ave,
            ai => { Set(ai, "diveSpeed", 9f); Set(ai, "diveCooldown", 2.5f); });
        P["Golem"] = Flyer("Golem_de_Piedra", "Enemies/Golems_De_Piedra_Suspendida.png", 1.5f, golem,
            ai => { Set(ai, "diveSpeed", 6f); Set(ai, "diveCooldown", 3.5f); Set(ai, "hoverRadius", 0.6f);
                    Arma(ai.gameObject, embestida, TipoGolpe.Embestida, 2.2f, 16f, 3f, 1.6f, 0f, null); });

        // Jefes
        P["Kaelor"] = Boss("Kaelor", "Enemies/Kaelor.png", 3.0f, typeof(KaelorBoss), 260, 20, 1.8f, false, new[] { 0.5f },
            new[] { P["Núcleo de Ascua"], P["Fragmento de Grimorio II"] }, (Elemento.Fuego, 0.5f), (Elemento.Hielo, 1.5f));
        P["Isolde"] = Boss("Isolde", "Enemies/Isolde.png", 2.7f, typeof(IsoldeBoss), 280, 15, 2f, false, new[] { 0.5f },
            new[] { P["Anillo de Escarcha"], P["Fragmento de Grimorio III"] }, (Elemento.Hielo, 0.3f), (Elemento.Fuego, 1.5f));
        P["Threnody"] = Boss("Threnody", "Enemies/Threnody.png", 3.0f, typeof(ThrenodyBoss), 300, 18, 3f, true, new[] { 0.5f },
            new[] { P["Pluma Ligera"], P["Fragmento de Grimorio IV"] }, (Elemento.Viento, 0.3f), (Elemento.Hielo, 1.3f));
        Set(P["Threnody"].GetComponent<ThrenodyBoss>(), "minionPrefab", P["Ave"]);
        P["Elenora"] = Boss("Eco de la Archimaga Elenora", "Enemies/Eco_Archimaga_Elenora.png", 3.3f, typeof(ElenoraBoss), 480, 20, 3f, true,
            new[] { 0.8f, 0.6f, 0.4f, 0.2f }, new[] { P["Fragmento de Grimorio V"], P["Grimorio Completo"] });

        Derrota("Kaelor", "Enemies/Kaelor.png",
            "Kaelor: Tienes la misma mirada que ella... la de Elenora.",
            "Kaelor: Sigue subiendo, aprendiz. Isolde no será tan paciente como yo.");
        Derrota("Isolde", "Enemies/Isolde.png",
            "Isolde: Elenora no desapareció. Se escondió de lo que ella misma creó.",
            "Isolde: Busca su diario en el Ala de Viento. Ahí está lo que nosotros no quisimos ver.");
        Derrota("Threnody", "Enemies/Threnody.png",
            "Threnody: Los cuatro guardianes fuimos sus aprendices. Cuando sus hechizos se salieron de control, juramos proteger cada ala.",
            "Threnody: En el corazón del grimorio te espera un guardián que copia todo lo que haces. No repitas el mismo hechizo.");
        Set(P["Elenora"].GetComponent<ElenoraBoss>(), "finalDelJuego", true);
        Derrota("Elenora", "Enemies/Eco_Archimaga_Elenora.png",
            "Elenora: Por fin... el grimorio vuelve a estar en equilibrio.",
            "Elenora: Quise unir los cuatro elementos y terminé dividida en ecos. Gracias por escucharme, aprendiz.",
            "Lira: Descanse, Archimaga. Yo cuidaré lo que queda de su grimorio.");

        P["Lira"] = LiraPrefab();
        P["GameManager"] = GameManagerPrefab();
    }

    static SpellData Spell(string name, Elemento element, float cost, float damage, float cooldown, float speed,
                           float slow, float slowTime, float knockback)
    {
        var s = LoadOrCreate<SpellData>($"{Data}/Hechizos/{FileName(name)}.asset");
        var art = LoadSprite($"{Sprites}Spells/{name}.png");

        // Prefab del proyectil con el arte del hechizo
        var go = new GameObject("Proyectil_" + element);
        AddSprite(go, art != null ? art : circle, art != null ? 0.9f : 0.45f, 15);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = (art != null ? art : circle).bounds.extents.y * 0.6f;
        go.AddComponent<SpellProjectile>();
        go.AddComponent<EstelaProyectil>();
        Light(go, 3, ElementoColor.Get(element), 1.3f, 2.2f);

        s.spellName = name;
        s.element = element;
        s.icon = art;
        s.manaCost = cost;
        s.damage = damage;
        s.cooldown = cooldown;
        s.projectileSpeed = speed;
        s.lifetime = 2.5f;
        s.color = art != null ? Color.white : ElementoColor.Get(element);
        s.slowFactor = slow;
        s.slowDuration = slowTime;
        s.knockback = knockback;
        s.projectilePrefab = SavePrefab(go, $"{Prefabs}/Proyectil_{element}.prefab");
        EditorUtility.SetDirty(s);
        return s;
    }

    static GameObject ProjectilePrefab(string name, string spritePath, float height, bool tint)
    {
        var go = new GameObject(name);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = height * 0.35f;

        var art = spritePath != null ? LoadSprite(spritePath) : null;
        var visual = new GameObject("Arte");
        visual.transform.SetParent(go.transform, false);
        AddSprite(visual, art != null ? art : circle, height, 15);
        if (art != null) visual.transform.localRotation = Quaternion.Euler(0, 0, -90f); // el arte apunta hacia arriba

        var p = go.AddComponent<EnemyProjectile>();
        Set(p, "tintByElement", tint || art == null);
        go.AddComponent<EstelaProyectil>();
        Light(go, 3, new Color(1f, 0.6f, 0.6f), 0.9f, 1.6f);
        return SavePrefab(go, $"{Prefabs}/{name}.prefab");
    }

    static void Item(string name, TipoItem type, float amount, string sprite, string description, params string[] lore)
    {
        var item = LoadOrCreate<ItemData>($"{Data}/Items/{FileName(name)}.asset");
        item.itemName = name;
        item.type = type;
        item.amount = amount;
        item.icon = LoadSprite(Sprites + sprite);
        item.description = description;
        item.lore = lore;
        EditorUtility.SetDirty(item);
        items[name] = item;
    }

    static GameObject PickupPrefab(ItemData item, Color tint, Color luz, float brillo)
    {
        var go = new GameObject(item.itemName);
        var sr = AddSprite(go, item.icon != null ? item.icon : circle, 0.8f, 3);
        sr.color = item.icon != null ? tint : Color.yellow;
        go.AddComponent<CircleCollider2D>().isTrigger = true;
        Set(go.AddComponent<Pickup>(), "item", item);
        Light(go, 3, luz, brillo, 1.8f);
        return SavePrefab(go, $"{Prefabs}/Items/{FileName(item.itemName)}.prefab");
    }

    static EnemyData Enemy(string name, float hp, int hits, float contact, float move, float chase, float range,
                           GameObject drop = null, float dropChance = 0f, Elemento? immune = null)
    {
        var d = LoadOrCreate<EnemyData>($"{Data}/Enemigos/{FileName(name)}.asset");
        d.enemyName = name;
        d.maxHealth = hp;
        d.hitsToKill = hits;
        d.contactDamage = contact;
        d.moveSpeed = move;
        d.chaseSpeed = chase;
        d.detectionRange = range;
        d.dropPrefab = drop;
        d.dropChance = dropChance;
        d.hasImmunity = immune.HasValue;
        if (immune.HasValue) d.immuneTo = immune.Value;
        EditorUtility.SetDirty(d);
        return d;
    }

    static GameObject EnemyBody(string name, string sprite, float height, bool flying, out SpriteRenderer sr)
    {
        // Raíz con física y un hijo "Visual" con el sprite, para poder animarlo sin mover el collider
        var go = new GameObject(name);
        go.tag = "Enemy";
        var vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        var art = LoadSprite(Sprites + sprite);
        sr = AddSprite(vis, art != null ? art : circle, height, 8);
        if (art == null) sr.color = new Color(0.5f, 0.3f, 0.7f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = flying ? 0f : 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        float k = vis.transform.localScale.x;
        Vector2 size = sr.sprite.bounds.size * k, center = sr.sprite.bounds.center * k;
        if (flying)
        {
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = Mathf.Min(size.x, size.y) * 0.4f;
            c.offset = center;
        }
        else
        {
            var c = go.AddComponent<CapsuleCollider2D>();
            bool wide = size.x > size.y;
            c.direction = wide ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
            c.size = wide ? new Vector2(size.x * 0.85f, size.y * 0.8f) : new Vector2(size.x * 0.55f, size.y * 0.92f);
            c.offset = new Vector2(center.x, center.y - size.y * 0.03f);
        }

        go.AddComponent<Health>();
        return go;
    }

    static void Arma(GameObject go, Sprite arte, TipoGolpe tipo, float alcance, float dano, float recarga, float tamano, float giro, Elemento? elemento)
    {
        var a = go.AddComponent<ArmaEnemigo>();
        Set(a, "arte", arte);
        Set(a, "tipo", tipo);
        Set(a, "alcance", alcance);
        Set(a, "dano", dano);
        Set(a, "recarga", recarga);
        Set(a, "tamano", tamano);
        Set(a, "rotacionArte", giro);
        Set(a, "conElemento", elemento.HasValue);
        if (elemento.HasValue) Set(a, "elemento", elemento.Value);
    }

    static GameObject Walker(string name, string sprite, float height, EnemyData data,
                             System.Action<EnemyBase> configure = null, System.Type aiType = null)
    {
        var go = EnemyBody(name, sprite, height, false, out _);
        var ai = (EnemyBase)go.AddComponent(aiType ?? typeof(EnemyAI));
        Set(ai, "data", data);
        configure?.Invoke(ai);
        return SavePrefab(go, $"{Prefabs}/Enemigos/{name}.prefab");
    }

    static GameObject Ranged(string name, string sprite, float height, EnemyData data)
    {
        var go = EnemyBody(name, sprite, height, false, out _);
        var ai = go.AddComponent<RangedEnemyAI>();
        Set(ai, "data", data);
        Set(ai, "projectilePrefab", P["ProyEsquirla"]);
        Set(ai, "projectileElement", Elemento.Hielo);
        Set(ai, "fireRate", 2f);
        return SavePrefab(go, $"{Prefabs}/Enemigos/{name}.prefab");
    }

    static GameObject Flyer(string name, string sprite, float height, EnemyData data, System.Action<FlyingEnemyAI> configure)
    {
        var go = EnemyBody(name, sprite, height, true, out _);
        var ai = go.AddComponent<FlyingEnemyAI>();
        Set(ai, "data", data);
        configure?.Invoke(ai);
        return SavePrefab(go, $"{Prefabs}/Enemigos/{name}.prefab");
    }

    static GameObject Boss(string name, string sprite, float height, System.Type type, float hp, float contact, float speed, bool flying,
                           float[] phases, GameObject[] rewards, params (Elemento, float)[] resist)
    {
        var data = Enemy(name, hp, 0, contact, speed, speed, 30f);
        var go = EnemyBody(FileName(name), sprite, height, flying, out _);
        go.GetComponent<Rigidbody2D>().mass = 20f; // que Lira no lo empuje
        if (resist.Length > 0) SetResistances(go.GetComponent<Health>(), resist);

        var boss = (BossController)go.AddComponent(type);
        Set(boss, "data", data);
        Set(boss, "bossName", name);
        SetFloats(boss, "phaseThresholds", phases);
        Set(boss, "projectilePrefab", type == typeof(IsoldeBoss) ? P["ProyEsquirla"] : P["ProyEnemigo"]);
        SetArray(boss, "rewards", rewards);
        return SavePrefab(go, $"{Prefabs}/Jefes/{FileName(name)}.prefab");
    }

    static void Derrota(string jefe, string retrato, params string[] lineas)
    {
        var boss = P[jefe].GetComponent<BossController>();
        Set(boss, "portrait", LoadSprite(Sprites + retrato));
        SetStrings(boss, "defeatLines", lineas);
        PrefabUtility.SavePrefabAsset(P[jefe]);
    }

    static GameObject LiraPrefab()
    {
        var go = new GameObject("Lira");
        go.tag = "Player";
        var vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        var sprite = LoadSprite(Sprites + "Characters/Lira.png");
        AddSprite(vis, sprite, 1.8f, 10);
        float k = vis.transform.localScale.x;

        // Luz cálida que acompaña a Lira (el grimorio brilla)
        var glow = new GameObject("Luz");
        glow.transform.SetParent(go.transform, false);
        glow.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        Light(glow, 3, new Color(1f, 0.85f, 0.6f), 0.9f, 5f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<CapsuleCollider2D>();
        col.sharedMaterial = noFriction;
        Bounds b = sprite != null ? sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
        Vector2 size = b.size * k, center = b.center * k;
        col.size = new Vector2(size.x * 0.4f, size.y * 0.95f);
        col.offset = center;

        var groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(go.transform, false);
        groundCheck.localPosition = new Vector3(center.x, center.y - size.y * 0.475f, 0f);

        var health = go.AddComponent<Health>();
        Set(health, "maxHealth", 150f);
        Set(health, "invulnerableTime", 1.5f);
        Set(health, "damageTakenMultiplier", 0.65f); // recibe 35% menos daño
        Set(health, "destroyOnDeath", false);

        Set(go.AddComponent<PlayerController>(), "groundCheck", groundCheck);

        var caster = go.AddComponent<SpellCaster>();
        SetArray(caster, "spells", new Object[] { spells[Elemento.Arcano], spells[Elemento.Fuego], spells[Elemento.Hielo], spells[Elemento.Viento] });
        SetArray(caster, "comboSprites", new Object[]
        {
            LoadSprite(Sprites + "Spells/Explosión Arcana.png"), LoadSprite(Sprites + "Spells/Vapor Cegador.png"),
            LoadSprite(Sprites + "Spells/Granizo Cortante.png"), LoadSprite(Sprites + "Spells/Tormenta de Ascuas.png")
        });

        go.AddComponent<PlayerUpgrades>();
        go.AddComponent<ModoArchimaga>();
        return SavePrefab(go, Prefabs + "/Lira.prefab");
    }

    static GameObject GameManagerPrefab()
    {
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        var am = go.AddComponent<AchievementManager>();

        var logros = new (TipoLogro tipo, string nombre, string condicion)[]
        {
            (TipoLogro.PrimerHechizo, "Primer Hechizo", "Aprender el primer hechizo (Arcano) en el Ala de Aprendizaje."),
            (TipoLogro.MaestraElemental, "Maestra Elemental", "Desbloquear los cuatro hechizos elementales: Arcano, Fuego, Hielo y Viento."),
            (TipoLogro.ComboPerfecto, "Combo Perfecto", "Ejecutar al menos una vez cada uno de los cuatro combos elementales del juego."),
            (TipoLogro.SinUnRasguno, "Sin Un Rasguño", "Completar cualquier nivel sin recibir daño."),
            (TipoLogro.ColeccionistaDelGrimorio, "Coleccionista Del Grimorio", "Encontrar los cinco Fragmentos de Grimorio repartidos por la Torre."),
            (TipoLogro.CazadoraDeEcos, "Cazadora De Ecos", "Derrotar a los tres guardianes elementales: Kaelor, Isolde y Threnody."),
            (TipoLogro.VelocistaArcana, "Velocista Arcana", "Completar el Ala de Aprendizaje en menos de cinco minutos."),
            (TipoLogro.ElGrimorioCompleto, "El Grimorio Completo", "Terminar el juego con el 100% de los coleccionables obtenidos."),
            (TipoLogro.SecretoRevelado, "Secreto Revelado", "Activar por primera vez el comando especial Modo Archimaga."),
            (TipoLogro.SinPiedad, "Sin Piedad", "Derrotar al Eco de la Archimaga Elenora sin usar ninguna poción de vida durante el combate.")
        };
        string[] archivos = { "Primer Hechizo", "Maestra Elemental", "Combo Perfecto", "Sin Un Rasguño", "Coleccionista del Grimorio",
                              "Cazadora de Ecos", "Velocista Arcana", "El Grimorio Completo", "Secreto Revelado", "Sin Piedad" };

        var so = new SerializedObject(am);
        var prop = so.FindProperty("logros");
        prop.arraySize = logros.Length;
        for (int i = 0; i < logros.Length; i++)
        {
            var el = prop.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("tipo").enumValueIndex = (int)logros[i].tipo;
            el.FindPropertyRelative("nombre").stringValue = logros[i].nombre;
            el.FindPropertyRelative("condicion").stringValue = logros[i].condicion;
            el.FindPropertyRelative("icono").objectReferenceValue = LoadSprite($"{Sprites}Achievements/{archivos[i]}.png");
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        // Música y efectos de sonido
        var audio = go.AddComponent<AudioManager>();
        Set(audio, "musicaMenu", Audio("Musica/Menu.ogg"));
        SetArray(audio, "musicaNiveles", new Object[]
        {
            Audio("Musica/Nivel1.ogg"), Audio("Musica/Nivel2.ogg"), Audio("Musica/Nivel3.ogg"),
            Audio("Musica/Nivel4.ogg"), Audio("Musica/Nivel5.ogg")
        });
        Set(audio, "musicaJefe", Audio("Musica/Jefe.ogg"));
        Set(audio, "musicaJefeFinal", Audio("Musica/JefeFinal.ogg"));
        Set(audio, "musicaCreditos", Audio("Musica/Creditos.ogg"));
        var nombres = System.Enum.GetNames(typeof(Sfx));
        var efectos = new Object[nombres.Length];
        for (int i = 0; i < nombres.Length; i++) efectos[i] = Audio($"Efectos/{nombres[i]}.wav");
        SetArray(audio, "efectos", efectos);

        // Voces de los personajes (sílabas) y quejidos de Lira
        var personajes = new (string nombre, string archivo, float volumen)[]
        {
            ("Lira", "Lira", 0.55f), ("Sable", "Sable", 0.6f), ("Kaelor", "Kaelor", 0.7f), ("Isolde", "Isolde", 0.6f),
            ("Threnody", "Threnody", 0.65f), ("Elenora", "Elenora", 0.6f), ("Eco", "Eco", 0.55f)
        };
        var soAudio = new SerializedObject(audio);
        var vocesProp = soAudio.FindProperty("voces");
        vocesProp.arraySize = personajes.Length;
        for (int i = 0; i < personajes.Length; i++)
        {
            var el = vocesProp.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("nombre").stringValue = personajes[i].nombre;
            el.FindPropertyRelative("volumen").floatValue = personajes[i].volumen;
            var silabas = el.FindPropertyRelative("silabas");
            silabas.arraySize = 8;
            for (int k = 0; k < 8; k++)
                silabas.GetArrayElementAtIndex(k).objectReferenceValue = Audio($"Voces/Voz{personajes[i].archivo}_{k + 1}.wav");
        }
        soAudio.ApplyModifiedPropertiesWithoutUndo();
        SetArray(audio, "liraDano", new Object[] { Audio("Voces/LiraDano_1.wav"), Audio("Voces/LiraDano_2.wav"), Audio("Voces/LiraDano_3.wav") });
        SetArray(audio, "liraEsfuerzo", new Object[] { Audio("Voces/LiraEsfuerzo_1.wav"), Audio("Voces/LiraEsfuerzo_2.wav") });
        Set(audio, "liraCaida", Audio("Voces/LiraCaida.wav"));

        return SavePrefab(go, Prefabs + "/GameManager.prefab");
    }

    static AudioClip Audio(string path)
    {
        string full = "Assets/Audio/" + path;
        // La música se reproduce desde el disco (streaming) para no ocupar memoria
        if (path.StartsWith("Musica") && AssetImporter.GetAtPath(full) is AudioImporter imp)
        {
            var cfg = imp.defaultSampleSettings;
            if (cfg.loadType != AudioClipLoadType.Streaming)
            {
                cfg.loadType = AudioClipLoadType.Streaming;
                imp.defaultSampleSettings = cfg;
                imp.SaveAndReimport();
            }
        }
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(full);
        if (clip == null) Debug.LogWarning("No se encontró el audio " + path);
        return clip;
    }

    // =====================================================================
    // Temas visuales de cada ala de la Torre
    // =====================================================================

    class Tema
    {
        public string fondo;
        public Color piedra, borde, plataforma, cielo, luz, antorcha, pilar, ambiente;
        public float intensidad;
        public Vector2 velAmbiente;
        public float tasaAmbiente, tamAmbiente;
        public bool ambienteDesdeArriba, pilares = true, nubes, chispas = true;
    }

    static readonly Tema Aprendizaje = new Tema
    {
        fondo = "ALA_DE_APRENDIZAJE.png", piedra = new Color(0.62f, 0.5f, 0.45f), borde = new Color(0.8f, 0.6f, 0.32f),
        plataforma = new Color(0.65f, 0.45f, 0.3f), cielo = new Color(0.08f, 0.06f, 0.1f), luz = new Color(1f, 0.92f, 0.85f), intensidad = 0.6f,
        antorcha = new Color(1f, 0.65f, 0.3f), pilar = new Color(0.38f, 0.3f, 0.28f), ambiente = new Color(1f, 0.9f, 0.6f, 0.5f),
        velAmbiente = new Vector2(0.1f, 0.15f), tasaAmbiente = 6f, tamAmbiente = 0.06f
    };

    static readonly Tema Fuego = new Tema
    {
        fondo = "Ala de Fuego.jpg", piedra = new Color(0.4f, 0.24f, 0.2f), borde = new Color(1f, 0.45f, 0.1f),
        plataforma = new Color(0.5f, 0.25f, 0.15f), cielo = new Color(0.12f, 0.04f, 0.02f), luz = new Color(1f, 0.7f, 0.6f), intensidad = 0.5f,
        antorcha = new Color(1f, 0.45f, 0.1f), pilar = new Color(0.28f, 0.14f, 0.12f), ambiente = new Color(1f, 0.5f, 0.1f, 0.8f),
        velAmbiente = new Vector2(0f, 1.2f), tasaAmbiente = 14f, tamAmbiente = 0.07f
    };

    static readonly Tema Hielo = new Tema
    {
        fondo = "Ala de Hielo.png", piedra = new Color(0.6f, 0.72f, 0.85f), borde = new Color(0.95f, 0.98f, 1f),
        plataforma = new Color(0.72f, 0.86f, 0.96f), cielo = new Color(0.04f, 0.06f, 0.12f), luz = new Color(0.8f, 0.9f, 1f), intensidad = 0.6f,
        antorcha = new Color(0.5f, 0.8f, 1f), pilar = new Color(0.32f, 0.42f, 0.58f), ambiente = new Color(1f, 1f, 1f, 0.8f),
        velAmbiente = new Vector2(-0.3f, -1.2f), tasaAmbiente = 18f, tamAmbiente = 0.08f, ambienteDesdeArriba = true, chispas = false
    };

    static readonly Tema Viento = new Tema
    {
        fondo = "Ala de Viento.jpg", piedra = new Color(0.75f, 0.75f, 0.7f), borde = new Color(0.45f, 0.78f, 0.35f),
        plataforma = new Color(0.78f, 0.72f, 0.6f), cielo = new Color(0.4f, 0.6f, 0.85f), luz = new Color(1f, 1f, 0.95f), intensidad = 0.95f,
        antorcha = new Color(1f, 1f, 0.8f), pilar = new Color(0.6f, 0.65f, 0.7f), ambiente = new Color(0.6f, 0.9f, 0.5f, 0.8f),
        velAmbiente = new Vector2(2.5f, -0.3f), tasaAmbiente = 8f, tamAmbiente = 0.1f, pilares = false, nubes = true
    };

    static readonly Tema Corazon = new Tema
    {
        fondo = "Corazón del Grimorio.jpg", piedra = new Color(0.4f, 0.3f, 0.52f), borde = new Color(0.9f, 0.75f, 0.35f),
        plataforma = new Color(0.55f, 0.4f, 0.65f), cielo = new Color(0.05f, 0.02f, 0.08f), luz = new Color(0.9f, 0.8f, 1f), intensidad = 0.45f,
        antorcha = new Color(0.75f, 0.45f, 1f), pilar = new Color(0.24f, 0.16f, 0.32f), ambiente = new Color(0.8f, 0.6f, 1f, 0.7f),
        velAmbiente = new Vector2(0f, 0.5f), tasaAmbiente = 10f, tamAmbiente = 0.07f
    };

    // =====================================================================
    // Construcción de escenas
    // =====================================================================

    class Nivel
    {
        public UnityEngine.SceneManagement.Scene scene;
        public Transform geo, deco, enemigos, items;
        public GameObject lira;
        public Tema tema;
    }

    static Nivel NuevoNivel(Tema t, string titulo, float minX, float maxX, Vector2 inicio)
    {
        var n = new Nivel { tema = t };
        n.scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Light(new GameObject("Luz Global"), 4, t.luz, t.intensidad);
        PrefabUtility.InstantiatePrefab(P["GameManager"]);
        EventSystem();

        // Cámara
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = t.cielo;
        camGo.transform.position = new Vector3(inicio.x + 4f, G + 3.5f, -10f);
        var follow = camGo.AddComponent<CameraFollow>();
        Set(follow, "minX", minX);
        Set(follow, "maxX", maxX);
        Set(follow, "minY", G - 3f);
        Set(follow, "maxY", 13f);

        var amb = camGo.AddComponent<ParticulasAmbiente>();
        Set(amb, "color", t.ambiente);
        Set(amb, "velocidad", t.velAmbiente);
        Set(amb, "porSegundo", t.tasaAmbiente);
        Set(amb, "tamano", t.tamAmbiente);
        Set(amb, "desdeArriba", t.ambienteDesdeArriba);

        n.geo = new GameObject("Nivel").transform;
        n.deco = new GameObject("Decoracion").transform;
        n.enemigos = new GameObject("Enemigos").transform;
        n.items = new GameObject("Items").transform;

        // Fondo lejano: dos copias (la segunda volteada) que se mueven casi con la cámara
        var bg = LoadSprite(Sprites + "Backgrounds/" + t.fondo);
        if (bg != null)
        {
            float scale = 15f / bg.bounds.size.y;
            float width = bg.bounds.size.x * scale;
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Fondo_" + i);
                go.transform.SetParent(n.deco, false);
                go.transform.position = new Vector3(camGo.transform.position.x + i * width, camGo.transform.position.y, 0f);
                go.transform.localScale = Vector3.one * scale;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = bg;
                sr.flipX = i == 1;
                sr.sortingOrder = -100;
                var px = go.AddComponent<Parallax>();
                Set(px, "factor", 0.88f);
                Set(px, "factorVertical", 0.88f);
            }
        }

        // Capa media: columnas de piedra o nubes que se mueven a media velocidad
        if (t.pilares)
        {
            // Columnas delgadas y oscuras para que no tapen el fondo
            for (float x = minX + 4f; x <= maxX + 10f; x += 15f)
            {
                var col = Tiled(n.deco, "Columna", new Vector2(x, G + 4f), new Vector2(0.9f, 20f), ladrillo, t.pilar * new Color(0.7f, 0.7f, 0.7f, 0.75f), -50);
                var px = col.AddComponent<Parallax>();
                Set(px, "factor", 0.45f);
                Set(px, "factorVertical", 0.2f);
            }
        }
        if (t.nubes)
        {
            var rnd = new System.Random(7);
            for (int i = 0; i < 16; i++)
            {
                var c = new GameObject("Nube");
                c.transform.SetParent(n.deco, false);
                c.transform.position = new Vector2(minX + (float)rnd.NextDouble() * (maxX - minX), 1f + (float)rnd.NextDouble() * 7f);
                c.transform.localScale = new Vector3(5f + (float)rnd.NextDouble() * 4f, 1.6f + (float)rnd.NextDouble(), 1f);
                var sr = c.AddComponent<SpriteRenderer>();
                sr.sprite = circle;
                sr.color = new Color(1f, 1f, 1f, 0.35f);
                sr.sortingOrder = -60;
                var px = c.AddComponent<Parallax>();
                Set(px, "factor", 0.6f);
                Set(px, "factorVertical", 0.3f);
            }
        }

        Solido(n.geo, "Pared_Izq", new Vector2(minX - 0.5f, 4f), new Vector2(1f, 24f), ladrillo, t.piedra * 0.7f, false);
        Solido(n.geo, "Pared_Der", new Vector2(maxX + 0.5f, 4f), new Vector2(1f, 24f), ladrillo, t.piedra * 0.7f, false);

        var kill = new GameObject("ZonaDeCaida");
        kill.transform.SetParent(n.geo, false);
        kill.transform.position = new Vector2((minX + maxX) / 2f, G - 8f);
        var killCol = kill.AddComponent<BoxCollider2D>();
        killCol.isTrigger = true;
        killCol.size = new Vector2(maxX - minX + 20f, 2f);
        kill.AddComponent<KillZone>();

        n.lira = (GameObject)PrefabUtility.InstantiatePrefab(P["Lira"]);
        n.lira.transform.position = new Vector3(inicio.x, G + 1f, 0f);

        HUD(n.lira, titulo);
        DecorarBioma(n, minX, maxX);
        return n;
    }

    // Detalles de cada bioma: libreros, cadenas, cristales, runas y montículos en primer plano
    static void DecorarBioma(Nivel n, float minX, float maxX)
    {
        var rnd = new System.Random(Mathf.RoundToInt(maxX * 7f));
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var t = n.tema;

        // Primer plano: montículos oscuros abajo de la pantalla que se mueven más rápido (profundidad)
        Color frente = t == Hielo ? new Color(0.75f, 0.85f, 0.95f, 1f)
                     : t == Viento ? new Color(0.22f, 0.4f, 0.2f, 1f)
                     : t.piedra * 0.3f;
        frente.a = 1f;
        for (float x = minX + R(2f, 6f); x < maxX + 10f; x += R(9f, 15f))
        {
            var m = new GameObject("PrimerPlano");
            m.transform.SetParent(n.deco, false);
            var sr = AddSprite(m, monticulo, R(1.1f, 1.6f), 60);
            m.transform.localScale = new Vector3(m.transform.localScale.x * R(0.8f, 1.3f), m.transform.localScale.y, 1f);
            m.transform.position = new Vector2(x, G - 1.2f);
            sr.color = frente;
            var px = m.AddComponent<Parallax>();
            Set(px, "factor", -0.25f);
            Set(px, "factorVertical", 0f);
        }

        if (t == Aprendizaje)
        {
            for (float x = minX + 3f; x < maxX; x += R(7f, 12f))
            {
                var e = new GameObject("Librero");
                e.transform.SetParent(n.deco, false);
                var sr = AddSprite(e, estante, R(2.6f, 3.4f), -15);
                e.transform.position = new Vector2(x, G + sr.bounds.extents.y);
                sr.color = new Color(0.75f, 0.7f, 0.68f, 1f);
            }
        }
        else if (t == Fuego)
        {
            for (float x = minX + 2f; x < maxX; x += R(5f, 9f))
            {
                float largo = R(3f, 7f);
                Tiled(n.deco, "Cadena", new Vector2(x, G + 13f - largo / 2f), new Vector2(0.45f, largo), cadena, new Color(0.45f, 0.35f, 0.32f, 1f), -16);
            }
        }
        else if (t == Hielo || t == Corazon)
        {
            var color = t == Hielo ? new Color(0.7f, 0.88f, 1f, 0.9f) : new Color(0.75f, 0.55f, 1f, 0.9f);
            for (float x = minX + 4f; x < maxX; x += R(7f, 12f))
            {
                var c = new GameObject("Cristales");
                c.transform.SetParent(n.deco, false);
                var sr = AddSprite(c, cristales, R(1.2f, 2.2f), -14);
                c.transform.position = new Vector2(x, G + sr.bounds.extents.y - 0.05f);
                sr.color = color;
                if (rnd.NextDouble() < 0.5) Light(c, 3, color, 0.6f, 2.2f);
            }
        }

        if (t == Corazon)
        {
            for (float x = minX + 6f; x < maxX; x += R(10f, 15f))
            {
                var r = new GameObject("Runa");
                r.transform.SetParent(n.deco, false);
                var sr = AddSprite(r, runaCirculo, R(2f, 3.5f), -30);
                r.transform.position = new Vector2(x, G + R(5f, 10f));
                sr.color = new Color(0.8f, 0.6f, 1f, 0.55f);
                var f = r.AddComponent<Flotar>();
                Set(f, "altura", 0.3f);
                Set(f, "giro", R(-20f, 20f));
                Light(r, 3, new Color(0.7f, 0.5f, 1f), 0.7f, 3f);
            }
        }
    }

    // ---------- Peligros de los biomas ----------

    static void Geiser(Nivel n, float x)
    {
        var go = new GameObject("Geiser");
        go.transform.SetParent(n.items, false);
        go.transform.position = new Vector2(x, G + 0.05f);
        var boca = AddSprite(new GameObject("Boca"), circle, 0.45f, 3);
        boca.transform.SetParent(go.transform, false);
        boca.transform.localScale = new Vector3(boca.transform.localScale.x * 2.6f, boca.transform.localScale.y, 1f);
        boca.color = new Color(1f, 0.45f, 0.1f, 0.9f);
        var fuego = new GameObject("Llama");
        fuego.transform.SetParent(go.transform, false);
        var sr = fuego.AddComponent<SpriteRenderer>();
        sr.sprite = llama;
        sr.sortingOrder = 12;
        // el sprite de la llama nace en su centro: se sube para que crezca desde el suelo
        var pivote = new GameObject("Pivote");
        pivote.transform.SetParent(go.transform, false);
        fuego.transform.SetParent(pivote.transform, false);
        fuego.transform.localPosition = new Vector3(0f, llama.bounds.extents.y, 0f);
        var g = go.AddComponent<GeiserFuego>();
        Set(g, "llama", sr);
        Light(go, 3, new Color(1f, 0.5f, 0.1f), 0.9f, 2.5f);
    }

    static void CarambanoTrampa(Nivel n, float x, float platTop)
    {
        var go = new GameObject("Carambano");
        go.transform.SetParent(n.enemigos, false);
        var sr = AddSprite(go, carambanoGrande, 1.1f, 6);
        sr.color = new Color(0.85f, 0.95f, 1f, 1f);
        go.transform.position = new Vector2(x, platTop - 0.5f - 0.55f);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = carambanoGrande.bounds.size * 0.7f;
        go.AddComponent<Carambano>();
    }

    static void Corriente(Nivel n, float x, float ancho, float alto)
    {
        var go = new GameObject("CorrienteViento");
        go.transform.SetParent(n.items, false);
        go.transform.position = new Vector2(x, G + alto / 2f);
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(ancho, alto);
        go.AddComponent<CorrienteViento>();
        var marca = Tiled(go.transform, "Base", new Vector2(x, G + 0.1f), new Vector2(ancho, 0.25f), square, new Color(0.7f, 1f, 0.8f, 0.35f), 4);
        Light(go, 3, new Color(0.7f, 1f, 0.8f), 0.4f, ancho * 1.2f);
    }

    // Espíritu de un guardián (Corazón del Grimorio): habla con Lira y le da una bendición
    static void Espiritu(Nivel n, float x, string quien, string sprite, float altura, Bendicion bendicion, string mensaje, params string[] lineas)
    {
        var go = new GameObject("Espiritu_" + FileName(quien));
        go.transform.SetParent(n.items, false);
        go.transform.position = new Vector2(x, G + altura / 2f + 0.3f);
        var vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        var art = LoadSprite(Sprites + sprite);
        var sr = AddSprite(vis, art, altura, 8);
        sr.color = new Color(0.65f, 0.85f, 1f, 0.6f);
        Set(vis.AddComponent<Flotar>(), "altura", 0.15f);
        Light(go, 3, new Color(0.6f, 0.8f, 1f), 0.8f, 3f);
        var d = go.AddComponent<NPCDialogue>();
        Set(d, "speakerName", quien);
        Set(d, "portrait", art);
        SetStrings(d, "lines", lineas);
        Set(d, "talkRange", 2.1f);
        Set(d, "bendicion", bendicion);
        Set(d, "mensajeBendicion", mensaje);
    }

    static string Guardar(Nivel n, string nombre)
    {
        string path = $"{Scenes}/{nombre}.unity";
        EditorSceneManager.SaveScene(n.scene, path);
        return path;
    }

    // Sprite que se repite (no se estira) del tamaño indicado
    static GameObject Tiled(Transform parent, string name, Vector2 center, Vector2 size, Sprite sprite, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    static GameObject Solido(Transform parent, string name, Vector2 center, Vector2 size, Sprite sprite, Color color, bool esSuelo = true)
    {
        var go = Tiled(parent, name, center, size, sprite, color, 1);
        if (esSuelo) go.tag = "Ground"; // las paredes no cuentan como suelo para que no se puedan escalar
        go.AddComponent<BoxCollider2D>().size = size;
        return go;
    }

    // Suelo: bloque de ladrillo que llega hasta abajo de la pantalla con un borde arriba
    static void Suelo(Nivel n, float x1, float x2)
    {
        float w = x2 - x1, cx = (x1 + x2) / 2f;
        Solido(n.geo, $"Suelo_{x1}_{x2}", new Vector2(cx, G - 6f), new Vector2(w, 12f), ladrillo, n.tema.piedra);
        Tiled(n.geo, "Borde", new Vector2(cx, G), new Vector2(w + 0.2f, 0.5f), borde, n.tema.borde, 3);
        if (n.tema == Viento)
            Tiled(n.geo, "Hierba", new Vector2(cx, G + 0.38f), new Vector2(w, 0.5f), hierba, n.tema.borde * 1.15f, 4);
    }

    static GameObject Plat(Nivel n, float x, float top, float w)
    {
        var go = Solido(n.geo, $"Plataforma_{x}", new Vector2(x, top - 0.25f), new Vector2(w, 0.5f), tablas, n.tema.plataforma);
        go.GetComponent<SpriteRenderer>().sortingOrder = 2;

        // Detalles colgando debajo (se mueven con la plataforma)
        if (n.tema == Hielo)
            Tiled(go.transform, "Carambanos", new Vector2(x, top - 0.5f - 0.35f), new Vector2(w - 0.3f, 0.75f), carambanos, new Color(0.8f, 0.92f, 1f, 0.95f), 1);
        else if (n.tema == Viento)
            foreach (var dx in new[] { -w * 0.3f, w * 0.22f })
            {
                float largo = 1f + Mathf.Abs(Mathf.Sin(x * 3.7f + dx)) * 1.2f;
                Tiled(go.transform, "Enredadera", new Vector2(x + dx, top - 0.5f - largo / 2f), new Vector2(0.5f, largo), enredadera, Color.white, 1);
            }
        return go;
    }

    static void PlatMovil(Nivel n, float x, float top, float w, Vector2 offset, float speed)
    {
        var go = Plat(n, x, top, w);
        go.name = $"PlataformaMovil_{x}";
        go.GetComponent<SpriteRenderer>().color = n.tema.plataforma * new Color(0.9f, 1f, 1.15f);
        var mp = go.AddComponent<MovingPlatform>();
        Set(mp, "offset", offset);
        Set(mp, "speed", speed);
        var glow = new GameObject("Runa");
        glow.transform.SetParent(go.transform, false);
        Light(glow, 3, new Color(0.6f, 1f, 0.8f), 0.7f, 2.2f);
    }

    // Pozo de lava (Ala de Fuego): brilla y suelta brasas
    static void Lava(Nivel n, float x1, float x2)
    {
        var go = Tiled(n.deco, "Lava", new Vector2((x1 + x2) / 2f, G - 3.5f), new Vector2(x2 - x1, 3f), square, new Color(1f, 0.4f, 0.05f), 0);
        Light(go, 3, new Color(1f, 0.45f, 0.1f), 1.6f, 4.5f);
        var f = go.AddComponent<LuzParpadeante>();
        Set(f, "intensidadBase", 1.6f);
        Set(f, "colorChispa", new Color(1f, 0.6f, 0.1f, 1f));
    }

    // Antorchas (o cristales en el Ala de Hielo) con luz que parpadea
    static void Antorchas(Nivel n, params float[] xs)
    {
        foreach (var x in xs)
        {
            var go = new GameObject("Antorcha");
            go.transform.SetParent(n.deco, false);
            go.transform.position = new Vector2(x, G + 2.8f);

            var soporte = Tiled(go.transform, "Soporte", new Vector2(x, G + 2.45f), new Vector2(0.15f, 0.5f), square, new Color(0.2f, 0.15f, 0.12f), -5);
            soporte.transform.SetParent(go.transform, true);

            var llama = new GameObject("Llama");
            llama.transform.SetParent(go.transform, false);
            var sr = AddSprite(llama, circle, 0.4f, -4);
            sr.color = n.tema.antorcha;

            Light(go, 3, n.tema.antorcha, 1.2f, 4.5f);
            var f = go.AddComponent<LuzParpadeante>();
            Set(f, "intensidadBase", 1.2f);
            Set(f, "echaChispas", n.tema.chispas);
            Set(f, "colorChispa", n.tema.antorcha);
        }
    }

    static GameObject Poner(Nivel n, string prefab, float x, float y, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(P[prefab]);
        go.transform.position = new Vector2(x, y);
        go.transform.SetParent(parent, true);
        return go;
    }

    // Enemigo de suelo (y = altura del piso donde está parado)
    static GameObject Enemigo(Nivel n, string prefab, float x, float suelo = G, float patrulla = 3f)
    {
        var go = Poner(n, prefab, x, suelo + 1.5f, n.enemigos);
        var ai = go.GetComponent<EnemyAI>();
        if (ai != null) Set(ai, "patrolDistance", patrulla);
        return go;
    }

    static GameObject Volador(Nivel n, string prefab, float x, float y) => Poner(n, prefab, x, y, n.enemigos);

    static void Objeto(Nivel n, string item, float x, float sobre) => Poner(n, item, x, sobre + 0.7f, n.items);

    static void PuntoReaparicion(Nivel n, float x)
    {
        var go = new GameObject("Piedra_de_Reaparicion");
        go.transform.SetParent(n.items, false);
        var sprite = LoadSprite(Sprites + "Items/Piedra de Reaparición.png");
        AddSprite(go, sprite != null ? sprite : circle, 1.2f, 3);
        go.transform.position = new Vector2(x, G + 0.6f);
        go.AddComponent<BoxCollider2D>().isTrigger = true;
        go.AddComponent<Checkpoint>();
        Light(go, 3, new Color(0.5f, 1f, 1f), 0.8f, 2.5f);
    }

    // Personaje con quien se habla (Maestra Sable) o diálogo automático antes de un jefe
    static void Dialogo(Nivel n, float x, string quien, string retrato, bool visible, bool automatico, params string[] lineas) =>
        Dialogo(n, x, quien, retrato, visible, automatico, Color.white, lineas);

    // Proyección mágica de la Maestra Sable en las demás alas (se ve azulada y transparente)
    static void Proyeccion(Nivel n, float x, params string[] lineas) =>
        Dialogo(n, x, "Maestra Sable", "Characters/Maestra Sable.png", true, false, new Color(0.7f, 0.85f, 1f, 0.7f), lineas);

    static void Dialogo(Nivel n, float x, string quien, string retrato, bool visible, bool automatico, Color tinte, params string[] lineas)
    {
        var go = new GameObject("Dialogo_" + FileName(quien));
        go.transform.SetParent(n.items, false);
        var sprite = retrato != null ? LoadSprite(Sprites + retrato) : null;
        if (visible && sprite != null)
        {
            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            AddSprite(vis, sprite, 1.95f, 9).color = tinte;
            if (tinte != Color.white)
            {
                Set(vis.AddComponent<Flotar>(), "altura", 0.1f);
                Light(go, 3, new Color(0.6f, 0.8f, 1f), 0.7f, 2.5f);
            }
        }
        go.transform.position = new Vector2(x, G + 0.975f);
        var d = go.AddComponent<NPCDialogue>();
        Set(d, "speakerName", quien);
        Set(d, "portrait", sprite);
        SetStrings(d, "lines", lineas);
        Set(d, "talkRange", automatico ? 4f : 2.2f);
        Set(d, "autoStart", automatico);
    }

    // Diálogo automático al pasar por un punto (pensamientos de Lira o susurros de los ecos)
    static void Narracion(Nivel n, float x, params string[] lineas) =>
        Dialogo(n, x, "Lira", "Characters/Lira.png", false, true, lineas);

    static GameObject Jefe(Nivel n, string prefab, float x, float y, float arenaMin, float arenaMax, float hover)
    {
        var go = Poner(n, prefab, x, y, n.enemigos);
        var boss = go.GetComponent<BossController>();
        Set(boss, "arenaMinX", arenaMin);
        Set(boss, "arenaMaxX", arenaMax);
        Set(boss, "hoverHeight", hover);
        Set(boss, "rewardPosition", new Vector2(x, G + 0.8f));
        Set(boss, "activationRange", 11f);
        return go;
    }

    static void Salida(Nivel n, float x, Health requerido, string siguiente, bool desbloquea, Elemento hechizo)
    {
        var exit = new GameObject("Salida_Nivel");
        exit.transform.position = new Vector2(x, G + 1.5f);
        var portal = AddSprite(exit, circle, 3f, 2);
        exit.transform.localScale = new Vector3(1.5f, 3f, 1f);
        portal.color = new Color(0.6f, 0.4f, 1f, 0.6f);
        exit.AddComponent<BoxCollider2D>().isTrigger = true;
        Light(exit, 3, new Color(0.7f, 0.5f, 1f), 1.5f, 3f);
        var le = exit.AddComponent<LevelExit>();
        Set(le, "requiredDefeat", requerido);
        Set(le, "portalRenderer", portal);
        Set(le, "nextScene", siguiente);
        Set(le, "unlocksSpell", desbloquea);
        Set(le, "spellToUnlock", hechizo);
    }

    // ---------- Secretos y objetos para explorar ----------

    // Sala secreta: un bloque de piedra con un muro agrietado que se rompe con cualquier hechizo.
    // Mientras no se rompa, una capa de piedra tapa el interior.
    static void Escondite(Nivel n, float x1, float x2, bool entradaDerecha, float alto = 3.6f)
    {
        var roca = n.tema.piedra * 0.75f; roca.a = 1f;
        float cx = (x1 + x2) / 2f, w = x2 - x1, grosor = 3f, mw = 0.9f;

        Solido(n.geo, "Escondite_Techo", new Vector2(cx, G + alto + grosor / 2f), new Vector2(w, grosor), ladrillo, roca);
        Tiled(n.geo, "Borde", new Vector2(cx, G + alto + grosor), new Vector2(w + 0.2f, 0.5f), borde, n.tema.borde, 3);

        float ix1 = entradaDerecha ? x1 : x1 + mw, ix2 = entradaDerecha ? x2 - mw : x2;
        var fondoColor = n.tema.piedra * 0.4f; fondoColor.a = 1f;
        Tiled(n.deco, "Escondite_Fondo", new Vector2((ix1 + ix2) / 2f, G + alto / 2f), new Vector2(ix2 - ix1, alto), ladrillo, fondoColor, -10);
        var tapa = Tiled(n.deco, "Escondite_Piedra", new Vector2((ix1 + ix2) / 2f, G + alto / 2f), new Vector2(ix2 - ix1, alto), ladrillo, roca, 40);

        float mx = entradaDerecha ? x2 - mw / 2f : x1 + mw / 2f;
        var muro = Tiled(n.geo, "Muro_Agrietado", new Vector2(mx, G + alto / 2f), new Vector2(mw, alto), ladrillo, roca * new Color(1.15f, 1.1f, 1.05f, 1f), 2);
        muro.AddComponent<BoxCollider2D>().size = new Vector2(mw, alto);
        Tiled(muro.transform, "Grietas", new Vector2(mx, G + alto / 2f), new Vector2(mw, alto), grietas, Color.white, 3);
        var r = muro.AddComponent<Rompible>();
        Set(muro.GetComponent<Health>(), "maxHealth", 10f);
        Set(r, "mensaje", "¡Un pasadizo secreto!");
        Set(r, "colorParticulas", roca);
        SetArray(r, "revelar", new Object[] { tapa.GetComponent<SpriteRenderer>() });
    }

    // Pasadizo bajo: Lira solo cabe agachada. Arriba se puede caminar.
    static void Tunel(Nivel n, float x1, float x2, float alto = 1.3f)
    {
        float cx = (x1 + x2) / 2f, grosor = 1.2f;
        var col = n.tema.piedra * 0.85f; col.a = 1f;
        Solido(n.geo, "Tunel_Techo", new Vector2(cx, G + alto + grosor / 2f), new Vector2(x2 - x1, grosor), ladrillo, col);
        Tiled(n.geo, "Borde", new Vector2(cx, G + alto + grosor), new Vector2(x2 - x1 + 0.2f, 0.5f), borde, n.tema.borde, 3);
        var fondo = n.tema.piedra * 0.4f; fondo.a = 1f;
        Tiled(n.deco, "Tunel_Fondo", new Vector2(cx, G + alto / 2f), new Vector2(x2 - x1, alto), ladrillo, fondo, -10);
    }

    // Vasija que se rompe con un hechizo o atravesándola con el esquive
    static void Vasija(Nivel n, float x, float suelo = G)
    {
        var go = new GameObject("Vasija");
        go.transform.SetParent(n.items, false);
        var sr = AddSprite(go, vasija, 0.95f, 4);
        sr.color = Color.Lerp(new Color(0.78f, 0.48f, 0.32f), n.tema.borde, 0.3f);
        go.transform.position = new Vector2(x, suelo + 0.475f);
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = vasija.bounds.size;
        var r = go.AddComponent<Rompible>();
        Set(go.GetComponent<Health>(), "maxHealth", 1f);
        SetArray(r, "botin", new Object[] { P["Cristal de Maná"], P["Poción Menor de Vida"], P["Cristal de Maná"] });
        Set(r, "probabilidadBotin", 0.7f);
        Set(r, "rompeConEsquive", true);
        Set(r, "colorParticulas", sr.color);
    }

    static void Vasijas(Nivel n, params float[] xs)
    {
        foreach (var x in xs) Vasija(n, x);
    }

    // Cofre de madera: se abre con E / Triángulo
    static void CofreMadera(Nivel n, float x, float suelo, params string[] contenido)
    {
        var go = new GameObject("Cofre");
        go.transform.SetParent(n.items, false);
        go.transform.position = new Vector2(x, suelo + 0.45f);
        Tiled(go.transform, "Cuerpo", new Vector2(x, suelo + 0.38f), new Vector2(1.1f, 0.75f), tablas, new Color(0.5f, 0.32f, 0.16f), 3);
        var tapa = Tiled(go.transform, "Tapa", new Vector2(x, suelo + 0.85f), new Vector2(1.2f, 0.24f), tablas, new Color(0.4f, 0.25f, 0.12f), 4);
        var cerradura = new GameObject("Cerradura");
        cerradura.transform.SetParent(go.transform, false);
        cerradura.transform.position = new Vector2(x, suelo + 0.55f);
        AddSprite(cerradura, circle, 0.22f, 5).color = new Color(1f, 0.85f, 0.3f);
        var c = go.AddComponent<Cofre>();
        var prefabs = new Object[contenido.Length];
        for (int i = 0; i < contenido.Length; i++) prefabs[i] = P[contenido[i]];
        SetArray(c, "contenido", prefabs);
        Set(c, "tapa", tapa.GetComponent<SpriteRenderer>());
        Light(go, 3, new Color(1f, 0.8f, 0.4f), 0.5f, 1.6f);
    }

    static void Pagina(Nivel n, float x, float sobre) => Objeto(n, "Página Perdida", x, sobre);

    // =====================================================================
    // Niveles (sección 2.7). Cada ala tiene 3 Páginas Perdidas escondidas:
    // una en una sala secreta (muro agrietado), una en un pasadizo bajo (agacharse)
    // y otra en lo alto de una ruta de plataformas.
    // =====================================================================

    static string Nivel1()
    {
        var n = NuevoNivel(Aprendizaje, "Ala de Aprendizaje", -24f, 100f, new Vector2(-7f, 0f));

        Suelo(n, -24, 22); Suelo(n, 25, 70); Suelo(n, 73, 100);
        Plat(n, 6, -2, 3); Plat(n, 10, 0, 3); Plat(n, 14, -2, 3);
        Plat(n, 30, -2, 4); Plat(n, 35, 0, 3); Plat(n, 40, 2, 3); Plat(n, 44.5f, 4, 2.5f);
        Plat(n, 49, 5.5f, 2.5f); Plat(n, 53.5f, 7f, 2.5f);
        Plat(n, 71.5f, -3, 1.5f);
        Antorchas(n, -9, 1, 12, 20, 28, 38, 48, 57, 70, 77, 86, 96);

        Escondite(n, -24, -15, true);
        Tunel(n, 61, 67);

        // Escena 1 del guion (sección 2.14)
        Narracion(n, -7f,
            "Lira: ¿Cuánto tiempo llevas aquí abajo...?",
            "Maestra Sable: Aléjate de eso.",
            "Lira: Maestra, estaba en el baúl de los materiales viejos, pensé que...",
            "Maestra Sable: Ese grimorio perteneció a alguien que intentó ir más allá de lo permitido. Ten cuidado con lo que despiertas, Lira.",
            "Lira: ¿Alguien? ¿Quién?",
            "Maestra Sable: Alguien que ya no está. Suéltalo.",
            "Lira: Solo quiero entender qué le pasó. ¿No merece eso saberse?",
            "Maestra Sable: ...Ciérralo. Los ecos ya despertaron y están llenando la sala de práctica.",
            "Maestra Sable: Empecemos por lo básico. Repite conmigo el primer sello arcano y saca a esos espectros de la biblioteca.");
        Dialogo(n, -10f, "Maestra Sable", "Characters/Maestra Sable.png", true, false,
            "Muévete con A/D o el stick y salta con Espacio o X.",
            "Lanza tus hechizos con 1, 2 y 3, o con Cuadrado, R1 y L1. Apunta a cualquier lado con el stick derecho o con el mouse.",
            "Si un enemigo se acerca demasiado, esquiva con Shift o Círculo: durante el esquive nada te toca.",
            "Agáchate con S o con el stick hacia abajo: así pasas por pasadizos bajos y tus hechizos salen a ras de suelo, contra los enemigos pequeños.",
            "La Torre está llena de secretos. Los muros agrietados se rompen con un hechizo y las vasijas guardan cristales y pociones.",
            "Busca las Páginas Perdidas: hay tres en cada ala y cada una te da más vida.",
            "Y Lira... si encuentras páginas sueltas del grimorio, léelas con cuidado.");
        Narracion(n, -13f, "Lira: Esa pared está agrietada... tal vez un hechizo la derribe.");
        Narracion(n, 19f, "Eco: ...Elenora... ¿dónde estás...?", "Lira: ¿Esa voz salió del grimorio?");
        Narracion(n, 58f, "Lira: Ese pasaje es muy bajo. Si me agacho (S o stick abajo) quizá quepa.");
        Narracion(n, 80f, "Lira: Esa sombra es más grande que las demás. Debe ser la que mantiene sellada la salida.");

        var cofre = new GameObject("Cofre_Secreto");
        cofre.transform.SetParent(n.items, false);
        cofre.transform.position = new Vector2(-4f, G + 0.45f);
        var cuerpo = Tiled(cofre.transform, "Cuerpo", new Vector2(-4f, G + 0.45f), new Vector2(1.2f, 0.9f), tablas, new Color(0.55f, 0.35f, 0.18f), 3);
        cuerpo.transform.SetParent(cofre.transform, true);
        var cerradura = new GameObject("Cerradura");
        cerradura.transform.SetParent(cofre.transform, false);
        AddSprite(cerradura, circle, 0.25f, 4).color = new Color(1f, 0.85f, 0.3f);
        cofre.AddComponent<SecretChest>();

        Enemigo(n, "Espectro", 3); Enemigo(n, "Espectro", 10, 0, 1); Enemigo(n, "Espectro", 28); Enemigo(n, "Espectro", 38);
        Volador(n, "Mota", 18, 0.5f); Volador(n, "Mota", 33, 1.5f);
        Enemigo(n, "Espectro", 52, G, 2); Volador(n, "Mota", 76, 1f); Enemigo(n, "Espectro", 78, G, 2);
        var mayor = Enemigo(n, "Mayor", 90, G, 2);

        Objeto(n, "Cristal de Maná", 14, -2);
        Objeto(n, "Poción Menor de Vida", 30, -2);
        Objeto(n, "Fragmento de Grimorio I", 40, 2);
        Objeto(n, "Llave Rúnica", 44.5f, 4);
        Objeto(n, "Poción Menor de Vida", -21, G);
        Pagina(n, -18, G);
        Pagina(n, 53.5f, 7f);
        Pagina(n, 64, G);
        Vasijas(n, 0, 21, 36, 56, 75);
        CofreMadera(n, 68.5f, G, "Cristal de Maná Grande", "Poción Menor de Vida");
        PuntoReaparicion(n, 59.5f);

        Salida(n, 98, mayor.GetComponent<Health>(), "Nivel2_AlaDeFuego", true, Elemento.Fuego);
        return Guardar(n, "Nivel1_AlaDeAprendizaje");
    }

    static string Nivel2()
    {
        var n = NuevoNivel(Fuego, "Ala de Fuego", -22f, 132f, new Vector2(-9f, 0f));

        Suelo(n, -22, 15); Suelo(n, 18, 40); Suelo(n, 43, 70); Suelo(n, 74, 132);
        Lava(n, 15, 18); Lava(n, 40, 43); Lava(n, 70, 74);
        Plat(n, 8, -2, 3); Plat(n, 24, -2, 3); Plat(n, 28, 0, 3); Plat(n, 32, -2, 3); Plat(n, 50, -2, 3);
        Plat(n, 56, -1.5f, 3); Plat(n, 60.5f, 1f, 3); Plat(n, 65, 3.5f, 3);
        Plat(n, 72, -3, 1.5f);
        Antorchas(n, -6, 4, 22, 35, 48, 58, 68, 88, 98, 108, 118, 128);

        Escondite(n, -22, -13, true);
        Tunel(n, 77, 83);

        Narracion(n, -7f,
            "Lira: El Ala de Fuego... la forja antigua sigue encendida después de tantos años.",
            "Lira: Ahora tengo el hechizo de Fuego. Si lo lanzo justo después del Arcano, tal vez logre una explosión.",
            "Lira: (Arcano con 1 o Cuadrado y enseguida Fuego con 2 o R1... o el combo rápido con C o L2)");
        Narracion(n, 30f, "Eco: El fuego fue robado... robado de su laboratorio...");
        Narracion(n, 54f, "Lira: Esas plataformas suben hasta lo alto de la forja. Algo brilla allá arriba.");

        Enemigo(n, "Centinela", 5, G, 2); Enemigo(n, "Salamandra", 12); Enemigo(n, "Centinela", 22, G, 2);
        Enemigo(n, "Salamandra", 35); Enemigo(n, "Centinela", 47, G, 2); Enemigo(n, "Salamandra", 55);
        Enemigo(n, "Salamandra", 66, G, 2); Enemigo(n, "Centinela", 89, G, 1.5f);

        Objeto(n, "Cristal de Maná Grande", 28, 0);
        Objeto(n, "Poción Menor de Vida", 50, -2);
        Objeto(n, "Cristal de Maná Grande", -20, G);
        Pagina(n, -17, G);
        Pagina(n, 65, 3.5f);
        Pagina(n, 80, G);
        Vasijas(n, 2, 26, 46, 61, 76);
        CofreMadera(n, 85, G, "Poción Menor de Vida", "Cristal de Maná");
        PuntoReaparicion(n, 93);
        Geiser(n, 20); Geiser(n, 37); Geiser(n, 58);
        Proyeccion(n, 91.5f,
            "Maestra Sable: Lira, ¿me escuchas? Te hablo desde la biblioteca, a través del grimorio.",
            "Maestra Sable: Kaelor resiste el fuego. Usa el hechizo Arcano y combínalo con Fuego para la Explosión Arcana.",
            "Maestra Sable: Cuando se encienda y embista, atraviésalo con el esquive. Y cuidado con los géiseres de la forja.");

        Dialogo(n, 98, "Kaelor", "Enemies/Kaelor.png", false, true,
            "¿Otra aprendiz que despierta el fuego dormido?",
            "Los ecos no perdonan a quien despierta el fuego dormido.");
        var kaelor = Jefe(n, "Kaelor", 116, G + 1.6f, 102, 130, 0);

        Salida(n, 130, kaelor.GetComponent<Health>(), "Nivel3_AlaDeHielo", true, Elemento.Hielo);
        return Guardar(n, "Nivel2_AlaDeFuego");
    }

    static string Nivel3()
    {
        var n = NuevoNivel(Hielo, "Ala de Hielo", -22f, 132f, new Vector2(-9f, 0f));

        Suelo(n, -22, 20); Suelo(n, 23, 45); Suelo(n, 48, 70); Suelo(n, 73, 132);
        Plat(n, 6, -2, 3); Plat(n, 10, 0, 3); Plat(n, 30, -2, 3); Plat(n, 34, 0, 3); Plat(n, 38, 2, 3);
        Plat(n, 52, -2, 3); Plat(n, 56, 0, 2.5f); Plat(n, 60, 2, 2.5f); Plat(n, 64, 4, 2.5f);
        Antorchas(n, -6, 8, 18, 28, 40, 55, 68, 90, 100, 112, 124);

        Escondite(n, -22, -13, true);
        Tunel(n, 77, 83);

        Narracion(n, -7f,
            "Lira: Hace tanto frío que el tiempo parece detenido.",
            "Lira: Fuego y Hielo juntos deberían crear vapor. Con eso podría cegar a los enemigos.");
        Narracion(n, 33f, "Eco: Ella escribía de noche... notas que nadie podía leer...");
        Narracion(n, 74f, "Lira: Otro pasadizo bajo... Agachada quepo, y de paso me escondo de las esquirlas.");

        Enemigo(n, "Escarchado", 4); Enemigo(n, "Cristal", 16); Enemigo(n, "Escarchado", 27);
        Enemigo(n, "Cristal", 43); Enemigo(n, "Escarchado", 51, G, 2); Enemigo(n, "Cristal", 67);
        Enemigo(n, "Escarchado", 86, G, 1.5f);
        Volador(n, "Gargola", 39f, 2.9f); Volador(n, "Gargola", 81f, G + 3.35f);
        CarambanoTrampa(n, 10, 0); CarambanoTrampa(n, 34, 0); CarambanoTrampa(n, 56, 0);

        Objeto(n, "Cristal de Maná Grande", 10, 0);
        Objeto(n, "Poción Menor de Vida", 34, 0);
        Objeto(n, "Nota cifrada de Elenora", 38, 2);
        Objeto(n, "Poción Menor de Vida", -20, G);
        Pagina(n, -17, G);
        Pagina(n, 64, 4);
        Pagina(n, 80, G);
        Vasijas(n, 0, 25, 49, 58, 75);
        CofreMadera(n, 36, G, "Cristal de Maná Grande");
        PuntoReaparicion(n, 92);
        Proyeccion(n, 89.5f,
            "Maestra Sable: Isolde controla el hielo, pero el fuego la debilita.",
            "Maestra Sable: Cuando caigan esquirlas, no te quedes quieta. Y si ves carámbanos sobre tu cabeza, corre.",
            "Maestra Sable: Ah, y esas estatuas de gárgola... no todas son estatuas.");

        Dialogo(n, 98, "Isolde", "Enemies/Isolde.png", false, true,
            "Fui aprendiz de la Archimaga Elenora.",
            "Desde que sellaron los hechizos, nadie cruza esta ala. Tú tampoco lo harás.");
        var isolde = Jefe(n, "Isolde", 116, G + 1.5f, 102, 130, 0);
        Set(isolde.GetComponent<IsoldeBoss>(), "groundY", G);

        Salida(n, 130, isolde.GetComponent<Health>(), "Nivel4_AlaDeViento", true, Elemento.Viento);
        return Guardar(n, "Nivel3_AlaDeHielo");
    }

    static string Nivel4()
    {
        var n = NuevoNivel(Viento, "Ala de Viento", -22f, 142f, new Vector2(-9f, 0f));

        Suelo(n, -22, 10); Suelo(n, 18, 28); Suelo(n, 38, 48); Suelo(n, 58, 80); Suelo(n, 88, 142);
        PlatMovil(n, 12.5f, G, 3, new Vector2(4f, 0f), 2f);
        PlatMovil(n, 30.5f, G, 3, new Vector2(6f, 0f), 2.5f);
        PlatMovil(n, 50.5f, G, 3, new Vector2(6f, 0f), 2.5f);
        PlatMovil(n, 82.5f, G, 3, new Vector2(4f, 0f), 2f);
        Plat(n, 22, -2, 3); Plat(n, 42, -2, 3); Plat(n, 45, 0, 3);
        Plat(n, 62, -1.5f, 3); Plat(n, 66, 1f, 2.5f); Plat(n, 70.5f, 3.5f, 2.5f); Plat(n, 75, 5.5f, 2.5f);
        Plat(n, 120, -2, 3); Plat(n, 128, -1, 3); Plat(n, 136, -2, 3);

        Escondite(n, -22, -13, true);
        Tunel(n, 93, 99);

        Narracion(n, -7f,
            "Lira: El Ala de Viento está abierta al cielo... las plataformas flotan sobre las corrientes.",
            "Lira: Ya tengo cuatro hechizos pero solo puedo llevar tres. Con Q o R2 cambio el que tengo equipado.");
        Narracion(n, 40f, "Eco: Sus aprendices la buscaron... pero nunca miraron dentro del libro...");
        Narracion(n, 60f, "Lira: Esa corriente de aire sube hasta lo más alto... y algo brilla en la plataforma de arriba.");

        Volador(n, "Ave", 6, 0); Volador(n, "Ave", 23, 1); Volador(n, "Golem", 33, 0);
        Volador(n, "Ave", 43, 2); Volador(n, "Golem", 53, 0); Volador(n, "Ave", 64, 1);
        Volador(n, "Ave", 84, 1.5f); Volador(n, "Golem", 90, 1f);
        Enemigo(n, "Coloso", 74, G, 0);
        Corriente(n, 64, 1.6f, 9f); Corriente(n, 124, 2.4f, 8f);

        Objeto(n, "Cristal de Maná Grande", 22, -2);
        Objeto(n, "Diario de la Archimaga Elenora", 45, 0);
        Objeto(n, "Poción Menor de Vida", 60, G);
        Objeto(n, "Poción Menor de Vida", -20, G);
        Pagina(n, -17, G);
        Pagina(n, 75, 5.5f);
        Pagina(n, 96, G);
        Vasijas(n, 0, 20, 39.5f, 68, 90);
        CofreMadera(n, 100, G, "Cristal de Maná Grande", "Poción Menor de Vida");
        PuntoReaparicion(n, 106);
        Proyeccion(n, 103,
            "Maestra Sable: Threnody vuela alto y resiste el viento. El hielo lo frena: mantén arriba para apuntar en diagonal.",
            "Maestra Sable: En su arena hay una corriente de aire. Úsala para subir y alcanzarlo.",
            "Maestra Sable: Y el Coloso de Raíz que viste... sus pisotones no te alcanzan si estás en el aire.");

        Dialogo(n, 110, "Threnody", "Enemies/Threnody.png", false, true,
            "Los cuatro guardianes fuimos aprendices de la Archimaga Elenora antes de que desapareciera.",
            "Si quieres la verdad, tendrás que ganártela en el aire.");
        var threnody = Jefe(n, "Threnody", 128, 2f, 114, 140, 2f);

        Salida(n, 140, threnody.GetComponent<Health>(), "Nivel5_CorazonDelGrimorio", false, Elemento.Arcano);
        return Guardar(n, "Nivel4_AlaDeViento");
    }

    static string Nivel5()
    {
        var n = NuevoNivel(Corazon, "Corazón del Grimorio", -22f, 132f, new Vector2(-6f, 0f));

        Suelo(n, -22, 40); Suelo(n, 43, 132);
        Plat(n, 8, -2, 3); Plat(n, 16, 0, 3); Plat(n, 24, -2, 3);
        Plat(n, 48, -2, 3); Plat(n, 52, 0, 2.5f); Plat(n, 56, 2, 2.5f); Plat(n, 61, 4, 2.5f);
        Plat(n, 106, -2, 3); Plat(n, 114, 0, 3); Plat(n, 122, -2, 3);
        Antorchas(n, -8, 4, 14, 26, 36, 46, 58, 80, 92, 104, 116, 126);

        Escondite(n, -22, -13, true);
        Tunel(n, 70, 76);

        Narracion(n, -6f,
            "Lira: Maestra Sable... ¿usted sabía todo esto?",
            "Maestra Sable: Lo supe desde el día en que Elenora desapareció. Quise protegerte de su grimorio.",
            "Maestra Sable: Pero ya no hay vuelta atrás. Su eco te espera en el corazón del grimorio.");
        Dialogo(n, -9.5f, "Maestra Sable", "Characters/Maestra Sable.png", true, false,
            "Elenora dominaba los cuatro elementos y resiste el que está usando en cada momento.",
            "Cambia de hechizo y combínalos como te enseñé. Y no le tengas miedo: ella también fue aprendiz alguna vez.",
            "Si quieres llegar con más vida, busca las últimas Páginas Perdidas de la Torre.");
        Narracion(n, 34f, "Eco: Lo que lances... volverá a ti...");

        Enemigo(n, "Eco", 6); Enemigo(n, "Eco", 16, 0, 1); Enemigo(n, "Eco", 30); Enemigo(n, "Espejo", 36, G, 2);
        Enemigo(n, "Eco", 47); Enemigo(n, "Espejo", 58, G, 1.5f); Enemigo(n, "Eco", 64, G, 2);
        Enemigo(n, "Coloso", 79, G, 0);
        Volador(n, "Gargola", 24f, -1.15f); Volador(n, "Gargola", 73f, G + 3.35f);

        // Los espíritus de los guardianes ayudan a Lira antes de la batalla final
        Espiritu(n, 88.5f, "Kaelor", "Enemies/Kaelor.png", 2.4f, Bendicion.Vida, "Bendición de Kaelor: vida restaurada",
            "Kaelor: Aprendiz... no esperaba volver a verte.",
            "Kaelor: Elenora fue nuestra maestra. Libérala de ese eco.",
            "Kaelor: Toma mi fuego: que te devuelva las fuerzas.");
        Espiritu(n, 93f, "Isolde", "Enemies/Isolde.png", 2.3f, Bendicion.Mana, "Bendición de Isolde: maná completo",
            "Isolde: Ya no hay frío entre nosotras, Lira.",
            "Isolde: El eco cambia de elemento en cada fase. Si resiste lo que lanzas, cambia de hechizo.",
            "Isolde: Llévate mi calma: tu maná está completo.");
        Espiritu(n, 97.5f, "Threnody", "Enemies/Threnody.png", 2.4f, Bendicion.VidaMaxima, "Bendición de Threnody: +25 de vida máxima",
            "Threnody: El viento me trajo hasta aquí para despedirme.",
            "Threnody: Cuando Elenora quede sin fuerzas, combina todo lo que aprendiste.",
            "Threnody: Que el viento te sostenga.");

        Objeto(n, "Cristal de Maná Grande", 22.5f, -2);
        Objeto(n, "Cristal de Maná Grande", -20, G);
        Pagina(n, -17, G);
        Pagina(n, 61, 4);
        Pagina(n, 73, G);
        Vasijas(n, 2, 28, 44.5f, 66, 77);
        CofreMadera(n, 85, G, "Poción Mayor de Vida", "Cristal de Maná Grande");
        PuntoReaparicion(n, 83);

        Dialogo(n, 104, "Eco de Elenora", "Enemies/Eco_Archimaga_Elenora.png", false, true,
            "¿Viniste a terminar lo que yo empecé, aprendiz?",
            "Entonces demuéstrame que entiendes el grimorio mejor que yo.");
        var elenora = Jefe(n, "Elenora", 116, 2f, 101, 128, 2f);
        Set(elenora.GetComponent<ElenoraBoss>(), "groundY", G);
        // Al derrotarla el juego termina solo: victoria, epílogo y créditos
        return Guardar(n, "Nivel5_CorazonDelGrimorio");
    }

    // =====================================================================
    // Interfaz: HUD, paneles, menú y créditos
    // =====================================================================

    static readonly Color Dorado = new Color(1f, 0.88f, 0.6f);
    static readonly Color Oscuro = new Color(0.05f, 0.03f, 0.1f, 0.88f);

    static Canvas NuevoCanvas(string name, int order)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static RectTransform UI(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static RectTransform Estirar(string name, Transform parent)
    {
        var rt = UI(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return rt;
    }

    static Image Img(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Sprite sprite, Color color)
    {
        var img = UI(name, parent, anchor, pos, size).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = sprite != null && sprite != square;
        img.raycastTarget = false;
        return img;
    }

    static Text Txt(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string text, int fontSize, TextAnchor align)
    {
        var t = UI(name, parent, anchor, pos, size).gameObject.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = Dorado;
        t.supportRichText = true;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -3);
        return t;
    }

    static Image Barra(Transform parent, string name, string spritePath, Vector2 pos, float w, Color fallback)
    {
        var sprite = LoadSprite(spritePath);
        float h = sprite != null ? w * sprite.rect.height / sprite.rect.width : 34f;
        var tl = new Vector2(0, 1);
        Img(name + "_Fondo", parent, tl, pos, new Vector2(w, h), sprite != null ? sprite : square, sprite != null ? new Color(0.25f, 0.25f, 0.3f, 0.9f) : new Color(0, 0, 0, 0.6f));
        var fill = Img(name, parent, tl, pos, new Vector2(w, h), sprite != null ? sprite : square, sprite != null ? Color.white : fallback);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        return fill;
    }

    static void EventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    static Button Boton(Transform parent, string texto, Vector2 pos, UnityAction accion)
    {
        var img = Img("Boton_" + texto, parent, new Vector2(0.5f, 0.5f), pos, new Vector2(440, 74), square, Color.white);
        img.raycastTarget = true;
        var btn = img.gameObject.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.16f, 0.09f, 0.28f, 0.95f);
        colors.highlightedColor = new Color(0.42f, 0.26f, 0.68f, 1f);
        colors.selectedColor = new Color(0.62f, 0.44f, 0.16f, 1f);  // seleccionado con el control
        colors.pressedColor = new Color(0.85f, 0.65f, 0.2f, 1f);
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        colors.colorMultiplier = 1f;
        btn.colors = colors;
        Txt("Texto", img.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 74), texto, 38, TextAnchor.MiddleCenter);
        UnityEventTools.AddPersistentListener(btn.onClick, accion);
        img.gameObject.AddComponent<SonidoBoton>();
        return btn;
    }

    static GameObject Panel(Transform parent, string name, Color dim)
    {
        var rt = Estirar(name, parent);
        var bg = rt.gameObject.AddComponent<Image>();
        bg.sprite = square;
        bg.color = dim;
        return rt.gameObject;
    }

    static VerticalLayoutGroup Lista(Transform parent, Vector2 pos, Vector2 size)
    {
        var lista = UI("Lista", parent, new Vector2(0.5f, 0.5f), pos, size);
        var layout = lista.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        return layout;
    }

    // Panel oscuro con borde dorado que se estira sin deformar las esquinas
    static Image Recuadro(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var img = Img(name, parent, anchor, pos, size, panelUI, Color.white);
        img.type = Image.Type.Sliced;
        img.preserveAspect = false;
        img.pixelsPerUnitMultiplier = 1.1f;
        return img;
    }

    // Pantalla de logros (menú principal y pausa)
    static GameObject PanelLogros(Transform parent, bool oscurecer, UnityAction volver, out VerticalLayoutGroup lista, out Button botonVolver)
    {
        var c = new Vector2(0.5f, 0.5f);
        var panel = oscurecer ? Panel(parent, "Logros", new Color(0, 0, 0, 0.75f))
                              : UI("Logros", parent, c, Vector2.zero, new Vector2(1920, 1080)).gameObject;
        Recuadro("Fondo", panel.transform, c, new Vector2(0, -15), new Vector2(1440, 930));
        Txt("Titulo", panel.transform, c, new Vector2(0, 395), new Vector2(800, 80), "LOGROS", 58, TextAnchor.MiddleCenter);
        lista = Lista(panel.transform, new Vector2(0, 0), new Vector2(1300, 690));
        botonVolver = Boton(panel.transform, "Volver", new Vector2(0, -415), volver);
        return panel;
    }

    static void HUD(GameObject lira, string titulo)
    {
        var canvas = NuevoCanvas("HUD", 0);
        var hud = canvas.gameObject.AddComponent<HUD>();
        var ui = canvas.gameObject.AddComponent<UIManager>();
        var t = canvas.transform;
        var tl = new Vector2(0, 1);
        var c = new Vector2(0.5f, 0.5f);

        // Viñeta: oscurece las orillas de la pantalla
        var vin = Estirar("Vineta", t).gameObject.AddComponent<Image>();
        vin.sprite = vineta;
        vin.raycastTarget = false;

        // Orillas rojas al recibir daño
        var dano = Estirar("Dano", t).gameObject.AddComponent<Image>();
        dano.sprite = vinetaBlanca;
        dano.color = new Color(0.85f, 0.05f, 0.1f, 0f);
        dano.raycastTarget = false;

        // Retrato de Lira: su cara recortada en un círculo, detrás del marco redondo
        var marcoRetrato = LoadSprite(Sprites + "UI/Marco de retrato.png");
        var mascara = Img("RetratoMascara", t, tl, new Vector2(58, -44), new Vector2(96, 96), circle, new Color(0.16f, 0.1f, 0.26f, 1f));
        mascara.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        Img("Retrato", mascara.transform, c, new Vector2(2, -144), new Vector2(277, 385), LoadSprite(Sprites + "Characters/Lira.png"), Color.white);
        Img("MarcoRetrato", t, tl, new Vector2(16, -14), new Vector2(176, 167), marcoRetrato, Color.white);

        var vida = Barra(t, "BarraVida", Sprites + "UI/Barra de vida.png", new Vector2(205, -22), 380, new Color(0.85f, 0.15f, 0.2f));
        var mana = Barra(t, "BarraMana", Sprites + "UI/Barra de maná.png", new Vector2(205, -82), 380, new Color(0.2f, 0.45f, 1f));

        var icons = new Image[3];
        var frames = new Image[3];
        var teclas = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            var pos = new Vector2(30 + i * 92, -190);
            frames[i] = Img("Espacio_" + (i + 1), t, tl, pos, new Vector2(82, 82), square, new Color(0, 0, 0, 0.5f));
            icons[i] = Img("Icono_" + (i + 1), frames[i].transform, c, Vector2.zero, new Vector2(70, 70), null, Color.white);
            icons[i].preserveAspect = true;
            teclas[i] = Txt("Tecla", frames[i].transform, tl, new Vector2(4, -2), new Vector2(60, 30), (i + 1).ToString(), 24, TextAnchor.UpperLeft);
        }
        var fragments = Txt("Fragmentos", t, tl, new Vector2(310, -192), new Vector2(420, 46), "Fragmentos: 0/5", 32, TextAnchor.MiddleLeft);
        var paginas = Txt("Paginas", t, tl, new Vector2(310, -236), new Vector2(420, 46), "Páginas del ala: 0/3", 32, TextAnchor.MiddleLeft);

        // Barra del jefe
        var top = new Vector2(0.5f, 1);
        var panel = UI("Jefe", t, top, new Vector2(0, -24), new Vector2(820, 80));
        var bossName = Txt("Nombre", panel, top, Vector2.zero, new Vector2(820, 44), "Jefe", 34, TextAnchor.MiddleCenter);
        Img("Fondo", panel, top, new Vector2(0, -42), new Vector2(806, 30), square, new Color(0, 0, 0, 0.75f));
        var bossFill = Img("Vida", panel, top, new Vector2(0, -45), new Vector2(800, 24), square, new Color(0.85f, 0.15f, 0.3f));
        bossFill.type = Image.Type.Filled;
        bossFill.fillMethod = Image.FillMethod.Horizontal;

        Set(hud, "playerHealth", lira.GetComponent<Health>());
        Set(hud, "playerCaster", lira.GetComponent<SpellCaster>());
        Set(hud, "healthFill", vida);
        Set(hud, "manaFill", mana);
        Set(hud, "fragmentsText", fragments);
        Set(hud, "pagesText", paginas);
        SetArray(hud, "slotKeys", teclas);
        var comboHint = Txt("ComboRapido", t, tl, new Vector2(30, -292), new Vector2(520, 40), "", 26, TextAnchor.MiddleLeft);
        Set(hud, "comboHint", comboHint);
        SetArray(hud, "elementIcons", new Object[]
        {
            spells[Elemento.Arcano].icon, spells[Elemento.Fuego].icon, spells[Elemento.Hielo].icon, spells[Elemento.Viento].icon
        });
        SetArray(hud, "slotIcons", icons);
        SetArray(hud, "slotFrames", frames);
        Set(hud, "bossPanel", panel.gameObject);
        Set(hud, "bossFill", bossFill);
        Set(hud, "bossName", bossName);
        Set(hud, "damageFlash", dano);

        // ---- Mensajes ----
        var msg = Recuadro("Mensaje", t, top, new Vector2(0, -125), new Vector2(1250, 96)).rectTransform;
        var msgText = Txt("Texto", msg, c, Vector2.zero, new Vector2(1150, 80), "", 32, TextAnchor.MiddleCenter);
        var msgGroup = msg.gameObject.AddComponent<CanvasGroup>();

        var title = UI("Titulo", t, c, new Vector2(0, 170), new Vector2(1400, 140));
        var titleText = Txt("Texto", title, c, Vector2.zero, new Vector2(1400, 140), titulo, 100, TextAnchor.MiddleCenter);
        var titleGroup = title.gameObject.AddComponent<CanvasGroup>();

        var prompt = Txt("Aviso", t, new Vector2(0.5f, 0f), new Vector2(0, 330), new Vector2(1100, 60), "", 38, TextAnchor.MiddleCenter);

        // ---- Diálogo ----
        var dlg = Recuadro("Dialogo", t, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(1700, 300)).rectTransform;
        Img("FondoRetrato", dlg, new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(230, 250), square, new Color(0.16f, 0.1f, 0.26f, 0.9f));
        var retrato = Img("Retrato", dlg, new Vector2(0, 0.5f), new Vector2(36, 0), new Vector2(214, 236), null, Color.white);
        retrato.preserveAspect = true;
        var dlgName = Txt("Nombre", dlg, tl, new Vector2(290, -18), new Vector2(1000, 52), "", 42, TextAnchor.MiddleLeft);
        var dlgBody = Txt("Texto", dlg, tl, new Vector2(290, -78), new Vector2(1370, 170), "", 38, TextAnchor.UpperLeft);
        dlgBody.color = Color.white;
        var dlgHint = Txt("Ayuda", dlg, new Vector2(1, 0), new Vector2(-24, 14), new Vector2(600, 40), "", 28, TextAnchor.LowerRight);

        // ---- Pausa ----
        var marcoMenu = LoadSprite(Sprites + "UI/Marco de menú.png");
        var pause = Panel(t, "Pausa", new Color(0, 0, 0, 0.6f));
        Img("Marco", pause.transform, c, Vector2.zero, new Vector2(760, 740), marcoMenu, Color.white);
        Txt("Titulo", pause.transform, c, new Vector2(0, 268), new Vector2(600, 90), "PAUSA", 60, TextAnchor.MiddleCenter);
        var reanudar = Boton(pause.transform, "Reanudar", new Vector2(0, 178), ui.BotonReanudar);
        Boton(pause.transform, "Grimorio", new Vector2(0, 96), ui.BotonGrimorio);
        Boton(pause.transform, "Controles", new Vector2(0, 14), ui.BotonControles);
        Boton(pause.transform, "Logros", new Vector2(0, -68), ui.BotonLogros);
        Boton(pause.transform, "Reiniciar nivel", new Vector2(0, -150), ui.BotonReiniciar);
        Boton(pause.transform, "Menú principal", new Vector2(0, -232), ui.BotonMenu);

        // Grimorio: hechizos, combos, mejoras y armas de los enemigos
        var grimorio = Panel(t, "Grimorio", new Color(0, 0, 0, 0.75f));
        Recuadro("Fondo", grimorio.transform, c, new Vector2(0, -15), new Vector2(1500, 960));
        Txt("Titulo", grimorio.transform, c, new Vector2(0, 410), new Vector2(800, 80), "GRIMORIO", 58, TextAnchor.MiddleCenter);
        var contenidoGrimorio = UI("Contenido", grimorio.transform, c, new Vector2(0, 0), new Vector2(1400, 760));
        var gui = grimorio.AddComponent<GrimorioUI>();
        Set(gui, "font", font);
        Set(gui, "contenido", contenidoGrimorio);
        SetArray(gui, "hechizos", new Object[] { spells[Elemento.Arcano], spells[Elemento.Fuego], spells[Elemento.Hielo], spells[Elemento.Viento] });
        SetArray(gui, "combos", new Object[]
        {
            LoadSprite(Sprites + "Spells/Explosión Arcana.png"), LoadSprite(Sprites + "Spells/Vapor Cegador.png"),
            LoadSprite(Sprites + "Spells/Granizo Cortante.png"), LoadSprite(Sprites + "Spells/Tormenta de Ascuas.png")
        });
        SetArray(gui, "mejoras", new Object[] { items["Núcleo de Ascua"], items["Anillo de Escarcha"], items["Pluma Ligera"] });
        Set(gui, "esquive", LoadSprite(Sprites + "Spells/Esquive.png"));
        SetArray(gui, "armas", new Object[]
        {
            LoadSprite(Sprites + "Weapons/Garras de Tinta Corrosiva.png"), LoadSprite(Sprites + "Weapons/Lanza Incandescente.png"),
            LoadSprite(Sprites + "Weapons/Embestida Rocosa.png"), LoadSprite(Sprites + "Weapons/Esquirlas de Hielo.png")
        });
        var volverGrimorio = Boton(grimorio.transform, "Volver", new Vector2(0, -430), ui.BotonVolverPausa);

        var controles = PanelControles(t, true, ui.BotonVolverPausa, out var volverControles);

        var logros = PanelLogros(t, true, ui.BotonVolverPausa, out var lista, out var volver);

        // ---- Game Over ----
        var over = Panel(t, "GameOver", new Color(0.25f, 0f, 0.05f, 0.7f));
        Txt("Titulo", over.transform, c, new Vector2(0, 160), new Vector2(1200, 120), "Lira ha caído", 84, TextAnchor.MiddleCenter);
        var reintentar = Boton(over.transform, "Reintentar", new Vector2(0, 0), ui.BotonReiniciar);
        Boton(over.transform, "Menú principal", new Vector2(0, -95), ui.BotonMenu);

        // ---- Aviso de logro ----
        var toast = Recuadro("AvisoLogro", t, new Vector2(1, 0), new Vector2(-24, 24), new Vector2(500, 130)).rectTransform;
        var toastIcon = Img("Icono", toast, new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(96, 96), null, Color.white);
        toastIcon.preserveAspect = true;
        var toastText = Txt("Texto", toast, new Vector2(0, 0.5f), new Vector2(126, 0), new Vector2(350, 100), "", 27, TextAnchor.MiddleLeft);
        var toastGroup = toast.gameObject.AddComponent<CanvasGroup>();

        Set(ui, "font", font);
        Set(ui, "levelTitle", titulo);
        Set(ui, "messageGroup", msgGroup);
        Set(ui, "messageText", msgText);
        Set(ui, "titleGroup", titleGroup);
        Set(ui, "titleText", titleText);
        Set(ui, "promptText", prompt);
        Set(ui, "dialoguePanel", dlg.gameObject);
        Set(ui, "dialoguePortrait", retrato);
        Set(ui, "dialogueName", dlgName);
        Set(ui, "dialogueBody", dlgBody);
        Set(ui, "dialogueHint", dlgHint);
        Set(ui, "pausePanel", pause);
        Set(ui, "pauseFirst", reanudar.gameObject);
        Set(ui, "achievementsPanel", logros);
        Set(ui, "achievementsList", lista.transform);
        Set(ui, "achievementsFirst", volver.gameObject);
        Set(ui, "controlsPanel", controles);
        Set(ui, "controlsFirst", volverControles.gameObject);
        Set(ui, "grimorioPanel", grimorio);
        Set(ui, "grimorioFirst", volverGrimorio.gameObject);
        Set(ui, "gameOverPanel", over);
        Set(ui, "gameOverFirst", reintentar.gameObject);
        Set(ui, "toastGroup", toastGroup);
        Set(ui, "toastIcon", toastIcon);
        Set(ui, "toastText", toastText);

        var personajes = new (string nombre, string sprite)[]
        {
            ("Lira", "Characters/Lira.png"), ("Maestra Sable", "Characters/Maestra Sable.png"),
            ("Kaelor", "Enemies/Kaelor.png"), ("Isolde", "Enemies/Isolde.png"), ("Threnody", "Enemies/Threnody.png"),
            ("Elenora", "Enemies/Eco_Archimaga_Elenora.png"), ("Eco", "Enemies/Ecos_Menores.png")
        };
        var so = new SerializedObject(ui);
        var prop = so.FindProperty("retratos");
        prop.arraySize = personajes.Length;
        for (int i = 0; i < personajes.Length; i++)
        {
            var el = prop.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("nombre").stringValue = personajes[i].nombre;
            el.FindPropertyRelative("retrato").objectReferenceValue = LoadSprite(Sprites + personajes[i].sprite);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Pantalla de controles (menú principal y pausa)
    static GameObject PanelControles(Transform parent, bool oscurecer, UnityAction volver, out Button botonVolver)
    {
        var c = new Vector2(0.5f, 0.5f);
        var panel = oscurecer ? Panel(parent, "Controles", new Color(0, 0, 0, 0.75f))
                              : UI("Controles", parent, c, Vector2.zero, new Vector2(1920, 1080)).gameObject;
        var t = panel.transform;
        Recuadro("Fondo", t, c, new Vector2(0, -15), new Vector2(1440, 930));
        Txt("Titulo", t, c, new Vector2(0, 395), new Vector2(800, 80), "CONTROLES", 58, TextAnchor.MiddleCenter);

        var filas = new (string accion, string teclado, string control)[]
        {
            ("<b>Acción</b>", "<b>Teclado</b>", "<b>Control PS4</b>"),
            ("Moverse", "A / D o flechas", "Stick o cruceta"),
            ("Saltar (mantén = más alto)", "Espacio o W", "X"),
            ("Agacharse", "S o flecha abajo", "Stick abajo"),
            ("Hechizo 1 / 2 / 3", "1, 2, 3  (J o clic)", "□  /  R1  /  L1"),
            ("Apuntar a cualquier lado", "Flechas o mouse", "Stick derecho"),
            ("Combo rápido", "C", "L2"),
            ("Cambiar hechizo equipado", "Q", "R2"),
            ("Esquive (invulnerable)", "Shift o K", "Círculo"),
            ("Hablar / abrir cofres", "E", "Triángulo"),
            ("Pausa", "Esc", "Options"),
        };
        for (int i = 0; i < filas.Length; i++)
        {
            float y = 305 - i * 54;
            if (i > 0 && i % 2 == 0)
                Img("Franja", t, c, new Vector2(0, y), new Vector2(1320, 50), square, new Color(1f, 1f, 1f, 0.05f));
            Txt("Accion", t, c, new Vector2(-370, y), new Vector2(560, 50), filas[i].accion, 32, TextAnchor.MiddleLeft);
            Txt("Teclado", t, c, new Vector2(150, y), new Vector2(420, 50), filas[i].teclado, 32, TextAnchor.MiddleLeft).color = Color.white;
            Txt("Control", t, c, new Vector2(510, y), new Vector2(300, 50), filas[i].control, 32, TextAnchor.MiddleLeft).color = Color.white;
        }
        Txt("Consejos", t, c, new Vector2(0, -322), new Vector2(1300, 90),
            "Combos: lanza dos elementos distintos seguidos (menos de 1 segundo) o usa el combo rápido.\n" +
            "Rompe vasijas y muros agrietados con tus hechizos: esconden objetos y Páginas Perdidas.",
            27, TextAnchor.MiddleCenter);
        botonVolver = Boton(t, "Volver", new Vector2(0, -415), volver);
        return panel;
    }

    static string CrearMenu()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PrefabUtility.InstantiatePrefab(P["GameManager"]);

        var cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(0, 0, -10);
        EventSystem();

        var canvas = NuevoCanvas("Menu", 0);
        var t = canvas.transform;
        var c = new Vector2(0.5f, 0.5f);

        var fondo = Estirar("Fondo", t).gameObject.AddComponent<Image>();
        fondo.sprite = LoadSprite(Sprites + "Backgrounds/Corazón del Grimorio.jpg");
        fondo.raycastTarget = false;
        var dim = Estirar("Oscurecer", t).gameObject.AddComponent<Image>();
        dim.sprite = vineta;
        dim.raycastTarget = false;

        var menu = canvas.gameObject.AddComponent<MainMenu>();
        var marco = LoadSprite(Sprites + "UI/Marco de menú.png");

        var main = UI("Principal", t, c, Vector2.zero, new Vector2(1920, 1080));
        Img("Lira", main, c, new Vector2(-620, -120), new Vector2(380, 620), LoadSprite(Sprites + "Characters/Lira.png"), Color.white);
        Img("Elenora", main, c, new Vector2(620, -80), new Vector2(420, 640), LoadSprite(Sprites + "Enemies/Eco_Archimaga_Elenora.png"), new Color(1f, 1f, 1f, 0.85f));
        Txt("Titulo", main, c, new Vector2(0, 380), new Vector2(1400, 140), "ECOS DEL GRIMORIO", 104, TextAnchor.MiddleCenter);
        Img("Marco", main, c, new Vector2(0, -70), new Vector2(680, 640), marco, Color.white);
        var nueva = Boton(main, "Nueva Partida", new Vector2(0, 150), menu.NuevaPartida);
        var cont = Boton(main, "Continuar", new Vector2(0, 65), menu.Continuar);
        Boton(main, "Controles", new Vector2(0, -20), menu.ShowControls);
        Boton(main, "Logros", new Vector2(0, -105), menu.ShowAchievements);
        Boton(main, "Créditos", new Vector2(0, -190), menu.Creditos);
        Boton(main, "Salir", new Vector2(0, -275), menu.Salir);
        Txt("Ayuda", main, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(1400, 40),
            "Teclado o control de PS4 / Xbox", 24, TextAnchor.MiddleCenter);

        var logros = PanelLogros(t, false, menu.ShowMain, out var lista, out var volver);

        Set(menu, "continueButton", cont);
        Set(menu, "mainPanel", main.gameObject);
        Set(menu, "achievementsPanel", logros);
        Set(menu, "achievementsList", lista.transform);
        Set(menu, "font", font);
        Set(menu, "firstButton", nueva.gameObject);
        Set(menu, "achievementsBack", volver.gameObject);
        var controlesMenu = PanelControles(t, false, menu.ShowMain, out var volverMenu);
        Set(menu, "controlsPanel", controlesMenu);
        Set(menu, "controlsBack", volverMenu.gameObject);

        string path = $"{Scenes}/{GameManager.MenuScene}.unity";
        EditorSceneManager.SaveScene(scene, path);
        return path;
    }

    static string CrearPrologo() => CrearHistoria(GameManager.PrologueScene, "", new (string, string)[]
    {
        ("Backgrounds/Corazón del Grimorio.jpg", "Hace años, la Archimaga Elenora intentó unir los cuatro elementos en un solo grimorio. Una noche su laboratorio quedó en silencio... y ella desapareció."),
        ("Backgrounds/ALA_DE_APRENDIZAJE.png", "Los maestros de la Torre de Cristal sellaron sus hechizos y prohibieron hablar de ella. Con el tiempo, su nombre se volvió un rumor entre aprendices."),
        ("Characters/Lira.png", "Lira, una aprendiz curiosa e impulsiva, encuentra en un baúl olvidado un grimorio de práctica con la cubierta agrietada. Sus páginas laten con una luz tenue."),
        ("Enemies/Eco_Archimaga_Elenora.png", "Al abrirlo, los hechizos sellados despiertan. Ecos de magia escapan del libro y empiezan a corromper cada ala de la Torre."),
        ("Backgrounds/Ala de Fuego.jpg", "Para restaurar el equilibrio, Lira tendrá que recorrer las cinco alas, aprender los cuatro elementos y enfrentar a los guardianes que protegen los secretos de Elenora.")
    });

    // Epílogo: se muestra al derrotar al Eco de la Archimaga Elenora y luego pasa a los créditos
    static string CrearEpilogo() => CrearHistoria(GameManager.EpilogueScene, GameManager.CreditsScene, new (string, string)[]
    {
        ("Backgrounds/Corazón del Grimorio.jpg", "El eco de Elenora se desvanece entre las páginas. Por primera vez en muchos años, el grimorio guarda silencio."),
        ("Enemies/Eco_Archimaga_Elenora.png", "Antes de desaparecer, la Archimaga sonríe: los cuatro elementos ya no pelean entre sí. Lira logró lo que ella nunca pudo."),
        ("Characters/Maestra Sable.png", "La Maestra Sable encuentra a Lira en la biblioteca con el grimorio completo entre las manos. Esta vez no le pide que lo suelte."),
        ("Backgrounds/Ala de Viento.jpg", "Kaelor, Isolde y Threnody por fin pueden descansar. Las alas de la Torre de Cristal vuelven a brillar."),
        ("Characters/Lira.png", "Y Lira, la aprendiz que se atrevió a abrir el grimorio, se convierte en su nueva guardiana.")
    });

    static string CrearHistoria(string nombreEscena, string siguiente, (string sprite, string texto)[] escenas)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PrefabUtility.InstantiatePrefab(P["GameManager"]);

        var cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(0, 0, -10);

        var canvas = NuevoCanvas("Prologo", 0);
        var t = canvas.transform;
        var c = new Vector2(0.5f, 0.5f);
        var grupo = Estirar("Contenido", t).gameObject.AddComponent<CanvasGroup>();

        var img = Img("Imagen", grupo.transform, c, new Vector2(0, 150), new Vector2(1300, 640), null, Color.white);
        img.preserveAspect = true;
        var vin = Estirar("Vineta", t).gameObject.AddComponent<Image>();
        vin.sprite = vineta;
        vin.raycastTarget = false;
        var texto = Txt("Texto", grupo.transform, new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(1500, 260), "", 42, TextAnchor.UpperCenter);
        texto.color = Color.white;
        var ayuda = Txt("Ayuda", t, new Vector2(1, 0), new Vector2(-30, 24), new Vector2(800, 40), "", 28, TextAnchor.LowerRight);

        var prologo = canvas.gameObject.AddComponent<Prologo>();
        var so = new SerializedObject(prologo);
        var prop = so.FindProperty("escenas");
        prop.arraySize = escenas.Length;
        for (int i = 0; i < escenas.Length; i++)
        {
            var el = prop.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("imagen").objectReferenceValue = LoadSprite(Sprites + escenas[i].sprite);
            el.FindPropertyRelative("texto").stringValue = escenas[i].texto;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        Set(prologo, "imagen", img);
        Set(prologo, "texto", texto);
        Set(prologo, "ayuda", ayuda);
        Set(prologo, "grupo", grupo);
        Set(prologo, "siguienteEscena", siguiente);

        string path = $"{Scenes}/{nombreEscena}.unity";
        EditorSceneManager.SaveScene(scene, path);
        return path;
    }

    static string CrearCreditos()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PrefabUtility.InstantiatePrefab(P["GameManager"]);

        var cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(0, 0, -10);

        var canvas = NuevoCanvas("Creditos", 0);
        var t = canvas.transform;
        var fondo = Estirar("Fondo", t).gameObject.AddComponent<Image>();
        fondo.sprite = LoadSprite(Sprites + "Backgrounds/ALA_DE_APRENDIZAJE.png");
        fondo.color = new Color(0.35f, 0.35f, 0.45f);
        var vin = Estirar("Vineta", t).gameObject.AddComponent<Image>();
        vin.sprite = vineta;

        var texto = Txt("Texto", t, new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(1400, 1800), "", 40, TextAnchor.UpperCenter);
        texto.rectTransform.pivot = new Vector2(0.5f, 1f);

        var credits = canvas.gameObject.AddComponent<CreditsScreen>();
        Set(credits, "scrollingText", texto.rectTransform);
        Set(credits, "textComponent", texto);
        Set(credits, "endY", 2600f);

        string path = $"{Scenes}/{GameManager.CreditsScene}.unity";
        EditorSceneManager.SaveScene(scene, path);
        return path;
    }
}
