using UnityEngine;

// Cámara que sigue a Lira con suavizado y no se sale de los límites del nivel.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(2f, 1.5f);
    [SerializeField] private float smoothTime = 0.2f;

    [Header("Límites del nivel")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 50f;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 10f;

    private Camera cam;
    private Vector3 velocity;
    private PlayerController playerController;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
        if (target != null) playerController = target.GetComponent<PlayerController>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        float lookAhead = playerController != null && !playerController.FacingRight ? -offset.x : offset.x;
        Vector3 desired = new Vector3(target.position.x + lookAhead, target.position.y + offset.y, transform.position.z);

        if (useBounds)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            desired.x = Mathf.Clamp(desired.x, minX + halfWidth, Mathf.Max(minX + halfWidth, maxX - halfWidth));
            desired.y = Mathf.Clamp(desired.y, minY + halfHeight, Mathf.Max(minY + halfHeight, maxY - halfHeight));
        }

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
