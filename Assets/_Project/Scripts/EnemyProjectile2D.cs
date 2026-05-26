using UnityEngine;

public class EnemyProjectile2D : MonoBehaviour
{
    public float speed = 7f;
    public int damage = 1;
    public float lifetime = 3f;
    public LayerMask hitLayers;

    private Vector2 direction = Vector2.left;

    public void SetDirection(Vector2 newDirection)
    {
        if (newDirection.sqrMagnitude <= 0.01f) newDirection = Vector2.left;
        direction = newDirection.normalized;
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (((1 << collision.gameObject.layer) & hitLayers) != 0)
            Destroy(gameObject);
    }
}
