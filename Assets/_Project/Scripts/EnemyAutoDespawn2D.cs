using UnityEngine;

public class EnemyAutoDespawn2D : MonoBehaviour
{
    public float behindCameraDistance = 18f;
    public float belowCameraDistance = 22f;

    private Damageable damageable;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
    }

    private void Update()
    {
        if (Camera.main == null || (damageable != null && damageable.IsDead))
            return;

        float cameraLeftEdge = Camera.main.transform.position.x - Camera.main.orthographicSize * Camera.main.aspect;
        if (transform.position.x < cameraLeftEdge - behindCameraDistance)
        {
            Destroy(gameObject);
            return;
        }

        float cameraBottomEdge = Camera.main.transform.position.y - Camera.main.orthographicSize;
        if (transform.position.y < cameraBottomEdge - belowCameraDistance)
            Destroy(gameObject);
    }
}
