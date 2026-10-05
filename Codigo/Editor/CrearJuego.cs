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

    static Sprite square, circle, ladrillo, tablas, borde, vineta;
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
            "Controles:\nA/D o flechas: moverse    Espacio: saltar\n1, 2, 3: hechizos equipados    Q: cambiar hechizo\n" +
            "J o clic: repetir hechizo    E: hablar / abrir\nEsc: pausa (L: logros)\n\nAbre MenuPrincipal y presiona Play.", "OK");
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
        Item("Poción Menor de Vida", TipoItem.Vida, 35, "Items/Poción de Vida.png", "Restaura parte de la vida.");
        Item("Poción Mayor de Vida", TipoItem.Vida, 100, "Items/Poción Mayor de Vida.png", "Restaura toda la vida.");
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
            P[pair.Key] = PickupPrefab(pair.Value, tint);
        }

        // Enemigos (sección 2.10)
        var espectro = Enemy("Espectro de tinta", 30, 0, 10, 1.5f, 3f, 5f);
        var mota = Enemy("Mota Corrupta", 10, 0, 0, 1.5f, 1.5f, 6f);
        var mayor = Enemy("Espectro Mayor", 60, 3, 20, 1f, 2.2f, 6f, P["Cristal de Maná"], 0.5f);
        var centinela = Enemy("Centinela de Ceniza", 45, 0, 15, 1.5f, 3f, 6f);
        var salamandra = Enemy("Salamandra de Forja", 25, 0, 12, 2.5f, 4f, 7f, immune: Elemento.Fuego);
        var escarchado = Enemy("Espectro Escarchado", 45, 0, 12, 1.6f, 3.2f, 6f, immune: Elemento.Hielo);
        var cristal = Enemy("Cristal Viviente", 35, 0, 8, 0f, 0f, 9f);
        var ave = Enemy("Ave de Tormenta", 20, 0, 10, 2f, 2f, 7f);
        var golem = Enemy("Golem de Piedra Suspendida", 60, 0, 20, 1f, 1f, 6f);
        var eco = Enemy("Eco Menor", 30, 0, 10, 1.8f, 3.2f, 7f, P["Cristal de Maná"], 0.25f);
        var espejo = Enemy("Guardián Espejo", 80, 0, 15, 1.5f, 1.5f, 10f);

        P["Espectro"] = Walker("Espectro_de_Tinta", "Enemies/Espectro_De_Tinta.png", 1.5f, espectro);
        P["Mayor"] = Walker("Espectro_Mayor", "Enemies/Espectro_Mayor.png", 2.3f, mayor);
        P["Centinela"] = Walker("Centinela_de_Ceniza", "Enemies/Centinelas_De_Ceniza.png", 1.9f, centinela);
        P["Salamandra"] = Walker("Salamandra_de_Forja", "Enemies/Salamandras_De_Forja.png", 0.9f, salamandra,
            ai => { Set(ai, "jumpAttack", true); Set(ai, "jumpAttackForce", 8f); });
        P["Escarchado"] = Walker("Espectro_Escarchado", "Enemies/Espectros_Escarchados.png", 1.7f, escarchado);
        P["Eco"] = Walker("Eco_Menor", "Enemies/Ecos_Menores.png", 1.4f, eco,
            ai => Set(ai, "projectilePrefab", P["ProyEnemigo"]), typeof(EcoMenorAI));
        P["Espejo"] = Walker("Guardian_Espejo", "Enemies/Guardian_Espejo.png", 2.1f, espejo,
            ai => Set(ai, "projectilePrefab", P["ProyEnemigo"]), typeof(MirrorEnemyAI));

        P["Cristal"] = Ranged("Cristal_Viviente", "Enemies/Cristales_Vivientes.png", 1.5f, cristal);

        P["Mota"] = Flyer("Mota_Corrupta", "Enemies/Motas_Corruptas.png", 0.9f, mota,
            ai => { Set(ai, "explodeOnContact", true); Set(ai, "driftTowardsPlayer", true); Set(ai, "hoverRadius", 0.8f); });
        P["Ave"] = Flyer("Ave_de_Tormenta", "Enemies/Aves_De_Tormenta.png", 1.0f, ave,
            ai => { Set(ai, "diveSpeed", 9f); Set(ai, "diveCooldown", 2.5f); });
        P["Golem"] = Flyer("Golem_de_Piedra", "Enemies/Golems_De_Piedra_Suspendida.png", 1.5f, golem,
            ai => { Set(ai, "diveSpeed", 6f); Set(ai, "diveCooldown", 3.5f); Set(ai, "hoverRadius", 0.6f); });

        // Jefes
        P["Kaelor"] = Boss("Kaelor", "Enemies/Kaelor.png", 3.0f, typeof(KaelorBoss), 320, 20, 1.8f, false, new[] { 0.5f },
            new[] { P["Núcleo de Ascua"], P["Fragmento de Grimorio II"] }, (Elemento.Fuego, 0.5f), (Elemento.Hielo, 1.5f));
        P["Isolde"] = Boss("Isolde", "Enemies/Isolde.png", 2.7f, typeof(IsoldeBoss), 350, 15, 2f, false, new[] { 0.5f },
            new[] { P["Anillo de Escarcha"], P["Fragmento de Grimorio III"] }, (Elemento.Hielo, 0.3f), (Elemento.Fuego, 1.5f));
        P["Threnody"] = Boss("Threnody", "Enemies/Threnody.png", 3.0f, typeof(ThrenodyBoss), 380, 18, 3f, true, new[] { 0.5f },
            new[] { P["Pluma Ligera"], P["Fragmento de Grimorio IV"] }, (Elemento.Viento, 0.3f), (Elemento.Hielo, 1.3f));
        Set(P["Threnody"].GetComponent<ThrenodyBoss>(), "minionPrefab", P["Ave"]);
        P["Elenora"] = Boss("Eco de la Archimaga Elenora", "Enemies/Eco_Archimaga_Elenora.png", 3.3f, typeof(ElenoraBoss), 600, 20, 3f, true,
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

    static GameObject PickupPrefab(ItemData item, Color tint)
    {
        var go = new GameObject(item.itemName);
        var sr = AddSprite(go, item.icon != null ? item.icon : circle, 0.8f, 3);
        sr.color = item.icon != null ? tint : Color.yellow;
        go.AddComponent<CircleCollider2D>().isTrigger = true;
        Set(go.AddComponent<Pickup>(), "item", item);
        Light(go, 3, new Color(1f, 0.85f, 0.5f), 0.8f, 1.8f);
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
        Set(health, "maxHealth", 100f);
        Set(health, "invulnerableTime", 1.2f);
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

        return SavePrefab(go, Prefabs + "/GameManager.prefab");
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
        return n;
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
    }

    static GameObject Plat(Nivel n, float x, float top, float w)
    {
        var go = Solido(n.geo, $"Plataforma_{x}", new Vector2(x, top - 0.25f), new Vector2(w, 0.5f), tablas, n.tema.plataforma);
        go.GetComponent<SpriteRenderer>().sortingOrder = 2;
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
    static void Dialogo(Nivel n, float x, string quien, string retrato, bool visible, bool automatico, params string[] lineas)
    {
        var go = new GameObject("Dialogo_" + FileName(quien));
        go.transform.SetParent(n.items, false);
        var sprite = retrato != null ? LoadSprite(Sprites + retrato) : null;
        if (visible && sprite != null)
        {
            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            AddSprite(vis, sprite, 1.95f, 9);
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

    // =====================================================================
    // Niveles (sección 2.7)
    // =====================================================================

    static string Nivel1()
    {
        var n = NuevoNivel(Aprendizaje, "Ala de Aprendizaje", -12f, 62f, new Vector2(-7f, 0f));

        Suelo(n, -12, 22); Suelo(n, 25, 62);
        Plat(n, 6, -2, 3); Plat(n, 10, 0, 3); Plat(n, 14, -2, 3);
        Plat(n, 30, -2, 4); Plat(n, 35, 0, 3); Plat(n, 40, 2, 3); Plat(n, 44.5f, 4, 2.5f);
        Antorchas(n, -9, 1, 12, 20, 28, 38, 48, 57);

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
            "Lanza el hechizo con J o Cuadrado. Mantén arriba para lanzarlo en diagonal.",
            "Si un enemigo se acerca demasiado, esquiva con Shift o Círculo: durante el esquive nada te toca.",
            "Y Lira... si encuentras páginas sueltas del grimorio, léelas con cuidado.");
        Narracion(n, 19f, "Eco: ...Elenora... ¿dónde estás...?", "Lira: ¿Esa voz salió del grimorio?");
        Narracion(n, 46f, "Lira: Esa sombra es más grande que las demás. Debe ser la que mantiene sellada la salida.");

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
        var mayor = Enemigo(n, "Mayor", 52, G, 2);

        Objeto(n, "Cristal de Maná", 14, -2);
        Objeto(n, "Poción Menor de Vida", 30, -2);
        Objeto(n, "Fragmento de Grimorio I", 40, 2);
        Objeto(n, "Llave Rúnica", 44.5f, 4);

        Salida(n, 60, mayor.GetComponent<Health>(), "Nivel2_AlaDeFuego", true, Elemento.Fuego);
        return Guardar(n, "Nivel1_AlaDeAprendizaje");
    }

    static string Nivel2()
    {
        var n = NuevoNivel(Fuego, "Ala de Fuego", -12f, 92f, new Vector2(-9f, 0f));

        Suelo(n, -12, 15); Suelo(n, 18, 40); Suelo(n, 43, 92);
        Lava(n, 15, 18); Lava(n, 40, 43);
        Plat(n, 8, -2, 3); Plat(n, 24, -2, 3); Plat(n, 28, 0, 3); Plat(n, 32, -2, 3); Plat(n, 52, -2, 3);
        Antorchas(n, -6, 4, 22, 35, 48, 58, 68, 78, 88);
        Narracion(n, -7f,
            "Lira: El Ala de Fuego... la forja antigua sigue encendida después de tantos años.",
            "Lira: Ahora tengo el hechizo de Fuego. Si lo lanzo justo después del Arcano, tal vez logre una explosión.",
            "Lira: (2 o R1 para cambiar de hechizo, y lanzar rápido uno después del otro)");
        Narracion(n, 30f, "Eco: El fuego fue robado... robado de su laboratorio...");
        Enemigo(n, "Centinela", 5, G, 2); Enemigo(n, "Salamandra", 12); Enemigo(n, "Centinela", 22, G, 2);
        Enemigo(n, "Salamandra", 35); Enemigo(n, "Centinela", 47, G, 2); Enemigo(n, "Salamandra", 55);

        Objeto(n, "Cristal de Maná Grande", 28, 0);
        Objeto(n, "Poción Menor de Vida", 52, -2);
        PuntoReaparicion(n, 60);

        Dialogo(n, 66, "Kaelor", "Enemies/Kaelor.png", false, true,
            "¿Otra aprendiz que despierta el fuego dormido?",
            "Los ecos no perdonan a quien despierta el fuego dormido.");
        var kaelor = Jefe(n, "Kaelor", 82, G + 1.6f, 68, 90, 0);

        Salida(n, 90, kaelor.GetComponent<Health>(), "Nivel3_AlaDeHielo", true, Elemento.Hielo);
        return Guardar(n, "Nivel2_AlaDeFuego");
    }

    static string Nivel3()
    {
        var n = NuevoNivel(Hielo, "Ala de Hielo", -12f, 92f, new Vector2(-9f, 0f));

        Suelo(n, -12, 20); Suelo(n, 23, 45); Suelo(n, 48, 92);
        Plat(n, 6, -2, 3); Plat(n, 10, 0, 3); Plat(n, 30, -2, 3); Plat(n, 34, 0, 3); Plat(n, 38, 2, 3);
        Antorchas(n, -6, 8, 18, 28, 40, 55, 70, 85);
        Narracion(n, -7f,
            "Lira: Hace tanto frío que el tiempo parece detenido.",
            "Lira: Fuego y Hielo juntos deberían crear vapor. Con eso podría cegar a los enemigos.");
        Narracion(n, 33f, "Eco: Ella escribía de noche... notas que nadie podía leer...");
        Enemigo(n, "Escarchado", 4); Enemigo(n, "Cristal", 16); Enemigo(n, "Escarchado", 27);
        Enemigo(n, "Cristal", 43); Enemigo(n, "Escarchado", 52); Enemigo(n, "Cristal", 57);

        Objeto(n, "Cristal de Maná Grande", 10, 0);
        Objeto(n, "Poción Menor de Vida", 34, 0);
        Objeto(n, "Nota cifrada de Elenora", 38, 2);
        PuntoReaparicion(n, 62);

        Dialogo(n, 66, "Isolde", "Enemies/Isolde.png", false, true,
            "Fui aprendiz de la Archimaga Elenora.",
            "Desde que sellaron los hechizos, nadie cruza esta ala. Tú tampoco lo harás.");
        var isolde = Jefe(n, "Isolde", 82, G + 1.5f, 68, 90, 0);
        Set(isolde.GetComponent<IsoldeBoss>(), "groundY", G);

        Salida(n, 90, isolde.GetComponent<Health>(), "Nivel4_AlaDeViento", true, Elemento.Viento);
        return Guardar(n, "Nivel3_AlaDeHielo");
    }

    static string Nivel4()
    {
        var n = NuevoNivel(Viento, "Ala de Viento", -12f, 100f, new Vector2(-9f, 0f));

        Suelo(n, -12, 10); Suelo(n, 18, 28); Suelo(n, 38, 48); Suelo(n, 58, 100);
        PlatMovil(n, 12.5f, G, 3, new Vector2(4f, 0f), 2f);
        PlatMovil(n, 30.5f, G, 3, new Vector2(6f, 0f), 2.5f);
        PlatMovil(n, 50.5f, G, 3, new Vector2(6f, 0f), 2.5f);
        Plat(n, 22, -2, 3); Plat(n, 42, -2, 3); Plat(n, 45, 0, 3);
        Plat(n, 78, -2, 3); Plat(n, 86, -1, 3); Plat(n, 94, -2, 3);
        Narracion(n, -7f,
            "Lira: El Ala de Viento está abierta al cielo... las plataformas flotan sobre las corrientes.",
            "Lira: Ya tengo cuatro hechizos pero solo puedo llevar tres. Con Q o R2 cambio el que tengo equipado.");
        Narracion(n, 40f, "Eco: Sus aprendices la buscaron... pero nunca miraron dentro del libro...");
        Volador(n, "Ave", 6, 0); Volador(n, "Ave", 23, 1); Volador(n, "Golem", 33, 0);
        Volador(n, "Ave", 43, 2); Volador(n, "Golem", 53, 0); Volador(n, "Ave", 62, 1);

        Objeto(n, "Cristal de Maná Grande", 22, -2);
        Objeto(n, "Diario de la Archimaga Elenora", 45, 0);
        Objeto(n, "Poción Menor de Vida", 60, G);
        PuntoReaparicion(n, 66);

        Dialogo(n, 70, "Threnody", "Enemies/Threnody.png", false, true,
            "Los cuatro guardianes fuimos aprendices de la Archimaga Elenora antes de que desapareciera.",
            "Si quieres la verdad, tendrás que ganártela en el aire.");
        var threnody = Jefe(n, "Threnody", 88, 2f, 72, 98, 2f);

        Salida(n, 98, threnody.GetComponent<Health>(), "Nivel5_CorazonDelGrimorio", false, Elemento.Arcano);
        return Guardar(n, "Nivel4_AlaDeViento");
    }

    static string Nivel5()
    {
        var n = NuevoNivel(Corazon, "Corazón del Grimorio", -12f, 92f, new Vector2(-6f, 0f));

        Suelo(n, -12, 92);
        Plat(n, 8, -2, 3); Plat(n, 16, 0, 3); Plat(n, 24, -2, 3);
        Plat(n, 66, -2, 3); Plat(n, 74, 0, 3); Plat(n, 82, -2, 3);
        Antorchas(n, -8, 4, 14, 26, 36, 46, 58, 70, 84);

        Narracion(n, -6f,
            "Lira: Maestra Sable... ¿usted sabía todo esto?",
            "Maestra Sable: Lo supe desde el día en que Elenora desapareció. Quise protegerte de su grimorio.",
            "Maestra Sable: Pero ya no hay vuelta atrás. Su eco te espera en el corazón del grimorio.");
        Dialogo(n, -9.5f, "Maestra Sable", "Characters/Maestra Sable.png", true, false,
            "Elenora dominaba los cuatro elementos y resiste el que está usando en cada momento.",
            "Cambia de hechizo y combínalos como te enseñé. Y no le tengas miedo: ella también fue aprendiz alguna vez.");
        Narracion(n, 34f, "Eco: Lo que lances... volverá a ti...");

        Enemigo(n, "Eco", 6); Enemigo(n, "Eco", 16, 0, 1); Enemigo(n, "Eco", 30); Enemigo(n, "Espejo", 40); Enemigo(n, "Eco", 48);

        Objeto(n, "Cristal de Maná Grande", 24, -2);
        Objeto(n, "Poción Mayor de Vida", 52, G);
        PuntoReaparicion(n, 56);

        Dialogo(n, 61, "Eco de Elenora", "Enemies/Eco_Archimaga_Elenora.png", false, true,
            "¿Viniste a terminar lo que yo empecé, aprendiz?",
            "Entonces demuéstrame que entiendes el grimorio mejor que yo.");
        var elenora = Jefe(n, "Elenora", 76, 2f, 62, 90, 2f);
        Set(elenora.GetComponent<ElenoraBoss>(), "groundY", G);

        Salida(n, 90, elenora.GetComponent<Health>(), GameManager.CreditsScene, false, Elemento.Arcano);
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

        // Retrato de Lira con marco, barras y hechizos equipados
        var marcoRetrato = LoadSprite(Sprites + "UI/Marco de retrato.png");
        Img("Retrato", t, tl, new Vector2(38, -36), new Vector2(118, 118), LoadSprite(Sprites + "Characters/Lira.png"), Color.white);
        Img("MarcoRetrato", t, tl, new Vector2(20, -20), new Vector2(155, 155), marcoRetrato, Color.white);

        var vida = Barra(t, "BarraVida", Sprites + "UI/Barra de vida.png", new Vector2(185, -22), 380, new Color(0.85f, 0.15f, 0.2f));
        var mana = Barra(t, "BarraMana", Sprites + "UI/Barra de maná.png", new Vector2(185, -82), 380, new Color(0.2f, 0.45f, 1f));

        var icons = new Image[3];
        var frames = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var pos = new Vector2(30 + i * 92, -190);
            frames[i] = Img("Espacio_" + (i + 1), t, tl, pos, new Vector2(82, 82), square, new Color(0, 0, 0, 0.5f));
            icons[i] = Img("Icono_" + (i + 1), frames[i].transform, c, Vector2.zero, new Vector2(70, 70), null, Color.white);
            icons[i].preserveAspect = true;
            Txt("Tecla", frames[i].transform, tl, new Vector2(4, -2), new Vector2(30, 30), (i + 1).ToString(), 24, TextAnchor.UpperLeft);
        }
        var fragments = Txt("Fragmentos", t, tl, new Vector2(310, -205), new Vector2(360, 50), "Fragmentos: 0/5", 34, TextAnchor.MiddleLeft);

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
        SetArray(hud, "elementIcons", new Object[]
        {
            spells[Elemento.Arcano].icon, spells[Elemento.Fuego].icon, spells[Elemento.Hielo].icon, spells[Elemento.Viento].icon
        });
        SetArray(hud, "slotIcons", icons);
        SetArray(hud, "slotFrames", frames);
        Set(hud, "bossPanel", panel.gameObject);
        Set(hud, "bossFill", bossFill);
        Set(hud, "bossName", bossName);

        // ---- Mensajes ----
        var msg = UI("Mensaje", t, top, new Vector2(0, -125), new Vector2(1200, 80));
        msg.gameObject.AddComponent<Image>().color = Oscuro;
        var msgText = Txt("Texto", msg, c, Vector2.zero, new Vector2(1180, 76), "", 34, TextAnchor.MiddleCenter);
        var msgGroup = msg.gameObject.AddComponent<CanvasGroup>();

        var title = UI("Titulo", t, c, new Vector2(0, 170), new Vector2(1400, 140));
        var titleText = Txt("Texto", title, c, Vector2.zero, new Vector2(1400, 140), titulo, 100, TextAnchor.MiddleCenter);
        var titleGroup = title.gameObject.AddComponent<CanvasGroup>();

        var prompt = Txt("Aviso", t, new Vector2(0.5f, 0f), new Vector2(0, 330), new Vector2(1100, 60), "", 38, TextAnchor.MiddleCenter);

        // ---- Diálogo ----
        var dlg = UI("Dialogo", t, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(1700, 290));
        dlg.gameObject.AddComponent<Image>().color = Oscuro;
        var retrato = Img("Retrato", dlg, new Vector2(0, 0.5f), new Vector2(35, 0), new Vector2(200, 235), null, Color.white);
        retrato.preserveAspect = true;
        Img("Marco", dlg, new Vector2(0, 0.5f), new Vector2(15, 0), new Vector2(245, 265), marcoRetrato, Color.white);
        var dlgName = Txt("Nombre", dlg, tl, new Vector2(290, -18), new Vector2(1000, 52), "", 42, TextAnchor.MiddleLeft);
        var dlgBody = Txt("Texto", dlg, tl, new Vector2(290, -78), new Vector2(1370, 170), "", 38, TextAnchor.UpperLeft);
        dlgBody.color = Color.white;
        var dlgHint = Txt("Ayuda", dlg, new Vector2(1, 0), new Vector2(-24, 14), new Vector2(600, 40), "", 28, TextAnchor.LowerRight);

        // ---- Pausa ----
        var marcoMenu = LoadSprite(Sprites + "UI/Marco de menú.png");
        var pause = Panel(t, "Pausa", new Color(0, 0, 0, 0.6f));
        Img("Marco", pause.transform, c, Vector2.zero, new Vector2(700, 660), marcoMenu, Color.white);
        Txt("Titulo", pause.transform, c, new Vector2(0, 200), new Vector2(600, 90), "PAUSA", 64, TextAnchor.MiddleCenter);
        var reanudar = Boton(pause.transform, "Reanudar", new Vector2(0, 90), ui.BotonReanudar);
        Boton(pause.transform, "Logros", new Vector2(0, 0), ui.BotonLogros);
        Boton(pause.transform, "Reiniciar nivel", new Vector2(0, -90), ui.BotonReiniciar);
        Boton(pause.transform, "Menú principal", new Vector2(0, -180), ui.BotonMenu);

        var logros = Panel(t, "Logros", new Color(0, 0, 0, 0.75f));
        Img("Marco", logros.transform, c, new Vector2(0, -10), new Vector2(1150, 1000), marcoMenu, Color.white);
        Txt("Titulo", logros.transform, c, new Vector2(0, 430), new Vector2(800, 80), "LOGROS", 60, TextAnchor.MiddleCenter);
        var lista = Lista(logros.transform, new Vector2(0, 40), new Vector2(900, 680));
        var volver = Boton(logros.transform, "Volver", new Vector2(0, -400), ui.BotonVolverPausa);

        // ---- Game Over ----
        var over = Panel(t, "GameOver", new Color(0.25f, 0f, 0.05f, 0.7f));
        Txt("Titulo", over.transform, c, new Vector2(0, 160), new Vector2(1200, 120), "Lira ha caído", 84, TextAnchor.MiddleCenter);
        var reintentar = Boton(over.transform, "Reintentar", new Vector2(0, 0), ui.BotonReiniciar);
        Boton(over.transform, "Menú principal", new Vector2(0, -95), ui.BotonMenu);

        // ---- Aviso de logro ----
        var toast = UI("AvisoLogro", t, new Vector2(1, 0), new Vector2(-24, 24), new Vector2(480, 120));
        toast.gameObject.AddComponent<Image>().color = Oscuro;
        var toastIcon = Img("Icono", toast, new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(96, 96), null, Color.white);
        toastIcon.preserveAspect = true;
        var toastText = Txt("Texto", toast, new Vector2(0, 0.5f), new Vector2(120, 0), new Vector2(350, 100), "", 28, TextAnchor.MiddleLeft);
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
        var nueva = Boton(main, "Nueva Partida", new Vector2(0, 120), menu.NuevaPartida);
        var cont = Boton(main, "Continuar", new Vector2(0, 30), menu.Continuar);
        Boton(main, "Logros", new Vector2(0, -60), menu.ShowAchievements);
        Boton(main, "Créditos", new Vector2(0, -150), menu.Creditos);
        Boton(main, "Salir", new Vector2(0, -240), menu.Salir);
        Txt("Ayuda", main, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(1400, 40),
            "Teclado o control de PS4 / Xbox", 24, TextAnchor.MiddleCenter);

        var logros = UI("Logros", t, c, Vector2.zero, new Vector2(1920, 1080));
        Img("Marco", logros, c, new Vector2(0, -20), new Vector2(1150, 1000), marco, Color.white);
        Txt("Titulo", logros, c, new Vector2(0, 430), new Vector2(800, 80), "LOGROS", 60, TextAnchor.MiddleCenter);
        var lista = Lista(logros, new Vector2(0, 40), new Vector2(900, 680));
        var volver = Boton(logros, "Volver", new Vector2(0, -400), menu.ShowMain);

        Set(menu, "continueButton", cont);
        Set(menu, "mainPanel", main.gameObject);
        Set(menu, "achievementsPanel", logros.gameObject);
        Set(menu, "achievementsList", lista.transform);
        Set(menu, "font", font);
        Set(menu, "firstButton", nueva.gameObject);
        Set(menu, "achievementsBack", volver.gameObject);

        string path = $"{Scenes}/{GameManager.MenuScene}.unity";
        EditorSceneManager.SaveScene(scene, path);
        return path;
    }

    static string CrearPrologo()
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

        var escenas = new (string sprite, string texto)[]
        {
            ("Backgrounds/Corazón del Grimorio.jpg", "Hace años, la Archimaga Elenora intentó unir los cuatro elementos en un solo grimorio. Una noche su laboratorio quedó en silencio... y ella desapareció."),
            ("Backgrounds/ALA_DE_APRENDIZAJE.png", "Los maestros de la Torre de Cristal sellaron sus hechizos y prohibieron hablar de ella. Con el tiempo, su nombre se volvió un rumor entre aprendices."),
            ("Characters/Lira.png", "Lira, una aprendiz curiosa e impulsiva, encuentra en un baúl olvidado un grimorio de práctica con la cubierta agrietada. Sus páginas laten con una luz tenue."),
            ("Enemies/Eco_Archimaga_Elenora.png", "Al abrirlo, los hechizos sellados despiertan. Ecos de magia escapan del libro y empiezan a corromper cada ala de la Torre."),
            ("Backgrounds/Ala de Fuego.jpg", "Para restaurar el equilibrio, Lira tendrá que recorrer las cinco alas, aprender los cuatro elementos y enfrentar a los guardianes que protegen los secretos de Elenora.")
        };

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

        string path = $"{Scenes}/{GameManager.PrologueScene}.unity";
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

        var texto = Txt("Texto", t, new Vector2(0.5f, 0f), new Vector2(0, -900), new Vector2(1400, 1800), "", 40, TextAnchor.UpperCenter);
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
