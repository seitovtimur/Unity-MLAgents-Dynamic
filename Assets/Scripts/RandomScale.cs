using UnityEngine;

public class RandomScale : MonoBehaviour
{
    [SerializeField] private Vector2 xScaleRange = new Vector2(1f, 3f);
    [SerializeField] private Vector2 zScaleRange = new Vector2(1f, 3f);

    void Start()
    {
        // Берём текущий размер
        Vector3 scale = transform.localScale;

        // Случайный размер по X и Z
        scale.x = Random.Range(xScaleRange.x, xScaleRange.y);
        scale.z = Random.Range(zScaleRange.x, zScaleRange.y);

        // Применяем
        transform.localScale = scale;
    }
}
