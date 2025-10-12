using UnityEngine;

public class PhysicsParticleEmitter : MonoBehaviour
{
    [SerializeField] private GameObject particlePrefab; // Префаб частицы с Rigidbody + Collider
    [SerializeField] private float spawnRate = 20f;     // Частиц в секунду
    [SerializeField] private float particleSpeed = 5f;  // Начальная скорость
    [SerializeField] private float particleLifetime = 3f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        float interval = 1f / spawnRate;

        while (timer >= interval)
        {
            SpawnParticle();
            timer -= interval;
        }
    }

    void SpawnParticle()
    {
        GameObject particle = Instantiate(particlePrefab, transform.position, transform.rotation);
        Rigidbody rb = particle.GetComponent<Rigidbody>();

        // Толкаем вперёд как у Particle System
        rb.linearVelocity = transform.forward * particleSpeed;

        // Уничтожаем после жизни
        Destroy(particle, particleLifetime);
    }
}
