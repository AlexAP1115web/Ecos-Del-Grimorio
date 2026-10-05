using UnityEngine;

// hace que algo flote de arriba abajo y gire despacio
public class Flotar : MonoBehaviour
{
    [SerializeField] private float altura = 0.2f;
    [SerializeField] private float velocidad = 1.2f;
    [SerializeField] private float giro = 0f;

    private Vector3 inicio;
    private float fase;

    void Start()
    {
        inicio = transform.localPosition;
        fase = Random.Range(0f, 6f);
    }

    void Update()
    {
        transform.localPosition = inicio + Vector3.up * Mathf.Sin(Time.time * velocidad + fase) * altura;
        if (giro != 0f) transform.Rotate(0f, 0f, giro * Time.deltaTime);
    }
}
