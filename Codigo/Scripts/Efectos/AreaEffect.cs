using UnityEngine;

// Círculo que aparece y se desvanece. Se usa para los combos y las explosiones
// mientras no tengamos los sprites finales de cada efecto.
public class AreaEffect : MonoBehaviour
{
    private static Sprite circleSprite;

    private SpriteRenderer sr;
    private float duration;
    private float timer;
    private Color baseColor;

    public static AreaEffect Spawn(Vector2 position, float radius, Color color, float duration)
    {
        return Spawn(position, radius, color, duration, null);
    }

    // Con sprite: usa el arte del hechizo en lugar del círculo
    public static AreaEffect Spawn(Vector2 position, float radius, Color color, float duration, Sprite sprite)
    {
        var go = new GameObject("Efecto");
        go.transform.position = position;
        Sprite s = sprite != null ? sprite : GetCircleSprite();
        float size = Mathf.Max(s.bounds.size.x, s.bounds.size.y);
        go.transform.localScale = Vector3.one * (radius * 2f / size);

        var effect = go.AddComponent<AreaEffect>();
        effect.sr = go.AddComponent<SpriteRenderer>();
        effect.sr.sprite = s;
        effect.sr.sortingOrder = 20;
        effect.baseColor = color;
        effect.sr.color = color;
        effect.duration = duration;
        return effect;
    }

    // Aplica daño a todo lo que tenga Health dentro del radio.
    // Devuelve cuántos objetivos golpeó.
    public static int Damage(Vector2 center, float radius, float damage, Elemento? element, bool hitsPlayer)
    {
        int hits = 0;
        var colliders = Physics2D.OverlapCircleAll(center, radius);
        var alreadyHit = new System.Collections.Generic.HashSet<Health>();

        foreach (var col in colliders)
        {
            var health = col.GetComponentInParent<Health>();
            if (health == null || alreadyHit.Contains(health)) continue;

            bool isPlayer = health.CompareTag("Player");
            if (isPlayer != hitsPlayer) continue;

            alreadyHit.Add(health);
            health.TakeDamage(damage, element);
            hits++;
        }
        return hits;
    }

    public static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = d <= 1f ? Mathf.Clamp01((1f - d) * 3f) : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;
        var c = baseColor;
        c.a = baseColor.a * (1f - t);
        sr.color = c;
        if (t >= 1f) Destroy(gameObject);
    }
}
