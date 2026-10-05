using UnityEngine;

// Cámara que sigue a Lira con suavizado, mira un poco hacia donde camina,
// no se sale de los límites del nivel y puede temblar con los golpes fuertes.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(2.5f, 1.2f);
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private float verticalDeadZone = 1.2f;

    [Header("Límites del nivel")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 50f;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 10f;

    static CameraFollow instance;

    private Camera cam;
    private Vector3 velocity;
    private PlayerController playerController;
    private float lookAhead;
    private float focusY;
    private float shakeAmount;
    private float shakeUntil;
    private Vector3 basePosition;

    public static void Shake(float amount, float duration)
    {
        if (instance == null) return;
        instance.shakeAmount = Mathf.Max(instance.Time01() < instance.shakeUntil ? instance.shakeAmount : 0f, amount);
        instance.shakeUntil = Time.unscaledTime + duration;
    }

    float Time01() => Time.unscaledTime;

    void Awake()
    {
        cam = GetComponent<Camera>();
        instance = this;
    }

    void Start()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
        if (target != null)
        {
            playerController = target.GetComponent<PlayerController>();
            focusY = target.position.y;
            basePosition = new Vector3(target.position.x, target.position.y + offset.y, transform.position.z);
            transform.position = Clamp(basePosition);
            basePosition = transform.position;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        float dir = playerController != null && !playerController.FacingRight ? -1f : 1f;
        lookAhead = Mathf.Lerp(lookAhead, offset.x * dir, 3f * Time.deltaTime);

        // Solo sube o baja la cámara cuando Lira se aleja de la zona muerta o está en el suelo
        float dy = target.position.y - focusY;
        if (Mathf.Abs(dy) > verticalDeadZone || (playerController != null && playerController.IsGrounded))
            focusY = Mathf.Lerp(focusY, target.position.y, 4f * Time.deltaTime);

        Vector3 desired = Clamp(new Vector3(target.position.x + lookAhead, focusY + offset.y, transform.position.z));
        basePosition = Vector3.SmoothDamp(basePosition, desired, ref velocity, smoothTime);

        Vector3 shake = Vector3.zero;
        if (Time.unscaledTime < shakeUntil)
            shake = (Vector3)(Random.insideUnitCircle * shakeAmount);

        transform.position = basePosition + shake;
    }

    Vector3 Clamp(Vector3 p)
    {
        if (!useBounds) return p;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        p.x = Mathf.Clamp(p.x, minX + halfWidth, Mathf.Max(minX + halfWidth, maxX - halfWidth));
        p.y = Mathf.Clamp(p.y, minY + halfHeight, Mathf.Max(minY + halfHeight, maxY - halfHeight));
        return p;
    }
}
