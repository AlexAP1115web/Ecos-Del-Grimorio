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

    static Sprite square, circle;
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
        foreach (var r in romanos)
            Item("Fragmento de Grimorio " + r, TipoItem.FragmentoGrimorio, 1, "Items/Fragmento de Grimorio.png", "Revela el pasado de la Archimaga Elenora.");
        Item("Núcleo de Ascua", TipoItem.NucleoDeAscua, 0, "Items/Núcleo de Ascua.png", "Tu hechizo de Fuego hace más daño.");
        Item("Anillo de Escarcha", TipoItem.AnilloDeEscarcha, 0, "Items/Anillo de Escarcha.png", "Recibes menos daño de fuego.");
        Item("Pluma Ligera", TipoItem.PlumaLigera, 0, "Items/Pluma Ligera.png", "Saltas más alto.");
        Item("Llave Rúnica", TipoItem.LlaveRunica, 0, "Items/Llave Rúnica.png", "Abre el cofre sellado del Ala de Aprendizaje.");
        Item("Nota cifrada de Elenora", TipoItem.Coleccionable, 0, "Items/Fragmento de Grimorio.png", "Primer indicio del plan de la Archimaga.");
        Item("Diario de la Archimaga Elenora", TipoItem.Coleccionable, 0, "Items/Diario de la Archimaga Elenora.png", "Las últimas palabras de Elenora.");
        Item("Grimorio Completo", TipoItem.Coleccionable, 0, "Items/Grimorio Completo.png", "El grimorio vuelve a estar completo.");

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
        return SavePrefab(go, $"{Prefabs}/{name}.prefab");
    }

    static void Item(string name, TipoItem type, float amount, string sprite, string description)
    {
        var item = LoadOrCreate<ItemData>($"{Data}/Items/{FileName(name)}.asset");
        item.itemName = name;
        item.type = type;
        item.amount = amount;
        item.icon = LoadSprite(Sprites + sprite);
        item.description = description;
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
        var go = new GameObject(name);
        go.tag = "Enemy";
        var art = LoadSprite(Sprites + sprite);
        sr = AddSprite(go, art != null ? art : circle, height, 8);
        if (art == null) sr.color = new Color(0.5f, 0.3f, 0.7f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = flying ? 0f : 3f;
        rb.freezeRotation = true;

        Bounds b = sr.sprite.bounds;
        if (flying)
        {
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = Mathf.Min(b.extents.x, b.extents.y) * 0.8f;
            c.offset = b.center;
        }
        else
        {
            var c = go.AddComponent<CapsuleCollider2D>();
            bool wide = b.size.x > b.size.y;
            c.direction = wide ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
            c.size = wide ? new Vector2(b.size.x * 0.85f, b.size.y * 0.8f) : new Vector2(b.size.x * 0.55f, b.size.y * 0.92f);
            c.offset = new Vector2(b.center.x, b.center.y - b.size.y * 0.03f);
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

    static GameObject LiraPrefab()
    {
        var go = new GameObject("Lira");
        go.tag = "Player";
        var sprite = LoadSprite(Sprites + "Characters/Lira.png");
        AddSprite(go, sprite, 1.7f, 10);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<CapsuleCollider2D>();
        col.sharedMaterial = noFriction;
        Bounds b = sprite != null ? sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
        col.size = new Vector2(b.size.x * 0.4f, b.size.y * 0.95f);
        col.offset = new Vector2(b.center.x, b.center.y);

        var groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(go.transform, false);
        groundCheck.localPosition = new Vector3(b.center.x, b.min.y, 0f);

        var health = go.AddComponent<Health>();
        Set(health, "maxHealth", 100f);
        Set(health, "invulnerableTime", 1f);
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
    // Construcción de escenas
    // =====================================================================

    class Nivel
    {
        public UnityEngine.SceneManagement.Scene scene;
        public Transform geo, enemigos, items;
        public GameObject lira;
        public Color piedra, plataforma;
    }

    static Nivel NuevoNivel(string fondo, float minX, float maxX, Vector2 inicio, Color piedra, Color plataforma, Color cielo)
    {
        var n = new Nivel { piedra = piedra, plataforma = plataforma };
        n.scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GlobalLight();
        PrefabUtility.InstantiatePrefab(P["GameManager"]);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = cielo;
        camGo.transform.position = new Vector3(inicio.x, 0f, -10f);
        var follow = camGo.AddComponent<CameraFollow>();
        Set(follow, "minX", minX);
        Set(follow, "maxX", maxX);
        Set(follow, "minY", -7f);
        Set(follow, "maxY", 12f);

        var bg = LoadSprite(Sprites + "Backgrounds/" + fondo);
        if (bg != null)
        {
            var go = new GameObject("Fondo");
            go.transform.SetParent(camGo.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 20f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = bg;
            sr.sortingOrder = -100;
            go.transform.localScale = Vector3.one * Mathf.Max(13f / bg.bounds.size.y, 25f / bg.bounds.size.x);
        }

        n.geo = new GameObject("Nivel").transform;
        n.enemigos = new GameObject("Enemigos").transform;
        n.items = new GameObject("Items").transform;

        Bloque(n, "Pared_Izq", new Vector2(minX - 0.5f, 3f), new Vector2(1f, 20f), piedra, false);
        Bloque(n, "Pared_Der", new Vector2(maxX + 0.5f, 3f), new Vector2(1f, 20f), piedra, false);

        var kill = new GameObject("ZonaDeCaida");
        kill.transform.SetParent(n.geo, false);
        kill.transform.position = new Vector2((minX + maxX) / 2f, -12f);
        var killCol = kill.AddComponent<BoxCollider2D>();
        killCol.isTrigger = true;
        killCol.size = new Vector2(maxX - minX + 20f, 2f);
        kill.AddComponent<KillZone>();

        n.lira = (GameObject)PrefabUtility.InstantiatePrefab(P["Lira"]);
        n.lira.transform.position = new Vector3(inicio.x, G + 1f, 0f);

        HUD(n.lira);
        return n;
    }

    static string Guardar(Nivel n, string nombre)
    {
        string path = $"{Scenes}/{nombre}.unity";
        EditorSceneManager.SaveScene(n.scene, path);
        return path;
    }

    static void GlobalLight()
    {
        var lightGo = new GameObject("Global Light 2D");
        var type = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.2D.Runtime");
        if (type == null) return;
        var light = lightGo.AddComponent(type);
        var so = new SerializedObject(light);
        var prop = so.FindProperty("m_LightType");
        if (prop != null)
        {
            prop.intValue = 4; // Global
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static GameObject Bloque(Nivel n, string name, Vector2 center, Vector2 size, Color color, bool esSuelo = true)
    {
        var go = new GameObject(name);
        if (esSuelo) go.tag = "Ground"; // las paredes no cuentan como suelo para que no se pueda escalar
        go.transform.SetParent(n.geo, false);
        go.transform.position = center;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        sr.sortingOrder = 1;
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    static void Suelo(Nivel n, float x1, float x2) =>
        Bloque(n, $"Suelo_{x1}_{x2}", new Vector2((x1 + x2) / 2f, G - 1f), new Vector2(x2 - x1, 2f), n.piedra);

    static void Plat(Nivel n, float x, float top, float w) =>
        Bloque(n, $"Plataforma_{x}", new Vector2(x, top - 0.2f), new Vector2(w, 0.4f), n.plataforma);

    static void PlatMovil(Nivel n, float x, float top, float w, Vector2 offset, float speed)
    {
        var go = Bloque(n, $"PlataformaMovil_{x}", new Vector2(x, top - 0.2f), new Vector2(w, 0.4f), n.plataforma * new Color(1f, 1f, 1.2f));
        var mp = go.AddComponent<MovingPlatform>();
        Set(mp, "offset", offset);
        Set(mp, "speed", speed);
    }

    static void Decoracion(Nivel n, float x1, float x2, Color color)
    {
        var go = new GameObject("Lava");
        go.transform.SetParent(n.geo, false);
        go.transform.position = new Vector2((x1 + x2) / 2f, G - 1.6f);
        go.transform.localScale = new Vector3(x2 - x1, 1.2f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
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
    }

    static void Dialogo(Nivel n, float x, string quien, string sprite, float altura, params string[] lineas)
    {
        var go = new GameObject("Dialogo_" + FileName(quien));
        go.transform.SetParent(n.items, false);
        if (sprite != null)
        {
            var s = LoadSprite(Sprites + sprite);
            if (s != null) AddSprite(go, s, altura, 9);
        }
        go.transform.position = new Vector2(x, G + altura / 2f);
        var d = go.AddComponent<NPCDialogue>();
        Set(d, "speakerName", quien);
        SetStrings(d, "lines", lineas);
        Set(d, "talkRange", sprite != null ? 2.5f : 4f);
    }

    static GameObject Jefe(Nivel n, string prefab, float x, float y, float arenaMin, float arenaMax, float hover)
    {
        var go = Poner(n, prefab, x, y, n.enemigos);
        var boss = go.GetComponent<BossController>();
        Set(boss, "arenaMinX", arenaMin);
        Set(boss, "arenaMaxX", arenaMax);
        Set(boss, "hoverHeight", hover);
        Set(boss, "rewardPosition", new Vector2(x, G + 0.8f));
        Set(boss, "activationRange", 13f);
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
        var n = NuevoNivel("ALA_DE_APRENDIZAJE.png", -12f, 62f, new Vector2(-8f, 0f),
            new Color(0.25f, 0.2f, 0.32f), new Color(0.55f, 0.4f, 0.25f), new Color(0.08f, 0.06f, 0.12f));

        Suelo(n, -12, 22); Suelo(n, 25, 62);
        Plat(n, 6, -2, 3); Plat(n, 10, 0, 3); Plat(n, 14, -2, 3);
        Plat(n, 30, -2, 4); Plat(n, 35, 0, 3); Plat(n, 40, 2, 3); Plat(n, 44.5f, 4, 2.5f);

        Dialogo(n, -10.5f, "Maestra Sable", "Characters/Maestra Sable.png", 1.9f,
            "Ese grimorio perteneció a alguien que intentó ir más allá de lo permitido. Ten cuidado con lo que despiertas, Lira.",
            "Empecemos por lo básico. Repite conmigo el primer sello arcano.",
            "A/D para moverte, Espacio para saltar y 1 para lanzar el hechizo Arcano. Con Q cambias el hechizo equipado.");

        var cofre = new GameObject("Cofre_Secreto");
        cofre.transform.SetParent(n.items, false);
        var csr = AddSprite(cofre, square, 0.9f, 3);
        cofre.transform.localScale = new Vector3(1.2f, 0.9f, 1f);
        csr.color = new Color(0.5f, 0.32f, 0.15f);
        cofre.transform.position = new Vector2(-6f, G + 0.45f);
        cofre.AddComponent<SecretChest>();

        Enemigo(n, "Espectro", 2); Enemigo(n, "Espectro", 10, 0, 1); Enemigo(n, "Espectro", 28); Enemigo(n, "Espectro", 38);
        Volador(n, "Mota", 18, 1.5f); Volador(n, "Mota", 33, 2.5f);
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
        var n = NuevoNivel("Ala de Fuego.jpg", -12f, 92f, new Vector2(-9f, 0f),
            new Color(0.22f, 0.12f, 0.1f), new Color(0.45f, 0.2f, 0.1f), new Color(0.15f, 0.05f, 0.03f));

        Suelo(n, -12, 15); Suelo(n, 18, 40); Suelo(n, 43, 92);
        Decoracion(n, 15, 18, new Color(1f, 0.4f, 0.05f)); Decoracion(n, 40, 43, new Color(1f, 0.4f, 0.05f));
        Plat(n, 8, -2, 3); Plat(n, 24, -2, 3); Plat(n, 28, 0, 3); Plat(n, 32, -2, 3); Plat(n, 52, -2, 3);

        Enemigo(n, "Centinela", 5, G, 2); Enemigo(n, "Salamandra", 12); Enemigo(n, "Centinela", 22, G, 2);
        Enemigo(n, "Salamandra", 35); Enemigo(n, "Centinela", 47, G, 2); Enemigo(n, "Salamandra", 55);

        Objeto(n, "Cristal de Maná Grande", 28, 0);
        Objeto(n, "Poción Menor de Vida", 52, -2);
        PuntoReaparicion(n, 60);

        Dialogo(n, 64, "Kaelor", null, 2f, "Los ecos no perdonan a quien despierta el fuego dormido.");
        var kaelor = Jefe(n, "Kaelor", 80, G + 1.6f, 66, 90, 0);

        Salida(n, 90, kaelor.GetComponent<Health>(), "Nivel3_AlaDeHielo", true, Elemento.Hielo);
        return Guardar(n, "Nivel2_AlaDeFuego");
    }

    static string Nivel3()
    {
        var n = NuevoNivel("Ala de Hielo.png", -12f, 92f, new Vector2(-9f, 0f),
            new Color(0.35f, 0.5f, 0.65f), new Color(0.7f, 0.85f, 0.95f), new Color(0.05f, 0.08f, 0.15f));

        Suelo(n, -12, 20); Suelo(n, 23, 45); Suelo(n, 48, 92);
        Plat(n, 6, -2, 3); Plat(n, 10, 0, 3); Plat(n, 30, -2, 3); Plat(n, 34, 0, 3); Plat(n, 38, 2, 3);

        Enemigo(n, "Escarchado", 4); Enemigo(n, "Cristal", 16); Enemigo(n, "Escarchado", 27);
        Enemigo(n, "Cristal", 43); Enemigo(n, "Escarchado", 52); Enemigo(n, "Cristal", 57);

        Objeto(n, "Cristal de Maná Grande", 10, 0);
        Objeto(n, "Poción Menor de Vida", 34, 0);
        Objeto(n, "Nota cifrada de Elenora", 38, 2);
        PuntoReaparicion(n, 62);

        Dialogo(n, 64, "Isolde", null, 2f, "Fui aprendiz de la Archimaga Elenora. Desde que sellaron los hechizos, nadie cruza esta ala.");
        var isolde = Jefe(n, "Isolde", 80, G + 1.5f, 66, 90, 0);
        Set(isolde.GetComponent<IsoldeBoss>(), "groundY", G);

        Salida(n, 90, isolde.GetComponent<Health>(), "Nivel4_AlaDeViento", true, Elemento.Viento);
        return Guardar(n, "Nivel3_AlaDeHielo");
    }

    static string Nivel4()
    {
        var n = NuevoNivel("Ala de Viento.jpg", -12f, 100f, new Vector2(-9f, 0f),
            new Color(0.55f, 0.6f, 0.6f), new Color(0.85f, 0.9f, 0.9f), new Color(0.35f, 0.55f, 0.75f));

        Suelo(n, -12, 10); Suelo(n, 18, 28); Suelo(n, 38, 48); Suelo(n, 58, 100);
        PlatMovil(n, 12.5f, G, 3, new Vector2(4f, 0f), 2f);
        PlatMovil(n, 30.5f, G, 3, new Vector2(6f, 0f), 2.5f);
        PlatMovil(n, 50.5f, G, 3, new Vector2(6f, 0f), 2.5f);
        Plat(n, 22, -2, 3); Plat(n, 42, -2, 3); Plat(n, 45, 0, 3);
        Plat(n, 78, -2, 3); Plat(n, 86, -1, 3); Plat(n, 94, -2, 3);

        Volador(n, "Ave", 6, 0); Volador(n, "Ave", 23, 1); Volador(n, "Golem", 33, 0);
        Volador(n, "Ave", 43, 2); Volador(n, "Golem", 53, 0); Volador(n, "Ave", 62, 1);

        Objeto(n, "Cristal de Maná Grande", 22, -2);
        Objeto(n, "Diario de la Archimaga Elenora", 45, 0);
        Objeto(n, "Poción Menor de Vida", 60, G);
        PuntoReaparicion(n, 66);

        Dialogo(n, 69, "Threnody", null, 2f, "Los cuatro guardianes fuimos aprendices de la Archimaga Elenora antes de que desapareciera.");
        var threnody = Jefe(n, "Threnody", 88, 2f, 72, 98, 2f);

        Salida(n, 98, threnody.GetComponent<Health>(), "Nivel5_CorazonDelGrimorio", false, Elemento.Arcano);
        return Guardar(n, "Nivel4_AlaDeViento");
    }

    static string Nivel5()
    {
        var n = NuevoNivel("Corazón del Grimorio.jpg", -12f, 92f, new Vector2(-7f, 0f),
            new Color(0.2f, 0.12f, 0.3f), new Color(0.65f, 0.5f, 0.2f), new Color(0.06f, 0.03f, 0.1f));

        Suelo(n, -12, 92);
        Plat(n, 8, -2, 3); Plat(n, 16, 0, 3); Plat(n, 24, -2, 3);
        Plat(n, 66, -2, 3); Plat(n, 74, 0, 3); Plat(n, 82, -2, 3);

        Dialogo(n, -9.5f, "Maestra Sable", "Characters/Maestra Sable.png", 1.9f,
            "Lira, el eco de Elenora te espera en el corazón del grimorio.",
            "Ella dominaba los cuatro elementos. Combínalos como te enseñé y no la dejes resistir el mismo hechizo dos veces.");

        Enemigo(n, "Eco", 6); Enemigo(n, "Eco", 16, 0, 1); Enemigo(n, "Eco", 30); Enemigo(n, "Espejo", 40); Enemigo(n, "Eco", 48);

        Objeto(n, "Cristal de Maná Grande", 24, -2);
        Objeto(n, "Poción Mayor de Vida", 52, G);
        PuntoReaparicion(n, 56);

        Dialogo(n, 60, "Eco de Elenora", null, 2f, "¿Viniste a terminar lo que yo empecé, aprendiz?");
        var elenora = Jefe(n, "Elenora", 76, 2f, 62, 90, 2f);
        Set(elenora.GetComponent<ElenoraBoss>(), "groundY", G);

        Salida(n, 90, elenora.GetComponent<Health>(), GameManager.CreditsScene, false, Elemento.Arcano);
        return Guardar(n, "Nivel5_CorazonDelGrimorio");
    }

    // =====================================================================
    // Interfaz: HUD, menú y créditos
    // =====================================================================

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

    static Image Img(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Sprite sprite, Color color)
    {
        var img = UI(name, parent, anchor, pos, size).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = sprite != null && sprite != square;
        return img;
    }

    static Text Txt(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string text, int fontSize, TextAnchor align)
    {
        var t = UI(name, parent, anchor, pos, size).gameObject.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = new Color(1f, 0.92f, 0.7f);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
        return t;
    }

    static Image Barra(Transform parent, string name, string spritePath, Vector2 pos)
    {
        var sprite = LoadSprite(spritePath);
        float w = 420f, h = sprite != null ? w * sprite.rect.height / sprite.rect.width : 40f;
        var tl = new Vector2(0, 1);
        Img(name + "_Fondo", parent, tl, pos, new Vector2(w, h), sprite, new Color(0.3f, 0.3f, 0.3f, 0.9f));
        var fill = Img(name, parent, tl, pos, new Vector2(w, h), sprite, Color.white);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        return fill;
    }

    static void HUD(GameObject lira)
    {
        var canvas = NuevoCanvas("HUD", 0);
        var hud = canvas.gameObject.AddComponent<HUD>();
        var t = canvas.transform;
        var tl = new Vector2(0, 1);

        var vida = Barra(t, "BarraVida", Sprites + "UI/Barra de vida.png", new Vector2(30, -30));
        var mana = Barra(t, "BarraMana", Sprites + "UI/Barra de maná.png", new Vector2(30, -120));

        var icons = new Image[3];
        var frames = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var pos = new Vector2(30 + i * 100, -215);
            frames[i] = Img("Espacio_" + (i + 1), t, tl, pos, new Vector2(88, 88), square, new Color(0, 0, 0, 0.5f));
            icons[i] = Img("Icono_" + (i + 1), frames[i].transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(74, 74), null, Color.white);
            icons[i].preserveAspect = true;
            Txt("Tecla", frames[i].transform, new Vector2(0, 1), new Vector2(4, -2), new Vector2(30, 30), (i + 1).ToString(), 20, TextAnchor.UpperLeft);
        }

        var fragments = Txt("Fragmentos", t, tl, new Vector2(30, -315), new Vector2(400, 36), "Fragmentos: 0/5", 28, TextAnchor.MiddleLeft);

        // Barra del jefe
        var top = new Vector2(0.5f, 1);
        var panel = UI("Jefe", t, top, new Vector2(0, -30), new Vector2(820, 80));
        var bossName = Txt("Nombre", panel, top, Vector2.zero, new Vector2(820, 34), "Jefe", 26, TextAnchor.MiddleCenter);
        Img("Fondo", panel, top, new Vector2(0, -40), new Vector2(800, 26), square, new Color(0, 0, 0, 0.7f));
        var bossFill = Img("Vida", panel, top, new Vector2(0, -40), new Vector2(800, 26), square, new Color(0.8f, 0.15f, 0.25f));
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
    }

    static void EventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    static Button Boton(Transform parent, string texto, Vector2 pos, UnityAction accion)
    {
        var img = Img("Boton_" + texto, parent, new Vector2(0.5f, 0.5f), pos, new Vector2(420, 70), square, new Color(0.18f, 0.1f, 0.3f, 0.9f));
        var btn = img.gameObject.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.45f, 0.3f, 0.7f);
        colors.pressedColor = new Color(0.7f, 0.55f, 0.2f);
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        btn.colors = colors;
        Txt("Texto", img.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 70), texto, 32, TextAnchor.MiddleCenter);
        UnityEventTools.AddPersistentListener(btn.onClick, accion);
        return btn;
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

        var fondo = Img("Fondo", t, c, Vector2.zero, new Vector2(1920, 1080), LoadSprite(Sprites + "Backgrounds/Corazón del Grimorio.jpg"), Color.white);
        fondo.preserveAspect = false;
        Img("Oscurecer", t, c, Vector2.zero, new Vector2(1920, 1080), square, new Color(0, 0, 0, 0.45f));

        var menu = canvas.gameObject.AddComponent<MainMenu>();
        var marco = LoadSprite(Sprites + "UI/Marco de menú.png");

        var main = UI("Principal", t, c, Vector2.zero, new Vector2(1920, 1080));
        Txt("Titulo", main, c, new Vector2(0, 360), new Vector2(1400, 140), "ECOS DEL GRIMORIO", 96, TextAnchor.MiddleCenter);
        Img("Marco", main, c, new Vector2(0, -60), new Vector2(700, 640), marco, Color.white);
        Boton(main, "Nueva Partida", new Vector2(0, 120), menu.NuevaPartida);
        var cont = Boton(main, "Continuar", new Vector2(0, 30), menu.Continuar);
        Boton(main, "Logros", new Vector2(0, -60), menu.ShowAchievements);
        Boton(main, "Créditos", new Vector2(0, -150), menu.Creditos);
        Boton(main, "Salir", new Vector2(0, -240), menu.Salir);

        var logros = UI("Logros", t, c, Vector2.zero, new Vector2(1920, 1080));
        Img("Marco", logros, c, new Vector2(0, -20), new Vector2(1150, 1000), marco, Color.white);
        Txt("Titulo", logros, c, new Vector2(0, 430), new Vector2(800, 80), "LOGROS", 60, TextAnchor.MiddleCenter);
        var lista = UI("Lista", logros, c, new Vector2(0, 40), new Vector2(900, 680));
        var layout = lista.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        Boton(logros, "Volver", new Vector2(0, -400), menu.ShowMain);

        Set(menu, "continueButton", cont);
        Set(menu, "mainPanel", main.gameObject);
        Set(menu, "achievementsPanel", logros.gameObject);
        Set(menu, "achievementsList", lista);
        Set(menu, "font", font);

        string path = $"{Scenes}/{GameManager.MenuScene}.unity";
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
        var c = new Vector2(0.5f, 0.5f);
        var fondo = Img("Fondo", t, c, Vector2.zero, new Vector2(1920, 1080), LoadSprite(Sprites + "Backgrounds/ALA_DE_APRENDIZAJE.png"), new Color(0.4f, 0.4f, 0.5f));
        fondo.preserveAspect = false;

        var texto = Txt("Texto", t, new Vector2(0.5f, 0f), new Vector2(0, -900), new Vector2(1400, 1800), "", 38, TextAnchor.UpperCenter);
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
