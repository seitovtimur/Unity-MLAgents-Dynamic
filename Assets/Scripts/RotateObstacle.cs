using UnityEngine;

public class RotateObstacle : MonoBehaviour
{
    [SerializeField] private Vector3 minRotationSpeed = new Vector3(0, 70, 0);
    [SerializeField] private Vector3 maxRotationSpeed = new Vector3(0, 150, 0);

    private Vector3 rotationSpeed;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        rotationSpeed = new Vector3(
            GetRandomSpeed(minRotationSpeed.x, maxRotationSpeed.x),
            GetRandomSpeed(minRotationSpeed.y, maxRotationSpeed.y),
            GetRandomSpeed(minRotationSpeed.z, maxRotationSpeed.z)
        );
    }

    void FixedUpdate()
    {
        Quaternion delta = Quaternion.Euler(rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(rb.rotation * delta);
    }

    private float GetRandomSpeed(float min, float max)
    {
        // Выбираем знак: -1 или +1
        int sign = Random.value < 0.5f ? -1 : 1;
        // Берём случайное значение в [min, max]
        return sign * Random.Range(min, max);
    }
}
