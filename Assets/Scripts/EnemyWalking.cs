using UnityEngine;

public class EnemyWalking : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;

    public float speed = 2f;

    private Transform target;

    void Start()
    {
        target = pointA;
    }

    void Update()
    {
        MoveToTarget();
    }

    void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance < 0.1f)
        {
            SwitchTarget();
        }
    }

    void SwitchTarget()
    {
        if (target == pointA)
            target = pointB;
        else
            target = pointA;
    }
}
