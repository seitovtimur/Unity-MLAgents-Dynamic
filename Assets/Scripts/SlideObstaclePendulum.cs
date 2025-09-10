using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SlideObstaclePendulum : MonoBehaviour
{
    [SerializeField] private float moveDistance = 4f; // Насколько далеко уходит от центра
    [SerializeField] private Vector2 speedRange = new Vector2(1f, 2f); // Диапазон частоты колебаний

    private Rigidbody rb;
    private Vector3 startPos;
    private float speed;
    private float timeOffset;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;       // управляем только кодом
        rb.interpolation = RigidbodyInterpolation.Interpolate; // для плавности движения

        startPos = transform.position;

        speed = Random.Range(speedRange.x, speedRange.y);
        timeOffset = Random.Range(0f, Mathf.PI * 2f); // случайная фаза
    }

    void FixedUpdate()
    {
        // Синусоидальное маятниковое движение
        float t = Mathf.Sin(Time.time * speed + timeOffset);
        Vector3 targetPos = startPos + transform.right * t * moveDistance;

        rb.MovePosition(targetPos);
    }
}
