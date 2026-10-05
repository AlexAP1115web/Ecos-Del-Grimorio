using UnityEngine;

// Efecto de profundidad: la capa se mueve más lento que la cámara.
// factor 0 = se mueve con el mundo (primer plano), 1 = se queda pegada a la cámara (muy lejos).
public class Parallax : MonoBehaviour
{
    [SerializeField] private float factor = 0.5f;
    [SerializeField] private float factorVertical = 0.3f;

    Transform cam;
    Vector3 startPos;
    Vector3 camStart;

    void Start()
    {
        if (Camera.main == null) return;
        cam = Camera.main.transform;
        startPos = transform.position;
        camStart = cam.position;
    }

    void LateUpdate()
    {
        if (cam == null) return;
        Vector3 delta = cam.position - camStart;
        transform.position = new Vector3(startPos.x + delta.x * factor, startPos.y + delta.y * factorVertical, startPos.z);
    }
}
