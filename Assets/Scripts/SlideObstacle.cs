using UnityEngine;

public class SlideObstacle : MonoBehaviour
{
    [SerializeField] private float moveDistance = 4f; // на сколько двигаться влево и вправо
    [SerializeField] private Vector2 speedRange = new Vector2(2f, 4f);
    [SerializeField] private Vector2 sizeRange = new Vector2(3f, 6f);
    [SerializeField] private float height = 3;

    
    private Vector3 pointA;
    private Vector3 pointB;
    private float speed;
    private bool movingToB = true;

    void Start()
    {
        // Центральная позиция — начальная позиция
        Vector3 center = transform.localPosition;
        float width = Random.Range(sizeRange.x, sizeRange.y);
        transform.localScale = new Vector3(width, height, 0.35f);

        speed = Random.Range(speedRange.x, speedRange.y);

        //Левая и правая крайние точки по локальной оси X
        pointA = center - transform.right * moveDistance;
        pointB = center + transform.right * moveDistance;

        //Vector3 moveDir = transform.parent != null ? transform.parent.right : transform.right;
        //pointA = center - moveDir * moveDistance;
        //pointB = center + moveDir * moveDistance;

    }

    void Update()
    {
        Vector3 target = movingToB ? pointB : pointA;

        transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.localPosition, target) < 0.05f)
        {
            movingToB = !movingToB;
        }
    }
}
