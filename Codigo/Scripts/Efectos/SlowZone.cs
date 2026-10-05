using UnityEngine;

// zona de suelo congelado: ralentiza a Lira mientras esta encima
[RequireComponent(typeof(BoxCollider2D))]
public class SlowZone : MonoBehaviour
{
    [SerializeField] private float slowMultiplier = 0.45f;
    [SerializeField] private float lifetime = 4f;

    private SpriteRenderer sr;
    private float endTime;

    public static SlowZone Spawn(Vector2 center, float width, float duration)
    {
        var go = new GameObject("SueloCongelado");
        go.transform.position = center;
        go.transform.localScale = new Vector3(width, 0.35f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AreaEffect.GetCircleSprite();
        sr.color = new Color(0.7f, 0.95f, 1f, 0.8f);
        sr.sortingOrder = 4;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        var zone = go.AddComponent<SlowZone>();
        zone.lifetime = duration;
        return zone;
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        endTime = Time.time + lifetime;
    }

    void Update()
    {
        if (Time.time >= endTime) Destroy(gameObject);
        else if (sr != null && endTime - Time.time < 1f)
        {
            var c = sr.color;
            c.a = 0.8f * (endTime - Time.time);
            sr.color = c;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var controller = other.GetComponentInParent<PlayerController>();
        if (controller != null) controller.ApplySlow(slowMultiplier, 0.25f);
    }
}
