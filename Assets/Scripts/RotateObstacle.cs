using UnityEngine;

public class RotateObstacle : MonoBehaviour
{
    [SerializeField] private Vector3 minRotationSpeed = new Vector3(0, -150, 0);
    [SerializeField] private Vector3 maxRotationSpeed = new Vector3(0, 150, 0);

    private Vector3 rotationSpeed;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        
        rotationSpeed = new Vector3(
            Random.Range(minRotationSpeed.x, maxRotationSpeed.x),
            Random.Range(minRotationSpeed.y, maxRotationSpeed.y),
            Random.Range(minRotationSpeed.z, maxRotationSpeed.z)
        );
    }

    void FixedUpdate()
    {
        Quaternion delta = Quaternion.Euler(rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(rb.rotation * delta);
    }
}