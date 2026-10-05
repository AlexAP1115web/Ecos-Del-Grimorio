using UnityEngine;

// plataforma que va y viene, Lira se mueve con ella
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Vector2 offset = new Vector2(4f, 0f);
    [SerializeField] private float speed = 2f;
    [SerializeField] private float waitTime = 0.5f;

    private Rigidbody2D rb;
    private Vector2 start;
    private Vector2 end;
    private bool goingToEnd = true;
    private float waitUntil;

    public Vector2 Velocity { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        start = transform.position;
        end = start + offset;
    }

    void FixedUpdate()
    {
        if (Time.time < waitUntil)
        {
            Velocity = Vector2.zero;
            return;
        }

        Vector2 target = goingToEnd ? end : start;
        Vector2 next = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);
        Velocity = (next - rb.position) / Time.fixedDeltaTime;
        rb.MovePosition(next);

        if (Vector2.Distance(next, target) < 0.01f)
        {
            goingToEnd = !goingToEnd;
            waitUntil = Time.time + waitTime;
        }
    }

    void OnDrawGizmos()
    {
        Vector3 a = Application.isPlaying ? (Vector3)start : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, a + (Vector3)offset);
    }
}
